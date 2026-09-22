using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Environment;
using Phagocyte.Player;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the five organ-specific fluid mechanics and physiological
/// environments that act directly on the player cell (docs/map.md Â§3 / TODO module 3).
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
        GD.Print(">>> STARTING ORGAN FLUID MECHANICS & ENVIRONMENT VERIFICATION <<<");
        GD.Print("==================================================================");
        Paused = false;
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
                RunPlayerDriftIntegrationTests();
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

        main._PhysicsProcess(0.02);
        AssertThat(main.OrganEnvironment!.PlayerDrift.IsEqualApprox(new Vector2(16.0f, 10.0f))).IsTrue();
        AssertThat(((BaseCell)main.Player!).EnvironmentDrift.IsEqualApprox(new Vector2(16.0f, 10.0f))).IsTrue();
        // Pathogen fluid current is the 40% suction drag owned by the environment.
        AssertThat(main.OrganEnvironment.FluidVector.IsEqualApprox(new Vector2(6.4f, 4.0f))).IsTrue();
        AssertThat(main.CurrentFluidVector.IsEqualApprox(new Vector2(6.4f, 4.0f))).IsTrue();

        // Fibrin clots congeal across the wound floor; the first appears after ~2.5s.
        main._PhysicsProcess(3.0f);
        FibrinClot? clot = null;
        foreach (var child in main.EnemyContainer!.GetChildren())
        {
            if (child is FibrinClot found)
            {
                clot = found;
                break;
            }
        }
        AssertThat(clot).IsNotNull();
        AssertThat(clot!.DealsDamage).IsFalse();
        AssertThat(clot.IsInGroup("neutral_matter")).IsTrue();
        AssertThat(clot.BlockRadius).IsEqualApprox(clot.Radius, 0.01f);

        // Fibrin mesh absorbs enemy projectiles.
        var pellet = new EnemyPellet { GlobalPosition = clot.GlobalPosition, Direction = Vector2.Right };
        main.EnemyContainer.AddChild(pellet);
        pellet._PhysicsProcess(0.02);
        AssertThat(pellet.IsQueuedForDeletion()).IsTrue();

        // Hard difficulty biofilm corrodes the membrane instead of just slowing.
        var biofilm = new FibrinClot { HardBiofilm = true, GlobalPosition = new Vector2(500, 500) };
        main.EnemyContainer.AddChild(biofilm);
        AssertThat(biofilm.DealsDamage).IsTrue();

        GD.Print("[PASS] Acute wound exudate suction, fibrin slow and projectile absorption verified.");
    }

    private void RunAlveolarTests()
    {
        var main = SpawnMain("alveolar_space");
        var environment = main.OrganEnvironment as AlveolarEnvironment;
        AssertThat(environment).IsNotNull();
        var player = (BaseCell)main.Player!;

        // 12s cycle: inhale pushes down for 3s, exhale pushes up for 3s.
        environment!.Tick(main, 0.5f);
        AssertThat(environment.PlayerDrift.IsEqualApprox(new Vector2(0.0f, 35.0f))).IsTrue();
        environment.Tick(main, 6.5f); // t = 7.0 -> exhale window
        AssertThat(environment.PlayerDrift.IsEqualApprox(new Vector2(0.0f, -35.0f))).IsTrue();
        environment.Tick(main, 3.5f); // t = 10.5 -> rest
        AssertThat(environment.PlayerDrift).IsEqual(Vector2.Zero);

        // Respiratory airflow is the 60% sinusoidal current at the run clock (t = 10.5).
        Vector2 expectedFlow = new Vector2(
            Mathf.Sin(main.EnvironmentTime * 1.2f) * 28.0f,
            Mathf.Sin(main.EnvironmentTime * 0.6f) * 12.0f) * 0.6f;
        AssertThat(environment.FluidVector.IsEqualApprox(expectedFlow)).IsTrue();

        // Hyperoxic pocket grants temporal CDR when entered.
        environment.Tick(main, 7.0f); // push pocket spawn timer past its interval
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

        GD.Print("[PASS] Alveolar 12s breathing shear and hyperoxic CDR pockets verified.");
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

        // Sinusoid flow drag also drives the pathogen current.
        AssertThat(environment.FluidVector.LengthSquared()).IsGreater(0.0f);

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
        player.EnvironmentDrift = Vector2.Zero;
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
        // Churn current drives the pathogen population.
        AssertThat(main.OrganEnvironment.FluidVector.LengthSquared()).IsGreater(0.0f);
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
        AssertThat(main.OrganEnvironment.PlayerDrift.LengthSquared()).IsGreater(0.0f);
        // Synaptic micro-vibration current is owned by the environment too.
        AssertThat(main.OrganEnvironment.FluidVector.LengthSquared()).IsGreater(0.0f);

        // Neural electric pulses periodically scramble the movement direction.
        main.OrganEnvironment.Tick(main, BloodBrainBarrierEnvironment.PulseInterval + 0.5f);
        AssertThat(player.InvertControlsTimer).IsGreater(0.0f);

        GD.Print("[PASS] BBB shear drag, astrocyte maze and neural pulse interference verified.");
    }

    private void RunPlayerDriftIntegrationTests()
    {
        var main = SpawnMain("acute_wound");
        var player = (BaseCell)main.Player!;

        // Fluid current keeps dragging the cell even with no input.
        player.Velocity = Vector2.Zero;
        player.EnvironmentDrift = new Vector2(16.0f, 10.0f);
        player._PhysicsProcess(0.1);
        AssertThat(player.Velocity.LengthSquared()).IsGreater(0.0f);
        AssertThat(player.Velocity.Normalized().Dot(new Vector2(16.0f, 10.0f).Normalized())).IsGreater(0.5f);

        GD.Print("[PASS] Organ drift is stacked onto the player's swim velocity.");
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
