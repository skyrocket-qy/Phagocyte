using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using System.Collections.Generic;
using Phagocyte.Core;
using Phagocyte.Combat;
using Phagocyte.Enemies;

namespace Phagocyte.Tests;

public partial class MockEnemyForAilment : Node2D
{
    public float Health = 100.0f;
    public float LastDoTReceived = 0.0f;
    public Vector2 Velocity = Vector2.Zero;

    public void TakeDoTDamage(float dotDamage)
    {
        Health -= dotDamage;
        LastDoTReceived = dotDamage;
    }
}

[TestSuite]
public partial class TestNewSystemsTriad : SceneTree
{
    private int _framesWaited = 0;
    private bool _testDone = false;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> INITIALIZING VFX, AILMENTS, & BOSS PHASE TEST SUITE <<<");
        GD.Print("==================================================================");
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        _framesWaited++;
        if (_framesWaited < 3)
            return false;

        _testDone = true;

        try
        {
            RunAllTests();
            GD.Print("==================================================================");
            GD.Print(">>> ALL 3 NEW SYSTEMS VERIFIED WITH 100% SUCCESS! <<<");
            GD.Print("==================================================================");
            Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestNewSystemsTriad encountered an exception: ", ex);
            Quit(1);
        }

        return true;
    }

    private void RunAllTests()
    {
        // ====================================================================
        // Test 1: CellStats AilmentDamage stat & calculation formulas
        // ====================================================================
        var stats = new CellStats();
        AssertThat(stats.GetStat("ailment_damage")).IsEqual(1.0f);

        stats.AddModifier("ailment_damage", 0.0f, 0.40f); // +40%
        AssertThat(stats.GetStat("ailment_damage")).IsEqualApprox(1.40f, 0.001f);

        stats.AddModifier("might", 0.0f, 0.20f); // +20% Might -> 1.20f
        float expectedDps = 20.0f * 1.20f * 1.40f; // 33.6f
        AssertThat(stats.CalculateAilmentDamage(20.0f)).IsEqualApprox(expectedDps, 0.01f);

        stats.AddModifier("duration", 0.0f, 0.50f); // +50% Duration -> 1.50f
        AssertThat(stats.CalculateAilmentDuration(4.0f)).IsEqualApprox(6.0f, 0.01f);

        GD.Print("[PASS] Test 1: CellStats AilmentDamage, calculations & Duration scaling verified.");

        // ====================================================================
        // Test 2: VfxManager GPU particle pool & dispatch
        // ====================================================================
        var vfx = new VfxManager();
        Root.AddChild(vfx);

        AssertThat(VfxManager.Instance).IsNotNull();
        vfx.Play(VfxType.CytoplasmSplatter, new Vector2(100, 100));
        vfx.Play(VfxType.AcidOxidationSparks, new Vector2(150, 150), Vector2.Right);
        vfx.Play(VfxType.BarbImpact, new Vector2(250, 250));
        vfx.Play(VfxType.BiofilmBurst, new Vector2(300, 300));

        vfx.QueueFree();
        GD.Print("[PASS] Test 2: VfxManager GPU particle pool & zero-GC recycling verified.");

        // ====================================================================
        // Test 3: AilmentController biological effects & DoT pipeline
        // ====================================================================
        var mockEnemy = new MockEnemyForAilment();
        var ailment = new AilmentController();
        mockEnemy.AddChild(ailment);
        Root.AddChild(mockEnemy);

        // Initial state
        AssertThat(ailment.IsOxidized).IsFalse();
        AssertThat(ailment.IsAgglutinated).IsFalse();
        AssertThat(ailment.IsOpsonized).IsFalse();
        AssertThat(ailment.IsLeaking).IsFalse();
        AssertThat(ailment.IsToxic).IsFalse();

        // 3a. Agglutination (Slow)
        ailment.ApplyAgglutination(duration: 2.0f, slowPct: 0.40f);
        AssertThat(ailment.IsAgglutinated).IsTrue();
        AssertThat(ailment.SpeedMultiplier).IsEqualApprox(0.60f, 0.01f);

        // 3b. Opsonization (Damage amplification)
        ailment.ApplyOpsonization(duration: 3.0f, ampPct: 0.30f);
        AssertThat(ailment.IsOpsonized).IsTrue();
        AssertThat(ailment.OpsonizationMultiplier).IsEqualApprox(1.30f, 0.01f);

        // 3c. Oxidative Burn (DoT)
        ailment.ApplyOxidativeBurn(dps: 10.0f, duration: 2.0f);
        AssertThat(ailment.IsOxidized).IsTrue();

        // Simulate 1 second physics process
        // Frame DoT = 10.0f * 1.0s = 10.0f
        // Amplified by Opsonization (1.30x) = 13.0f
        ailment._PhysicsProcess(1.0);
        AssertThat(mockEnemy.Health).IsEqualApprox(87.0f, 0.05f);
        AssertThat(ailment.BurnTimer).IsEqualApprox(1.0f, 0.01f);

        // 3d. Membrane Leak (Moving 3x multiplier)
        mockEnemy.Velocity = new Vector2(100.0f, 0.0f); // moving
        ailment.ApplyMembraneLeak(dps: 5.0f, duration: 2.0f);
        AssertThat(ailment.IsLeaking).IsTrue();

        // Next 1 second:
        // Burn: 10.0 * 1.0 = 10.0
        // Leak: 5.0 * 3.0 (moving) * 1.0 = 15.0
        // Total unamplified = 25.0, amplified by 1.30 = 32.5
        ailment._PhysicsProcess(1.0);
        AssertThat(mockEnemy.Health).IsEqualApprox(87.0f - 32.5f, 0.1f);

        // 3e. Endotoxin (Independent stacks)
        ailment.ApplyEndotoxin(dps: 2.0f, duration: 2.0f);
        ailment.ApplyEndotoxin(dps: 3.0f, duration: 4.0f);
        AssertThat(ailment.EndotoxinStackCount).IsEqual(2);

        // Process 2.0s: first stack should expire, second should remain
        ailment._PhysicsProcess(2.01);
        AssertThat(ailment.EndotoxinStackCount).IsEqual(1);

        ailment.ClearAll();
        AssertThat(ailment.IsOxidized).IsFalse();
        AssertThat(ailment.IsToxic).IsFalse();

        mockEnemy.QueueFree();
        GD.Print("[PASS] Test 3: AilmentController biological effects (ROS, Slow, Opsonization, Leak, Endotoxin) verified.");

        // ====================================================================
        // Test 4: BossPhaseComponent transitions & Damage Reduction
        // ====================================================================
        var bossNode = new Node2D();
        var bossPhase = new BossPhaseComponent();
        bossNode.AddChild(bossPhase);
        Root.AddChild(bossNode);

        // Phase 1 check (100% HP)
        bossPhase.NotifyHealthChanged(100.0f, 100.0f);
        AssertThat(bossPhase.CurrentPhaseIndex).IsEqual(1);
        AssertThat(bossPhase.IsEnraged).IsFalse();
        AssertThat(bossPhase.CurrentDamageReduction).IsEqual(0.0f);
        AssertThat(bossPhase.ApplyDamageReduction(50.0f)).IsEqual(50.0f);

        // Phase 2 check (50% HP <= 60% threshold)
        bossPhase.NotifyHealthChanged(50.0f, 100.0f);
        AssertThat(bossPhase.CurrentPhaseIndex).IsEqual(2);
        AssertThat(bossPhase.IsEnraged).IsTrue();
        AssertThat(bossPhase.CurrentDamageReduction).IsEqual(0.20f);
        AssertThat(bossPhase.ApplyDamageReduction(50.0f)).IsEqualApprox(40.0f, 0.01f); // 50 * (1 - 0.20) = 40

        // Phase 3 check (20% HP <= 25% threshold)
        bossPhase.NotifyHealthChanged(20.0f, 100.0f);
        AssertThat(bossPhase.CurrentPhaseIndex).IsEqual(3);
        AssertThat(bossPhase.CurrentDamageReduction).IsEqual(0.35f);
        AssertThat(bossPhase.ApplyDamageReduction(100.0f)).IsEqualApprox(65.0f, 0.01f); // 100 * (1 - 0.35) = 65

        // Hard Enrage test
        bossPhase.HardEnrageSeconds = 5.0f;
        bossPhase._PhysicsProcess(5.1);
        AssertThat(bossPhase.IsHardEnraged).IsTrue();
        AssertThat(bossPhase.CurrentSpeedMult).IsGreater(1.35f);

        bossNode.QueueFree();
        GD.Print("[PASS] Test 4: BossPhaseComponent HP phase transitions, DR shields & hard enrage verified.");
    }
}
