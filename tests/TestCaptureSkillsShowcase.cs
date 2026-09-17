using Godot;
using System;
using Phagocyte.Enemies;
using Phagocyte.Core;
using Phagocyte.UI;
using Phagocyte.Player;
using Phagocyte.Skills;

namespace Phagocyte.Tests;

public partial class TestCaptureSkillsShowcase : SceneTree
{
    private int _frames = 0;
    private Node2D? _main;
    private BaseCell? _player;
    private Hud? _hud;
    private Camera2D? _cam;

    private bool _initialized = false;

    public override void _Initialize()
    {
        var mainScene = GD.Load<PackedScene>("res://scenes/main.tscn");
        if (mainScene != null)
        {
            _main = mainScene.Instantiate<Node2D>();
            Root.AddChild(_main);
        }
    }

    public override bool _Process(double delta)
    {
        _frames++;

        if (!_initialized && _frames >= 2 && _main != null)
        {
            _initialized = true;
            _player = _main.GetNodeOrNull<BaseCell>("Macrophage");
            _hud = _main.GetNodeOrNull<Hud>("HUD");
            _cam = _player?.GetNodeOrNull<Camera2D>("Camera2D");

            if (_cam != null)
            {
                _cam.Zoom = new Vector2(1.5f, 1.5f);
            }

            if (_player != null && _player.CellSkillManager != null)
            {
                var sm = _player.CellSkillManager;

                // Equip 4 new active cytokine weapons (slot 0 already has ROS Torrent)
                var halo = new NitricOxideHaloSkill();
                var blades = new NucleaseBladesSkill();
                var defensin = new DefensinBarbsSkill();
                var histamine = new HistamineSurgeSkill();

                sm.EquipActive(halo, 1);
                sm.EquipActive(blades, 2);
                sm.EquipActive(defensin, 3);
                sm.EquipActive(histamine, 4);

                // Equip 5 new passive organelle traits
                sm.EquipPassive(new PassiveBilayerHardening(), 0);
                sm.EquipPassive(new PassiveAutophagicRecycle(), 1);
                sm.EquipPassive(new PassiveAerobicGlycolysis(), 2);
                sm.EquipPassive(new PassiveKinesinTransit(), 3);
                sm.EquipPassive(new PassiveEndotoxinBarrier(), 4);

                if (_hud != null)
                {
                    _hud.ConnectPlayer(_player);
                    _hud.UpdateSkillSlots();
                }

                // Spawn a ring of pathogens close to the cell
                var container = _main.GetNodeOrNull<Node2D>("EnemyContainer");
                if (container != null)
                {
                    string[] showcase = new[] { "staph", "pseudomonas", "s_virus", "candida", "staph", "tb" };
                    for (int i = 0; i < showcase.Length; i++)
                    {
                        var p = PathogenSpawner.CreatePathogen(showcase[i]);
                        if (p != null)
                        {
                            float ang = i * (Mathf.Tau / showcase.Length);
                            p.GlobalPosition = _player.GlobalPosition + Vector2.FromAngle(ang) * 125.0f;
                            container.AddChild(p);
                        }
                    }
                }

                GD.Print("[DEBUG] Skills equipped successfully. Slots count: " + sm.GetAllUiData().Count);
            }
        }

        if (_hud != null && _player != null)
        {
            _hud.UpdateSkillSlots();
        }

        // Trigger an interferon wave at frame 20
        if (_frames == 20)
        {
            if (_player != null && _player.CellSkillManager != null)
            {
                var wave = new InterferonWaveSkill();
                wave.Setup(_player, 0);
                _player.AddChild(wave);
                wave.Trigger();
            }
            return false;
        }

        if (_frames == 35)
        {
            var img = Root.GetViewport().GetTexture().GetImage();
            if (img != null && !img.IsEmpty())
            {
                img.SavePng("/Users/zelin/.gemini/antigravity/brain/90aa15b5-daf8-494f-9087-b33113cecd7f/skills_expansion_showcase.png");
                GD.Print("[SUCCESS] Captured skills_expansion_showcase.png");
            }
            Quit(0);
            return true;
        }

        return false;
    }
}
