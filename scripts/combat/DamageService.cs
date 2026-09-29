using Godot;

namespace Game.Combat;

/// <summary>
/// Stack-allocated damage outcome: final damage + crit flag.
/// Replaces per-hit <c>Godot.Collections.Dictionary</c> allocation in
/// <c>BaseSkill.GetCalculatedDamage</c> (zero GC on the combat hot path).
/// </summary>
public readonly record struct DamageResult(float Damage, bool IsCrit);

/// <summary>
/// Shared combat damage service: routes damage directly to typed <see cref="IDamageable"/> targets.
/// </summary>
public static class DamageService
{
    /// <summary>
    /// Applies skill damage to an entity, routing to the typed <see cref="IDamageable"/> API.
    /// </summary>
    public static void DealDamage(Node? target, float damage, Node2D? source, bool isCrit)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return;

        if (target is IDamageable damageable)
        {
            damageable.TakeDamage(damage, source, isCrit);
        }
    }

    /// <summary>Source-less damage variant used by AoE waves.</summary>
    public static void DealDamage(Node? target, float damage)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return;

        if (target is IDamageable damageable)
        {
            damageable.TakeDamage(damage);
        }
    }
}
