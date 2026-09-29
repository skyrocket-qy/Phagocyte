namespace Game.Combat;

/// <summary>
/// Typed contract for anything carrying status/ailment state. Lets generic effects
/// target hosts through <see cref="StatusController"/> instead of concrete
/// actor types.
/// </summary>
public interface IStatusHost
{
    StatusController? Status { get; }
}
