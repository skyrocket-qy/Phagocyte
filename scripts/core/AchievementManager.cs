using Godot;
using Godot.Collections;
using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Manages game achievements, milestone tracking, persistent unlocks, and class rewards.
/// </summary>
public partial class AchievementManager : Node
{
    [Signal]
    public delegate void AchievementUnlockedEventHandler(string achId, Godot.Collections.Dictionary achData);

    private static readonly JsonStore.SavePathSlot _savePath = new("achievements.json");
    public static string SavePath
    {
        get => _savePath.Value;
        set => _savePath.Value = value;
    }

    public static AchievementManager? Instance { get; private set; } = null;
    private static System.Collections.Generic.List<Callable> _listeners = new System.Collections.Generic.List<Callable>();

    // Achievement definitions are data-owned (assets/data/achievements.json).
    // Loaded once and cached; in-session unlocks mutate the cached entries.
    private static Godot.Collections.Dictionary? _achievements;
    public static Godot.Collections.Dictionary Achievements
    {
        get
        {
            _achievements ??= CatalogBuilders.BuildAchievements();
            DataValidator.EnsureValidated();
            return _achievements;
        }
    }

    public static Godot.Collections.Dictionary UnlockedIds = new Godot.Collections.Dictionary();
    public static Godot.Collections.Dictionary ProgressData = new Godot.Collections.Dictionary
    {
        { "digested", 0.0f },
        { "level", 1.0f },
        { "survival_time", 0.0f },
        { "radius_ratio", 1.0f },
        { "active_skills", 1.0f },
        { "first_evolution", 0.0f },
        { "prion_cleared", 0.0f }
    };

    public AchievementManager()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        Instance = this;
        SteamBridge.Initialize();
        LoadFromDisk();
        SyncWithSteam();
    }

    public static void AddUnlockListener(Callable callback)
    {
        if (!_listeners.Contains(callback))
        {
            _listeners.Add(callback);
        }
    }

    public static void RemoveUnlockListener(Callable callback)
    {
        _listeners.Remove(callback);
    }

    /// <summary>
    /// Check if an achievement is unlocked
    /// </summary>
    public static bool IsUnlocked(string achId)
    {
        return UnlockedIds.ContainsKey(achId) && UnlockedIds[achId].AsBool() == true;
    }

    /// <summary>
    /// Manually or systematically unlock an achievement
    /// </summary>
    public static bool Unlock(string achId)
    {
        if (!Achievements.ContainsKey(achId))
        {
            return false;
        }
        if (IsUnlocked(achId))
        {
            return false;
        }

        UnlockedIds[achId] = true;
        var data = Achievements[achId].AsGodotDictionary();

        // Unlock rewarding immune cell if applicable
        string rewardC = data.GetValueOrDefault("reward_cell", "").AsString();
        if (!string.IsNullOrEmpty(rewardC))
        {
            GameEvents.RaiseClassUnlock(rewardC);
        }

        // Apply organ-map unlock chain rewards (maps, Hard modes, talent points)
        ApplyMapRewards(data);
        SyncMapUnlocks();

        SaveToDisk();

        // Steam broadcast (no-op unless USE_STEAMWORKS is compiled in)
        SteamBridge.PushUnlock(achId);

        var info = GetAchievementInfo(achId);
        if (Instance != null && GodotObject.IsInstanceValid(Instance))
        {
            Instance.EmitSignal(SignalName.AchievementUnlocked, achId, info);
        }

        foreach (var cb in _listeners)
        {
            if (cb.Target is null || GodotObject.IsInstanceValid(cb.Target as GodotObject))
            {
                cb.Call(achId, info);
            }
        }

        return true;
    }

    /// <summary>
    /// Record runtime in-game events and evaluate completion conditions.
    /// Thresholds are read from the achievement catalog so tuning a target_value
    /// never desynchronizes the tracker.
    /// </summary>
    public static void RecordEvent(string eventName, Variant value = default)
    {
        switch (eventName)
        {
            case "pathogen_digested":
                float count = value.Obj != null ? value.AsSingle() : (ProgressData.GetValueOrDefault("digested", 0.0f).AsSingle() + 1.0f);
                ProgressData["digested"] = Mathf.Max(ProgressData.GetValueOrDefault("digested", 0.0f).AsSingle(), count);
                EvaluateThreshold("ach_first_digestion");
                EvaluateThreshold("ach_engulf_20");
                EvaluateThreshold("ach_devour_50");
                break;

            case "level_up":
                float lvl = value.Obj != null ? value.AsSingle() : 1.0f;
                ProgressData["level"] = Mathf.Max(ProgressData.GetValueOrDefault("level", 1.0f).AsSingle(), lvl);
                EvaluateThreshold("ach_reach_level_5");
                break;

            case "survival_time":
                float st = value.Obj != null ? value.AsSingle() : 0.0f;
                ProgressData["survival_time"] = Mathf.Max(ProgressData.GetValueOrDefault("survival_time", 0.0f).AsSingle(), st);
                EvaluateThreshold("ach_survive_180s");
                break;

            case "radius_ratio":
                float rr = value.Obj != null ? value.AsSingle() : 1.0f;
                ProgressData["radius_ratio"] = Mathf.Max(ProgressData.GetValueOrDefault("radius_ratio", 1.0f).AsSingle(), rr);
                EvaluateThreshold("ach_giant_volume");
                break;

            case "active_skills_count":
                float cnt = value.Obj != null ? value.AsSingle() : 1.0f;
                ProgressData["active_skills"] = Mathf.Max(ProgressData.GetValueOrDefault("active_skills", 1.0f).AsSingle(), cnt);
                EvaluateThreshold("ach_full_arsenal");
                break;

            case "first_evolution":
                ProgressData["first_evolution"] = 1.0f;
                Unlock("ach_first_evolution");
                break;

            case "pathogen_killed":
                // PrPsc amyloid crystals (regular prion aggregate or the map-5 terminal boss)
                string enemyId = value.Obj != null ? value.AsString() : "";
                if (enemyId == "prion" || enemyId == "prpsc_amyloid_aggregate")
                {
                    ProgressData["prion_cleared"] = 1.0f;
                    Unlock("ach_prion_cleared");
                }
                break;
        }
    }

    /// <summary>
    /// Unlocks an achievement once its tracked stat reaches the catalog target.
    /// </summary>
    private static void EvaluateThreshold(string achId)
    {
        if (!Achievements.ContainsKey(achId))
            return;

        var raw = Achievements[achId].AsGodotDictionary();
        string statKey = raw.GetValueOrDefault("stat_key", "").AsString();
        float target = raw.GetValueOrDefault("target_value", 1.0f).AsSingle();
        float current = ProgressData.GetValueOrDefault(statKey, 0.0f).AsSingle();

        if (current >= target)
            Unlock(achId);
    }

    /// <summary>
    /// Reports a cleared organ map run and unlocks the matching difficulty
    /// achievement from the catalog (docs/achievement.md unlock chain).
    /// </summary>
    public static void RecordMapClear(string mapId, bool hard = false)
    {
        if (string.IsNullOrEmpty(mapId))
            return;

        foreach (string achId in Achievements.Keys)
        {
            var ach = Achievements[achId].AsGodotDictionary();
            if (ach.GetValueOrDefault("map_id", "").AsString() != mapId)
                continue;

            bool isHardAchievement = ach.GetValueOrDefault("difficulty", "normal").AsString() == "hard";
            if (isHardAchievement != hard)
                continue;

            string statKey = ach.GetValueOrDefault("stat_key", achId).AsString();
            ProgressData[statKey] = 1.0f;
            Unlock(achId);
        }
    }

    /// <summary>
    /// True once the terminal Hard clear has unlocked the Endless Cytokine Storm mode.
    /// </summary>
    public static bool IsEndlessUnlocked()
    {
        return IsUnlocked("ach_wound_hard_clear");
    }

    private static void ApplyMapRewards(Godot.Collections.Dictionary achievementData)
    {
        string nextMap = achievementData.GetValueOrDefault("unlock_map", "").AsString();
        if (!string.IsNullOrEmpty(nextMap))
            GameEvents.RaiseMapUnlock(nextMap);

        string hardMap = achievementData.GetValueOrDefault("unlock_hard_map", "").AsString();
        if (!string.IsNullOrEmpty(hardMap))
            GameEvents.RaiseMapHardUnlock(hardMap);

        int talentPoints = achievementData.GetValueOrDefault("talent_points", 0).AsInt32();
        if (talentPoints > 0)
            GameEvents.RaiseBonusPoints(talentPoints);
    }

    /// <summary>
    /// Rebuilds GameManager.MapData lock state from the persisted achievement chain.
    /// Baseline: acute_wound Normal only; everything else must be earned.
    /// </summary>
    private static void SyncMapUnlocks()
    {
        GameEvents.RaiseMapsReset();

        foreach (string achId in Achievements.Keys)
        {
            if (!IsUnlocked(achId))
                continue;

            var raw = Achievements[achId].AsGodotDictionary();
            string nextMap = raw.GetValueOrDefault("unlock_map", "").AsString();
            if (!string.IsNullOrEmpty(nextMap))
                GameEvents.RaiseMapUnlock(nextMap);

            string hardMap = raw.GetValueOrDefault("unlock_hard_map", "").AsString();
            if (!string.IsNullOrEmpty(hardMap))
                GameEvents.RaiseMapHardUnlock(hardMap);
        }
    }

    /// <summary>
    /// Two-way Steamworks sync (docs/achievement.md §3):
    ///  1. Offline catch-up: batch-push every locally unlocked achievement.
    ///  2. Merge remote unlocks missing on this machine (fresh profile / cloud restore).
    /// Returns the number of remotely merged achievements. No-op without the SDK.
    /// </summary>
    public static int SyncWithSteam()
    {
        if (!SteamBridge.IsAvailable)
            return 0;

        var localIds = new System.Collections.Generic.List<string>();
        foreach (string achId in Achievements.Keys)
        {
            if (IsUnlocked(achId))
                localIds.Add(achId);
        }
        SteamBridge.PushUnlocks(localIds);

        int merged = 0;
        foreach (string achId in SteamBridge.PullUnlocked(GetAllAchievementIds()))
        {
            if (Achievements.ContainsKey(achId) && !IsUnlocked(achId) && Unlock(achId))
                merged++;
        }
        return merged;
    }

    private static System.Collections.Generic.List<string> GetAllAchievementIds()
    {
        var ids = new System.Collections.Generic.List<string>();
        foreach (string achId in Achievements.Keys)
            ids.Add(achId);
        return ids;
    }

    /// <summary>
    /// Get localized information for a single achievement
    /// </summary>
    public static Godot.Collections.Dictionary GetAchievementInfo(string achId)
    {
        if (!Achievements.ContainsKey(achId))
        {
            return new Godot.Collections.Dictionary();
        }

        var raw = Achievements[achId].AsGodotDictionary();
        bool unlocked = IsUnlocked(achId);
        string statKey = raw.GetValueOrDefault("stat_key", "").AsString();
        float currentVal = ProgressData.GetValueOrDefault(statKey, 0.0f).AsSingle();
        float targetVal = raw.GetValueOrDefault("target_value", 1.0f).AsSingle();

        string title = TranslationServer.Translate(raw["title_key"].AsString());
        string desc = TranslationServer.Translate(raw["desc_key"].AsString());
        string rewardText = raw.GetValueOrDefault("reward_key", "").AsString() != "" ? TranslationServer.Translate(raw["reward_key"].AsString()) : "";

        return new Godot.Collections.Dictionary
        {
            { "id", achId },
            { "title", title },
            { "desc", desc },
            { "reward", rewardText },
            { "reward_cell", raw.GetValueOrDefault("reward_cell", "") },
            { "icon", raw.GetValueOrDefault("icon", "🏆") },
            { "image_path", raw.GetValueOrDefault("image_path", $"res://assets/sprites/achievements/{achId}.png") },
            { "steam_api_name", SteamBridge.GetSteamApiName(achId) },
            { "unlocked", unlocked },
            { "current_value", currentVal },
            { "target_value", targetVal },
            { "progress_ratio", Mathf.Clamp(targetVal > 0.0f ? currentVal / targetVal : 1.0f, 0.0f, 1.0f) }
        };
    }

    /// <summary>
    /// Get all achievements formatted for UI display
    /// </summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetAllAchievements()
    {
        var list = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (string key in Achievements.Keys)
        {
            list.Add(GetAchievementInfo(key));
        }
        return list;
    }

    /// <summary>
    /// Helper: Get unlock condition text for a locked immune cell
    /// </summary>
    public static string GetCellUnlockRequirementText(string classId)
    {
        foreach (string achId in Achievements.Keys)
        {
            var ach = Achievements[achId].AsGodotDictionary();
            if (ach.GetValueOrDefault("reward_cell", "").AsString() == classId)
            {
                string title = TranslationServer.Translate(ach["title_key"].AsString());
                string desc = TranslationServer.Translate(ach["desc_key"].AsString());
                return string.Format("{0}🏆 {1} ({2})", TranslationServer.Translate("LABEL_UNLOCK_REQ"), title, desc);
            }
        }
        return TranslationServer.Translate("STATUS_LOCKED");
    }

    /// <summary>
    /// Helper: Get unlock condition text for a locked organ map
    /// (the prerequisite achievement that unlocks its Normal variant).
    /// </summary>
    public static string GetMapUnlockRequirementText(string mapId)
    {
        foreach (string achId in Achievements.Keys)
        {
            var ach = Achievements[achId].AsGodotDictionary();
            if (ach.GetValueOrDefault("unlock_map", "").AsString() == mapId)
            {
                string title = TranslationServer.Translate(ach["title_key"].AsString());
                string desc = TranslationServer.Translate(ach["desc_key"].AsString());
                return string.Format("{0}🏆 {1} ({2})", TranslationServer.Translate("LABEL_UNLOCK_REQ"), title, desc);
            }
        }

        if (GameManager.IsMapHardUnlocked(mapId))
            return TranslationServer.Translate("STATUS_UNLOCKED");

        return TranslationServer.Translate("STATUS_LOCKED");
    }

    /// <summary>
    /// Save achievement state to disk
    /// </summary>
    public static void SaveToDisk()
    {
        var payload = new Godot.Collections.Dictionary
        {
            { "unlocked_ids", UnlockedIds },
            { "progress_data", ProgressData }
        };

        JsonStore.Write(SavePath, payload);
    }

    /// <summary>
    /// Load achievement state from disk
    /// </summary>
    public static void LoadFromDisk()
    {
        var data = JsonStore.Read(SavePath);
        if (data == null)
        {
            SyncUnlockedClasses();
            SyncMapUnlocks();
            return;
        }

        if (data.ContainsKey("unlocked_ids") && data["unlocked_ids"].VariantType == Variant.Type.Dictionary)
        {
            UnlockedIds = data["unlocked_ids"].AsGodotDictionary();
        }
        if (data.ContainsKey("progress_data") && data["progress_data"].VariantType == Variant.Type.Dictionary)
        {
            var pd = data["progress_data"].AsGodotDictionary();
            foreach (var k in pd.Keys)
            {
                ProgressData[k] = pd[k].AsSingle();
            }
        }

        // Legacy migration: the retired "Metabolic Storm" burst achievement
        // is superseded by the 50-devoured survivor goal for the same reward.
        if (UnlockedIds.ContainsKey("ach_trigger_burst") && UnlockedIds["ach_trigger_burst"].AsBool()
            && !UnlockedIds.ContainsKey("ach_devour_50"))
        {
            UnlockedIds["ach_devour_50"] = true;
        }

        SyncUnlockedClasses();
        SyncMapUnlocks();
    }

    /// <summary>
    /// Synchronize unlocked classes in GameManager based on unlocked achievements
    /// </summary>
    private static void SyncUnlockedClasses()
    {
        GameEvents.RaiseClassUnlock("macrophage");

        foreach (string achId in Achievements.Keys)
        {
            var raw = Achievements[achId].AsGodotDictionary();
            string rewardCell = raw.GetValueOrDefault("reward_cell", "").AsString();
            if (!string.IsNullOrEmpty(rewardCell))
            {
                if (IsUnlocked(achId))
                {
                    GameEvents.RaiseClassUnlock(rewardCell);
                }
                else
                {
                    GameEvents.RaiseClassLock(rewardCell);
                }
            }
        }
    }

    /// <summary>
    /// Reset all progress and locked state (used for testing or clean restarts)
    /// </summary>
    public static void ResetAll()
    {
        UnlockedIds.Clear();
        ProgressData = new Godot.Collections.Dictionary
        {
            { "digested", 0.0f },
            { "level", 1.0f },
            { "survival_time", 0.0f },
            { "radius_ratio", 1.0f },
            { "active_skills", 1.0f },
            { "first_evolution", 0.0f },
            { "prion_cleared", 0.0f }
        };
        SyncUnlockedClasses();
        SyncMapUnlocks();

        JsonStore.Delete(SavePath);
    }
}
