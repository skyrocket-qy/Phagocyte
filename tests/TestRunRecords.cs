using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.UI;

namespace Phagocyte.Tests;

[TestSuite]
public partial class TestRunRecords : TestHarness
{
    private int _phase = 0;
    private int _phaseFrames = 0;
    private Main? _main = null;

    public override void _Initialize()
    {
        Banner("STARTING RUN RECORD & SETTLEMENT SYSTEM TEST");

        IsolateSaves("run_records");
        RunRecordManager.LoadFromDisk();
    }

    public override bool _Process(double delta)
    {
        try
        {
            switch (_phase)
            {
                case 0:
                    PhaseManagerCrud();
                    _phase++;
                    _phaseFrames = 0;
                    break;

                case 1:
                    PhaseHistoryModal();
                    _phase++;
                    _phaseFrames = 0;
                    break;

                case 2:
                    PhaseDeathAndRevival();
                    _phase++;
                    _phaseFrames = 0;
                    break;

                case 3:
                    PhaseStartMainScene();
                    _phase++;
                    _phaseFrames = 0;
                    break;

                case 4:
                    _phaseFrames++;
                    if (_phaseFrames < 3)
                        return false;

                    // 15:00 lockdown spawns the terminal boss; victory requires killing it.
                    AssertThat(_main!.BossLockdownActive).IsTrue();
                    AssertThat(_main.TerminalBoss).IsNotNull();
                    _main.TerminalBoss!.TakeDamage(9999999.0f);
                    _phase++;
                    _phaseFrames = 0;
                    break;

                case 5:
                    _phaseFrames++;
                    if (_phaseFrames < 2)
                        return false;
                    PhaseVictorySettlement();
                    _phase++;
                    _phaseFrames = 0;
                    break;

                case 6:
                    _phaseFrames++;
                    if (_phaseFrames < 3)
                        return false;
                    PhaseDefeatSettlement();
                    _phase++;
                    break;

                default:
                    Cleanup();
                    GD.Print("--- ALL RUN RECORD TESTS PASSED CLEANLY! ---");
                    Quit(0);
                    return true;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("RUN RECORD TEST FAILURE: " + e);
            Cleanup();
            Quit(1);
            return true;
        }

        return false;
    }

    private void PhaseManagerCrud()
    {
        RunRecordManager.ClearRecords();
        AssertThat(RunRecordManager.GetRunCount()).IsEqual(0);
        AssertThat(RunRecordManager.GetVictoryCount()).IsEqual(0);
        AssertThat(RunRecordManager.GetBestSurvivalTime()).IsEqual(0.0f);

        RunRecordManager.RecordRun(
            RunRecordManager.ResultVictory, "macrophage", "acute_wound",
            920.0f, 7, 42, 3, new[] { "perforin_lance", "ros_torrent" },
            bossNeutralized: true, kills: 2200);
        RunRecordManager.RecordRun(
            RunRecordManager.ResultDefeat, "ctl", "alveolar_space",
            123.5f, 4, 11, 1, Array.Empty<string>(), kills: 80);

        AssertThat(RunRecordManager.GetRunCount()).IsEqual(2);
        // Newest run is stored first
        AssertThat(RunRecordManager.Records[0]["result"].AsString()).IsEqual(RunRecordManager.ResultDefeat);
        // A cause-less defeat records the documented content/setup fallback cause.
        AssertThat(RunRecordManager.Records[0]["cause"].AsString()).IsEqual(RunRecordManager.CauseSystemFailure);
        AssertThat(RunRecordManager.Records[0]["class_id"].AsString()).IsEqual("ctl");
        AssertThat(RunRecordManager.Records[0]["kills"].AsInt32()).IsEqual(80);
        AssertThat(RunRecordManager.Records[0]["rank"].AsString()).IsEqual(RunRecordManager.RankD);
        AssertThat(RunRecordManager.Records[1]["active_skills"].AsGodotArray().Count).IsEqual(2);
        AssertThat(RunRecordManager.Records[1]["cause"].AsString()).IsEqual(RunRecordManager.CauseSpecificNeutralization);
        AssertThat(RunRecordManager.Records[1]["victory_criteria"].AsGodotDictionary()["met"].AsBool()).IsTrue();
        AssertThat(RunRecordManager.Records[1]["kills"].AsInt32()).IsEqual(2200);
        AssertThat(RunRecordManager.Records[1]["difficulty"].AsString()).IsEqual(RunRecordManager.DifficultyNormal);
        AssertThat(RunRecordManager.Records[1]["rank"].AsString()).IsEqual(RunRecordManager.RankA);
        AssertThat(RunRecordManager.Records[1]["kpm"].AsSingle()).IsEqualApprox(143.48f, 0.1f);
        // (920s × 10 + 0 kill score + Lv7 × 100) × 1.0 + 10,000 clear bonus
        AssertThat(RunRecordManager.Records[1]["score"].AsInt32()).IsEqual(19900);
        AssertThat(RunRecordManager.GetVictoryCount()).IsEqual(1);
        AssertThat(RunRecordManager.GetBestSurvivalTime()).IsEqualApprox(920.0f, 0.01f);
        AssertThat(RunRecordManager.GetFastestVictory()).IsEqualApprox(920.0f, 0.01f);

        // Victory criteria (docs/record.md): 15:00 survival AND boss neutralization
        AssertThat(RunRecordManager.IsVictoryCriteriaMet(900.0f, true)).IsTrue();
        AssertThat(RunRecordManager.IsVictoryCriteriaMet(899.0f, true)).IsFalse();
        AssertThat(RunRecordManager.IsVictoryCriteriaMet(1200.0f, false)).IsFalse();

        // Clinical grading (docs/record.md §4.3)
        AssertThat(RunRecordManager.ComputeKpm(3500, 900.0f)).IsEqualApprox(233.33f, 0.1f);
        AssertThat(RunRecordManager.ComputeRank(
            RunRecordManager.ResultVictory, RunRecordManager.DifficultyHard, 900.0f, 3500))
            .IsEqual(RunRecordManager.RankS);
        // Hard clear but KPM below the S gate falls back to A
        AssertThat(RunRecordManager.ComputeRank(
            RunRecordManager.ResultVictory, RunRecordManager.DifficultyHard, 900.0f, 3400))
            .IsEqual(RunRecordManager.RankA);
        // A flawless normal clear can never claim S (Hard-only grade)
        AssertThat(RunRecordManager.ComputeRank(
            RunRecordManager.ResultVictory, RunRecordManager.DifficultyNormal, 900.0f, 3600))
            .IsEqual(RunRecordManager.RankA);
        AssertThat(RunRecordManager.ComputeRank(
            RunRecordManager.ResultDefeat, RunRecordManager.DifficultyNormal, 601.0f, 800))
            .IsEqual(RunRecordManager.RankB);
        AssertThat(RunRecordManager.ComputeRank(
            RunRecordManager.ResultDefeat, RunRecordManager.DifficultyNormal, 241.0f, 300))
            .IsEqual(RunRecordManager.RankC);
        AssertThat(RunRecordManager.ComputeRank(
            RunRecordManager.ResultDefeat, RunRecordManager.DifficultyNormal, 239.0f, 500))
            .IsEqual(RunRecordManager.RankD);

        // Pathological score: (survival × 10 + kill score + level × 100) × difficulty + clear bonus
        AssertThat(RunRecordManager.ComputeScore(
            RunRecordManager.ResultVictory, RunRecordManager.DifficultyNormal, 900.0f, 50000, 40))
            .IsEqual(73000);
        AssertThat(RunRecordManager.ComputeScore(
            RunRecordManager.ResultVictory, RunRecordManager.DifficultyHard, 900.0f, 50000, 40))
            .IsEqual(104500);
        AssertThat(RunRecordManager.ComputeScore(
            RunRecordManager.ResultDefeat, RunRecordManager.DifficultyNormal, 300.0f, 1200, 10))
            .IsEqual(5200);

        AssertThat(RunRecordManager.FormatTime(0.0f)).IsEqual("00:00");
        AssertThat(RunRecordManager.FormatTime(65.4f)).IsEqual("01:05");
        AssertThat(RunRecordManager.FormatTime(600.0f)).IsEqual("10:00");
        AssertThat(RunRecordManager.FormatTimestamp(1758200000.0).Length).IsEqual(16);

        // Persistence round-trip: clearing in memory must not lose the disk copy
        RunRecordManager.LoadFromDisk();
        AssertThat(RunRecordManager.GetRunCount()).IsEqual(2);
        AssertThat(RunRecordManager.Records[1]["result"].AsString()).IsEqual(RunRecordManager.ResultVictory);

        // History is trimmed to MaxRecords (newest kept)
        for (int i = 0; i < RunRecordManager.MaxRecords + 5; i++)
        {
            RunRecordManager.RecordRun(
                RunRecordManager.ResultDefeat, "neutrophil", "gastric_lumen",
                i, 2, i, 0, Array.Empty<string>());
        }
        AssertThat(RunRecordManager.GetRunCount()).IsEqual(RunRecordManager.MaxRecords);
        AssertThat(RunRecordManager.Records[0]["survival_time"].AsSingle()).IsEqualApprox(
            RunRecordManager.MaxRecords + 4, 0.01f);

        // An unearned victory claim is downgraded to defeat by the manager guard
        var rejected = RunRecordManager.RecordRun(
            RunRecordManager.ResultVictory, "macrophage", "acute_wound",
            300.0f, 7, 42, 3, Array.Empty<string>());
        AssertThat(rejected["result"].AsString()).IsEqual(RunRecordManager.ResultDefeat);
        AssertThat(rejected["victory_criteria"].AsGodotDictionary()["met"].AsBool()).IsFalse();

        GD.Print("[PASS] Run record CRUD, statistics, formatting, persistence and trimming verified.");

        // Leave a small clean history for the UI phase:
        // 2 victories (macrophage 920s = 19,900 pts is the best, ctl 910s = 19,400 pts) + 1 defeat
        RunRecordManager.ClearRecords();
        RunRecordManager.RecordRun(
            RunRecordManager.ResultVictory, "macrophage", "acute_wound",
            920.0f, 7, 42, 3, new[] { "perforin_lance" }, bossNeutralized: true, kills: 2200);
        RunRecordManager.RecordRun(
            RunRecordManager.ResultDefeat, "b_cell", "hepatic_sinusoid",
            95.0f, 3, 8, 1, Array.Empty<string>());
        RunRecordManager.RecordRun(
            RunRecordManager.ResultVictory, "ctl", "alveolar_space",
            910.0f, 3, 15, 1, Array.Empty<string>(), bossNeutralized: true, kills: 300);
    }

    private void PhaseHistoryModal()
    {
        var menuScene = GD.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
        var menu = menuScene.Instantiate<MainMenu>();
        Root.AddChild(menu);

        var modal = menu.RecordsModal;
        AssertThat(modal).IsNotNull();
        AssertThat(modal!.Visible).IsFalse();

        modal.OpenHistory();
        AssertThat(modal.Visible).IsTrue();
        AssertThat(modal.SettlementMode).IsFalse();

        // Dual-tab classification: Victories / Defeats (docs/record.md §5)
        AssertThat(modal.HistoryTabs).IsNotNull();
        AssertThat(modal.HistoryTabs!.TabCount).IsEqual(2);
        AssertThat(modal.HistoryTabs.CurrentTab).IsEqual(0);

        // Victories tab: highest-score chart pinned first (19,900 > 19,400)
        AssertThat(modal.HistoryList).IsNotNull();
        AssertThat(modal.HistoryList!.GetChildCount()).IsEqual(2);
        AssertThat(modal.PinnedBest).IsNotNull();
        AssertThat(modal.PinnedBest!["class_id"].AsString()).IsEqual("macrophage");
        AssertThat(modal.PinnedBest!["score"].AsInt32()).IsEqual(19900);

        // One-click switch to the defeats tab re-filters the archive
        modal.SetHistoryTab(1);
        AssertThat(modal.HistoryList!.GetChildCount()).IsEqual(1);
        AssertThat(modal.PinnedBest!["result"].AsString()).IsEqual(RunRecordManager.ResultDefeat);

        // Tactical review: selecting a chart renders its full clinical metrics
        modal.SelectRecord(modal.PinnedBest!);
        AssertThat(modal.SelectedRecord).IsNotNull();
        AssertThat(modal.SummaryBox!.Visible).IsTrue();
        AssertThat(modal.SummaryBox!.GetChildCount()).IsGreater(5);

        // Switching tabs clears the review selection
        modal.SetHistoryTab(0);
        AssertThat(modal.SelectedRecord).IsNull();

        // Settlement mode owns the summary area; history selection is locked
        modal.OpenSettlement(RunRecordManager.Records[0]);
        AssertThat(modal.SettlementMode).IsTrue();
        modal.SelectRecord(RunRecordManager.Records[1]);
        AssertThat(modal.SelectedRecord).IsNull();
        AssertThat(modal.SummaryBox!.Visible).IsTrue();

        menu.QueueFree();
        GD.Print("[PASS] History tabs, pinned best chart and clinical review panel verified.");
    }

    private void PhaseDeathAndRevival()
    {
        var cellScene = GD.Load<PackedScene>("res://scenes/characters/macrophage.tscn");

        var doomed = cellScene.Instantiate<Macrophage>();
        Root.AddChild(doomed);
        // Damage math below must not be nullified by the innate block/evasion roll.
        doomed.Stats!.SetBase("block", 0.0f);
        doomed.Stats.SetBase("evasion", 0.0f);
        bool died = false;
        doomed.Died += () => died = true;
        doomed.TakeDamage(999999.0f);
        AssertThat(doomed.IsDead).IsTrue();
        AssertThat(died).IsTrue();
        AssertThat(doomed.Health).IsEqual(0.0f);
        doomed.QueueFree();

        var survivor = cellScene.Instantiate<Macrophage>();
        Root.AddChild(survivor);
        survivor.Stats!.SetBase("block", 0.0f);
        survivor.Stats.SetBase("evasion", 0.0f);
        survivor.Stats.AddModifier("armor", 40.0f, 0.0f); // Macrophage base 10 + 40 = 50; 50 / (50 + 50) = 0.5 DR
        float hpBefore = survivor.Health;
        survivor.TakeDamage(20.0f);
        float hpLost = hpBefore - survivor.Health;
        AssertThat(hpLost).IsEqualApprox(10.0f, 0.5f);
        survivor.QueueFree();

        GD.Print("[PASS] Lethal damage emits Died and armor damage reduction functions properly.");
    }

    private void PhaseStartMainScene()
    {
        _main = GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>();
        Root.AddChild(_main);
        AssertThat(_main.RunGoalSeconds).IsGreater(0.0f);
        AssertThat(_main.RunEnded).IsFalse();

        // Push survival time to the brink so the next physics tick enters the 15:00 boss lockdown.
        _main.EnvironmentTime = _main.RunGoalSeconds - 0.01f;
    }

    private void PhaseVictorySettlement()
    {
        AssertThat(_main).IsNotNull();

        // The physics tick observed the goal being reached and settled the run.
        AssertThat(_main!.RunEnded).IsTrue();
        AssertThat(_main.TerminalBossNeutralized).IsTrue();
        AssertThat(RunRecordManager.Records[0]["result"].AsString()).IsEqual(RunRecordManager.ResultVictory);
        AssertThat(RunRecordManager.Records[0]["cause"].AsString()).IsEqual(RunRecordManager.CauseSpecificNeutralization);
        AssertThat(RunRecordManager.Records[0]["class_id"].AsString()).IsEqual(GameManager.SelectedClass);

        var criteria = RunRecordManager.Records[0]["victory_criteria"].AsGodotDictionary();
        AssertThat(criteria["met"].AsBool()).IsTrue();
        AssertThat(criteria["survived_full_time"].AsBool()).IsTrue();
        AssertThat(criteria["boss_neutralized"].AsBool()).IsTrue();

        // Kill telemetry feeds the record and its clinical grade
        AssertThat(RunRecordManager.Records[0]["kills"].AsInt32()).IsGreater(0);
        AssertThat(RunRecordManager.Records[0]["kill_score"].AsInt32()).IsGreater(2999);
        AssertThat(RunRecordManager.Records[0]["kpm"].AsSingle()).IsGreater(0.0f);
        AssertThat(RunRecordManager.Records[0].ContainsKey("rank")).IsTrue();
        AssertThat(RunRecordManager.Records[0]["difficulty"].AsString()).IsEqual(RunRecordManager.DifficultyNormal);

        var modal = _main.GetNodeOrNull<RunRecordsModal>("UIOverlay/RunRecordsModal");
        AssertThat(modal).IsNotNull();
        AssertThat(modal!.Visible).IsTrue();
        AssertThat(modal.SettlementMode).IsTrue();

        // Guard: a second ending (e.g. death after victory) must not add another record
        int countAfterVictory = RunRecordManager.GetRunCount();
        _main.EndRun(false);
        AssertThat(RunRecordManager.GetRunCount()).IsEqual(countAfterVictory);

        Paused = false;
        GD.Print("[PASS] Terminal boss neutralization settles a victory, shows the modal and locks the result.");
    }

    private void PhaseDefeatSettlement()
    {
        var main2 = GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>();
        Root.AddChild(main2);

        var player = main2.GetNodeOrNull<BaseCell>("Macrophage");
        AssertThat(player).IsNotNull();

        // Lethal damage must not be nullified by the innate block/evasion roll.
        player!.Stats!.SetBase("block", 0.0f);
        player.Stats.SetBase("evasion", 0.0f);

        int before = RunRecordManager.GetRunCount();
        player.TakeDamage(999999.0f);

        AssertThat(player.IsDead).IsTrue();
        AssertThat(main2.RunEnded).IsTrue();
        AssertThat(RunRecordManager.GetRunCount()).IsEqual(before + 1);
        AssertThat(RunRecordManager.Records[0]["result"].AsString()).IsEqual(RunRecordManager.ResultDefeat);
        AssertThat(RunRecordManager.Records[0]["cause"].AsString()).IsEqual(RunRecordManager.CauseMembraneRupture);
        AssertThat(RunRecordManager.Records[0]["victory_criteria"].AsGodotDictionary()["met"].AsBool()).IsFalse();
        AssertThat(RunRecordManager.Records[0]["rank"].AsString()).IsEqual(RunRecordManager.RankD);
        AssertThat(RunRecordManager.Records[0]["kills"].AsInt32()).IsEqual(0);

        var modal = main2.GetNodeOrNull<RunRecordsModal>("UIOverlay/RunRecordsModal");
        AssertThat(modal).IsNotNull();
        AssertThat(modal!.Visible).IsTrue();
        AssertThat(modal.SettlementMode).IsTrue();

        Paused = false;
        main2.QueueFree();
        GD.Print("[PASS] Lethal damage ends the run as a defeat and records it.");
    }

    private void Cleanup()
    {
        Paused = false;
        FreeMain(_main);
        _main = null;
        RestoreSaves();
    }
}
