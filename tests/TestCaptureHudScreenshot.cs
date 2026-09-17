using Godot;
using System;
using Phagocyte.Enemies;
using Phagocyte.Core;
using Phagocyte.UI;
using Phagocyte.Player;

namespace Phagocyte.Tests;

public partial class TestCaptureHudScreenshot : SceneTree
{
    private int _frames = 0;
    private Node2D? _main;
    private BaseCell? _player;
    private Hud? _hud;

    public override void _Initialize()
    {
        var mainScene = GD.Load<PackedScene>("res://scenes/main.tscn");
        if (mainScene != null)
        {
            _main = mainScene.Instantiate<Node2D>();
            Root.AddChild(_main);
            _player = _main.GetNodeOrNull<BaseCell>("Macrophage");
            _hud = _main.GetNodeOrNull<Hud>("HUD");
        }
    }

    public override bool _Process(double delta)
    {
        _frames++;

        // Let simulation stabilize for 20 frames
        if (_frames < 20) return false;

        if (_frames == 20)
        {
            // Give some exp and a kill
            if (_player != null)
            {
                _player.AddExp(12.0f);
                _player.DigestedCount = 5;
            }
            if (_hud != null && _player != null)
            {
                _hud.ConnectPlayer(_player);
                _hud.UpdateExpDisplay();
            }
            return false;
        }

        if (_frames == 25)
        {
            var img = Root.GetViewport().GetTexture().GetImage();
            if (img != null && !img.IsEmpty())
            {
                img.SavePng("/Users/zelin/.gemini/antigravity/brain/90aa15b5-daf8-494f-9087-b33113cecd7f/hud_survivor_normal.png");
                GD.Print("[SUCCESS] Captured hud_survivor_normal.png");
            }

            // Damage player to 55% HP (triggers under-cell amber/green arc bar)
            if (_player != null)
            {
                _player.TakeDamage(45.0f);
            }
            return false;
        }

        if (_frames == 35)
        {
            var img = Root.GetViewport().GetTexture().GetImage();
            if (img != null && !img.IsEmpty())
            {
                img.SavePng("/Users/zelin/.gemini/antigravity/brain/90aa15b5-daf8-494f-9087-b33113cecd7f/hud_survivor_damaged.png");
                GD.Print("[SUCCESS] Captured hud_survivor_damaged.png (with under-cell arc bar)");
            }

            // Drop player to 20% HP (critical vignette + ruby heartbeat arc bar)
            if (_player != null)
            {
                _player.TakeDamage(35.0f);
            }
            return false;
        }

        if (_frames == 45)
        {
            var img = Root.GetViewport().GetTexture().GetImage();
            if (img != null && !img.IsEmpty())
            {
                img.SavePng("/Users/zelin/.gemini/antigravity/brain/90aa15b5-daf8-494f-9087-b33113cecd7f/hud_survivor_critical.png");
                GD.Print("[SUCCESS] Captured hud_survivor_critical.png (with critical vignette & pulsing red arc)");
            }

            Quit(0);
            return true;
        }

        return false;
    }
}
