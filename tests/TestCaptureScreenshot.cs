using Godot;
using System;
using Phagocyte.Enemies;
using Phagocyte.Core;
using Phagocyte.UI;

namespace Phagocyte.Tests;

public partial class TestCaptureScreenshot : SceneTree
{
    private int _frames = 0;
    private Node2D? _main;
    private Hud? _hud;

    public override void _Initialize()
    {
        var mainScene = GD.Load<PackedScene>("res://scenes/main.tscn");
        if (mainScene != null)
        {
            _main = mainScene.Instantiate<Node2D>();
            Root.AddChild(_main);
            _hud = _main.GetNodeOrNull<Hud>("HUD");
        }
    }

    public override bool _Process(double delta)
    {
        _frames++;

        if (_frames == 10 && _hud != null && _hud.CellCodexModal != null)
        {
            _hud.CellCodexModal.OpenCodex(2); // Switch to Pathogens tab
        }

        if (_frames < 30) return false;

        var img = Root.GetViewport().GetTexture().GetImage();
        if (img != null && !img.IsEmpty())
        {
            img.SavePng("/Users/zelin/.gemini/antigravity/brain/90aa15b5-daf8-494f-9087-b33113cecd7f/codex_pathogens_showcase.png");
            GD.Print("[SUCCESS] Captured codex pathogens showcase screenshot");
        }
        Quit(0);
        return true;
    }
}
