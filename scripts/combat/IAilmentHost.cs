namespace Game.Combat;

/// <summary>
/// Typed contract for anything carrying ailment state. Lets generic effects
/// target hosts through <see cref="AilmentController"/> instead of concrete
/// actor types.
/// </summary>
public interface IAilmentHost
{
    AilmentController? Ailments { get; }
}
