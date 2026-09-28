using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Game.Combat;
using Game.Enemies;
using Game.Player;

namespace Game.Tests;

/// <summary>
/// Verifies the four-mode threat-intent steering model (docs/pathogen.md):
/// chemo-chase, interception, standoff artillery and tissue invasion.
/// </summary>
[TestSuite]
public partial class TestEnemySteering : SceneTree
{
    private int _frame = 0;
    private bool _done = false;
    private Node2D? _container;
    private PlayerActor? _player;

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
            GD.PrintErr("[FAIL] TestEnemySteering threw: ", ex);
            Quit(1);
            return true;
        }

        GD.Print("==================================================================");
        GD.Print(">>> ALL THREAT-INTENT STEERING TESTS PASSED SUCCESSFULLY! <<<");
        GD.Print("==================================================================");
        Quit(0);
        return true;
    }

    private void RunTests()
    {
        EnemySteering.ConfigureArena(new Vector2(2000, 2000));

        _container = new Node2D { Name = "SteeringTestContainer" };
        Root.AddChild(_container);
        _player = new PlayerActor { Name = "SteeringTestHost", GlobalPosition = new Vector2(600, 0) };
        _container.AddChild(_player);

        TestModeMapping();
        TestChemoChaserSeek();
        TestInterceptorLead();
        TestStandoffOrbit();
        TestStandoffPellets();
        TestTissueInvader();
        TestTetanusArtillery();

        _container.QueueFree();
    }

    private void TestModeMapping()
    {
        AssertThat(EnemySpawner.CreateEnemy("staph")!.ThreatMode).IsEqual(EnemyThreatMode.ChemoChaser);
        AssertThat(EnemySpawner.CreateEnemy("norovirus")!.ThreatMode).IsEqual(EnemyThreatMode.ChemoChaser);
        AssertThat(EnemySpawner.CreateEnemy("e_coli")!.ThreatMode).IsEqual(EnemyThreatMode.ChemoChaser);
        AssertThat(EnemySpawner.CreateEnemy("pseudomonas")!.ThreatMode).IsEqual(EnemyThreatMode.ChemoChaser);
        AssertThat(EnemySpawner.CreateEnemy("tb")!.ThreatMode).IsEqual(EnemyThreatMode.ChemoChaser);
        AssertThat(EnemySpawner.CreateEnemy("flu_drift")!.ThreatMode).IsEqual(EnemyThreatMode.ChemoChaser);
        AssertThat(EnemySpawner.CreateEnemy("ebola")!.ThreatMode).IsEqual(EnemyThreatMode.ChemoChaser);
        AssertThat(EnemySpawner.CreateEnemy("malignant_cell")!.ThreatMode).IsEqual(EnemyThreatMode.ChemoChaser);
        AssertThat(EnemySpawner.CreateEnemy("prion")!.ThreatMode).IsEqual(EnemyThreatMode.ChemoChaser);
        AssertThat(EnemySpawner.CreateEnemy("hiv")!.ThreatMode).IsEqual(EnemyThreatMode.Interceptor);
        AssertThat(EnemySpawner.CreateEnemy("rabies")!.ThreatMode).IsEqual(EnemyThreatMode.Interceptor);
        AssertThat(EnemySpawner.CreateEnemy("s_virus")!.ThreatMode).IsEqual(EnemyThreatMode.Standoff);
        AssertThat(EnemySpawner.CreateEnemy("tetanus")!.ThreatMode).IsEqual(EnemyThreatMode.Standoff);
        AssertThat(EnemySpawner.CreateEnemy("h_pylori")!.ThreatMode).IsEqual(EnemyThreatMode.Invader);
        AssertThat(EnemySpawner.CreateEnemy("anthrax_spore")!.ThreatMode).IsEqual(EnemyThreatMode.Drifter);
        AssertThat(EnemySpawner.CreateEnemy("toxoplasma")!.ThreatMode).IsEqual(EnemyThreatMode.Drifter);

        var chainLord = EnemySpawner.CreateSubBoss("acute_wound");
        AssertThat(chainLord).IsNotNull();
        AssertThat(chainLord!.ThreatMode).IsEqual(EnemyThreatMode.Interceptor);

        var terminal = EnemySpawner.CreateTerminalBoss("acute_wound");
        AssertThat(terminal).IsNotNull();
        AssertThat(terminal!.ThreatMode).IsEqual(EnemyThreatMode.Drifter);
        GD.Print("[PASS] Threat-mode assignment verified across the pathogen roster.");
    }

    private void TestChemoChaserSeek()
    {
        var chaser = EnemySpawner.CreateEnemy("staph")!;
        chaser.GlobalPosition = new Vector2(-300, 0);
        _container!.AddChild(chaser);

        Vector2 direction = EnemySteering.GetDirection(chaser, 0.016f);
        AssertThat(direction.LengthSquared() > 0.9f).IsTrue();
        AssertThat(direction.Dot(Vector2.Right)).IsGreater(0.9f);
        GD.Print("[PASS] Chemo-chaser seeks the cell along the shortest vector.");
        chaser.QueueFree();
    }

    private void TestInterceptorLead()
    {
        var interceptor = EnemySpawner.CreateEnemy("hiv")!;
        interceptor.GlobalPosition = Vector2.Zero;
        _container!.AddChild(interceptor);

        // Player moving right at 200px/s => aim 120px ahead (clamped lead)
        Vector2 direction = EnemySteering.AimAtIntercept(interceptor, new Vector2(300, 0), new Vector2(200, 0));
        AssertThat(direction.Dot(Vector2.Right)).IsGreater(0.98f);

        // Player moving down at 300px/s => aim (300, 180)
        Vector2 expected = new Vector2(300, 180).Normalized();
        direction = EnemySteering.AimAtIntercept(interceptor, new Vector2(300, 0), new Vector2(0, 300));
        AssertThat(direction.Dot(expected)).IsGreater(0.98f);
        GD.Print("[PASS] Interceptor aims 100-200px ahead of the player's travel vector.");
        interceptor.QueueFree();
    }

    private void TestStandoffOrbit()
    {
        _player!.GlobalPosition = Vector2.Zero;
        var artillery = EnemySpawner.CreateEnemy("s_virus")!;
        artillery.GlobalPosition = new Vector2(300, 0);
        artillery.SteeringOrbitSign = 1.0f;
        _container!.AddChild(artillery);

        // In-band: orbit tangentially
        Vector2 direction = EnemySteering.Standoff(artillery);
        AssertThat(Mathf.Abs(direction.Dot(Vector2.Left))).IsLess(0.2f);

        // Far: close in
        artillery.GlobalPosition = new Vector2(700, 0);
        direction = EnemySteering.Standoff(artillery);
        AssertThat(direction.Dot(Vector2.Left)).IsGreater(0.95f);

        // Crowded: back off
        artillery.GlobalPosition = new Vector2(100, 0);
        direction = EnemySteering.Standoff(artillery);
        AssertThat(direction.Dot(Vector2.Right)).IsGreater(0.95f);
        GD.Print("[PASS] Standoff artillery closes in, orbits at range and backs off when crowded.");
        artillery.QueueFree();
    }

    private void TestStandoffPellets()
    {
        _player!.GlobalPosition = new Vector2(600, 0);
        var artillery = EnemySpawner.CreateEnemy("s_virus")!;
        artillery.GlobalPosition = new Vector2(250, 0);
        _container!.AddChild(artillery);

        artillery._PhysicsProcess(2.6);
        EnemyProjectile? pellet = null;
        foreach (var child in _container.GetChildren())
        {
            if (child is EnemyProjectile found)
            {
                pellet = found;
                break;
            }
        }
        AssertThat(pellet).IsNotNull();
        AssertThat(pellet!.IsInGroup("enemy_shots")).IsTrue();

        // Soft cap holds under pressure
        for (int i = 0; i < EnemyProjectile.MaxActivePellets + 40; i++)
        {
            artillery._PhysicsProcess(2.6);
        }
        AssertThat(EnemyProjectile.ActiveCount).IsLessEqual(EnemyProjectile.MaxActivePellets);
        GD.Print($"[PASS] Standoff pellets spawn and the soft cap holds at {EnemyProjectile.MaxActivePellets}.");
        artillery.QueueFree();
        foreach (var child in _container.GetChildren())
        {
            // Immediate removal (not QueueFree): the freed-pending count must
            // not pollute the tetanus artillery test below.
            if (child is EnemyProjectile spent)
            {
                _container.RemoveChild(spent);
                spent.Free();
            }
        }
    }

    private void TestTissueInvader()
    {
        var invader = EnemySpawner.CreateEnemy("h_pylori")!;
        invader.GlobalPosition = Vector2.Zero;
        _container!.AddChild(invader);

        Vector2 anchor = EnemySteering.GetNearestTissueAnchor(invader.GlobalPosition);
        Vector2 direction = EnemySteering.GetDirection(invader, 0.016f);
        Vector2 expected = (anchor - invader.GlobalPosition).Normalized();
        AssertThat(direction.Dot(expected)).IsGreater(0.99f);
        AssertThat(invader.SteeringAnchor.DistanceTo(anchor) < 1.0f).IsTrue();

        // Reaching the anchor latches and freezes it in place while it ulcerates
        invader.GlobalPosition = anchor;
        direction = EnemySteering.GetDirection(invader, 0.016f);
        AssertThat(direction.LengthSquared()).IsEqual(0.0f);
        AssertThat(EnemySteering.IsInvaderLatched(invader)).IsTrue();

        invader._PhysicsProcess(1.2);
        AssertThat(invader.LatchPulses).IsEqual(1);

        int lesions = 0;
        foreach (var child in _container.GetChildren())
        {
            if (child is HazardZone)
                lesions++;
        }
        AssertThat(lesions).IsEqual(1);
        GD.Print("[PASS] Tissue invader seeks a host anchor, latches, and ulcerates it.");
        invader.QueueFree();
    }

    private void TestTetanusArtillery()
    {
        _player!.GlobalPosition = new Vector2(900, 0);
        var tetanus = EnemySpawner.CreateEnemy("tetanus")!;
        tetanus.GlobalPosition = new Vector2(480, 0);
        _container!.AddChild(tetanus);

        for (int i = 0; i < 320; i++)
        {
            tetanus._PhysicsProcess(1.0 / 60.0);
        }

        EnemyProjectile? pulse = null;
        foreach (var child in _container.GetChildren())
        {
            if (child is EnemyProjectile found && found.StunDuration > 0.0f)
            {
                pulse = found;
                break;
            }
        }

        AssertThat(pulse).IsNotNull();
        AssertThat(tetanus.ThreatMode).IsEqual(EnemyThreatMode.Standoff);
        GD.Print("[PASS] Tetanus keeps its bespoke standoff artillery behavior (pulse fired).");
        tetanus.QueueFree();
    }
}

