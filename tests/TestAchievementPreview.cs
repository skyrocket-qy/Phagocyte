using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;
using Phagocyte.UI;

namespace Phagocyte.Tests;

/// <summary>
/// Visual preview harness for the achievement gallery (not a pass/fail art
/// check): opens the gallery with representative samples, saves viewport PNGs
/// via <see cref="TestHarness.CaptureScreenshot"/> for eyeball review, and
/// asserts the card-set logic so headless runs (where capture is skipped)
/// still verify behavior. Set PHAGOCYTE_CAPTURE_DIR for the PNG output dir.
/// </summary>
[TestSuite]
public partial class TestAchievementPreview : TestHarness
{
    private int _frame = 0;
    private int _stage = 0;
    private MainMenu? _menu;

    public override void _Initialize()
    {
        Banner("ACHIEVEMENT GALLERY PREVIEW");
        AchievementManager.ResetAll();
    }

    public override bool _Process(double delta)
    {
        try
        {
            switch (_stage)
            {
                case 0:
                    if (!Gate(ref _frame, 2))
                        return false;
                    OpenMenu();
                    _frame = 0;
                    _stage = 1;
                    return false;
                case 1:
                    // Title view is the menu's initial state: capture it before navigating.
                    if (!Gate(ref _frame, 6))
                        return false;
                    CaptureScreenshot("title_view.png");
                    _menu!.EndlessSetupModal!.OpenSetup();
                    _frame = 0;
                    _stage = 2;
                    return false;
                case 2:
                    // Endgame affliction modal lives on the menu: capture, then close.
                    if (!Gate(ref _frame, 6))
                        return false;
                    AssertThat(_menu!.EndlessSetupModal!.Visible).IsTrue();
                    AssertThat(_menu.EndlessSetupModal.ConfirmBtn).IsNotNull();
                    CaptureScreenshot("endgame_setup.png");
                    _menu.EndlessSetupModal.CancelBtn!.EmitSignal(Button.SignalName.Pressed);
                    _menu.AchievementsBtn!.EmitSignal(Button.SignalName.Pressed);
                    AssertThat(_menu.AchievementView!.Visible).IsTrue();
                    AchievementManager.Unlock("engulf_20");
                    AchievementManager.Unlock("first_digestion");
                    _frame = 0;
                    _stage = 3;
                    return false;
                case 3:
                    // Let layout + textures settle before capturing.
                    if (!Gate(ref _frame, 6))
                        return false;
                    AssertThat(_menu!.AchievementView!.CardCount).IsEqual(AchievementManager.Achievements.Count);
                    CaptureScreenshot("gallery_all.png");
                    _menu.AchievementView.SetFilter(AchievementGalleryView.FilterLocked);
                    _frame = 0;
                    _stage = 4;
                    return false;
                case 4:
                    if (!Gate(ref _frame, 4))
                        return false;
                    AssertThat(_menu!.AchievementView!.CardCount).IsEqual(AchievementManager.Achievements.Count - 2);
                    CaptureScreenshot("gallery_locked.png");
                    FreeMenu();
                    ShowSampleToast();
                    _frame = 0;
                    _stage = 5;
                    return false;
                case 5:
                    if (!Gate(ref _frame, 4))
                        return false;
                    CaptureScreenshot("toast_banner.png");
                    ShowSampleAchievementToast();
                    _frame = 0;
                    _stage = 6;
                    return false;
                case 6:
                    // Let the slide-down tween finish for a settled shot.
                    if (!Gate(ref _frame, 30))
                        return false;
                    CaptureScreenshot("achievement_toast.png");
                    OpenHudRun();
                    _frame = 0;
                    _stage = 7;
                    return false;
                case 7:
                    if (!Gate(ref _frame, 8))
                        return false;
                    CaptureScreenshot("hud_hp.png");
                    GD.Print(">>> ACHIEVEMENT GALLERY PREVIEW PASSED SUCCESSFULLY! <<<");
                    Quit(0);
                    return true;
                default:
                    return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestAchievementPreview threw: ", ex);
            FreeMenu();
            Quit(1);
            return true;
        }
    }

    private void OpenMenu()
    {
        var menuScene = AssetLoader.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
        AssertThat(menuScene).IsNotNull();
        _menu = menuScene!.Instantiate<MainMenu>();
        Root.AddChild(_menu);
        AssertThat(_menu.AchievementView!.Visible).IsFalse();
    }

    private void FreeMenu()
    {
        if (_menu != null && IsInstanceValid(_menu))
        {
            if (_menu.GetParent() != null)
                _menu.GetParent().RemoveChild(_menu);
            _menu.Free();
            _menu = null;
        }
    }

    /// <summary>Shows the toast banner standalone (no HUD needed) for capture.</summary>
    private void ShowSampleToast()
    {
        var banner = AssetLoader.Load<PackedScene>("res://scenes/ui/toast_banner.tscn").Instantiate<PanelContainer>();
        banner.Name = "PreviewToast";
        Root.AddChild(banner);
        var icon = banner.GetNodeOrNull<TextureRect>("HBox/Icon");
        if (icon != null)
            icon.Texture = AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
        var title = banner.GetNodeOrNull<Label>("HBox/VBox/Title");
        if (title != null)
            title.Text = "Preview toast title";
        var desc = banner.GetNodeOrNull<Label>("HBox/VBox/Desc");
        if (desc != null)
            desc.Text = "Preview toast description";
        banner.Visible = true;
        AssertThat(banner.Visible).IsTrue();
    }

    /// <summary>Shows the unlock toast standalone (no HUD needed) for capture.</summary>
    private void ShowSampleAchievementToast()
    {
        // Clear the previous banner stage so captures don't overlap.
        var old = Root.GetNodeOrNull("PreviewToast");
        if (old != null)
        {
            Root.RemoveChild(old);
            old.Free();
        }
        var dummyAch = new Godot.Collections.Dictionary
        {
            { "id", "toast_preview" },
            { "image_path", AssetPaths.SkillIcon("actin") },
            { "title_key", "ACH_ENGULF_20_TITLE" },
            { "desc_key", "ACH_ENGULF_20_DESC" },
            { "reward_cell", "ctl" }
        };
        AchievementToast.ShowToast(Root, dummyAch);
        AssertThat(Root.GetChildCount() >= 1).IsTrue();
    }

    /// <summary>Opens a frozen run, damages the player once, for the HP capture.</summary>
    private void OpenHudRun()
    {
        foreach (var child in Root.GetChildren())
        {
            if (child is PanelContainer || child is AchievementToast)
            {
                Root.RemoveChild(child);
                child.Free();
            }
        }
        var main = InstantiateMain();
        ClearArenaEntities(main);
        var player = main.GetNodeOrNull<Macrophage>("Macrophage");
        AssertThat(player).IsNotNull();
        player!.Stats!.SetBase("block", 0.0f);
        player.Stats.SetBase("evasion", 0.0f);
        player.Health = player.MaxHealth;
        var hud = main.GetNodeOrNull<Hud>("HUD");
        AssertThat(hud).IsNotNull();
        hud!.ConnectPlayer(player);
        player.TakeDamage(20.0f);
        AssertThat(hud.HpBar).IsNotNull();
    }

    private static void ClearArenaEntities(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is BaseEnemy || child is DormantToxinVesicle || child is BioHazardArea)
                child.Free();
            else
                ClearArenaEntities(child);
        }
    }
}
