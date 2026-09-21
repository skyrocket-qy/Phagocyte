using Godot;
using System;
using Phagocyte.Core;
using Phagocyte.UI;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Phagocyte.Tests;

public partial class TestMenuFlow : TestHarness
{
    private int _framesWaited = 0;
    private bool _testDone = false;

    public override void _Initialize()
    {
        GD.Print("--- BEGINNING MENU & SELECTION FLOW AUTOMATED TEST ---");
        IsolateSaves("menu_flow");

        var menuScene = GD.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
        if (menuScene == null)
        {
            GD.PrintErr("Failed to load main_menu.tscn");
            Quit(1);
            return;
        }
        var menu = menuScene.Instantiate();
        Root.AddChild(menu);
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        _framesWaited++;
        if (_framesWaited < 3)
            return false;

        _testDone = true;
        var menu = Root.GetNodeOrNull<MainMenu>("MainMenu");
        if (menu == null)
        {
            GD.PrintErr("MainMenu node not found");
            Quit(1);
            return true;
        }

        // 1. Test Title View initial state
        AssertThat(menu.TitleView != null && menu.TitleView.Visible).IsTrue();
        AssertThat(menu.ClassView != null && menu.ClassView.Visible).IsFalse();
        AssertThat(menu.PassiveView != null && menu.PassiveView.Visible).IsFalse();
        AssertThat(menu.MapView != null && menu.MapView.Visible).IsFalse();
        GD.Print("[PASS] TitleView initially visible; class, tree, and map views hidden.");

        // 2. Test Transition to Class Selection
        menu.OnStartPressed();
        AssertThat(menu.TitleView!.Visible).IsFalse();
        AssertThat(menu.ClassView!.Visible).IsTrue();
        GD.Print("[PASS] Transition to ClassView verified.");

        AchievementManager.ResetAll();
        menu.SetupClassButtons();

        // 3. Test Initial Lock States: Macrophage unlocked, others locked
        menu.SelectClass("macrophage");
        AssertThat(menu.ClassConfirmBtn!.Disabled).IsFalse();

        string[] lockedIds = { "ctl", "neutrophil", "b_cell", "dendritic" };
        foreach (var lockedId in lockedIds)
        {
            menu.SelectClass(lockedId);
            AssertThat(menu.ClassConfirmBtn.Disabled).IsTrue();
            AssertThat(menu.ClassBadgeLbl).IsNotNull();
            AssertThat(menu.ClassBadgeLbl!.Text.StartsWith("[ ")).IsTrue();
        }
        GD.Print("[PASS] Initial lock state enforced: Macrophage unlocked, other 4 cells locked.");

        // Unlock remaining cells via achievements and verify they become confirmable
        string[] achs = { "ach_engulf_20", "ach_devour_50", "ach_reach_level_5", "ach_survive_180s" };
        foreach (var ach in achs)
        {
            AchievementManager.Unlock(ach);
        }
        menu.SetupClassButtons();

        string[] allCells = { "macrophage", "ctl", "neutrophil", "b_cell", "dendritic" };
        foreach (var cellId in allCells)
        {
            menu.SelectClass(cellId);
            AssertThat(menu.ClassConfirmBtn.Disabled).IsFalse();
            // Detail dossier: badge + bio + baseline vitals + innate skill
            AssertThat(menu.ClassBadgeLbl!.Text.StartsWith("[ ")).IsTrue();
            AssertThat(menu.ClassBioLbl!.Text.Length).IsGreater(10);
            AssertThat(menu.ClassStatsLbl!.Text.Contains("◆")).IsFalse();
            AssertThat(menu.ClassStatsLbl!.Text.Contains("\n")).IsTrue();
            AssertThat(menu.ClassSkillLbl!.Text.Contains("\n")).IsTrue();
        }
        GD.Print("[PASS] All 5 immune defense cells selectable and confirmed unlocked via achievements.");

        // Re-select Macrophage and proceed through the passive tree
        menu.SelectClass("macrophage");
        menu.OnClassConfirmPressed();
        AssertThat(menu.ClassView.Visible).IsFalse();
        AssertThat(menu.PassiveView != null && menu.PassiveView.Visible).IsTrue();
        AssertThat(menu.MapView != null && menu.MapView.Visible).IsFalse();
        AssertThat(GameManager.SelectedClass).IsEqual("macrophage");
        AssertThat(menu.ActiveTreeClassKey).IsEqual("macrophage");
        GD.Print("[PASS] Transition to PassiveView with GameManager.selected_class = 'macrophage' verified.");

        AssertThat(menu.ProfileHBox).IsNotNull();
        AssertThat(menu.ProfileAddBtn).IsNotNull();
        AssertThat(menu.ProfileDeleteBtn).IsNotNull();
        AssertThat(menu.ProfileTabCount).IsEqual(1);
        AssertThat(menu.ProfileAddBtn!.Visible).IsTrue();
        AssertThat(menu.ProfileDeleteBtn!.Disabled).IsTrue();
        menu.OnProfileAddPressed();
        AssertThat(menu.ProfileTabCount).IsEqual(2);
        AssertThat(PassiveTreeManager.GetActiveProfile("macrophage")).IsEqual(1);
        menu.OnProfileAddPressed();
        AssertThat(menu.ProfileTabCount).IsEqual(3);
        AssertThat(menu.ProfileAddBtn.Visible).IsFalse();
        menu.OnProfileTabPressed(0);
        AssertThat(PassiveTreeManager.GetActiveProfile("macrophage")).IsEqual(0);
        menu.OnProfileDeletePressed();
        AssertThat(menu.ProfileTabCount).IsEqual(2);
        AssertThat(menu.ProfileAddBtn.Visible).IsTrue();
        menu.OnProfileDeletePressed();
        AssertThat(menu.ProfileTabCount).IsEqual(1);
        AssertThat(menu.ProfileDeleteBtn.Disabled).IsTrue();
        GD.Print("[PASS] Build profile tabs add, switch, and delete with correct button states.");

        menu.OnPassiveConfirmPressed();
        AssertThat(menu.PassiveView!.Visible).IsFalse();
        AssertThat(menu.MapView != null && menu.MapView.Visible).IsTrue();
        GD.Print("[PASS] Transition from PassiveView to MapView verified.");

        // 4. Test Map Selection & 5 Organ Battlefields
        AssertThat(GameManager.MapData.Count).IsEqual(5);
        AssertThat(menu.HoloScanner).IsNotNull();
        GD.Print("[PASS] GameManager.MapData has 5 maps and HoloScanner is initialized.");

        string[] allMaps = { "acute_wound", "alveolar_space", "hepatic_sinusoid", "gastric_lumen", "blood_brain_barrier" };
        foreach (var mapKey in allMaps)
        {
            menu.SelectMap(mapKey);
            AssertThat(menu.ActiveMapKey).IsEqual(mapKey);
            AssertThat(menu.HoloScanner!.ActiveMapKey).IsEqual(mapKey);
            var info = GameManager.GetMapInfo(mapKey);
            AssertThat(info["name"].AsString()).IsNotEmpty();
            AssertThat(info["organ"].AsString()).IsNotEmpty();
            AssertThat((int)info["difficulty"] >= 1 && (int)info["difficulty"] <= 5).IsTrue();
        }
        GD.Print("[PASS] All 5 organ battlefields selectable with synchronized HoloBodyScanner.");

        // Test scanner signal selection
        menu.HoloScanner!.EmitSignal(HoloBodyScanner.SignalName.OrganSelected, "hepatic_sinusoid");
        AssertThat(menu.ActiveMapKey).IsEqual("hepatic_sinusoid");
        GD.Print("[PASS] HoloBodyScanner interactive OrganSelected signal correctly switches map.");

        // 5. Test Main Scene loading with selected map
        menu.QueueFree();

        GameManager.SelectedMap = "hepatic_sinusoid";
        var mainScene = GD.Load<PackedScene>("res://scenes/main.tscn");
        AssertThat(mainScene).IsNotNull();
        var main = mainScene!.Instantiate<Main>();
        Root.AddChild(main);

        AssertThat(main.MapId).IsEqual("hepatic_sinusoid");
        GD.Print("[PASS] Main arena successfully configured with hepatic_sinusoid environment.");

        main.QueueFree();
        AchievementManager.ResetAll();
        RestoreSaves();

        GD.Print("--- ALL MENU & SELECTION FLOW TESTS PASSED! ---");
        Quit(0);
        return true;
    }
}
