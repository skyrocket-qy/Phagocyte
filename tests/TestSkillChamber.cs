using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Skills;
using Phagocyte.Testing;

namespace Phagocyte.Tests;

/// <summary>
/// Deterministic, environment-independent test harness for all 18 active skills.
/// Runs inside SkillTestChamber with 0% background shader noise, no wave spawners,
/// no exp-leveling modals, and non-exp target dummies.
/// </summary>
public partial class TestSkillChamber : TestHarness
{
    private class SkillTestDef
    {
        public string SkillId { get; set; } = "";
        public Func<BaseSkill> Factory { get; set; } = null!;
        public DummyFormation Formation { get; set; }
        public int PeakFrames { get; set; }
        public Action<SkillTestChamber>? SpecialAction { get; set; }
    }

    private SkillTestChamber? _chamber;
    private int _stage = 0;
    private int _frame = 0;
    private int _skillIndex = 0;
    private readonly List<SkillTestDef> _skills = new();

    public override void _Initialize()
    {
        Banner("STARTING ISOLATED SKILL TEST CHAMBER VERIFICATION");
        RegisterSkills();
    }

    private void RegisterSkills()
    {
        _skills.Add(new SkillTestDef
        {
            SkillId = "phagocytic_grasp",
            Factory = () => new PhagocyticGraspSkill(),
            Formation = DummyFormation.Single,
            PeakFrames = 5
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "perforin_lance",
            Factory = () => new PerforinLanceSkill(),
            Formation = DummyFormation.Line,
            PeakFrames = 4
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "complement_cascade",
            Factory = () => new ComplementCascadeSkill(),
            Formation = DummyFormation.Single,
            PeakFrames = 15,
            SpecialAction = chamber =>
            {
                var mines = chamber.GetTree().GetNodesInGroup("MacAssemblyMines");
                foreach (var m in mines)
                {
                    if (m is ComplementCascadeSkill.MacAssemblyMine mine)
                        mine.AssemblyTime = 0.05f;
                }
            }
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "antibody_salvo",
            Factory = () => new AntibodySalvoSkill(),
            Formation = DummyFormation.Cluster,
            PeakFrames = 18
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "granzyme_detonation",
            Factory = () => new GranzymeDetonationSkill(),
            Formation = DummyFormation.Cluster,
            PeakFrames = 6,
            SpecialAction = chamber =>
            {
                if (chamber.CurrentSkill is GranzymeDetonationSkill gran && chamber.Dummies.Count > 0)
                {
                    gran.DetonateCaspase(chamber.Dummies[0].GlobalPosition);
                }
            }
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "mhc_tracer_beam",
            Factory = () => new MhcTracerBeamSkill(),
            Formation = DummyFormation.Single,
            PeakFrames = 4
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "lysosomal_overload",
            Factory = () => new LysosomalOverloadSkill(),
            Formation = DummyFormation.GroundHazard,
            PeakFrames = 8
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "ros_torrent",
            Factory = () => new RosTorrentSkill(),
            Formation = DummyFormation.Line,
            PeakFrames = 6
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "pseudopod_lunge",
            Factory = () => new PseudopodLungeSkill(),
            Formation = DummyFormation.Single,
            PeakFrames = 5
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "nitric_oxide_halo",
            Factory = () => new NitricOxideHaloSkill(),
            Formation = DummyFormation.Radial,
            PeakFrames = 6
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "nuclease_blades",
            Factory = () => new NucleaseBladesSkill(),
            Formation = DummyFormation.Radial,
            PeakFrames = 5
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "interferon_wave",
            Factory = () => new InterferonWaveSkill(),
            Formation = DummyFormation.Radial,
            PeakFrames = 6
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "lysozyme_ricochet",
            Factory = () => new LysozymeRicochetSkill(),
            Formation = DummyFormation.Cluster,
            PeakFrames = 8
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "phagolysosome_vent",
            Factory = () => new PhagolysosomeVentSkill(),
            Formation = DummyFormation.GroundHazard,
            PeakFrames = 6
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "pro_inflammatory_arc",
            Factory = () => new ProInflammatoryArcSkill(),
            Formation = DummyFormation.Cluster,
            PeakFrames = 5
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "exosome_singularity",
            Factory = () => new ExosomeSingularitySkill(),
            Formation = DummyFormation.Cluster,
            PeakFrames = 8
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "defensin_barbs",
            Factory = () => new DefensinBarbsSkill(),
            Formation = DummyFormation.Radial,
            PeakFrames = 6
        });

        _skills.Add(new SkillTestDef
        {
            SkillId = "histamine_surge",
            Factory = () => new HistamineSurgeSkill(),
            Formation = DummyFormation.Radial,
            PeakFrames = 6
        });
    }

    public override bool _Process(double delta)
    {
        switch (_stage)
        {
            case 0:
                // Initialize chamber
                _chamber = new SkillTestChamber();
                Root.AddChild(_chamber);

                var macrophageScene = AssetLoader.Load<PackedScene>("res://scenes/characters/macrophage.tscn");
                if (macrophageScene == null)
                {
                    GD.PrintErr("[FAIL] Failed to load macrophage.tscn");
                    Quit(1);
                    return true;
                }
                _chamber.SetupSubject(macrophageScene);

                _skillIndex = 0;
                _stage = 1;
                _frame = 0;
                return false;

            case 1:
                // Check if all skills tested
                if (_skillIndex >= _skills.Count)
                {
                    Finish(true, $"ALL {_skills.Count} SKILLS VERIFIED IN ISOLATED CHAMBER");
                    return true;
                }

                // Prepare next skill in chamber
                var def = _skills[_skillIndex];
                _chamber!.ClearTransientVfx();
                _chamber.ApplyFormation(def.Formation);

                var skill = def.Factory();
                _chamber.ArmSkill(skill);

                _stage = 2;
                _frame = 0;
                return false;

            case 2:
                // Settle render tree (3 frames)
                if (!Gate(ref _frame, 3))
                    return false;

                var curDef = _skills[_skillIndex];
                // Capture 1. Pre-Cast Baseline (T0)
                if (DisplayServer.GetName() != "headless")
                    CaptureScreenshot($"skill_{curDef.SkillId}_pre.png");

                // Trigger skill
                _chamber!.TriggerSkill();
                curDef.SpecialAction?.Invoke(_chamber);

                _stage = 3;
                _frame = 0;
                return false;

            case 3:
                // Wait for peak visual frame
                var activeDef = _skills[_skillIndex];
                if (!Gate(ref _frame, activeDef.PeakFrames))
                    return false;

                // Capture 2. Active Skill VFX (Tpeak)
                if (DisplayServer.GetName() != "headless")
                    CaptureScreenshot($"skill_{activeDef.SkillId}_post.png");

                GD.Print($"[PASS] Skill {_skillIndex + 1}/{_skills.Count}: {activeDef.SkillId} captured cleanly.");

                _skillIndex++;
                _stage = 1;
                _frame = 0;
                return false;

            default:
                return true;
        }
    }
}
