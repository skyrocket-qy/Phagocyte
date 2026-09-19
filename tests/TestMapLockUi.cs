using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Core;
using Phagocyte.UI;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the organ-map lock UX: locked map list entries, requirement readouts,
/// disabled Deploy button and the holographic scanner lock visuals data path.
/// </summary>
[TestSuite]
public partial class TestMapLockUi : TestHarness
{
    private int _frame = 0;
    private bool _done = false;

    public override void _Initialize()
    {
        Banner("STARTING ORGAN MAP LOCK UI VERIFICATION");

        IsolateSaves("map_lock_ui");
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
            GD.Print(">>> ALL ORGAN MAP LOCK UI TESTS PASSED SUCCESSFULLY! <<<");
            GD.Print("==================================================================");
            Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestMapLockUi threw: ", ex);
            Cleanup();
            Quit(1);
        }
        return true;
    }

    private void RunTests()
    {
        var menuScene = GD.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
        AssertThat(menuScene).IsNotNull();
        var menu = menuScene!.Instantiate<MainMenu>();
        Root.AddChild(menu);
        menu.SetProcess(false);

        AssertThat(menu.DeployBtn).IsNotNull();
        AssertThat(menu.MapLockStatusLbl).IsNotNull();
        AssertThat(menu.HoloScanner).IsNotNull();

        // Baseline: only the tutorial wound map is deployable
        menu.SelectMap("acute_wound");
        AssertThat(menu.IsActiveMapLocked).IsFalse();
        AssertThat(menu.DeployBtn!.Disabled).IsFalse();
        AssertThat(menu.MapLockStatusLbl!.Visible).IsFalse();

        menu.SelectMap("alveolar_space");
        AssertThat(menu.IsActiveMapLocked).IsTrue();
        AssertThat(menu.DeployBtn!.Disabled).IsTrue();
        AssertThat(menu.MapLockStatusLbl!.Visible).IsTrue();
        AssertThat(menu.MapLockStatusLbl.Text.Contains("🏆") || menu.MapLockStatusLbl.Text.Contains("🔒")).IsTrue();
        AssertThat(string.IsNullOrEmpty(menu.DeployBtn.TooltipText)).IsFalse();
        GD.Print($"[PASS] Locked map disables Deploy and shows requirement: '{menu.MapLockStatusLbl.Text.Replace("\n", "  ")}'");

        // Locked list entry carries padlock + lock metadata
        var lockedBtn = menu.MapListContainer!.GetNodeOrNull<Button>("MapBtn_alveolar_space");
        AssertThat(lockedBtn).IsNotNull();
        AssertThat(lockedBtn!.HasMeta("map_locked")).IsTrue();
        AssertThat(lockedBtn.GetMeta("map_locked").AsBool()).IsTrue();
        AssertThat(lockedBtn.Text.Contains("🔒")).IsTrue();

        // Holographic scanner lock state
        AssertThat(HoloBodyScanner.IsOrganLocked("acute_wound")).IsFalse();
        AssertThat(HoloBodyScanner.IsOrganLocked("alveolar_space")).IsTrue();
        menu.HoloScanner!.SelectOrgan("alveolar_space");
        AssertThat(menu.HoloScanner.ActiveMapKey).IsEqual("alveolar_space");

        // Pressing Deploy on a locked map must be refused (no scene switch)
        GameManager.SelectedMap = "acute_wound";
        menu.DeployBtn.EmitSignal(Button.SignalName.Pressed);
        AssertThat(GameManager.SelectedMap).IsEqual("acute_wound");
        GD.Print("[PASS] Locked map deploy is refused; the requirement readout is refreshed instead.");

        // Earning the prerequisite achievement unlocks the next organ map
        AchievementManager.RecordMapClear("acute_wound");
        AssertThat(GameManager.IsMapUnlocked("alveolar_space")).IsTrue();
        menu.SelectMap("alveolar_space");
        AssertThat(menu.IsActiveMapLocked).IsFalse();
        AssertThat(menu.DeployBtn!.Disabled).IsFalse();
        AssertThat(menu.MapLockStatusLbl!.Visible).IsFalse();
        AssertThat(HoloBodyScanner.IsOrganLocked("alveolar_space")).IsFalse();
        GD.Print("[PASS] Clearing the prerequisite unlocks the next organ map in list, scanner and Deploy.");

        menu.QueueFree();
    }

    private static void Cleanup()
    {
        GameManager.SelectedMap = "acute_wound";
        AchievementManager.ResetAll();
        RestoreSaves();
    }
}
