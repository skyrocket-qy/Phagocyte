using Godot;
using Godot.Collections;
using System;
using System.Text;
using Phagocyte.Skills;

namespace Phagocyte.Core;

/// <summary>
/// Persistent pre-run passive-tree state shared by every immune cell.
/// Each cell begins from its own start node and earns one passive point per meta level.
/// </summary>
public static class PassiveTreeManager
{
    public enum TreeRarity
    {
        Normal,
        Magic,
        Rare,
        Unique
    }

    public enum TreeModifierUnit
    {
        Flat,
        Percent,
        PercentagePoints
    }

    public readonly record struct TreeStatModifier(string Stat, float Value, TreeModifierUnit Unit);

    public readonly record struct TreeNode(
        string Id,
        Vector2 Position,
        TreeRarity Rarity,
        string Branch,
        string Icon,
        string NameKey,
        string DescKey,
        int MaxStacks,
        int PointCost,
        Type? SkillType,
        TreeStatModifier[] Modifiers,
        int Ring);

    public const int BaseCellLevel = 1;

    public const string NucleusNodeId = "tree_hsc_core";

    public static readonly Vector2 WorldCenter = new(2400.0f, 2400.0f);

    public static readonly System.Collections.Generic.Dictionary<string, float> BranchBaseAngles = new()
    {
        { "senses", 18.0f },
        { "vitality", 90.0f },
        { "precision", 162.0f },
        { "motility", 234.0f },
        { "ballistics", 306.0f }
    };

    public static float GetRingRadius(int ring)
    {
        return ring switch
        {
            1 => 300.0f,
            2 => 580.0f,
            3 => 830.0f,
            4 => 1070.0f,
            5 => 1300.0f,
            _ => 0.0f
        };
    }

    public static bool IsNucleus(string nodeId)
    {
        return nodeId == NucleusNodeId;
    }

    public static Vector2 PolarPosition(int ring, float angleDegrees)
    {
        float radians = Mathf.DegToRad(angleDegrees);
        return WorldCenter + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * GetRingRadius(ring);
    }

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
                    _savePath = "user://passive_tree.json";
                }
                else
                {
                    _savePath = "res://.user_data/passive_tree.json";
                    DirAccess.MakeDirRecursiveAbsolute("res://.user_data");
                }
            }
            return _savePath;
        }
        set => _savePath = value;
    }

    private static TreeNode Make(string id, string branch, int ring, float angleDegrees, TreeRarity rarity, string icon, string nameKey, string descKey, int maxStacks, int pointCost, Type? skillType, TreeStatModifier[] modifiers)
    {
        return new TreeNode(id, PolarPosition(ring, angleDegrees), rarity, branch, icon, nameKey, descKey, maxStacks, pointCost, skillType, modifiers, ring);
    }

    private static TreeNode Legacy(string id, string branch, int ring, float angleDegrees, string icon, string nameKey, string descKey, Type skillType)
    {
        return Make(id, branch, ring, angleDegrees, TreeRarity.Magic, icon, nameKey, descKey, 5, 1, skillType, System.Array.Empty<TreeStatModifier>());
    }

    private static TreeNode Micro(string id, string branch, int ring, float angleDegrees, string icon, string nameKey, params TreeStatModifier[] modifiers)
    {
        return Make(id, branch, ring, angleDegrees, TreeRarity.Normal, icon, nameKey, "", 1, 1, null, modifiers);
    }

    private static TreeNode Notable(string id, string branch, int ring, float angleDegrees, string icon, string nameKey, params TreeStatModifier[] modifiers)
    {
        return Make(id, branch, ring, angleDegrees, TreeRarity.Magic, icon, nameKey, "", 1, 2, null, modifiers);
    }

    private static TreeNode RareNode(string id, string branch, int ring, float angleDegrees, string icon, string nameKey, params TreeStatModifier[] modifiers)
    {
        return Make(id, branch, ring, angleDegrees, TreeRarity.Rare, icon, nameKey, "", 1, 3, null, modifiers);
    }

    private static TreeNode UniqueNode(string id, string branch, int ring, float angleDegrees, string icon, string nameKey, params TreeStatModifier[] modifiers)
    {
        return Make(id, branch, ring, angleDegrees, TreeRarity.Unique, icon, nameKey, "", 1, 5, null, modifiers);
    }

    private static TreeStatModifier Fx(string stat, float value, TreeModifierUnit unit)
    {
        return new TreeStatModifier(stat, value, unit);
    }

    public static readonly TreeNode[] Nodes =
    {
        Make(NucleusNodeId, "core", 0, 0.0f, TreeRarity.Unique, "🧫", "TREE_NODE_HSC_NAME", "", 1, 0, null, System.Array.Empty<TreeStatModifier>()),

        Micro("tree_precise_edge", "precision", 2, 152.0f, "🔍", "TREE_NODE_PRECISE_EDGE_NAME",
            Fx("crit_chance", 0.02f, TreeModifierUnit.Flat),
            Fx("crit_damage", 0.10f, TreeModifierUnit.Percent)),
        Micro("tree_marked_core", "precision", 2, 172.0f, "🧿", "TREE_NODE_MARKED_CORE_NAME",
            Fx("crit_chance", 0.02f, TreeModifierUnit.Flat),
            Fx("might", 0.03f, TreeModifierUnit.Percent)),
        Legacy("passive_opsonin", "precision", 1, 162.0f, "🎯", "SKILL_OPSONIN_NAME", "SKILL_OPSONIN_DESC", typeof(PassiveOpsoninAffinity)),
        Legacy("passive_vdj", "precision", 2, 132.0f, "🎲", "SKILL_VDJ_NAME", "SKILL_VDJ_DESC", typeof(PassiveVdjDiversity)),
        Micro("tree_execution_tempo", "precision", 2, 192.0f, "⏱️", "TREE_NODE_EXECUTION_TEMPO_NAME",
            Fx("crit_damage", 0.20f, TreeModifierUnit.Percent),
            Fx("cooldown_reduction", -0.02f, TreeModifierUnit.PercentagePoints)),
        Notable("tree_assassins_mandate", "precision", 3, 162.0f, "⚔️", "TREE_NODE_ASSASSINS_MANDATE_NAME",
            Fx("crit_chance", 0.06f, TreeModifierUnit.Flat),
            Fx("crit_damage", 0.25f, TreeModifierUnit.Percent),
            Fx("move_speed", -0.03f, TreeModifierUnit.Percent)),
        Micro("tree_blood_price", "precision", 3, 184.0f, "🩸", "TREE_NODE_BLOOD_PRICE_NAME",
            Fx("might", 0.08f, TreeModifierUnit.Percent),
            Fx("max_health", -0.07f, TreeModifierUnit.Percent)),
        Micro("tree_deep_wound", "precision", 4, 140.0f, "🩹", "TREE_NODE_DEEP_WOUND_NAME",
            Fx("crit_damage", 0.15f, TreeModifierUnit.Percent),
            Fx("health_regen", -0.20f, TreeModifierUnit.Flat)),
        Micro("tree_lightfooted_killer", "precision", 4, 162.0f, "🪽", "TREE_NODE_LIGHTFOOTED_KILLER_NAME",
            Fx("move_speed", 0.05f, TreeModifierUnit.Percent),
            Fx("armor", -1.0f, TreeModifierUnit.Flat)),

        Legacy("passive_chemokine", "senses", 1, 18.0f, "🧲", "SKILL_CHEMOKINE_NAME", "SKILL_CHEMOKINE_DESC", typeof(PassiveChemokineReceptors)),
        Micro("tree_far_sense", "senses", 2, 354.0f, "🔭", "TREE_NODE_FAR_SENSE_NAME",
            Fx("magnet", 0.20f, TreeModifierUnit.Percent),
            Fx("luck", -0.05f, TreeModifierUnit.Percent)),
        Micro("tree_lucky_mutation", "senses", 2, 42.0f, "🍀", "TREE_NODE_LUCKY_MUTATION_NAME",
            Fx("luck", 0.12f, TreeModifierUnit.Percent),
            Fx("crit_chance", -0.01f, TreeModifierUnit.Flat)),
        Micro("tree_antigen_harvest", "senses", 3, 354.0f, "🌾", "TREE_NODE_ANTIGEN_HARVEST_NAME",
            Fx("growth", 0.10f, TreeModifierUnit.Percent),
            Fx("might", -0.02f, TreeModifierUnit.Percent)),
        Micro("tree_patient_observer", "senses", 3, 42.0f, "🦉", "TREE_NODE_PATIENT_OBSERVER_NAME",
            Fx("duration", 0.12f, TreeModifierUnit.Percent),
            Fx("move_speed", -0.03f, TreeModifierUnit.Percent)),
        Micro("tree_scavenger_field", "senses", 2, 18.0f, "🗺️", "TREE_NODE_SCAVENGER_FIELD_NAME",
            Fx("magnet", 0.15f, TreeModifierUnit.Percent),
            Fx("area", 0.03f, TreeModifierUnit.Percent)),
        Micro("tree_risk_assessment", "senses", 3, 18.0f, "⚖️", "TREE_NODE_RISK_ASSESSMENT_NAME",
            Fx("luck", 0.10f, TreeModifierUnit.Percent),
            Fx("max_health", -0.05f, TreeModifierUnit.Percent)),
        Notable("tree_swarm_cartography", "senses", 4, 30.0f, "📡", "TREE_NODE_SWARM_CARTOGRAPHY_NAME",
            Fx("magnet", 0.30f, TreeModifierUnit.Percent),
            Fx("growth", 0.15f, TreeModifierUnit.Percent),
            Fx("move_speed", -0.05f, TreeModifierUnit.Percent)),

        Legacy("passive_actin", "motility", 1, 234.0f, "🧬", "SKILL_ACTIN_NAME", "SKILL_ACTIN_DESC", typeof(PassiveActinPolymerization)),
        Micro("tree_cytoskeletal_drift", "motility", 2, 210.0f, "🌀", "TREE_NODE_CYTOSKELETAL_DRIFT_NAME",
            Fx("move_speed", 0.05f, TreeModifierUnit.Percent),
            Fx("area", 0.03f, TreeModifierUnit.Percent)),
        Micro("tree_slipstream", "motility", 2, 258.0f, "💨", "TREE_NODE_SLIPSTREAM_NAME",
            Fx("move_speed", 0.08f, TreeModifierUnit.Percent),
            Fx("armor", -1.0f, TreeModifierUnit.Flat)),
        Micro("tree_oxidative_pace", "motility", 3, 210.0f, "🔥", "TREE_NODE_OXIDATIVE_PACE_NAME",
            Fx("cooldown_reduction", 0.04f, TreeModifierUnit.PercentagePoints),
            Fx("health_regen", -0.15f, TreeModifierUnit.Flat)),
        Micro("tree_rapid_reposition", "motility", 3, 258.0f, "🧭", "TREE_NODE_RAPID_REPOSITION_NAME",
            Fx("move_speed", 0.06f, TreeModifierUnit.Percent),
            Fx("might", -0.02f, TreeModifierUnit.Percent)),
        Micro("tree_enduring_march", "motility", 4, 210.0f, "🥾", "TREE_NODE_ENDURING_MARCH_NAME",
            Fx("move_speed", 0.04f, TreeModifierUnit.Percent),
            Fx("max_health", 0.05f, TreeModifierUnit.Percent)),
        Micro("tree_aerobic_sprint", "motility", 4, 258.0f, "🏃", "TREE_NODE_AEROBIC_SPRINT_NAME",
            Fx("move_speed", 0.10f, TreeModifierUnit.Percent),
            Fx("cooldown_reduction", -0.03f, TreeModifierUnit.PercentagePoints)),
        Notable("tree_pseudopod_marathon", "motility", 5, 234.0f, "🏇", "TREE_NODE_PSEUDOPOD_MARATHON_NAME",
            Fx("move_speed", 0.10f, TreeModifierUnit.Percent),
            Fx("area", 0.08f, TreeModifierUnit.Percent),
            Fx("armor", -2.0f, TreeModifierUnit.Flat)),

        Legacy("passive_kinesin", "ballistics", 1, 306.0f, "🛤️", "SKILL_KINESIN_NAME", "SKILL_KINESIN_DESC", typeof(PassiveKinesinTransit)),
        Micro("tree_ballistic_threads", "ballistics", 2, 282.0f, "🧵", "TREE_NODE_BALLISTIC_THREADS_NAME",
            Fx("projectile_speed", 0.12f, TreeModifierUnit.Percent),
            Fx("duration", -0.05f, TreeModifierUnit.Percent)),
        Micro("tree_splitting_volley", "ballistics", 2, 330.0f, "🔱", "TREE_NODE_SPLITTING_VOLLEY_NAME",
            Fx("amount", 1.0f, TreeModifierUnit.Flat),
            Fx("might", -0.05f, TreeModifierUnit.Percent)),
        Micro("tree_guided_salvo", "ballistics", 3, 282.0f, "🛰️", "TREE_NODE_GUIDED_SALVO_NAME",
            Fx("projectile_speed", 0.10f, TreeModifierUnit.Percent),
            Fx("area", 0.04f, TreeModifierUnit.Percent),
            Fx("duration", -0.05f, TreeModifierUnit.Percent)),
        Micro("tree_piercing_filaments", "ballistics", 3, 330.0f, "📌", "TREE_NODE_PIERCING_FILAMENTS_NAME",
            Fx("pierce", 1.0f, TreeModifierUnit.Flat),
            Fx("projectile_speed", -0.05f, TreeModifierUnit.Percent)),
        Micro("tree_extended_chambers", "ballistics", 4, 282.0f, "⏳", "TREE_NODE_EXTENDED_CHAMBERS_NAME",
            Fx("duration", 0.12f, TreeModifierUnit.Percent),
            Fx("projectile_speed", -0.06f, TreeModifierUnit.Percent)),
        Micro("tree_overdrawn_strings", "ballistics", 4, 330.0f, "🏹", "TREE_NODE_OVERDRAWN_STRINGS_NAME",
            Fx("might", 0.08f, TreeModifierUnit.Percent),
            Fx("projectile_speed", 0.08f, TreeModifierUnit.Percent),
            Fx("health_regen", -0.25f, TreeModifierUnit.Flat)),
        Notable("tree_rolling_thunder", "ballistics", 5, 306.0f, "🌩️", "TREE_NODE_ROLLING_THUNDER_NAME",
            Fx("projectile_speed", 0.20f, TreeModifierUnit.Percent),
            Fx("pierce", 1.0f, TreeModifierUnit.Flat),
            Fx("cooldown_reduction", -0.04f, TreeModifierUnit.PercentagePoints)),

        Legacy("passive_lysosome", "vitality", 1, 90.0f, "🧪", "TREE_NODE_LYSOSOME_NAME", "TREE_NODE_LYSOSOME_DESC", typeof(PassiveLysosomePriming)),
        Micro("tree_thick_cytoplasm", "vitality", 2, 66.0f, "🫧", "TREE_NODE_THICK_CYTOPLASM_NAME",
            Fx("max_health", 0.08f, TreeModifierUnit.Percent),
            Fx("move_speed", -0.02f, TreeModifierUnit.Percent)),
        Micro("tree_rapid_clotting", "vitality", 2, 114.0f, "🩺", "TREE_NODE_RAPID_CLOTTING_NAME",
            Fx("armor", 2.0f, TreeModifierUnit.Flat),
            Fx("cooldown_reduction", -0.02f, TreeModifierUnit.PercentagePoints)),
        Micro("tree_lysosomal_appetite", "vitality", 3, 66.0f, "🍽️", "TREE_NODE_LYSOSOMAL_APPETITE_NAME",
            Fx("might", 0.06f, TreeModifierUnit.Percent),
            Fx("health_regen", 0.20f, TreeModifierUnit.Flat)),
        Micro("tree_iron_membrane", "vitality", 3, 114.0f, "🦾", "TREE_NODE_IRON_MEMBRANE_NAME",
            Fx("armor", 3.0f, TreeModifierUnit.Flat),
            Fx("move_speed", -0.03f, TreeModifierUnit.Percent)),
        Legacy("passive_bilayer", "vitality", 2, 90.0f, "🛡️", "SKILL_BILAYER_NAME", "SKILL_BILAYER_DESC", typeof(PassiveBilayerHardening)),
        Legacy("passive_endotoxin", "vitality", 3, 90.0f, "🧱", "SKILL_ENDOTOXIN_NAME", "SKILL_ENDOTOXIN_DESC", typeof(PassiveEndotoxinBarrier)),
        Micro("tree_second_wind", "vitality", 4, 66.0f, "🌬️", "TREE_NODE_SECOND_WIND_NAME",
            Fx("health_regen", 0.50f, TreeModifierUnit.Flat),
            Fx("max_health", -0.04f, TreeModifierUnit.Percent)),
        Micro("tree_contained_fury", "vitality", 4, 114.0f, "🌋", "TREE_NODE_CONTAINED_FURY_NAME",
            Fx("might", 0.08f, TreeModifierUnit.Percent),
            Fx("armor", -2.0f, TreeModifierUnit.Flat)),
        Legacy("passive_autophagy", "vitality", 4, 90.0f, "🔄", "SKILL_AUTOPHAGY_NAME", "SKILL_AUTOPHAGY_DESC", typeof(PassiveAutophagicRecycle)),
        Notable("tree_bulwark_metabolism", "vitality", 5, 90.0f, "🏰", "TREE_NODE_BULWARK_METABOLISM_NAME",
            Fx("max_health", 0.20f, TreeModifierUnit.Percent),
            Fx("armor", 3.0f, TreeModifierUnit.Flat),
            Fx("move_speed", -0.05f, TreeModifierUnit.Percent)),

        Legacy("passive_mitochondria", "core", 1, 198.0f, "⚡", "SKILL_MITOCHONDRIA_NAME", "SKILL_MITOCHONDRIA_DESC", typeof(PassiveMitochondrialOverclock)),
        RareNode("tree_adaptive_overdrive", "core", 2, 198.0f, "🚀", "TREE_NODE_ADAPTIVE_OVERDRIVE_NAME",
            Fx("might", 0.20f, TreeModifierUnit.Percent),
            Fx("move_speed", 0.08f, TreeModifierUnit.Percent),
            Fx("max_health", -0.20f, TreeModifierUnit.Percent),
            Fx("armor", -3.0f, TreeModifierUnit.Flat)),
        UniqueNode("tree_omnipotent_cytoplasm", "core", 3, 342.0f, "🌌", "TREE_NODE_OMNIPOTENT_CYTOPLASM_NAME",
            Fx("area", 0.30f, TreeModifierUnit.Percent),
            Fx("duration", 0.20f, TreeModifierUnit.Percent),
            Fx("projectile_speed", 0.20f, TreeModifierUnit.Percent),
            Fx("cooldown_reduction", 0.15f, TreeModifierUnit.PercentagePoints),
            Fx("max_health", -0.25f, TreeModifierUnit.Percent)),
        Legacy("passive_glycolysis", "core", 1, 126.0f, "🍬", "SKILL_GLYCOLYSIS_NAME", "SKILL_GLYCOLYSIS_DESC", typeof(PassiveAerobicGlycolysis)),
        Legacy("passive_hematopoietic", "core", 1, 54.0f, "🩸", "SKILL_HEMATOPOIETIC_NAME", "SKILL_HEMATOPOIETIC_DESC", typeof(PassiveHematopoieticReserve)),
        RareNode("tree_immortal_culture", "core", 2, 270.0f, "♾️", "TREE_NODE_IMMORTAL_CULTURE_NAME",
            Fx("health_regen", 2.0f, TreeModifierUnit.Flat),
            Fx("revival", 1.0f, TreeModifierUnit.Flat),
            Fx("might", -0.15f, TreeModifierUnit.Percent)),
        Legacy("passive_longevity", "core", 1, 270.0f, "⏳", "SKILL_LONGEVITY_NAME", "SKILL_LONGEVITY_DESC", typeof(PassiveCytokineLongevity))
    };

    public static readonly (string From, string To)[] Edges =
    {
        (NucleusNodeId, "passive_lysosome"),
        (NucleusNodeId, "passive_opsonin"),
        (NucleusNodeId, "passive_actin"),
        (NucleusNodeId, "passive_kinesin"),
        (NucleusNodeId, "passive_chemokine"),
        (NucleusNodeId, "passive_mitochondria"),
        (NucleusNodeId, "passive_glycolysis"),
        (NucleusNodeId, "passive_hematopoietic"),
        (NucleusNodeId, "passive_longevity"),

        ("tree_precise_edge", "passive_opsonin"),
        ("tree_marked_core", "passive_opsonin"),
        ("passive_opsonin", "passive_vdj"),
        ("passive_opsonin", "tree_execution_tempo"),
        ("passive_vdj", "tree_assassins_mandate"),
        ("tree_execution_tempo", "tree_assassins_mandate"),
        ("tree_assassins_mandate", "tree_blood_price"),
        ("tree_assassins_mandate", "tree_deep_wound"),
        ("tree_assassins_mandate", "tree_lightfooted_killer"),
        ("tree_execution_tempo", "tree_blood_price"),
        ("tree_deep_wound", "tree_lightfooted_killer"),

        ("passive_chemokine", "tree_far_sense"),
        ("passive_chemokine", "tree_lucky_mutation"),
        ("passive_chemokine", "tree_scavenger_field"),
        ("tree_far_sense", "tree_antigen_harvest"),
        ("tree_lucky_mutation", "tree_patient_observer"),
        ("tree_far_sense", "tree_risk_assessment"),
        ("tree_scavenger_field", "tree_risk_assessment"),
        ("tree_scavenger_field", "tree_swarm_cartography"),
        ("tree_swarm_cartography", "tree_patient_observer"),
        ("tree_risk_assessment", "tree_swarm_cartography"),
        ("tree_antigen_harvest", "tree_risk_assessment"),

        ("passive_actin", "tree_cytoskeletal_drift"),
        ("passive_actin", "tree_slipstream"),
        ("tree_cytoskeletal_drift", "tree_oxidative_pace"),
        ("tree_slipstream", "tree_rapid_reposition"),
        ("tree_cytoskeletal_drift", "tree_enduring_march"),
        ("tree_rapid_reposition", "tree_enduring_march"),
        ("tree_oxidative_pace", "tree_aerobic_sprint"),
        ("tree_rapid_reposition", "tree_pseudopod_marathon"),
        ("tree_enduring_march", "tree_aerobic_sprint"),
        ("tree_enduring_march", "tree_pseudopod_marathon"),

        ("passive_kinesin", "tree_ballistic_threads"),
        ("passive_kinesin", "tree_splitting_volley"),
        ("tree_ballistic_threads", "tree_guided_salvo"),
        ("tree_splitting_volley", "tree_piercing_filaments"),
        ("tree_guided_salvo", "tree_extended_chambers"),
        ("tree_piercing_filaments", "tree_overdrawn_strings"),
        ("tree_extended_chambers", "tree_overdrawn_strings"),
        ("tree_extended_chambers", "tree_rolling_thunder"),
        ("tree_overdrawn_strings", "tree_rolling_thunder"),

        ("passive_lysosome", "tree_thick_cytoplasm"),
        ("passive_lysosome", "tree_rapid_clotting"),
        ("passive_lysosome", "passive_bilayer"),
        ("tree_thick_cytoplasm", "tree_lysosomal_appetite"),
        ("tree_rapid_clotting", "tree_iron_membrane"),
        ("tree_lysosomal_appetite", "passive_bilayer"),
        ("tree_iron_membrane", "passive_bilayer"),
        ("passive_bilayer", "tree_second_wind"),
        ("passive_bilayer", "tree_contained_fury"),
        ("passive_bilayer", "passive_endotoxin"),
        ("tree_iron_membrane", "passive_endotoxin"),
        ("passive_bilayer", "passive_autophagy"),
        ("tree_second_wind", "passive_autophagy"),
        ("tree_contained_fury", "tree_bulwark_metabolism"),
        ("passive_autophagy", "tree_bulwark_metabolism"),
        ("passive_endotoxin", "tree_bulwark_metabolism"),

        ("passive_mitochondria", "tree_adaptive_overdrive"),
        ("passive_mitochondria", "passive_glycolysis"),
        ("tree_adaptive_overdrive", "tree_omnipotent_cytoplasm"),
        ("passive_glycolysis", "tree_omnipotent_cytoplasm"),
        ("passive_hematopoietic", "tree_omnipotent_cytoplasm"),
        ("tree_immortal_culture", "tree_omnipotent_cytoplasm"),
        ("passive_longevity", "tree_omnipotent_cytoplasm"),
        ("passive_mitochondria", "passive_longevity"),
        ("passive_glycolysis", "passive_hematopoietic"),
        ("passive_hematopoietic", "tree_immortal_culture"),
        ("tree_immortal_culture", "passive_longevity"),

        ("tree_ballistic_threads", "tree_aerobic_sprint"),
        ("tree_splitting_volley", "tree_pseudopod_marathon"),
        ("tree_cytoskeletal_drift", "tree_antigen_harvest"),
        ("tree_slipstream", "tree_far_sense"),
        ("tree_lucky_mutation", "tree_marked_core"),
        ("tree_patient_observer", "tree_execution_tempo"),
        ("tree_swarm_cartography", "tree_assassins_mandate"),
        ("tree_deep_wound", "passive_lysosome"),
        ("tree_lightfooted_killer", "tree_thick_cytoplasm"),
        ("tree_second_wind", "passive_glycolysis"),
        ("passive_autophagy", "passive_hematopoietic"),
        ("passive_mitochondria", "passive_actin"),
        ("passive_mitochondria", "passive_kinesin")
    };

    public static readonly System.Collections.Generic.Dictionary<string, string> StartNodes = new()
    {
        { "macrophage", "passive_lysosome" },
        { "ctl", "passive_opsonin" },
        { "neutrophil", "passive_actin" },
        { "b_cell", "passive_kinesin" },
        { "dendritic", "passive_chemokine" }
    };

    private static readonly System.Collections.Generic.Dictionary<string, string> StatLabels = new()
    {
        { "might", "STAT_MIGHT" },
        { "area", "STAT_AREA" },
        { "cooldown_reduction", "STAT_COOLDOWN_REDUCTION" },
        { "projectile_speed", "STAT_PROJECTILE_SPEED" },
        { "duration", "STAT_DURATION" },
        { "amount", "STAT_AMOUNT" },
        { "pierce", "STAT_PIERCE" },
        { "knockback", "STAT_KNOCKBACK" },
        { "crit_chance", "STAT_CRIT_CHANCE" },
        { "crit_damage", "STAT_CRIT_DAMAGE" },
        { "max_health", "STAT_MAX_HEALTH" },
        { "health_regen", "STAT_HEALTH_REGEN" },
        { "armor", "STAT_ARMOR" },
        { "move_speed", "STAT_MOVE_SPEED" },
        { "revival", "STAT_REVIVAL" },
        { "knockback_resist", "STAT_KNOCKBACK_RESIST" },
        { "magnet", "STAT_MAGNET" },
        { "growth", "STAT_GROWTH" },
        { "luck", "STAT_LUCK" },
        { "curse", "STAT_CURSE" }
    };

    public static string GetRarityName(TreeRarity rarity)
    {
        return rarity switch
        {
            TreeRarity.Magic => TranslationServer.Translate("TREE_RARITY_MAGIC"),
            TreeRarity.Rare => TranslationServer.Translate("TREE_RARITY_RARE"),
            TreeRarity.Unique => TranslationServer.Translate("TREE_RARITY_UNIQUE"),
            _ => TranslationServer.Translate("TREE_RARITY_NORMAL")
        };
    }

    public static string GetStatLabel(string stat)
    {
        return StatLabels.TryGetValue(stat, out var key) ? TranslationServer.Translate(key) : stat;
    }

    public static string FormatTreeEffect(TreeStatModifier effect)
    {
        string magnitude = effect.Unit switch
        {
            TreeModifierUnit.Percent or TreeModifierUnit.PercentagePoints => FormatSignedPercent(effect.Value),
            _ => FormatSignedNumber(effect.Value)
        };
        return TextFormatter.Format(TranslationServer.Translate("TREE_EFFECT_LINE"), magnitude, GetStatLabel(effect.Stat));
    }

    public static string DescribeNodeEffects(TreeNode node)
    {
        if (IsNucleus(node.Id))
            return TranslationServer.Translate("TREE_NUCLEUS_DESC");
        if (!string.IsNullOrEmpty(node.DescKey))
            return TranslationServer.Translate(node.DescKey);

        var text = new StringBuilder();
        text.AppendLine(TranslationServer.Translate("TREE_EFFECTS_PER_STACK"));
        foreach (var effect in node.Modifiers)
            text.AppendLine(FormatTreeEffect(effect));
        return text.ToString().TrimEnd();
    }

    public static string GetNodeIcon(string nodeId)
    {
        return TryGetNode(nodeId, out var node) ? node.Icon : "🧬";
    }

    public static string GetNodeName(string nodeId)
    {
        return TryGetNode(nodeId, out var node) ? TranslationServer.Translate(node.NameKey) : nodeId;
    }

    public static string GetNodeDescription(string nodeId)
    {
        if (!TryGetNode(nodeId, out var node))
            return "";
        if (!string.IsNullOrEmpty(node.DescKey))
            return TranslationServer.Translate(node.DescKey);
        return DescribeNodeEffects(node);
    }

    public static string GetNodeTooltipText(string cellId, string nodeId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId) || !TryGetNode(nodeId, out var node))
            return "";

        int stacks = GetNodeStacks(cellId, nodeId);
        var text = new StringBuilder();
        text.AppendLine("[b]" + node.Icon + " " + TranslationServer.Translate(node.NameKey) + "[/b]");
        if (IsNucleus(nodeId))
        {
            text.AppendLine(GetRarityName(node.Rarity) + " • " + TranslationServer.Translate("TREE_NUCLEUS_INNATE"));
        }
        else
        {
            text.AppendLine(GetRarityName(node.Rarity) + " • "
                + TextFormatter.Format(TranslationServer.Translate("TREE_COST"), node.PointCost) + " • "
                + TextFormatter.Format(TranslationServer.Translate("TREE_NODE_STACKS"), stacks, node.MaxStacks));
        }
        if (nodeId == GetStartNode(cellId))
            text.AppendLine(TranslationServer.Translate("TREE_START_NODE"));
        text.AppendLine(GetNodeDescription(nodeId));
        text.AppendLine();

        if (stacks >= node.MaxStacks)
            text.Append(TranslationServer.Translate(IsNucleus(nodeId) ? "TREE_NUCLEUS_ACTIVE" : "TREE_MAXED"));
        else if (IsAtrophic(cellId, nodeId))
            text.Append(TranslationServer.Translate("TREE_ATROPHY_LOCKED"));
        else if (CanPurchase(cellId, nodeId))
            text.Append(TranslationServer.Translate("TREE_PURCHASE_HINT"));
        else if (GetPointsAvailable(cellId) < node.PointCost)
            text.Append(TranslationServer.Translate("TREE_NO_POINTS"));
        else
            text.Append(TranslationServer.Translate("TREE_LOCKED"));
        return text.ToString();
    }

    private static string FormatSignedNumber(float value)
    {
        string sign = value < 0.0f ? "-" : "+";
        float magnitude = Mathf.Abs(value);
        string digits = magnitude >= 100.0f ? magnitude.ToString("F0") : magnitude.ToString("F2").TrimEnd('0').TrimEnd('.');
        return sign + digits;
    }

    private static string FormatSignedPercent(float value)
    {
        return FormatSignedNumber(value * 100.0f) + "%";
    }

    public static Dictionary<string, int> CellLevels = new();
    public static Dictionary<string, Dictionary<string, int>> Allocations = new();

    private static bool _loaded;

    public static void EnsureLoaded()
    {
        if (_loaded)
            return;

        _loaded = true;
        LoadFromDisk();
    }

    public static bool IsKnownCell(string cellId)
    {
        return !string.IsNullOrEmpty(cellId) && GameManager.ClassData.ContainsKey(cellId);
    }

    public static bool IsKnownNode(string nodeId)
    {
        return TryGetNode(nodeId, out _);
    }

    public static bool TryGetNode(string nodeId, out TreeNode node)
    {
        foreach (var candidate in Nodes)
        {
            if (candidate.Id == nodeId)
            {
                node = candidate;
                return true;
            }
        }
        node = default;
        return false;
    }

    public static int GetMaxStacks(string nodeId)
    {
        return TryGetNode(nodeId, out var node) ? Math.Max(1, node.MaxStacks) : 0;
    }

    public static int GetPointCost(string nodeId)
    {
        if (!TryGetNode(nodeId, out var node))
            return int.MaxValue;
        return IsNucleus(nodeId) ? 0 : Math.Max(1, node.PointCost);
    }

    public static TreeRarity GetRarity(string nodeId)
    {
        return TryGetNode(nodeId, out var node) ? node.Rarity : TreeRarity.Normal;
    }

    public static string GetStartNode(string cellId)
    {
        EnsureLoaded();
        return StartNodes.TryGetValue(cellId, out var nodeId) ? nodeId : "";
    }

    public static int GetCellLevel(string cellId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return BaseCellLevel;
        return CellLevels.TryGetValue(cellId, out var level) ? Math.Max(BaseCellLevel, level) : BaseCellLevel;
    }

    public static bool RecordRunLevel(string cellId, int runLevel)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return false;

        int normalized = Math.Max(BaseCellLevel, runLevel);
        int current = GetCellLevel(cellId);
        if (normalized <= current)
            return false;

        CellLevels[cellId] = normalized;
        SaveToDisk();
        return true;
    }

    public static Dictionary<string, int> GetAllocation(string cellId)
    {
        EnsureLoaded();
        return GetConnectedAllocation(cellId);
    }

    private static Dictionary<string, int> GetValidOwnedNodes(string cellId)
    {
        var result = new Dictionary<string, int>();
        if (!IsKnownCell(cellId) || !Allocations.TryGetValue(cellId, out var owned))
            return result;

        foreach (var pair in owned)
        {
            if (IsKnownNode(pair.Key) && pair.Value > 0)
                result[pair.Key] = Math.Min(GetMaxStacks(pair.Key), pair.Value);
        }
        return result;
    }

    private static bool HasVisitedNeighbor(string nodeId, System.Collections.Generic.HashSet<string> visited)
    {
        foreach (var neighbor in GetNeighbors(nodeId))
        {
            if (visited.Contains(neighbor))
                return true;
        }
        return false;
    }

    public static Dictionary<string, int> GetConnectedAllocation(string cellId)
    {
        var remaining = GetValidOwnedNodes(cellId);
        var connected = new Dictionary<string, int> { [NucleusNodeId] = 1 };
        string start = GetStartNode(cellId);
        var visited = new System.Collections.Generic.HashSet<string> { NucleusNodeId };
        if (!string.IsNullOrEmpty(start))
        {
            if (remaining.TryGetValue(start, out int startStacks))
                connected[start] = startStacks;
            visited.Add(start);
        }

        var frontier = new System.Collections.Generic.Queue<string>();
        foreach (var id in visited)
            frontier.Enqueue(id);
        while (frontier.Count > 0)
        {
            frontier.Dequeue();
            foreach (var pair in remaining)
            {
                if (!connected.ContainsKey(pair.Key) && HasVisitedNeighbor(pair.Key, visited))
                {
                    connected[pair.Key] = pair.Value;
                    visited.Add(pair.Key);
                    frontier.Enqueue(pair.Key);
                }
            }
        }
        return connected;
    }

    public static bool IsFullyConnected(string cellId, string nodeId)
    {
        if (!IsKnownNode(nodeId))
            return false;
        if (nodeId == GetStartNode(cellId))
            return true;
        return GetConnectedAllocation(cellId).ContainsKey(nodeId);
    }

    private static bool WouldRemainConnected(string cellId, string removeNodeId)
    {
        var remaining = GetValidOwnedNodes(cellId);
        if (!remaining.TryGetValue(removeNodeId, out int stacks))
            return false;
        if (stacks > 1)
            remaining[removeNodeId] = stacks - 1;
        else
            remaining.Remove(removeNodeId);

        string start = GetStartNode(cellId);
        var visited = new System.Collections.Generic.HashSet<string> { NucleusNodeId };
        if (!string.IsNullOrEmpty(start))
            visited.Add(start);
        var frontier = new System.Collections.Generic.Queue<string>();
        foreach (var id in visited)
            frontier.Enqueue(id);
        while (frontier.Count > 0)
        {
            frontier.Dequeue();
            foreach (var pair in remaining)
            {
                if (!visited.Contains(pair.Key) && HasVisitedNeighbor(pair.Key, visited))
                {
                    visited.Add(pair.Key);
                    frontier.Enqueue(pair.Key);
                }
            }
        }

        foreach (var pair in remaining)
        {
            if (!visited.Contains(pair.Key))
                return false;
        }
        return true;
    }

    public static int GetNodeStacks(string cellId, string nodeId)
    {
        var allocation = GetAllocation(cellId);
        return allocation.TryGetValue(nodeId, out var stacks) ? stacks : 0;
    }

    public static int GetSpentPoints(string cellId)
    {
        int total = 0;
        foreach (var pair in GetAllocation(cellId))
        {
            if (IsNucleus(pair.Key))
                continue;
            total += pair.Value * GetPointCost(pair.Key);
        }
        return total;
    }

    /// <summary>
    /// Points committed to a lineage. The nucleus and other free nodes do not count.
    /// </summary>
    public static int GetBranchInvestment(string cellId, string branch)
    {
        int total = 0;
        foreach (var pair in GetAllocation(cellId))
        {
            if (IsNucleus(pair.Key) || !TryGetNode(pair.Key, out var node) || node.Branch != branch)
                continue;
            total += pair.Value * GetPointCost(pair.Key);
        }
        return total;
    }

    /// <summary>
    /// Outer ring at which uncommitted lineages begin to autophagocytose.
    /// Metabolic pressure rises with total points spent, so committing early protects a path.
    /// </summary>
    public static int GetAtrophyThreshold(string cellId)
    {
        EnsureLoaded();
        return Math.Max(2, 5 - GetSpentPoints(cellId) / 4);
    }

    public static int GetNodeAtrophy(string cellId, string nodeId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId) || !TryGetNode(nodeId, out var node))
            return 0;
        if (IsNucleus(nodeId) || nodeId == GetStartNode(cellId) || GetNodeStacks(cellId, nodeId) > 0)
            return 0;

        int threshold = GetAtrophyThreshold(cellId);
        if (node.Ring < threshold)
            return 0;
        if (GetBranchInvestment(cellId, node.Branch) > 0)
            return 0;
        return node.Ring >= threshold + 2 ? 2 : 1;
    }

    public static bool IsAtrophic(string cellId, string nodeId)
    {
        return GetNodeAtrophy(cellId, nodeId) > 0;
    }

    /// <summary>
    /// All currently autophagocytosed (unpurchasable) nodes for a cell, computed in one pass.
    /// </summary>
    public static System.Collections.Generic.HashSet<string> GetAtrophicNodes(string cellId)
    {
        var result = new System.Collections.Generic.HashSet<string>();
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return result;

        var allocation = GetAllocation(cellId);
        int threshold = Math.Max(2, 5 - GetSpentPoints(cellId) / 4);
        var investments = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var pair in allocation)
        {
            if (IsNucleus(pair.Key) || !TryGetNode(pair.Key, out var owned) || owned.PointCost <= 0)
                continue;
            investments.TryGetValue(owned.Branch, out int current);
            investments[owned.Branch] = current + pair.Value * owned.PointCost;
        }

        string start = GetStartNode(cellId);
        foreach (var node in Nodes)
        {
            if (IsNucleus(node.Id) || node.Id == start || allocation.ContainsKey(node.Id))
                continue;
            if (node.Ring < threshold)
                continue;
            if (investments.TryGetValue(node.Branch, out int invested) && invested > 0)
                continue;
            result.Add(node.Id);
        }
        return result;
    }

    public static int GetPointsAvailable(string cellId)
    {
        return Math.Max(0, GetCellLevel(cellId) - BaseCellLevel - GetSpentPoints(cellId));
    }

    public static System.Collections.Generic.List<string> GetNeighbors(string nodeId)
    {
        var neighbors = new System.Collections.Generic.List<string>();
        foreach (var edge in Edges)
        {
            if (edge.From == nodeId && !neighbors.Contains(edge.To))
                neighbors.Add(edge.To);
            else if (edge.To == nodeId && !neighbors.Contains(edge.From))
                neighbors.Add(edge.From);
        }
        return neighbors;
    }

    public static bool IsNodeConnected(string cellId, string nodeId)
    {
        if (!IsKnownNode(nodeId))
            return false;
        if (nodeId == GetStartNode(cellId))
            return true;

        var owned = GetAllocation(cellId);
        foreach (var neighbor in GetNeighbors(nodeId))
        {
            if (neighbor == GetStartNode(cellId))
                return true;
            if (owned.ContainsKey(neighbor))
                return true;
        }
        return false;
    }

    public static bool CanPurchase(string cellId, string nodeId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId) || !IsKnownNode(nodeId))
            return false;
        if (IsNucleus(nodeId))
            return false;
        if (GetNodeStacks(cellId, nodeId) >= GetMaxStacks(nodeId))
            return false;
        if (IsAtrophic(cellId, nodeId))
            return false;
        if (GetPointsAvailable(cellId) < GetPointCost(nodeId))
            return false;
        if (GetNodeStacks(cellId, nodeId) > 0)
            return IsFullyConnected(cellId, nodeId);
        return IsNodeConnected(cellId, nodeId);
    }

    public static bool Purchase(string cellId, string nodeId)
    {
        if (!CanPurchase(cellId, nodeId))
            return false;

        if (!Allocations.TryGetValue(cellId, out var owned))
        {
            owned = new Dictionary<string, int>();
            Allocations[cellId] = owned;
        }

        owned[nodeId] = GetNodeStacks(cellId, nodeId) + 1;
        SaveToDisk();
        return true;
    }

    public static bool RefundNode(string cellId, string nodeId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId) || !IsKnownNode(nodeId) || IsNucleus(nodeId))
            return false;
        if (!Allocations.TryGetValue(cellId, out var owned) || !owned.TryGetValue(nodeId, out var stacks) || stacks <= 0)
            return false;
        if (!WouldRemainConnected(cellId, nodeId))
            return false;

        if (stacks == 1)
            owned.Remove(nodeId);
        else
            owned[nodeId] = stacks - 1;

        if (owned.Count == 0)
            Allocations.Remove(cellId);
        SaveToDisk();
        return true;
    }

    public static void ResetAllocation(string cellId)
    {
        EnsureLoaded();
        if (Allocations.Remove(cellId))
            SaveToDisk();
    }

    public static BaseSkill? CreateStackedSkill(string nodeId, int stacks)
    {
        if (!TryGetNode(nodeId, out var node))
            return null;

        int level = Mathf.Clamp(stacks, 1, node.MaxStacks);
        if (node.SkillType != null)
        {
            if (Activator.CreateInstance(node.SkillType) is not BaseSkill skill)
                return null;
            skill.Level = level;
            return skill;
        }

        return new TreeStatBundleSkill(node, level);
    }

    public static void SaveToDisk()
    {
        var levels = new Dictionary();
        foreach (var pair in CellLevels)
            levels[pair.Key] = pair.Value;

        var allocations = new Dictionary();
        foreach (var cellPair in Allocations)
        {
            var owned = new Dictionary();
            foreach (var nodePair in GetConnectedAllocation(cellPair.Key))
                owned[nodePair.Key] = nodePair.Value;
            if (owned.Count > 0)
                allocations[cellPair.Key] = owned;
        }

        var payload = new Dictionary
        {
            { "cell_levels", levels },
            { "allocations", allocations }
        };

        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (file != null)
            file.StoreString(Json.Stringify(payload, "\t"));
    }

    public static void LoadFromDisk()
    {
        _loaded = true;
        if (!FileAccess.FileExists(SavePath))
            return;

        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file == null)
            return;

        var json = new Json();
        if (json.Parse(file.GetAsText()) != Error.Ok || json.Data.VariantType != Variant.Type.Dictionary)
            return;

        var data = json.Data.AsGodotDictionary();
        if (data.TryGetValue("cell_levels", out var levelsVal) && levelsVal.VariantType == Variant.Type.Dictionary)
        {
            var levels = levelsVal.AsGodotDictionary();
            foreach (var key in levels.Keys)
            {
                string cellId = key.AsString();
                if (IsKnownCell(cellId))
                    CellLevels[cellId] = Math.Max(BaseCellLevel, levels[key].AsInt32());
            }
        }

        if (data.TryGetValue("allocations", out var allocVal) && allocVal.VariantType == Variant.Type.Dictionary)
        {
            var allocations = allocVal.AsGodotDictionary();
            foreach (var key in allocations.Keys)
            {
                string cellId = key.AsString();
                if (!IsKnownCell(cellId) || allocations[key].VariantType != Variant.Type.Dictionary)
                    continue;

                var owned = new Dictionary<string, int>();
                var savedOwned = allocations[key].AsGodotDictionary();
                foreach (var nodeKey in savedOwned.Keys)
                {
                    string nodeId = nodeKey.AsString();
                    int stacks = Mathf.Clamp(savedOwned[nodeKey].AsInt32(), 1, GetMaxStacks(nodeId));
                    if (IsKnownNode(nodeId))
                        owned[nodeId] = stacks;
                }
                if (owned.Count > 0)
                    Allocations[cellId] = owned;
            }
        }

        foreach (var cellId in new System.Collections.Generic.List<string>(Allocations.Keys))
        {
            var connected = GetConnectedAllocation(cellId);
            if (connected.Count > 0)
                Allocations[cellId] = connected;
            else
                Allocations.Remove(cellId);
        }
    }

    public static void ReloadFromDisk()
    {
        CellLevels.Clear();
        Allocations.Clear();
        _loaded = false;
        EnsureLoaded();
    }

    public static void ResetAll()
    {
        CellLevels.Clear();
        Allocations.Clear();
        _loaded = true;
        if (FileAccess.FileExists(SavePath))
            DirAccess.RemoveAbsolute(SavePath);
    }
}
