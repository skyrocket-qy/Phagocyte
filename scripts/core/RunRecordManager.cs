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
        string cause = "")
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

        bool survivedFullTime = survivalTime >= StandardClearSeconds - 0.01f;
        bool criteriaMet = IsVictoryCriteriaMet(survivalTime, bossNeutralized);

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

        var record = new Dictionary
        {
            { "result", result },
            { "cause", cause },
            { "class_id", classId },
            { "map_id", mapId },
            { "survival_time", survivalTime },
            { "level", level },
            { "digested", digested },
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
