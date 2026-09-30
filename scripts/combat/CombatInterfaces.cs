using Godot;

namespace Game.Combat;

public interface IDamageable
{
    /// <summary>Applies a mitigated hit; reports dealt damage and avoidance flags.</summary>
    HitResult TakeDamage(float damage, Node2D? source = null, bool isCrit = false);
    void TakeDoTDamage(float dotDamage);
}

public interface IStatusHost
{
    StatusController? Status { get; }
}
