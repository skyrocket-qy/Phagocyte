using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using Phagocyte.Player;
using Phagocyte.Enemies;

namespace Phagocyte.Tests;

[TestSuite]
public partial class TestPrototype : TestHarness
{
    private int _framesWaited = 0;
    private int _phase = 0;
    private bool _testDone = false;
    private Macrophage? _protoPlayer;
    private float _protoInitialExp;

    public override void _Initialize()
    {
        GD.Print("--- BEGINNING PROTOTYPE AUTOMATED VERIFICATION ---");
        var mainScene = GD.Load<PackedScene>("res://scenes/main.tscn");
        AssertThat(mainScene).IsNotNull();
        var main = mainScene.Instantiate();
        Root.AddChild(main);
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        _framesWaited++;
        if (_framesWaited < 3)
            return false;

        if (_phase == 0)
        {
            _framesWaited = 0;
            var main = Root.GetNodeOrNull<Node2D>("Main");
            AssertThat(main).IsNotNull();

            var player = main!.GetNodeOrNull<Macrophage>("Macrophage");
            AssertThat(player).IsNotNull();

            var enemyContainer = main.GetNodeOrNull<Node2D>("EnemyContainer");
            AssertThat(enemyContainer).IsNotNull();

            // 1. Verify smooth visual vertices deformation & 32 collider sync
            player!.UpdatePseudopodDeformation(0.016f);
            AssertThat(player.Cytoplasm!.Polygon.Length >= 64).IsTrue();
            AssertThat(player.EngulfCollider!.Polygon.Length).IsEqual(32);
            GD.Print($"[PASS] Initial smooth {player.Cytoplasm!.Polygon.Length}-vertex pseudopod deformation & 32-vertex collision sync verified.");

            // 2. Check initial stats (calibrated matrix: HP 140, Area 1.25)
            AssertThat(player.Health).IsEqual(140.0f);
            AssertThat(player.CurrentRadius).IsEqualApprox(player.BaseRadius * 1.25f, 0.01f);
            GD.Print("[PASS] Macrophage initial stats verified.");

            // 3. Test Ingestion Loop (pure EXP feedback).
            // EXP lands at digestion end (Die), not at swallow start.
            _protoPlayer = player;
            _protoInitialExp = player.CurrentExp;

            var staphScene = GD.Load<PackedScene>("res://scenes/enemies/staph_enemy.tscn");
            var staph = staphScene.Instantiate<StaphEnemy>();
            staph.GlobalPosition = player.GlobalPosition;
            enemyContainer!.AddChild(staph);

            // Simulate engulfment
            player.ConsumePathogen(staph);
            _phase = 1;
            return false;
        }

        // Digest tween (0.25s) must finish before EXP lands.
        if (_framesWaited < 30)
            return false;

        _testDone = true;
        AssertThat(_protoPlayer!.CurrentExp).IsGreater(_protoInitialExp);
        AssertThat(_protoPlayer.DigestedCount).IsEqual(1);
        GD.Print("[PASS] Pathogen engulfment grants EXP and increments digestion count.");

        // 4. Test Area Mutation Radius Growth
        _protoPlayer.Stats!.SetBase("area", 2.0f);
        _protoPlayer.UpdatePseudopodDeformation(0.016f);
        float expansionRatio = _protoPlayer.CurrentRadius / _protoPlayer.BaseRadius;
        AssertThat(expansionRatio).IsEqualApprox(2.0f, 0.05f);
        GD.Print($"[PASS] Area mutation radius expansion verified: {expansionRatio:F2}x");

        GD.Print("--- ALL PROTOTYPE VERIFICATION TESTS PASSED SUCCESSFULLY! ---");
        Quit(0);
        return true;
    }
}
