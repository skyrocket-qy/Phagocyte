using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;
using Phagocyte.UI;

namespace Phagocyte.Tests;

[TestSuite]
public partial class TestSurvivorHudUx : TestHarness
{
    private int _framesWaited = 0;
    private bool _testDone = false;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING SURVIVOR-LIKE HUD & UX OVERHAUL VERIFICATION <<<");
        GD.Print("==================================================================");

        var mainScene = AssetLoader.Load<PackedScene>("res://scenes/main.tscn");
        AssertThat(mainScene).IsNotNull();
        var main = mainScene.Instantiate();
        Root.AddChild(main);
    }

    private bool _isolated = false;

    /// <summary>
    /// Freeze the spawner driver, clear everything Main._Ready spawned and
    /// restore the pristine player baseline. Must run on a live frame: nodes
    /// added during MainLoop._Initialize don't enter the tree (no _Ready)
    /// until the first iteration, so _Initialize-time isolation is a no-op.
    /// Afterwards nothing can accrue ambient EXP/damage/kills.
    /// </summary>
    private void IsolateArena()
    {
        var main = Root.GetNodeOrNull<Main>("Main");
        if (main == null)
            return;
        main.SetPhysicsProcess(false);
        ClearArenaEntities(main);
        var player = main.GetNodeOrNull<Macrophage>("Macrophage");
        if (player != null)
        {
            player.CurrentLevel = 1;
            player.CurrentExp = 0.0f;
            player.ExpToNextLevel = 30.0f;
            player.Health = player.MaxHealth;
            // The cell's own physics ticks auto-firing skills; a kill would
            // award ambient EXP and open the draft modal (pausing the tree),
            // which flips the HUD back to its hover alpha.
            player.SetPhysicsProcess(false);
        }
        // The bar renders a cached snapshot fed by the ExpChanged signal, so a
        // direct player reset alone leaves stale values on screen: reset the
        // snapshot through its public setters and refresh the display.
        var hud = main.GetNodeOrNull<Hud>("HUD");
        if (hud != null)
        {
            hud.LastCurrentExp = 0.0f;
            hud.LastExpToNext = 30.0f;
            hud.LastLevel = 1;
            hud.UpdateExpDisplay();
            if (hud.CellUpgradeModal != null)
                hud.CellUpgradeModal.Visible = false;
            hud.ResetTutorialCues();
        }
        Paused = false;
        Engine.TimeScale = 1.0;
    }

    private static void ClearArenaEntities(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is BaseEnemy || child is SenescentRBC || child is DormantToxinVesicle || child is BioHazardArea)
                child.Free();
            else
                ClearArenaEntities(child);
        }
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        if (!_isolated)
        {
            _isolated = true;
            IsolateArena();
        }

        _framesWaited++;
        if (_framesWaited < 5)
            return false;

        _testDone = true;
        // Re-run the isolation at the assertion frame: an ambient level-up
        // during the settle frames would open the draft modal and pause the
        // tree before these HUD assertions (docs/AGENTS.md determinism rules).
        IsolateArena();
        var main = Root.GetNodeOrNull<Node2D>("Main");
        AssertThat(main).IsNotNull();

        var player = main!.GetNodeOrNull<Macrophage>("Macrophage");
        AssertThat(player).IsNotNull();

        var hud = main.GetNodeOrNull<Hud>("HUD");
        AssertThat(hud).IsNotNull();

        // =====================================================================
        // TEST 1: Bottom-Edge Full-Width EXP Progress Bar
        // =====================================================================
        AssertThat(hud!.BottomExpBar).IsNotNull();
        AssertThat(hud.BottomExpBar!.AnchorRight).IsEqual(1.0f);
        AssertThat(hud.BottomExpBar.AnchorBottom).IsEqual(1.0f);
        AssertThat(hud.BottomExpBar.CustomMinimumSize.Y).IsEqual(7.0f);
        AssertThat(hud.BottomExpBar.Value).IsEqual(0.0f);

        player!.AddExp(15.0f);
        hud.UpdateExpDisplay();
        AssertThat(hud.BottomExpBar.Value).IsEqual(15.0f);
        AssertThat(hud.BottomExpBar.MaxValue).IsEqual(30.0f);
        GD.Print("[PASS] 1. Bottom-edge full-width EXP bar verified (AnchorBottom=1.0, AnchorRight=1.0, 7px bio-cyan fill, dynamic value sync).");

        // =====================================================================
        // TEST 2: Center-Top Stopwatch & Kill Count Capsule
        // =====================================================================
        AssertThat(hud.TopCenterCapsule).IsNotNull();
        AssertThat(hud.TimerLabel).IsNotNull();
        AssertThat(hud.KillLabel).IsNotNull();
        AssertThat(hud.TimerLabel!.Text.Contains("\u23F1\uFE0F")).IsTrue();
        AssertThat(hud.KillLabel!.Text.Contains("\U0001F480")).IsTrue();
        AssertThat(hud.KillLabel.Text.Contains("0")).IsTrue();

        // Simulate digestion / kill increment
        player.DigestedCount = 4;
        hud.ConnectPlayer(player);
        AssertThat(hud.KillLabel.Text.Contains("4")).IsTrue();
        GD.Print("[PASS] 2. Center-top stopwatch capsule & kill count anchor verified.");

        // =====================================================================
        // TEST 3: Top-Left HUD Health Bar & Dodge Ring (no follower arc)
        // =====================================================================
        var dodgeRing = player.GetNodeOrNull<Node2D>("DodgeRing");
        AssertThat(dodgeRing).IsNotNull();
        AssertThat(player.GetNodeOrNull<Node2D>("ArcHealthBar")).IsNull();

        // HUD bar is wired and shows full health.
        AssertThat(hud.HpBar).IsNotNull();
        AssertThat(hud.HpLabel).IsNotNull();
        AssertThat(player.Health).IsEqual(140.0f);
        AssertThat(hud.HpBar!.MaxValue).IsEqual(140.0f);
        AssertThat(hud.HpBar.Value).IsEqual(140.0f);

        // Take damage -> HUD bar drops & hit flash (Armor 10 => 10/60 DR)
        // Zero the innate block/evasion roll so the DR math below is deterministic.
        player.Stats!.SetBase("block", 0.0f);
        player.Stats.SetBase("evasion", 0.0f);
        player.TakeDamage(20.0f);
        AssertThat(player.Health).IsEqualApprox(140.0f - 20.0f * (1.0f - 10.0f / 60.0f), 0.01f);
        AssertThat(hud.HpBar.Value).IsEqualApprox(player.Health, 0.01f);
        AssertThat(hud.HpBar.MaxValue).IsEqual(140.0f);
        GD.Print("[PASS] 3. Top-left HUD health bar tracks damage; dodge ring kept, follower arc removed.");

        // =====================================================================
        // TEST 4: Critical HP (<30%) Vignette Heartbeat Pulse
        // =====================================================================
        AssertThat(hud.VignetteRect).IsNotNull();
        var vignetteMat = hud.VignetteRect!.Material as ShaderMaterial;
        AssertThat(vignetteMat).IsNotNull();

        // When health > 30%, pulse_intensity is 0.0
        hud._Process(0.016);
        float normalIntensity = vignetteMat!.GetShaderParameter("pulse_intensity").AsSingle();
        AssertThat(normalIntensity).IsEqual(0.0f);

        // Drop player health to 20% (<30%)
        player.Health = 20.0f;
        hud.LastHealth = 20.0f;
        hud.LastMaxHealth = 140.0f;
        hud._Process(0.016);
        float critIntensity = vignetteMat.GetShaderParameter("pulse_intensity").AsSingle();
        AssertThat(critIntensity > 0.0f).IsTrue();
        GD.Print($"[PASS] 4. Critical HP full-screen red vignette heartbeat pulse verified (Intensity={critIntensity:F2} > 0).");

        // Restore health -> pulse ceases
        player.Health = 140.0f;
        hud.LastHealth = 140.0f;
        hud._Process(0.016);
        AssertThat(vignetteMat.GetShaderParameter("pulse_intensity").AsSingle()).IsEqual(0.0f);

        // =====================================================================
        // TEST 5: Slim Skill Bar (40x36 micro-slots) & Dynamic Transparency
        // =====================================================================
        AssertThat(hud.SkillContainer).IsNotNull();
        AssertThat(hud.SlotsContainer).IsNotNull();
        AssertThat(hud.SlotsContainer!.GetChildCount()).IsEqual(10);

        var slot0 = hud.SlotsContainer.GetChild<Control>(0);
        AssertThat(slot0.CustomMinimumSize.X).IsEqual(40.0f);
        AssertThat(slot0.CustomMinimumSize.Y).IsEqual(36.0f);

        // Simulate combat idle transparency
        hud.HoveredSlotIdx = -1;
        for (int i = 0; i < 30; i++)
        {
            hud._Process(0.033);
        }
        float combatAlpha = hud.SkillContainer!.Modulate.A;
        AssertThat(combatAlpha <= 0.50f).IsTrue();

        // Simulate hover -> alpha restores to 1.0
        hud.HoveredSlotIdx = 0;
        for (int i = 0; i < 30; i++)
        {
            hud._Process(0.033);
        }
        float hoverAlpha = hud.SkillContainer.Modulate.A;
        AssertThat(hoverAlpha >= 0.90f).IsTrue();
        AssertThat(hud.SkillTitleLbl?.Visible ?? false).IsFalse();
        GD.Print($"[PASS] 5. Pure skill bar (TitleLabel hidden, 40x36 micro-slots & dynamic transparency: Combat alpha={combatAlpha:F2} -> Hover alpha={hoverAlpha:F2}) verified.");

        // =====================================================================
        // TEST 6: Tactical Telemetry & Buff/Debuff Monitor (Top-Left Buff Only)
        // =====================================================================
        AssertThat(hud.BuffTag).IsNotNull();
        var headerHBox = hud.GetNodeOrNull<Control>("MarginContainer/PanelContainer/VBoxContainer/HeaderHBox");
        var footerHBox = hud.GetNodeOrNull<Control>("MarginContainer/PanelContainer/VBoxContainer/FooterHBox");
        AssertThat(headerHBox?.Visible ?? true).IsFalse();
        AssertThat(footerHBox?.Visible ?? true).IsFalse();

        hud.UpdateLocalizedTexts();
        AssertThat(hud.BuffTag!.Visible).IsFalse();

        // Apply a Slow debuff
        player.ApplySlow(3.0f, 0.5f);
        hud._Process(0.016);
        AssertThat(hud.BuffTag.Visible).IsTrue();
        AssertThat(hud.BuffTag.Text.Contains("\U0001F40C")).IsTrue();
        GD.Print("[PASS] 6. Frameless Buff-only monitor verified (Hidden when calm, pops up on active Buff/Debuff).");

        GD.Print("==================================================================");
        GD.Print(">>> ALL SURVIVOR-LIKE HUD & UX TESTS PASSED SUCCESSFULLY! <<<");
        GD.Print("==================================================================");

        Quit(0);
        return true;
    }
}
