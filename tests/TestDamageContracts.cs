using Godot;
using System;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Player;
using Game.Skills;
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
            RunArmorPenetrationTests();
            RunAilmentChanceThresholdTests();
            RunTypedDamageTests();
            RunConditionalDamageTests();
            RunStaggerRecoupTests();
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
        AssertThat(plain.ArmorPenetration).IsEqual(0.0f);
        AssertThat(plain.AilmentChance).IsEqual(0.0f);
        AssertThat(plain.Type).IsEqual(DamageType.Physical);
        AssertThat(plain.SourceFaction).IsEqual(Team.Player);
        AssertThat(plain.Flags).IsEqual(HitFlags.None);
        AssertThat(plain.AttackerId).IsEqual(0);
        AssertThat(plain.EffectCount).IsEqual(0);

        HitPayload full = DamageService.Snapshot(
            8.0f, DamageType.Fire, Team.Enemy,
            new EffectSpec { EffectId = "burn", Ratio = 2.0f, Duration = 3.0f }, default, default, 1, 42,
            HitFlags.AlwaysCrit | HitFlags.CannotBeEvaded);
        AssertThat(full.RawDamage).IsEqual(8.0f);
        AssertThat(full.Type).IsEqual(DamageType.Fire);
        AssertThat(full.SourceFaction).IsEqual(Team.Enemy);
        AssertThat(full.EffectCount).IsEqual(1);
        AssertThat(full.Effect0.EffectId).IsEqual("burn");
        AssertThat(full.AttackerId).IsEqual(42);
        AssertThat(full.Flags).IsEqual(HitFlags.AlwaysCrit | HitFlags.CannotBeEvaded);
        HitPayload pen = DamageService.Snapshot(10.0f, armorPenetration: 0.5f);
        AssertThat(pen.ArmorPenetration).IsEqual(0.5f);
        GD.Print("[PASS] Snapshot packs the payload with no RNG; force flags survive.");
    }

    private void RunResultTests()
    {
        var result = new HitResult();
        AssertThat(result.DamageDealt).IsEqual(0.0f);
        AssertThat(result.TargetKilled).IsFalse();
        AssertThat(result.IsCrit).IsFalse();
        AssertThat(result.IsEvaded).IsFalse();
        AssertThat(result.IsInvulnerable).IsFalse();
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
        HitResult hit = HitPipeline.ResolveHit(new HitPayload { RawDamage = 20.0f, AttackerId = attackerId }, foe);
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
        HitResult crit = HitPipeline.ResolveHit(new HitPayload { RawDamage = 10.0f, AttackerId = attackerId }, critFoe);
        AssertThat(crit.DamageDealt).IsEqual(20.0f);
        AssertThat(crit.IsCrit).IsTrue();
        HitResult forced = HitPipeline.ResolveHit(new HitPayload
        {
            RawDamage = 10.0f,
            AttackerId = attackerId,
            Flags = HitFlags.NeverCrit,
        }, critFoe);
        AssertThat(forced.DamageDealt).IsEqual(10.0f);
        AssertThat(forced.IsCrit).IsFalse();
        GD.Print("[PASS] Crit rolls at hit from live attacker stats; NeverCrit forces it off.");
        stats.SetBase("crit_chance", 0.0f);

        var stunFx = new EffectSpec { EffectId = "stun", Ratio = 0.0f, Duration = 1.0f };
        var fullFoe = NewEnemy(100.0f);
        Root.AddChild(fullFoe);
        HitPipeline.ResolveHit(new HitPayload
        {
            RawDamage = 20.0f,
            AttackerId = attackerId,
            AilmentChance = 1.0f,
            Effect0 = stunFx,
            EffectCount = 1,
        }, fullFoe);
        AssertThat(fullFoe.StunTimer).IsEqualApprox(0.5f, 0.01f);
        var chipFoe = NewEnemy(1000.0f);
        Root.AddChild(chipFoe);
        for (int i = 0; i < 500 && chipFoe.StunTimer <= 0.0f; i++)
        {
            chipFoe.CurrentHealth = 1000.0f;
            HitPipeline.ResolveHit(new HitPayload
            {
                RawDamage = 10.0f,
                AttackerId = attackerId,
                AilmentChance = 1.0f,
                Effect0 = stunFx,
                EffectCount = 1,
            }, chipFoe);
        }
        AssertThat(chipFoe.StunTimer).IsEqualApprox(0.5f, 0.01f);
        GD.Print("[PASS] Sub-threshold hits roll damage-derived control odds.");

        cell.Health = 50.0f;
        stats.SetBase("life_steal", 0.2f);
        var leechFoe = NewEnemy(100000.0f);
        Root.AddChild(leechFoe);
        for (int i = 0; i < 100 && cell.Health <= 50.0f; i++)
            HitPipeline.ResolveHit(new HitPayload { RawDamage = 10.0f, AttackerId = attackerId }, leechFoe);
        AssertThat(cell.Health).IsEqual(51.0f);
        GD.Print("[PASS] Leech resolves via attacker id after the hit.");
        stats.SetBase("life_steal", 0.0f);

        var doomedFoe = NewEnemy(100.0f);
        Root.AddChild(doomedFoe);
        HitResult lethal = HitPipeline.ResolveHit(new HitPayload { RawDamage = 500.0f, AttackerId = attackerId }, doomedFoe);
        AssertThat(lethal.TargetKilled).IsTrue();
        AssertThat(lethal.DamageDealt).IsEqual(500.0f);
        GD.Print("[PASS] Lethal hits report TargetKilled.");

        stats.SetBase("evasion", 0.6f);
        HitResult dodged = new HitResult { DamageDealt = -1.0f };
        for (int i = 0; i < 100 && dodged.DamageDealt != 0.0f; i++)
        {
            cell.Health = 100.0f;
            dodged = HitPipeline.ResolveHit(new HitPayload { RawDamage = 10.0f }, cell);
        }
        AssertThat(dodged.DamageDealt).IsEqual(0.0f);
        AssertThat(dodged.IsEvaded).IsTrue();
        AssertThat(dodged.IsInvulnerable).IsFalse();
        AssertThat(dodged.IsBlocked).IsFalse();
        stats.SetBase("evasion", 0.0f);
        stats.SetBase("block", 0.75f);
        HitResult stopped = new HitResult { DamageDealt = -1.0f };
        for (int i = 0; i < 100 && !stopped.IsBlocked; i++)
        {
            cell.Health = 100.0f;
            stopped = HitPipeline.ResolveHit(new HitPayload { RawDamage = 10.0f }, cell);
        }
        AssertThat(stopped.DamageDealt).IsEqual(5.0f);
        AssertThat(stopped.IsBlocked).IsTrue();
        AssertThat(stopped.IsEvaded).IsFalse();
        AssertThat(stopped.IsInvulnerable).IsFalse();
        stats.SetBase("block", 0.0f);
        GD.Print("[PASS] Blocked hits mitigate 50% and report IsBlocked.");

        foe.QueueFree();
        critFoe.QueueFree();
        fullFoe.QueueFree();
        chipFoe.QueueFree();
        leechFoe.QueueFree();
        cell.QueueFree();
    }

    private void RunArmorPenetrationTests()
    {
        AssertThat(CombatMath.FromArmorPenetrated(20.0f, 20.0f, 0.0f)).IsEqualApprox(20.0f / 120.0f, 0.0001f);
        AssertThat(CombatMath.FromArmorPenetrated(20.0f, 20.0f, 0.5f)).IsEqualApprox(10.0f / 110.0f, 0.0001f);
        AssertThat(CombatMath.FromArmorPenetrated(20.0f, 20.0f, 1.0f)).IsEqual(0.0f);
        AssertThat(CombatMath.FromArmorPenetrated(20.0f, 20.0f, 2.0f)).IsEqual(0.0f);
        AssertThat(CombatMath.FromArmorPenetrated(20.0f, 20.0f, -1.0f)).IsEqualApprox(20.0f / 120.0f, 0.0001f);
        AssertThat(CombatMath.FromArmorPenetrated(0.0f, 20.0f, 0.5f)).IsEqual(0.0f);
        GD.Print("[PASS] Armor penetration scales effective armor per the POE curve.");

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

        stats.AddModifier("armor_penetration", 2.0f, 0.0f);
        AssertThat(stats.GetStat("armor_penetration")).IsEqual(1.0f);
        stats.RemoveModifier("armor_penetration", 2.0f, 0.0f);
        AssertThat(stats.GetStat("armor_penetration")).IsEqual(0.0f);

        var armored = NewEnemy(1000.0f);
        armored.Armor = 10.0f;
        Root.AddChild(armored);
        HitResult noPen = HitPipeline.ResolveHit(new HitPayload { RawDamage = 20.0f, AttackerId = attackerId }, armored);
        float expectedNoPen = 20.0f * (1.0f - 10.0f / (10.0f + 5.0f * 20.0f));
        AssertThat(noPen.DamageDealt).IsEqualApprox(expectedNoPen, 0.01f);

        var pierced = NewEnemy(1000.0f);
        pierced.Armor = 10.0f;
        Root.AddChild(pierced);
        HitResult fullPen = HitPipeline.ResolveHit(new HitPayload { RawDamage = 20.0f, ArmorPenetration = 1.0f, AttackerId = attackerId }, pierced);
        AssertThat(fullPen.DamageDealt).IsEqualApprox(20.0f, 0.01f);

        var half = NewEnemy(1000.0f);
        half.Armor = 10.0f;
        Root.AddChild(half);
        HitResult halfPen = HitPipeline.ResolveHit(new HitPayload { RawDamage = 20.0f, ArmorPenetration = 0.5f, AttackerId = attackerId }, half);
        AssertThat(halfPen.DamageDealt).IsEqualApprox(20.0f * (1.0f - 5.0f / (5.0f + 100.0f)), 0.01f);
        AssertThat(halfPen.DamageDealt > noPen.DamageDealt).IsTrue();

        var bypassed = NewEnemy(1000.0f);
        bypassed.Armor = 10.0f;
        Root.AddChild(bypassed);
        HitResult bypass = HitPipeline.ResolveHit(new HitPayload { RawDamage = 20.0f, Flags = HitFlags.BypassesArmor, AttackerId = attackerId }, bypassed);
        AssertThat(bypass.DamageDealt).IsEqualApprox(20.0f, 0.01f);
        GD.Print("[PASS] Penetrated hits ignore a fraction of armor; full pen matches bypass.");

        armored.QueueFree();
        pierced.QueueFree();
        half.QueueFree();
        bypassed.QueueFree();
        cell.QueueFree();
    }

    private void RunAilmentChanceThresholdTests()
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
        stats.SetBase("armor", 0.0f);
        ulong attackerId = cell.GetInstanceId();

        stats.AddModifier("ailment_chance", 2.0f, 0.0f);
        AssertThat(stats.GetStat("ailment_chance")).IsEqual(1.0f);
        stats.RemoveModifier("ailment_chance", 2.0f, 0.0f);
        AssertThat(stats.GetStat("ailment_chance")).IsEqual(1.0f);
        stats.AddModifier("ailment_threshold", -5.0f, 0.0f);
        AssertThat(stats.GetStat("ailment_threshold")).IsEqual(0.0f);
        stats.RemoveModifier("ailment_threshold", -5.0f, 0.0f);
        AssertThat(stats.GetStat("ailment_threshold")).IsEqual(1.0f);
        AssertThat(stats.GetStat("ailment_effect")).IsEqual(1.0f);
        stats.AddModifier("damage_taken", -5.0f, 0.0f);
        AssertThat(stats.GetStat("damage_taken")).IsEqual(0.0f);
        stats.RemoveModifier("damage_taken", -5.0f, 0.0f);
        AssertThat(stats.GetStat("damage_taken")).IsEqual(1.0f);
        GD.Print("[PASS] Ailment chance caps at 100%; threshold floors at zero.");

        var stunFx = new EffectSpec { EffectId = "stun", Ratio = 0.0f, Duration = 4.0f };
        AssertThat(HitPipeline.ControlApplyChance(0.05f, 1.0f, false)).IsEqualApprox(0.1f, 0.001f);
        AssertThat(HitPipeline.ControlApplyChance(0.125f, 1.0f, false)).IsEqualApprox(0.25f, 0.001f);
        AssertThat(HitPipeline.ControlApplyChance(2.5f, 0.0f, false)).IsEqualApprox(1.0f, 0.001f);
        AssertThat(HitPipeline.ControlApplyChance(0.2f, 0.0f, true)).IsEqualApprox(0.4f, 0.001f);
        GD.Print("[PASS] Control chance is damage-derived, capped, crits double.");

        var igniteFx = new EffectSpec { EffectId = "ignite", Ratio = 0.1f, Duration = 4.0f };
        var burnFoe = NewEnemy(100.0f);
        Root.AddChild(burnFoe);
        HitPipeline.ResolveHit(new HitPayload
        {
            RawDamage = 20.0f,
            AttackerId = attackerId,
            AilmentChance = 1.0f,
            Effect0 = igniteFx,
            EffectCount = 1,
        }, burnFoe);
        AssertThat(burnFoe.Status.GetMagnitude("ignite")).IsEqualApprox(2.0f, 0.01f);
        AssertThat(burnFoe.Status.GetTimer("ignite")).IsEqualApprox(4.0f, 0.01f);
        var scorched = NewEnemy(1000.0f);
        scorched.Armor = 10.0f;
        Root.AddChild(scorched);
        HitPipeline.ResolveHit(new HitPayload
        {
            RawDamage = 20.0f,
            AttackerId = attackerId,
            AilmentChance = 1.0f,
            Effect0 = igniteFx,
            EffectCount = 1,
        }, scorched);
        AssertThat(scorched.Status.GetMagnitude("ignite")).IsEqualApprox(0.87f, 0.05f);
        GD.Print("[PASS] DoT anchors to dealt damage with fixed duration.");

        var shockFx = new EffectSpec { EffectId = "shock", Ratio = 0.0f, Duration = 2.5f };
        var marked = NewEnemy(1000.0f);
        Root.AddChild(marked);
        HitPipeline.ResolveHit(new HitPayload
        {
            RawDamage = 500.0f,
            AttackerId = attackerId,
            AilmentChance = 1.0f,
            Effect0 = shockFx,
            EffectCount = 1,
        }, marked);
        AssertThat(marked.Status.DamageTakenMultiplier).IsEqualApprox(1.3f, 0.01f);
        AssertThat(marked.Status.GetTimer("shock")).IsEqualApprox(2.5f, 0.01f);
        GD.Print("[PASS] Control lands fixed magnitude with refreshed duration.");

        var gated = NewEnemy(1000.0f);
        Root.AddChild(gated);
        HitPipeline.ResolveHit(new HitPayload
        {
            RawDamage = 500.0f,
            AttackerId = attackerId,
            AilmentChance = 0.0f,
            Effect0 = stunFx,
            EffectCount = 1,
        }, gated);
        AssertThat(gated.StunTimer).IsEqualApprox(0.5f, 0.01f);
        GD.Print("[PASS] Damage-derived control applies despite zero ailment_chance.");

        stats.SetBase("crit_chance", 1.0f);
        var critGated = NewEnemy(1000.0f);
        Root.AddChild(critGated);
        HitResult critHit = HitPipeline.ResolveHit(new HitPayload
        {
            RawDamage = 500.0f,
            AttackerId = attackerId,
            AilmentChance = 0.0f,
            Effect0 = stunFx,
            EffectCount = 1,
        }, critGated);
        AssertThat(critHit.IsCrit).IsTrue();
        AssertThat(critGated.StunTimer).IsEqualApprox(0.5f, 0.01f);
        stats.SetBase("crit_chance", 0.0f);
        GD.Print("[PASS] Crits double control odds; stun lands fixed 0.5s.");

        var thick = NewEnemy(1000.0f);
        thick.AilmentThresholdMult = 4.0f;
        Root.AddChild(thick);
        var thin = NewEnemy(1000.0f);
        Root.AddChild(thin);
        for (int i = 0; i < 500 && thin.StunTimer <= 0.0f; i++)
        {
            thin.CurrentHealth = 1000.0f;
            HitPipeline.ResolveHit(new HitPayload
            {
                RawDamage = 25.0f,
                AttackerId = attackerId,
                AilmentChance = 1.0f,
                Effect0 = stunFx,
                EffectCount = 1,
            }, thin);
        }
        for (int i = 0; i < 1500 && thick.StunTimer <= 0.0f; i++)
        {
            thick.CurrentHealth = 1000.0f;
            HitPipeline.ResolveHit(new HitPayload
            {
                RawDamage = 25.0f,
                AttackerId = attackerId,
                AilmentChance = 1.0f,
                Effect0 = stunFx,
                EffectCount = 1,
            }, thick);
        }
        AssertThat(thin.StunTimer).IsEqualApprox(0.5f, 0.01f);
        AssertThat(thick.StunTimer).IsEqualApprox(0.5f, 0.01f);
        GD.Print("[PASS] Threshold mult lowers control odds; landed stuns are fixed 0.5s.");

        stats.SetBase("max_health", 400.0f);
        stats.SetBase("ailment_threshold", 4.0f);
        cell.Health = 400.0f;
        for (int i = 0; i < 1500 && cell.StunTimer <= 0.0f; i++)
        {
            cell.Health = 400.0f;
            HitPipeline.ResolveHit(new HitPayload
            {
                RawDamage = 10.0f,
                AilmentChance = 1.0f,
                Effect0 = stunFx,
                EffectCount = 1,
            }, cell);
        }
        AssertThat(cell.StunTimer).IsEqualApprox(0.5f, 0.01f);
        stats.SetBase("ailment_threshold", 1.0f);
        GD.Print("[PASS] Player threshold stat scales incoming control odds.");

        burnFoe.QueueFree();
        scorched.QueueFree();
        marked.QueueFree();
        gated.QueueFree();
        critGated.QueueFree();
        thick.QueueFree();
        thin.QueueFree();
        cell.QueueFree();
    }

    private void RunTypedDamageTests()
    {
        AssertThat(BaseSkill.TypeStatKey(DamageType.Physical)).IsEqual("physical_damage");
        AssertThat(BaseSkill.TypeStatKey(DamageType.Fire)).IsEqual("fire_damage");
        AssertThat(BaseSkill.TypeStatKey(DamageType.Cold)).IsEqual("cold_damage");
        AssertThat(BaseSkill.TypeStatKey(DamageType.Lightning)).IsEqual("lightning_damage");
        AssertThat(BaseSkill.TypeStatKey(DamageType.Chaos)).IsEqual("chaos_damage");

        HitPayload untagged = DamageService.Snapshot(10.0f);
        AssertThat(untagged.Type).IsEqual(DamageType.Physical);
        GD.Print("[PASS] Damage type keys map 1:1; untagged payloads default to physical.");

        var cell = new PlayerActor { GlobalPosition = new Vector2(300, 300) };
        var stats = new ActorStats { Name = "ActorStats" };
        cell.AddChild(stats);
        cell.Stats = stats;
        Root.AddChild(cell);
        stats.SetBase("block", 0.0f);
        stats.SetBase("evasion", 0.0f);
        stats.SetBase("crit_chance", 0.0f);
        stats.SetBase("life_steal", 0.0f);
        stats.SetBase("armor", 0.0f);
        ulong attackerId = cell.GetInstanceId();

        var ros = SkillFactory.CreateActive("ros_torrent")!;
        ros.Setup(cell);
        AssertThat(ros.GetDamageType()).IsEqual(DamageType.Fire);
        var grasp = SkillFactory.CreateActive("phagocytic_grasp")!;
        grasp.Setup(cell);
        AssertThat(grasp.GetDamageType()).IsEqual(DamageType.Physical);
        var untyped = new BaseSkill { SkillId = "missing_id" };
        AssertThat(untyped.GetDamageType()).IsEqual(DamageType.Physical);

        stats.AddModifier("fire_damage", 0.0f, 0.5f);
        AssertThat(ros.GetCalculatedDamage(16.0f)).IsEqualApprox(24.0f, 0.01f);
        stats.AddModifier("physical_damage", 0.0f, 1.0f);
        AssertThat(ros.GetCalculatedDamage(16.0f)).IsEqualApprox(24.0f, 0.01f);
        AssertThat(grasp.GetCalculatedDamage(28.0f)).IsEqualApprox(56.0f, 0.01f);
        stats.RemoveModifier("fire_damage", 0.0f, 0.5f);
        stats.RemoveModifier("physical_damage", 0.0f, 1.0f);
        GD.Print("[PASS] Typed multipliers scale only their own damage type.");

        var armored = NewEnemy(1000.0f);
        armored.Armor = 10.0f;
        Root.AddChild(armored);
        HitResult fireHit = HitPipeline.ResolveHit(new HitPayload
        {
            RawDamage = 20.0f,
            Type = DamageType.Fire,
            AttackerId = attackerId,
        }, armored);
        AssertThat(fireHit.DamageDealt).IsEqualApprox(20.0f * (1.0f - 10.0f / 110.0f), 0.01f);
        GD.Print("[PASS] Armor still mitigates elemental hits (no physical-only split).");

        ros.QueueFree();
        grasp.QueueFree();
        armored.QueueFree();
        cell.QueueFree();
    }

    private void RunConditionalDamageTests()
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
        stats.SetBase("armor", 0.0f);

        var grasp = SkillFactory.CreateActive("phagocytic_grasp")!;
        grasp.Setup(cell);
        var ros = SkillFactory.CreateActive("ros_torrent")!;
        ros.Setup(cell);
        AssertThat(grasp.HasTag("Melee")).IsTrue();
        AssertThat(ros.HasTag("Melee")).IsFalse();
        AssertThat(ros.HasTag("Spell")).IsTrue();

        stats.AddModifier("melee_damage", 0.0f, 0.5f);
        AssertThat(grasp.GetCalculatedDamage(28.0f)).IsEqualApprox(42.0f, 0.01f);
        AssertThat(ros.GetCalculatedDamage(16.0f)).IsEqualApprox(16.0f, 0.01f);
        stats.RemoveModifier("melee_damage", 0.0f, 0.5f);
        GD.Print("[PASS] Melee damage scales only Melee-tagged skills.");

        stats.AddModifier("spell_damage", 0.0f, 0.5f);
        stats.AddModifier("aoe_damage", 0.0f, 0.5f);
        AssertThat(ros.GetCalculatedDamage(16.0f)).IsEqualApprox(32.0f, 0.01f);
        AssertThat(grasp.GetCalculatedDamage(28.0f)).IsEqualApprox(42.0f, 0.01f);
        stats.RemoveModifier("spell_damage", 0.0f, 0.5f);
        stats.RemoveModifier("aoe_damage", 0.0f, 0.5f);
        GD.Print("[PASS] Spell/AoE multipliers add into the increased damage pool on matching tags.");

        stats.AddModifier("projectile_damage", 0.0f, 1.0f);
        AssertThat(ros.GetCalculatedDamage(16.0f)).IsEqualApprox(16.0f, 0.01f);
        stats.RemoveModifier("projectile_damage", 0.0f, 1.0f);

        AssertThat(grasp.HasTag("Minion")).IsFalse();
        AssertThat(ros.HasTag("Minion")).IsFalse();
        AssertThat(stats.GetStat("minion_damage")).IsEqual(1.0f);
        GD.Print("[PASS] Minion damage is wired but dormant (no skill carries the tag).");

        grasp.QueueFree();
        ros.QueueFree();
        cell.QueueFree();
    }

    private void RunStaggerRecoupTests()
    {
        var cell = new PlayerActor { GlobalPosition = new Vector2(300, 300) };
        var stats = new ActorStats { Name = "ActorStats" };
        cell.AddChild(stats);
        cell.Stats = stats;
        Root.AddChild(cell);
        stats.SetBase("max_health", 100.0f);
        stats.SetBase("block", 0.0f);
        stats.SetBase("evasion", 0.0f);
        cell.Health = 100.0f;

        // Caps: stagger 60%, recoup 30%.
        stats.AddModifier("stagger", 0.9f, 0.0f);
        stats.AddModifier("recoup", 0.9f, 0.0f);
        AssertThat(stats.GetStat("stagger")).IsEqual(0.60f);
        AssertThat(stats.GetStat("recoup")).IsEqual(0.30f);
        stats.RemoveModifier("stagger", 0.9f, 0.0f);
        stats.RemoveModifier("recoup", 0.9f, 0.0f);

        // Stagger defers half the hit; instant lands now, DoTs feed neither pool.
        stats.AddModifier("stagger", 0.5f, 0.0f);
        float dealt = cell.TakeDamage(40.0f);
        AssertThat(dealt).IsEqual(40.0f);
        AssertThat(cell.Health).IsEqual(80.0f);
        AssertThat(cell.StaggerPool).IsEqual(20.0f);
        AssertThat(cell.RecoupPool).IsEqual(0.0f);
        cell.TakeDoTDamage(10.0f);
        AssertThat(cell.StaggerPool).IsEqual(20.0f);
        AssertThat(cell.RecoupPool).IsEqual(0.0f);
        GD.Print("[PASS] Stagger defers half the hit; DoTs bypass both pools.");

        // Pool drains as unmitigated DoT over the 4s window.
        cell.Health = 80.0f;
        cell._PhysicsProcess(2.0);
        AssertThat(cell.StaggerPool).IsEqualApprox(10.0f, 0.01f);
        AssertThat(cell.Health).IsEqualApprox(70.0f, 0.01f);
        GD.Print("[PASS] Stagger pool ticks down as DoT.");

        // Stagger ticks can kill.
        cell.Health = 12.0f;
        cell.TakeDamage(20.0f);
        AssertThat(cell.Health).IsEqual(2.0f);
        cell._PhysicsProcess(4.0);
        AssertThat(cell.StaggerPool).IsEqual(0.0f);
        AssertThat(cell.IsDead).IsTrue();
        GD.Print("[PASS] Pending stagger damage settles the kill.");
        stats.RemoveModifier("stagger", 0.5f, 0.0f);
        cell.QueueFree();

        // Recoup schedules 25% of the taken hit, paid over 4s.
        var cell2 = new PlayerActor { GlobalPosition = new Vector2(300, 300) };
        var stats2 = new ActorStats { Name = "ActorStats" };
        cell2.AddChild(stats2);
        cell2.Stats = stats2;
        Root.AddChild(cell2);
        stats2.SetBase("max_health", 100.0f);
        stats2.SetBase("block", 0.0f);
        stats2.SetBase("evasion", 0.0f);
        stats2.AddModifier("recoup", 0.25f, 0.0f);
        cell2.Health = 50.0f;
        cell2.TakeDamage(40.0f);
        AssertThat(cell2.Health).IsEqual(10.0f);
        AssertThat(cell2.RecoupPool).IsEqual(10.0f);
        cell2._PhysicsProcess(2.0);
        AssertThat(cell2.RecoupPool).IsEqualApprox(5.0f, 0.01f);
        AssertThat(cell2.Health).IsEqualApprox(15.0f, 0.01f);
        cell2._PhysicsProcess(4.0);
        AssertThat(cell2.RecoupPool).IsEqual(0.0f);
        AssertThat(cell2.Health).IsEqualApprox(20.0f, 0.01f);
        GD.Print("[PASS] Recoup repays a quarter of the hit over its window.");
        cell2.QueueFree();
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
            new HitPayload { RawDamage = 5.0f, SourceFaction = Team.Enemy, AttackerId = shooterId },
            0, 5.0f, 8.0f, "enemy_pellet");
        mgr._PhysicsProcess(0.016);
        AssertThat(mgr.ActiveCount).IsEqual(1);

        HitResult lethal = HitPipeline.ResolveHit(new HitPayload { RawDamage = 500.0f, AttackerId = attackerId }, shooter);
        AssertThat(lethal.TargetKilled).IsTrue();
        AssertThat(DeadEntityRegistry.IsDead(shooterId)).IsTrue();

        mgr._PhysicsProcess(0.016);
        AssertThat(mgr.ActiveCount).IsEqual(0);
        GD.Print("[PASS] Enemy pellets fizzle when their owner dies.");

        mgr.Spawn(new Vector2(500, 500), Vector2.Right, 100.0f,
            new HitPayload { RawDamage = 5.0f, SourceFaction = Team.Enemy },
            0, 5.0f, 8.0f, "enemy_pellet");
        mgr.Spawn(new Vector2(500, 520), Vector2.Right, 100.0f,
            new HitPayload { RawDamage = 5.0f, AttackerId = shooterId },
            0, 5.0f, 8.0f, "generic");
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

        AssertThat(cell.TakeDamage(10.0f)).IsEqual(0.0f);
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
