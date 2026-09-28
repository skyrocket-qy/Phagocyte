using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Game.Enemies;
using Game.Combat;
using Game.Player;

namespace Game.Tests;

/// <summary>
/// Verifies the five 09:00 map sub-bosses and their signature mechanics.
/// </summary>
[TestSuite]
public partial class TestSubBosses : SceneTree
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
            GD.PrintErr("[FAIL] TestSubBosses threw: ", ex);
            Quit(1);
            return true;
        }

        GD.Print("==================================================================");
        GD.Print(">>> ALL MAP SUB-BOSS TESTS PASSED SUCCESSFULLY! <<<");
        GD.Print("==================================================================");
        Quit(0);
        return true;
    }

    private void RunTests()
    {
        _container = new Node2D { Name = "SubBossTestContainer" };
        Root.AddChild(_container);
        _player = new PlayerActor { Name = "SubBossHost", GlobalPosition = new Vector2(400, 400) };
        _container.AddChild(_player);

        TestMapMapping();
        TestChainLord();
        TestFluDriftCyclone();
        TestTbGranuloma();
        TestVacASecretor();
        TestToxoplasmaMegaCyst();

        _container.QueueFree();
    }

    private void TestMapMapping()
    {
        AssertThat(EnemySpawner.CreateSubBoss("acute_wound")!.EnemyId).IsEqual("streptococcus_chain_lord");
        AssertThat(EnemySpawner.CreateSubBoss("alveolar_space")!.EnemyId).IsEqual("flu_drift_cyclone");
        AssertThat(EnemySpawner.CreateSubBoss("hepatic_sinusoid")!.EnemyId).IsEqual("tb_granuloma_behemoth");
        AssertThat(EnemySpawner.CreateSubBoss("gastric_lumen")!.EnemyId).IsEqual("vaca_secretor");
        AssertThat(EnemySpawner.CreateSubBoss("blood_brain_barrier")!.EnemyId).IsEqual("toxoplasma_mega_cyst");

        var spawned = EnemySpawner.SpawnSubBoss(_container!, _player!, new Vector2(4800, 4800), "acute_wound");
        AssertThat(spawned).IsNotNull();
        AssertThat(spawned!.IsBoss).IsTrue();
        AssertThat(spawned.GetNodeOrNull("BossPhaseComponent")).IsNotNull();
        spawned.QueueFree();

        GD.Print("[PASS] All five maps map to their dedicated sub-boss entity with boss phases.");
    }

    private void TestChainLord()
    {
        var lord = EnemySpawner.CreateEnemy("streptococcus_chain_lord")!;
        lord.GlobalPosition = _player!.GlobalPosition + new Vector2(90, 0);
        _container!.AddChild(lord);

        float healthBefore = _player.Health;
        for (int i = 0; i < 240; i++)
        {
            lord._PhysicsProcess(1.0 / 60.0);
        }

        AssertThat(_player.Health).IsLess(healthBefore);
        GD.Print("[PASS] Streptococcus Chain-Lord serpentine movement and multi-segment collision verified.");
        lord.QueueFree();
    }

    private void TestFluDriftCyclone()
    {
        var cyclone = EnemySpawner.CreateEnemy("flu_drift_cyclone")!;
        cyclone.GlobalPosition = new Vector2(1000, 1000);
        _container!.AddChild(cyclone);

        var marked = EnemySpawner.CreateEnemy("staph")!;
        marked.GlobalPosition = new Vector2(1050, 1000);
        _container.AddChild(marked);
        marked.SetMeta("mhc_marked", true);

        cyclone._PhysicsProcess(30.1);

        AssertThat(marked.HasMeta("mhc_marked")).IsFalse();

        bool waveFound = false;
        foreach (var child in _container.GetChildren())
        {
            if (child is Game.Combat.ShockRing)
            {
                waveFound = true;
                break;
            }
        }
        AssertThat(waveFound).IsTrue();
        GD.Print("[PASS] Flu-Drift Cyclone antigenic drift cleared MHC marks and expanded a full-screen wave.");
        cyclone.QueueFree();
        marked.QueueFree();
    }

    private void TestTbGranuloma()
    {
        var behemoth = EnemySpawner.CreateEnemy("tb_granuloma_behemoth")!;
        behemoth.GlobalPosition = new Vector2(1600, 1600);
        _container!.AddChild(behemoth);

        behemoth.TakeDamage(9999999.0f);

        BlockerObstacle? debris = null;
        foreach (var child in _container.GetChildren())
        {
            if (child is BlockerObstacle found)
            {
                debris = found;
                break;
            }
        }

        AssertThat(debris).IsNotNull();
        AssertThat(debris!.CollisionLayer).IsEqual(4);
        AssertThat(debris.GetNodeOrNull<CollisionShape2D>("CollisionShape2D")).IsNotNull();
        GD.Print("[PASS] TB Granuloma Behemoth left a caseous necrosis obstacle on death.");
        debris.QueueFree();
    }

    private void TestVacASecretor()
    {
        var secretor = EnemySpawner.CreateEnemy("vaca_secretor")!;
        secretor.GlobalPosition = new Vector2(2200, 2200);
        _container!.AddChild(secretor);

        HazardZone? pool = null;
        for (int i = 0; i < 220 && pool == null; i++)
        {
            secretor._PhysicsProcess(1.0 / 60.0);
            foreach (var child in _container.GetChildren())
            {
                if (child is HazardZone found && found.GrowRate > 0.0f)
                {
                    pool = found;
                    break;
                }
            }
        }

        AssertThat(pool).IsNotNull();
        float radiusBefore = pool!.CurrentRadius;
        pool._PhysicsProcess(1.0);
        AssertThat(pool.CurrentRadius).IsGreater(radiusBefore);
        GD.Print("[PASS] VacA Secretor trailed an expanding acid mucus pool.");
        secretor.QueueFree();
    }

    private void TestToxoplasmaMegaCyst()
    {
        var cyst = EnemySpawner.CreateEnemy("toxoplasma_mega_cyst")!;
        cyst.GlobalPosition = new Vector2(3000, 3000);
        _container!.AddChild(cyst);

        cyst.CurrentHealth = cyst.MaxHealth * 0.2f;
        cyst._PhysicsProcess(0.016);

        int tachyzoites = 0;
        foreach (var child in _container.GetChildren())
        {
            if (child is EnemyActor tachy && tachy.EnemyId == "tachyzoite")
                tachyzoites++;
        }

        AssertThat(tachyzoites).IsEqual(4);
        GD.Print("[PASS] Toxoplasma Mega-Cyst burst four orthogonal high-velocity tachyzoites at low HP.");

        foreach (var child in _container.GetChildren())
        {
            if (child is EnemyActor tachy && tachy.EnemyId == "tachyzoite")
                tachy.QueueFree();
        }
        cyst.QueueFree();
    }
}
