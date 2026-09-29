using Godot;
using Godot.Collections;
using System;
using Game.Combat;
using Game.Core;

namespace Game.Skills;

/// <summary>
/// Base class for all Active weapons and Passive Equipment Traits in Game.
/// Interacts directly with the universal ActorStats system.
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

    public string[] Tags { get; set; } = System.Array.Empty<string>();
    public float[] DamagePerLevel { get; set; } = System.Array.Empty<float>();
    public float[] CooldownPerLevel { get; set; } = System.Array.Empty<float>();

    public float CooldownTimer { get; set; } = 0.0f;
    public CharacterBody2D? Host { get; set; } = null;
    public Node? Stats { get; set; } = null;

    public virtual void SetupCatalogData()
    {
        if (string.IsNullOrEmpty(SkillId) || GameManager.SkillCatalog == null) return;
        if (GameManager.SkillCatalog.TryGetValue(SkillId, out var rawInfo) && rawInfo.VariantType == Variant.Type.Dictionary)
        {
            var info = rawInfo.AsGodotDictionary();
            if (info.TryGetValue("tags", out var tVal))
            {
                if (tVal.VariantType == Variant.Type.PackedStringArray)
                    Tags = tVal.AsStringArray();
                else if (tVal.VariantType == Variant.Type.Array)
                {
                    var arr = tVal.AsGodotArray();
                    Tags = new string[arr.Count];
                    for (int i = 0; i < arr.Count; i++) Tags[i] = arr[i].AsString();
                }
            }

            if (info.TryGetValue("damage_per_level", out var dVal))
            {
                if (dVal.VariantType == Variant.Type.PackedFloat32Array)
                    DamagePerLevel = dVal.AsFloat32Array();
                else if (dVal.VariantType == Variant.Type.Array)
                {
                    var arr = dVal.AsGodotArray();
                    DamagePerLevel = new float[arr.Count];
                    for (int i = 0; i < arr.Count; i++) DamagePerLevel[i] = (float)arr[i].AsDouble();
                }
            }

            if (info.TryGetValue("cooldown_per_level", out var cdVal))
            {
                if (cdVal.VariantType == Variant.Type.PackedFloat32Array)
                    CooldownPerLevel = cdVal.AsFloat32Array();
                else if (cdVal.VariantType == Variant.Type.Array)
                {
                    var arr = cdVal.AsGodotArray();
                    CooldownPerLevel = new float[arr.Count];
                    for (int i = 0; i < arr.Count; i++) CooldownPerLevel[i] = (float)arr[i].AsDouble();
                }
            }
        }
    }

    public float GetBaseDamageForLevel(float fallback)
    {
        if (DamagePerLevel != null && DamagePerLevel.Length > 0)
        {
            int idx = Math.Clamp(Level - 1, 0, DamagePerLevel.Length - 1);
            return DamagePerLevel[idx];
        }
        return fallback;
    }

    public float GetBaseCooldownForLevel(float fallback)
    {
        if (CooldownPerLevel != null && CooldownPerLevel.Length > 0)
        {
            int idx = Math.Clamp(Level - 1, 0, CooldownPerLevel.Length - 1);
            return CooldownPerLevel[idx];
        }
        return fallback;
    }

    public virtual void Setup(CharacterBody2D pHost)
    {
        Host = pHost;
        SetupCatalogData();

        if (Host != null)
        {
            if (Host.HasNode("ActorStats"))
            {
                Stats = Host.GetNode<Node>("ActorStats");
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

    [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, nameof(Host))]
    protected bool HasValidHost()
    {
        return Host != null && GodotObject.IsInstanceValid(Host);
    }

    protected void GetDamage(float baseDmg, out float damage, out bool isCrit)
    {
        var data = GetCalculatedDamage(baseDmg);
        damage = data.Damage;
        isCrit = data.IsCrit;
    }

    protected void ApplyStat(string stat, float flat, float percent)
    {
        if (Stats is IStatHost host)
        {
            host.AddModifier(stat, flat, percent);
        }
    }

    protected void RemoveStat(string stat, float flat, float percent)
    {
        if (Stats is IStatHost host)
        {
            host.RemoveModifier(stat, flat, percent);
        }
    }

    /// <summary>
    /// Archetype params for this skill id (assets/data/skill/*.json "params").
    /// Empty when the catalog row carries none.
    /// </summary>
    protected Godot.Collections.Dictionary SkillParams()
    {
        if (!string.IsNullOrEmpty(SkillId) && GameManager.SkillCatalog.TryGetValue(SkillId, out var raw)
            && raw.VariantType == Variant.Type.Dictionary)
        {
            var info = (Godot.Collections.Dictionary)raw;
            if (info.TryGetValue("params", out Variant pv) && pv.VariantType == Variant.Type.Dictionary)
                return (Godot.Collections.Dictionary)pv;
        }
        return new Godot.Collections.Dictionary();
    }

    protected float ParamFloat(Godot.Collections.Dictionary p, string key, float fallback)
    {
        return CatalogLoader.GetFloat(p, key, fallback);
    }

    /// <summary>
    /// Parses one on_hit entry (data: ailment id + mult/flat/duration).
    /// magnitude = mult != null ? dmg * mult : flat (both absent = def default).
    /// </summary>
    private static bool TryParseOnHit(Variant item, float dmg, out string ailment, out float mag, out float dur)
    {
        ailment = "";
        mag = -1.0f;
        dur = -1.0f;
        if (item.VariantType != Variant.Type.Dictionary)
            return false;
        var e = item.AsGodotDictionary();
        ailment = CatalogLoader.GetString(e, "ailment");
        if (ailment == "")
            return false;
        if (e.TryGetValue("mult", out var mv) && mv.VariantType != Variant.Type.Nil)
            mag = dmg * CatalogLoader.ToFloat(mv);
        else if (e.TryGetValue("flat", out var fv) && fv.VariantType != Variant.Type.Nil)
            mag = CatalogLoader.ToFloat(fv);
        dur = CatalogLoader.GetFloat(e, "duration", -1.0f);
        return true;
    }

    /// <summary>
    /// Bakes on_hit entries into pooled EffectSpecs for spawn-time handoff
    /// (salvo/zone). Truncates past 3 with a warning (hit-rate paths only).
    /// </summary>
    protected int BuildOnHitEffects(Dictionary p, float dmg, out EffectSpec e0, out EffectSpec e1, out EffectSpec e2)
    {
        e0 = default;
        e1 = default;
        e2 = default;
        if (!p.TryGetValue("on_hit", out var v) || v.VariantType != Variant.Type.Array)
            return 0;
        int n = 0;
        foreach (var item in v.AsGodotArray())
        {
            if (!TryParseOnHit(item, dmg, out string ailment, out float mag, out float dur))
                continue;
            var fx = new EffectSpec { EffectId = ailment, Magnitude = mag, Duration = dur };
            if (n == 0) e0 = fx;
            else if (n == 1) e1 = fx;
            else if (n == 2) e2 = fx;
            else
            {
                GD.PushWarning($"[BaseSkill] on_hit truncated past 3 for '{SkillId}'.");
                break;
            }
            n++;
        }
        return n;
    }

    /// <summary>
    /// Applies on_hit entries directly (beam/aura hit-time paths, zero alloc).
    /// </summary>
    protected void ApplyOnHitEffects(Node? target, Dictionary p, float dmg)
    {
        if (target is not IStatusHost host || host.Status == null)
            return;
        if (!p.TryGetValue("on_hit", out var v) || v.VariantType != Variant.Type.Array)
            return;
        foreach (var item in v.AsGodotArray())
        {
            if (TryParseOnHit(item, dmg, out string ailment, out float mag, out float dur))
                host.Status.Apply(ailment, mag, dur);
        }
    }

    protected int ParamInt(Godot.Collections.Dictionary p, string key, int fallback)
    {
        return CatalogLoader.GetInt(p, key, fallback);
    }

    protected string ParamString(Godot.Collections.Dictionary p, string key, string fallback = "")
    {
        return CatalogLoader.GetString(p, key, fallback);
    }

    public float GetCalculatedCooldown()
    {
        float baseCd = GetBaseCooldownForLevel(Cooldown);
        if (baseCd <= 0.0f)
            return 0.0f;

        if (Stats is IStatHost host)
        {
            float cdr = host.GetStat("cooldown_reduction");
            return baseCd * (1.0f - cdr);
        }
        return baseCd;
    }

    public DamageResult GetCalculatedDamage(float baseDmg)
    {
        if (Stats is ActorStats cs)
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

        return new DamageResult(baseDmg, false);
    }

    public float GetCalculatedArea(float baseArea)
    {
        if (Stats is IStatHost host)
            return baseArea * host.GetStat("area");
        return baseArea;
    }

    public int GetCalculatedAmount(int baseAmount)
    {
        if (Stats is IStatHost host)
            return baseAmount + (int)host.GetStat("amount");
        return baseAmount;
    }

    public int GetCalculatedPierce(int basePierce)
    {
        if (Stats is IStatHost host)
            return basePierce + (int)host.GetStat("pierce");
        return basePierce;
    }

    public float GetCalculatedSpeed(float baseSpeed)
    {
        if (Stats is IStatHost host)
            return baseSpeed * host.GetStat("projectile_speed");
        return baseSpeed;
    }

    public float GetCalculatedDuration(float baseDuration)
    {
        if (Stats is IStatHost host)
            return baseDuration * host.GetStat("duration");
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
            ["tags"] = Tags,
            ["cooldown_max"] = effCd,
            ["cooldown_ratio"] = cdPct,
            ["cooldown_time"] = Mathf.Max(0.0f, CooldownTimer)
        };
    }
}

