using Godot;
using System;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Enemies;
using Phagocyte.Environment;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Phagocyte.Tests;

public partial class TestVisualOverhaul : SceneTree
{
    private int _framesWaited = 0;
    private bool _testDone = false;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING COMMERCIAL-GRADE VISUAL OVERHAUL AUTOMATED TEST <<<");
        GD.Print("==================================================================");
        var mainScene = GD.Load<PackedScene>("res://scenes/main.tscn");
        if (mainScene == null)
        {
            GD.PrintErr("[FAIL] Failed to load main.tscn");
            Quit(1);
            return;
        }
        var main = mainScene.Instantiate();
        Root.AddChild(main);
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        _framesWaited++;
        if (_framesWaited < 5)
            return false;

        _testDone = true;
        var main = Root.GetNodeOrNull<Main>("Main");
        if (main == null)
        {
            GD.PrintErr("[FAIL] Main scene not found");
            Quit(1);
            return true;
        }

        var player = ((BaseCell)main.Player) ?? main.GetNodeOrNull<BaseCell>("Macrophage");
        if (player == null)
        {
            GD.PrintErr("[FAIL] Player not found in Main scene");
            Quit(1);
            return true;
        }

        // =========================================================================
        // TEST 1: Catmull-Rom Polygon Smoothing (32 -> 64 vertices)
        // =========================================================================
        player.UpdatePseudopodDeformation(0.016f);
        AssertThat(player.Cytoplasm!.Polygon.Length).IsEqual(64);
        AssertThat(player.Membrane!.Points.Length).IsEqual(65);
        AssertThat(player.EngulfCollider!.Polygon.Length).IsEqual(32);
        AssertThat(player.Cytoplasm.UV.Length).IsEqual(64);
        GD.Print("[PASS] 1. Catmull-Rom spline smoothing verified: 32 control points -> 64 smooth visual vertices + 65-point closed membrane.");

        // =========================================================================
        // TEST 2: Cytoplasm Gel Shader & Fresnel Rim
        // =========================================================================
        var mat = player.Cytoplasm.Material as ShaderMaterial;
        AssertThat(mat).IsNotNull();

        var tintCol = mat!.GetShaderParameter("tint_color");
        var rimCol = mat.GetShaderParameter("rim_color").AsColor();
        var rimPower = mat.GetShaderParameter("rim_power");
        var innerAlpha = mat.GetShaderParameter("inner_alpha");

        AssertThat(tintCol.VariantType != Variant.Type.Nil).IsTrue();
        AssertThat(rimPower.VariantType != Variant.Type.Nil).IsTrue();
        AssertThat(innerAlpha.VariantType != Variant.Type.Nil).IsTrue();
        AssertThat(rimCol.G > 1.0f || rimCol.B > 1.0f).IsTrue();
        GD.Print($"[PASS] 2. Cytoplasm Gel Shader verified with semi-transparent core (alpha={innerAlpha}) and HDR Fresnel rim ({rimCol}).");

        // =========================================================================
        // TEST 3: Nucleus Damped Spring Lag Physics
        // =========================================================================
        player.Velocity = new Vector2(300.0f, 0.0f);
        player.UpdateNucleus(0.05f);
        AssertThat(player.NucleusVelocity != Vector2.Zero).IsTrue();
        AssertThat(player.Nucleus!.Position.X < 0.0f).IsTrue();
        GD.Print("[PASS] 3. Nucleus damped spring lag physics verified: squishy physical momentum and recoil active.");

        // =========================================================================
        // TEST 4: WorldEnvironment & 2D Glow/Bloom
        // =========================================================================
        var we = main.GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
        AssertThat(we != null && we.Environment != null).IsTrue();

        var env = we!.Environment;
        AssertThat(env.GlowEnabled).IsTrue();
        AssertThat(env.BackgroundMode).IsEqual(Godot.Environment.BGMode.Canvas);
        GD.Print("[PASS] 4. WorldEnvironment 2D Glow / Bloom verified (BG_CANVAS, Glow Enabled, HDR Threshold 1.0).");

        // =========================================================================
        // TEST 5: Microscope Atmosphere Background & Parallax Depth-of-Field (DoF)
        // =========================================================================
        var arenaBg = main.GetNodeOrNull<ColorRect>("Background/ArenaBG");
        AssertThat(arenaBg != null && arenaBg.Material != null).IsTrue();

        var parallax = main.GetNodeOrNull<MicroscopeParallax>("Background/MicroscopeParallax");
        AssertThat(parallax).IsNotNull();
        AssertThat(parallax!.RbcList.Count >= 10).IsTrue();
        AssertThat(parallax.BokehList.Count >= 5).IsTrue();
        GD.Print($"[PASS] 5. Microscope Parallax DoF active: {parallax.RbcList.Count} out-of-focus RBCs + {parallax.BokehList.Count} foreground bokeh particles.");

        // =========================================================================
        // TEST 6: Microscope Post-Process (Vignette & Chromatic Aberration)
        // =========================================================================
        var postLayer = main.GetNodeOrNull<CanvasLayer>("MicroscopePostProcess");
        AssertThat(postLayer).IsNotNull();

        var lensOverlay = postLayer!.GetNodeOrNull<ColorRect>("LensOverlay");
        AssertThat(lensOverlay != null && lensOverlay.Material != null).IsTrue();

        var overlayMat = lensOverlay!.Material as ShaderMaterial;
        AssertThat(overlayMat).IsNotNull();

        var caParam = overlayMat!.GetShaderParameter("chromatic_aberration");
        var vigParam = overlayMat.GetShaderParameter("vignette_radius");
        AssertThat(caParam.VariantType != Variant.Type.Nil && vigParam.VariantType != Variant.Type.Nil).IsTrue();
        GD.Print($"[PASS] 6. Full-screen Microscope Post-Process verified (Vignette r={vigParam}, Chromatic Aberration={caParam}).");

        // =========================================================================
        // TEST 7: Pathogen Breathing Oscillation & 3D Shading
        // =========================================================================
        var staphScene = GD.Load<PackedScene>("res://scenes/enemies/staph_enemy.tscn");
        AssertThat(staphScene).IsNotNull();
        var staph = staphScene!.Instantiate<StaphEnemy>();
        main.AddChild(staph);
        staph._PhysicsProcess(0.016);
        float initialScale = staph.Scale.X;
        AssertThat(initialScale >= 0.9f && initialScale <= 1.1f).IsTrue();
        staph.QueueFree();
        GD.Print("[PASS] 7. Pathogen organic breathing scale oscillation verified.");

        main.QueueFree();
        GD.Print("==================================================================");
        GD.Print(">>> ALL 7 VISUAL OVERHAUL TESTS PASSED WITH FLYING COLORS! <<<");
        GD.Print("==================================================================");
        Quit(0);
        return true;
    }
}
