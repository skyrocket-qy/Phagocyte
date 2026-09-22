using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Environment;
using Phagocyte.Player;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies neutral environment matter (senescent RBC cover, dormant toxin vesicles)
/// and the host ulceration meter.
/// </summary>
[TestSuite]
public partial class TestNeutralMatter : TestHarness
{
    private int _frame = 0;
    private int _phase = 0;
    private bool _done = false;
    private Node2D? _container;
    private BaseCell? _player;
    private float _rbcExpBefore;
    private int _rbcKillsBefore;

    public override bool _Process(double delta)
    {
        if (_done)
            return true;

        _frame++;
        if (_frame < 3)
            return false;

        try
        {
            if (_phase == 0)
            {
                RunSetupAndRbcConsume();
                _phase = 1;
                _frame = 0;
                return false;
            }

            // RBC harvest tween (0.18s) pays out on completion.
            if (_frame < 20)
                return false;

            AssertSenescentRbc();
            TestPelletCover();
            TestToxinVesicle();
            TestUlcerationMeter();
            TestMainIntegration();

            _container!.QueueFree();
            _done = true;
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestNeutralMatter threw: ", ex);
            Quit(1);
            return true;
        }

        GD.Print("==================================================================");
        GD.Print(">>> ALL NEUTRAL MATTER & ULCERATION TESTS PASSED SUCCESSFULLY! <<<");
        GD.Print("==================================================================");
        Quit(0);
        return true;
    }

    private void RunSetupAndRbcConsume()
    {
        EnemySteering.ConfigureArena(new Vector2(2000, 2000));
        HostUlceration.Reset();

        _container = new Node2D { Name = "NeutralMatterContainer" };
        Root.AddChild(_container);
        _player = new BaseCell { Name = "NeutralMatterHost", GlobalPosition = new Vector2(500, 500) };
        _container.AddChild(_player);

        ConsumeSenescentRbc();
    }

    private void ConsumeSenescentRbc()
    {
        var rbc = new SenescentRBC { GlobalPosition = new Vector2(700, 700) };
        _container!.AddChild(rbc);

        AssertThat(rbc.IsInGroup("neutral_matter")).IsTrue();
        AssertThat(rbc.IsInGroup("senescent_rbc")).IsTrue();
        AssertThat(rbc.IsInGroup("pathogens")).IsFalse();
        AssertThat(rbc.GetAtpValue()).IsEqual(25.0f);
        AssertThat(rbc.GetBaseScore()).IsEqual(0);

        _rbcKillsBefore = _player!.DigestedCount;
        _rbcExpBefore = _player.CurrentExp;
        _player.ConsumePathogen(rbc);

        AssertThat(rbc.IsConsumed).IsTrue();
    }

    private void AssertSenescentRbc()
    {
        AssertThat(_player!.DigestedCount).IsEqual(_rbcKillsBefore + 1);
        AssertThat(_player.CurrentExp).IsGreater(_rbcExpBefore);
        GD.Print("[PASS] Senescent RBC is neutral cover and only yields ATP/kill when engulfed.");
    }

    private void TestPelletCover()
    {
        _player!.GlobalPosition = new Vector2(-900, -900);
        var rbc = new SenescentRBC { GlobalPosition = Vector2.Zero };
        _container!.AddChild(rbc);

        var pellet = new EnemyPellet
        {
            GlobalPosition = new Vector2(-200, 0),
            Direction = Vector2.Right,
            Speed = 300.0f,
            Damage = 5.0f
        };
        _container.AddChild(pellet);

        for (int i = 0; i < 20 && !pellet.IsQueuedForDeletion(); i++)
        {
            pellet._PhysicsProcess(0.05);
        }

        AssertThat(pellet.IsQueuedForDeletion()).IsTrue();
        AssertThat(rbc.IsConsumed).IsFalse();
        GD.Print("[PASS] Enemy pellets are absorbed by neutral RBC cover.");
        // Free immediately: this synchronous suite runs every phase in one frame,
        // so a queued free would leave a stale RBC at the origin for later phases.
        _container.RemoveChild(rbc);
        rbc.Free();
    }

    private void TestToxinVesicle()
    {
        _player!.GlobalPosition = new Vector2(900, 900);
        _player.Stats!.SetBase("max_health", 500.0f);
        _player.Health = 500.0f;

        var vesicle = new DormantToxinVesicle { GlobalPosition = Vector2.Zero };
        _container!.AddChild(vesicle);

        int hazardsBefore = CountChildren<BioHazardArea>();
        vesicle._PhysicsProcess(0.016);

        AssertThat(vesicle.HasExploded).IsTrue();
        AssertThat(CountChildren<BioHazardArea>()).IsEqual(hazardsBefore + 1);

        // Enemy contact also triggers the mine (friendly fire)
        var staph = new StaphEnemy { GlobalPosition = new Vector2(1500, 1500) };
        _container.AddChild(staph);
        var vesicle2 = new DormantToxinVesicle { GlobalPosition = new Vector2(1500, 1500) };
        _container.AddChild(vesicle2);
        vesicle2._PhysicsProcess(0.016);

        AssertThat(vesicle2.HasExploded).IsTrue();
        AssertThat(staph.CurrentHealth).IsLess(staph.MaxHealth);
        GD.Print("[PASS] Dormant toxin vesicles detonate on player and pathogen contact.");
        staph.QueueFree();
    }

    private void TestUlcerationMeter()
    {
        HostUlceration.Reset();
        AssertThat(HostUlceration.Pulses).IsEqual(0);

        var invader = new HpyloriEnemy { GlobalPosition = Vector2.Zero };
        _container!.AddChild(invader);
        var rbc = new SenescentRBC { GlobalPosition = new Vector2(220, 0) };
        _container.AddChild(rbc);

        // Below threshold: invaders head for host tissue anchors
        Vector2 tissueDirection = EnemySteering.GetDirection(invader, 0.016f);
        AssertThat(tissueDirection.Dot(Vector2.Right)).IsLess(0.99f);

        // Accumulated ulceration retargets invaders onto red blood cells
        HostUlceration.RegisterPulse();
        HostUlceration.RegisterPulse();
        HostUlceration.RegisterPulse();
        Vector2 rbcDirection = EnemySteering.GetDirection(invader, 0.016f);
        AssertThat(rbcDirection.Dot(Vector2.Right)).IsGreater(0.99f);

        // Pulses are registered and dissolve nearby RBCs
        HostUlceration.Reset();
        invader.GlobalPosition = new Vector2(220, 0);
        invader.EmitUlcerationPulse();
        AssertThat(HostUlceration.Pulses).IsEqual(1);
        AssertThat(rbc.IsConsumed).IsTrue();
        GD.Print("[PASS] Host ulceration registers pulses and retargets invaders onto RBCs.");
        invader.QueueFree();
    }

    private void TestMainIntegration()
    {
        HostUlceration.Reset();
        GameManager.SelectedClass = "macrophage";
        GameManager.SelectedMap = "acute_wound";

        var main = GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>();
        Root.AddChild(main);
        main.SetPhysicsProcess(false);

        int rbcCount = 0;
        int vesicleCount = 0;
        foreach (var child in main.EnemyContainer!.GetChildren())
        {
            if (child is SenescentRBC)
                rbcCount++;
            if (child is DormantToxinVesicle)
                vesicleCount++;
        }

        AssertThat(rbcCount).IsEqual(4);
        AssertThat(vesicleCount).IsEqual(3);

        // Neutrals never consume pathogen screen-cap slots
        int pathogensBefore = main.ActivePathogenCount;
        var extraRbc = new SenescentRBC { GlobalPosition = new Vector2(100, 100) };
        main.EnemyContainer.AddChild(extraRbc);
        AssertThat(main.ActivePathogenCount).IsEqual(pathogensBefore);

        // Ulceration environment degradation spawns ambient acid mist
        for (int i = 0; i < HostUlceration.EnvironmentThreshold; i++)
            HostUlceration.RegisterPulse();
        AssertThat(main.UlcerationPulses).IsEqual(HostUlceration.EnvironmentThreshold);

        int hazardsBefore = 0;
        foreach (var child in main.EnemyContainer.GetChildren())
        {
            if (child is BioHazardArea && child is not FibrinClot)
                hazardsBefore++;
        }

        main._PhysicsProcess(12.1f);

        int hazardsAfter = 0;
        foreach (var child in main.EnemyContainer.GetChildren())
        {
            if (child is BioHazardArea && child is not FibrinClot)
                hazardsAfter++;
        }
        AssertThat(hazardsAfter).IsEqual(hazardsBefore + 1);

        GD.Print("[PASS] Main seeds neutrals, keeps them out of the cap, and ulceration degrades the arena.");
        main.QueueFree();
    }

    private int CountChildren<T>() where T : Node
    {
        int count = 0;
        foreach (var child in _container!.GetChildren())
        {
            if (child is T)
                count++;
        }
        return count;
    }
}
