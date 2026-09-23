using Godot;

namespace Phagocyte.Combat;

/// <summary>
/// Shared confocal laser-glow renderer (Phase 4): every weapon beam draws the
/// same three passes — wide fluorophore halo, saturated mid glow, white-hot
/// excitation core — so Perforin Lance, ROS jets, grasp chains and MAC blasts
/// read as one coherent laser family under the HDR bloom in
/// <c>main.tscn:Environment_main</c>. Transient visuals call these from their
/// own <c>_Draw</c> (scene-vs-code rule: effect nodes stay in code).
/// </summary>
public static class LaserGlow
{
    /// <summary>Beam in local space: halo + mid + white-hot core.</summary>
    public static void DrawBeam(
        CanvasItem canvas, Vector2 from, Vector2 to,
        Color halo, Color core, float width, float alpha)
    {
        Color haloCol = new(halo.R, halo.G, halo.B, alpha * 0.30f);
        Color midCol = new(halo.R, halo.G, halo.B, alpha * 0.65f);
        Color coreCol = new(core.R, core.G, core.B, alpha * 0.95f);
        canvas.DrawLine(from, to, haloCol, width * 2.2f);
        canvas.DrawLine(from, to, midCol, width * 1.15f);
        canvas.DrawLine(from, to, coreCol, width * 0.45f);
    }

    /// <summary>Impact halo: expanding-style ring + hot fill + white spark.</summary>
    public static void DrawImpactHalo(
        CanvasItem canvas, Vector2 pos, float radius,
        Color halo, Color core, float alpha, float ringWidth = 3.0f)
    {
        canvas.DrawCircle(pos, radius * 0.55f, new Color(core.R, core.G, core.B, alpha * 0.35f));
        canvas.DrawArc(pos, radius, 0.0f, Mathf.Tau, 40,
            new Color(halo.R, halo.G, halo.B, alpha * 0.85f), ringWidth);
        canvas.DrawCircle(pos, radius * 0.16f, new Color(1.0f, 1.0f, 1.0f, alpha * 0.9f));
    }
}
