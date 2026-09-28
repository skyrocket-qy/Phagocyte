using Godot;

namespace Game.Combat;

/// <summary>
/// Generic on-hit effect. The engine sees only effect_id + scalars, never
/// domain types: "stun" dispatches via <see cref="IStunnable"/>, every ailment
/// id routes straight into <see cref="AilmentController.Apply"/> (channel
/// behavior comes from the ailment def). Magnitude semantics per contract:
/// DoT dps for dot-channel ailments, removed-fraction for slow, added-fraction
/// for amp (-1 = def default). Radius reserves on-hit AoE; Tint reserves
/// per-instance batch color.
/// </summary>
public struct EffectSpec
{
    public string EffectId;
    public float Magnitude;
    public float Duration;
    public float Radius;
    public Color Tint;

    public readonly void ApplyTo(Node? target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target) || string.IsNullOrEmpty(EffectId))
            return;
        if (EffectId == "stun")
        {
            if (target is IStunnable stunnable)
                stunnable.ApplyStun(Duration);
            return;
        }
        if (target is IAilmentHost host && host.Ailments != null)
            host.Ailments.Apply(EffectId, Magnitude, Duration);
    }

    public static void ApplyAll(Node? target, in EffectSpec e0, in EffectSpec e1, in EffectSpec e2, int count)
    {
        if (count <= 0)
            return;
        e0.ApplyTo(target);
        if (count <= 1)
            return;
        e1.ApplyTo(target);
        if (count <= 2)
            return;
        e2.ApplyTo(target);
    }
}
