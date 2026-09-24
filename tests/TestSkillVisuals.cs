using Godot;
using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Enemies;
using Phagocyte.Player;
using Phagocyte.Skills;

using Phagocyte.Core;

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

                _phase = 3;
                return false;

            case 3:
                // Test 6: ROS Jet
                var ros = new RosTorrentSkill();
                ForceEquip(ros, 1);
                Spawn<TbEnemy>(new Vector2(120, 0));
                ros.Trigger();
                AssertThat(Collect<RosJet>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 6: ROS torrent jet visual spawned.");

                // Test 7: Granzyme Detonation
                var gran = new GranzymeDetonationSkill();
                ForceEquip(gran, 2);
                var granTarget = Spawn<TbEnemy>(new Vector2(100, 0));
                gran.Trigger();
                AssertThat(Collect<GranzymeDetonationSkill.ApoptosisMarker>(_arena!).Count).IsGreater(0);
                gran.DetonateCaspase(granTarget.GlobalPosition);
                AssertThat(Collect<GranzymeDetonationSkill.ApoptosisBurstVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 7: Granzyme detonation apoptosis marker + burst visuals spawned.");

                // Test 8: Nuclease Blades
                var nuc = new NucleaseBladesSkill();
                ForceEquip(nuc, 3);
                AssertThat(Collect<NucleaseBladesSkill.BladesCanvas>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 8: Nuclease blades orbiting canvas visual active.");

                // Test 9: Defensin Barbs
                var def = new DefensinBarbsSkill();
                ForceEquip(def, 4);
                def.Trigger();
                AssertThat(Collect<DefensinBarbsSkill.BarbProjectile>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 9: Defensin barbs projectile visual spawned.");

                _phase = 4;
                return false;

            case 4:
                // Test 10: Pro-Inflammatory Arc
                var arc = new ProInflammatoryArcSkill();
                ForceEquip(arc, 1);
                Spawn<TbEnemy>(new Vector2(100, 0));
                arc.Trigger();
                AssertThat(Collect<ProInflammatoryArcSkill.ArcLightningVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 10: Pro-inflammatory arc lightning visual spawned.");

                // Test 11: Exosome Singularity
                var exo = new ExosomeSingularitySkill();
                ForceEquip(exo, 2);
                exo.Trigger();
                AssertThat(Collect<ExosomeSingularitySkill.SingularityVortex>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 11: Exosome singularity vortex visual spawned.");

                // Test 12: Phagolysosome Vent
                var vent = new PhagolysosomeVentSkill();
                ForceEquip(vent, 3);
                vent.Trigger();
                AssertThat(Collect<PhagolysosomeVentSkill.AcidPuddle>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 12: Phagolysosome vent acid puddle visual spawned.");

                // Test 13: MHC Tracer Beam
                var mhc = new MhcTracerBeamSkill();
                ForceEquip(mhc, 4);
                AssertThat(Collect<MhcTracerBeamSkill.TracerVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 13: MHC tracer beam visual active.");

                _phase = 5;
                return false;

            case 5:
                // Test 14: Histamine Surge
                var hist = new HistamineSurgeSkill();
                ForceEquip(hist, 1);
                hist.Trigger();
                AssertThat(Collect<HistamineSurgeSkill.SurgeVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 14: Histamine surge degranulation wave visual spawned.");

                // Test 15: Nitric Oxide Halo
                var no = new NitricOxideHaloSkill();
                ForceEquip(no, 2);
                AssertThat(Collect<NitricOxideHaloSkill.HaloVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 15: Nitric oxide halo toxic gas cloud visual active.");

                // Test 16: Interferon Wave
                var ifn = new InterferonWaveSkill();
                ForceEquip(ifn, 3);
                ifn.Trigger();
                AssertThat(Collect<InterferonWaveSkill.WaveVisual>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 16: Interferon wave acoustic shockwave visual spawned.");

                // Test 17: Lysozyme Ricochet & Pseudopod Lunge
                var lyso = new LysozymeRicochetSkill();
                ForceEquip(lyso, 4);
                Spawn<TbEnemy>(new Vector2(100, 0));
                lyso.Trigger();
                AssertThat(Collect<LysozymeRicochetSkill.RicochetVesicle>(_arena!).Count).IsGreater(0);
                GD.Print("[PASS] Test 17a: Lysozyme ricochet enzyme capsule visual spawned.");

                var lunge = new PseudopodLungeSkill();
                ForceEquip(lunge, 4);
                Spawn<TbEnemy>(new Vector2(120, 0));
                lunge.Trigger();
                var lunges = Collect<PseudopodChainVisual>(_arena!);
                AssertThat(lunges.Exists(c => c.IsBluntFist)).IsTrue();
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

        var playerScene = AssetLoader.Load<PackedScene>("res://scenes/characters/macrophage.tscn");
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
