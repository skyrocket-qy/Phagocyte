using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the 15:00 standard wave timeline and the 3-minute escalation loop
/// (elite raid -> swarm -> sub-boss -> extreme swarm -> terminal boss lockdown).
/// </summary>
[TestSuite]
public partial class TestWaveTimeline : SceneTree
{
    private const string TestSavePath = "user://test_wave_timeline_records.json";

    private int _frame = 0;
    private bool _done = false;
    private Node2D? _container;
    private Main? _main;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING 15-MINUTE WAVE TIMELINE VERIFICATION <<<");
        GD.Print("==================================================================");
        RunRecordManager.SavePath = TestSavePath;
        if (FileAccess.FileExists(TestSavePath))
            DirAccess.RemoveAbsolute(TestSavePath);
        RunRecordManager.LoadFromDisk();
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
            RunTimelineMathTests();
            RunSpawnerEventTests();
            RunDirectorIntegrationTests();
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestWaveTimeline threw: ", ex);
            Cleanup();
            Quit(1);
            return true;
        }

        Cleanup();
        GD.Print("==================================================================");
        GD.Print(">>> ALL 15-MINUTE WAVE TIMELINE TESTS PASSED SUCCESSFULLY! <<<");
        GD.Print("==================================================================");
        Quit(0);
        return true;
    }

    private void RunTimelineMathTests()
    {
        AssertThat(PathogenSpawner.StandardRunDuration).IsEqual(900.0f);
        AssertThat(PathogenSpawner.GetPhaseIndex(0.0f)).IsEqual(0);
        AssertThat(PathogenSpawner.GetPhaseIndex(179.9f)).IsEqual(0);
        AssertThat(PathogenSpawner.GetPhaseIndex(180.0f)).IsEqual(1);
        AssertThat(PathogenSpawner.GetPhaseIndex(359.9f)).IsEqual(1);
        AssertThat(PathogenSpawner.GetPhaseIndex(360.0f)).IsEqual(2);
        AssertThat(PathogenSpawner.GetPhaseIndex(540.0f)).IsEqual(3);
        AssertThat(PathogenSpawner.GetPhaseIndex(720.0f)).IsEqual(4);
        AssertThat(PathogenSpawner.GetPhaseIndex(900.0f)).IsEqual(4);

        var terminalPool = PathogenSpawner.GetPhasePool(4);
        AssertThat(Array.IndexOf(terminalPool, "malignant_cell") >= 0).IsTrue();
        AssertThat(Array.IndexOf(terminalPool, "prion") >= 0).IsTrue();

        GD.Print("[PASS] 5-phase 3-minute escalation timeline math and terminal pool verified.");
    }

    private void RunSpawnerEventTests()
    {
        _container = new Node2D { Name = "WaveTimelineContainer" };
        Root.AddChild(_container);

        var player = new BaseCell { Name = "TimelineHost", GlobalPosition = new Vector2(400, 400) };
        _container.AddChild(player);

        var arena = new Vector2(4800.0f, 4800.0f);

        var elite = PathogenSpawner.SpawnElite(_container, player, arena, 10.0f, 2);
        AssertThat(elite).IsNotNull();
        AssertThat(elite!.IsElite).IsTrue();
        var baseline = PathogenSpawner.CreatePathogen(elite.EnemyId);
        AssertThat(baseline).IsNotNull();
        AssertThat(elite.MaxHealth).IsGreater(baseline!.MaxHealth);

        int spawned = PathogenSpawner.SpawnSwarm(_container, player, arena, 200.0f, 2);
        AssertThat(spawned).IsGreater(0);

        var subBoss = PathogenSpawner.SpawnSubBoss(_container, player, arena, "acute_wound");
        AssertThat(subBoss).IsNotNull();
        AssertThat(subBoss!.IsBoss).IsTrue();
        AssertThat(subBoss.GetNodeOrNull<BossPhaseComponent>("BossPhaseComponent")).IsNotNull();

        var terminalBoss = PathogenSpawner.SpawnTerminalBoss(_container, player, arena, "acute_wound");
        AssertThat(terminalBoss).IsNotNull();
        AssertThat(terminalBoss!.IsBoss).IsTrue();
        AssertThat(terminalBoss.MaxHealth).IsGreater(subBoss.MaxHealth);
        AssertThat(terminalBoss.GetNodeOrNull<BossPhaseComponent>("BossPhaseComponent")).IsNotNull();

        GD.Print($"[PASS] Elite raid, swarm burst ({spawned} units), sub-boss and terminal boss spawners verified.");
    }

    private void RunDirectorIntegrationTests()
    {
        GameManager.SelectedClass = "macrophage";
        GameManager.SelectedMap = "acute_wound";

        var main = GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>();
        _main = main;
        Root.AddChild(main);
        main.SetPhysicsProcess(false);

        AssertThat(main.RunGoalSeconds).IsEqual(900.0f);

        // 03:00 elite raid
        main.EnvironmentTime = PathogenSpawner.EscalationInterval - 0.01f;
        main._PhysicsProcess(0.02f);
        AssertThat(main.EliteRaidTriggered).IsTrue();
        AssertThat(main.FirstSwarmTriggered).IsFalse();

        // 06:00 first swarm + elite pincer
        main.EnvironmentTime = PathogenSpawner.EscalationInterval * 2.0f - 0.01f;
        main._PhysicsProcess(0.02f);
        AssertThat(main.FirstSwarmTriggered).IsTrue();

        // 09:00 sub-boss showdown + guaranteed reward
        main.EnvironmentTime = PathogenSpawner.EscalationInterval * 3.0f - 0.01f;
        main._PhysicsProcess(0.02f);
        AssertThat(main.SubBossTriggered).IsTrue();
        AssertThat(main.SubBoss).IsNotNull();
        AssertThat(main.SubBossRewardGranted).IsFalse();
        main.SubBoss!.TakeDamage(9999999.0f);
        AssertThat(main.SubBossRewardGranted).IsTrue();

        // 12:00 extreme swarm
        main.EnvironmentTime = PathogenSpawner.EscalationInterval * 4.0f - 0.01f;
        main._PhysicsProcess(0.02f);
        AssertThat(main.ExtremeSwarmTriggered).IsTrue();

        // 15:00 terminal boss lockdown
        main.EnvironmentTime = main.RunGoalSeconds - 0.01f;
        main._PhysicsProcess(0.02f);
        AssertThat(main.BossLockdownActive).IsTrue();
        AssertThat(main.TerminalBoss).IsNotNull();
        AssertThat(main.RunEnded).IsFalse();

        // Victory only after the terminal boss is neutralized
        main.TerminalBoss!.TakeDamage(9999999.0f);
        AssertThat(main.RunEnded).IsTrue();
        AssertThat(RunRecordManager.GetRunCount()).IsEqual(1);
        AssertThat(RunRecordManager.Records[0]["result"].AsString()).IsEqual(RunRecordManager.ResultVictory);

        GD.Print("[PASS] Wave director fired every escalation event and settled victory on terminal boss kill.");
    }

    private void Cleanup()
    {
        Paused = false;
        if (_main != null && IsInstanceValid(_main))
        {
            if (_main.GetParent() != null)
                _main.GetParent().RemoveChild(_main);
            _main.Free();
            _main = null;
        }

        if (_container != null && IsInstanceValid(_container))
        {
            _container.QueueFree();
            _container = null;
        }

        if (FileAccess.FileExists(TestSavePath))
            DirAccess.RemoveAbsolute(TestSavePath);
        RunRecordManager.SavePath = "";
    }
}
