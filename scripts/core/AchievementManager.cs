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

    public static readonly Godot.Collections.Dictionary Achievements = new Godot.Collections.Dictionary
    {
        { "ach_first_digestion", new Godot.Collections.Dictionary {
            { "id", "ach_first_digestion" },
            { "title_key", "ACH_FIRST_DIGESTION_TITLE" },
            { "desc_key", "ACH_FIRST_DIGESTION_DESC" },
            { "reward_key", "" },
            { "reward_cell", "" },
            { "icon", "🦠" },
            { "target_value", 1.0f },
            { "stat_key", "digested" }
        }},
        { "ach_engulf_20", new Godot.Collections.Dictionary {
            { "id", "ach_engulf_20" },
            { "title_key", "ACH_ENGULF_20_TITLE" },
            { "desc_key", "ACH_ENGULF_20_DESC" },
            { "reward_key", "ACH_ENGULF_20_REWARD" },
            { "reward_cell", "ctl" },
            { "icon", "⚡" },
            { "target_value", 200.0f },
            { "stat_key", "digested" }
        }},
        { "ach_devour_50", new Godot.Collections.Dictionary {
            { "id", "ach_devour_50" },
            { "title_key", "ACH_DEVOUR_50_TITLE" },
            { "desc_key", "ACH_DEVOUR_50_DESC" },
            { "reward_key", "ACH_DEVOUR_50_REWARD" },
            { "reward_cell", "neutrophil" },
            { "icon", "🌪️" },
            { "target_value", 500.0f },
            { "stat_key", "digested" }
        }},
        { "ach_reach_level_5", new Godot.Collections.Dictionary {
            { "id", "ach_reach_level_5" },
            { "title_key", "ACH_REACH_LEVEL_5_TITLE" },
            { "desc_key", "ACH_REACH_LEVEL_5_DESC" },
            { "reward_key", "ACH_REACH_LEVEL_5_REWARD" },
            { "reward_cell", "b_cell" },
            { "icon", "🏹" },
            { "target_value", 15.0f },
            { "stat_key", "level" }
        }},
        { "ach_survive_180s", new Godot.Collections.Dictionary {
            { "id", "ach_survive_180s" },
            { "title_key", "ACH_SURVIVE_180S_TITLE" },
            { "desc_key", "ACH_SURVIVE_180S_DESC" },
            { "reward_key", "ACH_SURVIVE_180S_REWARD" },
            { "reward_cell", "dendritic" },
            { "icon", "📍" },
            { "target_value", 480.0f },
            { "stat_key", "survival_time" }
        }},
        { "ach_giant_volume", new Godot.Collections.Dictionary {
            { "id", "ach_giant_volume" },
            { "title_key", "ACH_GIANT_VOLUME_TITLE" },
            { "desc_key", "ACH_GIANT_VOLUME_DESC" },
            { "reward_key", "" },
            { "reward_cell", "" },
            { "icon", "🌟" },
            { "target_value", 2.0f },
            { "stat_key", "radius_ratio" }
        }},
        { "ach_full_arsenal", new Godot.Collections.Dictionary {
            { "id", "ach_full_arsenal" },
            { "title_key", "ACH_FULL_ARSENAL_TITLE" },
            { "desc_key", "ACH_FULL_ARSENAL_DESC" },
            { "reward_key", "" },
            { "reward_cell", "" },
            { "icon", "🛡️" },
            { "target_value", 5.0f },
            { "stat_key", "active_skills" }
        }},
        { "ach_first_evolution", new Godot.Collections.Dictionary {
            { "id", "ach_first_evolution" },
            { "title_key", "ACH_FIRST_EVOLUTION_TITLE" },
            { "desc_key", "ACH_FIRST_EVOLUTION_DESC" },
            { "reward_key", "" },
            { "reward_cell", "" },
            { "icon", "🧬" },
            { "target_value", 1.0f },
            { "stat_key", "first_evolution" }
        }},
        { "ach_prion_cleared", new Godot.Collections.Dictionary {
            { "id", "ach_prion_cleared" },
            { "title_key", "ACH_PRION_CLEARED_TITLE" },
            { "desc_key", "ACH_PRION_CLEARED_DESC" },
            { "reward_key", "" },
            { "reward_cell", "" },
            { "icon", "💎" },
            { "target_value", 1.0f },
            { "stat_key", "prion_cleared" }
        }},

        // --- Organ map clear chain (docs/achievement.md §2 / docs/map.md §2) ---
        // Each map first-clear (Normal / Hard) awards 1 microtube talent point
        // (docs/passivetree.md §5.2: 10 points from the 10 map clears). The final
        // blood-brain-barrier clear additionally carries its +1 milestone bonus.
        { "ach_wound_clear", new Godot.Collections.Dictionary {
            { "id", "ach_wound_clear" },
            { "title_key", "ACH_WOUND_CLEAR_TITLE" },
            { "desc_key", "ACH_WOUND_CLEAR_DESC" },
            { "reward_key", "ACH_WOUND_CLEAR_REWARD" },
            { "reward_cell", "" },
            { "icon", "🩹" },
            { "target_value", 1.0f },
            { "stat_key", "map_clear_acute_wound" },
            { "map_id", "acute_wound" },
            { "difficulty", "normal" },
            { "unlock_map", "alveolar_space" },
            { "unlock_hard_map", "acute_wound" },
            { "talent_points", 1 }
        }},
        { "ach_alveolar_clear", new Godot.Collections.Dictionary {
            { "id", "ach_alveolar_clear" },
            { "title_key", "ACH_ALVEOLAR_CLEAR_TITLE" },
            { "desc_key", "ACH_ALVEOLAR_CLEAR_DESC" },
            { "reward_key", "ACH_ALVEOLAR_CLEAR_REWARD" },
            { "reward_cell", "" },
            { "icon", "🫁" },
            { "target_value", 1.0f },
            { "stat_key", "map_clear_alveolar_space" },
            { "map_id", "alveolar_space" },
            { "difficulty", "normal" },
            { "unlock_map", "hepatic_sinusoid" },
            { "unlock_hard_map", "alveolar_space" },
            { "talent_points", 1 }
        }},
        { "ach_hepatic_clear", new Godot.Collections.Dictionary {
            { "id", "ach_hepatic_clear" },
            { "title_key", "ACH_HEPATIC_CLEAR_TITLE" },
            { "desc_key", "ACH_HEPATIC_CLEAR_DESC" },
            { "reward_key", "ACH_HEPATIC_CLEAR_REWARD" },
            { "reward_cell", "" },
            { "icon", "🫀" },
            { "target_value", 1.0f },
            { "stat_key", "map_clear_hepatic_sinusoid" },
            { "map_id", "hepatic_sinusoid" },
            { "difficulty", "normal" },
            { "unlock_map", "gastric_lumen" },
            { "unlock_hard_map", "hepatic_sinusoid" },
            { "talent_points", 1 }
        }},
        { "ach_gastric_clear", new Godot.Collections.Dictionary {
            { "id", "ach_gastric_clear" },
            { "title_key", "ACH_GASTRIC_CLEAR_TITLE" },
            { "desc_key", "ACH_GASTRIC_CLEAR_DESC" },
            { "reward_key", "ACH_GASTRIC_CLEAR_REWARD" },
            { "reward_cell", "" },
            { "icon", "🌋" },
            { "target_value", 1.0f },
            { "stat_key", "map_clear_gastric_lumen" },
            { "map_id", "gastric_lumen" },
            { "difficulty", "normal" },
            { "unlock_map", "blood_brain_barrier" },
            { "unlock_hard_map", "gastric_lumen" },
            { "talent_points", 1 }
        }},
        { "ach_bbb_clear", new Godot.Collections.Dictionary {
            { "id", "ach_bbb_clear" },
            { "title_key", "ACH_BBB_CLEAR_TITLE" },
            { "desc_key", "ACH_BBB_CLEAR_DESC" },
            { "reward_key", "ACH_BBB_CLEAR_REWARD" },
            { "reward_cell", "" },
            { "icon", "🧠" },
            { "target_value", 1.0f },
            { "stat_key", "map_clear_blood_brain_barrier" },
            { "map_id", "blood_brain_barrier" },
            { "difficulty", "normal" },
            { "unlock_hard_map", "blood_brain_barrier" },
            { "talent_points", 2 }
        }},
        { "ach_wound_hard_clear", new Godot.Collections.Dictionary {
            { "id", "ach_wound_hard_clear" },
            { "title_key", "ACH_WOUND_HARD_CLEAR_TITLE" },
            { "desc_key", "ACH_WOUND_HARD_CLEAR_DESC" },
            { "reward_key", "ACH_WOUND_HARD_CLEAR_REWARD" },
            { "reward_cell", "" },
            { "icon", "☣️" },
            { "target_value", 1.0f },
            { "stat_key", "map_clear_acute_wound_hard" },
            { "map_id", "acute_wound" },
            { "difficulty", "hard" },
            { "unlock_endless", true },
            { "talent_points", 1 }
        }},
        { "ach_alveolar_hard_clear", new Godot.Collections.Dictionary {
            { "id", "ach_alveolar_hard_clear" },
            { "title_key", "ACH_ALVEOLAR_HARD_CLEAR_TITLE" },
            { "desc_key", "ACH_ALVEOLAR_HARD_CLEAR_DESC" },
            { "reward_key", "ACH_ALVEOLAR_HARD_CLEAR_REWARD" },
            { "reward_cell", "" },
            { "icon", "🌬️" },
            { "target_value", 1.0f },
            { "stat_key", "map_clear_alveolar_space_hard" },
            { "map_id", "alveolar_space" },
            { "difficulty", "hard" },
            { "talent_points", 1 }
        }},
        { "ach_hepatic_hard_clear", new Godot.Collections.Dictionary {
            { "id", "ach_hepatic_hard_clear" },
            { "title_key", "ACH_HEPATIC_HARD_CLEAR_TITLE" },
            { "desc_key", "ACH_HEPATIC_HARD_CLEAR_DESC" },
            { "reward_key", "ACH_HEPATIC_HARD_CLEAR_REWARD" },
            { "reward_cell", "" },
            { "icon", "🩸" },
            { "target_value", 1.0f },
            { "stat_key", "map_clear_hepatic_sinusoid_hard" },
            { "map_id", "hepatic_sinusoid" },
            { "difficulty", "hard" },
            { "talent_points", 1 }
        }},
        { "ach_gastric_hard_clear", new Godot.Collections.Dictionary {
            { "id", "ach_gastric_hard_clear" },
            { "title_key", "ACH_GASTRIC_HARD_CLEAR_TITLE" },
            { "desc_key", "ACH_GASTRIC_HARD_CLEAR_DESC" },
            { "reward_key", "ACH_GASTRIC_HARD_CLEAR_REWARD" },
            { "reward_cell", "" },
            { "icon", "🔥" },
            { "target_value", 1.0f },
            { "stat_key", "map_clear_gastric_lumen_hard" },
            { "map_id", "gastric_lumen" },
            { "difficulty", "hard" },
            { "talent_points", 1 }
        }},
        { "ach_bbb_hard_clear", new Godot.Collections.Dictionary {
            { "id", "ach_bbb_hard_clear" },
            { "title_key", "ACH_BBB_HARD_CLEAR_TITLE" },
            { "desc_key", "ACH_BBB_HARD_CLEAR_DESC" },
            { "reward_key", "ACH_BBB_HARD_CLEAR_REWARD" },
            { "reward_cell", "" },
            { "icon", "🧬" },
            { "target_value", 1.0f },
            { "stat_key", "map_clear_blood_brain_barrier_hard" },
            { "map_id", "blood_brain_barrier" },
            { "difficulty", "hard" },
            { "talent_points", 1 }
        }}
    };

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
            GameManager.UnlockClass(rewardC);
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
            GameManager.UnlockMap(nextMap);

        string hardMap = achievementData.GetValueOrDefault("unlock_hard_map", "").AsString();
        if (!string.IsNullOrEmpty(hardMap))
            GameManager.UnlockMapHard(hardMap);

        int talentPoints = achievementData.GetValueOrDefault("talent_points", 0).AsInt32();
        if (talentPoints > 0)
            PassiveTreeManager.AddBonusPoints(talentPoints);
    }

    /// <summary>
    /// Rebuilds GameManager.MapData lock state from the persisted achievement chain.
    /// Baseline: acute_wound Normal only; everything else must be earned.
    /// </summary>
    private static void SyncMapUnlocks()
    {
        GameManager.ResetMapUnlocks();

        foreach (string achId in Achievements.Keys)
        {
            if (!IsUnlocked(achId))
                continue;

            var raw = Achievements[achId].AsGodotDictionary();
            string nextMap = raw.GetValueOrDefault("unlock_map", "").AsString();
            if (!string.IsNullOrEmpty(nextMap))
                GameManager.UnlockMap(nextMap);

            string hardMap = raw.GetValueOrDefault("unlock_hard_map", "").AsString();
            if (!string.IsNullOrEmpty(hardMap))
                GameManager.UnlockMapHard(hardMap);
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
    /// Helper: Get the prerequisite achievement id that unlocks an organ map, or "".
    /// </summary>
    public static string GetMapUnlockAchievementId(string mapId)
    {
        foreach (string achId in Achievements.Keys)
        {
            var ach = Achievements[achId].AsGodotDictionary();
            if (ach.GetValueOrDefault("unlock_map", "").AsString() == mapId)
                return achId;
        }
        return "";
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
        GameManager.UnlockClass("macrophage");

        foreach (string achId in Achievements.Keys)
        {
            var raw = Achievements[achId].AsGodotDictionary();
            string rewardCell = raw.GetValueOrDefault("reward_cell", "").AsString();
            if (!string.IsNullOrEmpty(rewardCell))
            {
                if (IsUnlocked(achId))
                {
                    GameManager.UnlockClass(rewardCell);
                }
                else
                {
                    GameManager.LockClass(rewardCell);
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
