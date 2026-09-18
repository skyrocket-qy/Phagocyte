using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.UI;

namespace Phagocyte.Tests;

[TestSuite]
public partial class TestRunRecords : SceneTree
{
    private const string TestSavePath = "user://test_run_records.json";

    private int _phase = 0;
    private int _phaseFrames = 0;
    private Main? _main = null;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING RUN RECORD & SETTLEMENT SYSTEM TEST <<<");
        GD.Print("==================================================================");

        RunRecordManager.SavePath = TestSavePath;
        if (FileAccess.FileExists(TestSavePath))
            DirAccess.RemoveAbsolute(TestSavePath);
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
                    PhaseVictorySettlement();
                    _phase++;
                    _phaseFrames = 0;
                    break;

                case 5:
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
            300.0f, 7, 42, 3, new[] { "perforin_lance", "ros_torrent" });
        RunRecordManager.RecordRun(
            RunRecordManager.ResultDefeat, "ctl", "alveolar_space",
            123.5f, 4, 11, 1, Array.Empty<string>());

        AssertThat(RunRecordManager.GetRunCount()).IsEqual(2);
        // Newest run is stored first
        AssertThat(RunRecordManager.Records[0]["result"].AsString()).IsEqual(RunRecordManager.ResultDefeat);
        AssertThat(RunRecordManager.Records[0]["class_id"].AsString()).IsEqual("ctl");
        AssertThat(RunRecordManager.Records[1]["active_skills"].AsGodotArray().Count).IsEqual(2);
        AssertThat(RunRecordManager.GetVictoryCount()).IsEqual(1);
        AssertThat(RunRecordManager.GetBestSurvivalTime()).IsEqualApprox(300.0f, 0.01f);
        AssertThat(RunRecordManager.GetFastestVictory()).IsEqualApprox(300.0f, 0.01f);

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

        GD.Print("[PASS] Run record CRUD, statistics, formatting, persistence and trimming verified.");

        // Leave a small clean history for the UI phase
        RunRecordManager.ClearRecords();
        RunRecordManager.RecordRun(
            RunRecordManager.ResultVictory, "macrophage", "acute_wound",
            300.0f, 7, 42, 3, new[] { "perforin_lance" });
        RunRecordManager.RecordRun(
            RunRecordManager.ResultDefeat, "b_cell", "hepatic_sinusoid",
            95.0f, 3, 8, 1, Array.Empty<string>());
    }

    private void PhaseHistoryModal()
    {
        var menuScene = GD.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
        var menu = menuScene.Instantiate<MainMenu>();
        Root.AddChild(menu);

        AssertThat(menu.RecordsModal).IsNotNull();
        AssertThat(menu.RecordsModal!.Visible).IsFalse();

        menu.RecordsModal.OpenHistory();
        AssertThat(menu.RecordsModal.Visible).IsTrue();
        AssertThat(menu.RecordsModal.SettlementMode).IsFalse();
        AssertThat(menu.RecordsModal.HistoryList).IsNotNull();
        AssertThat(menu.RecordsModal.HistoryList!.GetChildCount()).IsEqual(RunRecordManager.GetRunCount());

        menu.QueueFree();
        GD.Print("[PASS] Menu history modal renders every stored run.");
    }

    private void PhaseDeathAndRevival()
    {
        var cellScene = GD.Load<PackedScene>("res://scenes/characters/macrophage.tscn");

        var doomed = cellScene.Instantiate<Macrophage>();
        Root.AddChild(doomed);
        bool died = false;
        doomed.Died += () => died = true;
        doomed.TakeDamage(999999.0f);
        AssertThat(doomed.IsDead).IsTrue();
        AssertThat(died).IsTrue();
        AssertThat(doomed.Health).IsEqual(0.0f);
        doomed.QueueFree();

        var survivor = cellScene.Instantiate<Macrophage>();
        Root.AddChild(survivor);
        survivor.Stats!.AddModifier("armor", 50.0f, 0.0f); // 50% DR: 50 / (50 + 50) = 0.5
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

        // Push survival time to the brink so the next physics tick clears the run.
        _main.EnvironmentTime = _main.RunGoalSeconds - 0.01f;
    }

    private void PhaseVictorySettlement()
    {
        AssertThat(_main).IsNotNull();

        // The physics tick observed the goal being reached and settled the run.
        AssertThat(_main!.RunEnded).IsTrue();
        AssertThat(RunRecordManager.Records[0]["result"].AsString()).IsEqual(RunRecordManager.ResultVictory);
        AssertThat(RunRecordManager.Records[0]["class_id"].AsString()).IsEqual(GameManager.SelectedClass);

        var modal = _main.GetNodeOrNull<RunRecordsModal>("UIOverlay/RunRecordsModal");
        AssertThat(modal).IsNotNull();
        AssertThat(modal!.Visible).IsTrue();
        AssertThat(modal.SettlementMode).IsTrue();

        // Guard: a second ending (e.g. death after victory) must not add another record
        int countAfterVictory = RunRecordManager.GetRunCount();
        _main.EndRun(false);
        AssertThat(RunRecordManager.GetRunCount()).IsEqual(countAfterVictory);

        Paused = false;
        GD.Print("[PASS] Survival goal settlement records a victory, shows the modal and locks the result.");
    }

    private void PhaseDefeatSettlement()
    {
        var main2 = GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>();
        Root.AddChild(main2);

        var player = main2.GetNodeOrNull<BaseCell>("Macrophage");
        AssertThat(player).IsNotNull();

        int before = RunRecordManager.GetRunCount();
        player!.TakeDamage(999999.0f);

        AssertThat(player.IsDead).IsTrue();
        AssertThat(main2.RunEnded).IsTrue();
        AssertThat(RunRecordManager.GetRunCount()).IsEqual(before + 1);
        AssertThat(RunRecordManager.Records[0]["result"].AsString()).IsEqual(RunRecordManager.ResultDefeat);

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
        if (_main != null && IsInstanceValid(_main))
        {
            _main.QueueFree();
            _main = null;
        }

        if (FileAccess.FileExists(TestSavePath))
            DirAccess.RemoveAbsolute(TestSavePath);

        RunRecordManager.SavePath = "";
    }
}
