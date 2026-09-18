using Godot;
using System;
using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Endgame;
using Phagocyte.Player;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the six Pathological Overload Afflictions (docs/endgame.md §4):
/// additive score multipliers, endotoxemia damage amplification, autophagic
/// regen lock, extreme viscosity slow, febrile burn and antigenic drift.
/// </summary>
[TestSuite]
public partial class TestAfflictions : SceneTree
{
    private const string TestRecordsPath = "user://test_afflictions_records.json";

    private int _phase = 0;
    private Main? _main = null;

    private static readonly string[] AllAfflictions =
    {
        AfflictionManager.FebrileConvulsion,
        AfflictionManager.Endotoxemia,
        AfflictionManager.AutophagicFailure,
        AfflictionManager.MicrotubuleSclerosis,
        AfflictionManager.AntigenicDrift,
        AfflictionManager.ExtremeViscosity
    };

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING PATHOLOGICAL OVERLOAD AFFLICTION VERIFICATION <<<");
        GD.Print("==================================================================");

        RunRecordManager.SavePath = TestRecordsPath;
        if (FileAccess.FileExists(TestRecordsPath))
            DirAccess.RemoveAbsolute(TestRecordsPath);
        RunRecordManager.LoadFromDisk();

        AfflictionManager.Clear();
        GameManager.EndlessMode = false;
    }

    public override bool _Process(double delta)
    {
        try
        {
            switch (_phase)
            {
            case 0:
                _phase++;
                return false; // let the scene tree settle
            case 1:
                RunCatalogAndScoringTests();
                _phase++;
                return false;
            case 2:
                RunPlayerEffectTests();
                _phase++;
                return false;
            case 3:
                RunEndlessIntegrationTests();
                _phase++;
                return false;
            default:
                Cleanup();
                GD.Print("==================================================================");
                GD.Print(">>> ALL AFFLICTION TESTS PASSED SUCCESSFULLY! <<<");
                GD.Print("==================================================================");
                Quit(0);
                return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestAfflictions threw: ", ex);
            Cleanup();
            Quit(1);
            return true;
        }
    }

    private void RunCatalogAndScoringTests()
    {
        AssertThat(AfflictionManager.Definitions.Count).IsEqual(6);

        int bonusSum = 0;
        foreach (var def in AfflictionManager.Definitions)
            bonusSum += def.BonusPercent;
        AssertThat(bonusSum).IsEqual(175);
        AssertThat(AfflictionManager.Definitions[0].Id).IsEqual(AfflictionManager.FebrileConvulsion);

        AfflictionManager.Clear();
        AssertThat(AfflictionManager.ScoreMultiplier).IsEqualApprox(1.0f, 0.0001f);

        AfflictionManager.SetSelected(AfflictionManager.FebrileConvulsion, true);
        AssertThat(AfflictionManager.IsActive(AfflictionManager.FebrileConvulsion)).IsTrue();
        AssertThat(AfflictionManager.ScoreMultiplier).IsEqualApprox(1.25f, 0.0001f);

        // Unknown ids are ignored
        AfflictionManager.SetSelected("not_an_affliction", true);
        AssertThat(AfflictionManager.ScoreBonus).IsEqualApprox(0.25f, 0.0001f);

        // All six stack additively to the documented ×2.75 maximum
        AfflictionManager.SetSelection(AllAfflictions);
        AssertThat(AfflictionManager.IsActive(AfflictionManager.ExtremeViscosity)).IsTrue();
        AssertThat(AfflictionManager.ScoreMultiplier).IsEqualApprox(2.75f, 0.0001f);
        AssertThat(AfflictionManager.IncomingDamageMultiplier).IsEqualApprox(1.5f, 0.0001f);
        AssertThat(AfflictionManager.BlocksHealthRegen).IsTrue();
        AssertThat(AfflictionManager.SqueezeModeDisabled).IsTrue();
        AssertThat(AfflictionManager.MoveSpeedPercentPenalty).IsEqualApprox(-0.25f, 0.0001f);

        // Score: (900×10 + 50,000 + 40×100) × 1.5 (Hard) × 2.75
        int boosted = RunRecordManager.ComputeScore(
            RunRecordManager.ResultDefeat, RunRecordManager.DifficultyHard, 900.0f, 50000, 40, 2.75f);
        AssertThat(boosted).IsEqual(259875);

        // RecordRun persists the multiplier and the affliction list
        var record = RunRecordManager.RecordRun(
            RunRecordManager.ResultDefeat, "macrophage", "acute_wound",
            1800.0f, 30, 10, 0, Array.Empty<string>(),
            difficulty: RunRecordManager.DifficultyHard,
            endless: true,
            afflictionMultiplier: 2.75f,
            afflictions: AllAfflictions);
        AssertThat(record["affliction_multiplier"].AsSingle()).IsEqualApprox(2.75f, 0.0001f);
        AssertThat(record["afflictions"].AsGodotArray().Count).IsEqual(6);
        AssertThat(record["score"].AsInt32()).IsGreater(0);

        GD.Print("[PASS] Affliction catalog, additive multipliers (+175% -> ×2.75) and scoring verified.");
    }

    private void RunPlayerEffectTests()
    {
        var cellScene = GD.Load<PackedScene>("res://scenes/characters/macrophage.tscn");

        // Endotoxemia: pathogen damage +50%, environmental damage unaffected
        AfflictionManager.SetSelection(new[] { AfflictionManager.Endotoxemia });
        var cell = cellScene.Instantiate<Macrophage>();
        Root.AddChild(cell);
        AssertThat(cell.Stats).IsNotNull();

        float hpBeforePathogen = cell.Health;
        cell.TakeDamage(10.0f);
        float pathogenLoss = hpBeforePathogen - cell.Health;

        float hpBeforeEnv = cell.Health;
        cell.TakeEnvironmentalDamage(10.0f);
        float environmentalLoss = hpBeforeEnv - cell.Health;

        AssertThat(pathogenLoss).IsEqualApprox(environmentalLoss * 1.5f, 0.01f);
        cell.QueueFree();

        // Autophagic failure: health_regen is fully suppressed
        AfflictionManager.SetSelection(new[] { AfflictionManager.AutophagicFailure });
        var regenCell = cellScene.Instantiate<Macrophage>();
        Root.AddChild(regenCell);
        regenCell.Stats!.SetBase("health_regen", 10.0f);
        regenCell.Health = 50.0f;
        regenCell._PhysicsProcess(1.0);
        AssertThat(regenCell.Health).IsEqual(50.0f);

        // Control: without the affliction the same cell regenerates
        AfflictionManager.Clear();
        var controlCell = cellScene.Instantiate<Macrophage>();
        Root.AddChild(controlCell);
        controlCell.Stats!.SetBase("health_regen", 10.0f);
        controlCell.Health = 50.0f;
        controlCell._PhysicsProcess(1.0);
        AssertThat(controlCell.Health).IsGreater(50.0f);
        controlCell.QueueFree();
        regenCell.QueueFree();

        GD.Print("[PASS] Endotoxemia amplification, environmental bypass and regen lock verified.");
    }

    private void RunEndlessIntegrationTests()
    {
        GameManager.SelectedClass = "macrophage";
        GameManager.SelectedMap = "acute_wound";
        GameManager.EndlessMode = true;
        AfflictionManager.SetSelection(AllAfflictions);

        var main = GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>();
        _main = main;
        Root.AddChild(main);
        main.SetPhysicsProcess(false);

        var player = main.GetNodeOrNull<BaseCell>("Macrophage");
        AssertThat(player).IsNotNull();

        // Extreme viscosity: -25% base move speed percent modifier
        var speedStat = player!.Stats!.GetStatObj("move_speed");
        AssertThat(speedStat).IsNotNull();
        AssertThat(speedStat!.PercentBonus).IsEqualApprox(-0.25f, 0.0001f);

        // Febrile convulsion: 2% max HP environmental burn every 5s
        float hpBeforeBurn = player.Health;
        main._PhysicsProcess(AfflictionManager.FebrileBurnInterval + 0.1f);
        AssertThat(player.Health).IsLess(hpBeforeBurn);

        // Antigenic drift: vulnerability marks reset every 20s
        var enemy = PathogenSpawner.CreatePathogen("staph");
        AssertThat(enemy).IsNotNull();
        main.EnemyContainer!.AddChild(enemy!);
        AssertThat(enemy!.Ailments).IsNotNull();
        enemy.Ailments!.ApplyOpsonization();
        AssertThat(enemy.Ailments.IsOpsonized).IsTrue();

        main._PhysicsProcess(AfflictionManager.AntigenicDriftInterval + 0.1f);
        AssertThat(enemy.Ailments.IsOpsonized).IsFalse();

        GD.Print("[PASS] Endless integration: viscosity, febrile burn and antigenic drift verified.");
    }

    private void Cleanup()
    {
        Paused = false;
        GameManager.EndlessMode = false;
        AfflictionManager.Clear();

        if (_main != null && IsInstanceValid(_main))
        {
            if (_main.GetParent() != null)
                _main.GetParent().RemoveChild(_main);
            _main.Free();
            _main = null;
        }

        if (FileAccess.FileExists(TestRecordsPath))
            DirAccess.RemoveAbsolute(TestRecordsPath);
        RunRecordManager.SavePath = "";
    }
}
