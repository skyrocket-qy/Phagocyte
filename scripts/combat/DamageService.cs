using Godot;

namespace Game.Combat;

public static class DamageService
{
    public static void DealDamage(Node? target, float baseDamage, Node2D? source = null, float critChance = 0.0f, float critMultiplier = 1.0f)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return;

        bool isCrit = critChance > 0.0f && GD.Randf() < critChance;
        float finalDamage = isCrit ? baseDamage * critMultiplier : baseDamage;

        if (target is IDamageable damageable)
        {
            damageable.TakeDamage(finalDamage, source, isCrit);
        }
    }
}
