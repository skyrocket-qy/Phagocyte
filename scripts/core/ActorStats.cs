using Godot;
using System.Collections.Generic;

namespace Game.Core;

/// <summary>
/// Centralized 18-Universal-Stat Manager for Cells in Game.
/// Thin Node adapter over <see cref="StatBlock"/> (full schema): owns the
/// SceneTree membership and the <c>StatChanged</c> signal, delegates all
/// math. Formula: Final = (Base + Flat) * (1 + Pct).
/// </summary>
public partial class ActorStats : Node, IStatHost
{
    [Signal]
    public delegate void StatChangedEventHandler(string statName, float newVal);

    private readonly StatBlock _block = new();

    // Full-schema convenience surface (prefer GetStat(string) for profiled blocks).
    public Stat Might => _block.GetStatObj("might")!;
    public Stat Area => _block.GetStatObj("area")!;
    public Stat CooldownReduction => _block.GetStatObj("cooldown_reduction")!;
    public Stat ProjectileSpeed => _block.GetStatObj("projectile_speed")!;
    public Stat Duration => _block.GetStatObj("duration")!;
    public Stat Amount => _block.GetStatObj("amount")!;
    public Stat Pierce => _block.GetStatObj("pierce")!;
    public Stat Knockback => _block.GetStatObj("knockback")!;
    public Stat CritChance => _block.GetStatObj("crit_chance")!;
    public Stat CritDamage => _block.GetStatObj("crit_damage")!;
    public Stat AilmentDamage => _block.GetStatObj("ailment_damage")!;

    // Defense & Survival Stats (7)
    public Stat MaxHealth => _block.GetStatObj("max_health")!;
    public Stat HealthRegen => _block.GetStatObj("health_regen")!;
    public Stat Armor => _block.GetStatObj("armor")!;
    public Stat MoveSpeed => _block.GetStatObj("move_speed")!;
    public Stat Evasion => _block.GetStatObj("evasion")!;
    public Stat Block => _block.GetStatObj("block")!;
    public Stat LifeSteal => _block.GetStatObj("life_steal")!;

    // Utility & Meta Stats (1)
    public Stat Magnet => _block.GetStatObj("magnet")!;

    /// <summary>
    /// Read-only access to the raw pool (e.g. tests). Mutate only through the
    /// ActorStats methods: scaled/rules recompute rewrites FlatBonus, so direct
    /// writes here are overwritten on the next mutation.
    /// </summary>
    public Stat? GetStatObj(string statName)
    {
        return _block.GetStatObj(statName);
    }

    public bool HasStat(string statName) => _block.HasStat(statName);

    public float GetStat(string statName)
    {
        return _block.GetStat(statName);
    }

    public void AddModifier(string statName, float flat, float pct)
    {
        _block.AddModifier(statName, flat, pct);
        EmitChanged(statName);
    }

    public void RemoveModifier(string statName, float flat, float pct)
    {
        _block.RemoveModifier(statName, flat, pct);
        EmitChanged(statName);
    }

    public void SetBase(string statName, float val)
    {
        _block.SetBase(statName, val);
        EmitChanged(statName);
    }

    public void AddScaledModifier(string target, float flat, float pct, string source, float scalePer)
    {
        _block.AddScaledModifier(target, flat, pct, source, scalePer);
        EmitChanged(target);
    }

    public bool RemoveScaledModifier(string target, float flat, float pct, string source, float scalePer)
    {
        if (!_block.RemoveScaledModifier(target, flat, pct, source, scalePer))
            return false;
        EmitChanged(target);
        return true;
    }

    public void AddStatRule(string target, string source, float ratio)
    {
        _block.AddStatRule(target, source, ratio);
        EmitChanged(target);
    }

    public void RemoveStatRule(string target, string source, float ratio)
    {
        if (!_block.RemoveStatRule(target, source, ratio))
            return;
        EmitChanged(target);
    }

    public void ClearScaled()
    {
        _block.ClearScaled();
        EmitChanged("");
    }

    /// <summary>
    /// Recomputes scaled + rule shares to a fixed point (no-cycle contract:
    /// warns past <see cref="StatBlock.MaxScalePasses"/> passes), rewrites the pools,
    /// then emits <c>StatChanged</c> for every stat whose final value moved.
    /// </summary>
    private void EmitChanged(string hint)
    {
        var before = new Dictionary<string, float>();
        foreach (string key in _block.Keys)
            before[key] = GetStat(key);

        _block.RecomputeScaled();

        var emitted = new HashSet<string>();
        foreach (string key in _block.Keys)
        {
            if (GetStat(key) != before[key])
            {
                EmitSignal(SignalName.StatChanged, key, GetStat(key));
                emitted.Add(key);
            }
        }
        if (!string.IsNullOrEmpty(hint) && !emitted.Contains(hint) && _block.HasStat(hint))
            EmitSignal(SignalName.StatChanged, hint, GetStat(hint));
    }

    /// <summary>
    /// Rolls for critical hit
    /// </summary>
    public bool RollCritical()
    {
        return _block.RollCritical();
    }

    /// <summary>
    /// Rolls for fluid deformation evasion
    /// </summary>
    public bool RollEvasion()
    {
        return _block.RollEvasion();
    }

    /// <summary>
    /// Rolls for glycocalyx block
    /// </summary>
    public bool RollBlock()
    {
        return _block.RollBlock();
    }

    /// <summary>
    /// Rolls for receptor life steal on hit
    /// </summary>
    public bool RollLifeSteal()
    {
        return _block.RollLifeSteal();
    }

    /// <summary>
    /// Computes effective ailment damage (DoT) based on Might and AilmentDamage multipliers
    /// </summary>
    public float CalculateAilmentDamage(float baseDps)
    {
        return _block.CalculateAilmentDamage(baseDps);
    }

    /// <summary>
    /// Computes effective ailment duration based on the universal Duration stat
    /// </summary>
    public float CalculateAilmentDuration(float baseDuration)
    {
        return _block.CalculateAilmentDuration(baseDuration);
    }
}
