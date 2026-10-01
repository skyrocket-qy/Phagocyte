using Godot;
using System.Collections.Generic;
using Game.Core;
using Game.Player;

namespace Game.UI;

/// <summary>Calculates aggregate player build stats across class, equipment, and passive tree.</summary>
public static class BuildStatsPreview
{
    public static readonly string[] CombatKeys =
    {
        "might", "area", "cooldown_reduction", "projectile_speed", "duration",
        "amount", "pierce", "crit_chance", "crit_damage", "armor_penetration", "ailment_damage"
    };

    public static readonly string[] DefenseKeys =
    {
        "max_health", "health_regen", "armor", "move_speed", "evasion", "block", "life_steal",
        "stagger", "recoup"
    };

    public static readonly string[] UtilityKeys = { "magnet" };

    public static IEnumerable<string> AllKeys
    {
        get
        {
            foreach (string k in CombatKeys) yield return k;
            foreach (string k in DefenseKeys) yield return k;
            foreach (string k in UtilityKeys) yield return k;
        }
    }

    /// <summary>Final stat values for <paramref name="classId"/> with its active loadout + tree profile.</summary>
    public static Dictionary<string, float> PreviewStats(string classId)
    {
        var result = new Dictionary<string, float>();
        if (string.IsNullOrEmpty(classId))
            return result;

        CharacterBody2D? host = null;
        PlayerActor? cell = null;
        try
        {
            cell = GameManager.GetPlayerScene(classId).Instantiate<PlayerActor>();
            // Same identity pass as PlayerActor._Ready (export defaults, then
            // per-class identity, then bases) — SetupCellIdentity is
            // field-only in every class, safe off-tree.
            cell.SetupCellIdentity();
            var stats = new ActorStats { Name = "ActorStats" };
            cell.Stats = stats;
            stats.SetBase("max_health", cell.MaxHealth);
            stats.SetBase("move_speed", cell.BaseSpeed);
            cell.ApplyClassBaseStats();

            host = new CharacterBody2D { Name = "BuildPreviewHost" };
            host.AddChild(stats);
            var chamber = new EquipmentChamber { Name = "EquipmentChamber" };
            host.AddChild(chamber);
            chamber.Setup(host);

            // Chamber entries through a real chamber: same locked skip as
            // GameRoot.ApplyChamberLoadout, same energy rules via Equip itself.
            // Slot position is stat-neutral, so profile indices are reused.
            string[] slots = LoadoutManager.GetActiveSlots(classId);
            for (int i = 0; i < slots.Length && i < EquipmentChamber.MaxSlots; i++)
            {
                string id = slots[i];
                if (string.IsNullOrEmpty(id) || !EquipmentUnlockManager.IsUnlocked(id))
                    continue;
                if (!chamber.AddToBackpack(id))
                    continue;
                chamber.Equip(id, i);
            }

            var allocation = PassiveTreeManager.GetAllocation(classId);
            foreach (var node in PassiveTreeManager.Nodes)
            {
                if (!allocation.Contains(node.Id))
                    continue;
                foreach (var modifier in node.Modifiers)
                {
                    var (flat, pct, scalingStat, scalePer, hasScaling) = PassiveTreeManager.SplitScaledModifier(modifier);
                    if (hasScaling)
                        stats.AddScaledModifier(modifier.Stat, flat, pct, scalingStat, scalePer);
                    else
                        stats.AddModifier(modifier.Stat, flat, pct);
                }
            }

            foreach (string key in AllKeys)
                result[key] = stats.GetStat(key);
        }
        finally
        {
            host?.Free();
            cell?.Free();
        }
        return result;
    }

    /// <summary>Shared value formatter (also used by the in-run C overlay).</summary>
    public static string FormatValue(string statKey, float value)
    {
        return statKey switch
        {
            "max_health" or "health_regen" or "move_speed" or "magnet" => $"{value:F1}",
            "amount" or "pierce" or "armor" => $"{value:F0}",
            "cooldown_reduction" or "crit_chance" or "evasion" or "block" or "life_steal" or "stagger" or "recoup" or "armor_penetration" => $"{value * 100.0f:F1}%",
            "might" or "area" or "projectile_speed" or "duration" or "crit_damage" or "ailment_damage" => $"{value * 100.0f:F0}%",
            _ => $"{value:F2}"
        };
    }
}
