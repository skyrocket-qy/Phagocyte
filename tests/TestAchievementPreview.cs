using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using Phagocyte.Core;
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
                    OpenGalleryWithSamples();
                    _frame = 0;
                    _stage = 1;
                    return false;
                case 1:
                    // Let layout + textures settle before capturing.
                    if (!Gate(ref _frame, 6))
                        return false;
                    AssertThat(_menu!.AchievementView!.CardCount).IsEqual(AchievementManager.Achievements.Count);
                    CaptureScreenshot("gallery_all.png");
                    _menu.AchievementView.SetFilter(AchievementGalleryView.FilterLocked);
                    _frame = 0;
                    _stage = 2;
                    return false;
                case 2:
                    if (!Gate(ref _frame, 4))
                        return false;
                    AssertThat(_menu!.AchievementView!.CardCount).IsEqual(AchievementManager.Achievements.Count - 2);
                    CaptureScreenshot("gallery_locked.png");
                    FreeMenu();
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

    private void OpenGalleryWithSamples()
    {
        var menuScene = AssetLoader.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
        AssertThat(menuScene).IsNotNull();
        _menu = menuScene!.Instantiate<MainMenu>();
        Root.AddChild(_menu);

        // Representative samples: two unlocked (one with art path, one milestone),
        // the rest stay locked.
        _menu.AchievementsBtn!.EmitSignal(Button.SignalName.Pressed);
        AssertThat(_menu.AchievementView!.Visible).IsTrue();
        AchievementManager.Unlock("engulf_20");
        AchievementManager.Unlock("first_digestion");
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
}
