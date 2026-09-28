using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Game.Core;
using Game.Combat;
using Game.Enemies;
using Game.Stages;
using Game.Player;

namespace Game.Tests;

/// <summary>
/// Verifies data-driven stage environments: every stage id resolves to a
/// StageEnvironment whose effects come from assets/data/maps.json, and each
/// effect kind (spawner, volley, dot_scan, stat_strip, scramble, scatter)
/// behaves per its parameters (docs/map.md §3 / TODO module 3).
/// </summary>
[TestSuite]
public partial class TestStageEnvironments : TestHarness
{
    private int _phase = 0;
    private GameRoot? _main = null;
    private BlockerWall? _fenestraWall = null;
    private PlayerActor? _hepaticPlayer = null;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING ORGAN MECHANICS & ENVIRONMENT VERIFICATION <<<");
        GD.Print("==================================================================");
        Paused = false;
        // Mechanics suites opt in: the global flag defaults to off.
        SettingsManager.MapEffectsEnabled = true;
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
                RunFactoryTests();
                _phase++;
                return false;
            case 2:
                RunAcuteWoundTests();
                _phase++;
                return false;
            case 3:
                RunAlveolarTests();
                _phase++;
                return false;
            case 4:
                RunHepaticTestsPart1();
                _phase++;
                return false;
            case 5:
                RunHepaticTestsPart2();
                _phase++;
                return false;
            case 6:
                RunGastricTests();
                _phase++;
                return false;
            case 7:
                RunBloodBrainBarrierTests();
                _phase++;
                return false;
            case 8:
                RunDisabledFlagTests();
                _phase++;
                return false;
            default:
                Cleanup();
                GD.Print("==================================================================");
                GD.Print(">>> ALL ORGAN ENVIRONMENT TESTS PASSED SUCCESSFULLY! <<<");
                GD.Print("==================================================================");
                Quit(0);
                return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestStageEnvironments threw: ", ex);
            Cleanup();
            Quit(1);
            return true;
        }
    }

    private GameRoot SpawnMain(string stageId)
    {
        Cleanup();
        GameManager.SelectedClass = "macrophage";
        GameManager.SelectedMap = stageId;
        var main = AssetLoader.Load<PackedScene>("res://scenes/main.tscn").Instantiate<GameRoot>();
        _main = main;
        Root.AddChild(main);
        main.SetPhysicsProcess(false);
        return main;
    }

    private void RunFactoryTests()
    {
        foreach (string stageId in new[] { "acute_wound", "alveolar_space", "hepatic_sinusoid", "gastric_lumen", "blood_brain_barrier" })
        {
            var env = StageEnvironment.ForMap(stageId);
            AssertThat(env).IsNotNull();
            AssertThat(env.StageId).IsEqual(stageId);
        }
        // Unknown ids fall back to an empty (cleared-arena) environment.
        AssertThat(StageEnvironment.ForMap("no_such_stage").StageId).IsEqual("no_such_stage");
        GD.Print("[PASS] One data-driven environment per stage id.");
    }

    private void RunAcuteWoundTests()
    {
        var main = SpawnMain("acute_wound");
        AssertThat(main.Stage).IsNotNull();
        AssertThat(main.Stage!.StageId).IsEqual("acute_wound");

        // Wound floor is a cleared arena: ticking past the old clot interval
        // spawns no hazards.
        main._PhysicsProcess(0.02);
        main._PhysicsProcess(10.0f);
        int hazards = 0;
        foreach (var child in main.EnemyContainer!.GetChildren())
        {
            if (child is HazardZone)
                hazards++;
        }
        AssertThat(hazards).IsEqual(0);

        GD.Print("[PASS] Acute wound builds with no environmental hazards.");
    }

    private void RunAlveolarTests()
    {
        var main = SpawnMain("alveolar_space");
        var environment = main.Stage;
        AssertThat(environment).IsNotNull();
        var player = (PlayerActor)main.Player!;

        // Buff-zone spawner effect grants its configured stat when entered.
        environment!.Tick(main, 7.0f); // push pocket spawn timer past its interval
        BuffZone? pocket = null;
        foreach (var child in main.EnemyContainer!.GetChildren())
        {
            if (child is BuffZone found)
            {
                pocket = found;
                break;
            }
        }
        AssertThat(pocket).IsNotNull();
        AssertThat(pocket!.StatId).IsEqual("cooldown_reduction");
        AssertThat(player.Stats!.GetStat("cooldown_reduction")).IsEqualApprox(0.0f, 0.001f);

        player.GlobalPosition = pocket!.GlobalPosition;
        pocket._PhysicsProcess(0.02);
        AssertThat(pocket.Consumed).IsTrue();
        AssertThat(player.Stats.GetStat("cooldown_reduction")).IsEqualApprox(pocket.Bonus, 0.001f);

        GD.Print("[PASS] Alveolar buff-zone spawner verified.");
    }

    private void RunHepaticTestsPart1()
    {
        var main = SpawnMain("hepatic_sinusoid");
        _main = main;
        var environment = main.Stage;
        AssertThat(environment).IsNotNull();
        var player = (PlayerActor)main.Player!;
        _hepaticPlayer = player;

        float baseArmor = player.Stats!.GetStat("armor");
        AssertThat(baseArmor).IsGreater(0.0f);

        // Stat-strip effect suppresses armor for its duration, then restores it.
        environment!.Tick(main, 11.0f);
        AssertThat(environment.StatStripped).IsTrue();
        AssertThat(player.Stats.GetStat("armor")).IsEqualApprox(0.0f, 0.001f);

        environment.Tick(main, 3.0f + 0.5f);
        AssertThat(environment.StatStripped).IsFalse();
        AssertThat(player.Stats.GetStat("armor")).IsEqualApprox(baseArmor, 0.001f);

        // Blocker-wall spawner gates enlarged cells; dodging never phases terrain.
        environment.Tick(main, 24.0f + 1.0f);
        BlockerWall? wall = null;
        foreach (var child in main.EnemyContainer!.GetChildren())
        {
            if (child is BlockerWall found)
            {
                wall = found;
                break;
            }
        }
        AssertThat(wall).IsNotNull();
        _fenestraWall = wall;

        Input.ActionRelease("dodge");
        wall!._PhysicsProcess(0.02);
        AssertThat(((CollisionShape2D)wall.GetChild(0)).Disabled).IsFalse();

        // Dodge on: the dash grants i-frames but the wall stays solid.
        Input.ActionPress("dodge");
        player._PhysicsProcess(0.02);
        AssertThat(player.IsDodging).IsTrue();
        wall._PhysicsProcess(0.02);
        AssertThat(((CollisionShape2D)wall.GetChild(0)).Disabled).IsFalse();
        Input.ActionRelease("dodge");
    }

    private void RunHepaticTestsPart2()
    {
        var wall = _fenestraWall;
        AssertThat(wall).IsNotNull();
        AssertThat(((CollisionShape2D)wall!.GetChild(0)).Disabled).IsFalse();

        GD.Print("[PASS] Hepatic stat-strip and blocker-wall pore gating verified.");
    }

    private void RunGastricTests()
    {
        var main = SpawnMain("gastric_lumen");
        AssertThat(main.Stage).IsNotNull();
        AssertThat(main.Stage!.StageId).IsEqual("gastric_lumen");
        var player = (PlayerActor)main.Player!;

        // Force a volley and park the player on top of the dot zone.
        main.Stage!.Tick(main, 7.0f);
        DotZone? surge = null;
        foreach (var child in main.EnemyContainer!.GetChildren())
        {
            if (child is DotZone found)
            {
                surge = found;
                break;
            }
        }
        AssertThat(surge).IsNotNull();
        surge!._PhysicsProcess(3.0f); // expand the wavefront

        player.GlobalPosition = surge.GlobalPosition;
        float hpOutside = player.Health;
        // Zero the defensive RNG before a damage assert (docs/AGENTS.md):
        // the macrophage chassis ships 8% glycocalyx block, which otherwise
        // lets the corrosion tick be blocked ~8% of runs.
        player.Stats!.SetBase("block", 0.0f);
        player.Stats.SetBase("evasion", 0.0f);
        main.Stage.Tick(main, 1.0f);
        AssertThat(player.Health).IsLess(hpOutside);

        // Safe zone negates the dot scan.
        var zone = new SafeZone { GlobalPosition = player.GlobalPosition };
        main.EnemyContainer.AddChild(zone);
        AssertThat(SafeZone.CoversPoint(player.GlobalPosition)).IsTrue();

        float hpInside = player.Health;
        main.Stage.Tick(main, 1.0f);
        AssertThat(player.Health).IsEqualApprox(hpInside, 0.01f);

        zone.QueueFree();
        GD.Print("[PASS] Gastric dot volley, dot scan and safe zones verified.");
    }

    private void RunBloodBrainBarrierTests()
    {
        var main = SpawnMain("blood_brain_barrier");
        AssertThat(main.Stage).IsNotNull();
        AssertThat(main.Stage!.StageId).IsEqual("blood_brain_barrier");
        var player = (PlayerActor)main.Player!;

        int pillars = 0;
        foreach (var child in main.EnemyContainer!.GetChildren())
        {
            if (child is BlockerPillar)
                pillars++;
        }
        AssertThat(pillars).IsEqual(5);

        main.Stage!.Tick(main, 0.4f);

        // Scramble effect periodically inverts the movement direction.
        main.Stage.Tick(main, 11.0f + 0.5f);
        AssertThat(player.InvertControlsTimer).IsGreater(0.0f);

        GD.Print("[PASS] BBB blocker scatter and control scramble verified.");
    }

    private void RunDisabledFlagTests()
    {
        SettingsManager.MapEffectsEnabled = false;
        var main = SpawnMain("acute_wound");
        AssertThat(main.Stage).IsNull();
        GD.Print("[PASS] With the flag off, no environment builds and only tints apply.");
    }

    private void Cleanup()
    {
        Paused = false;
        Input.ActionRelease("dodge");

        if (_main != null && IsInstanceValid(_main))
        {
            if (_main.GetParent() != null)
                _main.GetParent().RemoveChild(_main);
            _main.Free();
            _main = null;
        }

        GameManager.SelectedMap = "acute_wound";
    }
}
