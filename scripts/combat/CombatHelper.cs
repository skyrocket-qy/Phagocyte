using Godot;
using Phagocyte.Enemies;

namespace Phagocyte.Combat;

/// <summary>
/// Shared combat shims so skills, projectiles and hazards do not repeat the
/// C# / GDScript damage-dispatch branch.
/// </summary>
public static class CombatHelper
{
    /// <summary>
    /// Applies skill damage to a pathogen, routing to the typed C# API when
    /// possible and falling back to a GDScript <c>take_damage</c> method.
    /// </summary>
    public static void DealDamage(Node? target, float damage, Node2D? source, bool isCrit)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return;

        if (target is BaseEnemy enemy)
        {
            enemy.TakeDamage(damage, source, isCrit);
        }
        else if (target.HasMethod("take_damage"))
        {
            target.Call("take_damage", damage, source, isCrit);
        }
    }

    /// <summary>Source-less damage variant used by AoE waves.</summary>
    public static void DealDamage(Node? target, float damage)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return;

        if (target is BaseEnemy enemy)
        {
            enemy.TakeDamage(damage);
        }
        else if (target.HasMethod("take_damage"))
        {
            target.Call("take_damage", damage);
        }
    }
}
