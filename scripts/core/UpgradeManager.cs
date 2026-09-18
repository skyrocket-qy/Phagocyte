using Godot;
using Godot.Collections;
using System;
using Phagocyte.Skills;

namespace Phagocyte.Core;

/// <summary>
/// Manages Level-Up 3-Choice generation from Active Cytokines and Passive Organelles.
/// </summary>
public partial class UpgradeManager : RefCounted
{
    // All catalog active weapon classes (16)
    public static Array<Dictionary> ActiveCatalog = new Array<Dictionary>()
    {
        new Dictionary { { "id", "ros_torrent" }, { "name", "SKILL_ROS_NAME" }, { "desc", "SKILL_ROS_DESC" }, { "icon", "💨" }, { "class_type", typeof(RosTorrentSkill).AssemblyQualifiedName }, { "class_id", "macrophage" } },
        new Dictionary { { "id", "perforin_lance" }, { "name", "SKILL_PERFORIN_NAME" }, { "desc", "SKILL_PERFORIN_DESC" }, { "icon", "🗡️" }, { "class_type", typeof(PerforinLanceSkill).AssemblyQualifiedName }, { "class_id", "ctl" } },
        new Dictionary { { "id", "complement_cascade" }, { "name", "SKILL_COMPLEMENT_NAME" }, { "desc", "SKILL_COMPLEMENT_DESC" }, { "icon", "💥" }, { "class_type", typeof(ComplementCascadeSkill).AssemblyQualifiedName }, { "class_id", "neutrophil" } },
        new Dictionary { { "id", "antibody_salvo" }, { "name", "SKILL_ANTIBODY_NAME" }, { "desc", "SKILL_ANTIBODY_DESC" }, { "icon", "🏹" }, { "class_type", typeof(AntibodySalvoSkill).AssemblyQualifiedName }, { "class_id", "b_cell" } },
        new Dictionary { { "id", "pseudopod_lunge" }, { "name", "SKILL_LUNGE_NAME" }, { "desc", "SKILL_LUNGE_DESC" }, { "icon", "🥊" }, { "class_type", typeof(PseudopodLungeSkill).AssemblyQualifiedName }, { "class_id", "dendritic" } },
        new Dictionary { { "id", "nitric_oxide_halo" }, { "name", "SKILL_NO_NAME" }, { "desc", "SKILL_NO_DESC" }, { "icon", "⭕" }, { "class_type", typeof(NitricOxideHaloSkill).AssemblyQualifiedName } },
        new Dictionary { { "id", "nuclease_blades" }, { "name", "SKILL_NUCLEASE_NAME" }, { "desc", "SKILL_NUCLEASE_DESC" }, { "icon", "⛓️" }, { "class_type", typeof(NucleaseBladesSkill).AssemblyQualifiedName } },
        new Dictionary { { "id", "granzyme_detonation" }, { "name", "SKILL_GRANZYME_NAME" }, { "desc", "SKILL_GRANZYME_DESC" }, { "icon", "🧬" }, { "class_type", typeof(GranzymeDetonationSkill).AssemblyQualifiedName }, { "class_id", "neutrophil" } },
        new Dictionary { { "id", "interferon_wave" }, { "name", "SKILL_INTERFERON_NAME" }, { "desc", "SKILL_INTERFERON_DESC" }, { "icon", "🌊" }, { "class_type", typeof(InterferonWaveSkill).AssemblyQualifiedName } },
        new Dictionary { { "id", "lysozyme_ricochet" }, { "name", "SKILL_LYSOZYME_NAME" }, { "desc", "SKILL_LYSOZYME_DESC" }, { "icon", "🧪" }, { "class_type", typeof(LysozymeRicochetSkill).AssemblyQualifiedName } },
        new Dictionary { { "id", "phagolysosome_vent" }, { "name", "SKILL_PHAGO_VENT_NAME" }, { "desc", "SKILL_PHAGO_VENT_DESC" }, { "icon", "🛢️" }, { "class_type", typeof(PhagolysosomeVentSkill).AssemblyQualifiedName } },
        new Dictionary { { "id", "pro_inflammatory_arc" }, { "name", "SKILL_PRO_INFLAM_NAME" }, { "desc", "SKILL_PRO_INFLAM_DESC" }, { "icon", "⚡" }, { "class_type", typeof(ProInflammatoryArcSkill).AssemblyQualifiedName } },
        new Dictionary { { "id", "exosome_singularity" }, { "name", "SKILL_EXOSOME_NAME" }, { "desc", "SKILL_EXOSOME_DESC" }, { "icon", "🧲" }, { "class_type", typeof(ExosomeSingularitySkill).AssemblyQualifiedName } },
        new Dictionary { { "id", "defensin_barbs" }, { "name", "SKILL_DEFENSIN_NAME" }, { "desc", "SKILL_DEFENSIN_DESC" }, { "icon", "🛡️" }, { "class_type", typeof(DefensinBarbsSkill).AssemblyQualifiedName } },
        new Dictionary { { "id", "mhc_tracer_beam" }, { "name", "SKILL_MHC_TRACER_NAME" }, { "desc", "SKILL_MHC_TRACER_DESC" }, { "icon", "🎯" }, { "class_type", typeof(MhcTracerBeamSkill).AssemblyQualifiedName }, { "class_id", "dendritic" } },
        new Dictionary { { "id", "histamine_surge" }, { "name", "SKILL_HISTAMINE_NAME" }, { "desc", "SKILL_HISTAMINE_DESC" }, { "icon", "💉" }, { "class_type", typeof(HistamineSurgeSkill).AssemblyQualifiedName } }
    };

    // All catalog passive trait classes (13)
    public static Array<Dictionary> PassiveCatalog = new Array<Dictionary>()
    {
        new Dictionary { { "id", "passive_actin" }, { "name", "SKILL_ACTIN_NAME" }, { "desc", "SKILL_ACTIN_DESC" }, { "icon", "🧬" }, { "class_type", typeof(PassiveActinPolymerization).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_lysosome" }, { "name", "TREE_NODE_LYSOSOME_NAME" }, { "desc", "TREE_NODE_LYSOSOME_DESC" }, { "icon", "🧪" }, { "class_type", typeof(PassiveLysosomePriming).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_mitochondria" }, { "name", "SKILL_MITOCHONDRIA_NAME" }, { "desc", "SKILL_MITOCHONDRIA_DESC" }, { "icon", "⚡" }, { "class_type", typeof(PassiveMitochondrialOverclock).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_opsonin" }, { "name", "SKILL_OPSONIN_NAME" }, { "desc", "SKILL_OPSONIN_DESC" }, { "icon", "🎯" }, { "class_type", typeof(PassiveOpsoninAffinity).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_chemokine" }, { "name", "SKILL_CHEMOKINE_NAME" }, { "desc", "SKILL_CHEMOKINE_DESC" }, { "icon", "🧲" }, { "class_type", typeof(PassiveChemokineReceptors).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_bilayer" }, { "name", "SKILL_BILAYER_NAME" }, { "desc", "SKILL_BILAYER_DESC" }, { "icon", "🛡️" }, { "class_type", typeof(PassiveBilayerHardening).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_autophagy" }, { "name", "SKILL_AUTOPHAGY_NAME" }, { "desc", "SKILL_AUTOPHAGY_DESC" }, { "icon", "🔄" }, { "class_type", typeof(PassiveAutophagicRecycle).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_glycolysis" }, { "name", "SKILL_GLYCOLYSIS_NAME" }, { "desc", "SKILL_GLYCOLYSIS_DESC" }, { "icon", "🍬" }, { "class_type", typeof(PassiveAerobicGlycolysis).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_kinesin" }, { "name", "SKILL_KINESIN_NAME" }, { "desc", "SKILL_KINESIN_DESC" }, { "icon", "🛤️" }, { "class_type", typeof(PassiveKinesinTransit).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_longevity" }, { "name", "SKILL_LONGEVITY_NAME" }, { "desc", "SKILL_LONGEVITY_DESC" }, { "icon", "⏳" }, { "class_type", typeof(PassiveCytokineLongevity).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_vdj" }, { "name", "SKILL_VDJ_NAME" }, { "desc", "SKILL_VDJ_DESC" }, { "icon", "🎲" }, { "class_type", typeof(PassiveVdjDiversity).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_endotoxin" }, { "name", "SKILL_ENDOTOXIN_NAME" }, { "desc", "SKILL_ENDOTOXIN_DESC" }, { "icon", "🧱" }, { "class_type", typeof(PassiveEndotoxinBarrier).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_hematopoietic" }, { "name", "SKILL_HEMATOPOIETIC_NAME" }, { "desc", "SKILL_HEMATOPOIETIC_DESC" }, { "icon", "🩸" }, { "class_type", typeof(PassiveHematopoieticReserve).AssemblyQualifiedName } }
    };

    /// <summary>
    /// Superweapon catalyst pairs (docs/skill.md §5): a maxed active + its
    /// paired passive fuse into the corresponding epigenetic evolution.
    /// </summary>
    public const int CatalystActiveLevel = 5;

    public static readonly System.Collections.Generic.Dictionary<string, string> CatalystPairs = new()
    {
        { "perforin_lance", "passive_lysosome" },
        { "complement_cascade", "passive_actin" },
        { "antibody_salvo", "passive_opsonin" },
        { "ros_torrent", "passive_mitochondria" },
        { "pseudopod_lunge", "passive_chemokine" }
    };

    /// <summary>
    /// True when <paramref name="passiveId"/> is the catalyst partner of an
    /// equipped active skill already at Lv.5 (docs/skill.md §5 / tutorial cue 4).
    /// </summary>
    public static bool IsCatalystReady(Node2D player, string passiveId, out string activeId)
    {
        activeId = "";
        if (player == null || !GodotObject.IsInstanceValid(player) || string.IsNullOrEmpty(passiveId))
            return false;

        foreach (var pair in CatalystPairs)
        {
            if (pair.Value == passiveId)
            {
                activeId = pair.Key;
                break;
            }
        }

        if (string.IsNullOrEmpty(activeId))
            return false;

        var sm = player.GetNodeOrNull<SkillManager>("SkillManager");
        if (sm == null)
            return false;

        foreach (var skill in sm.ActiveSlots)
        {
            if (skill != null && GodotObject.IsInstanceValid(skill)
                && skill.SkillId == activeId && skill.Level >= CatalystActiveLevel)
            {
                return true;
            }
        }

        activeId = "";
        return false;
    }

    /// <summary>Tags a passive candidate with the golden catalyst resonance flag.</summary>
    private static void ApplyCatalystFlag(Node2D player, Dictionary candidate, string passiveId)
    {
        if (IsCatalystReady(player, passiveId, out string activeId))
        {
            candidate["catalyst"] = true;
            candidate["catalyst_active"] = activeId;
        }
    }

    /// <summary>
    /// Generates 3 distinct randomized upgrade cards for the player
    /// </summary>
    public static Array<Dictionary> GenerateChoices(Node2D player, int count = 3)
    {
        if (player == null || !GodotObject.IsInstanceValid(player))
        {
            return new Array<Dictionary>();
        }

        var sm = player.GetNodeOrNull<SkillManager>("SkillManager");
        if (sm == null)
        {
            return new Array<Dictionary>();
        }

        var candidates = new Array<Dictionary>();

        // Gather equipped active IDs
        var equippedActiveIds = new System.Collections.Generic.List<string>();
        int activeCount = 0;
        foreach (var skill in sm.ActiveSlots)
        {
            if (skill != null && GodotObject.IsInstanceValid(skill))
            {
                activeCount++;
                equippedActiveIds.Add(skill.SkillId);
                if (skill.Level < skill.MaxLevel)
                {
                    candidates.Add(new Dictionary
                    {
                        { "type", "upgrade_active" },
                        { "id", skill.SkillId },
                        { "name", !string.IsNullOrEmpty(skill.NameKey) ? skill.NameKey : skill.SkillId },
                        { "icon", skill.IconSymbol },
                        { "level", skill.Level + 1 },
                        { "badge", "UPGRADE" },
                        { "desc", !string.IsNullOrEmpty(skill.DescKey) ? skill.DescKey : $"Upgrade to Lv.{skill.Level + 1}" },
                        { "skill_ref", skill }
                    });
                }
            }
        }

        // Gather equipped passive IDs
        var equippedPassiveIds = new System.Collections.Generic.List<string>();
        int passiveCount = 0;
        foreach (var skill in sm.PassiveSlots)
        {
            if (skill != null && GodotObject.IsInstanceValid(skill))
            {
                passiveCount++;
                equippedPassiveIds.Add(skill.SkillId);
                if (skill.Level < skill.MaxLevel)
                {
                    var passiveCandidate = new Dictionary
                    {
                        { "type", "upgrade_passive" },
                        { "id", skill.SkillId },
                        { "name", !string.IsNullOrEmpty(skill.NameKey) ? skill.NameKey : skill.SkillId },
                        { "icon", skill.IconSymbol },
                        { "level", skill.Level + 1 },
                        { "badge", "UPGRADE" },
                        { "desc", !string.IsNullOrEmpty(skill.DescKey) ? skill.DescKey : $"Upgrade to Lv.{skill.Level + 1}" },
                        { "skill_ref", skill }
                    };
                    ApplyCatalystFlag(player, passiveCandidate, skill.SkillId);
                    candidates.Add(passiveCandidate);
                }
            }
        }

        // New Actives if slots available (< 5)
        if (activeCount < SkillManager.MaxActiveSlots)
        {
            foreach (var item in ActiveCatalog)
            {
                // Check if required cell class is unlocked
                string reqClass = item.TryGetValue("class_id", out var classIdVal) ? classIdVal.AsString() : "";
                if (!string.IsNullOrEmpty(reqClass) && !GameManager.IsClassUnlocked(reqClass))
                {
                    continue;
                }

                string id = item["id"].AsString();
                if (!equippedActiveIds.Contains(id))
                {
                    candidates.Add(new Dictionary
                    {
                        { "type", "new_active" },
                        { "id", item["id"] },
                        { "name", item["name"] },
                        { "icon", item["icon"] },
                        { "level", 1 },
                        { "badge", "NEW ACTIVE" },
                        { "desc", item["desc"] },
                        { "skill_class", item["class_type"] }
                    });
                }
            }
        }

        // New Passives if slots available (< 5)
        if (passiveCount < SkillManager.MaxPassiveSlots)
        {
            var treeOwned = PassiveTreeManager.GetAllocation(GameManager.SelectedClass);
            foreach (var item in PassiveCatalog)
            {
                string id = item["id"].AsString();
                if (!equippedPassiveIds.Contains(id) && !treeOwned.ContainsKey(id))
                {
                    var newPassiveCandidate = new Dictionary
                    {
                        { "type", "new_passive" },
                        { "id", item["id"] },
                        { "name", item["name"] },
                        { "icon", item["icon"] },
                        { "level", 1 },
                        { "badge", "NEW PASSIVE" },
                        { "desc", item["desc"] },
                        { "skill_class", item["class_type"] }
                    };
                    ApplyCatalystFlag(player, newPassiveCandidate, id);
                    candidates.Add(newPassiveCandidate);
                }
            }
        }

        // Shuffle candidates
        candidates.Shuffle();

        // Pick requested count
        var results = new Array<Dictionary>();
        for (int i = 0; i < Mathf.Min(count, candidates.Count); i++)
        {
            results.Add(candidates[i]);
        }

        // Fallback if no candidates exist (all maxed out)
        while (results.Count < count)
        {
            results.Add(new Dictionary
            {
                { "type", "heal_fallback" },
                { "id", "heal_fallback" },
                { "name", "ATP 生化質回充" },
                { "icon", "💚" },
                { "level", 0 },
                { "badge", "HEAL" },
                { "desc", "立即回復 35% 最大生命值並觸發脈衝" }
            });
        }

        return results;
    }

    /// <summary>
    /// Applies the selected choice to the player
    /// </summary>
    public static bool ApplyChoice(Node2D player, Dictionary choice)
    {
        if (player == null || !GodotObject.IsInstanceValid(player))
        {
            return false;
        }

        var sm = player.GetNodeOrNull<SkillManager>("SkillManager");
        if (sm == null)
        {
            return false;
        }

        string cType = choice.TryGetValue("type", out var typeVal) ? typeVal.AsString() : "";

        switch (cType)
        {
            case "new_active":
            {
                if (choice.TryGetValue("skill_class", out var classVal))
                {
                    Type skillType = Type.GetType(classVal.AsString());
                    if (skillType != null)
                    {
                        BaseSkill newSkill = (BaseSkill)Activator.CreateInstance(skillType);
                        return sm.EquipActive(newSkill);
                    }
                }
                break;
            }

            case "new_passive":
            {
                if (choice.TryGetValue("skill_class", out var classVal))
                {
                    Type skillType = Type.GetType(classVal.AsString());
                    if (skillType != null)
                    {
                        BaseSkill newSkill = (BaseSkill)Activator.CreateInstance(skillType);
                        return sm.EquipPassive(newSkill);
                    }
                }
                break;
            }

            case "upgrade_active":
            case "upgrade_passive":
            {
                if (choice.TryGetValue("skill_ref", out var skillVal))
                {
                    BaseSkill skill = skillVal.As<BaseSkill>();
                    if (skill != null && GodotObject.IsInstanceValid(skill))
                    {
                        skill.Upgrade();
                        return true;
                    }
                }
                break;
            }

            case "heal_fallback":
            {
                if (player.HasMethod("Heal"))
                {
                    Variant statsVar = player.Get("stats");
                    if (statsVar.AsGodotObject() is CellStats stats)
                    {
                        float maxHp = stats.GetStat("max_health");
                        player.Call("Heal", maxHp * 0.35f);
                        return true;
                    }
                }
                break;
            }
        }

        return false;
    }
}
