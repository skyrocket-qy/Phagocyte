using Godot;
using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Enemies;
using Phagocyte.Player;
using Phagocyte.Skills;

namespace Phagocyte.Tests;

/// <summary>
/// Visual-effect presence tests for the four restored skills:
/// MAC assembly mine + detonation blast, Y-missile flight,
/// lance beam + pore decals, grasp chain. Spawned visuals assert
/// synchronously; timer-gated ones fast-forward deterministically.
/// </summary>
[TestSuite]
public partial class TestSkillVisuals : TestHarness
{
    private int _phase = 0;
    private int _frameCount = 0;

    private Macrophage? _player;
    private Node2D? _arena;

    public override void _Initialize()
    {
        Banner("STARTING SKILL VISUAL EFFECTS VERIFICATION");
    }

    public override bool _Process(double delta)
    {
        switch (_phase)
        {
            case 0:
                _frameCount++;
                if (_frameCount < 3)
                    return false;
                _frameCount = 0;
                if (!SetupArena())
                {
                    GD.PrintErr("[FAIL] TestSkillVisuals setup failed; aborting.");
                    Quit(1);
                    return true;
                }

                // Complement mine spawns synchronously on trigger.
                var comp = new ComplementCascadeSkill();
                AssertThat(_player!.CellSkillManager!.EquipActive(comp, 1)).IsTrue();
                Spawn<TbEnemy>(new Vector2(150, 0));
                comp.Trigger();
                var mines = Collect<ComplementCascadeSkill.MacAssemblyMine>(_arena!);
                AssertThat(mines.Count).IsGreater(0);
                GD.Print("[PASS] Test 1: MAC assembly mine visual spawned on trigger.");

                // Fast-forward the fuse deterministically.
                mines[0].AssemblyTime = 0.05f;
                _phase = 1;
                return false;

            case 1:
                _frameCount++;
                if (_frameCount < 25)
                    return false;
                _frameCount = 0;

                var blasts = Collect<ComplementCascadeSkill.MacDetonationBlast>(_arena!);
                AssertThat(blasts.Count).IsGreater(0);
                GD.Print("[PASS] Test 2: MAC detonation blast visual fired after assembly.");

                // Antibody missile arrives via launch timer; wait it out.
                var ab = new AntibodySalvoSkill();
                AssertThat(_player!.CellSkillManager!.EquipActive(ab, 2)).IsTrue();
                Spawn<TbEnemy>(new Vector2(200, 0));
                ab.Trigger();
                _phase = 2;
                return false;

            case 2:
                _frameCount++;
                if (_frameCount < 60)
                    return false;
                _frameCount = 0;

                var missiles = Collect<AntibodyMissile>(_arena!);
                AssertThat(missiles.Count).IsGreater(0);
                GD.Print("[PASS] Test 3: Y-shaped antibody missile in flight.");

                // Perforin beam + pore decals spawn synchronously.
                var perf = new PerforinLanceSkill();
                AssertThat(_player!.CellSkillManager!.EquipActive(perf, 3)).IsTrue();
                Spawn<TbEnemy>(new Vector2(150, 0));
                perf.Trigger();
                AssertThat(Collect<PerforinLanceSkill.LanceBeamVisual>(_arena!).Count).IsGreater(0);
                AssertThat(Collect<PerforinLanceSkill.PoreDecal>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 4: perforin lance beam + pore decals spawned on strike.");

                // Grasp chain spawns synchronously per grabbed target.
                Spawn<TbEnemy>(new Vector2(100, 0));
                var grasp = _player.CellSkillManager.GetActiveSlot(0) as PhagocyticGraspSkill;
                AssertThat(grasp).IsNotNull();
                grasp!.Trigger();
                AssertThat(Collect<PseudopodChainVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 5: pseudopod grasp chain visual spawned on grab.");

                Finish(true, "ALL SKILL VISUAL EFFECT TESTS");
                return true;
        }

        return false;
    }

    private bool SetupArena()
    {
        _arena = new Node2D { Name = "VisualArena" };
        Root.AddChild(_arena);

        var playerScene = GD.Load<PackedScene>("res://scenes/characters/macrophage.tscn");
        if (playerScene == null)
        {
            GD.PrintErr("[DIAG] macrophage.tscn failed to load.");
            return false;
        }
        _player = playerScene.Instantiate<Macrophage>();
        if (_player == null)
        {
            GD.PrintErr("[DIAG] macrophage scene instantiated null.");
            return false;
        }
        _arena.AddChild(_player);
        if (_player.Stats == null || _player.CellSkillManager == null)
            return false;
        _player.GlobalPosition = Vector2.Zero;
        _player.Stats.SetBase("crit_chance", 0.0f);
        _player.Stats.SetBase("max_health", 999999.0f);
        _player.Health = 999999.0f;
        _player.SetPhysicsProcess(false);

        return true;
    }

    private T Spawn<T>(Vector2 pos) where T : BaseEnemy, new()
    {
        var enemy = new T { GlobalPosition = pos };
        _arena!.AddChild(enemy);
        return enemy;
    }

    private static List<T> Collect<T>(Node root) where T : Node
    {
        var found = new List<T>();
        CollectInto(root, found);
        return found;
    }

    private static void CollectInto<T>(Node node, List<T> found) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T typed)
                found.Add(typed);
            CollectInto(child, found);
        }
    }
}
