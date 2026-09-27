using Godot;
using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Centralized 18-Universal-Stat Manager for Cells in Phagocyte.
/// Excludes any skill-specific stats to maintain complete modularity.
/// Formula: Final = (Base + Flat) * (1 + Pct)
/// Flat/Pct pool three sources: direct modifiers, scaled modifiers
/// (value * sourceStat / scalePer, recomputed live) and built-in
/// cross-stat rules (ratio * sourceStat, flat channel).
/// </summary>
public partial class CellStats : Node, IStatHost
{
    [Signal]
    public delegate void StatChangedEventHandler(string statName, float newVal);

    /// <summary>One scaled modifier contribution (Vistrace ScalingStat/ScalePer).</summary>
    public readonly record struct ScaledRecord(string Target, float Flat, float Pct, string Source, float ScalePer);

    /// <summary>One built-in cross-stat rule: flat += Ratio * source (Vistrace layer rules).</summary>
    public readonly record struct StatRule(string Target, string Source, float Ratio);

    /// <summary>Fixed-point cap for the scaled/rules recompute (no-cycle contract).</summary>
    public const int MaxScalePasses = 8;

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

    // Unscaled mirrors of Stat.FlatBonus/PercentBonus (scaled + rule shares
    // are rewritten by RecomputeScaled, so the direct sums live here).
    private readonly Dictionary<string, float> _directFlat = new();
    private readonly Dictionary<string, float> _directPct = new();
    private readonly List<ScaledRecord> _scaled = new();
    private readonly List<StatRule> _rules = new();

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

    /// <summary>
    /// Read-only access to the raw pool (e.g. tests). Mutate only through the
    /// CellStats methods: scaled/rules recompute rewrites FlatBonus, so direct
    /// writes here are overwritten on the next mutation.
    /// </summary>
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
        if (!_statsMap.TryGetValue(statName, out _))
        {
            GD.PushWarning($"CellStats: Cannot add modifier to unknown stat '{statName}'.");
            return;
        }
        _directFlat[statName] = DirectFlat(statName) + flat;
        _directPct[statName] = DirectPct(statName) + pct;
        EmitChanged(statName);
    }

    public void RemoveModifier(string statName, float flat, float pct)
    {
        if (!_statsMap.TryGetValue(statName, out _))
        {
            GD.PushWarning($"CellStats: Cannot remove modifier from unknown stat '{statName}'.");
            return;
        }
        _directFlat[statName] = DirectFlat(statName) - flat;
        _directPct[statName] = DirectPct(statName) - pct;
        EmitChanged(statName);
    }

    public void SetBase(string statName, float val)
    {
        if (_statsMap.TryGetValue(statName, out var s))
        {
            s.SetBase(val);
            EmitChanged(statName);
        }
    }

    /// <summary>
    /// Registers a scaled modifier: contributes flat/pct multiplied by
    /// (sourceStat / scalePer), recomputed live whenever any stat changes.
    /// Unknown target/source is warned and ignored; scalePer &lt;= 0 falls
    /// back to 1.0 (Vistrace guard).
    /// </summary>
    public void AddScaledModifier(string target, float flat, float pct, string source, float scalePer)
    {
        if (!_statsMap.ContainsKey(target))
        {
            GD.PushWarning($"CellStats: Cannot add scaled modifier to unknown stat '{target}'.");
            return;
        }
        if (string.IsNullOrEmpty(source) || !_statsMap.ContainsKey(source))
        {
            GD.PushWarning($"CellStats: Scaled modifier on '{target}' references unknown source '{source}'.");
            return;
        }
        if (scalePer <= 0.0f)
        {
            GD.PushWarning($"CellStats: scalePer {scalePer} on '{target}' must be positive; using 1.0.");
            scalePer = 1.0f;
        }
        _scaled.Add(new ScaledRecord(target, flat, pct, source, scalePer));
        EmitChanged(target);
    }

    /// <summary>Removes one matching scaled record (exact match); silent when absent.</summary>
    public void RemoveScaledModifier(string target, float flat, float pct, string source, float scalePer)
    {
        for (int i = 0; i < _scaled.Count; i++)
        {
            var r = _scaled[i];
            if (r.Target == target && r.Flat == flat && r.Pct == pct && r.Source == source && r.ScalePer == scalePer)
            {
                _scaled.RemoveAt(i);
                EmitChanged(target);
                return;
            }
        }
    }

    /// <summary>
    /// Registers a built-in cross-stat rule: flat += ratio * sourceStat,
    /// recomputed live (Vistrace layer rules, flat channel only).
    /// </summary>
    public void AddStatRule(string target, string source, float ratio)
    {
        if (!_statsMap.ContainsKey(target) || string.IsNullOrEmpty(source) || !_statsMap.ContainsKey(source))
        {
            GD.PushWarning($"CellStats: Stat rule '{target}' <- '{source}' references an unknown stat.");
            return;
        }
        _rules.Add(new StatRule(target, source, ratio));
        EmitChanged(target);
    }

    /// <summary>Removes one matching rule (exact match); silent when absent.</summary>
    public void RemoveStatRule(string target, string source, float ratio)
    {
        for (int i = 0; i < _rules.Count; i++)
        {
            var r = _rules[i];
            if (r.Target == target && r.Source == source && r.Ratio == ratio)
            {
                _rules.RemoveAt(i);
                EmitChanged(target);
                return;
            }
        }
    }

    /// <summary>Test/reset hook: drops every scaled record and rule.</summary>
    public void ClearScaled()
    {
        _scaled.Clear();
        _rules.Clear();
        EmitChanged("");
    }

    private float DirectFlat(string statName) => _directFlat.TryGetValue(statName, out float v) ? v : 0.0f;
    private float DirectPct(string statName) => _directPct.TryGetValue(statName, out float v) ? v : 0.0f;

    /// <summary>
    /// Recomputes scaled + rule shares to a fixed point (no-cycle contract:
    /// warns past <see cref="MaxScalePasses"/> passes), rewrites the pools,
    /// then emits <c>StatChanged</c> for every stat whose final value moved.
    /// </summary>
    private void EmitChanged(string hint)
    {
        var before = new Dictionary<string, float>();
        foreach (string key in _statsMap.Keys)
            before[key] = GetStat(key);

        RecomputeScaled();

        var emitted = new HashSet<string>();
        foreach (string key in _statsMap.Keys)
        {
            if (GetStat(key) != before[key])
            {
                EmitSignal(SignalName.StatChanged, key, GetStat(key));
                emitted.Add(key);
            }
        }
        if (!string.IsNullOrEmpty(hint) && !emitted.Contains(hint) && _statsMap.ContainsKey(hint))
            EmitSignal(SignalName.StatChanged, hint, GetStat(hint));
    }

    private void RecomputeScaled()
    {
        for (int pass = 0; pass < MaxScalePasses; pass++)
        {
            var snapshot = new Dictionary<string, float>();
            foreach (string key in _statsMap.Keys)
                snapshot[key] = GetStat(key);

            var addFlat = new Dictionary<string, float>();
            var addPct = new Dictionary<string, float>();
            foreach (var r in _scaled)
            {
                float factor = snapshot[r.Source] / r.ScalePer;
                addFlat[r.Target] = addFlat.GetValueOrDefault(r.Target) + r.Flat * factor;
                addPct[r.Target] = addPct.GetValueOrDefault(r.Target) + r.Pct * factor;
            }
            foreach (var rule in _rules)
                addFlat[rule.Target] = addFlat.GetValueOrDefault(rule.Target) + rule.Ratio * snapshot[rule.Source];

            float maxDelta = 0.0f;
            foreach (string key in _statsMap.Keys)
            {
                var s = _statsMap[key];
                float flat = DirectFlat(key) + addFlat.GetValueOrDefault(key);
                float pct = DirectPct(key) + addPct.GetValueOrDefault(key);
                maxDelta = Mathf.Max(maxDelta, Mathf.Abs(s.FlatBonus - flat));
                maxDelta = Mathf.Max(maxDelta, Mathf.Abs(s.PercentBonus - pct));
                s.FlatBonus = flat;
                s.PercentBonus = pct;
            }
            if (maxDelta <= 1e-6f)
                return;
        }
        GD.PushWarning("[CellStats] Scaling did not converge: check for source cycles.");
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
