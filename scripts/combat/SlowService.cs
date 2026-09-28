using Godot;

namespace Game.Combat;

/// <summary>
/// Shared slow-dispatch shim so hazards, skills, traits, stage effects and
/// directors never name concrete host types on the slow path. Mirrors
/// <see cref="DamageService"/>: callers pass a <c>Node?</c>, the service
/// pattern-matches <see cref="ISlowable"/> internally.
/// </summary>
public static class SlowService
{
    /// <summary>
    /// Applies a movement slow to any <see cref="ISlowable"/> host.
    /// Null, freed or non-slowable targets are silently ignored.
    /// </summary>
    public static void ApplySlow(Node? target, float duration, float factor)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return;
        if (target is ISlowable slowable)
            slowable.ApplySlow(duration, factor);
    }
}
