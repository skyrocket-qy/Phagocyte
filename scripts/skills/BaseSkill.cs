using Godot;
using Godot.Collections;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Base class for all Active Cytokine Weapons and Passive Organelle Traits in Phagocyte.
/// Interacts directly with the universal CellStats system.
/// </summary>
public partial class BaseSkill : Node2D
{
    [Export] public string SkillId { get; set; } = "";
    [Export] public string NameKey { get; set; } = "";
    [Export] public string DescKey { get; set; } = "";
    [Export] public string BioKey { get; set; } = "";
    [Export] public string IconSymbol { get; set; } = "⚡";
    [Export] public int Level { get; set; } = 1;
    [Export] public int MaxLevel { get; set; } = 5;
    [Export] public float Cooldown { get; set; } = 0.0f;
    [Export] public bool IsPassive { get; set; } = false;
    [Export] public bool IsInnate { get; set; } = false;

    public float CooldownTimer { get; set; } = 0.0f;
    public CharacterBody2D? Host { get; set; } = null;
    public Node? Stats { get; set; } = null;

    public virtual void Setup(CharacterBody2D pHost)
    {
        Host = pHost;

        if (Host != null)
        {
            if (Host.HasNode("CellStats"))
            {
                Stats = Host.GetNode<Node>("CellStats");
            }
            else
            {
                var statsProp = Host.Get("stats");
                if (statsProp.VariantType == Variant.Type.Object && statsProp.AsGodotObject() is Node n)
                {
                    Stats = n;
                }
            }
        }

        if (IsPassive)
        {
            ApplyPassiveModifiers();
        }
    }

    public virtual void UpdateSkill(double delta)
    {
        if (IsPassive)
            return;

        float currentCd = GetCalculatedCooldown();
        if (currentCd <= 0.0f)
            return;

        if (CooldownTimer > 0.0f)
        {
            CooldownTimer -= (float)delta;
            if (CooldownTimer <= 0.0f)
            {
                Trigger();
            }
        }
    }

    public virtual void Trigger()
    {
        CooldownTimer = GetCalculatedCooldown();
    }

    public virtual void Upgrade()
    {
        if (Level < MaxLevel)
        {
            if (IsPassive)
            {
                RemovePassiveModifiers();
            }
            Level += 1;
            if (IsPassive)
            {
                ApplyPassiveModifiers();
            }
        }
    }

    public override void _ExitTree()
    {
        if (IsPassive)
        {
            RemovePassiveModifiers();
        }
    }

    // --- Stat Consumption Helpers for Active Skills ---

    /// <summary>True when the skill has a live host to act through.</summary>
    [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, nameof(Host))]
    protected bool HasValidHost()
    {
        return Host != null && GodotObject.IsInstanceValid(Host);
    }

    /// <summary>Damage + crit flag in one call, zero-GC struct unpack.</summary>
    protected void GetDamage(float baseDmg, out float damage, out bool isCrit)
    {
        var data = GetCalculatedDamage(baseDmg);
        damage = data.Damage;
        isCrit = data.IsCrit;
    }

    /// <summary>
    /// Registers one flat/percent stat modifier on the host, routing to the
    /// typed CellStats API and falling back to a GDScript add_modifier method.
    /// </summary>
    protected void ApplyStat(string stat, float flat, float percent)
    {
        if (Stats is IStatHost host)
        {
            host.AddModifier(stat, flat, percent);
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            Stats.Call("add_modifier", stat, flat, percent);
        }
    }

    /// <summary>Removes a modifier previously registered by <see cref="ApplyStat"/>.</summary>
    protected void RemoveStat(string stat, float flat, float percent)
    {
        if (Stats is IStatHost host)
        {
            host.RemoveModifier(stat, flat, percent);
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            Stats.Call("remove_modifier", stat, flat, percent);
        }
    }

    public float GetCalculatedCooldown()
    {
        if (Cooldown <= 0.0f)
            return 0.0f;

        if (Stats is IStatHost host)
        {
            float cdr = host.GetStat("cooldown_reduction");
            return Cooldown * (1.0f - cdr);
        }
        else if (Stats != null && Stats.HasMethod("get_stat"))
        {
            float cdr = (float)Stats.Call("get_stat", "cooldown_reduction");
            return Cooldown * (1.0f - cdr);
        }
        return Cooldown;
    }

    public DamageResult GetCalculatedDamage(float baseDmg)
    {
        if (Stats is CellStats cs)
        {
            float might = cs.GetStat("might");
            float dmg = baseDmg * might;
            if (cs.RollCritical())
            {
                return new DamageResult(dmg * cs.GetStat("crit_damage"), true);
            }
            return new DamageResult(dmg, false);
        }
        else if (Stats is IStatHost host)
        {
            float might = host.GetStat("might");
            return new DamageResult(baseDmg * might, false);
        }
        else if (Stats != null && Stats.HasMethod("get_stat"))
        {
            float might = (float)Stats.Call("get_stat", "might");
            float dmg = baseDmg * might;
            bool rollCrit = Stats.HasMethod("roll_critical") && (bool)Stats.Call("roll_critical");
            if (rollCrit)
            {
                return new DamageResult(dmg * (float)Stats.Call("get_stat", "crit_damage"), true);
            }
            return new DamageResult(dmg, false);
        }

        return new DamageResult(baseDmg, false);
    }

    public float GetCalculatedArea(float baseArea)
    {
        if (Stats is IStatHost host)
            return baseArea * host.GetStat("area");
        if (Stats != null && Stats.HasMethod("get_stat"))
            return baseArea * (float)Stats.Call("get_stat", "area");
        return baseArea;
    }

    public int GetCalculatedAmount(int baseAmount)
    {
        if (Stats is IStatHost host)
            return baseAmount + (int)host.GetStat("amount");
        if (Stats != null && Stats.HasMethod("get_stat"))
            return baseAmount + (int)(float)Stats.Call("get_stat", "amount");
        return baseAmount;
    }

    public int GetCalculatedPierce(int basePierce)
    {
        if (Stats is IStatHost host)
            return basePierce + (int)host.GetStat("pierce");
        if (Stats != null && Stats.HasMethod("get_stat"))
            return basePierce + (int)(float)Stats.Call("get_stat", "pierce");
        return basePierce;
    }

    public float GetCalculatedSpeed(float baseSpeed)
    {
        if (Stats is IStatHost host)
            return baseSpeed * host.GetStat("projectile_speed");
        if (Stats != null && Stats.HasMethod("get_stat"))
            return baseSpeed * (float)Stats.Call("get_stat", "projectile_speed");
        return baseSpeed;
    }

    public float GetCalculatedDuration(float baseDuration)
    {
        if (Stats is IStatHost host)
            return baseDuration * host.GetStat("duration");
        if (Stats != null && Stats.HasMethod("get_stat"))
            return baseDuration * (float)Stats.Call("get_stat", "duration");
        return baseDuration;
    }

    // --- Virtual Hooks for Passive Traits to provide Stat Modifiers ---

    public virtual void ApplyPassiveModifiers()
    {
    }

    public virtual void RemovePassiveModifiers()
    {
    }

    // --- UI Representation ---

    public virtual Dictionary GetUiData()
    {
        float cdPct = 0.0f;
        float effCd = GetCalculatedCooldown();
        if (effCd > 0.0f && !IsPassive)
        {
            cdPct = Mathf.Clamp(CooldownTimer / effCd, 0.0f, 1.0f);
        }

        return new Dictionary
        {
            ["id"] = SkillId,
            ["name"] = !string.IsNullOrEmpty(NameKey) ? Tr(NameKey) : SkillId,
            ["description"] = !string.IsNullOrEmpty(DescKey) ? Tr(DescKey) : "",
            ["biochemistry"] = !string.IsNullOrEmpty(BioKey) ? Tr(BioKey) : "",
            ["icon"] = IconSymbol,
            ["image_path"] = AssetPaths.SkillIcon(SkillId),
            ["level"] = Level,
            ["max_level"] = MaxLevel,
            ["is_passive"] = IsPassive,
            ["is_innate"] = IsInnate,
            ["cooldown_max"] = effCd,
            ["cooldown_ratio"] = cdPct,
            ["cooldown_time"] = Mathf.Max(0.0f, CooldownTimer)
        };
    }
}
