using Godot;
using Game.Core;
using Game.Enemies;
using Game.Player;

namespace Game.Combat;

/// <summary>Central hit resolution: avoidance, block, damage calculation, mitigation, intake, ailments, leech.</summary>
public static class HitPipeline
{
    public const float SlowAmpThresholdFraction = 0.10f;
    public const float StunThresholdFraction = 0.20f;
    public const float ControlFixedMagnitude = 0.3f;
    public const float StunDuration = 0.5f;

    public static HitResult ResolveHit(in HitPayload payload, Node? targetNode)
    {
        var result = new HitResult();
        if (targetNode == null || !GodotObject.IsInstanceValid(targetNode))
            return result;
        if (targetNode is not IDamageable target)
            return result;

        DefenseProfile def = target.Defenses;

        // Stage 1: Avoidance (Invulnerability & Evasion)
        if (def.IsInvulnerable)
        {
            result.IsInvulnerable = true;
            return result;
        }

        if ((payload.Flags & HitFlags.CannotBeEvaded) == 0 && def.Evasion > 0.0f)
        {
            if (GD.Randf() < def.Evasion)
            {
                result.IsEvaded = true;
                return result;
            }
        }

        // Stage 2: Block
        bool isBlocked = false;
        float blockMitigation = 0.0f;
        if ((payload.Flags & HitFlags.CannotBeBlocked) == 0 && def.BlockChance > 0.0f)
        {
            if (GD.Randf() < def.BlockChance)
            {
                isBlocked = true;
                blockMitigation = def.BlockMitigation > 0.0f ? def.BlockMitigation : DefenseProfile.DefaultBlockMitigation;
                if (blockMitigation >= 1.0f)
                {
                    result.IsBlocked = true;
                    return result;
                }
            }
        }

        // Stage 3: Attacker Damage & Crit Roll (Payload snapshot preferred, fallback to live attacker)
        Node2D? attacker = ResolveAttacker(payload.AttackerId);
        bool isCrit = RollCrit(payload, attacker);
        float damage = payload.RawDamage;
        float critMult = GetCritMultiplier(payload, attacker);
        if (isCrit) damage *= critMult;
        if (isBlocked) damage *= 1.0f - blockMitigation;

        // Stage 4: Target Defense Mitigation (POE armour + vulnerability)
        if ((payload.Flags & HitFlags.BypassesArmor) == 0 && def.Armor > 0.0f && damage > 0.0f)
            damage *= 1.0f - CombatMath.FromArmorPenetrated(def.Armor, damage, payload.ArmorPenetration);
        if (def.DamageTakenMultiplier > 0.0f)
            damage *= def.DamageTakenMultiplier;

        // Stage 5: Target Pure Intake
        float actualDamage = target.TakeDamage(damage, isCrit);

        result.DamageDealt = actualDamage;
        result.IsCrit = isCrit;
        result.IsBlocked = isBlocked;
        result.TargetKilled = target.IsDead;

        if (actualDamage <= 0.0f)
            return result;

        // Stage 6: Post-Hit Procs
        DispatchEffects(payload, targetNode, actualDamage, isCrit);
        result.LifeStolen = ApplyLeech(attacker, actualDamage);
        return result;
    }

    internal static Node2D? ResolveAttacker(ulong attackerId)
    {
        if (attackerId == 0) return null;
        var obj = GodotObject.InstanceFromId(attackerId);
        return obj is Node2D node && GodotObject.IsInstanceValid(node) ? node : null;
    }

    private static bool RollCrit(in HitPayload payload, Node2D? attacker)
    {
        if ((payload.Flags & HitFlags.NeverCrit) != 0)
            return false;
        if ((payload.Flags & HitFlags.AlwaysCrit) != 0)
            return true;
        if (payload.CritChance > 0.0f)
            return GD.Randf() < payload.CritChance;
        if (attacker is PlayerActor pa && pa.Stats is ActorStats st)
            return GD.Randf() < st.GetStat("crit_chance");
        return false;
    }

    private static float GetCritMultiplier(in HitPayload payload, Node2D? attacker)
    {
        if (payload.CritMultiplier > 0.0f)
            return payload.CritMultiplier;
        if (attacker is PlayerActor pa && pa.Stats is ActorStats st)
        {
            float cd = st.GetStat("crit_damage");
            return cd > 0.0f ? cd : 2.0f;
        }
        return 2.0f;
    }

    private static bool RollAilment(float chance)
    {
        if (chance >= 1.0f)
            return true;
        if (chance <= 0.0f)
            return false;
        return GD.Randf() < chance;
    }

    /// <summary>Damage-derived control chance: scale x (1 + ailment_chance), crits double, capped at 1.</summary>
    public static float ControlApplyChance(float scale, float ailmentChance, bool isCrit)
    {
        float chance = scale * (1.0f + ailmentChance) * (isCrit ? 2.0f : 1.0f);
        return Mathf.Clamp(chance, 0.0f, 1.0f);
    }

    private static void DispatchEffects(in HitPayload payload, Node target, float dealt, bool isCrit)
    {
        if (payload.EffectCount <= 0)
            return;
        if (target is not IStatusHost host)
            return;
        float ailChance = payload.AilmentChance;
        float ailEffect = AilmentEffectOf(payload.AttackerId);
        float dotMult = DotDamageOf(payload.AttackerId);
        float armor = target is IDamageable damageable ? damageable.Defenses.Armor : 0.0f;
        ApplyEffect(host.Status, payload.Effect0, target, dealt, isCrit, ailChance, ailEffect, dotMult, armor, payload);
        if (payload.EffectCount > 1)
            ApplyEffect(host.Status, payload.Effect1, target, dealt, isCrit, ailChance, ailEffect, dotMult, armor, payload);
        if (payload.EffectCount > 2)
            ApplyEffect(host.Status, payload.Effect2, target, dealt, isCrit, ailChance, ailEffect, dotMult, armor, payload);
    }

    private static void ApplyEffect(StatusController status, in EffectSpec e, Node target, float dealt, bool isCrit, float ailChance, float ailEffect, float dotMult, float armor, in HitPayload payload)
    {
        if (string.IsNullOrEmpty(e.EffectId))
            return;
        string kind = status.KindOf(e.EffectId);
        if (kind == "dot")
        {
            if (!isCrit && !RollAilment(ailChance))
                return;
            float ratio = e.Ratio;
            if (ratio <= 0.0f)
                return;
            float dps = dealt * ratio * dotMult;
            dps *= 1.0f - CombatMath.FromArmorDot(armor, dps, payload.ArmorPenetration);
            status.Apply(e.EffectId, dps, e.Duration);
            return;
        }
        if (kind == "slow" || kind == "amp" || kind == "stun")
        {
            float fraction = kind == "stun" ? StunThresholdFraction : SlowAmpThresholdFraction;
            float threshold = ControlThresholdOf(target, fraction);
            float scale = threshold > 0.0f ? dealt / threshold : 1.0f;
            if (GD.Randf() >= ControlApplyChance(scale, ailChance, isCrit))
                return;
            if (kind == "stun")
            {
                status.Apply(e.EffectId, 0.0f, StunDuration);
                return;
            }
            status.Apply(e.EffectId, ControlFixedMagnitude * ailEffect, e.Duration);
            return;
        }
        GD.PushWarning($"[HitPipeline] Unknown status kind for '{e.EffectId}'.");
        return;
    }

    internal static float ControlThresholdOf(Node target, float fraction)
    {
        float maxHp = 0.0f;
        float mult = 1.0f;
        switch (target)
        {
            case PlayerActor pa:
                maxHp = pa.Stats?.GetStat("max_health") ?? 100.0f;
                mult = pa.Stats?.GetStat("ailment_threshold") ?? 1.0f;
                break;
            case EnemyActor ea:
                maxHp = ea.MaxHealth;
                mult = ea.AilmentThresholdMult;
                break;
        }
        return Mathf.Max(0.0f, maxHp * fraction * Mathf.Max(0.0f, mult));
    }

    private static float AilmentEffectOf(ulong attackerId)
    {
        Node2D? attacker = ResolveAttacker(attackerId);
        if (attacker is PlayerActor pa && pa.Stats is ActorStats st)
            return Mathf.Max(0.0f, st.GetStat("ailment_effect"));
        return 1.0f;
    }

    private static float DotDamageOf(ulong attackerId)
    {
        Node2D? attacker = ResolveAttacker(attackerId);
        if (attacker is PlayerActor pa && pa.Stats is ActorStats st)
            return Mathf.Max(0.0f, st.GetStat("dot_damage"));
        return 1.0f;
    }

    private static float ApplyLeech(Node2D? attacker, float dealt)
    {
        if (attacker is PlayerActor pa && pa.Stats is ActorStats st && st.RollLifeSteal())
        {
            pa.Heal(1.0f);
            RunTelemetryManager.Instance?.RecordLifeSteal(1.0f);
            return 1.0f;
        }
        return 0.0f;
    }
}
