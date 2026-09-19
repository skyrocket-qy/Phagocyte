using Godot;
using System;
using Phagocyte.Enemies;
using Phagocyte.Core;
using Phagocyte.UI;
using Phagocyte.Player;

namespace Phagocyte.Tests;

public partial class TestCaptureScreenshot : TestHarness
{
    private int _frames = 0;
    private Node2D? _main;
    private Camera2D? _cam;
    private BaseCell? _player;

    public override void _Initialize()
    {
        var mainScene = GD.Load<PackedScene>("res://scenes/main.tscn");
        if (mainScene != null)
        {
            _main = mainScene.Instantiate<Node2D>();
            Root.AddChild(_main);
            _player = _main.GetNodeOrNull<BaseCell>("Macrophage");
            _cam = _player?.GetNodeOrNull<Camera2D>("Camera2D");
            if (_cam != null)
            {
                _cam.Zoom = new Vector2(2.8f, 2.8f); // High-magnification microscope view
            }
        }
    }

    public override bool _Process(double delta)
    {
        _frames++;

        // Let simulation run for 35 frames so deformation, spring physics, and shader stabilize
        if (_frames < 35) return false;

        if (_frames == 35)
        {
            CaptureScreenshot("macrophage_detail.png");

            // Switch to medium close-up
            if (_cam != null)
            {
                _cam.Zoom = new Vector2(1.6f, 1.6f);
            }
            return false;
        }

        if (_frames == 45)
        {
            CaptureScreenshot("game_screen_close.png");

            // Switch to overview
            if (_cam != null)
            {
                _cam.Zoom = new Vector2(0.55f, 0.55f);
            }
            return false;
        }

        if (_frames < 55) return false;

        CaptureScreenshot("pathogens_showcase.png");

        Quit(0);
        return true;
    }
}
