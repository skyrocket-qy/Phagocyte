using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Game.Combat;
using Game.Enemies;
using Game.Player;

namespace Game.Tests;

/// <summary>
/// Verifies the five 15:00 stage terminal bosses and their signature mechanics.
/// </summary>
[TestSuite]
public partial class TestTerminalBosses : SceneTree
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
            GD.PrintErr("[FAIL] TestTerminalBosses threw: ", ex);
            Quit(1);
            return true;
        }

        GD.Print("==================================================================");
        GD.Print(">>> ALL TERMINAL BOSS TESTS PASSED SUCCESSFULLY! <<<");
        GD.Print("==================================================================");
        Quit(0);
        return true;
    }

    private void RunTests()
    {
        _container = new Node2D { Name = "TerminalBossTestContainer" };
        Root.AddChild(_container);
        _player = new PlayerActor { Name = "TerminalBossHost", GlobalPosition = new Vector2(400, 400) };
        _container.AddChild(_player);

        TestStageMapping();
        TestMrsaSuperColony();
        TestSyncytialMegaCapsid();
        TestPlasmodiumMacroSchizont();
        TestHpyloriBiofilmCore();
        TestPrpscAmyloidAggregate();

        _container.QueueFree();
    }

    private void TestStageMapping()
    {
        AssertThat(EnemySpawner.CreateTerminalBoss("acute_wound")!.EnemyId).IsEqual("mrsa_super_colony");
        AssertThat(EnemySpawner.CreateTerminalBoss("alveolar_space")!.EnemyId).IsEqual("syncytial_mega_capsid");
        AssertThat(EnemySpawner.CreateTerminalBoss("hepatic_sinusoid")!.EnemyId).IsEqual("plasmodium_macro_schizont");
        AssertThat(EnemySpawner.CreateTerminalBoss("gastric_lumen")!.EnemyId).IsEqual("hpylori_biofilm_core");
        AssertThat(EnemySpawner.CreateTerminalBoss("blood_brain_barrier")!.EnemyId).IsEqual("prpsc_amyloid_aggregate");

        var spawned = EnemySpawner.SpawnTerminalBoss(_container!, _player!, new Vector2(4800, 4800), "acute_wound");
        AssertThat(spawned).IsNotNull();
        AssertThat(spawned!.IsBoss).IsTrue();
        AssertThat(spawned.GetNodeOrNull("BossPhaseComponent")).IsNotNull();
        spawned.QueueFree();

        GD.Print("[PASS] All five stages map to their dedicated terminal boss entity with boss phases.");
    }

    private void TestMrsaSuperColony()
    {
        var mrsa = EnemySpawner.CreateEnemy("mrsa_super_colony")!;
        mrsa.GlobalPosition = new Vector2(900, 900);
        _container!.AddChild(mrsa);

        mrsa.TakeDamage(9999999.0f);

        int elites = 0;
        foreach (var child in _container.GetChildren())
        {
            if (child is EnemyActor elite && elite.EnemyId == "mrsa_enraged_elite")
                elites++;
        }

        AssertThat(elites).IsEqual(4);
        GD.Print("[PASS] MRSA Super-Colony ruptured into four enraged child elites.");
    }

    private void TestSyncytialMegaCapsid()
    {
        var capsid = EnemySpawner.CreateEnemy("syncytial_mega_capsid")!;
        capsid.GlobalPosition = _player!.GlobalPosition + new Vector2(120, 0);
        _container!.AddChild(capsid);

        _player.Status?.ClearAll();
        capsid._PhysicsProcess(0.016);
        AssertThat(_player.Status != null && _player.Status.HasSlow).IsTrue();

        Vector2 velocityBefore = _player.Velocity;
        for (int i = 0; i < 366; i++)
            capsid._PhysicsProcess(1.0 / 60.0);
        AssertThat(_player.Velocity.DistanceTo(velocityBefore) > 1.0f).IsTrue();

        // Traction field widens as the boss loses HP
        float fullHpRadius = capsid.CurrentAuraRadius;
        capsid.CurrentHealth = capsid.MaxHealth * 0.3f;
        AssertThat(capsid.CurrentAuraRadius).IsGreater(fullHpRadius);

        GD.Print("[PASS] Syncytial Mega-Capsid slowed and dragged the player while its field widened.");
        capsid.QueueFree();
    }

    private void TestPlasmodiumMacroSchizont()
    {
        var schizont = EnemySpawner.CreateEnemy("plasmodium_macro_schizont")!;
        schizont.GlobalPosition = new Vector2(1800, 1800);
        _container!.AddChild(schizont);

        schizont.TakeDamage(300.0f);
        float hpBefore = schizont.CurrentHealth;
        schizont._PhysicsProcess(5.1);
        AssertThat(schizont.CurrentHealth).IsGreater(hpBefore);

        schizont.TakeDamage(9999999.0f);

        int merozoites = 0;
        foreach (var child in _container.GetChildren())
        {
            if (child is EnemyActor mero && mero.EnemyId == "plasmodium_merozoite")
                merozoites++;
        }

        AssertThat(merozoites).IsGreaterEqual(10);
        GD.Print("[PASS] Plasmodium Macro-Schizont fed on RBCs to heal and burst into merozoites.");
    }

    private void TestHpyloriBiofilmCore()
    {
        var core = EnemySpawner.CreateEnemy("hpylori_biofilm_core")!;
        core.GlobalPosition = _player!.GlobalPosition + new Vector2(60, 0);
        _container!.AddChild(core);

        core._PhysicsProcess(6.1);
        Zone? scar = null;
        foreach (var child in _container.GetChildren())
        {
            if (child is Zone found)
            {
                scar = found;
                break;
            }
        }
        AssertThat(scar).IsNotNull();
        AssertThat(scar!.Duration).IsGreater(1000.0f);

        _player.Stats!.SetBase("block", 0.0f);
        _player.Stats.SetBase("evasion", 0.0f);
        float hpBefore = _player.Health;
        core._PhysicsProcess(1.0);
        AssertThat(_player.Health).IsLess(hpBefore);

        GD.Print("[PASS] H. pylori Biofilm Core left permanent acid muck and pulsed its spiral toxin storm.");
        core.QueueFree();
    }

    private void TestPrpscAmyloidAggregate()
    {
        var amyloid = EnemySpawner.CreateEnemy("prpsc_amyloid_aggregate")!;
        amyloid.GlobalPosition = new Vector2(3600, 3600);
        _container!.AddChild(amyloid);

        AssertThat(amyloid.ShellIntact).IsTrue();
        AssertThat(amyloid.ShellMax).IsGreater(0.0f);
        AssertThat(amyloid.Armor).IsEqual(25.0f);

        amyloid.TakeDamage(100.0f);
        AssertThat(amyloid.ShellIntact).IsTrue();
        AssertThat(amyloid.ShellHealth).IsLess(amyloid.ShellMax);
        AssertThat(amyloid.CurrentHealth).IsGreater(amyloid.MaxHealth * 0.9f);

        amyloid.TakeDamage(2000.0f);
        AssertThat(amyloid.ShellBroken).IsTrue();
        AssertThat(amyloid.ShellIntact).IsFalse();
        AssertThat(amyloid.Armor).IsEqual(6.0f);

        GD.Print("[PASS] PrPsc Amyloid Aggregate crystalline shell absorbed damage until shattered.");
        amyloid.QueueFree();
    }
}
