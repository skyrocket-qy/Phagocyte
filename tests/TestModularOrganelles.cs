using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Enemies;
using Phagocyte.Organelles;
using Phagocyte.Player;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the Modular Organelles (PseudopodLimb IK grabber + ReceptorSpikes ring).
/// </summary>
[TestSuite]
public partial class TestModularOrganelles : SceneTree
{
    private int _frame = 0;
    private bool _done = false;
    private Node2D? _container;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING MODULAR ORGANELLES VERIFICATION <<<");
        GD.Print("==================================================================");
    }

    public override bool _Process(double delta)
    {
        if (_done)
            return true;

        _frame++;
        if (_frame < 3)
            return false;

        _done = true;
        try
        {
            RunTests();
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestModularOrganelles threw: ", ex);
            Quit(1);
            return true;
        }

        GD.Print("==================================================================");
        GD.Print(">>> ALL MODULAR ORGANELLE TESTS PASSED SUCCESSFULLY! <<<");
        GD.Print("==================================================================");
        Quit(0);
        return true;
    }

    private void RunTests()
    {
        _container = new Node2D { Name = "OrganelleTestContainer" };
        Root.AddChild(_container);

        var limbScene = GD.Load<PackedScene>("res://scenes/skills/PseudopodLimb.tscn");
        var spikeScene = GD.Load<PackedScene>("res://scenes/skills/ReceptorSpikes.tscn");
        AssertThat(limbScene).IsNotNull();
        AssertThat(spikeScene).IsNotNull();

        var cell = new BaseCell { Name = "OrganelleHost", GlobalPosition = new Vector2(500, 500) };
        _container!.AddChild(cell);
        AssertThat(cell.Stats).IsNotNull();

        TestPseudopodLimb(limbScene!, cell);
        TestReceptorSpikes(spikeScene!, cell);

        _container.QueueFree();
    }

    private void TestPseudopodLimb(PackedScene scene, BaseCell cell)
    {
        var limb = scene.Instantiate<PseudopodLimb>();
        cell.AddChild(limb);
        limb.AttachTo(cell);

        AssertThat(limb.IsAttached).IsTrue();
        AssertThat(limb.Host).IsEqual(cell);
        AssertThat(limb.State).IsEqual(PseudopodLimb.LimbState.Retracted);
        AssertThat(limb.GetNodeOrNull<Line2D>("Chain")).IsNotNull();

        limb.BaseDamage = 0.1f;

        var enemy = new StaphEnemy
        {
            GlobalPosition = cell.GlobalPosition + new Vector2(120.0f, 0.0f)
        };
        _container!.AddChild(enemy);

        limb.ForceLaunch(enemy);
        AssertThat(limb.State).IsEqual(PseudopodLimb.LimbState.Extending);

        bool grabbed = false;
        float maxTipDistance = 0.0f;
        for (int i = 0; i < 180 && !grabbed; i++)
        {
            limb._PhysicsProcess(1.0 / 60.0);
            maxTipDistance = Mathf.Max(maxTipDistance, limb.TipDistance);
            grabbed = limb.GrabbedTarget == enemy;
        }

        AssertThat(grabbed).IsTrue();
        AssertThat(maxTipDistance).IsGreater(cell.CurrentRadius);
        GD.Print($"[PASS] PseudopodLimb IK chain extended to {maxTipDistance:F1}px and grabbed a pathogen.");

        bool engulfed = false;
        for (int i = 0; i < 240 && !engulfed; i++)
        {
            limb._PhysicsProcess(1.0 / 60.0);
            engulfed = enemy.IsBeingEaten || !GodotObject.IsInstanceValid(enemy);
        }

        AssertThat(engulfed).IsTrue();
        AssertThat(limb.State).IsEqual(PseudopodLimb.LimbState.Cooldown);
        AssertThat(limb.CooldownTimer).IsGreater(0.0f);
        GD.Print("[PASS] PseudopodLimb dragged the grabbed pathogen back into the cell body and entered cooldown.");

        limb.Detach();
    }

    private void TestReceptorSpikes(PackedScene scene, BaseCell cell)
    {
        var spikes = scene.Instantiate<ReceptorSpikes>();
        cell.AddChild(spikes);
        spikes.AttachTo(cell);

        AssertThat(spikes.IsAttached).IsTrue();
        AssertThat(spikes.Host).IsEqual(cell);

        spikes.ContactDamage = 5.0f;
        spikes.HitInterval = 0.55f;
        spikes.StunOnIntercept = 0.0f;

        AssertThat(spikes.CurrentRingRadius(1.0f)).IsGreater(cell.CurrentRadius);

        var enemy = new StaphEnemy
        {
            GlobalPosition = cell.GlobalPosition + new Vector2(spikes.CurrentRingRadius(1.0f) + 4.0f, 0.0f)
        };
        _container!.AddChild(enemy);
        float healthBefore = enemy.CurrentHealth;

        float startAngle = spikes.Angle;
        for (int i = 0; i < 300 && spikes.InterceptedCount == 0; i++)
        {
            spikes._PhysicsProcess(1.0 / 60.0);
        }

        AssertThat(spikes.Angle).IsNotEqual(startAngle);
        AssertThat(spikes.InterceptedCount).IsGreater(0);
        AssertThat(enemy.CurrentHealth).IsLess(healthBefore);
        GD.Print($"[PASS] ReceptorSpikes rotated (angle={spikes.Angle:F2}) and intercepted a pathogen for contact damage.");

        spikes.Detach();
    }
}
