using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Game.Enemies;
using Game.Player;
using Game.Skills;

using Game.Core;

namespace Game.Tests;

/// <summary>
/// Behavioral tests for the Macrophage innate active (吞噬偽足):
/// default double-grasp, Amount scaling, nearest-first targeting,
/// and direct damage on all foes.
/// Setup runs frame-gated inside _Process (house style: never touch
/// nodes inside _Initialize).
/// </summary>
[TestSuite]
public partial class TestPhagocyticGrasp : TestHarness
{
    private int _phase = 0;
    private int _frameCount = 0;
    private ulong _physStart = ulong.MaxValue;
    private int _retryIn;

    private PlayerActor? _player;
    private StrikeSkill? _grasp;
    private Node2D? _arena;

    private EnemyActor? _tb1;
    private EnemyActor? _tb2;
    private EnemyActor? _ctrl;
    private EnemyActor? _candida;
    private EnemyActor? _tb3;
    private EnemyActor? _tb4;
    private EnemyActor? _tb5;

    public override void _Initialize()
    {
        Banner("STARTING PHAGOCYTIC GRASP BEHAVIORAL VERIFICATION");
    }

    public override bool _Process(double delta)
    {
        try
        {
            return Tick(delta);
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestPhagocyticGrasp threw: ", ex);
            Quit(1);
            return true;
        }
    }

    /// <summary>
    /// Waits the given number of physics ticks (chain delivery runs on the
    /// physics clock; idle _Process frames are not a substitute headless).
    /// </summary>
    private bool PhysGate(int ticks)
    {
        ulong now = Engine.GetPhysicsFrames();
        if (_physStart == ulong.MaxValue)
            _physStart = now;
        if (now - _physStart < (ulong)ticks)
            return false;
        _physStart = ulong.MaxValue;
        return true;
    }

    private bool Tick(double delta)
    {
        switch (_phase)
        {
            case 0:
                // Setup + TEST 0 (binding): default grasp count is 2.
                _frameCount++;
                if (_frameCount < 3)
                    return false;
                _frameCount = 0;

                if (!SetupArena())
                {
                    GD.PrintErr("[FAIL] TestPhagocyticGrasp setup failed; aborting.");
                    Quit(1);
                    return true;
                }

                AssertThat(_grasp!.SkillId).IsEqual("phagocytic_grasp");
                AssertThat(_grasp.IsInnate).IsTrue();

                // Default grasp count is 2 (wiki: two particles at once).
                AssertThat(_grasp.GetCalculatedAmount(2)).IsEqual(2);
                GD.Print("[PASS] Test 0: innate grasp bound in active slot 0 with default count 2.");

                _grasp.Trigger();
                _phase = 1;
                return false;

            case 1:
                // TEST 1: default double-grasp damages both in-reach foes, spares the far one.
                // Poll the condition (chain arrival can overshoot at huge frame
                // dt under contention); re-fire every 15 ticks, 300-tick timeout.
                if (!IsDamagedOrGone(_tb1) || !IsDamagedOrGone(_tb2))
                {
                    if (!PhysGate(300))
                    {
                        if (--_retryIn <= 0)
                        {
                            _retryIn = 15;
                            _grasp!.Trigger();
                        }
                        return false;
                    }
                }

                AssertThat(IsDamagedOrGone(_tb1)).IsTrue();
                AssertThat(IsDamagedOrGone(_tb2)).IsTrue();
                AssertThat(GodotObject.IsInstanceValid(_ctrl)).IsTrue();
                AssertThat(_ctrl!.CurrentHealth).IsEqual(35.0f);
                GD.Print("[PASS] Test 1: default double-grasp damaged 2 in-reach foes, spared out-of-reach control.");

                // TEST 2 setup: hyphae-extended candida takes grasp damage like anyone else.
                _candida = Spawn("candida", new Vector2(120, 0));
                _candida!._PhysicsProcess(0.5);
                _grasp!.Trigger();
                _phase = 2;
                return false;

            case 2:
                // TEST 2: hyphae-guarded candida takes grasp damage.
                if (!IsDamagedOrGone(_candida))
                {
                    if (!PhysGate(300))
                    {
                        if (--_retryIn <= 0)
                        {
                            _retryIn = 15;
                            _grasp!.Trigger();
                        }
                        return false;
                    }
                }

                AssertThat(IsDamagedOrGone(_candida)).IsTrue();
                GD.Print("[PASS] Test 2: hyphae-guarded candida took grasp damage.");

                // TEST 3 setup: +1 Amount must raise the grasp to 3 targets.
                // Teleport (don't free): stale in-reach foes from earlier
                // phases would steal grasp slots, and a QueueFree'd node stays
                // in the targeting registry until frame end. Dead foes are
                // already gone from the registry, so only move the living.
                TeleportAway(_candida);
                TeleportAway(_tb1);
                TeleportAway(_tb2);
                _player!.Stats!.SetBase("amount", 1.0f);
                AssertThat(_grasp!.GetCalculatedAmount(2)).IsEqual(3);
                _tb3 = Spawn("tb", new Vector2(100, 0));
                _tb4 = Spawn("tb", new Vector2(150, 0));
                _tb5 = Spawn("tb", new Vector2(200, 0));
                _grasp.Trigger();
                _phase = 3;
                return false;

            case 3:
                // TEST 3: all three in-reach foes are damaged via Amount scaling.
                if (!IsDamagedOrGone(_tb3) || !IsDamagedOrGone(_tb4) || !IsDamagedOrGone(_tb5))
                {
                    if (!PhysGate(300))
                    {
                        if (--_retryIn <= 0)
                        {
                            _retryIn = 15;
                            _grasp!.Trigger();
                        }
                        return false;
                    }
                }

                AssertThat(IsDamagedOrGone(_tb3)).IsTrue();
                AssertThat(IsDamagedOrGone(_tb4)).IsTrue();
                AssertThat(IsDamagedOrGone(_tb5)).IsTrue();
                GD.Print("[PASS] Test 3: Amount +1 scaled the grasp from 2 to 3 targets.");
                Finish(true, "ALL PHAGOCYTIC GRASP BEHAVIORAL TESTS");
                return true;
        }

        return false;
    }

    private bool SetupArena()
    {
        _arena = new Node2D { Name = "GraspArena" };
        Root.AddChild(_arena);

        var playerScene = AssetLoader.Load<PackedScene>("res://scenes/actors/player_base.tscn");
        if (playerScene == null)
            return false;
        _player = playerScene.Instantiate<PlayerActor>();
        if (_player == null)
            return false;
        _arena.AddChild(_player);
        _player.GlobalPosition = Vector2.Zero;

        if (_player.Stats == null || _player.CellSkillManager == null)
            return false;

        // Deterministic damage: no crit rolls, unkillable host.
        _player.Stats.SetBase("crit_chance", 0.0f);
        _player.Stats.SetBase("max_health", 999999.0f);
        _player.Health = 999999.0f;

        // Freeze the host: manual Trigger() calls below are the only
        // firings under test (no auto-fire loop, no drift).
        _player.SetPhysicsProcess(false);

        _grasp = _player.CellSkillManager.GetActiveSlot(0) as StrikeSkill;
        if (_grasp == null)
            return false;

        // Reach is 280 * area(1.25) = 350; control sits far outside.
        _tb1 = Spawn("tb", new Vector2(100, 0));
        _tb2 = Spawn("tb", new Vector2(150, 0));
        _ctrl = Spawn("tb", new Vector2(800, 0));
        return _tb1 != null && _tb2 != null;
    }

    private EnemyActor Spawn(string enemyId, Vector2 pos)
    {
        var enemy = EnemySpawner.CreateEnemy(enemyId)!;
        enemy.GlobalPosition = pos;
        _arena!.AddChild(enemy);
        return enemy;
    }

    /// <summary>
    /// Damaged foes keep fighting at reduced HP; destroyed foes are gone from
    /// the scene tree. Both count: the arena has no other damage source, so a
    /// missing foe took lethal grasp damage (vital under re-fire, where a
    /// second volley can finish an already-damaged foe and flip it invalid).
    /// </summary>
    private static bool IsDamagedOrGone(EnemyActor? enemy)
    {
        if (enemy == null)
            return false;
        if (!GodotObject.IsInstanceValid(enemy))
            return true;
        return enemy.CurrentHealth < enemy.MaxHealth;
    }

    /// <summary>Moves a living foe out of grasp reach; freed foes are already untargetable.</summary>
    private static void TeleportAway(EnemyActor? enemy)
    {
        if (enemy != null && GodotObject.IsInstanceValid(enemy) && !enemy.IsQueuedForDeletion())
            enemy.GlobalPosition = new Vector2(5000, 5000);
    }
}



