using Godot;

namespace Game.Combat;

public interface IDamageable
{
    void TakeDamage(float damage, Node2D? source = null, bool isCrit = false);
    void TakeDoTDamage(float dotDamage);
}

public interface ISlowable
{
    void ApplySlow(float duration, float factor);
}

public interface IStunnable
{
    void ApplyStun(float duration);
}

public interface IStatusHost
{
    StatusController? Status { get; }
}
