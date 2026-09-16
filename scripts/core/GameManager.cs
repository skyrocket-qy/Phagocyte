using Godot;
using Godot.Collections;

namespace Phagocyte.Core;

public partial class GameManager : Node
{
    // Runtime Player Selections
    public static string SelectedClass = "macrophage";
    public static string SelectedMap = "acute_wound";
    public static string CurrentLanguage = "zh_CN";

    [Signal]
    public delegate void LanguageChangedEventHandler(string locale);

    // Static callback list for decoupled notification
    private static Array<Callable> _languageListeners = new Array<Callable>();

    private static System.Collections.Generic.Dictionary<string, PackedScene> _cellScenes;
    private static System.Collections.Generic.Dictionary<string, PackedScene> CellScenes
    {
        get
        {
            if (_cellScenes == null)
            {
                _cellScenes = new System.Collections.Generic.Dictionary<string, PackedScene>
                {
                    { "macrophage", GD.Load<PackedScene>("res://scenes/characters/macrophage.tscn") },
                    { "ctl", GD.Load<PackedScene>("res://scenes/characters/ctl_cell.tscn") },
                    { "neutrophil", GD.Load<PackedScene>("res://scenes/characters/neutrophil_cell.tscn") },
                    { "b_cell", GD.Load<PackedScene>("res://scenes/characters/b_cell.tscn") },
                    { "dendritic", GD.Load<PackedScene>("res://scenes/characters/dendritic_cell.tscn") }
                };
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
    public static Dictionary ClassData = new Dictionary
    {
        { "macrophage", new Dictionary {
            { "name_key", "CLASS_MACROPHAGE_NAME" },
            { "role_key", "CLASS_MACROPHAGE_ROLE" },
            { "trait_key", "CLASS_MACROPHAGE_TRAIT" },
            { "passive_key", "CLASS_MACROPHAGE_PASSIVE" },
            { "burst_key", "CLASS_MACROPHAGE_BURST" },
            { "unlocked", true },
            { "unlock_achievement", "" }
        }},
        { "ctl", new Dictionary {
            { "name_key", "CLASS_CTL_NAME" },
            { "role_key", "CLASS_CTL_ROLE" },
            { "trait_key", "CLASS_CTL_TRAIT" },
            { "passive_key", "CLASS_CTL_PASSIVE" },
            { "burst_key", "CLASS_CTL_BURST" },
            { "unlocked", false },
            { "unlock_achievement", "ach_engulf_20" }
        }},
        { "neutrophil", new Dictionary {
            { "name_key", "CLASS_NEUTROPHIL_NAME" },
            { "role_key", "CLASS_NEUTROPHIL_ROLE" },
            { "trait_key", "CLASS_NEUTROPHIL_TRAIT" },
            { "passive_key", "CLASS_NEUTROPHIL_PASSIVE" },
            { "burst_key", "CLASS_NEUTROPHIL_BURST" },
            { "unlocked", false },
            { "unlock_achievement", "ach_trigger_burst" }
        }},
        { "b_cell", new Dictionary {
            { "name_key", "CLASS_B_CELL_NAME" },
            { "role_key", "CLASS_B_CELL_ROLE" },
            { "trait_key", "CLASS_B_CELL_TRAIT" },
            { "passive_key", "CLASS_B_CELL_PASSIVE" },
            { "burst_key", "CLASS_B_CELL_BURST" },
            { "unlocked", false },
            { "unlock_achievement", "ach_reach_level_5" }
        }},
        { "dendritic", new Dictionary {
            { "name_key", "CLASS_DENDRITIC_NAME" },
            { "role_key", "CLASS_DENDRITIC_ROLE" },
            { "trait_key", "CLASS_DENDRITIC_TRAIT" },
            { "passive_key", "CLASS_DENDRITIC_PASSIVE" },
            { "burst_key", "CLASS_DENDRITIC_BURST" },
            { "unlocked", false },
            { "unlock_achievement", "ach_survive_180s" }
        }}
    };

    // Map Metadata referencing translation keys
    public static readonly Dictionary MapData = new Dictionary
    {
        { "acute_wound", new Dictionary {
            { "name_key", "MAP_WOUND_NAME" },
            { "env_key", "MAP_WOUND_ENV" },
            { "mech_key", "MAP_WOUND_MECH" },
            { "threat_key", "MAP_WOUND_THREAT" },
            { "bg_color", new Color(0.05f, 0.08f, 0.12f, 1.0f) },
            { "unlocked", true }
        }},
        { "alveolar_space", new Dictionary {
            { "name_key", "MAP_ALVEOLAR_NAME" },
            { "env_key", "MAP_ALVEOLAR_ENV" },
            { "mech_key", "MAP_ALVEOLAR_MECH" },
            { "threat_key", "MAP_ALVEOLAR_THREAT" },
            { "bg_color", new Color(0.04f, 0.11f, 0.13f, 1.0f) },
            { "unlocked", true }
        }}
    };

    // Skill Catalog for Manual and Tooltips
    public static readonly Dictionary SkillCatalog = new Dictionary
    {
        { "macrophage_pseudopods", new Dictionary {
            { "id", "macrophage_pseudopods" },
            { "name_key", "SKILL_DEFORM_NAME" },
            { "desc_key", "SKILL_DEFORM_DESC" },
            { "bio_key", "SKILL_DEFORM_BIO" },
            { "icon", "🦠" },
            { "type", "innate" },
            { "class_id", "macrophage" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }},
        { "ros_torrent", new Dictionary {
            { "id", "ros_torrent" },
            { "name_key", "SKILL_ROS_NAME" },
            { "desc_key", "SKILL_ROS_DESC" },
            { "bio_key", "SKILL_ROS_BIO" },
            { "icon", "💨" },
            { "type", "active" },
            { "class_id", "" },
            { "cooldown", 3.2f },
            { "max_level", 5 }
        }},
        { "complement_cascade", new Dictionary {
            { "id", "complement_cascade" },
            { "name_key", "SKILL_COMPLEMENT_NAME" },
            { "desc_key", "SKILL_COMPLEMENT_DESC" },
            { "bio_key", "SKILL_COMPLEMENT_BIO" },
            { "icon", "💥" },
            { "type", "active" },
            { "class_id", "" },
            { "cooldown", 5.0f },
            { "max_level", 5 }
        }},
        { "lysosomal_overload", new Dictionary {
            { "id", "lysosomal_overload" },
            { "name_key", "SKILL_LYSOSOME_NAME" },
            { "desc_key", "SKILL_LYSOSOME_DESC" },
            { "bio_key", "SKILL_LYSOSOME_BIO" },
            { "icon", "🧪" },
            { "type", "active" },
            { "class_id", "" },
            { "cooldown", 4.5f },
            { "max_level", 5 }
        }},
        { "interferon_pulse", new Dictionary {
            { "id", "interferon_pulse" },
            { "name_key", "SKILL_INTERFERON_NAME" },
            { "desc_key", "SKILL_INTERFERON_DESC" },
            { "bio_key", "SKILL_INTERFERON_BIO" },
            { "icon", "📡" },
            { "type", "active" },
            { "class_id", "" },
            { "cooldown", 6.0f },
            { "max_level", 5 }
        }},
        { "perforin_injection", new Dictionary {
            { "id", "perforin_injection" },
            { "name_key", "SKILL_PERFORIN_NAME" },
            { "desc_key", "SKILL_PERFORIN_DESC" },
            { "bio_key", "SKILL_PERFORIN_BIO" },
            { "icon", "🗡️" },
            { "type", "innate" },
            { "class_id", "ctl" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }},
        { "net_trap", new Dictionary {
            { "id", "net_trap" },
            { "name_key", "SKILL_NET_NAME" },
            { "desc_key", "SKILL_NET_DESC" },
            { "bio_key", "SKILL_NET_BIO" },
            { "icon", "🕸️" },
            { "type", "innate" },
            { "class_id", "neutrophil" },
            { "cooldown", 8.0f },
            { "max_level", 5 }
        }},
        { "phagocytic_instinct", new Dictionary {
            { "id", "phagocytic_instinct" },
            { "name_key", "SKILL_INSTINCT_NAME" },
            { "desc_key", "SKILL_INSTINCT_DESC" },
            { "bio_key", "SKILL_INSTINCT_BIO" },
            { "icon", "🩸" },
            { "type", "passive" },
            { "class_id", "" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }},
        { "chemotaxis_guidance", new Dictionary {
            { "id", "chemotaxis_guidance" },
            { "name_key", "SKILL_CHEMOTAXIS_NAME" },
            { "desc_key", "SKILL_CHEMOTAXIS_DESC" },
            { "bio_key", "SKILL_CHEMOTAXIS_BIO" },
            { "icon", "🧭" },
            { "type", "passive" },
            { "class_id", "" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }}
    };

    // Pathogen Catalog for Codex
    public static readonly Dictionary PathogenCatalog = new Dictionary
    {
        { "staph", new Dictionary {
            { "id", "staph" },
            { "name_key", "PATHOGEN_STAPH_NAME" },
            { "desc_key", "PATHOGEN_STAPH_DESC" },
            { "trait_key", "PATHOGEN_STAPH_TRAIT" },
            { "icon", "🧫" },
            { "danger_level", "★☆☆" }
        }},
        { "s_virus", new Dictionary {
            { "id", "s_virus" },
            { "name_key", "PATHOGEN_SVIRUS_NAME" },
            { "desc_key", "PATHOGEN_SVIRUS_DESC" },
            { "trait_key", "PATHOGEN_SVIRUS_TRAIT" },
            { "icon", "🦠" },
            { "danger_level", "★★☆" }
        }},
        { "flu_drift", new Dictionary {
            { "id", "flu_drift" },
            { "name_key", "PATHOGEN_FLUDRIFT_NAME" },
            { "desc_key", "PATHOGEN_FLUDRIFT_DESC" },
            { "trait_key", "PATHOGEN_FLUDRIFT_TRAIT" },
            { "icon", "🧬" },
            { "danger_level", "★★★" }
        }},
        { "malignant_cell", new Dictionary {
            { "id", "malignant_cell" },
            { "name_key", "PATHOGEN_MALIGNANT_NAME" },
            { "desc_key", "PATHOGEN_MALIGNANT_DESC" },
            { "trait_key", "PATHOGEN_MALIGNANT_TRAIT" },
            { "icon", "☣️" },
            { "danger_level", "★★★★" }
        }},
        { "pseudomonas", new Dictionary {
            { "id", "pseudomonas" },
            { "name_key", "PATHOGEN_PSEUDOMONAS_NAME" },
            { "desc_key", "PATHOGEN_PSEUDOMONAS_DESC" },
            { "trait_key", "PATHOGEN_PSEUDOMONAS_TRAIT" },
            { "icon", "🧪" },
            { "danger_level", "★★☆" }
        }},
        { "e_coli", new Dictionary {
            { "id", "e_coli" },
            { "name_key", "PATHOGEN_ECOLI_NAME" },
            { "desc_key", "PATHOGEN_ECOLI_DESC" },
            { "trait_key", "PATHOGEN_ECOLI_TRAIT" },
            { "icon", "⚡" },
            { "danger_level", "★★☆" }
        }},
        { "tb", new Dictionary {
            { "id", "tb" },
            { "name_key", "PATHOGEN_TB_NAME" },
            { "desc_key", "PATHOGEN_TB_DESC" },
            { "trait_key", "PATHOGEN_TB_TRAIT" },
            { "icon", "🔥" },
            { "danger_level", "★★★" }
        }},
        { "tetanus", new Dictionary {
            { "id", "tetanus" },
            { "name_key", "PATHOGEN_TETANUS_NAME" },
            { "desc_key", "PATHOGEN_TETANUS_DESC" },
            { "trait_key", "PATHOGEN_TETANUS_TRAIT" },
            { "icon", "🎯" },
            { "danger_level", "★★★" }
        }},
        { "h_pylori", new Dictionary {
            { "id", "h_pylori" },
            { "name_key", "PATHOGEN_HPYLORI_NAME" },
            { "desc_key", "PATHOGEN_HPYLORI_DESC" },
            { "trait_key", "PATHOGEN_HPYLORI_TRAIT" },
            { "icon", "🌀" },
            { "danger_level", "★★☆" }
        }},
        { "anthrax_spore", new Dictionary {
            { "id", "anthrax_spore" },
            { "name_key", "PATHOGEN_ANTHRAX_NAME" },
            { "desc_key", "PATHOGEN_ANTHRAX_DESC" },
            { "trait_key", "PATHOGEN_ANTHRAX_TRAIT" },
            { "icon", "🛡️" },
            { "danger_level", "★★★" }
        }},
        { "hiv", new Dictionary {
            { "id", "hiv" },
            { "name_key", "PATHOGEN_HIV_NAME" },
            { "desc_key", "PATHOGEN_HIV_DESC" },
            { "trait_key", "PATHOGEN_HIV_TRAIT" },
            { "icon", "🩸" },
            { "danger_level", "★★★" }
        }},
        { "rabies", new Dictionary {
            { "id", "rabies" },
            { "name_key", "PATHOGEN_RABIES_NAME" },
            { "desc_key", "PATHOGEN_RABIES_DESC" },
            { "trait_key", "PATHOGEN_RABIES_TRAIT" },
            { "icon", "💥" },
            { "danger_level", "★★★" }
        }},
        { "ebola", new Dictionary {
            { "id", "ebola" },
            { "name_key", "PATHOGEN_EBOLA_NAME" },
            { "desc_key", "PATHOGEN_EBOLA_DESC" },
            { "trait_key", "PATHOGEN_EBOLA_TRAIT" },
            { "icon", "🐍" },
            { "danger_level", "★★★★" }
        }},
        { "norovirus", new Dictionary {
            { "id", "norovirus" },
            { "name_key", "PATHOGEN_NOROVIRUS_NAME" },
            { "desc_key", "PATHOGEN_NOROVIRUS_DESC" },
            { "trait_key", "PATHOGEN_NOROVIRUS_TRAIT" },
            { "icon", "🌊" },
            { "danger_level", "★☆☆" }
        }},
        { "varicella_zoster", new Dictionary {
            { "id", "varicella_zoster" },
            { "name_key", "PATHOGEN_ZOSTER_NAME" },
            { "desc_key", "PATHOGEN_ZOSTER_DESC" },
            { "trait_key", "PATHOGEN_ZOSTER_TRAIT" },
            { "icon", "👻" },
            { "danger_level", "★★★" }
        }},
        { "candida", new Dictionary {
            { "id", "candida" },
            { "name_key", "PATHOGEN_CANDIDA_NAME" },
            { "desc_key", "PATHOGEN_CANDIDA_DESC" },
            { "trait_key", "PATHOGEN_CANDIDA_TRAIT" },
            { "icon", "🍄" },
            { "danger_level", "★★★" }
        }},
        { "aspergillus", new Dictionary {
            { "id", "aspergillus" },
            { "name_key", "PATHOGEN_ASPERGILLUS_NAME" },
            { "desc_key", "PATHOGEN_ASPERGILLUS_DESC" },
            { "trait_key", "PATHOGEN_ASPERGILLUS_TRAIT" },
            { "icon", "☣️" },
            { "danger_level", "★★★" }
        }},
        { "plasmodium", new Dictionary {
            { "id", "plasmodium" },
            { "name_key", "PATHOGEN_PLASMODIUM_NAME" },
            { "desc_key", "PATHOGEN_PLASMODIUM_DESC" },
            { "trait_key", "PATHOGEN_PLASMODIUM_TRAIT" },
            { "icon", "🦟" },
            { "danger_level", "★★★" }
        }},
        { "toxoplasma", new Dictionary {
            { "id", "toxoplasma" },
            { "name_key", "PATHOGEN_TOXOPLASMA_NAME" },
            { "desc_key", "PATHOGEN_TOXOPLASMA_DESC" },
            { "trait_key", "PATHOGEN_TOXOPLASMA_TRAIT" },
            { "icon", "🧠" },
            { "danger_level", "★★★" }
        }},
        { "prion", new Dictionary {
            { "id", "prion" },
            { "name_key", "PATHOGEN_PRION_NAME" },
            { "desc_key", "PATHOGEN_PRION_DESC" },
            { "trait_key", "PATHOGEN_PRION_TRAIT" },
            { "icon", "💎" },
            { "danger_level", "★★★★★" }
        }}
    };

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

    public static string ToggleLanguage()
    {
        string nextLang = CurrentLanguage == "zh_CN" ? "en" : "zh_CN";
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
            { "passive", TranslationServer.Translate(d["passive_key"].AsString()) },
            { "burst", TranslationServer.Translate(d["burst_key"].AsString()) },
            { "unlocked", d["unlocked"] },
            { "unlock_achievement", d.TryGetValue("unlock_achievement", out Variant val) ? val : "" }
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
            { "name", TranslationServer.Translate(d["name_key"].AsString()) },
            { "environment", TranslationServer.Translate(d["env_key"].AsString()) },
            { "mechanic", TranslationServer.Translate(d["mech_key"].AsString()) },
            { "threat", TranslationServer.Translate(d["threat_key"].AsString()) },
            { "bg_color", d["bg_color"] },
            { "unlocked", d["unlocked"] }
        };
    }

    public static Dictionary GetSkillInfo(string key)
    {
        if (!SkillCatalog.ContainsKey(key))
        {
            return new Dictionary();
        }
        var d = (Dictionary)SkillCatalog[key];
        string typeLabel = "";
        switch (d["type"].AsString())
        {
            case "innate":
                typeLabel = TranslationServer.Translate("TOOLTIP_TAG_INNATE");
                break;
            case "active":
                typeLabel = TranslationServer.Translate("TOOLTIP_TAG_ACTIVE");
                break;
            case "passive":
                typeLabel = TranslationServer.Translate("TOOLTIP_TAG_PASSIVE");
                break;
        }
        return new Dictionary {
            { "id", d["id"] },
            { "name", TranslationServer.Translate(d["name_key"].AsString()) },
            { "description", TranslationServer.Translate(d["desc_key"].AsString()) },
            { "biochemistry", TranslationServer.Translate(d["bio_key"].AsString()) },
            { "icon", d["icon"] },
            { "type", d["type"] },
            { "type_label", typeLabel },
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

    public static void StartGame(SceneTree tree)
    {
        tree.Paused = false;
        tree.ChangeSceneToFile("res://scenes/main.tscn");
    }

    public static void GoToMenu(SceneTree tree)
    {
        tree.Paused = false;
        tree.ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
    }

    public static void RestartGame(SceneTree tree)
    {
        tree.Paused = false;
        tree.ReloadCurrentScene();
    }
}
