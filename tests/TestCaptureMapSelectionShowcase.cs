using Godot;
using System;
using Phagocyte.Core;
using Phagocyte.UI;

namespace Phagocyte.Tests;

public partial class TestCaptureMapSelectionShowcase : TestHarness
{
    private int _frames = 0;
    private MainMenu? _menu;

    public override void _Initialize()
    {
        var menuScene = GD.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
        if (menuScene != null)
        {
            _menu = menuScene.Instantiate<MainMenu>();
            Root.AddChild(_menu);
        }
    }

    public override bool _Process(double delta)
    {
        _frames++;

        if (_frames == 2 && _menu != null)
        {
            // Advance from Title -> Class -> Map
            _menu.OnStartPressed();
            _menu.SelectClass("macrophage");
            _menu.OnClassConfirmPressed();

            // Select hepatic_sinusoid (Liver) for showcase
            _menu.SelectMap("hepatic_sinusoid");
        }

        if (_frames == 20 && _menu != null)
        {
            // Give time for scanline to move and draw
            CaptureScreenshot("map_selection_hologram_showcase.png");
            Quit(0);
            return true;
        }

        return false;
    }
}
