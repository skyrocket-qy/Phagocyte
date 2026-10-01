using Godot;
using Game.Core;
using Game.Enemies;
using Game.Player;
using Game.UI;

namespace Game.Combat;

/// <summary>Central hit resolution: avoidance, block, damage calculation, mitigation, intake, ailments, leech.</summary>
public static class HitPipeline
{
    public const float AilmentThresholdFraction = 0.05f;

    public static HitResult ResolveHit(in HitPayload payload, Node? targetNode)
    {
        var result = new HitResult();
        if (targetNode == null || !GodotObject.IsInstanceValid(targetNode))
            return result;
        if (targetNode is not IDamageable target)
            return result;

        DefenseProfile def = target.Defenses;
        Vector2 targetPos = targetNode is Node2D n2d ? n2d.GlobalPosition : Vector2.Zero;

        // Stage 1: Avoidance (Invulnerability & Evasion)
        if (def.IsInvulnerable)
        {
            DamageNumberSpawner.ShowEvaded(targetPos);
            result.IsEvaded = true;
            return result;
        }

        if ((payload.Flags & HitFlags.CannotBeEvaded) == 0 && def.Evasion > 0.0f)
        {
            if (GD.Randf() < def.Evasion)
            {
                DamageNumberSpawner.ShowEvaded(targetPos);
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
                blockMitigation = def.BlockMitigation > 0.0f ? def.BlockMitigation : 1.0f;
                if (blockMitigation >= 1.0f)
                {
                    DamageNumberSpawner.ShowBlocked(targetPos);
                    result.IsBlocked = true;
                    return result;
                }
            }
        }

        // Stage 3: Attacker Damage & Crit Roll (Payload snapshot preferred, fallback to live attacker)
        Node2D? attacker = ResolveAttacker(payload.AttackerId);
        bool isCrit = RollCrit(payload, attacker);
        float critMult = GetCritMultiplier(payload, attacker);
        float rawDamage = isCrit ? payload.RawDamage * critMult : payload.RawDamage;
        if (isBlocked)
            rawDamage *= (1.0f - blockMitigation);

        // Stage 4: Target Defense Mitigation (POE armour + vulnerability)
        float damage = rawDamage;
        if ((payload.Flags & HitFlags.BypassesArmor) == 0 && def.Armor > 0.0f && damage > 0.0f)
            damage *= 1.0f - CombatMath.FromArmorPenetrated(def.Armor, damage, payload.ArmorPenetration);
        if (def.DamageTakenMultiplier > 0.0f)
            damage *= def.DamageTakenMultiplier;

        // Stage 5: Target Pure Intake
        float actualDamage = target.TakeDamage(damage, attacker, isCrit);

        result.DamageDealt = actualDamage;
        result.IsCrit = isCrit;
        result.IsBlocked = isBlocked;
        result.TargetKilled = target.IsDead;

        if (actualDamage <= 0.0f)
            return result;

        // Stage 6: Post-Hit Procs
        if (payload.EffectCount > 0 && (isCrit || RollAilment(payload.AilmentChance)))
            DispatchEffects(payload, targetNode, actualDamage);
        ApplyLeech(attacker, actualDamage);
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

    private static void DispatchEffects(in HitPayload payload, Node target, float dealt)
    {
        if (payload.EffectCount <= 0)
            return;
        if (target is not IStatusHost host)
            return;
        float threshold = AilmentThresholdOf(target);
        float scale = threshold > 0.0f ? Mathf.Clamp(dealt / threshold, 0.0f, 1.0f) : 1.0f;
        ApplyEffect(host.Status, payload.Effect0, scale);
        if (payload.EffectCount > 1)
            ApplyEffect(host.Status, payload.Effect1, scale);
        if (payload.EffectCount > 2)
            ApplyEffect(host.Status, payload.Effect2, scale);
    }

    internal static float AilmentThresholdOf(Node target)
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
        return Mathf.Max(0.0f, maxHp * AilmentThresholdFraction * Mathf.Max(0.0f, mult));
    }

    private static void ApplyEffect(StatusController status, in EffectSpec e, float scale)
    {
        if (string.IsNullOrEmpty(e.EffectId))
            return;
        float mag = e.Magnitude >= 0.0f ? e.Magnitude * scale : e.Magnitude;
        float dur = e.Duration >= 0.0f ? Mathf.Max(0.1f, e.Duration * scale) : e.Duration;
        status.Apply(e.EffectId, mag, dur);
    }

    private static void ApplyLeech(Node2D? attacker, float dealt)
    {
        if (attacker is PlayerActor pa && pa.Stats is ActorStats st && st.RollLifeSteal())
        {
            pa.Heal(1.0f);
            DamageNumberSpawner.ShowHeal(pa.GlobalPosition, 1.0f);
            RunTelemetryManager.Instance?.RecordLifeSteal(1.0f);
        }
    }
}
