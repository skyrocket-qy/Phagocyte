using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Combat;
using Phagocyte.Enemies;
using Phagocyte.Player;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the five 15:00 map terminal bosses and their signature mechanics.
/// </summary>
[TestSuite]
public partial class TestTerminalBosses : SceneTree
{
    private int _frame = 0;
    private bool _done = false;
    private Node2D? _container;
    private BaseCell? _player;

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
        _player = new BaseCell { Name = "TerminalBossHost", GlobalPosition = new Vector2(400, 400) };
        _container.AddChild(_player);

        TestMapMapping();
        TestMrsaSuperColony();
        TestSyncytialMegaCapsid();
        TestPlasmodiumMacroSchizont();
        TestHpyloriBiofilmCore();
        TestPrpscAmyloidAggregate();

        _container.QueueFree();
    }

    private void TestMapMapping()
    {
        AssertThat(PathogenSpawner.CreateTerminalBoss("acute_wound") is MrsASuperColony).IsTrue();
        AssertThat(PathogenSpawner.CreateTerminalBoss("alveolar_space") is SyncytialMegaCapsid).IsTrue();
        AssertThat(PathogenSpawner.CreateTerminalBoss("hepatic_sinusoid") is PlasmodiumMacroSchizont).IsTrue();
        AssertThat(PathogenSpawner.CreateTerminalBoss("gastric_lumen") is HpyloriBiofilmCore).IsTrue();
        AssertThat(PathogenSpawner.CreateTerminalBoss("blood_brain_barrier") is PrpscAmyloidAggregate).IsTrue();

        var spawned = PathogenSpawner.SpawnTerminalBoss(_container!, _player!, new Vector2(4800, 4800), "acute_wound");
        AssertThat(spawned).IsNotNull();
        AssertThat(spawned!.IsBoss).IsTrue();
        AssertThat(spawned.CanBeEngulfed).IsFalse();
        AssertThat(spawned.GetNodeOrNull("BossPhaseComponent")).IsNotNull();
        spawned.QueueFree();

        GD.Print("[PASS] All five maps map to their dedicated terminal boss entity with boss phases.");
    }

    private void TestMrsaSuperColony()
    {
        var mrsa = new MrsASuperColony { GlobalPosition = new Vector2(900, 900) };
        _container!.AddChild(mrsa);

        mrsa.TakeDamage(9999999.0f);
        AssertThat(mrsa.HasSplit).IsTrue();

        int elites = 0;
        foreach (var child in _container.GetChildren())
        {
            if (child is MrsaEnragedElite)
                elites++;
        }

        AssertThat(elites).IsEqual(MrsASuperColony.SplitCount);
        GD.Print("[PASS] MRSA Super-Colony ruptured into four enraged child elites.");
    }

    private void TestSyncytialMegaCapsid()
    {
        var capsid = new SyncytialMegaCapsid { GlobalPosition = _player!.GlobalPosition + new Vector2(120, 0) };
        _container!.AddChild(capsid);

        _player.SlowTimer = 0.0f;
        capsid._PhysicsProcess(0.016);
        AssertThat(_player.SlowTimer).IsGreater(0.0f);

        Vector2 velocityBefore = _player.Velocity;
        capsid.TriggerTractionPulse();
        AssertThat(capsid.TractionPulses).IsEqual(1);
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
        var schizont = new PlasmodiumMacroSchizont { GlobalPosition = new Vector2(1800, 1800) };
        _container!.AddChild(schizont);

        schizont.TakeDamage(300.0f);
        float hpBefore = schizont.CurrentHealth;
        schizont.FeedOnRbc();
        AssertThat(schizont.FeedsCount).IsEqual(1);
        AssertThat(schizont.CurrentHealth).IsGreater(hpBefore);

        schizont.TakeDamage(9999999.0f);
        AssertThat(schizont.HasRuptured).IsTrue();

        int merozoites = 0;
        foreach (var child in _container.GetChildren())
        {
            if (child is PlasmodiumMerozoite)
                merozoites++;
        }

        AssertThat(merozoites).IsGreaterEqual(PlasmodiumMacroSchizont.MerozoiteBurstCount);
        GD.Print("[PASS] Plasmodium Macro-Schizont fed on RBCs to heal and burst into merozoites.");
    }

    private void TestHpyloriBiofilmCore()
    {
        var core = new HpyloriBiofilmCore { GlobalPosition = _player!.GlobalPosition + new Vector2(60, 0) };
        _container!.AddChild(core);

        core.SpawnAcidScar();
        HpyloriAcidScar? scar = null;
        foreach (var child in _container.GetChildren())
        {
            if (child is HpyloriAcidScar found)
            {
                scar = found;
                break;
            }
        }
        AssertThat(scar).IsNotNull();
        AssertThat(scar!.Duration).IsGreater(1000.0f);

        float hpBefore = _player.Health;
        core.TriggerToxinTick();
        AssertThat(core.ToxinTicks).IsEqual(1);
        AssertThat(_player.Health).IsLess(hpBefore);

        GD.Print("[PASS] H. pylori Biofilm Core left permanent acid muck and pulsed its spiral toxin storm.");
        core.QueueFree();
    }

    private void TestPrpscAmyloidAggregate()
    {
        var amyloid = new PrpscAmyloidAggregate { GlobalPosition = new Vector2(3600, 3600) };
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
        AssertThat(amyloid.Armor).IsEqual(PrpscAmyloidAggregate.BrokenArmor);

        GD.Print("[PASS] PrPsc Amyloid Aggregate crystalline shell absorbed damage until shattered.");
        amyloid.QueueFree();
    }
}
