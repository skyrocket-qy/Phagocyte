using Godot;

namespace Game.Combat;

public readonly record struct DamageResult(float Damage, bool IsCrit);

public static class DamageService
{
    public static void DealDamage(Node? target, float damage, Node2D? source, bool isCrit)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return;

        if (target is IDamageable damageable)
        {
            damageable.TakeDamage(damage, source, isCrit);
        }
    }
}
