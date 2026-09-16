using Godot;
using System;
using Phagocyte.Core;
using Phagocyte.UI;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Phagocyte.Tests;

public partial class TestMenuFlow : SceneTree
{
    private int _framesWaited = 0;
    private bool _testDone = false;

    public override void _Initialize()
    {
        GD.Print("--- BEGINNING MENU & SELECTION FLOW AUTOMATED TEST ---");

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
        AssertThat(menu.MapView != null && menu.MapView.Visible).IsFalse();
        GD.Print("[PASS] TitleView initially visible, class and map views hidden.");

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
        }
        GD.Print("[PASS] Initial lock state enforced: Macrophage unlocked, other 4 cells locked.");

        // Unlock remaining cells via achievements and verify they become confirmable
        string[] achs = { "ach_engulf_20", "ach_trigger_burst", "ach_reach_level_5", "ach_survive_180s" };
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
        }
        GD.Print("[PASS] All 5 immune defense cells selectable and confirmed unlocked via achievements.");

        // Re-select Macrophage and proceed
        menu.SelectClass("macrophage");
        menu.OnClassConfirmPressed();
        AssertThat(menu.ClassView.Visible).IsFalse();
        AssertThat(menu.MapView != null && menu.MapView.Visible).IsTrue();
        AssertThat(GameManager.SelectedClass).IsEqual("macrophage");
        GD.Print("[PASS] Transition to MapView with GameManager.selected_class = 'macrophage' verified.");

        // 4. Test Map Selection
        menu.SelectMap("alveolar_space");
        AssertThat(menu.ActiveMapKey).IsEqual("alveolar_space");
        GD.Print("[PASS] Map selection (Alveolar Space) verified.");

        // 5. Test Main Scene loading with selected map
        menu.QueueFree();

        GameManager.SelectedMap = "alveolar_space";
        var mainScene = GD.Load<PackedScene>("res://scenes/main.tscn");
        AssertThat(mainScene).IsNotNull();
        var main = mainScene!.Instantiate<Main>();
        Root.AddChild(main);

        AssertThat(main.MapId).IsEqual("alveolar_space");
        GD.Print("[PASS] Main arena successfully configured with alveolar_space environment.");

        main.QueueFree();
        AchievementManager.ResetAll();

        GD.Print("--- ALL MENU & SELECTION FLOW TESTS PASSED! ---");
        Quit(0);
        return true;
    }
}
