namespace Game.Combat;

/// <summary>
/// Typed contract for anything accepting a movement slow (fraction factor).
/// Implemented by PlayerActor and EnemyActor so slows target hosts, not types —
/// map hazards and skills slow through this instead of concrete cell classes.
/// </summary>
public interface ISlowable
{
    void ApplySlow(float duration, float factor);
}
