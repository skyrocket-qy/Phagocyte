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
    // All catalog active weapon classes
    public static Array<Dictionary> ActiveCatalog = new Array<Dictionary>()
    {
        new Dictionary { { "id", "ros_torrent" }, { "name", "SKILL_ROS_NAME" }, { "desc", "SKILL_ROS_DESC" }, { "icon", "💨" }, { "class_type", typeof(RosTorrentSkill).AssemblyQualifiedName }, { "class_id", "macrophage" } },
        new Dictionary { { "id", "perforin_lance" }, { "name", "SKILL_PERFORIN_NAME" }, { "desc", "SKILL_PERFORIN_DESC" }, { "icon", "🗡️" }, { "class_type", typeof(PerforinLanceSkill).AssemblyQualifiedName }, { "class_id", "ctl" } },
        new Dictionary { { "id", "complement_cascade" }, { "name", "SKILL_COMPLEMENT_NAME" }, { "desc", "SKILL_COMPLEMENT_DESC" }, { "icon", "💥" }, { "class_type", typeof(ComplementCascadeSkill).AssemblyQualifiedName }, { "class_id", "neutrophil" } },
        new Dictionary { { "id", "antibody_salvo" }, { "name", "SKILL_ANTIBODY_NAME" }, { "desc", "SKILL_ANTIBODY_DESC" }, { "icon", "🏹" }, { "class_type", typeof(AntibodySalvoSkill).AssemblyQualifiedName }, { "class_id", "b_cell" } },
        new Dictionary { { "id", "pseudopod_lunge" }, { "name", "SKILL_LUNGE_NAME" }, { "desc", "SKILL_LUNGE_DESC" }, { "icon", "🥊" }, { "class_type", typeof(PseudopodLungeSkill).AssemblyQualifiedName }, { "class_id", "dendritic" } }
    };

    // All catalog passive trait classes
    public static Array<Dictionary> PassiveCatalog = new Array<Dictionary>()
    {
        new Dictionary { { "id", "passive_actin" }, { "name", "SKILL_ACTIN_NAME" }, { "desc", "SKILL_ACTIN_DESC" }, { "icon", "🧬" }, { "class_type", typeof(PassiveActinPolymerization).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_lysosome" }, { "name", "SKILL_LYSOSOME_NAME" }, { "desc", "SKILL_LYSOSOME_DESC" }, { "icon", "🧪" }, { "class_type", typeof(PassiveLysosomePriming).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_mitochondria" }, { "name", "SKILL_MITOCHONDRIA_NAME" }, { "desc", "SKILL_MITOCHONDRIA_DESC" }, { "icon", "⚡" }, { "class_type", typeof(PassiveMitochondrialOverclock).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_opsonin" }, { "name", "SKILL_OPSONIN_NAME" }, { "desc", "SKILL_OPSONIN_DESC" }, { "icon", "🎯" }, { "class_type", typeof(PassiveOpsoninAffinity).AssemblyQualifiedName } },
        new Dictionary { { "id", "passive_chemokine" }, { "name", "SKILL_CHEMOKINE_NAME" }, { "desc", "SKILL_CHEMOKINE_DESC" }, { "icon", "🧲" }, { "class_type", typeof(PassiveChemokineReceptors).AssemblyQualifiedName } }
    };

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
                    candidates.Add(new Dictionary
                    {
                        { "type", "upgrade_passive" },
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
            foreach (var item in PassiveCatalog)
            {
                string id = item["id"].AsString();
                if (!equippedPassiveIds.Contains(id))
                {
                    candidates.Add(new Dictionary
                    {
                        { "type", "new_passive" },
                        { "id", item["id"] },
                        { "name", item["name"] },
                        { "icon", item["icon"] },
                        { "level", 1 },
                        { "badge", "NEW PASSIVE" },
                        { "desc", item["desc"] },
                        { "skill_class", item["class_type"] }
                    });
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
