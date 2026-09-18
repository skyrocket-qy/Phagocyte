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
            { "unlocked", true },
            { "unlock_achievement", "" }
        }},
        { "ctl", new Dictionary {
            { "name_key", "CLASS_CTL_NAME" },
            { "role_key", "CLASS_CTL_ROLE" },
            { "trait_key", "CLASS_CTL_TRAIT" },
            { "unlocked", false },
            { "unlock_achievement", "ach_engulf_20" }
        }},
        { "neutrophil", new Dictionary {
            { "name_key", "CLASS_NEUTROPHIL_NAME" },
            { "role_key", "CLASS_NEUTROPHIL_ROLE" },
            { "trait_key", "CLASS_NEUTROPHIL_TRAIT" },
            { "unlocked", false },
            { "unlock_achievement", "ach_devour_50" }
        }},
        { "b_cell", new Dictionary {
            { "name_key", "CLASS_B_CELL_NAME" },
            { "role_key", "CLASS_B_CELL_ROLE" },
            { "trait_key", "CLASS_B_CELL_TRAIT" },
            { "unlocked", false },
            { "unlock_achievement", "ach_reach_level_5" }
        }},
        { "dendritic", new Dictionary {
            { "name_key", "CLASS_DENDRITIC_NAME" },
            { "role_key", "CLASS_DENDRITIC_ROLE" },
            { "trait_key", "CLASS_DENDRITIC_TRAIT" },
            { "unlocked", false },
            { "unlock_achievement", "ach_survive_180s" }
        }}
    };

    // Map Metadata referencing translation keys and holographic scanner positioning
    public static readonly Dictionary MapData = new Dictionary
    {
        { "acute_wound", new Dictionary {
            { "id", "acute_wound" },
            { "organ_key", "ORGAN_SKIN" },
            { "organ_icon", "🩹" },
            { "name_key", "MAP_WOUND_NAME" },
            { "subtitle_key", "MAP_WOUND_SUBTITLE" },
            { "env_key", "MAP_WOUND_ENV" },
            { "mech_key", "MAP_WOUND_MECH" },
            { "threat_key", "MAP_WOUND_THREAT" },
            { "difficulty", 1 },
            { "color_code", new Color("#e63946") },
            { "scanner_pos", new Vector2(0.26f, 0.48f) },
            { "bg_color", new Color(0.05f, 0.08f, 0.12f, 1.0f) },
            { "bg_color_deep", new Color(0.04f, 0.05f, 0.09f, 1.0f) },
            { "bg_color_accent", new Color(0.14f, 0.04f, 0.08f, 1.0f) },
            { "fiber_color", new Color(0.22f, 0.18f, 0.32f, 0.35f) },
            { "unlocked", true }
        }},
        { "alveolar_space", new Dictionary {
            { "id", "alveolar_space" },
            { "organ_key", "ORGAN_LUNGS" },
            { "organ_icon", "🫁" },
            { "name_key", "MAP_ALVEOLAR_NAME" },
            { "subtitle_key", "MAP_ALVEOLAR_SUBTITLE" },
            { "env_key", "MAP_ALVEOLAR_ENV" },
            { "mech_key", "MAP_ALVEOLAR_MECH" },
            { "threat_key", "MAP_ALVEOLAR_THREAT" },
            { "difficulty", 2 },
            { "color_code", new Color("#2a9d8f") },
            { "scanner_pos", new Vector2(0.50f, 0.28f) },
            { "bg_color", new Color(0.04f, 0.11f, 0.13f, 1.0f) },
            { "bg_color_deep", new Color(0.02f, 0.06f, 0.10f, 1.0f) },
            { "bg_color_accent", new Color(0.04f, 0.12f, 0.16f, 1.0f) },
            { "fiber_color", new Color(0.2f, 0.5f, 0.65f, 0.35f) },
            { "unlocked", true }
        }},
        { "hepatic_sinusoid", new Dictionary {
            { "id", "hepatic_sinusoid" },
            { "organ_key", "ORGAN_LIVER" },
            { "organ_icon", "🫀" },
            { "name_key", "MAP_HEPATIC_NAME" },
            { "subtitle_key", "MAP_HEPATIC_SUBTITLE" },
            { "env_key", "MAP_HEPATIC_ENV" },
            { "mech_key", "MAP_HEPATIC_MECH" },
            { "threat_key", "MAP_HEPATIC_THREAT" },
            { "difficulty", 3 },
            { "color_code", new Color("#e76f51") },
            { "scanner_pos", new Vector2(0.42f, 0.38f) },
            { "bg_color", new Color(0.05f, 0.03f, 0.02f, 1.0f) },
            { "bg_color_deep", new Color(0.05f, 0.03f, 0.02f, 1.0f) },
            { "bg_color_accent", new Color(0.18f, 0.09f, 0.04f, 1.0f) },
            { "fiber_color", new Color(0.55f, 0.40f, 0.15f, 0.35f) },
            { "unlocked", true }
        }},
        { "gastric_lumen", new Dictionary {
            { "id", "gastric_lumen" },
            { "organ_key", "ORGAN_STOMACH" },
            { "organ_icon", "🌋" },
            { "name_key", "MAP_GASTRIC_NAME" },
            { "subtitle_key", "MAP_GASTRIC_SUBTITLE" },
            { "env_key", "MAP_GASTRIC_ENV" },
            { "mech_key", "MAP_GASTRIC_MECH" },
            { "threat_key", "MAP_GASTRIC_THREAT" },
            { "difficulty", 4 },
            { "color_code", new Color("#f4a261") },
            { "scanner_pos", new Vector2(0.58f, 0.40f) },
            { "bg_color", new Color(0.06f, 0.04f, 0.01f, 1.0f) },
            { "bg_color_deep", new Color(0.06f, 0.04f, 0.01f, 1.0f) },
            { "bg_color_accent", new Color(0.17f, 0.12f, 0.02f, 1.0f) },
            { "fiber_color", new Color(0.60f, 0.50f, 0.10f, 0.35f) },
            { "unlocked", true }
        }},
        { "blood_brain_barrier", new Dictionary {
            { "id", "blood_brain_barrier" },
            { "organ_key", "ORGAN_BRAIN" },
            { "organ_icon", "🧠" },
            { "name_key", "MAP_BBB_NAME" },
            { "subtitle_key", "MAP_BBB_SUBTITLE" },
            { "env_key", "MAP_BBB_ENV" },
            { "mech_key", "MAP_BBB_MECH" },
            { "threat_key", "MAP_BBB_THREAT" },
            { "difficulty", 5 },
            { "color_code", new Color("#9d4edd") },
            { "scanner_pos", new Vector2(0.50f, 0.11f) },
            { "bg_color", new Color(0.03f, 0.02f, 0.07f, 1.0f) },
            { "bg_color_deep", new Color(0.03f, 0.02f, 0.07f, 1.0f) },
            { "bg_color_accent", new Color(0.08f, 0.04f, 0.15f, 1.0f) },
            { "fiber_color", new Color(0.35f, 0.25f, 0.65f, 0.40f) },
            { "unlocked", true }
        }}
    };

    // Skill Catalog for Manual and Tooltips
    public static readonly Dictionary SkillCatalog = new Dictionary
    {
        // --- Innate Morphology Skill ---
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
        // --- 16 Active Weapons ---
        { "ros_torrent", new Dictionary {
            { "id", "ros_torrent" },
            { "name_key", "SKILL_ROS_NAME" },
            { "desc_key", "SKILL_ROS_DESC" },
            { "bio_key", "SKILL_ROS_BIO" },
            { "icon", "💨" },
            { "type", "active" },
            { "class_id", "macrophage" },
            { "cooldown", 3.2f },
            { "max_level", 5 }
        }},
        { "perforin_lance", new Dictionary {
            { "id", "perforin_lance" },
            { "name_key", "SKILL_PERFORIN_NAME" },
            { "desc_key", "SKILL_PERFORIN_DESC" },
            { "bio_key", "SKILL_PERFORIN_BIO" },
            { "icon", "🗡️" },
            { "type", "innate" },
            { "class_id", "ctl" },
            { "cooldown", 2.8f },
            { "max_level", 5 }
        }},
        { "complement_cascade", new Dictionary {
            { "id", "complement_cascade" },
            { "name_key", "SKILL_COMPLEMENT_NAME" },
            { "desc_key", "SKILL_COMPLEMENT_DESC" },
            { "bio_key", "SKILL_COMPLEMENT_BIO" },
            { "icon", "💥" },
            { "type", "active" },
            { "class_id", "neutrophil" },
            { "cooldown", 5.0f },
            { "max_level", 5 }
        }},
        { "antibody_salvo", new Dictionary {
            { "id", "antibody_salvo" },
            { "name_key", "SKILL_ANTIBODY_NAME" },
            { "desc_key", "SKILL_ANTIBODY_DESC" },
            { "bio_key", "SKILL_ANTIBODY_BIO" },
            { "icon", "🏹" },
            { "type", "innate" },
            { "class_id", "b_cell" },
            { "cooldown", 4.0f },
            { "max_level", 5 }
        }},
        { "pseudopod_lunge", new Dictionary {
            { "id", "pseudopod_lunge" },
            { "name_key", "SKILL_LUNGE_NAME" },
            { "desc_key", "SKILL_LUNGE_DESC" },
            { "bio_key", "SKILL_LUNGE_BIO" },
            { "icon", "🥊" },
            { "type", "active" },
            { "class_id", "dendritic" },
            { "cooldown", 2.5f },
            { "max_level", 5 }
        }},
        { "nitric_oxide_halo", new Dictionary {
            { "id", "nitric_oxide_halo" },
            { "name_key", "SKILL_NO_NAME" },
            { "desc_key", "SKILL_NO_DESC" },
            { "bio_key", "SKILL_NO_BIO" },
            { "icon", "⭕" },
            { "type", "active" },
            { "class_id", "" },
            { "cooldown", 0.25f },
            { "max_level", 5 }
        }},
        { "nuclease_blades", new Dictionary {
            { "id", "nuclease_blades" },
            { "name_key", "SKILL_NUCLEASE_NAME" },
            { "desc_key", "SKILL_NUCLEASE_DESC" },
            { "bio_key", "SKILL_NUCLEASE_BIO" },
            { "icon", "⛓️" },
            { "type", "active" },
            { "class_id", "" },
            { "cooldown", 4.5f },
            { "max_level", 5 }
        }},
        { "granzyme_detonation", new Dictionary {
            { "id", "granzyme_detonation" },
            { "name_key", "SKILL_GRANZYME_NAME" },
            { "desc_key", "SKILL_GRANZYME_DESC" },
            { "bio_key", "SKILL_GRANZYME_BIO" },
            { "icon", "🧬" },
            { "type", "innate" },
            { "class_id", "neutrophil" },
            { "cooldown", 3.5f },
            { "max_level", 5 }
        }},
        { "interferon_wave", new Dictionary {
            { "id", "interferon_wave" },
            { "name_key", "SKILL_INTERFERON_NAME" },
            { "desc_key", "SKILL_INTERFERON_DESC" },
            { "bio_key", "SKILL_INTERFERON_BIO" },
            { "icon", "🌊" },
            { "type", "active" },
            { "class_id", "" },
            { "cooldown", 6.0f },
            { "max_level", 5 }
        }},
        { "lysozyme_ricochet", new Dictionary {
            { "id", "lysozyme_ricochet" },
            { "name_key", "SKILL_LYSOZYME_NAME" },
            { "desc_key", "SKILL_LYSOZYME_DESC" },
            { "bio_key", "SKILL_LYSOZYME_BIO" },
            { "icon", "🧪" },
            { "type", "active" },
            { "class_id", "" },
            { "cooldown", 3.8f },
            { "max_level", 5 }
        }},
        { "phagolysosome_vent", new Dictionary {
            { "id", "phagolysosome_vent" },
            { "name_key", "SKILL_PHAGO_VENT_NAME" },
            { "desc_key", "SKILL_PHAGO_VENT_DESC" },
            { "bio_key", "SKILL_PHAGO_VENT_BIO" },
            { "icon", "🛢️" },
            { "type", "active" },
            { "class_id", "" },
            { "cooldown", 2.0f },
            { "max_level", 5 }
        }},
        { "pro_inflammatory_arc", new Dictionary {
            { "id", "pro_inflammatory_arc" },
            { "name_key", "SKILL_PRO_INFLAM_NAME" },
            { "desc_key", "SKILL_PRO_INFLAM_DESC" },
            { "bio_key", "SKILL_PRO_INFLAM_BIO" },
            { "icon", "⚡" },
            { "type", "active" },
            { "class_id", "" },
            { "cooldown", 3.0f },
            { "max_level", 5 }
        }},
        { "exosome_singularity", new Dictionary {
            { "id", "exosome_singularity" },
            { "name_key", "SKILL_EXOSOME_NAME" },
            { "desc_key", "SKILL_EXOSOME_DESC" },
            { "bio_key", "SKILL_EXOSOME_BIO" },
            { "icon", "🧲" },
            { "type", "active" },
            { "class_id", "" },
            { "cooldown", 5.5f },
            { "max_level", 5 }
        }},
        { "defensin_barbs", new Dictionary {
            { "id", "defensin_barbs" },
            { "name_key", "SKILL_DEFENSIN_NAME" },
            { "desc_key", "SKILL_DEFENSIN_DESC" },
            { "bio_key", "SKILL_DEFENSIN_BIO" },
            { "icon", "🛡️" },
            { "type", "active" },
            { "class_id", "" },
            { "cooldown", 3.0f },
            { "max_level", 5 }
        }},
        { "mhc_tracer_beam", new Dictionary {
            { "id", "mhc_tracer_beam" },
            { "name_key", "SKILL_MHC_TRACER_NAME" },
            { "desc_key", "SKILL_MHC_TRACER_DESC" },
            { "bio_key", "SKILL_MHC_TRACER_BIO" },
            { "icon", "🎯" },
            { "type", "innate" },
            { "class_id", "dendritic" },
            { "cooldown", 4.0f },
            { "max_level", 5 }
        }},
        { "histamine_surge", new Dictionary {
            { "id", "histamine_surge" },
            { "name_key", "SKILL_HISTAMINE_NAME" },
            { "desc_key", "SKILL_HISTAMINE_DESC" },
            { "bio_key", "SKILL_HISTAMINE_BIO" },
            { "icon", "💉" },
            { "type", "active" },
            { "class_id", "" },
            { "cooldown", 4.8f },
            { "max_level", 5 }
        }},

        // --- 13 Passive Traits ---
        { "passive_actin", new Dictionary {
            { "id", "passive_actin" },
            { "name_key", "SKILL_ACTIN_NAME" },
            { "desc_key", "SKILL_ACTIN_DESC" },
            { "bio_key", "SKILL_ACTIN_BIO" },
            { "icon", "🧬" },
            { "type", "passive" },
            { "class_id", "" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }},
        { "passive_lysosome", new Dictionary {
            { "id", "passive_lysosome" },
            { "name_key", "TREE_NODE_LYSOSOME_NAME" },
            { "desc_key", "TREE_NODE_LYSOSOME_DESC" },
            { "bio_key", "SKILL_LYSOSOME_BIO" },
            { "icon", "🧪" },
            { "type", "passive" },
            { "class_id", "" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }},
        { "passive_mitochondria", new Dictionary {
            { "id", "passive_mitochondria" },
            { "name_key", "SKILL_MITOCHONDRIA_NAME" },
            { "desc_key", "SKILL_MITOCHONDRIA_DESC" },
            { "bio_key", "SKILL_MITOCHONDRIA_BIO" },
            { "icon", "⚡" },
            { "type", "passive" },
            { "class_id", "" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }},
        { "passive_opsonin", new Dictionary {
            { "id", "passive_opsonin" },
            { "name_key", "SKILL_OPSONIN_NAME" },
            { "desc_key", "SKILL_OPSONIN_DESC" },
            { "bio_key", "SKILL_OPSONIN_BIO" },
            { "icon", "🎯" },
            { "type", "passive" },
            { "class_id", "" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }},
        { "passive_chemokine", new Dictionary {
            { "id", "passive_chemokine" },
            { "name_key", "SKILL_CHEMOKINE_NAME" },
            { "desc_key", "SKILL_CHEMOKINE_DESC" },
            { "bio_key", "SKILL_CHEMOKINE_BIO" },
            { "icon", "🧲" },
            { "type", "passive" },
            { "class_id", "" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }},
        { "passive_bilayer", new Dictionary {
            { "id", "passive_bilayer" },
            { "name_key", "SKILL_BILAYER_NAME" },
            { "desc_key", "SKILL_BILAYER_DESC" },
            { "bio_key", "SKILL_BILAYER_BIO" },
            { "icon", "🛡️" },
            { "type", "passive" },
            { "class_id", "" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }},
        { "passive_autophagy", new Dictionary {
            { "id", "passive_autophagy" },
            { "name_key", "SKILL_AUTOPHAGY_NAME" },
            { "desc_key", "SKILL_AUTOPHAGY_DESC" },
            { "bio_key", "SKILL_AUTOPHAGY_BIO" },
            { "icon", "🔄" },
            { "type", "passive" },
            { "class_id", "" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }},
        { "passive_glycolysis", new Dictionary {
            { "id", "passive_glycolysis" },
            { "name_key", "SKILL_GLYCOLYSIS_NAME" },
            { "desc_key", "SKILL_GLYCOLYSIS_DESC" },
            { "bio_key", "SKILL_GLYCOLYSIS_BIO" },
            { "icon", "🍬" },
            { "type", "passive" },
            { "class_id", "" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }},
        { "passive_kinesin", new Dictionary {
            { "id", "passive_kinesin" },
            { "name_key", "SKILL_KINESIN_NAME" },
            { "desc_key", "SKILL_KINESIN_DESC" },
            { "bio_key", "SKILL_KINESIN_BIO" },
            { "icon", "🛤️" },
            { "type", "passive" },
            { "class_id", "" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }},
        { "passive_longevity", new Dictionary {
            { "id", "passive_longevity" },
            { "name_key", "SKILL_LONGEVITY_NAME" },
            { "desc_key", "SKILL_LONGEVITY_DESC" },
            { "bio_key", "SKILL_LONGEVITY_BIO" },
            { "icon", "⏳" },
            { "type", "passive" },
            { "class_id", "" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }},
        { "passive_vdj", new Dictionary {
            { "id", "passive_vdj" },
            { "name_key", "SKILL_VDJ_NAME" },
            { "desc_key", "SKILL_VDJ_DESC" },
            { "bio_key", "SKILL_VDJ_BIO" },
            { "icon", "🎲" },
            { "type", "passive" },
            { "class_id", "" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }},
        { "passive_endotoxin", new Dictionary {
            { "id", "passive_endotoxin" },
            { "name_key", "SKILL_ENDOTOXIN_NAME" },
            { "desc_key", "SKILL_ENDOTOXIN_DESC" },
            { "bio_key", "SKILL_ENDOTOXIN_BIO" },
            { "icon", "🧱" },
            { "type", "passive" },
            { "class_id", "" },
            { "cooldown", 0.0f },
            { "max_level", 5 }
        }},
        { "passive_hematopoietic", new Dictionary {
            { "id", "passive_hematopoietic" },
            { "name_key", "SKILL_HEMATOPOIETIC_NAME" },
            { "desc_key", "SKILL_HEMATOPOIETIC_DESC" },
            { "bio_key", "SKILL_HEMATOPOIETIC_BIO" },
            { "icon", "🩸" },
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
