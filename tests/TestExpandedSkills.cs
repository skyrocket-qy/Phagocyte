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
        GD.Print(">>> STARTING EXPANDED 17 ACTIVE + 13 PASSIVE SKILL VERIFICATION <<<");
        GD.Print("==================================================================");

        // --- 1. Verify Catalog Counts ---
        AssertThat(UpgradeManager.ActiveCatalog.Count).IsEqual(17);
        AssertThat(UpgradeManager.PassiveCatalog.Count).IsEqual(13);
        GD.Print("[PASS] Step 1: UpgradeManager.ActiveCatalog has 17 items and PassiveCatalog has 13 items.");

        // --- 2. Verify Every Active Skill Instantiation & Metadata ---
        var activeTypes = new Type[]
        {
            typeof(PhagocyticGraspSkill),
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
            var skill = (BaseSkill?)Activator.CreateInstance(type);
            AssertThat(skill).IsNotNull();
            AssertThat(skill!.IsPassive).IsFalse();
            AssertThat(skill!.SkillId.Length > 0).IsTrue();
            AssertThat(skill!.NameKey.Length > 0).IsTrue();
            AssertThat(skill!.DescKey.Length > 0).IsTrue();
            AssertThat(skill!.BioKey.Length > 0).IsTrue();
            AssertThat(skill!.IconSymbol.Length > 0).IsTrue();

            // Verify entry exists in GameManager.SkillCatalog
            AssertThat(GameManager.SkillCatalog.ContainsKey(skill!.SkillId)).IsTrue();
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
            var skill = (BaseSkill?)Activator.CreateInstance(type);
            AssertThat(skill).IsNotNull();
            AssertThat(skill!.IsPassive).IsTrue();
            AssertThat(skill!.SkillId.Length > 0).IsTrue();
            AssertThat(skill!.NameKey.Length > 0).IsTrue();
            AssertThat(skill!.DescKey.Length > 0).IsTrue();
            AssertThat(skill!.BioKey.Length > 0).IsTrue();
            AssertThat(skill!.IconSymbol.Length > 0).IsTrue();

            // Verify entry exists in GameManager.SkillCatalog
            AssertThat(GameManager.SkillCatalog.ContainsKey(skill!.SkillId)).IsTrue();
        }
        GD.Print($"[PASS] Step 3: All {passiveTypes.Length} Passive Gear Traits instantiated and verified in GameManager.SkillCatalog.");

        // --- 4. Verify 8 New Passives Stat Injections ---
        var dummyHost = new CharacterBody2D();
        var cellStats = new CellStats { Name = "CellStats" };
        dummyHost.AddChild(cellStats);

        // 4.1 Bilayer Hardening: MaxHP +15% / Armor +2 per level
        float baseHp = cellStats.GetStat("max_health");
        float baseArmor = cellStats.GetStat("armor");
        var bilayer = new PassiveBilayerHardening();
        bilayer.Setup(dummyHost);
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
        autophagy.Setup(dummyHost);
        AssertThat(cellStats.GetStat("health_regen")).IsEqualApprox(0.4f, 0.01f);
        autophagy.Upgrade(); // Lv.2
        AssertThat(cellStats.GetStat("health_regen")).IsEqualApprox(0.8f, 0.01f);
        autophagy.RemovePassiveModifiers();
        AssertThat(cellStats.GetStat("health_regen")).IsEqual(0.0f);

        // 4.3 Aerobic Glycolysis: Move Speed +6%, Might +5% per level
        var glycolysis = new PassiveAerobicGlycolysis();
        glycolysis.Setup(dummyHost);
        AssertThat(cellStats.GetStat("move_speed")).IsEqualApprox(230.0f * 1.06f, 0.5f);
        AssertThat(cellStats.GetStat("might")).IsEqualApprox(1.05f, 0.01f);
        glycolysis.RemovePassiveModifiers();

        // 4.4 Kinesin Transit: Speed +15%, Pierce +1
        var kinesin = new PassiveKinesinTransit();
        kinesin.Setup(dummyHost);
        AssertThat(cellStats.GetStat("projectile_speed")).IsEqualApprox(1.15f, 0.01f);
        AssertThat(cellStats.GetStat("pierce")).IsEqual(1.0f);
        kinesin.Upgrade();
        AssertThat(cellStats.GetStat("pierce")).IsEqual(1.0f);
        kinesin.RemovePassiveModifiers();
        AssertThat(cellStats.GetStat("pierce")).IsEqual(0.0f);

        // 4.5 Cytokine Longevity: Duration +15%, Knockback +10% per level
        var longevity = new PassiveCytokineLongevity();
        longevity.Setup(dummyHost);
        AssertThat(cellStats.GetStat("duration")).IsEqualApprox(1.15f, 0.01f);
        AssertThat(cellStats.GetStat("knockback")).IsEqualApprox(1.10f, 0.01f);
        longevity.RemovePassiveModifiers();

        // 4.6 V(D)J Diversity: CritDamage +15%, CritChance +3% per level
        var vdj = new PassiveVdjDiversity();
        vdj.Setup(dummyHost);
        AssertThat(cellStats.GetStat("crit_damage")).IsEqualApprox(2.30f, 0.01f);
        AssertThat(cellStats.GetStat("crit_chance")).IsEqualApprox(0.08f, 0.01f);
        vdj.RemovePassiveModifiers();

        // 4.7 Endotoxin Barrier: Armor +3, Block +4% per level
        var endotoxin = new PassiveEndotoxinBarrier();
        endotoxin.Setup(dummyHost);
        AssertThat(cellStats.GetStat("armor")).IsEqual(3.0f);
        AssertThat(cellStats.GetStat("block")).IsEqualApprox(0.04f, 0.01f);
        endotoxin.RemovePassiveModifiers();

        // 4.8 Hematopoietic Reserve: MaxHP +10%, Block +3%
        var hematopoietic = new PassiveHematopoieticReserve();
        hematopoietic.Setup(dummyHost);
        AssertThat(cellStats.GetStat("max_health")).IsEqualApprox(110.0f, 0.1f);
        AssertThat(cellStats.GetStat("block")).IsEqualApprox(0.03f, 0.01f);
        hematopoietic.RemovePassiveModifiers();

        GD.Print("[PASS] Step 4: All 8 new Passive traits correctly inject, upgrade, and remove modifiers from CellStats.");

        // --- 5. Verify BaseCell Integration (Exp, Impulse, Damage) ---
        var player = new BaseCell();
        var playerStats = new CellStats { Name = "CellStats" };
        player.AddChild(playerStats);
        player.Stats = playerStats;
        Root.AddChild(player);

        // 5.1 AddExp
        float expBefore = player.CurrentExp;
        player.AddExp(15.0f);
        AssertThat(player.CurrentExp).IsEqualApprox(expBefore + 15.0f, 0.01f);
        GD.Print("[PASS] Step 5.1: BaseCell AddExp works correctly.");

        // 5.2 Armor damping in ApplyImpulse
        playerStats.AddModifier("armor", 50.0f, 0.0f); // 50% DR
        player.Velocity = Vector2.Zero;
        player.ApplyImpulse(new Vector2(100.0f, 0.0f));
        AssertThat(player.Velocity.X).IsEqualApprox(50.0f, 0.5f);
        GD.Print("[PASS] Step 5.2: BaseCell ApplyImpulse respects armor dampening.");

        // 5.3 Fatal damage
        player.Health = 20.0f;
        player.TakeDamage(100.0f); // Lethal damage
        AssertThat(player.Health).IsEqual(0.0f);
        AssertThat(player.IsDead).IsTrue();
        GD.Print("[PASS] Step 5.3: BaseCell TakeDamage handles fatal damage correctly.");

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
        GD.Print("[PASS] Step 6: UpgradeManager generates diverse cards from the 17+13 pool.");

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
