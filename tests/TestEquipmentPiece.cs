using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Game.Enemies;
using Game.Equipment;
using Game.Player;

using Game.Core;

namespace Game.Tests;

/// <summary>
/// Verifies the Modular Equipment (ContactSpikes ring).
/// The PseudopodLimb IK grabber was merged into Phagocytic Grasp's
/// chain-strike delivery and deleted.
/// </summary>
[TestSuite]
public partial class TestEquipmentPiece : SceneTree
{
    private int _frame = 0;
    private bool _done = false;
    private Node2D? _container;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING MODULAR GEAR VERIFICATION <<<");
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
            GD.PrintErr("[FAIL] TestEquipmentPiece threw: ", ex);
            Quit(1);
            return true;
        }

        GD.Print("==================================================================");
        GD.Print(">>> ALL MODULAR GEAR TESTS PASSED SUCCESSFULLY! <<<");
        GD.Print("==================================================================");
        Quit(0);
        return true;
    }

    private void RunTests()
    {
        _container = new Node2D { Name = "GearTestContainer" };
        Root.AddChild(_container);

        var spikeScene = AssetLoader.Load<PackedScene>("res://scenes/skills/ContactSpikes.tscn");
        AssertThat(spikeScene).IsNotNull();

        var cell = new PlayerActor { Name = "GearHost", GlobalPosition = new Vector2(500, 500) };
        _container!.AddChild(cell);
        AssertThat(cell.Stats).IsNotNull();

        TestContactSpikes(spikeScene!, cell);

        _container.QueueFree();
    }

    private void TestContactSpikes(PackedScene scene, PlayerActor cell)
    {
        var spikes = scene.Instantiate<ContactSpikes>();
        cell.AddChild(spikes);
        spikes.AttachTo(cell);

        AssertThat(spikes.IsAttached).IsTrue();
        AssertThat(spikes.Host).IsEqual(cell);

        spikes.ContactDamage = 5.0f;
        spikes.HitInterval = 0.55f;
        spikes.StunOnIntercept = 0.0f;

        AssertThat(spikes.CurrentRingRadius(1.0f)).IsGreater(cell.CurrentRadius);

        var enemy = EnemySpawner.CreateEnemy("staph")!;
        enemy.GlobalPosition = cell.GlobalPosition + new Vector2(spikes.CurrentRingRadius(1.0f) + 4.0f, 0.0f);
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
        GD.Print($"[PASS] ContactSpikes rotated (angle={spikes.Angle:F2}) and intercepted a pathogen for contact damage.");

        spikes.Detach();
    }
}
