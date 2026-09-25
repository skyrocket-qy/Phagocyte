using Godot;
using System.Collections.Generic;
using Phagocyte.Core;
using Phagocyte.Player;

namespace Phagocyte.UI;

/// <summary>
/// Menu build preview (class → chamber → tree): assembles the exact final
/// stats a deploy would produce, so the loadout and tree pages can show
/// live totals while crafting. Rules are never duplicated here — class bases
/// run through the real <c>BaseCell.ApplyClassBaseStats</c>, chamber entries
/// through a real <see cref="OrganelleChamber"/> (same skip + energy rules
/// as <c>Main.ApplyChamberLoadout</c>), tree entries through
/// <see cref="PassiveTreeManager.ApplyModifier"/>. Nothing enters the tree;
/// all scratch nodes are freed before return.
/// </summary>
public static class BuildStatsPreview
{
    public static readonly string[] CombatKeys =
    {
        "might", "area", "cooldown_reduction", "projectile_speed", "duration",
        "amount", "pierce", "knockback", "crit_chance", "crit_damage", "ailment_damage"
    };

    public static readonly string[] DefenseKeys =
    {
        "max_health", "health_regen", "armor", "move_speed", "evasion", "block", "life_steal"
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
        BaseCell? cell = null;
        try
        {
            cell = GameManager.GetCellScene(classId).Instantiate<BaseCell>();
            // Same identity pass as BaseCell._Ready (export defaults, then
            // per-class identity, then bases) — SetupCellIdentity is
            // field-only in every class, safe off-tree.
            cell.SetupCellIdentity();
            var stats = new CellStats { Name = "CellStats" };
            cell.Stats = stats;
            stats.SetBase("max_health", cell.MaxHealth);
            stats.SetBase("move_speed", cell.BaseSpeed);
            cell.ApplyClassBaseStats();

            host = new CharacterBody2D { Name = "BuildPreviewHost" };
            host.AddChild(stats);
            var chamber = new OrganelleChamber { Name = "OrganelleChamber" };
            host.AddChild(chamber);
            chamber.Setup(host);

            // Chamber entries through a real chamber: same locked skip as
            // Main.ApplyChamberLoadout, same energy rules via Equip itself.
            // Slot position is stat-neutral, so profile indices are reused.
            string[] slots = LoadoutManager.GetActiveSlots(classId);
            for (int i = 0; i < slots.Length && i < OrganelleChamber.MaxSlots; i++)
            {
                string id = slots[i];
                if (string.IsNullOrEmpty(id) || !OrganelleUnlockManager.IsUnlocked(id))
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
                    var (flat, pct) = PassiveTreeManager.SplitModifier(modifier);
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
            "cooldown_reduction" or "crit_chance" or "evasion" or "block" or "life_steal" => $"{value * 100.0f:F1}%",
            "might" or "area" or "projectile_speed" or "duration" or "knockback" or "crit_damage" or "ailment_damage" => $"{value * 100.0f:F0}%",
            _ => $"{value:F2}"
        };
    }
}
