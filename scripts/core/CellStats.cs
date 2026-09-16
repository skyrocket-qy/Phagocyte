using Godot;
using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Centralized 16-Universal-Stat Manager for Cells in Phagocyte.
/// Excludes any skill-specific stats to maintain complete modularity.
/// </summary>
public partial class CellStats : Node
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

    // Survival & Defense Stats (5)
    public Stat MaxHealth { get; private set; } = new(100.0f);
    public Stat HealthRegen { get; private set; } = new(0.0f);
    public Stat Armor { get; private set; } = new(0.0f);
    public Stat MoveSpeed { get; private set; } = new(230.0f);
    public Stat Revival { get; private set; } = new(0.0f);

    // Utility & Meta Stats (4)
    public Stat Magnet { get; private set; } = new(150.0f);
    public Stat Growth { get; private set; } = new(1.0f);
    public Stat Luck { get; private set; } = new(1.0f);
    public Stat Curse { get; private set; } = new(1.0f);

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

            ["max_health"] = MaxHealth,
            ["health_regen"] = HealthRegen,
            ["armor"] = Armor,
            ["move_speed"] = MoveSpeed,
            ["revival"] = Revival,

            ["magnet"] = Magnet,
            ["growth"] = Growth,
            ["luck"] = Luck,
            ["curse"] = Curse
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
            return 1.0f;
        }

        float val = s.GetValue();

        // Universal clamps
        if (statName == "cooldown_reduction")
            return Mathf.Clamp(val, 0.0f, 0.75f); // Cap CDR at 75%
        else if (statName == "crit_chance")
            return Mathf.Clamp(val, 0.0f, 1.0f); // Crit chance capped at 100%
        else if (statName is "move_speed" or "max_health" or "magnet")
            return Mathf.Max(0.0f, val);

        return val;
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
}
