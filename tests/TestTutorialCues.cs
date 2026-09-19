using Godot;
using Godot.Collections;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Endgame;
using Phagocyte.Player;
using Phagocyte.Skills;
using Phagocyte.UI;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the five first-run micro-cues and their supporting systems
/// (docs/tutorial.md Â§2): Squeeze Mode, catalyst resonance pairing, opening
/// guide pathogens, WASD ring timing, squeeze hint and fluid arrow field data.
/// </summary>
[TestSuite]
public partial class TestTutorialCues : TestHarness
{
    private int _phase = 0;
    private Main? _main = null;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING FIRST-RUN MICRO-CUE VERIFICATION <<<");
        GD.Print("==================================================================");
        Paused = false;
        Engine.TimeScale = 1.0;
    }

    public override bool _Process(double delta)
    {
        try
        {
            switch (_phase)
            {
            case 0:
                _phase++;
                return false; // let the scene tree settle
            case 1:
                RunSqueezeModeTests();
                _phase++;
                return false;
            case 2:
                RunCatalystResonanceTests();
                _phase++;
                return false;
            case 3:
                RunWorldCueTests();
                _phase++;
                return false;
            default:
                Cleanup();
                GD.Print("==================================================================");
                GD.Print(">>> ALL FIRST-RUN MICRO-CUE TESTS PASSED SUCCESSFULLY! <<<");
                GD.Print("==================================================================");
                Quit(0);
                return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestTutorialCues threw: ", ex);
            Cleanup();
            Quit(1);
            return true;
        }
    }

    private void RunSqueezeModeTests()
    {
        AfflictionManager.Clear();

        var cellScene = GD.Load<PackedScene>("res://scenes/characters/macrophage.tscn");
        var cell = cellScene.Instantiate<Macrophage>();
        Root.AddChild(cell);
        AssertThat(cell.Stats).IsNotNull();

        cell._PhysicsProcess(0.02);
        AssertThat(cell.IsSqueezing).IsFalse();

        float baseSpeed = cell.Stats!.GetStat("move_speed");
        float baseRadius = cell.BaseRadius * cell.Stats.GetStat("area");

        // Hold Space -> compress 40%, +25% speed, engulf disabled
        Input.ActionPress("squeeze_mode");
        cell._PhysicsProcess(0.02);
        AssertThat(cell.IsSqueezing).IsTrue();
        AssertThat(cell.CurrentRadius).IsEqualApprox(baseRadius * BaseCell.SqueezeRadiusFactor, 0.5f);
        AssertThat(cell.CurrentSpeed).IsEqualApprox(baseSpeed * (1.0f + BaseCell.SqueezeSpeedBonus), 0.5f);

        var dummy = new StaphEnemy { GlobalPosition = cell.GlobalPosition, FibrinShield = 0 };
        Root.AddChild(dummy);
        int digestedBefore = cell.DigestedCount;
        cell.ConsumePathogen(dummy);
        AssertThat(cell.DigestedCount).IsEqual(digestedBefore);

        // Release -> restore
        Input.ActionRelease("squeeze_mode");
        cell._PhysicsProcess(0.02);
        AssertThat(cell.IsSqueezing).IsFalse();
        AssertThat(cell.CurrentRadius).IsEqualApprox(baseRadius, 0.5f);
        dummy.QueueFree();

        // Microtubule Sclerosis (endless affliction) hard-disables the mode
        AfflictionManager.SetSelection(new[] { AfflictionManager.MicrotubuleSclerosis });
        Input.ActionPress("squeeze_mode");
        cell._PhysicsProcess(0.02);
        AssertThat(cell.IsSqueezing).IsFalse();
        Input.ActionRelease("squeeze_mode");
        AfflictionManager.Clear();
        cell.QueueFree();

        GD.Print("[PASS] Squeeze Mode compress/speed/engulf-lock and affliction override verified.");
    }

    private void RunCatalystResonanceTests()
    {
        var probe = new BaseCell { Name = "CatalystProbe", GlobalPosition = new Vector2(60, 60) };
        Root.AddChild(probe);

        var sm = new SkillManager { Name = "SkillManager" };
        probe.AddChild(sm);
        sm.Setup(probe);

        var lance = new PerforinLanceSkill();
        AssertThat(sm.EquipActive(lance)).IsTrue();

        AssertThat(UpgradeManager.IsCatalystReady(probe, "passive_lysosome", out _)).IsFalse();
        while (lance.Level < UpgradeManager.CatalystActiveLevel)
            lance.Upgrade();

        AssertThat(lance.Level).IsEqual(UpgradeManager.CatalystActiveLevel);
        AssertThat(UpgradeManager.IsCatalystReady(probe, "passive_lysosome", out string pairedActive)).IsTrue();
        AssertThat(pairedActive).IsEqual("perforin_lance");
        AssertThat(UpgradeManager.IsCatalystReady(probe, "passive_actin", out _)).IsFalse();

        // The corresponding passive card renders the golden aura + catalyst tag
        var choices = new Array<Dictionary>
        {
            new Dictionary
            {
                { "type", "new_passive" }, { "id", "passive_lysosome" },
                { "name", "TREE_NODE_LYSOSOME_NAME" }, { "icon", "ðŸ§ª" },
                { "level", 1 }, { "badge", "NEW PASSIVE" },
                { "desc", "TREE_NODE_LYSOSOME_DESC" }, { "catalyst", true }
            },
            new Dictionary
            {
                { "type", "heal_fallback" }, { "id", "heal_a" },
                { "name", "FALLBACK" }, { "icon", "ðŸ’š" },
                { "level", 0 }, { "badge", "HEAL" }, { "desc", "heal" }
            },
            new Dictionary
            {
                { "type", "heal_fallback" }, { "id", "heal_b" },
                { "name", "FALLBACK" }, { "icon", "ðŸ’š" },
                { "level", 0 }, { "badge", "HEAL" }, { "desc", "heal" }
            }
        };

        var modalScene = GD.Load<PackedScene>("res://scenes/ui/upgrade_modal.tscn");
        var modal = modalScene.Instantiate<UpgradeModal>();
        Root.AddChild(modal);
        modal.ShowChoices(choices);
        AssertThat(modal.Visible).IsTrue();

        var card0 = modal.CardsContainer!.GetChild<Control>(0);
        var badge = card0.GetNodeOrNull<Label>("VBox/BadgeLabel");
        AssertThat(badge).IsNotNull();
        AssertThat(badge!.Text.Contains(modal.Tr("BADGE_CATALYST"))).IsTrue();

        var aura = card0.GetNodeOrNull<Panel>("CatalystAura");
        AssertThat(aura).IsNotNull();
        AssertThat(aura!.Visible).IsTrue();

        var card1 = modal.CardsContainer!.GetChild<Control>(1);
        var aura1 = card1.GetNodeOrNull<Panel>("CatalystAura");
        AssertThat(aura1 == null || !aura1.Visible).IsTrue();

        Paused = false;
        modal.QueueFree();
        probe.QueueFree();

        GD.Print("[PASS] Catalyst pairing detection and golden resonance card rendering verified.");
    }

    private void RunWorldCueTests()
    {
        GameManager.SelectedClass = "macrophage";
        GameManager.SelectedMap = "acute_wound";

        var main = GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>();
        _main = main;
        Root.AddChild(main);
        main.SetPhysicsProcess(false);

        // Cue 1: two dormant guide staphylococci ahead of the player
        int dormantGuides = 0;
        foreach (var child in main.EnemyContainer!.GetChildren())
        {
            if (child is BaseEnemy be && be.StunTimer > 100.0f)
                dormantGuides++;
        }
        AssertThat(main.EnemyContainer.GetChildCount()).IsGreater(0);
        AssertThat(dormantGuides).IsEqual(2);

        // Cue 5: map fluid field data is exposed for the arrow overlay
        main._PhysicsProcess(0.02f);
        AssertThat(main.CurrentFluidVector.LengthSquared()).IsGreater(0.0f);

        // Hud cues: opening WASD ring expires after 5s, fluid field flagged
        var hud = main.HudNode;
        AssertThat(hud).IsNotNull();
        hud!._Process(0.1);
        AssertThat(hud.TutorialOverlayNode).IsNotNull();
        AssertThat(hud.TutorialOverlayNode!.ShowMoveCue).IsTrue();
        AssertThat(hud.TutorialOverlayNode.FluidFieldActive).IsTrue();

        hud._Process(Hud.MoveCueSeconds);
        AssertThat(hud.TutorialOverlayNode.ShowMoveCue).IsFalse();

        // Cue 3: squeeze hint fires once per run
        hud.ResetTutorialCues();
        AssertThat(hud.SqueezeHintShownOnce).IsFalse();
        hud.ShowSqueezeHint();
        AssertThat(hud.SqueezeHintShownOnce).IsTrue();
        hud.ShowSqueezeHint();
        AssertThat(hud.SqueezeHintShownOnce).IsTrue();

        GD.Print("[PASS] Guide pathogens, WASD cue window, squeeze hint and fluid field verified.");
    }

    private void Cleanup()
    {
        Paused = false;
        Engine.TimeScale = 1.0;
        AfflictionManager.Clear();
        Input.ActionRelease("squeeze_mode");

        if (_main != null && IsInstanceValid(_main))
        {
            if (_main.GetParent() != null)
                _main.GetParent().RemoveChild(_main);
            _main.Free();
            _main = null;
        }
    }
}
