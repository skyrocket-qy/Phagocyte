using Godot;

namespace Game.Combat;

/// <summary>Generic on-hit status payload. Ratio is the dealt-anchored multiplier
/// for dot kinds (dps = dealt x ratio); unused (0) for control kinds.</summary>
public struct EffectSpec
{
    public string EffectId;
    public float Ratio;
    public float Duration;
}
