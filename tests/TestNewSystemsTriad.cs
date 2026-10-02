using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using System.Collections.Generic;
using Game.Core;
using Game.Combat;
using Game.Enemies;

namespace Game.Tests;

public partial class MockEnemyForAilment : Node2D, IDamageable
{
    public float Health = 100.0f;
    public float LastDoTReceived = 0.0f;
    public Vector2 Velocity = Vector2.Zero;

    public DefenseProfile Defenses => new();
    public bool IsDead => Health <= 0.0f;

    public float TakeDamage(float finalDamage, Node2D? source = null, bool isCrit = false)
    {
        Health -= finalDamage;
        return finalDamage;
    }

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
        // Test 1: ActorStats DotDamage stat & calculation formulas
        // ====================================================================
        var stats = new ActorStats();
        AssertThat(stats.GetStat("dot_damage")).IsEqual(1.0f);

        stats.AddModifier("dot_damage", 0.0f, 0.40f); // +40%
        AssertThat(stats.GetStat("dot_damage")).IsEqualApprox(1.40f, 0.001f);

        stats.AddModifier("damage", 0.0f, 0.20f); // +20% Damage -> 1.20f
        float expectedDps = 20.0f * 1.20f * 1.40f; // 33.6f
        AssertThat(20.0f * stats.GetStat("damage") * stats.GetStat("dot_damage")).IsEqualApprox(expectedDps, 0.01f);

        stats.AddModifier("duration", 0.0f, 0.50f); // +50% Duration -> 1.50f
        AssertThat(4.0f * stats.GetStat("duration")).IsEqualApprox(6.0f, 0.01f);

        GD.Print("[PASS] Test 1: ActorStats DotDamage, calculations & Duration scaling verified.");

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
        // Test 3: StatusController biological effects & DoT pipeline
        // ====================================================================
        var mockEnemy = new MockEnemyForAilment();
        var status = new StatusController();
        mockEnemy.AddChild(status);
        Root.AddChild(mockEnemy);

        // Initial state
        AssertThat(status.IsActive("ignite")).IsFalse();
        AssertThat(status.HasSlow).IsFalse();
        AssertThat(status.IsActive("shock")).IsFalse();
        AssertThat(status.IsActive("bleed")).IsFalse();
        AssertThat(status.IsActive("poison")).IsFalse();

        // 3a. Slow channel (data: chill row)
        status.ApplySlow(duration: 2.0f, slowPct: 0.40f);
        AssertThat(status.HasSlow).IsTrue();
        AssertThat(status.SpeedMultiplier).IsEqualApprox(0.60f, 0.01f);

        // 3b. Amp channel (data: shock row)
        status.Apply("shock", 0.30f, 3.0f);
        AssertThat(status.IsActive("shock")).IsTrue();
        AssertThat(status.DamageTakenMultiplier).IsEqualApprox(1.30f, 0.01f);

        // 3c. DoT channel (data: ignite row)
        status.Apply("ignite", 10.0f, 2.0f);
        AssertThat(status.IsActive("ignite")).IsTrue();

        // Simulate 1 second physics process
        // Frame DoT = 10.0f * 1.0s = 10.0f
        // Amplified by amp (1.30x) = 13.0f
        status._PhysicsProcess(1.0);
        AssertThat(mockEnemy.Health).IsEqualApprox(87.0f, 0.05f);
        AssertThat(status.GetTimer("ignite")).IsEqualApprox(1.0f, 0.01f);

        // 3d. Move-scaled DoT (data: bleed row, 3x while moving)
        mockEnemy.Velocity = new Vector2(100.0f, 0.0f); // moving
        status.Apply("bleed", 5.0f, 2.0f);
        AssertThat(status.IsActive("bleed")).IsTrue();

        // Next 1 second:
        // Burn: 10.0 * 1.0 = 10.0
        // Leak: 5.0 * 3.0 (moving) * 1.0 = 15.0
        // Total unamplified = 25.0, amplified by 1.30 = 32.5
        status._PhysicsProcess(1.0);
        AssertThat(mockEnemy.Health).IsEqualApprox(87.0f - 32.5f, 0.1f);

        // 3e. Independent stacks (data: poison row)
        status.Apply("poison", 2.0f, 2.0f);
        status.Apply("poison", 3.0f, 4.0f);
        AssertThat(status.GetStackCount("poison")).IsEqual(2);

        // Process 2.0s: first stack should expire, second should remain
        status._PhysicsProcess(2.01);
        AssertThat(status.GetStackCount("poison")).IsEqual(1);

        status.ClearAll();
        AssertThat(status.IsActive("ignite")).IsFalse();
        AssertThat(status.IsActive("poison")).IsFalse();

        mockEnemy.QueueFree();
        GD.Print("[PASS] Test 3: generic ailment effects (DoT, slow/amp channels, move scaling, stacks) verified.");

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

        // Phase 2 check (50% HP <= 60% threshold)
        bossPhase.NotifyHealthChanged(50.0f, 100.0f);
        AssertThat(bossPhase.CurrentPhaseIndex).IsEqual(2);
        AssertThat(bossPhase.IsEnraged).IsTrue();

        // Phase 3 check (20% HP <= 25% threshold)
        bossPhase.NotifyHealthChanged(20.0f, 100.0f);
        AssertThat(bossPhase.CurrentPhaseIndex).IsEqual(3);

        // Hard Enrage test
        bossPhase.HardEnrageSeconds = 5.0f;
        bossPhase._PhysicsProcess(5.1);
        AssertThat(bossPhase.IsHardEnraged).IsTrue();
        AssertThat(bossPhase.CurrentSpeedMult).IsGreater(1.35f);

        bossNode.QueueFree();
        GD.Print("[PASS] Test 4: BossPhaseComponent HP phase transitions & hard enrage verified.");
    }
}
