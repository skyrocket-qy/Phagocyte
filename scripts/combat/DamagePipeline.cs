using Godot;
using Game.Core;
using Game.Enemies;
using Game.Player;
using Game.UI;

namespace Game.Combat;

/// <summary>Central hit resolution: crit at hit, actor intake, ailment derivation, knockback, leech.</summary>
public static class DamagePipeline
{
    /// <summary>Ailment threshold as a fraction of target max HP (full strength at/above).</summary>
    public const float AilmentThresholdFraction = 0.05f;

    public static HitResult ResolveHit(in HitPayload payload, Node? target)
    {
        var result = new HitResult();
        if (target == null || !GodotObject.IsInstanceValid(target))
            return result;
        if (target is not IDamageable damageable)
            return result;

        Node2D? attacker = ResolveAttacker(payload.AttackerId);
        bool isCrit = RollCrit(payload, attacker);
        float raw = isCrit ? payload.RawDamage * CritMultiplierOf(attacker) : payload.RawDamage;

        result = damageable.TakeDamage(raw, attacker, isCrit);
        result.IsCrit = isCrit;
        result.TargetKilled = IsKilled(target);
        if (result.DamageDealt <= 0.0f)
            return result;

        DispatchEffects(payload, target, result.DamageDealt);
        ApplyLeech(attacker, result.DamageDealt);
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
        if (attacker is PlayerActor pa && pa.Stats is ActorStats st)
            return GD.Randf() < st.GetStat("crit_chance");
        return false;
    }

    private static float CritMultiplierOf(Node2D? attacker)
    {
        if (attacker is PlayerActor pa && pa.Stats is ActorStats st)
        {
            float cd = st.GetStat("crit_damage");
            return cd > 0.0f ? cd : 1.0f;
        }
        return 1.0f;
    }

    private static bool IsKilled(Node target)
    {
        return target switch
        {
            PlayerActor pa => pa.IsDead,
            EnemyActor ea => ea.CurrentHealth <= 0.0f,
            _ => false,
        };
    }

    private static void DispatchEffects(in HitPayload payload, Node target, float dealt)
    {
        if (payload.EffectCount <= 0)
            return;
        if (target is not IStatusHost host || host.Status == null)
            return;
        float threshold = MaxHpOf(target) * AilmentThresholdFraction;
        float scale = threshold > 0.0f ? Mathf.Clamp(dealt / threshold, 0.0f, 1.0f) : 1.0f;
        ApplyEffect(host.Status, payload.Effect0, scale);
        if (payload.EffectCount > 1)
            ApplyEffect(host.Status, payload.Effect1, scale);
        if (payload.EffectCount > 2)
            ApplyEffect(host.Status, payload.Effect2, scale);
    }

    private static void ApplyEffect(StatusController status, in EffectSpec e, float scale)
    {
        if (string.IsNullOrEmpty(e.EffectId))
            return;
        float mag = e.Magnitude >= 0.0f ? e.Magnitude * scale : e.Magnitude;
        float dur = e.Duration >= 0.0f ? Mathf.Max(0.1f, e.Duration * scale) : e.Duration;
        status.Apply(e.EffectId, mag, dur);
    }

    private static float MaxHpOf(Node target)
    {
        return target switch
        {
            PlayerActor pa => pa.Stats?.GetStat("max_health") ?? 100.0f,
            EnemyActor ea => ea.MaxHealth,
            _ => 0.0f,
        };
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
