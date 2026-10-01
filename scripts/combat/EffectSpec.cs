using Godot;

namespace Game.Combat;

/// <summary>Generic on-hit status or damage effect payload.</summary>
public struct EffectSpec
{
    public string EffectId;
    public float Magnitude;
    public float Duration;
}
