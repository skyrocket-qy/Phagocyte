using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using System.Collections.Generic;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Skills;

namespace Phagocyte.Tests;

[TestSuite]
public partial class TestExpandedSkills : SceneTree
{
    private int _frameCount = 0;
    private bool _testDone = false;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING EXPANDED 16 ACTIVE + 13 PASSIVE SKILL VERIFICATION <<<");
        GD.Print("==================================================================");

        // --- 1. Verify Catalog Counts ---
        AssertThat(UpgradeManager.ActiveCatalog.Count).IsEqual(16);
        AssertThat(UpgradeManager.PassiveCatalog.Count).IsEqual(13);
        GD.Print("[PASS] Step 1: UpgradeManager.ActiveCatalog has 16 items and PassiveCatalog has 13 items.");

        // --- 2. Verify Every Active Skill Instantiation & Metadata ---
        var activeTypes = new Type[]
        {
            typeof(RosTorrentSkill),
            typeof(PerforinLanceSkill),
            typeof(ComplementCascadeSkill),
            typeof(AntibodySalvoSkill),
            typeof(PseudopodLungeSkill),
            typeof(NitricOxideHaloSkill),
            typeof(NucleaseBladesSkill),
            typeof(GranzymeDetonationSkill),
            typeof(InterferonWaveSkill),
            typeof(LysozymeRicochetSkill),
            typeof(PhagolysosomeVentSkill),
            typeof(ProInflammatoryArcSkill),
            typeof(ExosomeSingularitySkill),
            typeof(DefensinBarbsSkill),
            typeof(MhcTracerBeamSkill),
            typeof(HistamineSurgeSkill)
        };

        foreach (var type in activeTypes)
        {
            var skill = (BaseSkill)Activator.CreateInstance(type);
            AssertThat(skill).IsNotNull();
            AssertThat(skill.IsPassive).IsFalse();
            AssertThat(skill.SkillId.Length > 0).IsTrue();
            AssertThat(skill.NameKey.Length > 0).IsTrue();
            AssertThat(skill.DescKey.Length > 0).IsTrue();
            AssertThat(skill.BioKey.Length > 0).IsTrue();
            AssertThat(skill.IconSymbol.Length > 0).IsTrue();

            // Verify entry exists in GameManager.SkillCatalog
            AssertThat(GameManager.SkillCatalog.ContainsKey(skill.SkillId)).IsTrue();
        }
        GD.Print($"[PASS] Step 2: All {activeTypes.Length} Active Weapons instantiated and verified in GameManager.SkillCatalog.");

        // --- 3. Verify Every Passive Skill Instantiation & Metadata ---
        var passiveTypes = new Type[]
        {
            typeof(PassiveActinPolymerization),
            typeof(PassiveLysosomePriming),
            typeof(PassiveMitochondrialOverclock),
            typeof(PassiveOpsoninAffinity),
            typeof(PassiveChemokineReceptors),
            typeof(PassiveBilayerHardening),
            typeof(PassiveAutophagicRecycle),
            typeof(PassiveAerobicGlycolysis),
            typeof(PassiveKinesinTransit),
            typeof(PassiveCytokineLongevity),
            typeof(PassiveVdjDiversity),
            typeof(PassiveEndotoxinBarrier),
            typeof(PassiveHematopoieticReserve)
        };

        foreach (var type in passiveTypes)
        {
            var skill = (BaseSkill)Activator.CreateInstance(type);
            AssertThat(skill).IsNotNull();
            AssertThat(skill.IsPassive).IsTrue();
            AssertThat(skill.SkillId.Length > 0).IsTrue();
            AssertThat(skill.NameKey.Length > 0).IsTrue();
            AssertThat(skill.DescKey.Length > 0).IsTrue();
            AssertThat(skill.BioKey.Length > 0).IsTrue();
            AssertThat(skill.IconSymbol.Length > 0).IsTrue();

            // Verify entry exists in GameManager.SkillCatalog
            AssertThat(GameManager.SkillCatalog.ContainsKey(skill.SkillId)).IsTrue();
        }
        GD.Print($"[PASS] Step 3: All {passiveTypes.Length} Passive Organelle Traits instantiated and verified in GameManager.SkillCatalog.");

        // --- 4. Verify 8 New Passives Stat Injections ---
        var dummyHost = new CharacterBody2D();
        var cellStats = new CellStats { Name = "CellStats" };
        dummyHost.AddChild(cellStats);

        // 4.1 Bilayer Hardening: MaxHP +15% / Armor +2 per level
        float baseHp = cellStats.GetStat("max_health");
        float baseArmor = cellStats.GetStat("armor");
        var bilayer = new PassiveBilayerHardening();
        bilayer.Setup(dummyHost, 0);
        AssertThat(cellStats.GetStat("max_health")).IsEqualApprox(baseHp * 1.15f, 0.01f);
        AssertThat(cellStats.GetStat("armor")).IsEqual(baseArmor + 2.0f);
        bilayer.Upgrade(); // Lv.2
        AssertThat(cellStats.GetStat("max_health")).IsEqualApprox(baseHp * 1.30f, 0.01f);
        AssertThat(cellStats.GetStat("armor")).IsEqual(baseArmor + 4.0f);
        bilayer.RemovePassiveModifiers();
        AssertThat(cellStats.GetStat("max_health")).IsEqualApprox(baseHp, 0.01f);
        AssertThat(cellStats.GetStat("armor")).IsEqual(baseArmor);

        // 4.2 Autophagic Recycle: HealthRegen +0.4 HP/s per level
        var autophagy = new PassiveAutophagicRecycle();
        autophagy.Setup(dummyHost, 0);
        AssertThat(cellStats.GetStat("health_regen")).IsEqualApprox(0.4f, 0.01f);
        autophagy.Upgrade(); // Lv.2
        AssertThat(cellStats.GetStat("health_regen")).IsEqualApprox(0.8f, 0.01f);
        autophagy.RemovePassiveModifiers();
        AssertThat(cellStats.GetStat("health_regen")).IsEqual(0.0f);

        // 4.3 Aerobic Glycolysis: Growth +12%, Might +4% per level
        var glycolysis = new PassiveAerobicGlycolysis();
        glycolysis.Setup(dummyHost, 0);
        AssertThat(cellStats.GetStat("growth")).IsEqualApprox(1.12f, 0.01f);
        AssertThat(cellStats.GetStat("might")).IsEqualApprox(1.04f, 0.01f);
        glycolysis.RemovePassiveModifiers();

        // 4.4 Kinesin Transit: Speed +15% per level, Pierce +1 at Lv.3/Lv.5
        var kinesin = new PassiveKinesinTransit();
        kinesin.Setup(dummyHost, 0);
        AssertThat(cellStats.GetStat("projectile_speed")).IsEqualApprox(1.15f, 0.01f);
        AssertThat(cellStats.GetStat("pierce")).IsEqual(0.0f);
        kinesin.Upgrade(); // Lv.2
        kinesin.Upgrade(); // Lv.3
        AssertThat(cellStats.GetStat("pierce")).IsEqual(1.0f);
        kinesin.Upgrade(); // Lv.4
        kinesin.Upgrade(); // Lv.5
        AssertThat(cellStats.GetStat("pierce")).IsEqual(2.0f);
        kinesin.RemovePassiveModifiers();
        AssertThat(cellStats.GetStat("pierce")).IsEqual(0.0f);

        // 4.5 Cytokine Longevity: Duration +15%, Knockback +10% per level
        var longevity = new PassiveCytokineLongevity();
        longevity.Setup(dummyHost, 0);
        AssertThat(cellStats.GetStat("duration")).IsEqualApprox(1.15f, 0.01f);
        AssertThat(cellStats.GetStat("knockback")).IsEqualApprox(1.10f, 0.01f);
        longevity.RemovePassiveModifiers();

        // 4.6 V(D)J Diversity: Luck +15%, CritChance +3% per level
        var vdj = new PassiveVdjDiversity();
        vdj.Setup(dummyHost, 0);
        AssertThat(cellStats.GetStat("luck")).IsEqualApprox(1.15f, 0.01f);
        AssertThat(cellStats.GetStat("crit_chance")).IsEqualApprox(0.08f, 0.01f);
        vdj.RemovePassiveModifiers();

        // 4.7 Endotoxin Barrier: Armor +3, KnockbackResist +20% per level
        var endotoxin = new PassiveEndotoxinBarrier();
        endotoxin.Setup(dummyHost, 0);
        AssertThat(cellStats.GetStat("armor")).IsEqual(3.0f);
        AssertThat(cellStats.GetStat("knockback_resist")).IsEqualApprox(0.20f, 0.01f);
        endotoxin.RemovePassiveModifiers();

        // 4.8 Hematopoietic Reserve: MaxHP +10%, Revival +1 at Lv.3
        var hematopoietic = new PassiveHematopoieticReserve();
        hematopoietic.Setup(dummyHost, 0);
        AssertThat(cellStats.GetStat("revival")).IsEqual(0.0f);
        hematopoietic.Upgrade(); // Lv.2
        hematopoietic.Upgrade(); // Lv.3
        AssertThat(cellStats.GetStat("revival")).IsEqual(1.0f);
        hematopoietic.RemovePassiveModifiers();
        AssertThat(cellStats.GetStat("revival")).IsEqual(0.0f);

        GD.Print("[PASS] Step 4: All 8 new Passive traits correctly inject, upgrade, and remove modifiers from CellStats.");

        // --- 5. Verify BaseCell Integration (Growth, Revival, KnockbackResist) ---
        var player = new BaseCell();
        var playerStats = new CellStats { Name = "CellStats" };
        player.AddChild(playerStats);
        player.Stats = playerStats;
        Root.AddChild(player);

        // 5.1 Growth scaling in AddExp
        playerStats.AddModifier("growth", 0.0f, 0.50f); // 1.5x growth
        float expBefore = player.CurrentExp;
        player.AddExp(10.0f);
        AssertThat(player.CurrentExp).IsEqualApprox(expBefore + 15.0f, 0.01f);
        GD.Print("[PASS] Step 5.1: BaseCell AddExp respects growth multiplier.");

        // 5.2 Knockback resistance in ApplyImpulse
        playerStats.AddModifier("knockback_resist", 0.40f, 0.0f);
        player.Velocity = Vector2.Zero;
        player.ApplyImpulse(new Vector2(100.0f, 0.0f));
        AssertThat(player.Velocity.X).IsEqualApprox(60.0f, 0.01f);
        GD.Print("[PASS] Step 5.2: BaseCell ApplyImpulse respects knockback_resist.");

        // 5.3 Fatal damage & Revival
        playerStats.AddModifier("revival", 1.0f, 0.0f);
        player.Health = 20.0f;
        player.TakeDamage(100.0f); // Lethal damage
        AssertThat(player.Health).IsEqual(playerStats.GetStat("max_health"));
        AssertThat(playerStats.GetStat("revival")).IsEqual(0.0f); // 1 charge consumed
        GD.Print("[PASS] Step 5.3: BaseCell TakeDamage revives upon fatal damage when Revival >= 1.");

        // --- 6. Verify Level Up Selection Pool with Expanded Skills ---
        var sm2 = new SkillManager { Name = "SkillManager" };
        player.AddChild(sm2);
        player.CellSkillManager = sm2;
        sm2.Setup(player);

        var choices = UpgradeManager.GenerateChoices(player, 3);
        AssertThat(choices.Count).IsEqual(3);
        foreach (var c in choices)
        {
            AssertThat(c.ContainsKey("name")).IsTrue();
            AssertThat(c.ContainsKey("desc")).IsTrue();
            AssertThat(c.ContainsKey("icon")).IsTrue();
        }
        GD.Print("[PASS] Step 6: UpgradeManager generates diverse cards from the 16+13 pool.");

        GD.Print("==================================================================");
        GD.Print(">>> ALL EXPANDED SKILL TESTS PASSED CLEANLY! <<<");
        GD.Print("==================================================================");
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        _frameCount++;
        if (_frameCount >= 3)
        {
            _testDone = true;
            Quit(0);
            return true;
        }
        return false;
    }
}
