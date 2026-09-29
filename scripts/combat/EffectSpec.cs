using Godot;

namespace Game.Combat;

/// <summary>Generic on-hit status or damage effect payload.</summary>
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
        if (target is IStatusHost host && host.Status != null)
            host.Status.Apply(EffectId, Magnitude, Duration);
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
