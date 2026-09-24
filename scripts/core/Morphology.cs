using Godot;

namespace Phagocyte.Core;

/// <summary>
/// Canonical real-world to game-world size map (the "A formula").
///
/// gameRadius = round(16.3 * realMicrons^0.3), floored at 6px for visibility.
/// Anchored on the macrophage (20um -> 40px). The power-law compresses three
/// orders of magnitude of biology (0.005um prions .. 20um macrophages) into
/// one order of playable pixels (6 .. 40) while preserving rank order.
///
/// RULE: every new enemy and playable cell MUST derive its collision radius
/// through <see cref="RealSizeToRadius"/> with its real-world characteristic
/// size in micrometres. Fictional composites (bosses, elites) and VFX areas
/// (pellets, clouds, hazard pools) are the only exceptions and keep
/// hand-tuned values.
/// </summary>
public static class Morphology
{
    public const float AnchorK = 16.3f;
    public const float AnchorExponent = 0.3f;

    /// <summary>Nothing renders or clicks below this radius; prions live here.</summary>
    public const float MinVisibleRadius = 6.0f;

    public static float RealSizeToRadius(float microns)
    {
        float r = AnchorK * Mathf.Pow(microns, AnchorExponent);
        return Mathf.Max(MinVisibleRadius, Mathf.Round(r));
    }
}
