using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using Phagocyte.Player;
using Phagocyte.Enemies;

namespace Phagocyte.Tests;

[TestSuite]
public partial class TestPrototype : SceneTree
{
    private int _framesWaited = 0;
    private bool _testDone = false;

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

        _testDone = true;
        var main = Root.GetNodeOrNull<Node2D>("Main");
        AssertThat(main).IsNotNull();

        var player = main!.GetNodeOrNull<Macrophage>("Macrophage");
        AssertThat(player).IsNotNull();

        var enemyContainer = main.GetNodeOrNull<Node2D>("EnemyContainer");
        AssertThat(enemyContainer).IsNotNull();

        // 1. Verify 64 smooth visual vertices deformation & 32 collider sync
        player!.UpdatePseudopodDeformation(0.016f);
        AssertThat(player.Cytoplasm!.Polygon.Length).IsEqual(64);
        AssertThat(player.EngulfCollider!.Polygon.Length).IsEqual(32);
        GD.Print("[PASS] Initial smooth 64-vertex pseudopod deformation & 32-vertex collision sync verified.");

        // 2. Check initial stats
        AssertThat(player.Health).IsEqual(100.0f);
        AssertThat(player.Satiety).IsEqual(0.0f);
        AssertThat(player.CurrentRadius).IsEqual(player.BaseRadius);
        GD.Print("[PASS] Macrophage initial stats verified.");

        // 3. Test Ingestion Loop
        float initialHp = 80.0f;
        player.Health = initialHp;

        var staphScene = GD.Load<PackedScene>("res://scenes/enemies/staph_enemy.tscn");
        var staph = staphScene.Instantiate<StaphEnemy>();
        staph.GlobalPosition = player.GlobalPosition;
        enemyContainer!.AddChild(staph);

        // Simulate engulfment
        player.ConsumePathogen(staph);

        AssertThat(player.Satiety).IsGreater(0.0f);
        AssertThat(player.Health).IsGreater(initialHp);
        AssertThat(player.DigestedCount).IsEqual(1);
        GD.Print("[PASS] Pathogen engulfment, satiety accumulation, and passive healing verified.");

        // 4. Test Satiety Growth (up to 2.5x)
        player.Satiety = 100.0f;
        player.UpdatePseudopodDeformation(0.016f);
        float expansionRatio = player.CurrentRadius / player.BaseRadius;
        AssertThat(expansionRatio).IsGreaterEqual(2.4f);
        GD.Print($"[PASS] Dynamic cell radius expansion (approx 2.5x) verified: {expansionRatio:F2}x");

        // 5. Test Respiratory Burst
        player.TriggerRespiratoryBurst();
        AssertThat(player.IsRespiratoryBurst).IsTrue();
        AssertThat(player.CurrentSpeed).IsGreater(player.BaseSpeed * 2.4f);
        AssertThat(player.AcidicAura!.Monitoring).IsTrue();
        GD.Print("[PASS] Respiratory Burst (+150% speed, acidic aura) verified.");

        GD.Print("--- ALL PROTOTYPE VERIFICATION TESTS PASSED SUCCESSFULLY! ---");
        Quit(0);
        return true;
    }
}
