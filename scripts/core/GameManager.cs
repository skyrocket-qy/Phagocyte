using Godot;
using Godot.Collections;
using System.Collections.Generic;
using Game.Directors;

namespace Game.Core;

public partial class GameManager : Node
{
    // Runtime Player Selections
    public static string SelectedClass = "macrophage";
    public static string SelectedStage = "acute_wound";
    public static string SelectedDifficulty = RunRecordManager.DifficultyNormal;
    public static string CurrentLanguage = "en";

    /// <summary>
    /// True while the current run is the Endless Cytokine Storm mode (docs/endgame.md).
    /// Reset by a normal deploy; survives a retry so endless runs can be replayed.
    /// </summary>
    public static bool EndlessMode = false;

    // Static callback list for decoupled notification
    private static Array<Callable> _languageListeners = new Array<Callable>();

    private static System.Collections.Generic.Dictionary<string, PackedScene>? _playerScenes;
    private static System.Collections.Generic.Dictionary<string, PackedScene> PlayerScenes
    {
        get
        {
            if (_playerScenes == null)
            {
                // Scene paths are data-owned (classes.json); loaded once and cached.
                _playerScenes = new System.Collections.Generic.Dictionary<string, PackedScene>();
                foreach (string key in PlayerClassData.Keys)
                {
                    var d = (Dictionary)PlayerClassData[key];
                    string path = d.TryGetValue("scene_path", out Variant v) ? v.AsString() : "";
                    if (!string.IsNullOrEmpty(path))
                        _playerScenes[key] = AssetLoader.Load<PackedScene>(path);
                }
            }
            return _playerScenes;
        }
    }

    public static PackedScene GetPlayerScene(string classId)
    {
        if (PlayerScenes.ContainsKey(classId))
        {
            return PlayerScenes[classId];
        }
        return PlayerScenes["macrophage"];
    }

    // Class Metadata referencing translation keys
    // Class Metadata referencing translation keys (data-owned: assets/data/classes.json).
    private static Dictionary? _classData;
    public static Dictionary PlayerClassData
    {
        get
        {
            _classData ??= CatalogBuilders.BuildClasses();
            DataValidator.EnsureValidated();
            return _classData;
        }
    }

    // Stage metadata referencing translation keys and holographic scanner positioning
    // Stage metadata referencing translation keys and holographic scanner positioning
    // (data-owned: assets/data/stages.json).
    private static Dictionary? _stageData;
    public static Dictionary StageData
    {
        get
        {
            _stageData ??= CatalogBuilders.BuildStages();
            DataValidator.EnsureValidated();
            return _stageData;
        }
    }

    // Skill Catalog for Manual and Tooltips
    // (data-owned: assets/data/skill/active.json + passive.json).
    private static Dictionary? _skillCatalog;
    public static Dictionary SkillCatalog
    {
        get
        {
            _skillCatalog ??= CatalogBuilders.BuildSkills();
            DataValidator.EnsureValidated();
            return _skillCatalog;
        }
    }

    private static Dictionary? _gearCatalog;
    public static Dictionary EquipmentCatalog
    {
        get
        {
            _gearCatalog ??= CatalogBuilders.BuildGear();
            DataValidator.EnsureValidated();
            return _gearCatalog;
        }
    }

    // Enemy Catalog for Codex
    // Enemy Catalog for Codex (data-owned: assets/data/enemies.json).
    private static Dictionary? _enemyCatalog;
    public static Dictionary EnemyCatalog
    {
        get
        {
            _enemyCatalog ??= CatalogBuilders.BuildEnemies();
            DataValidator.EnsureValidated();
            return _enemyCatalog;
        }
    }

    // Enemy simulation defs (data-owned: assets/data/enemies.json).
    private static Dictionary? _enemyDefs;
    public static Dictionary EnemyDefs
    {
        get
        {
            _enemyDefs ??= CatalogBuilders.BuildEnemyDefs();
            DataValidator.EnsureValidated();
            return _enemyDefs;
        }
    }

    /// <summary>Simulation def for an enemy id, or an empty dict when unknown.</summary>
    public static Dictionary GetEnemyDef(string enemyId)
    {
        if (string.IsNullOrEmpty(enemyId) || !EnemyDefs.ContainsKey(enemyId))
            return new Dictionary();
        return (Dictionary)EnemyDefs[enemyId];
    }

    // Boss Catalog for Codex (data-owned: assets/data/bosses.json).
    private static Dictionary? _bossCatalog;
    public static Dictionary BossCatalog
    {
        get
        {
            _bossCatalog ??= CatalogBuilders.BuildBosses();
            DataValidator.EnsureValidated();
            return _bossCatalog;
        }
    }

    public override void _Ready()
    {
        // Initialize locale
        SetLanguage(CurrentLanguage);
    }

    public static void AddLanguageListener(Callable callback)
    {
        if (!_languageListeners.Contains(callback))
        {
            _languageListeners.Add(callback);
        }
    }

    public static void RemoveLanguageListener(Callable callback)
    {
        _languageListeners.Remove(callback);
    }

    public static void SetLanguage(string locale)
    {
        CurrentLanguage = locale;
        TranslationServer.SetLocale(locale);
        // Notify all registered listeners
        foreach (Callable cb in _languageListeners)
        {
            if (cb.Target is null || GodotObject.IsInstanceValid(cb.Target as GodotObject))
            {
                cb.Call(locale);
            }
        }
    }

    /// <summary>Language cycle: zh_CN -> zh_TW -> ja -> de -> fr -> ru -> es -> en.</summary>
    public static string ToggleLanguage()
    {
        string nextLang = CurrentLanguage switch
        {
            "zh_CN" => "zh_TW",
            "zh_TW" => "ja",
            "ja" => "de",
            "de" => "fr",
            "fr" => "ru",
            "ru" => "es",
            "es" => "en",
            _ => "zh_CN",
        };
        SetLanguage(nextLang);
        return nextLang;
    }

    public static void UnlockClass(string key)
    {
        if (PlayerClassData.ContainsKey(key))
        {
            var data = (Dictionary)PlayerClassData[key];
            data["unlocked"] = true;
        }
    }

    public static void LockClass(string key)
    {
        if (PlayerClassData.ContainsKey(key) && key != "macrophage")
        {
            var data = (Dictionary)PlayerClassData[key];
            data["unlocked"] = false;
        }
    }

    public static bool IsClassUnlocked(string key)
    {
        if (PlayerClassData.ContainsKey(key))
        {
            var data = (Dictionary)PlayerClassData[key];
            return data["unlocked"].AsBool();
        }
        return false;
    }

    public static Dictionary GetPlayerClass(string key)
    {
        if (!PlayerClassData.ContainsKey(key))
        {
            return new Dictionary();
        }
        var d = (Dictionary)PlayerClassData[key];
        return new Dictionary {
            { "name", TranslationServer.Translate(d["name_key"].AsString()) },
            { "role", TranslationServer.Translate(d["role_key"].AsString()) },
            { "trait", TranslationServer.Translate(d["trait_key"].AsString()) },
            { "bio_key", d.TryGetValue("bio_key", out Variant bioVal) ? bioVal : "" },
            { "unlocked", d["unlocked"] },
            { "unlock_achievement", d.TryGetValue("unlock_achievement", out Variant val) ? val : "" },
            { "base_hp", d.TryGetValue("base_hp", out Variant hpVal) ? hpVal : 100.0f },
            { "base_speed", d.TryGetValue("base_speed", out Variant spVal) ? spVal : 230.0f },
            { "base_armor", d.TryGetValue("base_armor", out Variant arVal) ? arVal : 0.0f },
            { "trait_stat", d.TryGetValue("trait_stat", out Variant tsVal) ? tsVal : "" },
            { "trait_stat_value", d.TryGetValue("trait_stat_value", out Variant tsvVal) ? tsvVal : 0.0f },
            { "extra_stats", d.TryGetValue("extra_stats", out Variant esVal) ? esVal : new Dictionary() },
            { "body_microns", d.TryGetValue("body_microns", out Variant bmVal) ? bmVal : 20.0f },
            { "deform_mag", d.TryGetValue("deform_mag", out Variant dmVal) ? dmVal : 10.0f },
            { "deform_speed", d.TryGetValue("deform_speed", out Variant dsVal) ? dsVal : 3.6f },
            { "deform_kind", d.TryGetValue("deform_kind", out Variant dkVal) ? dkVal : "standard" },
            { "deform_arms", d.TryGetValue("deform_arms", out Variant daVal) ? daVal : 0 },
            { "noise_freq", d.TryGetValue("noise_freq", out Variant nfVal) ? nfVal : 0.65f },
            { "noise_octaves", d.TryGetValue("noise_octaves", out Variant noVal) ? noVal : 2 },
            { "cyto_color", CatalogLoader.GetColor(d, "cyto_color", new Color(0.5f, 0.6f, 0.8f, 0.4f)) },
            { "membrane_color", CatalogLoader.GetColor(d, "membrane_color", new Color(0.8f, 0.9f, 1.0f, 0.8f)) },
            { "nucleus_color", CatalogLoader.GetColor(d, "nucleus_color", new Color(0.4f, 0.2f, 0.6f, 0.9f)) },
            { "nucleus_points", d.TryGetValue("nucleus_points", out Variant npVal) ? npVal : 32 },
            { "nucleus_radius", d.TryGetValue("nucleus_radius", out Variant nrVal) ? nrVal : 18.0f },
            { "nucleus_kind", d.TryGetValue("nucleus_kind", out Variant nkVal) ? nkVal : "circle" },
            { "nucleus_amp", d.TryGetValue("nucleus_amp", out Variant naVal) ? naVal : 0.0f },
            { "nucleus_freq", d.TryGetValue("nucleus_freq", out Variant nfqVal) ? nfqVal : 1.0f },
            { "innate_skill", d.TryGetValue("innate_skill", out Variant isVal) ? isVal : "" },
            { "innate_slot", d.TryGetValue("innate_slot", out Variant islVal) ? islVal : 0 }
        };
    }

    public static Dictionary GetStageInfo(string key)
    {
        if (!StageData.ContainsKey(key))
        {
            return new Dictionary();
        }
        var d = (Dictionary)StageData[key];
        return new Dictionary {
            { "id", key },
            { "name", TranslationServer.Translate(d["name_key"].AsString()) },
            { "subtitle", d.ContainsKey("subtitle_key") ? TranslationServer.Translate(d["subtitle_key"].AsString()) : "" },
            { "organ", d.ContainsKey("organ_key") ? TranslationServer.Translate(d["organ_key"].AsString()) : "" },
            { "organ_icon", d.ContainsKey("organ_icon") ? d["organ_icon"] : "🌐" },
            { "environment", TranslationServer.Translate(d["env_key"].AsString()) },
            { "mechanic", TranslationServer.Translate(d["mech_key"].AsString()) },
            { "threat", TranslationServer.Translate(d["threat_key"].AsString()) },
            { "difficulty", d.ContainsKey("difficulty") ? d["difficulty"] : 1 },
            { "color_code", d.ContainsKey("color_code") ? d["color_code"] : new Color(0.3f, 0.6f, 0.9f) },
            { "scanner_pos", d.ContainsKey("scanner_pos") ? d["scanner_pos"] : new Vector2(0.5f, 0.5f) },
            { "bg_color", d["bg_color"] },
            { "bg_color_deep", d.ContainsKey("bg_color_deep") ? d["bg_color_deep"] : d["bg_color"] },
            { "bg_color_accent", d.ContainsKey("bg_color_accent") ? d["bg_color_accent"] : d["bg_color"] },
            { "fiber_color", d.ContainsKey("fiber_color") ? d["fiber_color"] : new Color(0.2f, 0.3f, 0.4f, 0.35f) },
            { "unlocked", d["unlocked"] },
            { "hard_unlocked", d.ContainsKey("hard_unlocked") && d["hard_unlocked"].AsBool() },
            { "effects", d.TryGetValue("effects", out Variant fxVal) ? fxVal : new Array() },
            { "biochemistry", d.ContainsKey("bio_key") ? TranslationServer.Translate(d["bio_key"].AsString()) : "" }
        };
    }

    // --- Organ stage unlock chain (docs/achievement.md) ---

    public static void UnlockStage(string stageId)
    {
        if (StageData.ContainsKey(stageId))
        {
            var d = (Dictionary)StageData[stageId];
            d["unlocked"] = true;
        }
    }

    public static void UnlockStageHard(string stageId)
    {
        if (StageData.ContainsKey(stageId))
        {
            var d = (Dictionary)StageData[stageId];
            d["hard_unlocked"] = true;
        }
    }

    public static bool IsStageUnlocked(string stageId)
    {
        return StageData.ContainsKey(stageId) && ((Dictionary)StageData[stageId])["unlocked"].AsBool();
    }

    public static bool IsStageHardUnlocked(string stageId)
    {
        return StageData.ContainsKey(stageId)
            && ((Dictionary)StageData[stageId]).TryGetValue("hard_unlocked", out var val)
            && val.AsBool();
    }

    /// <summary>
    /// Restores the baseline lock state: only acute_wound (Normal) is available,
    /// every other organ stage and every Hard difficulty starts locked.
    /// The achievement chain re-applies earned unlocks afterwards.
    /// </summary>
    public static void ResetStageUnlocks()
    {
        foreach (var keyVar in StageData.Keys)
        {
            string stageId = keyVar.AsString();
            var d = (Dictionary)StageData[stageId];
            d["unlocked"] = stageId == "acute_wound";
            d["hard_unlocked"] = false;
        }
    }

    public static Dictionary GetSkillInfo(string key)
    {
        if (!SkillCatalog.ContainsKey(key))
        {
            return new Dictionary();
        }
        var d = (Dictionary)SkillCatalog[key];
        var info = new Dictionary {
            { "id", d["id"] },
            { "name", TranslationServer.Translate(d["name_key"].AsString()) },
            { "description", TranslationServer.Translate(d["desc_key"].AsString()) },
            { "biochemistry", TranslationServer.Translate(d["bio_key"].AsString()) },
            { "icon", d["icon"] },
            { "type", d["type"] },
            { "cooldown", d["cooldown"] },
            { "max_level", d["max_level"] }
        };
        if (d.TryGetValue("tags", out var tgv))
            info["tags"] = tgv;
        return info;
    }

    public static Dictionary GetEnemyInfo(string key)
    {
        if (!EnemyCatalog.ContainsKey(key))
        {
            return new Dictionary();
        }
        var d = (Dictionary)EnemyCatalog[key];
        return new Dictionary {
            { "id", d["id"] },
            { "name", TranslationServer.Translate(d["name_key"].AsString()) },
            { "description", TranslationServer.Translate(d["desc_key"].AsString()) },
            { "trait", TranslationServer.Translate(d["trait_key"].AsString()) },
            { "biochemistry", d.ContainsKey("bio_key") ? TranslationServer.Translate(d["bio_key"].AsString()) : "" },
            { "icon", d["icon"] },
            { "danger_level", d["danger_level"] }
        };
    }

    public static Dictionary GetBossInfo(string key)
    {
        if (!BossCatalog.ContainsKey(key))
        {
            return new Dictionary();
        }
        var d = (Dictionary)BossCatalog[key];
        return new Dictionary {
            { "id", d["id"] },
            { "name", TranslationServer.Translate(d["name_key"].AsString()) },
            { "description", TranslationServer.Translate(d["desc_key"].AsString()) },
            { "trait", TranslationServer.Translate(d["trait_key"].AsString()) },
            { "biochemistry", d.ContainsKey("bio_key") ? TranslationServer.Translate(d["bio_key"].AsString()) : "" },
            { "icon", d["icon"] },
            { "danger_level", d["danger_level"] }
        };
    }

    /// <summary>
    /// Endless Cytokine Storm entry (docs/endgame.md §2): unlocked by clearing any
    /// organ stage on Hard (achievement wound_hard_clear).
    /// </summary>
    public static bool IsEndlessAvailable()
    {
        return AchievementManager.IsEndlessUnlocked();
    }

    public static void StartGame(SceneTree tree)
    {
        EndlessMode = false;
        RunMutatorService.Clear();
        Engine.TimeScale = 1.0;
        PauseManager.Clear(tree);
        AudioManager.Instance?.PlayGameStart();
        tree.ChangeSceneToFile("res://scenes/main.tscn");
    }

    /// <summary>
    /// Launch an Endless Overdrive run on the selected organ. Returns false
    /// (without changing scenes) while the mode is still locked. Pass the
    /// affliction ids to activate (docs/endgame.md §4); null preserves the
    /// current RunMutatorService selection.
    /// </summary>
    public static bool StartEndlessGame(SceneTree tree, IEnumerable<string>? afflictions = null)
    {
        if (!IsEndlessAvailable())
        {
            GD.PushWarning("[GameManager] Endless Cytokine Storm is still locked (clear any organ on Hard first).");
            return false;
        }

        if (afflictions != null)
            RunMutatorService.SetSelection(afflictions);

        SelectedDifficulty = RunRecordManager.DifficultyHard;
        EndlessMode = true;
        Engine.TimeScale = 1.0;
        PauseManager.Clear(tree);
        AudioManager.Instance?.PlayGameStart();
        tree.ChangeSceneToFile("res://scenes/main.tscn");
        return true;
    }

    public static void GoToMenu(SceneTree tree)
    {
        Engine.TimeScale = 1.0;
        PauseManager.Clear(tree);
        tree.ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
    }

    public static void RestartGame(SceneTree tree)
    {
        Engine.TimeScale = 1.0;
        PauseManager.Clear(tree);
        tree.ReloadCurrentScene();
    }
}

