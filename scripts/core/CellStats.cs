using Godot;
using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Centralized 18-Universal-Stat Manager for Cells in Phagocyte.
/// Excludes any skill-specific stats to maintain complete modularity.
/// Formula: Final = (Base + Flat) * (1 + Pct)
/// </summary>
public partial class CellStats : Node, IStatHost
{
    [Signal]
    public delegate void StatChangedEventHandler(string statName, float newVal);

    // Combat Stats (10)
    public Stat Might { get; private set; } = new(1.0f);
    public Stat Area { get; private set; } = new(1.0f);
    public Stat CooldownReduction { get; private set; } = new(0.0f);
    public Stat ProjectileSpeed { get; private set; } = new(1.0f);
    public Stat Duration { get; private set; } = new(1.0f);
    public Stat Amount { get; private set; } = new(0.0f);
    public Stat Pierce { get; private set; } = new(0.0f);
    public Stat Knockback { get; private set; } = new(1.0f);
    public Stat CritChance { get; private set; } = new(0.05f);
    public Stat CritDamage { get; private set; } = new(2.0f);
    public Stat AilmentDamage { get; private set; } = new(1.0f);

    // Defense & Survival Stats (7)
    public Stat MaxHealth { get; private set; } = new(100.0f);
    public Stat HealthRegen { get; private set; } = new(0.0f);
    public Stat Armor { get; private set; } = new(0.0f);
    public Stat MoveSpeed { get; private set; } = new(230.0f);
    public Stat Evasion { get; private set; } = new(0.0f);
    public Stat Block { get; private set; } = new(0.0f);
    public Stat LifeSteal { get; private set; } = new(0.0f);

    // Utility & Meta Stats (1)
    public Stat Magnet { get; private set; } = new(150.0f);

    private Dictionary<string, Stat> _statsMap = new();

    public CellStats()
    {
        RegisterStats();
    }

    public override void _Ready()
    {
        if (_statsMap.Count == 0)
            RegisterStats();
    }

    private void RegisterStats()
    {
        _statsMap = new Dictionary<string, Stat>
        {
            // Combat (10)
            ["might"] = Might,
            ["area"] = Area,
            ["cooldown_reduction"] = CooldownReduction,
            ["projectile_speed"] = ProjectileSpeed,
            ["duration"] = Duration,
            ["amount"] = Amount,
            ["pierce"] = Pierce,
            ["knockback"] = Knockback,
            ["crit_chance"] = CritChance,
            ["crit_damage"] = CritDamage,
            ["ailment_damage"] = AilmentDamage,

            // Defense (7)
            ["max_health"] = MaxHealth,
            ["health_regen"] = HealthRegen,
            ["armor"] = Armor,
            ["move_speed"] = MoveSpeed,
            ["evasion"] = Evasion,
            ["block"] = Block,
            ["life_steal"] = LifeSteal,

            // Utility (1)
            ["magnet"] = Magnet
        };
    }

    public Stat? GetStatObj(string statName)
    {
        return _statsMap.TryGetValue(statName, out var stat) ? stat : null;
    }

    public float GetStat(string statName)
    {
        if (!_statsMap.TryGetValue(statName, out var s))
        {
            GD.PushWarning($"CellStats: Stat '{statName}' not found.");
            return 0.0f;
        }

        float val = s.GetValue();

        // Universal constraints and caps defined in docs/stat.md
        return statName switch
        {
            "cooldown_reduction" => Mathf.Clamp(val, 0.0f, 0.75f), // Cap CDR at 75%
            "crit_chance" => Mathf.Clamp(val, 0.0f, 1.0f),         // Cap Crit Chance at 100%
            "evasion" => Mathf.Clamp(val, 0.0f, 0.60f),             // Cap Evasion at 60%
            "block" => Mathf.Clamp(val, 0.0f, 0.75f),               // Cap Block at 75%
            "life_steal" => Mathf.Clamp(val, 0.0f, 0.20f),          // Cap Life Steal at 20%
            "might" or "area" or "projectile_speed" or "duration" or "crit_damage" or "ailment_damage" => Mathf.Max(0.0f, val),
            "amount" or "pierce" => Mathf.Max(0.0f, val),
            "move_speed" or "max_health" or "magnet" or "armor" => Mathf.Max(0.0f, val),
            _ => val
        };
    }

    public void AddModifier(string statName, float flat, float pct)
    {
        if (_statsMap.TryGetValue(statName, out var s))
        {
            s.AddModifier(flat, pct);
            EmitSignal(SignalName.StatChanged, statName, GetStat(statName));
        }
        else
        {
            GD.PushWarning($"CellStats: Cannot add modifier to unknown stat '{statName}'.");
        }
    }

    public void RemoveModifier(string statName, float flat, float pct)
    {
        if (_statsMap.TryGetValue(statName, out var s))
        {
            s.RemoveModifier(flat, pct);
            EmitSignal(SignalName.StatChanged, statName, GetStat(statName));
        }
        else
        {
            GD.PushWarning($"CellStats: Cannot remove modifier from unknown stat '{statName}'.");
        }
    }

    public void SetBase(string statName, float val)
    {
        if (_statsMap.TryGetValue(statName, out var s))
        {
            s.SetBase(val);
            EmitSignal(SignalName.StatChanged, statName, GetStat(statName));
        }
    }

    /// <summary>
    /// Returns percentage damage reduction from armor: Armor / (Armor + 50)
    /// </summary>
    public float GetDamageReductionRatio()
    {
        float a = GetStat("armor");
        if (a <= 0.0f)
            return 0.0f;
        return a / (a + 50.0f);
    }

    /// <summary>
    /// Rolls for critical hit
    /// </summary>
    public bool RollCritical()
    {
        return GD.Randf() < GetStat("crit_chance");
    }

    /// <summary>
    /// Rolls for fluid deformation evasion (免傷)
    /// </summary>
    public bool RollEvasion()
    {
        float ev = GetStat("evasion");
        return ev > 0.0f && GD.Randf() < ev;
    }

    /// <summary>
    /// Rolls for glycocalyx block (格擋)
    /// </summary>
    public bool RollBlock()
    {
        float blk = GetStat("block");
        return blk > 0.0f && GD.Randf() < blk;
    }

    /// <summary>
    /// Rolls for receptor life steal on hit (吸血回復)
    /// </summary>
    public bool RollLifeSteal()
    {
        float ls = GetStat("life_steal");
        return ls > 0.0f && GD.Randf() < ls;
    }

    /// <summary>
    /// Computes effective ailment damage (DoT) based on Might and AilmentDamage multipliers
    /// </summary>
    public float CalculateAilmentDamage(float baseDps)
    {
        return Mathf.Max(0.0f, baseDps * GetStat("might") * GetStat("ailment_damage"));
    }

    /// <summary>
    /// Computes effective ailment duration based on the universal Duration stat
    /// </summary>
    public float CalculateAilmentDuration(float baseDuration)
    {
        return Mathf.Max(0.1f, baseDuration * GetStat("duration"));
    }
}
