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
    // All catalog active weapon classes (17). Display metadata comes from the
    // single source of truth (GameManager.SkillCatalog); only the runtime class
    // used by the reflection factory lives here.
    public static Array<Dictionary> ActiveCatalog = new Array<Dictionary>()
    {
        MakeEntry(SkillIds.PhagocyticGrasp, typeof(PhagocyticGraspSkill)),
        MakeEntry(SkillIds.RosTorrent, typeof(RosTorrentSkill)),
        MakeEntry(SkillIds.PerforinLance, typeof(PerforinLanceSkill)),
        MakeEntry(SkillIds.ComplementCascade, typeof(ComplementCascadeSkill)),
        MakeEntry(SkillIds.AntibodySalvo, typeof(AntibodySalvoSkill)),
        MakeEntry(SkillIds.PseudopodLunge, typeof(PseudopodLungeSkill)),
        MakeEntry(SkillIds.NitricOxideHalo, typeof(NitricOxideHaloSkill)),
        MakeEntry(SkillIds.NucleaseBlades, typeof(NucleaseBladesSkill)),
        MakeEntry(SkillIds.GranzymeDetonation, typeof(GranzymeDetonationSkill)),
        MakeEntry(SkillIds.InterferonWave, typeof(InterferonWaveSkill)),
        MakeEntry(SkillIds.LysozymeRicochet, typeof(LysozymeRicochetSkill)),
        MakeEntry(SkillIds.PhagolysosomeVent, typeof(PhagolysosomeVentSkill)),
        MakeEntry(SkillIds.ProInflammatoryArc, typeof(ProInflammatoryArcSkill)),
        MakeEntry(SkillIds.ExosomeSingularity, typeof(ExosomeSingularitySkill)),
        MakeEntry(SkillIds.DefensinBarbs, typeof(DefensinBarbsSkill)),
        MakeEntry(SkillIds.MhcTracerBeam, typeof(MhcTracerBeamSkill)),
        MakeEntry(SkillIds.HistamineSurge, typeof(HistamineSurgeSkill))
    };

    // All catalog passive trait classes (13)
    public static Array<Dictionary> PassiveCatalog = new Array<Dictionary>()
    {
        MakeEntry(SkillIds.PassiveActin, typeof(PassiveActinPolymerization)),
        MakeEntry(SkillIds.PassiveLysosome, typeof(PassiveLysosomePriming)),
        MakeEntry(SkillIds.PassiveMitochondria, typeof(PassiveMitochondrialOverclock)),
        MakeEntry(SkillIds.PassiveOpsonin, typeof(PassiveOpsoninAffinity)),
        MakeEntry(SkillIds.PassiveChemokine, typeof(PassiveChemokineReceptors)),
        MakeEntry(SkillIds.PassiveBilayer, typeof(PassiveBilayerHardening)),
        MakeEntry(SkillIds.PassiveAutophagy, typeof(PassiveAutophagicRecycle)),
        MakeEntry(SkillIds.PassiveGlycolysis, typeof(PassiveAerobicGlycolysis)),
        MakeEntry(SkillIds.PassiveKinesin, typeof(PassiveKinesinTransit)),
        MakeEntry(SkillIds.PassiveLongevity, typeof(PassiveCytokineLongevity)),
        MakeEntry(SkillIds.PassiveVdj, typeof(PassiveVdjDiversity)),
        MakeEntry(SkillIds.PassiveEndotoxin, typeof(PassiveEndotoxinBarrier)),
        MakeEntry(SkillIds.PassiveHematopoietic, typeof(PassiveHematopoieticReserve))
    };

    /// <summary>
    /// Builds one catalog row from GameManager.SkillCatalog (names/descriptions/
    /// icons/class gating) plus the runtime class used by the reflection factory.
    /// </summary>
    private static Dictionary MakeEntry(string id, Type skillType)
    {
        var info = (Dictionary)GameManager.SkillCatalog[id];
        return new Dictionary
        {
            { "id", id },
            { "name", info["name_key"] },
            { "desc", info["desc_key"] },
            { "icon", info["icon"] },
            { "image_path", AssetPaths.SkillIcon(id) },
            { "class_type", skillType.AssemblyQualifiedName ?? "" },
            { "class_id", info["class_id"] }
        };
    }

    /// <summary>
    /// Superweapon catalyst pairs (docs/skill.md §5): a maxed active + its
    /// paired passive fuse into the corresponding epigenetic evolution.
    /// </summary>
    public const int CatalystActiveLevel = 5;

    public static readonly System.Collections.Generic.Dictionary<string, string> CatalystPairs = new()
    {
        { SkillIds.PerforinLance, SkillIds.PassiveLysosome },
        { SkillIds.ComplementCascade, SkillIds.PassiveActin },
        { SkillIds.AntibodySalvo, SkillIds.PassiveOpsonin },
        { SkillIds.RosTorrent, SkillIds.PassiveMitochondria },
        { SkillIds.PseudopodLunge, SkillIds.PassiveChemokine }
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
                        { "image_path", AssetPaths.SkillIcon(skill.SkillId) },
                        { "level", skill.Level + 1 },
                        { "badge", "BADGE_UPGRADE" },
                        { "desc", !string.IsNullOrEmpty(skill.DescKey) ? skill.DescKey : "UPGRADE_TO_LV" },
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
                        { "image_path", AssetPaths.SkillIcon(skill.SkillId) },
                        { "level", skill.Level + 1 },
                        { "badge", "BADGE_UPGRADE" },
                        { "desc", !string.IsNullOrEmpty(skill.DescKey) ? skill.DescKey : "UPGRADE_TO_LV" },
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
                        { "image_path", item.TryGetValue("image_path", out var ipVal) ? ipVal.AsString() : AssetPaths.SkillIcon(item["id"].AsString()) },
                        { "level", 1 },
                        { "badge", "BADGE_NEW_ACTIVE" },
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
                if (!equippedPassiveIds.Contains(id) && !treeOwned.Contains(id))
                {
                    var newPassiveCandidate = new Dictionary
                    {
                        { "type", "new_passive" },
                        { "id", item["id"] },
                        { "name", item["name"] },
                        { "icon", item["icon"] },
                        { "image_path", item.TryGetValue("image_path", out var ipVal2) ? ipVal2.AsString() : AssetPaths.SkillIcon(item["id"].AsString()) },
                        { "level", 1 },
                        { "badge", "BADGE_NEW_PASSIVE" },
                        { "desc", item["desc"] },
                        { "skill_class", item["class_type"] }
                    };
                    ApplyCatalystFlag(player, newPassiveCandidate, id);
                    candidates.Add(newPassiveCandidate);
                }
            }
        }

        // Draft offers weapons and passives only: organelles join the run
        // through enemy drops and the pre-run loadout, never through cards.

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
                { "name", "HEAL_FALLBACK_NAME" },
                { "icon", "💚" },
                { "image_path", AssetPaths.UiIcon("badge_atp") },
                { "level", 0 },
                { "badge", "BADGE_HEAL" },
                { "desc", "HEAL_FALLBACK_DESC" }
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
                    Type? skillType = Type.GetType(classVal.AsString());
                    if (skillType != null)
                    {
                        BaseSkill? newSkill = (BaseSkill?)Activator.CreateInstance(skillType);
                        if (newSkill != null)
                            return sm.EquipActive(newSkill);
                    }
                }
                break;
            }

            case "new_passive":
            {
                if (choice.TryGetValue("skill_class", out var classVal))
                {
                    Type? skillType = Type.GetType(classVal.AsString());
                    if (skillType != null)
                    {
                        BaseSkill? newSkill = (BaseSkill?)Activator.CreateInstance(skillType);
                        if (newSkill != null)
                            return sm.EquipPassive(newSkill);
                    }
                }
                break;
            }

            case "new_organelle":
            {
                // Not offered by drafts anymore (weapons/passives only); kept
                // as the acquisition engine beneath the in-modal swap step,
                // which acquires into the backpack then best-effort equips.
                // Returns whether anything was acquired.
                string organelleId = choice.TryGetValue("id", out var organelleIdVal) ? organelleIdVal.AsString() : "";
                if (string.IsNullOrEmpty(organelleId) || !OrganelleUnlockManager.IsUnlocked(organelleId))
                    break;
                var organelleChamber = player.GetNodeOrNull<OrganelleChamber>("OrganelleChamber");
                if (organelleChamber == null || organelleChamber.Owns(organelleId))
                    break;
                if (!organelleChamber.AddToBackpack(organelleId))
                    break;
                for (int slot = 0; slot < OrganelleChamber.MaxSlots; slot++)
                {
                    if (!string.IsNullOrEmpty(organelleChamber.GetSlot(slot)))
                        continue;
                    if (organelleChamber.CanEquip(organelleId, slot, out _))
                    {
                        organelleChamber.Equip(organelleId, slot);
                        break;
                    }
                }
                return true;
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
