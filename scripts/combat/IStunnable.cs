namespace Game.Combat;

/// <summary>
/// Typed contract for anything accepting a stun (mirrors <see cref="ISlowable"/>).
/// Stun state lives in data/timers on the host; this stays only as the dispatch
/// endpoint so generic effects never branch on concrete actor types.
/// </summary>
public interface IStunnable
{
    void ApplyStun(float duration);
}
