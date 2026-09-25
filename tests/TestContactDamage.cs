using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Enemies;
using Phagocyte.Player;
using Phagocyte.Skills;

using Phagocyte.Core;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies survivor-like contact damage: an overlapping monster hurts the
/// cell on a per-enemy tick, and the cell never damages monsters back by
/// touching (one-directional by design).
/// </summary>
[TestSuite]
public partial class TestContactDamage : TestHarness
{
    private int _phase = 0;
    private int _framesWaited = 0;
    private bool _testDone = false;

    private Macrophage? _player;
    private StaphEnemy? _staph;
    private Node2D? _arena;
    private float _startHealth;

    public override void _Initialize()
    {
        Banner("STARTING CONTACT DAMAGE VERIFICATION");
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        switch (_phase)
        {
            case 0:
                _framesWaited++;
                if (_framesWaited < 3)
                    return false;
                _framesWaited = 0;

                if (!SetupArena())
                {
                    GD.PrintErr("[FAIL] TestContactDamage setup failed; aborting.");
                    Quit(1);
                    return true;
                }
                _phase = 1;
                return false;

            case 1:
                // Event-driven: the first contact tick fires on overlap.
                // Timeout only guards a broken wiring, not timing.
                _framesWaited++;
                if (_player!.Health < _startHealth)
                {
                    AssertThat(GodotObject.IsInstanceValid(_staph)).IsTrue();
                    AssertThat(_staph!.CurrentHealth).IsEqual(25.0f);
                    GD.Print("[PASS] Contact tick hurt the cell; overlapping monster unharmed (one-directional).");
                    _testDone = true;
                    GD.Print("--- ALL CONTACT DAMAGE TESTS PASSED! ---");
                    Quit(0);
                    return true;
                }
                if (_framesWaited > 1200)
                {
                    GD.PrintErr("[FAIL] No contact tick fired within 1200 frames.");
                    Quit(1);
                    return true;
                }
                return false;
        }

        return false;
    }

    private bool SetupArena()
    {
        _arena = new Node2D { Name = "ContactArena" };
        Root.AddChild(_arena);

        var playerScene = AssetLoader.Load<PackedScene>("res://scenes/characters/macrophage.tscn");
        if (playerScene == null)
            return false;
        _player = playerScene.Instantiate<Macrophage>();
        if (_player == null)
            return false;
        _arena.AddChild(_player);
        _player.GlobalPosition = Vector2.Zero;

        if (_player.Stats == null || _player.CellSkillManager == null)
            return false;

        // Deterministic first tick: no dodge, no block; armor pipeline stays live.
        _player.Stats.SetBase("evasion", 0.0f);
        _player.Stats.SetBase("block", 0.0f);
        _startHealth = _player.Health;

        // Park the grasp: contact damage is measured with skills silenced.
        if (_player.CellSkillManager.GetActiveSlot(0) is PhagocyticGraspSkill grasp)
        {
            grasp.Cooldown = 9999.0f;
            grasp.CooldownTimer = 9999.0f;
        }

        // Overlapping from frame one: well inside the contact sensor.
        _staph = new StaphEnemy { GlobalPosition = new Vector2(10, 0) };
        _arena.AddChild(_staph);
        return true;
    }
}
