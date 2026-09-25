using Godot;

namespace Phagocyte.Environment;

/// <summary>
/// Fullscreen fill-rate switch (FPS survey §1). Performance mode drops the
/// three always-on fullscreen costs — 2D glow/bloom, the microscope
/// postprocess overlay and the animated tissue backdrop shader — while the
/// default path stays pixel-identical. Originals are stashed in node metadata
/// on first toggle so the switch is reversible mid-run. All lookups are
/// null-guarded so menu scenes (no backdrop nodes) no-op.
/// </summary>
public static class BackdropQuality
{
    public static void ApplyTo(Node? root, bool performanceMode)
    {
        if (root == null)
            return;

        var bg = root.GetNodeOrNull<ColorRect>("Background/ArenaBG");
        if (bg != null)
        {
            if (performanceMode)
            {
                if (!bg.HasMeta("full_material") && bg.Material != null)
                    bg.SetMeta("full_material", bg.Material);
                bg.Material = null;
            }
            else if (bg.HasMeta("full_material"))
            {
                bg.Material = bg.GetMeta("full_material").AsGodotObject() as Material;
                bg.RemoveMeta("full_material");
            }
        }

        var overlay = root.GetNodeOrNull<ColorRect>("MicroscopePostProcess/LensOverlay");
        if (overlay != null)
        {
            if (performanceMode)
            {
                if (!overlay.HasMeta("full_visible"))
                    overlay.SetMeta("full_visible", overlay.Visible);
                overlay.Visible = false;
            }
            else if (overlay.HasMeta("full_visible"))
            {
                overlay.Visible = overlay.GetMeta("full_visible").AsBool();
                overlay.RemoveMeta("full_visible");
            }
        }

        var worldEnv = root.GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
        var env = worldEnv?.Environment;
        if (env != null)
        {
            if (performanceMode)
            {
                if (worldEnv != null && !worldEnv.HasMeta("full_glow"))
                    worldEnv.SetMeta("full_glow", env.GlowEnabled);
                env.GlowEnabled = false;
            }
            else if (worldEnv != null && worldEnv.HasMeta("full_glow"))
            {
                env.GlowEnabled = worldEnv.GetMeta("full_glow").AsBool();
                worldEnv.RemoveMeta("full_glow");
            }
        }

        // Mid-layer procedural backdrops (30 Hz redraws each): freeze their
        // simulation so they persist as a static backdrop at zero per-frame
        // cost. Not hidden — the frozen frame keeps the tissue read.
        var parallax = root.GetNodeOrNull<Node2D>("Background/MicroscopeParallax");
        if (parallax != null)
            parallax.SetProcess(!performanceMode);
        var tissue = root.GetNodeOrNull<Node2D>("Background/CapillaryTissue");
        if (tissue != null)
            tissue.SetProcess(!performanceMode);

        // Ambient CPU particles: stop emission in performance mode (in-flight
        // particles drain, sim idles). Stashed in metadata for reversibility.
        // Accessed as untyped Node (property "emitting") to avoid a hard
        // dependency on the particle class binding.
        var fluid = root.GetNodeOrNull<Node>("Background/FluidParticles");
        if (fluid != null)
        {
            if (performanceMode)
            {
                if (!fluid.HasMeta("full_emitting"))
                    fluid.SetMeta("full_emitting", fluid.Get("emitting").AsBool());
                fluid.Set("emitting", false);
            }
            else if (fluid.HasMeta("full_emitting"))
            {
                fluid.Set("emitting", fluid.GetMeta("full_emitting").AsBool());
                fluid.RemoveMeta("full_emitting");
            }
        }
    }
}
