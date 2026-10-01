using Godot;
using System.Collections.Generic;

namespace Game.Core;

/// <summary>
/// Host-agnostic stat profiles: the key subset a <see cref="StatBlock"/>
/// registers. Heroes take <see cref="Full"/>; enemies and hero/minion
/// summons take the smaller sets (reward/arsenal concepts like xp, score
/// and contact damage stay plain fields — they are not stats).
/// Static arrays, not JSON: profiles change at architectural pace.
/// </summary>
public static class StatProfiles
{
    /// <summary>All 20 universal stats (heroes, UI previews, balance probes).</summary>
    public static readonly string[] Full =
    {
        "might", "area", "cooldown_reduction", "projectile_speed", "duration",
        "amount", "pierce", "crit_chance", "crit_damage",
        "ailment_damage", "max_health", "health_regen", "armor", "move_speed",
        "evasion", "block", "life_steal", "magnet", "stagger", "recoup",
    };

    /// <summary>Enemy combat stats (heroes' build stats excluded).</summary>
    public static readonly string[] Enemy = { "max_health", "armor", "move_speed" };

    /// <summary>Summoned-minion stats (spawned children of any summoner).</summary>
    public static readonly string[] Minion = { "max_health", "move_speed" };
}

/// <summary>Stat container computing Final = (Base + Flat) * (1 + Pct) with live scaling.</summary>
public sealed class StatBlock : IStatHost
{
    public readonly record struct ScaledRecord(string Target, float Flat, float Pct, string Source, float ScalePer);
    public readonly record struct StatRule(string Target, string Source, float Ratio);

    /// <summary>Fixed-point cap for the scaled/rules recompute (no-cycle contract).</summary>
    public const int MaxScalePasses = 8;

    private static readonly Dictionary<string, float> CanonicalBases = new()
    {
        ["might"] = 1.0f,
        ["area"] = 1.0f,
        ["cooldown_reduction"] = 0.0f,
        ["projectile_speed"] = 1.0f,
        ["duration"] = 1.0f,
        ["amount"] = 0.0f,
        ["pierce"] = 0.0f,
        ["crit_chance"] = 0.05f,
        ["crit_damage"] = 2.0f,
        ["ailment_damage"] = 1.0f,
        ["max_health"] = 100.0f,
        ["health_regen"] = 0.0f,
        ["armor"] = 0.0f,
        ["move_speed"] = 230.0f,
        ["evasion"] = 0.0f,
        ["block"] = 0.0f,
        ["life_steal"] = 0.0f,
        ["magnet"] = 150.0f,
        ["stagger"] = 0.0f,
        ["recoup"] = 0.0f,
    };

    private readonly Dictionary<string, Stat> _statsMap = new();

    // Unscaled mirrors of Stat.FlatBonus/PercentBonus (scaled + rule shares
    // are rewritten by RecomputeScaled, so the direct sums live here).
    private readonly Dictionary<string, float> _directFlat = new();
    private readonly Dictionary<string, float> _directPct = new();
    private readonly List<ScaledRecord> _scaled = new();
    private readonly List<StatRule> _rules = new();

    /// <summary>Registers <paramref name="keys"/> (null = <see cref="StatProfiles.Full"/>).</summary>
    public StatBlock(string[]? keys = null)
    {
        foreach (string key in keys ?? StatProfiles.Full)
        {
            if (!CanonicalBases.TryGetValue(key, out float baseValue))
            {
                GD.PushWarning($"StatBlock: Unknown stat '{key}' in schema; skipped.");
                continue;
            }
            if (!_statsMap.ContainsKey(key))
                _statsMap[key] = new Stat(baseValue);
        }
    }

    public IReadOnlyCollection<string> Keys => _statsMap.Keys;

    /// <summary>Read-only access to the raw pool. Mutate only through the methods below.</summary>
    public Stat? GetStatObj(string statName)
    {
        return _statsMap.TryGetValue(statName, out var stat) ? stat : null;
    }

    public bool HasStat(string statName) => _statsMap.ContainsKey(statName);

    public float GetStat(string statName)
    {
        if (!_statsMap.TryGetValue(statName, out var s))
        {
            GD.PushWarning($"StatBlock: Stat '{statName}' not found.");
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
            "stagger" => Mathf.Clamp(val, 0.0f, 0.60f),               // Cap Stagger at 60%
            "recoup" => Mathf.Clamp(val, 0.0f, 0.30f),                // Cap Recoup at 30%
            "might" or "area" or "projectile_speed" or "duration" or "crit_damage" or "ailment_damage" => Mathf.Max(0.0f, val),
            "amount" or "pierce" => Mathf.Max(0.0f, val),
            "move_speed" or "max_health" or "magnet" or "armor" => Mathf.Max(0.0f, val),
            _ => val
        };
    }

    public void AddModifier(string statName, float flat, float pct)
    {
        if (!_statsMap.ContainsKey(statName))
        {
            GD.PushWarning($"StatBlock: Cannot add modifier to unknown stat '{statName}'.");
            return;
        }
        _directFlat[statName] = DirectFlat(statName) + flat;
        _directPct[statName] = DirectPct(statName) + pct;
    }

    public void RemoveModifier(string statName, float flat, float pct)
    {
        if (!_statsMap.ContainsKey(statName))
        {
            GD.PushWarning($"StatBlock: Cannot remove modifier from unknown stat '{statName}'.");
            return;
        }
        _directFlat[statName] = DirectFlat(statName) - flat;
        _directPct[statName] = DirectPct(statName) - pct;
    }

    public void SetBase(string statName, float val)
    {
        if (_statsMap.TryGetValue(statName, out var s))
            s.SetBase(val);
    }

    /// <summary>Registers a modifier scaled by (sourceStat / scalePer).</summary>
    public void AddScaledModifier(string target, float flat, float pct, string source, float scalePer)
    {
        if (!_statsMap.ContainsKey(target))
        {
            GD.PushWarning($"StatBlock: Cannot add scaled modifier to unknown stat '{target}'.");
            return;
        }
        if (string.IsNullOrEmpty(source) || !_statsMap.ContainsKey(source))
        {
            GD.PushWarning($"StatBlock: Scaled modifier on '{target}' references unknown source '{source}'.");
            return;
        }
        if (scalePer <= 0.0f)
        {
            GD.PushWarning($"StatBlock: scalePer {scalePer} on '{target}' must be positive; using 1.0.");
            scalePer = 1.0f;
        }
        _scaled.Add(new ScaledRecord(target, flat, pct, source, scalePer));
    }

    public bool RemoveScaledModifier(string target, float flat, float pct, string source, float scalePer)
    {
        for (int i = 0; i < _scaled.Count; i++)
        {
            var r = _scaled[i];
            if (r.Target == target && r.Flat == flat && r.Pct == pct && r.Source == source && r.ScalePer == scalePer)
            {
                _scaled.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>Registers a cross-stat rule: flat += ratio * sourceStat.</summary>
    public void AddStatRule(string target, string source, float ratio)
    {
        if (!_statsMap.ContainsKey(target) || string.IsNullOrEmpty(source) || !_statsMap.ContainsKey(source))
        {
            GD.PushWarning($"StatBlock: Stat rule '{target}' <- '{source}' references an unknown stat.");
            return;
        }
        _rules.Add(new StatRule(target, source, ratio));
    }

    /// <summary>Removes one matching rule (exact match); silent when absent.</summary>
    public bool RemoveStatRule(string target, string source, float ratio)
    {
        for (int i = 0; i < _rules.Count; i++)
        {
            var r = _rules[i];
            if (r.Target == target && r.Source == source && r.Ratio == ratio)
            {
                _rules.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>Test/reset hook: drops every scaled record and rule.</summary>
    public void ClearScaled()
    {
        _scaled.Clear();
        _rules.Clear();
    }

    private float DirectFlat(string statName) => _directFlat.TryGetValue(statName, out float v) ? v : 0.0f;
    private float DirectPct(string statName) => _directPct.TryGetValue(statName, out float v) ? v : 0.0f;

    /// <summary>
    /// Recomputes scaled + rule shares to a fixed point (no-cycle contract:
    /// warns past <see cref="MaxScalePasses"/> passes) and rewrites the pools.
    /// Owners must call this after mutating (<see cref="ActorStats"/> does it
    /// in its change notification); reads between mutation and recompute see
    /// the previous shares.
    /// </summary>
    public void RecomputeScaled()
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
        GD.PushWarning("[StatBlock] Scaling did not converge: check for source cycles.");
    }

    /// <summary>
    /// Rolls for critical hit
    /// </summary>
    public bool RollCritical()
    {
        return GD.Randf() < GetStat("crit_chance");
    }

    /// <summary>
    /// Rolls for fluid deformation evasion
    /// </summary>
    public bool RollEvasion()
    {
        float ev = GetStat("evasion");
        return ev > 0.0f && GD.Randf() < ev;
    }

    /// <summary>
    /// Rolls for glycocalyx block
    /// </summary>
    public bool RollBlock()
    {
        float blk = GetStat("block");
        return blk > 0.0f && GD.Randf() < blk;
    }

    /// <summary>
    /// Rolls for receptor life steal on hit
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
