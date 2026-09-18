using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Phagocyte.Core;

/// <summary>
/// Telemetry manager recording in-run combat statistics:
/// weapon damage breakdown, damage taken, and defensive procs (evasion, block, life steal).
/// </summary>
public partial class RunTelemetryManager : Node
{
    public static RunTelemetryManager? Instance { get; private set; }

    public bool IsRunActive { get; private set; } = false;
    public float TotalDamageDealt { get; private set; } = 0.0f;
    public float TotalDamageTaken { get; private set; } = 0.0f;
    public int EvadedCount { get; private set; } = 0;
    public int BlockedCount { get; private set; } = 0;
    public float LifeStealHealed { get; private set; } = 0.0f;

    /// <summary>Total pathogens killed this run (ranged kills + engulfed kills).</summary>
    public int KillCount { get; private set; } = 0;

    /// <summary>Accumulated fixed base score of every killed pathogen (docs/record.md §4.1).</summary>
    public int KillScore { get; private set; } = 0;

    public Dictionary<string, float> SkillDamageMap { get; } = new(StringComparer.OrdinalIgnoreCase);

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base._ExitTree();
    }

    public void StartRun()
    {
        IsRunActive = true;
        TotalDamageDealt = 0.0f;
        TotalDamageTaken = 0.0f;
        EvadedCount = 0;
        BlockedCount = 0;
        LifeStealHealed = 0.0f;
        KillCount = 0;
        KillScore = 0;
        SkillDamageMap.Clear();
    }

    public void EndRun()
    {
        IsRunActive = false;
    }

    public void RecordDamageDealt(string? skillId, float amount)
    {
        if (!IsRunActive || amount <= 0.0f) return;

        TotalDamageDealt += amount;
        string key = string.IsNullOrEmpty(skillId) ? "basic_attack" : skillId;

        if (!SkillDamageMap.ContainsKey(key))
        {
            SkillDamageMap[key] = 0.0f;
        }
        SkillDamageMap[key] += amount;
    }

    public void RecordDamageTaken(float amount)
    {
        if (!IsRunActive || amount <= 0.0f) return;
        TotalDamageTaken += amount;
    }

    public void RecordEvaded()
    {
        if (!IsRunActive) return;
        EvadedCount++;
    }

    public void RecordBlocked()
    {
        if (!IsRunActive) return;
        BlockedCount++;
    }

    public void RecordLifeSteal(float amount)
    {
        if (!IsRunActive || amount <= 0.0f) return;
        LifeStealHealed += amount;
    }

    /// <summary>
    /// Log a pathogen kill. Score is the enemy's fixed base score
    /// (0 for farming-neutral hazards such as senescent RBCs).
    /// </summary>
    public void RecordKill(int baseScore)
    {
        if (!IsRunActive) return;
        KillCount++;
        KillScore += Math.Max(0, baseScore);
    }

    public List<(string SkillId, float Damage, float Pct)> GetTopSkills(int count = 4)
    {
        float total = TotalDamageDealt > 0.0f ? TotalDamageDealt : 1.0f;
        return SkillDamageMap
            .OrderByDescending(kv => kv.Value)
            .Take(count)
            .Select(kv => (kv.Key, kv.Value, (kv.Value / total) * 100.0f))
            .ToList();
    }

    public Godot.Collections.Dictionary GetTelemetryDictionary()
    {
        var dict = new Godot.Collections.Dictionary
        {
            { "total_damage_dealt", TotalDamageDealt },
            { "total_damage_taken", TotalDamageTaken },
            { "evaded_count", EvadedCount },
            { "blocked_count", BlockedCount },
            { "lifesteal_healed", LifeStealHealed },
            { "kills", KillCount },
            { "kill_score", KillScore }
        };

        var skillDict = new Godot.Collections.Dictionary();
        foreach (var (k, v) in SkillDamageMap)
        {
            skillDict[k] = v;
        }
        dict["skills"] = skillDict;

        return dict;
    }
}
