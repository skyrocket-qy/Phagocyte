using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using Phagocyte.Core;
using Phagocyte.UI;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the independent MainMenu achievement gallery: Steam-mirrored artwork
/// fields, category accents, the 3-state filter partition, and the dedicated
/// Title → Achievements → Title navigation (Codex Tab 4 is gone).
/// </summary>
[TestSuite]
public partial class TestAchievementGallery : TestHarness
{
    private int _frame = 0;
    private bool _done = false;

    public override void _Initialize()
    {
        Banner("STARTING ACHIEVEMENT GALLERY VERIFICATION");
        AchievementManager.ResetAll();
    }

    public override bool _Process(double delta)
    {
        if (_done)
            return true;

        if (!Gate(ref _frame, 2))
            return false;

        _done = true;
        MainMenu? menu = null;
        try
        {
            RunDataTests();
            menu = RunMenuTests();
            Cleanup(menu);
            GD.Print("==================================================================");
            GD.Print(">>> ALL ACHIEVEMENT GALLERY TESTS PASSED SUCCESSFULLY! <<<");
            GD.Print("==================================================================");
            Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestAchievementGallery threw: ", ex);
            Cleanup(menu);
            Quit(1);
        }
        return true;
    }

    private static void RunDataTests()
    {
        // Every achievement exposes a derived image path + API name
        foreach (string achId in AchievementManager.Achievements.Keys)
        {
            var info = AchievementManager.GetAchievementInfo(achId);
            AssertThat(info.ContainsKey("image_path")).IsTrue();
            AssertThat(info.ContainsKey("steam_api_name")).IsTrue();
            AssertThat(info["image_path"].AsString()).IsEqual($"res://assets/gen/achievement/{achId}.png");
            AssertThat(info["steam_api_name"].AsString()).IsEqual(achId.ToUpperInvariant());
        }
        GD.Print($"[PASS] All {AchievementManager.Achievements.Count} achievements carry image_path + steam_api_name.");

        // Category accents: hard = red, cell unlocks = green, map clears = blue, milestones = gold
        AssertThat(AchievementGalleryView.CategoryColor(AchievementManager.GetAchievementInfo("wound_hard_clear")))
            .IsEqual(new Color(1.0f, 0.45f, 0.4f));
        AssertThat(AchievementGalleryView.CategoryColor(AchievementManager.GetAchievementInfo("engulf_20")))
            .IsEqual(new Color(0.3f, 1.0f, 0.4f));
        AssertThat(AchievementGalleryView.CategoryColor(AchievementManager.GetAchievementInfo("wound_clear")))
            .IsEqual(new Color(0.4f, 0.8f, 1.0f));
        AssertThat(AchievementGalleryView.CategoryColor(AchievementManager.GetAchievementInfo("first_digestion")))
            .IsEqual(new Color(1.0f, 0.84f, 0.35f));
        GD.Print("[PASS] Category accent colors verified.");

        // Missing art falls back cleanly (no art shipped yet — every load is null, never throws)
        AssertThat(AssetLoader.TryLoad<Texture2D>("") == null).IsTrue();
        AssertThat(AssetLoader.TryLoad<Texture2D>("res://assets/gen/achievement/engulf_20.png") == null).IsTrue();
        GD.Print("[PASS] Missing-artwork fallback verified (emoji plates until PNGs land).");
    }

    private MainMenu RunMenuTests()
    {
        var menuScene = AssetLoader.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
        AssertThat(menuScene).IsNotNull();
        var menu = menuScene!.Instantiate<MainMenu>();
        Root.AddChild(menu);

        AssertThat(menu.AchievementsBtn).IsNotNull();
        AssertThat(menu.AchievementView).IsNotNull();
        AssertThat(menu.GlobalBackBtn).IsNotNull();
        AssertThat(menu.TitleView!.Visible).IsTrue();
        AssertThat(menu.AchievementView!.Visible).IsFalse();
        AssertThat(menu.GlobalBackBtn!.Visible).IsFalse();

        // Dedicated button opens the fifth view with a full card set
        menu.AchievementsBtn!.EmitSignal(Button.SignalName.Pressed);
        AssertThat(menu.AchievementView.Visible).IsTrue();
        AssertThat(menu.TitleView.Visible).IsFalse();
        AssertThat(menu.GlobalBackBtn.Visible).IsTrue();
        AssertThat(menu.AchievementView.CardCount).IsEqual(AchievementManager.Achievements.Count);

        // Detail selection renders title, progress and the Steam API audit row
        menu.AchievementView.SelectAchievement("engulf_20");
        AssertThat(menu.AchievementView.ActiveAchievementId).IsEqual("engulf_20");
        AssertThat(string.IsNullOrEmpty(menu.AchievementView.DetailTitle!.Text)).IsFalse();
        AssertThat(menu.AchievementView.DetailSteam!.Text.Contains("ENGULF_20")).IsTrue();
        GD.Print("[PASS] Gallery opens from its own button with full card set + Steam audit row.");

        // Unlocking mid-session refreshes the gallery without reopening
        AchievementManager.Unlock("engulf_20");
        AssertThat(menu.AchievementView.CardCount).IsEqual(AchievementManager.Achievements.Count);
        menu.AchievementView.SetFilter(AchievementGalleryView.FilterUnlocked);
        AssertThat(menu.AchievementView.CardCount >= 1).IsTrue();
        menu.AchievementView.SetFilter(AchievementGalleryView.FilterLocked);
        AssertThat(menu.AchievementView.CardCount).IsEqual(AchievementManager.Achievements.Count - 1);
        menu.AchievementView.SetFilter(AchievementGalleryView.FilterAll);
        GD.Print("[PASS] 3-state filter partitions the set and live-refreshes on unlock.");

        // Shared top-left back returns to the title (gallery stays out of the Start flow)
        menu.GlobalBackBtn!.EmitSignal(Button.SignalName.Pressed);
        AssertThat(menu.TitleView.Visible).IsTrue();
        AssertThat(menu.AchievementView.Visible).IsFalse();
        AssertThat(menu.GlobalBackBtn.Visible).IsFalse();
        GD.Print("[PASS] Shared top-left back returns to TitleView and hides itself.");

        // Per-view back targets: Map -> Passive, Passive -> Class, Class -> Title
        menu.OnStartPressed();
        menu.OnClassConfirmPressed();
        menu.OnPassiveConfirmPressed();
        AssertThat(menu.MapView!.Visible).IsTrue();
        AssertThat(menu.GlobalBackBtn.Visible).IsTrue();
        menu.GlobalBackBtn.EmitSignal(Button.SignalName.Pressed);
        AssertThat(menu.PassiveView!.Visible).IsTrue();
        menu.GlobalBackBtn.EmitSignal(Button.SignalName.Pressed);
        AssertThat(menu.ClassView!.Visible).IsTrue();
        menu.GlobalBackBtn.EmitSignal(Button.SignalName.Pressed);
        AssertThat(menu.TitleView.Visible).IsTrue();
        AssertThat(menu.GlobalBackBtn.Visible).IsFalse();
        GD.Print("[PASS] Shared back walks Map -> Passive -> Class -> Title.");

        return menu;
    }

    private void Cleanup(MainMenu? menu)
    {
        if (menu != null && IsInstanceValid(menu))
            menu.QueueFree();
        AchievementManager.ResetAll();
        RestoreSaves();
    }
}
