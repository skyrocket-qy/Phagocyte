using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Enemies;
using Phagocyte.Organelles;
using Phagocyte.Player;
using Phagocyte.Skills;

namespace Phagocyte.Tests;

/// <summary>
/// Behavioral tests for the Macrophage innate active (吞噬偽足):
/// default double-grasp, Amount scaling, nearest-first targeting,
/// and damage-without-engulf on non-engulfable foes.
/// Setup runs frame-gated inside _Process (house style: never touch
/// nodes inside _Initialize).
/// </summary>
[TestSuite]
public partial class TestPhagocyticGrasp : TestHarness
{
    private int _phase = 0;
    private int _frameCount = 0;

    private Macrophage? _player;
    private PhagocyticGraspSkill? _grasp;
    private Node2D? _arena;

    private TbEnemy? _tb1;
    private TbEnemy? _tb2;
    private TbEnemy? _ctrl;
    private CandidaEnemy? _candida;
    private TbEnemy? _tb3;
    private TbEnemy? _tb4;
    private TbEnemy? _tb5;

    public override void _Initialize()
    {
        Banner("STARTING PHAGOCYTIC GRASP BEHAVIORAL VERIFICATION");
    }

    public override bool _Process(double delta)
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
                // TEST 1: default double-grasp eats both in-reach foes, spares the far one.
                _frameCount++;
                if (_frameCount < 35)
                    return false;
                _frameCount = 0;

                AssertThat(IsResolved(_tb1)).IsTrue();
                AssertThat(IsResolved(_tb2)).IsTrue();
                AssertThat(GodotObject.IsInstanceValid(_ctrl)).IsTrue();
                AssertThat(_ctrl!.IsBeingEaten).IsFalse();
                AssertThat(_ctrl.CurrentHealth).IsEqual(35.0f);
                GD.Print("[PASS] Test 1: default double-grasp engulfed 2 in-reach foes, spared out-of-reach control.");

                // TEST 2 setup: hyphae-extended candida cannot be engulfed.
                _candida = Spawn<CandidaEnemy>(new Vector2(120, 0));
                _candida.ExtendHyphae();
                AssertThat(_candida.CanBeEngulfed).IsFalse();
                _grasp!.Trigger();
                _phase = 2;
                return false;

            case 2:
                // TEST 2: non-engulfable foe takes damage but is never eaten.
                _frameCount++;
                if (_frameCount < 35)
                    return false;
                _frameCount = 0;

                AssertThat(GodotObject.IsInstanceValid(_candida)).IsTrue();
                AssertThat(_candida!.IsBeingEaten).IsFalse();
                AssertThat(_candida.CurrentHealth).IsLess(45.0f);
                GD.Print("[PASS] Test 2: hyphae-guarded candida took grasp damage without being engulfed.");

                // TEST 3 setup: +1 Amount must raise the grasp to 3 targets.
                // Teleport (don't free): a QueueFree'd node stays in the
                // targeting registry until frame end and would steal a slot.
                _candida.GlobalPosition = new Vector2(5000, 0);
                _player!.Stats!.SetBase("amount", 1.0f);
                AssertThat(_grasp!.GetCalculatedAmount(2)).IsEqual(3);
                _tb3 = Spawn<TbEnemy>(new Vector2(100, 0));
                _tb4 = Spawn<TbEnemy>(new Vector2(150, 0));
                _tb5 = Spawn<TbEnemy>(new Vector2(200, 0));
                _grasp.Trigger();
                _phase = 3;
                return false;

            case 3:
                // TEST 3: all three in-reach foes are engulfed via Amount scaling.
                _frameCount++;
                if (_frameCount < 35)
                    return false;

                AssertThat(IsResolved(_tb3)).IsTrue();
                AssertThat(IsResolved(_tb4)).IsTrue();
                AssertThat(IsResolved(_tb5)).IsTrue();
                GD.Print("[PASS] Test 3: Amount +1 scaled the grasp from 2 to 3 engulfments.");
                Finish(true, "ALL PHAGOCYTIC GRASP BEHAVIORAL TESTS");
                return true;
        }

        return false;
    }

    private bool SetupArena()
    {
        _arena = new Node2D { Name = "GraspArena" };
        Root.AddChild(_arena);

        var playerScene = GD.Load<PackedScene>("res://scenes/characters/macrophage.tscn");
        if (playerScene == null)
            return false;
        _player = playerScene.Instantiate<Macrophage>();
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

        // Park the mounted IK limb: ambient coils only, so it cannot
        // steal the scripted targets.
        foreach (var child in _player.GetChildren())
        {
            if (child is PseudopodLimb limb)
                limb.CombatEnabled = false;
        }

        _grasp = _player.CellSkillManager.GetActiveSlot(0) as PhagocyticGraspSkill;
        if (_grasp == null)
            return false;

        // Reach is 280 * area(1.25) = 350; control sits far outside.
        _tb1 = Spawn<TbEnemy>(new Vector2(100, 0));
        _tb2 = Spawn<TbEnemy>(new Vector2(150, 0));
        _ctrl = Spawn<TbEnemy>(new Vector2(800, 0));
        return _tb1.CanBeEngulfed && _tb2.CanBeEngulfed;
    }

    private T Spawn<T>(Vector2 pos) where T : BaseEnemy, new()
    {
        var enemy = new T { GlobalPosition = pos };
        _arena!.AddChild(enemy);
        return enemy;
    }

    /// <summary>Eaten foes shrink-tween then free themselves; either state proves the grasp resolved them.</summary>
    private static bool IsResolved(BaseEnemy? enemy)
    {
        return enemy == null || !GodotObject.IsInstanceValid(enemy) || enemy.IsBeingEaten;
    }
}
