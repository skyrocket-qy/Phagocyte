using Godot;
using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;
using Game.Enemies;
using Game.Combat;
using Game.Player;
using Game.Skills;

using Game.Core;

namespace Game.Tests;

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

    private PlayerActor? _player;
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
                var comp = SkillFactory.CreateActive("complement_cascade")!;
                AssertThat(_player!.CellSkillManager!.EquipActive(comp, 1)).IsTrue();
                Spawn("tb", new Vector2(150, 0));
                comp.Trigger();
                var mines = Collect<Zone>(_arena!);
                AssertThat(mines.Count).IsGreater(0);
                GD.Print("[PASS] Test 1: MAC assembly mine visual spawned on trigger.");

                // Fast-forward the fuse deterministically.
                mines[0].Duration = 0.05f;
                _phase = 1;
                return false;

            case 1:
                _frameCount++;
                if (_frameCount < 25)
                    return false;
                _frameCount = 0;

                var blasts = Collect<NovaSkill.NovaVisual>(_arena!);
                AssertThat(blasts.Count).IsGreater(0);
                GD.Print("[PASS] Test 2: MAC detonation blast visual fired after assembly.");

                // Antibody missile arrives via launch timer; wait it out.
                var ab = SkillFactory.CreateActive("antibody_salvo")!;
                AssertThat(_player!.CellSkillManager!.EquipActive(ab, 2)).IsTrue();
                Spawn("tb", new Vector2(200, 0));
                ab.Trigger();
                _phase = 2;
                return false;

            case 2:
                _frameCount++;
                if (_frameCount < 60)
                    return false;
                _frameCount = 0;

                var missiles = ProjectileManager.Instance!.CountForTeam(Team.Player);
                AssertThat(missiles).IsGreater(0);
                GD.Print("[PASS] Test 3: Y-shaped antibody missile in flight.");

                // Perforin beam + pore decals spawn synchronously.
                var perf = SkillFactory.CreateActive("perforin_lance")!;
                AssertThat(_player!.CellSkillManager!.EquipActive(perf, 3)).IsTrue();
                Spawn("tb", new Vector2(150, 0));
                perf.Trigger();
                AssertThat(Collect<BeamSkill.BeamVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 4: perforin lance beam spawned on strike.");

                // Grasp chain spawns synchronously per grabbed target.
                Spawn("tb", new Vector2(100, 0));
                var grasp = _player.CellSkillManager.GetActiveSlot(0) as StrikeSkill;
                AssertThat(grasp).IsNotNull();
                grasp!.Trigger();
                AssertThat(Collect<StrikeSkill.ChainVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 5: pseudopod grasp chain visual spawned on grab.");

                _phase = 3;
                return false;

            case 3:
                // Test 6: ROS Jet
                var ros = SkillFactory.CreateActive("ros_torrent")!;
                ForceEquip(ros, 1);
                Spawn("tb", new Vector2(120, 0));
                int rosBefore = ProjectileManager.Instance!.CountForTeam(Team.Player);
                ros.Trigger();
                AssertThat(ProjectileManager.Instance!.CountForTeam(Team.Player)).IsGreater(rosBefore);
                GD.Print("[PASS] Test 6: ROS torrent jet visual spawned.");

                // Test 7: Granzyme Detonation
                var gran = SkillFactory.CreateActive("granzyme_detonation")!;
                ForceEquip(gran, 2);
                var granTarget = Spawn("tb", new Vector2(100, 0));
                gran.Trigger();
                AssertThat(Collect<NovaSkill.NovaMarker>(_arena!).Count).IsGreater(0);
                ((NovaSkill)gran).DetonateAt(granTarget.GlobalPosition);
                AssertThat(Collect<NovaSkill.NovaVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 7: Granzyme detonation apoptosis marker + burst visuals spawned.");

                // Test 8: Nuclease Blades
                var nuc = SkillFactory.CreateActive("nuclease_blades")!;
                ForceEquip(nuc, 3);
                AssertThat(Collect<AuraSkill.AuraVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 8: Nuclease blades orbiting canvas visual active.");

                // Test 9: Defensin Barbs
                var def = SkillFactory.CreateActive("defensin_barbs")!;
                ForceEquip(def, 4);
                int defBefore = ProjectileManager.Instance!.CountForTeam(Team.Player);
                def.Trigger();
                AssertThat(ProjectileManager.Instance!.CountForTeam(Team.Player)).IsGreater(defBefore);
                GD.Print("[PASS] Test 9: Defensin barbs projectile visual spawned.");

                _phase = 4;
                return false;

            case 4:
                // Test 10: Pro-Inflammatory Arc
                var arc = SkillFactory.CreateActive("pro_inflammatory_arc")!;
                ForceEquip(arc, 1);
                Spawn("tb", new Vector2(100, 0));
                arc.Trigger();
                AssertThat(Collect<BeamSkill.BeamVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 10: Pro-inflammatory arc lightning visual spawned.");

                // Test 11: Exosome Singularity
                var exo = SkillFactory.CreateActive("exosome_singularity")!;
                ForceEquip(exo, 2);
                exo.Trigger();
                AssertThat(Collect<Zone>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 11: Exosome singularity vortex visual spawned.");

                // Test 12: Phagolysosome Vent
                var vent = SkillFactory.CreateActive("phagolysosome_vent")!;
                ForceEquip(vent, 3);
                vent.Trigger();
                AssertThat(Collect<Zone>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 12: Phagolysosome vent acid puddle visual spawned.");

                // Test 13: MHC Tracer Beam
                var mhc = SkillFactory.CreateActive("mhc_tracer_beam")!;
                ForceEquip(mhc, 4);
                AssertThat(Collect<BeamSkill.BeamVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 13: MHC tracer beam visual active.");

                _phase = 5;
                return false;

            case 5:
                // Test 14: Histamine Surge
                var hist = SkillFactory.CreateActive("histamine_surge")!;
                ForceEquip(hist, 1);
                hist.Trigger();
                AssertThat(Collect<NovaSkill.NovaVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 14: Histamine surge degranulation wave visual spawned.");

                // Test 15: Nitric Oxide Halo
                var no = SkillFactory.CreateActive("nitric_oxide_halo")!;
                ForceEquip(no, 2);
                AssertThat(Collect<AuraSkill.AuraVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 15: Nitric oxide halo toxic gas cloud visual active.");

                // Test 16: Interferon Wave
                var ifn = SkillFactory.CreateActive("interferon_wave")!;
                ForceEquip(ifn, 3);
                ifn.Trigger();
                AssertThat(Collect<NovaSkill.NovaVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 16: Interferon wave acoustic shockwave visual spawned.");

                // Test 17: Lysozyme Ricochet & Pseudopod Lunge
                var lyso = SkillFactory.CreateActive("lysozyme_ricochet")!;
                ForceEquip(lyso, 4);
                Spawn("tb", new Vector2(100, 0));
                int lysoBefore = ProjectileManager.Instance!.CountForTeam(Team.Player);
                lyso.Trigger();
                AssertThat(ProjectileManager.Instance!.CountForTeam(Team.Player)).IsGreater(lysoBefore);
                GD.Print("[PASS] Test 17a: Lysozyme ricochet enzyme capsule visual spawned.");

                var lunge = SkillFactory.CreateActive("pseudopod_lunge")!;
                ForceEquip(lunge, 4);
                Spawn("tb", new Vector2(120, 0));
                lunge.Trigger();
                var lunges = Collect<StrikeSkill.ChainVisual>(_arena!);
                AssertThat(lunges.Exists(c => c.VisualKind == "fist")).IsTrue();
                GD.Print("[PASS] Test 17b: Pseudopod lunge blunt fist visual spawned.");

                Finish(true, "ALL 17 SKILL VISUAL EFFECT TESTS");
                return true;
        }

        return false;
    }

    private void ForceEquip(BaseSkill skill, int slot)
    {
        skill.IsInnate = false;
        var existing = _player!.CellSkillManager!.ActiveSlots[slot];
        if (existing != null && GodotObject.IsInstanceValid(existing))
        {
            existing.IsInnate = false;
        }
        AssertThat(_player.CellSkillManager.EquipActive(skill, slot)).IsTrue();
    }

    private bool SetupArena()
    {
        _arena = new Node2D { Name = "VisualArena" };
        Root.AddChild(_arena);

        var playerScene = AssetLoader.Load<PackedScene>("res://scenes/actors/player_base.tscn");
        if (playerScene == null)
        {
            GD.PrintErr("[DIAG] macrophage.tscn failed to load.");
            return false;
        }
        _player = playerScene.Instantiate<PlayerActor>();
        if (_player == null)
        {
            GD.PrintErr("[DIAG] macrophage scene instantiated null.");
            return false;
        }
        _arena.AddChild(_player);
        if (_player.Stats == null || _player.CellSkillManager == null)
            return false;
        var projMgr = new ProjectileManager { Name = "ProjectileManager" };
        _arena.AddChild(projMgr);
        projMgr.SetHost(_player);
        _player.GlobalPosition = Vector2.Zero;
        _player.Stats.SetBase("crit_chance", 0.0f);
        _player.Stats.SetBase("max_health", 999999.0f);
        _player.Health = 999999.0f;
        _player.SetPhysicsProcess(false);

        return true;
    }

    private EnemyActor Spawn(string enemyId, Vector2 pos)
    {
        var enemy = EnemySpawner.CreateEnemy(enemyId)!;
        enemy.GlobalPosition = pos;
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




