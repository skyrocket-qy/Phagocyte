using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;
using System.Diagnostics;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;

namespace Phagocyte.Tests;

/// <summary>
/// Tick-budget profiler for the 6fps-pin investigation: times the per-enemy
/// physics tick, the contact-strike storm and the player damage pipeline with
/// 300 live enemies, so the hot section is data instead of guesses. Bounds are
/// generous regression alarms, not targets.
/// </summary>
[TestSuite]
public partial class TestTickBudget : TestHarness
{
    private const int EnemyCount = 300;
    private const int TickIters = 60;
    private const float TickDt = 1.0f / 60.0f;

    private int _phase = 0;
    private int _framesWaited = 0;
    private bool _testDone = false;
    private Main? _main;
    private readonly List<BaseEnemy> _enemies = new();

    public override void _Initialize()
    {
        Banner("STARTING TICK BUDGET PROFILING");
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        switch (_phase)
        {
            case 0:
                _framesWaited++;
                if (_framesWaited < 3)
                    return false;
                _framesWaited = 0;
                SetupArena();
                _phase = 1;
                return false;

            case 1:
                ProfileDriftTicks();
                _phase = 2;
                return false;

            case 2:
                ProfileContactStorm();
                _phase = 3;
                return false;

            case 3:
                ProfilePlayerDamage();
                _phase = 4;
                return false;

            case 4:
                ProfileStatsRolls();
                _phase = 5;
                return false;

            case 5:
                ProfileStatsSignal();
                DeleteIfExists(RunTelemetryManager.SpikeLogPath);
                RunTelemetryManager.SpikeLogPath = "";
                _main?.QueueFree();
                _testDone = true;
                GD.Print(">>> ALL TICK BUDGET TESTS PASSED SUCCESSFULLY! <<<");
                Quit(0);
                return true;

            default:
                return true;
        }
    }

    private void SetupArena()
    {
        GameManager.SelectedClass = "macrophage";
        GameManager.SelectedMap = "acute_wound";

        var mainScene = AssetLoader.Load<PackedScene>("res://scenes/main.tscn");
        _main = mainScene.Instantiate<Main>();
        Root.AddChild(_main);
        MakePlayerInvulnerable(_main);
        RunTelemetryManager.SpikeLogPath = JsonStore.ResolvePath("test_tickbudget_spikes.json");

        if (_main!.EnemyContainer != null)
        {
            foreach (var child in _main.EnemyContainer.GetChildren())
            {
                if (child is BaseEnemy enemy)
                    enemy.Free();
            }
        }

        Vector2 center = _main.Player != null ? (_main.Player as Node2D)?.GlobalPosition ?? Vector2.Zero : Vector2.Zero;
        for (int i = 0; i < EnemyCount; i++)
        {
            var enemy = PathogenSpawner.CreatePathogen("tb");
            AssertThat(enemy).IsNotNull();
            enemy!.GlobalPosition = center + new Vector2((i % 30) * 40.0f - 600.0f, (i / 30) * 40.0f - 200.0f);
            _main.EnemyContainer!.AddChild(enemy);
            _enemies.Add(enemy);
        }
        GD.Print($"[TickBudget] Arena ready: {_enemies.Count} tb enemies.");
    }

    /// <summary>300 enemies × 60 steering/drift ticks, driven manually.</summary>
    private void ProfileDriftTicks()
    {
        var sw = Stopwatch.StartNew();
        for (int t = 0; t < TickIters; t++)
        {
            foreach (var enemy in _enemies)
                enemy._PhysicsProcess(TickDt);
        }
        sw.Stop();
        double perEnemyUsec = sw.Elapsed.TotalMicroseconds / (TickIters * _enemies.Count);
        GD.Print($"[PASS] 1. Drift+steer: {sw.Elapsed.TotalMilliseconds:F1}ms for {TickIters}x{_enemies.Count} ticks = {perEnemyUsec:F2}us/enemy/tick.");
        AssertThat(perEnemyUsec < 100.0).IsTrue();
    }

    /// <summary>All 300 overlapping the player: one contact strike each.</summary>
    private void ProfileContactStorm()
    {
        var cell = _main!.Player as BaseCell;
        AssertThat(cell).IsNotNull();
        foreach (var enemy in _enemies)
            enemy.GlobalPosition = cell!.GlobalPosition;
        foreach (var enemy in _enemies)
            enemy.NextContactTickMsec = 0.0;

        var sw = Stopwatch.StartNew();
        int fired = 0;
        foreach (var enemy in _enemies)
        {
            if (enemy.TryContactStrike(cell!))
                fired++;
        }
        sw.Stop();
        GD.Print($"[PASS] 2. Contact storm: {fired}/{_enemies.Count} strikes in {sw.Elapsed.TotalMilliseconds:F1}ms.");
        AssertThat(fired).IsEqual(_enemies.Count);
        AssertThat(sw.Elapsed.TotalMilliseconds < 500.0).IsTrue();
    }

    /// <summary>300 direct hits through the full cell pipeline (stats/HUD/toast/libs).</summary>
    private void ProfilePlayerDamage()
    {
        var cell = _main!.Player as BaseCell;
        AssertThat(cell).IsNotNull();

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < _enemies.Count; i++)
            cell!.TakeDamage(5.0f);
        sw.Stop();
        double perHitUsec = sw.Elapsed.TotalMicroseconds / _enemies.Count;
        GD.Print($"[PASS] 3. Player damage x{_enemies.Count}: {sw.Elapsed.TotalMilliseconds:F1}ms = {perHitUsec:F2}us/hit.");
        AssertThat(sw.Elapsed.TotalMilliseconds < 500.0).IsTrue();
    }

    /// <summary>Stats rolls + DR + max_hp lookup in isolation.</summary>
    private void ProfileStatsRolls()
    {
        var cell = _main!.Player as BaseCell;
        AssertThat(cell).IsNotNull();

        var sw = Stopwatch.StartNew();
        float sink = 0.0f;
        for (int i = 0; i < _enemies.Count; i++)
        {
            if (cell!.Stats is CellStats cs && cs.RollEvasion())
                sink += 1.0f;
            if (cell!.Stats is CellStats csBlock && csBlock.RollBlock())
                sink += 1.0f;
            sink += cell!.Stats != null ? cell!.Stats.GetDamageReductionRatio() : 0.0f;
            sink += cell!.Stats != null ? cell!.Stats.GetStat("max_health") : 0.0f;
        }
        sw.Stop();
        GD.Print($"[INFO] 4. Stats rolls x{_enemies.Count}: {sw.Elapsed.TotalMilliseconds:F1}ms (sink={sink:F0}).");
    }

    /// <summary>StatsChanged emit + full HUD cascade, no damage math.</summary>
    private void ProfileStatsSignal()
    {
        var cell = _main!.Player as BaseCell;
        AssertThat(cell).IsNotNull();

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < _enemies.Count; i++)
            cell!.EmitStatsSignal();
        sw.Stop();
        double perUsec = sw.Elapsed.TotalMicroseconds / _enemies.Count;
        GD.Print($"[INFO] 5. StatsSignal x{_enemies.Count}: {sw.Elapsed.TotalMilliseconds:F1}ms = {perUsec:F2}us/emit.");
    }
}
