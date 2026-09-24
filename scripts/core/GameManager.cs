using Godot;
using Godot.Collections;
using System.Collections.Generic;
using Phagocyte.Endgame;

namespace Phagocyte.Core;

public partial class GameManager : Node
{
    // Runtime Player Selections
    public static string SelectedClass = "macrophage";
    public static string SelectedMap = "acute_wound";
    public static string SelectedDifficulty = RunRecordManager.DifficultyNormal;
    public static string CurrentLanguage = "zh_CN";

    /// <summary>
    /// True while the current run is the Endless Cytokine Storm mode (docs/endgame.md).
    /// Reset by a normal deploy; survives a retry so endless runs can be replayed.
    /// </summary>
    public static bool EndlessMode = false;

    // Static callback list for decoupled notification
    private static Array<Callable> _languageListeners = new Array<Callable>();

    private static System.Collections.Generic.Dictionary<string, PackedScene>? _cellScenes;
    private static System.Collections.Generic.Dictionary<string, PackedScene> CellScenes
    {
        get
        {
            if (_cellScenes == null)
            {
                // Scene paths are data-owned (classes.json); loaded once and cached.
                _cellScenes = new System.Collections.Generic.Dictionary<string, PackedScene>();
                foreach (string key in ClassData.Keys)
                {
                    var d = (Dictionary)ClassData[key];
                    string path = d.TryGetValue("scene_path", out Variant v) ? v.AsString() : "";
                    if (!string.IsNullOrEmpty(path))
                        _cellScenes[key] = AssetLoader.Load<PackedScene>(path);
                }
            }
            return _cellScenes;
        }
    }

    public static PackedScene GetCellScene(string classId)
    {
        if (CellScenes.ContainsKey(classId))
        {
            return CellScenes[classId];
        }
        return CellScenes["macrophage"];
    }

    // Class Metadata referencing translation keys
    // Class Metadata referencing translation keys (data-owned: assets/data/classes.json).
    private static Dictionary? _classData;
    public static Dictionary ClassData
    {
        get
        {
            _classData ??= CatalogBuilders.BuildClasses();
            DataValidator.EnsureValidated();
            return _classData;
        }
    }

    // Map Metadata referencing translation keys and holographic scanner positioning
    // Map Metadata referencing translation keys and holographic scanner positioning
    // (data-owned: assets/data/maps.json).
    private static Dictionary? _mapData;
    public static Dictionary MapData
    {
        get
        {
            _mapData ??= CatalogBuilders.BuildMaps();
            DataValidator.EnsureValidated();
            return _mapData;
        }
    }

    // Skill Catalog for Manual and Tooltips
    // Skill Catalog for Manual and Tooltips (data-owned: assets/data/skills.json).
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

    // Organelle chamber catalog (TODO Phase 0): 2x2 equipment definitions
    // (data-owned: assets/data/organelles.json).
    private static Dictionary? _organelleCatalog;
    public static Dictionary OrganelleCatalog
    {
        get
        {
            _organelleCatalog ??= CatalogBuilders.BuildOrganelles();
            DataValidator.EnsureValidated();
            return _organelleCatalog;
        }
    }

    // Pathogen Catalog for Codex
    // Pathogen Catalog for Codex (data-owned: assets/data/pathogens.json).
    private static Dictionary? _pathogenCatalog;
    public static Dictionary PathogenCatalog
    {
        get
        {
            _pathogenCatalog ??= CatalogBuilders.BuildPathogens();
            DataValidator.EnsureValidated();
            return _pathogenCatalog;
        }
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
        if (ClassData.ContainsKey(key))
        {
            var data = (Dictionary)ClassData[key];
            data["unlocked"] = true;
        }
    }

    public static void LockClass(string key)
    {
        if (ClassData.ContainsKey(key) && key != "macrophage")
        {
            var data = (Dictionary)ClassData[key];
            data["unlocked"] = false;
        }
    }

    public static bool IsClassUnlocked(string key)
    {
        if (ClassData.ContainsKey(key))
        {
            var data = (Dictionary)ClassData[key];
            return data["unlocked"].AsBool();
        }
        return false;
    }

    public static Dictionary GetClassInfo(string key)
    {
        if (!ClassData.ContainsKey(key))
        {
            return new Dictionary();
        }
        var d = (Dictionary)ClassData[key];
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
            { "trait_stat_value", d.TryGetValue("trait_stat_value", out Variant tsvVal) ? tsvVal : 0.0f }
        };
    }

    public static Dictionary GetMapInfo(string key)
    {
        if (!MapData.ContainsKey(key))
        {
            return new Dictionary();
        }
        var d = (Dictionary)MapData[key];
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
            { "biochemistry", d.ContainsKey("bio_key") ? TranslationServer.Translate(d["bio_key"].AsString()) : "" }
        };
    }

    // --- Organ map unlock chain (docs/achievement.md) ---

    public static void UnlockMap(string mapId)
    {
        if (MapData.ContainsKey(mapId))
        {
            var d = (Dictionary)MapData[mapId];
            d["unlocked"] = true;
        }
    }

    public static void UnlockMapHard(string mapId)
    {
        if (MapData.ContainsKey(mapId))
        {
            var d = (Dictionary)MapData[mapId];
            d["hard_unlocked"] = true;
        }
    }

    public static bool IsMapUnlocked(string mapId)
    {
        return MapData.ContainsKey(mapId) && ((Dictionary)MapData[mapId])["unlocked"].AsBool();
    }

    public static bool IsMapHardUnlocked(string mapId)
    {
        return MapData.ContainsKey(mapId)
            && ((Dictionary)MapData[mapId]).TryGetValue("hard_unlocked", out var val)
            && val.AsBool();
    }

    /// <summary>
    /// Restores the baseline lock state: only acute_wound (Normal) is available,
    /// every other organ map and every Hard difficulty starts locked.
    /// The achievement chain re-applies earned unlocks afterwards.
    /// </summary>
    public static void ResetMapUnlocks()
    {
        foreach (var keyVar in MapData.Keys)
        {
            string mapId = keyVar.AsString();
            var d = (Dictionary)MapData[mapId];
            d["unlocked"] = mapId == "acute_wound";
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
        return new Dictionary {
            { "id", d["id"] },
            { "name", TranslationServer.Translate(d["name_key"].AsString()) },
            { "description", TranslationServer.Translate(d["desc_key"].AsString()) },
            { "biochemistry", TranslationServer.Translate(d["bio_key"].AsString()) },
            { "icon", d["icon"] },
            { "type", d["type"] },
            { "cooldown", d["cooldown"] },
            { "max_level", d["max_level"] }
        };
    }

    public static Dictionary GetPathogenInfo(string key)
    {
        if (!PathogenCatalog.ContainsKey(key))
        {
            return new Dictionary();
        }
        var d = (Dictionary)PathogenCatalog[key];
        return new Dictionary {
            { "id", d["id"] },
            { "name", TranslationServer.Translate(d["name_key"].AsString()) },
            { "description", TranslationServer.Translate(d["desc_key"].AsString()) },
            { "trait", TranslationServer.Translate(d["trait_key"].AsString()) },
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
            { "icon", d["icon"] },
            { "danger_level", d["danger_level"] }
        };
    }

    /// <summary>
    /// Endless Cytokine Storm entry (docs/endgame.md §2): unlocked by clearing any
    /// organ map on Hard (achievement wound_hard_clear).
    /// </summary>
    public static bool IsEndlessAvailable()
    {
        return AchievementManager.IsEndlessUnlocked();
    }

    public static void StartGame(SceneTree tree)
    {
        EndlessMode = false;
        AfflictionManager.Clear();
        Engine.TimeScale = 1.0;
        PauseManager.Clear(tree);
        AudioManager.Instance?.PlayGameStart();
        tree.ChangeSceneToFile("res://scenes/main.tscn");
    }

    /// <summary>
    /// Launch an Endless Overdrive run on the selected organ. Returns false
    /// (without changing scenes) while the mode is still locked. Pass the
    /// affliction ids to activate (docs/endgame.md §4); null preserves the
    /// current AfflictionManager selection.
    /// </summary>
    public static bool StartEndlessGame(SceneTree tree, IEnumerable<string>? afflictions = null)
    {
        if (!IsEndlessAvailable())
        {
            GD.PushWarning("[GameManager] Endless Cytokine Storm is still locked (clear any organ on Hard first).");
            return false;
        }

        if (afflictions != null)
            AfflictionManager.SetSelection(afflictions);

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
