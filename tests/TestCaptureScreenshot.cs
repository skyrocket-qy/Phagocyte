using Godot;
using System;

namespace Phagocyte.Tests;

public partial class TestCaptureScreenshot : SceneTree
{
    private int _frames = 0;
    private Node2D? _main;
    public override void _Initialize()
    {
        var mainScene = GD.Load<PackedScene>("res://scenes/main.tscn");
        if (mainScene != null)
        {
            _main = mainScene.Instantiate<Node2D>();
            Root.AddChild(_main);
            var cam = _main.GetNodeOrNull<Camera2D>("Macrophage/Camera2D");
            if (cam != null)
            {
                cam.Zoom = new Vector2(1.5f, 1.5f);
            }
        }
    }

    public override bool _Process(double delta)
    {
        _frames++;
        if (_frames < 30) return false;

        var img = Root.GetViewport().GetTexture().GetImage();
        if (img != null && !img.IsEmpty())
        {
            img.SavePng("/Users/zelin/.gemini/antigravity/brain/90aa15b5-daf8-494f-9087-b33113cecd7f/game_screen_close.png");
            GD.Print("[SUCCESS] Captured close-up screenshot");
        }
        Quit(0);
        return true;
    }
}
