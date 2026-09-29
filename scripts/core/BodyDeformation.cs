using Godot;

namespace Game.Core;

/// <summary>Converts cell size in microns to collision radius in pixels.</summary>
public static class BodyDeformation
{
    public const float AnchorK = 16.3f;
    public const float AnchorExponent = 0.3f;
    public const float MinVisibleRadius = 6.0f;

    public static float RealSizeToRadius(float microns)
    {
        float r = AnchorK * Mathf.Pow(microns, AnchorExponent);
        return Mathf.Max(MinVisibleRadius, Mathf.Round(r));
    }
}
