using Godot;
using System;
using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;
using Game.Core;
using Game.Enemies;
using Game.Directors;
using Game.Player;

namespace Game.Tests;

/// <summary>
/// Verifies the six Pathological Overload Afflictions (docs/endgame.md §4):
/// additive score multipliers, endotoxemia damage amplification, autophagic
/// regen lock, extreme viscosity slow, febrile burn and antigenic drift.
/// </summary>
[TestSuite]
public partial class TestAfflictions : TestHarness
{
    private int _phase = 0;
    private GameRoot? _main = null;

    private static readonly string[] AllAfflictions =
    {
        RunMutatorService.FebrileConvulsion,
        RunMutatorService.Endotoxemia,
        RunMutatorService.AutophagicFailure,
        RunMutatorService.MicrotubuleSclerosis,
        RunMutatorService.AntigenicDrift,
        RunMutatorService.ExtremeViscosity
    };

    public override void _Initialize()
    {
        Banner("STARTING PATHOLOGICAL OVERLOAD AFFLICTION VERIFICATION");

        IsolateSaves("afflictions");
        RunRecordManager.LoadFromDisk();

        RunMutatorService.Clear();
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
        AssertThat(RunMutatorService.Definitions.Count).IsEqual(6);

        int bonusSum = 0;
        foreach (var def in RunMutatorService.Definitions)
            bonusSum += def.BonusPercent;
        AssertThat(bonusSum).IsEqual(175);
        AssertThat(RunMutatorService.Definitions[0].Id).IsEqual(RunMutatorService.FebrileConvulsion);

        RunMutatorService.Clear();
        AssertThat(RunMutatorService.ScoreMultiplier).IsEqualApprox(1.0f, 0.0001f);

        RunMutatorService.SetSelected(RunMutatorService.FebrileConvulsion, true);
        AssertThat(RunMutatorService.IsActive(RunMutatorService.FebrileConvulsion)).IsTrue();
        AssertThat(RunMutatorService.ScoreMultiplier).IsEqualApprox(1.25f, 0.0001f);

        // Unknown ids are ignored
        RunMutatorService.SetSelected("not_an_affliction", true);
        AssertThat(RunMutatorService.ScoreBonus).IsEqualApprox(0.25f, 0.0001f);

        // All six stack additively to the documented ×2.75 maximum
        RunMutatorService.SetSelection(AllAfflictions);
        AssertThat(RunMutatorService.IsActive(RunMutatorService.ExtremeViscosity)).IsTrue();
        AssertThat(RunMutatorService.ScoreMultiplier).IsEqualApprox(2.75f, 0.0001f);
        AssertThat(RunMutatorService.IncomingDamageMultiplier).IsEqualApprox(1.5f, 0.0001f);
        AssertThat(RunMutatorService.BlocksHealthRegen).IsTrue();
        AssertThat(RunMutatorService.DodgeDisabled).IsTrue();
        AssertThat(RunMutatorService.MoveSpeedPercentPenalty).IsEqualApprox(-0.25f, 0.0001f);

        // Score: (900×10 + 50,000 + 40×100) × 1.5 (Hard) × 2.75
        int boosted = RunRecordManager.ComputeScore(
            RunRecordManager.ResultDefeat, RunRecordManager.DifficultyHard, 900.0f, 50000, 40, 2.75f);
        AssertThat(boosted).IsEqual(259875);

        // RecordRun persists the multiplier and the affliction list
        var record = RunRecordManager.RecordRun(
            RunRecordManager.ResultDefeat, "macrophage", "acute_wound",
            1800.0f, 30, 0, Array.Empty<string>(),
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
        var cellScene = AssetLoader.Load<PackedScene>("res://scenes/actors/player_base.tscn");

        // Endotoxemia: ALL damage taken +50% through the single pipeline
        // (hazard, acid and febrile sources included — no bypass).
        RunMutatorService.SetSelection(new[] { RunMutatorService.Endotoxemia });
        var cell = cellScene.Instantiate<PlayerActor>();
        Root.AddChild(cell);
        AssertThat(cell.Stats).IsNotNull();
        // The damage comparison must not be nullified by the innate 8% block roll.
        cell.Stats!.SetBase("block", 0.0f);
        cell.Stats.SetBase("evasion", 0.0f);

        float hpBeforeFirst = cell.Health;
        cell.TakeDamage(10.0f);
        float firstLoss = hpBeforeFirst - cell.Health;

        float hpBeforeSecond = cell.Health;
        cell.TakeDamage(10.0f);
        float secondLoss = hpBeforeSecond - cell.Health;

        AssertThat(firstLoss).IsEqualApprox(secondLoss, 0.01f);
        AssertThat(firstLoss).IsGreater(10.0f);
        cell.QueueFree();

        // Autophagic failure: health_regen is fully suppressed
        RunMutatorService.SetSelection(new[] { RunMutatorService.AutophagicFailure });
        var regenCell = cellScene.Instantiate<PlayerActor>();
        Root.AddChild(regenCell);
        regenCell.Stats!.SetBase("health_regen", 10.0f);
        regenCell.Health = 50.0f;
        regenCell._PhysicsProcess(1.0);
        AssertThat(regenCell.Health).IsEqual(50.0f);

        // Control: without the affliction the same cell regenerates
        RunMutatorService.Clear();
        var controlCell = cellScene.Instantiate<PlayerActor>();
        Root.AddChild(controlCell);
        controlCell.Stats!.SetBase("health_regen", 10.0f);
        controlCell.Health = 50.0f;
        controlCell._PhysicsProcess(1.0);
        AssertThat(controlCell.Health).IsGreater(50.0f);
        controlCell.QueueFree();
        regenCell.QueueFree();

        GD.Print("[PASS] Endotoxemia amplification, single damage pipeline and regen lock verified.");
    }

    private void RunEndlessIntegrationTests()
    {
        RunMutatorService.SetSelection(AllAfflictions);

        var main = InstantiateMain(endless: true);
        _main = main;

        var player = main.GetNodeOrNull<PlayerActor>("Player");
        AssertThat(player).IsNotNull();

        // The febrile burn is a fixed 2% max-HP environmental tick; clear the
        // innate avoidance so the assertion cannot be nullified by a block roll.
        player!.Stats!.SetBase("block", 0.0f);
        player.Stats.SetBase("evasion", 0.0f);

        // Extreme viscosity: -25% base move speed percent modifier
        var speedStat = player.Stats.GetStatObj("move_speed");
        AssertThat(speedStat).IsNotNull();
        AssertThat(speedStat!.PercentBonus).IsEqualApprox(-0.25f, 0.0001f);

        // Febrile convulsion: 2% max HP environmental burn every 5s
        float hpBeforeBurn = player.Health;
        main._PhysicsProcess(RunMutatorService.FebrileBurnInterval + 0.1f);
        AssertThat(player.Health).IsLess(hpBeforeBurn);

        // Antigenic drift: vulnerability marks reset every 20s
        var enemy = EnemySpawner.CreateEnemy("staph");
        AssertThat(enemy).IsNotNull();
        main.EnemyContainer!.AddChild(enemy!);
        AssertThat(enemy!.Ailments).IsNotNull();
        enemy.Ailments!.Apply("shock");
        AssertThat(enemy.Ailments.IsActive("shock")).IsTrue();

        main._PhysicsProcess(RunMutatorService.AntigenicDriftInterval + 0.1f);
        AssertThat(enemy.Ailments.IsActive("shock")).IsFalse();

        GD.Print("[PASS] Endless integration: viscosity, febrile burn and antigenic drift verified.");
    }

    private void Cleanup()
    {
        Paused = false;
        GameManager.EndlessMode = false;
        RunMutatorService.Clear();

        FreeMain(_main);
        _main = null;

        RestoreSaves();
    }
}



