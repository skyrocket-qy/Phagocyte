using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Game.Core;
using Game.UI;

namespace Game.Tests;

/// <summary>
/// Verifies the organ-stage lock UX: locked stage list entries, requirement readouts,
/// disabled Deploy button and the holographic scanner lock visuals data path.
/// </summary>
[TestSuite]
public partial class TestStageLockUi : TestHarness
{
    private int _frame = 0;
    private bool _done = false;

    public override void _Initialize()
    {
        Banner("STARTING ORGAN STAGE LOCK UI VERIFICATION");

        IsolateSaves("stage_lock_ui");
        AchievementManager.ResetAll();
    }

    public override bool _Process(double delta)
    {
        if (_done)
            return true;

        if (!Gate(ref _frame, 3))
            return false;

        _done = true;
        try
        {
            RunTests();
            Cleanup();
            GD.Print("==================================================================");
            GD.Print(">>> ALL ORGAN STAGE LOCK UI TESTS PASSED SUCCESSFULLY! <<<");
            GD.Print("==================================================================");
            Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestStageLockUi threw: ", ex);
            Cleanup();
            Quit(1);
        }
        return true;
    }

    private void RunTests()
    {
        var menuScene = AssetLoader.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
        AssertThat(menuScene).IsNotNull();
        var menu = menuScene!.Instantiate<MainMenu>();
        Root.AddChild(menu);
        menu.SetProcess(false);

        AssertThat(menu.DeployBtn).IsNotNull();
        AssertThat(menu.StageLockStatusLbl).IsNotNull();
        AssertThat(menu.HoloScanner).IsNotNull();

        // Baseline: only the tutorial wound stage is deployable
        menu.SelectStage("acute_wound");
        AssertThat(menu.IsActiveStageLocked).IsFalse();
        AssertThat(menu.DeployBtn!.Disabled).IsFalse();
        AssertThat(menu.StageLockStatusLbl!.Visible).IsFalse();

        menu.SelectStage("alveolar_space");
        AssertThat(menu.IsActiveStageLocked).IsTrue();
        AssertThat(menu.DeployBtn!.Disabled).IsTrue();
        AssertThat(menu.StageLockStatusLbl!.Visible).IsTrue();
        AssertThat(menu.StageLockStatusLbl.Text.Contains("🏆") || menu.StageLockStatusLbl.Text.Contains("🔒")).IsTrue();
        AssertThat(string.IsNullOrEmpty(menu.DeployBtn.TooltipText)).IsFalse();
        GD.Print($"[PASS] Locked stage disables Deploy and shows requirement: '{menu.StageLockStatusLbl.Text.Replace("\n", "  ")}'");

        // Locked list entry carries padlock + lock metadata
        var lockedBtn = menu.StageListContainer!.GetNodeOrNull<Button>("MapBtn_alveolar_space");
        AssertThat(lockedBtn).IsNotNull();
        AssertThat(lockedBtn!.HasMeta("map_locked")).IsTrue();
        AssertThat(lockedBtn.GetMeta("map_locked").AsBool()).IsTrue();
        AssertThat(lockedBtn.Text.Contains("🔒")).IsTrue();

        // Holographic scanner lock state
        AssertThat(StageSelectView.IsStageLocked("acute_wound")).IsFalse();
        AssertThat(StageSelectView.IsStageLocked("alveolar_space")).IsTrue();
        menu.HoloScanner!.SelectStage("alveolar_space");
        AssertThat(menu.HoloScanner.ActiveStageKey).IsEqual("alveolar_space");

        // Pressing Deploy on a locked stage must be refused (no scene switch)
        GameManager.SelectedStage = "acute_wound";
        menu.DeployBtn.EmitSignal(Button.SignalName.Pressed);
        AssertThat(GameManager.SelectedStage).IsEqual("acute_wound");
        GD.Print("[PASS] Locked stage deploy is refused; the requirement readout is refreshed instead.");

        // Earning the prerequisite achievement unlocks the next organ stage
        AchievementManager.RecordStageClear("acute_wound");
        AssertThat(GameManager.IsStageUnlocked("alveolar_space")).IsTrue();
        menu.SelectStage("alveolar_space");
        AssertThat(menu.IsActiveStageLocked).IsFalse();
        AssertThat(menu.DeployBtn!.Disabled).IsFalse();
        AssertThat(menu.StageLockStatusLbl!.Visible).IsFalse();
        AssertThat(StageSelectView.IsStageLocked("alveolar_space")).IsFalse();
        GD.Print("[PASS] Clearing the prerequisite unlocks the next organ stage in list, scanner and Deploy.");

        menu.QueueFree();
    }

    private static void Cleanup()
    {
        GameManager.SelectedStage = "acute_wound";
        AchievementManager.ResetAll();
        RestoreSaves();
    }
}
