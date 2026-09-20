using Godot;

namespace Phagocyte.Combat;

/// <summary>
/// Typed contract for anything that accepts direct or DoT damage.
/// Replaces string duck-typing (<c>HasMethod("take_damage")</c>).
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage, Node2D? source = null, bool isCrit = false);
    void TakeDoTDamage(float dotDamage);
}
