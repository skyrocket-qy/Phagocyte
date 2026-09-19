using Godot;
using System;
using Phagocyte.Enemies;
using Phagocyte.Core;
using Phagocyte.UI;
using Phagocyte.Player;

namespace Phagocyte.Tests;

public partial class TestCaptureHudScreenshot : TestHarness
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
            CaptureScreenshot("hud_survivor_normal.png");

            // Damage player to 55% HP (triggers under-cell amber/green arc bar)
            if (_player != null)
            {
                _player.TakeDamage(45.0f);
            }
            return false;
        }

        if (_frames == 35)
        {
            CaptureScreenshot("hud_survivor_damaged.png");

            // Drop player to 20% HP (critical vignette + ruby heartbeat arc bar)
            if (_player != null)
            {
                _player.TakeDamage(35.0f);
            }
            return false;
        }

        if (_frames == 45)
        {
            CaptureScreenshot("hud_survivor_critical.png");

            Quit(0);
            return true;
        }

        return false;
    }
}
