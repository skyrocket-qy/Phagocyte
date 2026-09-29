using Godot;

namespace Game.Combat;

/// <summary>
/// Typed contract for anything that accepts direct or DoT damage.
/// Replaces string duck-typing (<c>HasMethod("take_damage")</c>).
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage, Node2D? source = null, bool isCrit = false);
    void TakeDoTDamage(float dotDamage);
}

/// <summary>
/// Typed contract for anything accepting a movement slow (fraction factor).
/// Implemented by PlayerActor and EnemyActor so slows target hosts, not types —
/// stage hazards and skills slow through this instead of concrete cell classes.
/// </summary>
public interface ISlowable
{
    void ApplySlow(float duration, float factor);
}

/// <summary>
/// Typed contract for anything accepting a stun (mirrors <see cref="ISlowable"/>).
/// Stun state lives in data/timers on the host; this stays only as the dispatch
/// endpoint so generic effects never branch on concrete actor types.
/// </summary>
public interface IStunnable
{
    void ApplyStun(float duration);
}

/// <summary>
/// Typed contract for anything carrying status/ailment state. Lets generic effects
/// target hosts through <see cref="StatusController"/> instead of concrete
/// actor types.
/// </summary>
public interface IStatusHost
{
    StatusController? Status { get; }
}
