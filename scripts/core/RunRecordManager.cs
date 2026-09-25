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
    private static readonly JsonStore.SavePathSlot _savePath = new("run_records.json");
    public static string SavePath
    {
        get => _savePath.Value;
        set => _savePath.Value = value;
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

    // Endless overdrive grades (docs/endgame.md §5.1)
    public const string RankSSS = "SSS"; // 超載神話
    public const string RankEX = "EX";   // 破格存在

    /// <summary>Endless grade thresholds (docs/endgame.md §5.1).</summary>
    public const float SSSSurvivalSeconds = 1800.0f; // 30:00 triple-siege survival
    public const int SSSKills = 5000;
    public const float EXSurvivalSeconds = 2400.0f;  // 40:00 terminal compensation
    public const int EXKills = 8000;
    public const float EXAfflictionMultiplier = 2.5f; // ≥ +150% overload

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
    /// Endless overdrive (docs/endgame.md §5.1) opens the two apex grades on top:
    /// EX = 40:00 + 8,000 kills + ≥×2.5 affliction overload;
    /// SSS = 30:00 + 5,000 kills with at least one affliction.
    /// </summary>
    public static string ComputeRank(
        string result,
        string difficulty,
        float survivalTime,
        int kills,
        bool endless = false,
        float afflictionMultiplier = 1.0f)
    {
        if (endless)
        {
            if (survivalTime >= EXSurvivalSeconds && kills >= EXKills && afflictionMultiplier >= EXAfflictionMultiplier)
                return RankEX;

            if (survivalTime >= SSSSurvivalSeconds && kills >= SSSKills && afflictionMultiplier > 1.0f)
                return RankSSS;
        }

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

    public static RunRecordManager? Instance { get; private set; } = null;

    public static Array<Dictionary> Records { get; private set; } = new Array<Dictionary>();

    /// <summary>
    /// Canonical victory criteria (docs/record.md): survive to 15:00 AND
    /// successfully neutralize the terminal primary pathogen boss.
    /// </summary>
    public static bool IsVictoryCriteriaMet(float survivalTime, bool bossNeutralized)
    {
        return bossNeutralized && survivalTime >= StandardClearSeconds - 0.01f;
    }

    /// <summary>
    /// Single settlement decision shared by the run lifecycle (Main.EndRun) and
    /// the record store (RecordRun): endless overdrive runs can only settle as
    /// defeat, and standard victories must meet the canonical criteria.
    /// </summary>
    public static bool CanSettleVictory(float survivalTime, bool bossNeutralized, bool endless)
    {
        return !endless && IsVictoryCriteriaMet(survivalTime, bossNeutralized);
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
        bool criteriaMet = CanSettleVictory(survivalTime, bossNeutralized, endless);

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
        string rank = ComputeRank(result, difficulty, survivalTime, kills, endless, afflictionMultiplier);
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
            { "kpm", kpm },
            { "kill_score", killScore },
            { "score", score },
            { "rank", rank },
            { "points_spent", pointsSpent },
            { "active_skills", skills },
            { "locked", false },
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
        TrimToCapacity();

        SaveToDisk();
        return record;
    }

    /// <summary>Archived (pinned) charts survive automatic FIFO eviction.</summary>
    public static bool IsLocked(Dictionary rec) => rec.GetValueOrDefault("locked", false).AsBool();

    public static void SetLocked(Dictionary rec, bool locked)
    {
        rec["locked"] = locked;
        SaveToDisk();
    }

    /// <summary>
    /// FIFO cap: evict the oldest unlocked chart first. Locked charts are
    /// immune; when every chart is locked the overflow is kept as-is.
    /// </summary>
    public static void TrimToCapacity()
    {
        while (Records.Count > MaxRecords)
        {
            int victim = -1;
            for (int i = Records.Count - 1; i >= 0; i--)
            {
                if (!IsLocked(Records[i]))
                {
                    victim = i;
                    break;
                }
            }
            if (victim < 0)
                break;
            Records.RemoveAt(victim);
        }
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

    /// <summary>Longest endless overdrive survival on record (0 when none) — local leaderboard seed.</summary>
    public static float GetBestEndlessSurvivalTime()
    {
        float best = 0.0f;
        foreach (var rec in Records)
        {
            if (!rec.GetValueOrDefault("endless", false).AsBool())
                continue;
            best = Mathf.Max(best, rec.GetValueOrDefault("survival_time", 0.0f).AsSingle());
        }
        return best;
    }

    /// <summary>Highest pathological score among endless runs (0 when none).</summary>
    public static int GetBestEndlessScore()
    {
        int best = 0;
        foreach (var rec in Records)
        {
            if (!rec.GetValueOrDefault("endless", false).AsBool())
                continue;
            best = Mathf.Max(best, rec.GetValueOrDefault("score", 0).AsInt32());
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

        SavePath = JsonStore.Write(SavePath, payload);
    }

    public static void LoadFromDisk()
    {
        Records = new Array<Dictionary>();

        var data = JsonStore.Read(SavePath);
        if (data == null)
            return;

        if (!data.ContainsKey("records") || data["records"].VariantType != Variant.Type.Array)
            return;

        foreach (var entry in data["records"].AsGodotArray())
        {
            if (entry.VariantType == Variant.Type.Dictionary)
                Records.Add(entry.AsGodotDictionary());
        }
    }
}
