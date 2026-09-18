using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using Dictionary = Godot.Collections.Dictionary;

namespace Phagocyte.Core;

/// <summary>
/// Persists per-run settlement records (victory / defeat) and exposes
/// aggregate statistics for the medical record screens.
/// </summary>
public partial class RunRecordManager : Node
{
    private static string _savePath = "";
    public static string SavePath
    {
        get
        {
            if (string.IsNullOrEmpty(_savePath))
            {
                using var probe = FileAccess.Open("user://.probe", FileAccess.ModeFlags.Write);
                if (probe != null)
                {
                    probe.Close();
                    DirAccess.RemoveAbsolute("user://.probe");
                    _savePath = "user://run_records.json";
                }
                else
                {
                    _savePath = "res://.user_data/run_records.json";
                    DirAccess.MakeDirRecursiveAbsolute("res://.user_data");
                }
            }
            return _savePath;
        }
        set => _savePath = value;
    }

    public const int MaxRecords = 50;

    public const string ResultVictory = "victory";
    public const string ResultDefeat = "defeat";

    /// <summary>Standard clear requirement: survive the full 15:00 infection timeline.</summary>
    public const float StandardClearSeconds = 900.0f;

    // Settlement causes (docs/record.md §3)
    public const string CauseSpecificNeutralization = "specific_neutralization"; // 特異性中和成功
    public const string CauseMembraneRupture = "membrane_rupture";               // SIRS / 敗血性休克陣亡
    public const string CauseSystemFailure = "system_failure";                   // Content/setup failure fallback

    // Difficulty tiers for scoring / ranking (docs/record.md §4.2)
    public const string DifficultyNormal = "normal";
    public const string DifficultyHard = "hard";

    // Clinical grades (docs/record.md §4.3)
    public const string RankS = "S";
    public const string RankA = "A";
    public const string RankB = "B";
    public const string RankC = "C";
    public const string RankD = "D";

    // Pathological score weights (docs/record.md §4.2)
    public const float SurvivalScorePerSecond = 10.0f;
    public const int LevelScoreBonus = 100;
    public const int ClearBonus = 10000;
    public const float HardDifficultyMultiplier = 1.5f;

    /// <summary>Kill flux: pathogens killed per minute of survival.</summary>
    public static float ComputeKpm(int kills, float survivalTime)
    {
        if (survivalTime <= 0.01f)
            return 0.0f;
        return kills / (survivalTime / 60.0f);
    }

    /// <summary>
    /// Clinical grade (docs/record.md §4.3), evaluated top-down:
    /// S = Hard clear with ≥3,500 kills and KPM ≥230;
    /// A = Normal clear or Hard survival past 12:00 with ≥2,000 kills and KPM ≥130;
    /// B = survival past 08:00 with ≥800 kills; C = past 04:00 with ≥300 kills; D otherwise.
    /// </summary>
    public static string ComputeRank(string result, string difficulty, float survivalTime, int kills)
    {
        bool hard = difficulty == DifficultyHard;
        bool victory = result == ResultVictory;
        float kpm = ComputeKpm(kills, survivalTime);

        if (victory && hard && kills >= 3500 && kpm >= 230.0f)
            return RankS;

        bool rankATimeGate = (victory && !hard) || (hard && survivalTime > 720.0f);
        if (rankATimeGate && kills >= 2000 && kpm >= 130.0f)
            return RankA;

        if (survivalTime > 480.0f && kills >= 800)
            return RankB;

        if (survivalTime > 240.0f && kills >= 300)
            return RankC;

        return RankD;
    }

    /// <summary>
    /// Pathological score (docs/record.md §4.2):
    /// (survival × 10 + kill score + level × 100) × difficulty multiplier
    /// × affliction multiplier + clear bonus. Endless afflictions stack up to ×2.75.
    /// </summary>
    public static int ComputeScore(string result, string difficulty, float survivalTime, int killScore, int level, float afflictionMultiplier = 1.0f)
    {
        float multiplier = difficulty == DifficultyHard ? HardDifficultyMultiplier : 1.0f;
        float total = (survivalTime * SurvivalScorePerSecond + Mathf.Max(0, killScore) + level * LevelScoreBonus)
                      * multiplier * Mathf.Max(1.0f, afflictionMultiplier);
        if (result == ResultVictory)
            total += ClearBonus;
        return Mathf.RoundToInt(total);
    }

    public static RunRecordManager Instance { get; private set; } = null;

    public static Array<Dictionary> Records { get; private set; } = new Array<Dictionary>();

    /// <summary>
    /// Canonical victory criteria (docs/record.md): survive to 15:00 AND
    /// successfully neutralize the terminal primary pathogen boss.
    /// </summary>
    public static bool IsVictoryCriteriaMet(float survivalTime, bool bossNeutralized)
    {
        return bossNeutralized && survivalTime >= StandardClearSeconds - 0.01f;
    }

    public RunRecordManager()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        Instance = this;
        LoadFromDisk();
    }

    /// <summary>
    /// Append a finished run to the front of the history and persist it.
    /// Victory records are only accepted when the canonical criteria are met;
    /// an unearned victory claim is downgraded to a defeat with a warning.
    /// Kills, KPM, clinical grade and pathological score are derived here so
    /// every stored record stays internally consistent.
    /// Returns the stored record.
    /// </summary>
    public static Dictionary RecordRun(
        string result,
        string classId,
        string mapId,
        float survivalTime,
        int level,
        int digested,
        int pointsSpent,
        string[] activeSkillIds,
        bool bossNeutralized = false,
        string cause = "",
        int kills = 0,
        int killScore = 0,
        string difficulty = DifficultyNormal,
        bool endless = false,
        float afflictionMultiplier = 1.0f,
        string[]? afflictions = null)
    {
        var skills = new Array<string>();
        if (activeSkillIds != null)
        {
            foreach (string id in activeSkillIds)
            {
                if (!string.IsNullOrEmpty(id))
                    skills.Add(id);
            }
        }

        var afflictionList = new Array<string>();
        if (afflictions != null)
        {
            foreach (string id in afflictions)
            {
                if (!string.IsNullOrEmpty(id))
                    afflictionList.Add(id);
            }
        }

        if (afflictionMultiplier < 1.0f)
            afflictionMultiplier = 1.0f;

        bool survivedFullTime = survivalTime >= StandardClearSeconds - 0.01f;
        // Endless overdrive has no clear settlement: the standard criteria are
        // recorded as raw facts but can never be "met" into a victory.
        bool criteriaMet = !endless && IsVictoryCriteriaMet(survivalTime, bossNeutralized);

        if (result == ResultVictory && !criteriaMet)
        {
            GD.PushWarning($"[RunRecord] Rejected invalid victory on '{mapId}' " +
                           $"(survival={survivalTime:F1}s, boss_neutralized={bossNeutralized}); recording as defeat.");
            result = ResultDefeat;
        }

        if (string.IsNullOrEmpty(cause))
        {
            cause = result == ResultVictory ? CauseSpecificNeutralization : CauseSystemFailure;
        }

        if (string.IsNullOrEmpty(difficulty))
            difficulty = DifficultyNormal;

        float kpm = ComputeKpm(kills, survivalTime);
        string rank = ComputeRank(result, difficulty, survivalTime, kills);
        int score = ComputeScore(result, difficulty, survivalTime, killScore, level, afflictionMultiplier);

        var record = new Dictionary
        {
            { "result", result },
            { "cause", cause },
            { "class_id", classId },
            { "map_id", mapId },
            { "difficulty", difficulty },
            { "endless", endless },
            { "afflictions", afflictionList },
            { "affliction_multiplier", afflictionMultiplier },
            { "survival_time", survivalTime },
            { "level", level },
            { "kills", kills },
            { "digested", digested },
            { "kpm", kpm },
            { "kill_score", killScore },
            { "score", score },
            { "rank", rank },
            { "points_spent", pointsSpent },
            { "active_skills", skills },
            { "victory_criteria", new Dictionary
                {
                    { "survived_full_time", survivedFullTime },
                    { "boss_neutralized", bossNeutralized },
                    { "met", criteriaMet }
                }
            },
            { "timestamp", Time.GetUnixTimeFromSystem() }
        };

        Records.Insert(0, record);
        while (Records.Count > MaxRecords)
        {
            Records.RemoveAt(Records.Count - 1);
        }

        SaveToDisk();
        return record;
    }

    public static Array<Dictionary> GetAll()
    {
        var copy = new Array<Dictionary>();
        foreach (var rec in Records)
        {
            copy.Add(rec);
        }
        return copy;
    }

    public static int GetRunCount() => Records.Count;

    public static int GetVictoryCount()
    {
        int count = 0;
        foreach (var rec in Records)
        {
            if (rec.GetValueOrDefault("result", "").AsString() == ResultVictory)
                count++;
        }
        return count;
    }

    /// <summary>Longest survival time on record (0 when empty).</summary>
    public static float GetBestSurvivalTime()
    {
        float best = 0.0f;
        foreach (var rec in Records)
        {
            best = Mathf.Max(best, rec.GetValueOrDefault("survival_time", 0.0f).AsSingle());
        }
        return best;
    }

    /// <summary>Shortest victory time, or -1 when no run has been cleared.</summary>
    public static float GetFastestVictory()
    {
        float best = -1.0f;
        foreach (var rec in Records)
        {
            if (rec.GetValueOrDefault("result", "").AsString() != ResultVictory)
                continue;
            float t = rec.GetValueOrDefault("survival_time", 0.0f).AsSingle();
            if (best < 0.0f || t < best)
                best = t;
        }
        return best;
    }

    public static void ClearRecords()
    {
        Records.Clear();
        SaveToDisk();
    }

    public static string FormatTime(float seconds)
    {
        float clamped = Mathf.Max(0.0f, seconds);
        int minutes = (int)(clamped / 60.0f);
        int secs = (int)(clamped % 60.0f);
        return $"{minutes:D2}:{secs:D2}";
    }

    public static string FormatTimestamp(double unixTime)
    {
        var dt = Time.GetDatetimeDictFromUnixTime((long)unixTime);
        int year = dt.GetValueOrDefault("year", 0).AsInt32();
        int month = dt.GetValueOrDefault("month", 1).AsInt32();
        int day = dt.GetValueOrDefault("day", 1).AsInt32();
        int hour = dt.GetValueOrDefault("hour", 0).AsInt32();
        int minute = dt.GetValueOrDefault("minute", 0).AsInt32();
        return $"{year:D4}-{month:D2}-{day:D2} {hour:D2}:{minute:D2}";
    }

    public static void SaveToDisk()
    {
        var payload = new Dictionary
        {
            { "records", Records }
        };

        var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (file == null && SavePath.StartsWith("user://"))
        {
            SavePath = "res://.user_data/run_records.json";
            DirAccess.MakeDirRecursiveAbsolute("res://.user_data");
            file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        }

        if (file != null)
        {
            using (file)
            {
                file.StoreString(Json.Stringify(payload, "\t"));
            }
        }
    }

    public static void LoadFromDisk()
    {
        Records = new Array<Dictionary>();

        if (!FileAccess.FileExists(SavePath))
            return;

        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file == null)
            return;

        var json = new Json();
        if (json.Parse(file.GetAsText()) != Error.Ok || json.Data.VariantType != Variant.Type.Dictionary)
            return;

        var data = json.Data.AsGodotDictionary();
        if (!data.ContainsKey("records") || data["records"].VariantType != Variant.Type.Array)
            return;

        foreach (var entry in data["records"].AsGodotArray())
        {
            if (entry.VariantType == Variant.Type.Dictionary)
                Records.Add(entry.AsGodotDictionary());
        }
    }
}
