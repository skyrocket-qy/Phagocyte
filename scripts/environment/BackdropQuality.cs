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
    }
}
