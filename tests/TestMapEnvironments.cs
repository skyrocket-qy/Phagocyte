using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Core;
using Phagocyte.Combat;
using Phagocyte.Enemies;
using Phagocyte.Map;
using Phagocyte.Hero;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the five organ-specific mechanics and physiological
/// props that act on the arena (docs/map.md §3 / TODO module 3).
/// </summary>
[TestSuite]
public partial class TestMapEnvironments : TestHarness
{
    private int _phase = 0;
    private Main? _main = null;
    private FenestraWall? _fenestraWall = null;
    private BaseCell? _hepaticPlayer = null;

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
            GD.PrintErr("[FAIL] TestMapEnvironments threw: ", ex);
            Cleanup();
            Quit(1);
            return true;
        }
    }

    private Main SpawnMain(string mapId)
    {
        Cleanup();
        GameManager.SelectedClass = "macrophage";
        GameManager.SelectedMap = mapId;
        var main = AssetLoader.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>();
        _main = main;
        Root.AddChild(main);
        main.SetPhysicsProcess(false);
        return main;
    }

    private void RunFactoryTests()
    {
        AssertThat(MapEnvironment.ForMap("acute_wound")).IsInstanceOf<AcuteWoundEnvironment>();
        AssertThat(MapEnvironment.ForMap("alveolar_space")).IsInstanceOf<AlveolarEnvironment>();
        AssertThat(MapEnvironment.ForMap("hepatic_sinusoid")).IsInstanceOf<HepaticEnvironment>();
        AssertThat(MapEnvironment.ForMap("gastric_lumen")).IsInstanceOf<GastricEnvironment>();
        AssertThat(MapEnvironment.ForMap("blood_brain_barrier")).IsInstanceOf<BloodBrainBarrierEnvironment>();
        GD.Print("[PASS] One environment implementation per organ map.");
    }

    private void RunAcuteWoundTests()
    {
        var main = SpawnMain("acute_wound");
        AssertThat(main.OrganEnvironment).IsInstanceOf<AcuteWoundEnvironment>();

        // Wound floor is a cleared arena: ticking past the old clot interval
        // spawns no hazards.
        main._PhysicsProcess(0.02);
        main._PhysicsProcess(10.0f);
        int hazards = 0;
        foreach (var child in main.EnemyContainer!.GetChildren())
        {
            if (child is BioHazardArea)
                hazards++;
        }
        AssertThat(hazards).IsEqual(0);

        GD.Print("[PASS] Acute wound builds with no environmental hazards.");
    }

    private void RunAlveolarTests()
    {
        var main = SpawnMain("alveolar_space");
        var environment = main.OrganEnvironment as AlveolarEnvironment;
        AssertThat(environment).IsNotNull();
        var player = (BaseCell)main.Player!;

        // Hyperoxic pocket grants temporal CDR when entered.
        environment!.Tick(main, 7.0f); // push pocket spawn timer past its interval
        HyperoxicPocket? pocket = null;
        foreach (var child in main.EnemyContainer!.GetChildren())
        {
            if (child is HyperoxicPocket found)
            {
                pocket = found;
                break;
            }
        }
        AssertThat(pocket).IsNotNull();
        AssertThat(player.Stats!.GetStat("cooldown_reduction")).IsEqualApprox(0.0f, 0.001f);

        player.GlobalPosition = pocket!.GlobalPosition;
        pocket._PhysicsProcess(0.02);
        AssertThat(pocket.Consumed).IsTrue();
        AssertThat(player.Stats.GetStat("cooldown_reduction")).IsEqualApprox(HyperoxicPocket.BuffBonus, 0.001f);

        GD.Print("[PASS] Alveolar hyperoxic CDR pockets verified.");
    }

    private void RunHepaticTestsPart1()
    {
        var main = SpawnMain("hepatic_sinusoid");
        _main = main;
        var environment = main.OrganEnvironment as HepaticEnvironment;
        AssertThat(environment).IsNotNull();
        var player = (BaseCell)main.Player!;
        _hepaticPlayer = player;

        float baseArmor = player.Stats!.GetStat("armor");
        AssertThat(baseArmor).IsGreater(0.0f);

        // Bile-acid surge strips the whole arena's armor for 3s, then restores it.
        environment!.Tick(main, 11.0f);
        AssertThat(environment.ArmorBroken).IsTrue();
        AssertThat(player.Stats.GetStat("armor")).IsEqualApprox(0.0f, 0.001f);

        environment.Tick(main, HepaticEnvironment.BileArmorBreakSeconds + 0.5f);
        AssertThat(environment.ArmorBroken).IsFalse();
        AssertThat(player.Stats.GetStat("armor")).IsEqualApprox(baseArmor, 0.001f);

        // Endothelial fenestrae block enlarged cells; dodging never phases terrain.
        environment.Tick(main, HepaticEnvironment.FenestraInterval + 1.0f);
        FenestraWall? wall = null;
        foreach (var child in main.EnemyContainer!.GetChildren())
        {
            if (child is FenestraWall found)
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

        GD.Print("[PASS] Hepatic bile-acid armor strip and fenestra pore gating verified.");
    }

    private void RunGastricTests()
    {
        var main = SpawnMain("gastric_lumen");
        AssertThat(main.OrganEnvironment).IsInstanceOf<GastricEnvironment>();
        var player = (BaseCell)main.Player!;

        // Force a surge and park the player on top of the acid pool.
        main.OrganEnvironment!.Tick(main, 7.0f);
        AcidSurge? surge = null;
        foreach (var child in main.EnemyContainer!.GetChildren())
        {
            if (child is AcidSurge found)
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
        main.OrganEnvironment.Tick(main, 1.0f);
        AssertThat(player.Health).IsLess(hpOutside);

        // Alkaline neutralization zone negates the corrosion.
        var zone = new NeutralizationZone { GlobalPosition = player.GlobalPosition };
        main.EnemyContainer.AddChild(zone);
        AssertThat(NeutralizationZone.CoversPoint(player.GlobalPosition)).IsTrue();

        float hpInside = player.Health;
        main.OrganEnvironment.Tick(main, 1.0f);
        AssertThat(player.Health).IsEqualApprox(hpInside, 0.01f);

        zone.QueueFree();
        GD.Print("[PASS] Gastric acid surge corrosion and alkaline neutralization zones verified.");
    }

    private void RunBloodBrainBarrierTests()
    {
        var main = SpawnMain("blood_brain_barrier");
        AssertThat(main.OrganEnvironment).IsInstanceOf<BloodBrainBarrierEnvironment>();
        var player = (BaseCell)main.Player!;

        int pillars = 0;
        foreach (var child in main.EnemyContainer!.GetChildren())
        {
            if (child is AstrocytePillar)
                pillars++;
        }
        AssertThat(pillars).IsEqual(BloodBrainBarrierEnvironment.PillarCount);

        main.OrganEnvironment!.Tick(main, 0.4f);

        // Neural electric pulses periodically scramble the movement direction.
        main.OrganEnvironment.Tick(main, BloodBrainBarrierEnvironment.PulseInterval + 0.5f);
        AssertThat(player.InvertControlsTimer).IsGreater(0.0f);

        GD.Print("[PASS] BBB astrocyte maze and neural pulse interference verified.");
    }

    private void RunDisabledFlagTests()
    {
        SettingsManager.MapEffectsEnabled = false;
        var main = SpawnMain("acute_wound");
        AssertThat(main.OrganEnvironment).IsNull();
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
