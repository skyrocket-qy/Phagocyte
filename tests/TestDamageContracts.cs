using Godot;
using System;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Player;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Game.Tests;

[TestSuite]
public partial class TestDamageContracts : TestHarness
{
    private int _frame = 0;
    private bool _done = false;

    public override void _Initialize()
    {
        Banner("STARTING DAMAGE CONTRACTS VERIFICATION");
    }

    public override bool _Process(double delta)
    {
        if (_done)
            return true;
        if (!Gate(ref _frame, 2))
            return false;

        _done = true;
        try
        {
            RunEnumTests();
            RunSnapshotTests();
            RunResultTests();
            RunPipelineTests();
            RunDeadOwnerTests();
            RunGraceTests();
            Finish(true, "ALL DAMAGE CONTRACTS TESTS");
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestDamageContracts threw: ", ex);
            Finish(false, "DAMAGE CONTRACTS TESTS");
        }
        return true;
    }

    private void RunEnumTests()
    {
        AssertThat((byte)DamageType.Physical).IsEqual(0);
        AssertThat((byte)Team.Neutral).IsEqual(2);
        AssertThat((byte)HitFlags.BypassesArmor).IsEqual(1);
        AssertThat((byte)HitFlags.NeverCrit).IsEqual(2);
        AssertThat((byte)HitFlags.AlwaysCrit).IsEqual(4);
        AssertThat((byte)HitFlags.CannotBeEvaded).IsEqual(8);
        AssertThat((byte)HitFlags.CannotBeBlocked).IsEqual(16);
        AssertThat((byte)(HitFlags.AlwaysCrit | HitFlags.CannotBeEvaded)).IsEqual(12);
        GD.Print("[PASS] DamageType/Team/HitFlags discriminants are stable.");
    }

    private void RunSnapshotTests()
    {
        HitPayload plain = DamageService.Snapshot(10.0f);
        AssertThat(plain.RawDamage).IsEqual(10.0f);
        AssertThat(plain.Type).IsEqual(DamageType.Physical);
        AssertThat(plain.Faction).IsEqual(Team.Player);
        AssertThat(plain.Flags).IsEqual(HitFlags.None);
        AssertThat(plain.AttackerId).IsEqual(0);
        AssertThat(plain.EffectCount).IsEqual(0);

        var knock = new Vector2(3.0f, 4.0f);
        HitPayload full = DamageService.Snapshot(
            8.0f, DamageType.Fire, Team.Enemy, knock,
            new EffectSpec { EffectId = "burn", Magnitude = 2.0f, Duration = 3.0f }, default, default, 1, 42,
            HitFlags.AlwaysCrit | HitFlags.CannotBeEvaded);
        AssertThat(full.RawDamage).IsEqual(8.0f);
        AssertThat(full.Type).IsEqual(DamageType.Fire);
        AssertThat(full.Faction).IsEqual(Team.Enemy);
        AssertThat(full.Knockback).IsEqual(knock);
        AssertThat(full.EffectCount).IsEqual(1);
        AssertThat(full.Effect0.EffectId).IsEqual("burn");
        AssertThat(full.AttackerId).IsEqual(42);
        AssertThat(full.Flags).IsEqual(HitFlags.AlwaysCrit | HitFlags.CannotBeEvaded);
        GD.Print("[PASS] Snapshot packs the payload with no RNG; force flags survive.");
    }

    private void RunResultTests()
    {
        var result = new HitResult();
        AssertThat(result.DamageDealt).IsEqual(0.0f);
        AssertThat(result.TargetKilled).IsFalse();
        AssertThat(result.IsCrit).IsFalse();
        AssertThat(result.IsEvaded).IsFalse();
        AssertThat(result.IsBlocked).IsFalse();
        GD.Print("[PASS] HitResult defaults are zero.");
    }

    private static EnemyActor NewEnemy(float maxHp)
    {
        var enemy = EnemySpawner.CreateEnemy("staph")!;
        enemy.MaxHealth = maxHp;
        enemy.CurrentHealth = maxHp;
        enemy.Armor = 0.0f;
        return enemy;
    }

    private void RunPipelineTests()
    {
        var cell = new PlayerActor { GlobalPosition = new Vector2(300, 300) };
        var stats = new ActorStats { Name = "ActorStats" };
        cell.AddChild(stats);
        cell.Stats = stats;
        Root.AddChild(cell);
        stats.SetBase("block", 0.0f);
        stats.SetBase("evasion", 0.0f);
        stats.SetBase("crit_chance", 0.0f);
        stats.SetBase("life_steal", 0.0f);
        ulong attackerId = cell.GetInstanceId();

        var foe = NewEnemy(100.0f);
        Root.AddChild(foe);
        HitResult hit = DamagePipeline.ResolveHit(new HitPayload { RawDamage = 20.0f, AttackerId = attackerId }, foe);
        AssertThat(hit.DamageDealt).IsEqual(20.0f);
        AssertThat(hit.IsCrit).IsFalse();
        AssertThat(hit.IsEvaded).IsFalse();
        AssertThat(hit.IsBlocked).IsFalse();
        AssertThat(hit.TargetKilled).IsFalse();
        AssertThat(foe.CurrentHealth).IsEqual(80.0f);
        GD.Print("[PASS] ResolveHit deals unmitigated damage and reports it.");

        stats.SetBase("crit_chance", 1.0f);
        stats.SetBase("crit_damage", 2.0f);
        var critFoe = NewEnemy(100.0f);
        Root.AddChild(critFoe);
        HitResult crit = DamagePipeline.ResolveHit(new HitPayload { RawDamage = 10.0f, AttackerId = attackerId }, critFoe);
        AssertThat(crit.DamageDealt).IsEqual(20.0f);
        AssertThat(crit.IsCrit).IsTrue();
        HitResult forced = DamagePipeline.ResolveHit(new HitPayload
        {
            RawDamage = 10.0f,
            AttackerId = attackerId,
            Flags = HitFlags.NeverCrit,
        }, critFoe);
        AssertThat(forced.DamageDealt).IsEqual(10.0f);
        AssertThat(forced.IsCrit).IsFalse();
        GD.Print("[PASS] Crit rolls at hit from live attacker stats; NeverCrit forces it off.");
        stats.SetBase("crit_chance", 0.0f);

        var stunFx = new EffectSpec { EffectId = "stun", Magnitude = 0.0f, Duration = 1.0f };
        var fullFoe = NewEnemy(100.0f);
        Root.AddChild(fullFoe);
        DamagePipeline.ResolveHit(new HitPayload
        {
            RawDamage = 20.0f,
            AttackerId = attackerId,
            Effect0 = stunFx,
            EffectCount = 1,
        }, fullFoe);
        AssertThat(fullFoe.StunTimer).IsEqualApprox(1.0f, 0.01f);
        var chipFoe = NewEnemy(1000.0f);
        Root.AddChild(chipFoe);
        DamagePipeline.ResolveHit(new HitPayload
        {
            RawDamage = 10.0f,
            AttackerId = attackerId,
            Effect0 = stunFx,
            EffectCount = 1,
        }, chipFoe);
        AssertThat(chipFoe.StunTimer).IsEqualApprox(0.2f, 0.01f);
        GD.Print("[PASS] Sub-threshold hits attenuate ailments vs max-HP threshold.");

        cell.Health = 50.0f;
        stats.SetBase("life_steal", 0.2f);
        var leechFoe = NewEnemy(100000.0f);
        Root.AddChild(leechFoe);
        for (int i = 0; i < 100 && cell.Health <= 50.0f; i++)
            DamagePipeline.ResolveHit(new HitPayload { RawDamage = 10.0f, AttackerId = attackerId }, leechFoe);
        AssertThat(cell.Health).IsEqual(51.0f);
        GD.Print("[PASS] Leech resolves via attacker id after the hit.");
        stats.SetBase("life_steal", 0.0f);

        var doomedFoe = NewEnemy(100.0f);
        Root.AddChild(doomedFoe);
        HitResult lethal = DamagePipeline.ResolveHit(new HitPayload { RawDamage = 500.0f, AttackerId = attackerId }, doomedFoe);
        AssertThat(lethal.TargetKilled).IsTrue();
        AssertThat(lethal.DamageDealt).IsEqual(500.0f);
        GD.Print("[PASS] Lethal hits report TargetKilled.");

        stats.SetBase("evasion", 0.6f);
        HitResult dodged = new HitResult { DamageDealt = -1.0f };
        for (int i = 0; i < 100 && dodged.DamageDealt != 0.0f; i++)
        {
            cell.Health = 100.0f;
            dodged = DamagePipeline.ResolveHit(new HitPayload { RawDamage = 10.0f }, cell);
        }
        AssertThat(dodged.DamageDealt).IsEqual(0.0f);
        AssertThat(dodged.IsEvaded).IsTrue();
        AssertThat(dodged.IsBlocked).IsFalse();
        stats.SetBase("evasion", 0.0f);
        stats.SetBase("block", 0.75f);
        HitResult stopped = new HitResult { DamageDealt = -1.0f };
        for (int i = 0; i < 100 && stopped.DamageDealt != 0.0f; i++)
        {
            cell.Health = 100.0f;
            stopped = DamagePipeline.ResolveHit(new HitPayload { RawDamage = 10.0f }, cell);
        }
        AssertThat(stopped.DamageDealt).IsEqual(0.0f);
        AssertThat(stopped.IsBlocked).IsTrue();
        AssertThat(stopped.IsEvaded).IsFalse();
        stats.SetBase("block", 0.0f);
        GD.Print("[PASS] Avoided hits report IsEvaded / IsBlocked.");

        foe.QueueFree();
        critFoe.QueueFree();
        fullFoe.QueueFree();
        chipFoe.QueueFree();
        leechFoe.QueueFree();
        cell.QueueFree();
    }

    private void RunDeadOwnerTests()
    {
        DeadEntityRegistry.Clear();
        AssertThat(DeadEntityRegistry.Count).IsEqual(0);

        var cell = new PlayerActor { GlobalPosition = new Vector2(300, 300) };
        var stats = new ActorStats { Name = "ActorStats" };
        cell.AddChild(stats);
        cell.Stats = stats;
        Root.AddChild(cell);
        stats.SetBase("block", 0.0f);
        stats.SetBase("evasion", 0.0f);
        stats.SetBase("crit_chance", 0.0f);
        ulong attackerId = cell.GetInstanceId();

        var mgr = new ProjectileManager { Name = "DeadOwnerProjMgr" };
        Root.AddChild(mgr);
        mgr.SetHost(cell);

        var shooter = NewEnemy(100.0f);
        shooter.GlobalPosition = Vector2.Zero;
        Root.AddChild(shooter);
        ulong shooterId = shooter.GetInstanceId();

        mgr.Spawn(Vector2.Zero, Vector2.Left, 100.0f,
            new HitPayload { RawDamage = 5.0f, Faction = Team.Enemy, AttackerId = shooterId },
            0, 5.0f, 8.0f, "enemy_pellet", Team.Enemy);
        mgr._PhysicsProcess(0.016);
        AssertThat(mgr.ActiveCount).IsEqual(1);

        HitResult lethal = DamagePipeline.ResolveHit(new HitPayload { RawDamage = 500.0f, AttackerId = attackerId }, shooter);
        AssertThat(lethal.TargetKilled).IsTrue();
        AssertThat(DeadEntityRegistry.IsDead(shooterId)).IsTrue();

        mgr._PhysicsProcess(0.016);
        AssertThat(mgr.ActiveCount).IsEqual(0);
        GD.Print("[PASS] Enemy pellets fizzle when their owner dies.");

        mgr.Spawn(new Vector2(500, 500), Vector2.Right, 100.0f,
            new HitPayload { RawDamage = 5.0f, Faction = Team.Enemy },
            0, 5.0f, 8.0f, "enemy_pellet", Team.Enemy);
        mgr.Spawn(new Vector2(500, 520), Vector2.Right, 100.0f,
            new HitPayload { RawDamage = 5.0f, AttackerId = shooterId },
            0, 5.0f, 8.0f, "generic", Team.Player);
        mgr._PhysicsProcess(0.016);
        AssertThat(mgr.ActiveCount).IsEqual(2);
        GD.Print("[PASS] Sourceless pellets and player-faction shots survive enemy death.");

        mgr.QueueFree();
        cell.QueueFree();
    }

    private void RunGraceTests()
    {
        var cell = new PlayerActor { GlobalPosition = new Vector2(300, 300) };
        var stats = new ActorStats { Name = "ActorStats" };
        cell.AddChild(stats);
        cell.Stats = stats;
        Root.AddChild(cell);
        stats.SetBase("block", 0.0f);
        stats.SetBase("evasion", 0.0f);
        cell.Health = 10.0f;

        bool diedEmitted = false;
        cell.Died += () => diedEmitted = true;

        cell.TakeDamage(50.0f);
        AssertThat(cell.IsDead).IsTrue();
        AssertThat(cell.IsDowned).IsTrue();
        AssertThat(diedEmitted).IsFalse();
        AssertThat(Engine.TimeScale).IsEqualApprox(0.35f, 0.001f);
        GD.Print("[PASS] Lethal blows open grace: dead, downed, settlement pending.");

        AssertThat(cell.TakeDamage(10.0f, null, false).DamageDealt).IsEqual(0.0f);
        cell._PhysicsProcess(0.5);
        AssertThat(cell.IsDowned).IsTrue();
        AssertThat(diedEmitted).IsFalse();

        cell._PhysicsProcess(0.6);
        AssertThat(cell.IsDowned).IsFalse();
        AssertThat(diedEmitted).IsTrue();
        AssertThat(Engine.TimeScale).IsEqualApprox(1.0f, 0.001f);
        GD.Print("[PASS] Grace expiry emits Died exactly once and restores time.");

        cell.QueueFree();
    }
}
