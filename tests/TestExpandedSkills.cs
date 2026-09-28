using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using System.Collections.Generic;
using Game.Core;
using Game.Player;
using Game.Skills;

namespace Game.Tests;

[TestSuite]
public partial class TestExpandedSkills : SceneTree
{
    private int _frameCount = 0;
    private bool _testDone = false;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING EXPANDED 18 ACTIVE + 13 PASSIVE SKILL VERIFICATION <<<");
        GD.Print("==================================================================");

        // --- 1. Verify Catalog Counts ---
        AssertThat(UpgradeManager.ActiveCatalog.Count).IsEqual(18);
        AssertThat(UpgradeManager.PassiveCatalog.Count).IsEqual(13);
        GD.Print("[PASS] Step 1: UpgradeManager.ActiveCatalog has 18 items and PassiveCatalog has 13 items.");

        // --- 2. Verify Every Active Skill Instantiation & Metadata ---
        var activeIds = new string[]
        {
            "phagocytic_grasp", "lysosomal_overload", "ros_torrent", "perforin_lance",
            "complement_cascade", "antibody_salvo", "pseudopod_lunge", "nitric_oxide_halo",
            "nuclease_blades", "granzyme_detonation", "interferon_wave", "lysozyme_ricochet",
            "phagolysosome_vent", "pro_inflammatory_arc", "exosome_singularity", "defensin_barbs",
            "mhc_tracer_beam", "histamine_surge"
        };

        foreach (var id in activeIds)
        {
            var skill = SkillFactory.CreateActive(id);
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
        GD.Print($"[PASS] Step 2: All {activeIds.Length} Active Weapons instantiated and verified in GameManager.SkillCatalog.");

        // --- 3. Verify Every Passive Skill Instantiation & Metadata ---
        var passiveIds = new string[]
        {
            "actin", "lysosome", "mitochondria", "opsonin", "chemokine", "bilayer",
            "autophagy", "glycolysis", "kinesin", "longevity", "vdj", "endotoxin",
            "hematopoietic"
        };

        foreach (var id in passiveIds)
        {
            var skill = SkillFactory.CreatePassive(id);
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
        GD.Print($"[PASS] Step 3: All {passiveIds.Length} Passive Equipment Traits instantiated and verified in GameManager.SkillCatalog.");

        // --- 4. Verify 8 New Passives Stat Injections ---
        var dummyHost = new CharacterBody2D();
        var cellStats = new ActorStats { Name = "ActorStats" };
        dummyHost.AddChild(cellStats);

        // 4.1 Bilayer Hardening: MaxHP +15% / Armor +2 per level
        float baseHp = cellStats.GetStat("max_health");
        float baseArmor = cellStats.GetStat("armor");
        var bilayer = SkillFactory.CreatePassive("bilayer")!;
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
        var autophagy = SkillFactory.CreatePassive("autophagy")!;
        autophagy.Setup(dummyHost);
        AssertThat(cellStats.GetStat("health_regen")).IsEqualApprox(0.4f, 0.01f);
        autophagy.Upgrade(); // Lv.2
        AssertThat(cellStats.GetStat("health_regen")).IsEqualApprox(0.8f, 0.01f);
        autophagy.RemovePassiveModifiers();
        AssertThat(cellStats.GetStat("health_regen")).IsEqual(0.0f);

        // 4.3 Aerobic Glycolysis: Move Speed +6%, Might +5% per level
        var glycolysis = SkillFactory.CreatePassive("glycolysis")!;
        glycolysis.Setup(dummyHost);
        AssertThat(cellStats.GetStat("move_speed")).IsEqualApprox(230.0f * 1.06f, 0.5f);
        AssertThat(cellStats.GetStat("might")).IsEqualApprox(1.05f, 0.01f);
        glycolysis.RemovePassiveModifiers();

        // 4.4 Kinesin Transit: Speed +15%, Pierce +1
        var kinesin = SkillFactory.CreatePassive("kinesin")!;
        kinesin.Setup(dummyHost);
        AssertThat(cellStats.GetStat("projectile_speed")).IsEqualApprox(1.15f, 0.01f);
        AssertThat(cellStats.GetStat("pierce")).IsEqual(1.0f);
        kinesin.Upgrade();
        AssertThat(cellStats.GetStat("pierce")).IsEqual(1.0f);
        kinesin.RemovePassiveModifiers();
        AssertThat(cellStats.GetStat("pierce")).IsEqual(0.0f);

        // 4.5 Cytokine Longevity: Duration +15%, Knockback +10% per level
        var longevity = SkillFactory.CreatePassive("longevity")!;
        longevity.Setup(dummyHost);
        AssertThat(cellStats.GetStat("duration")).IsEqualApprox(1.15f, 0.01f);
        AssertThat(cellStats.GetStat("knockback")).IsEqualApprox(1.10f, 0.01f);
        longevity.RemovePassiveModifiers();

        // 4.6 V(D)J Diversity: CritDamage +15%, CritChance +3% per level
        var vdj = SkillFactory.CreatePassive("vdj")!;
        vdj.Setup(dummyHost);
        AssertThat(cellStats.GetStat("crit_damage")).IsEqualApprox(2.30f, 0.01f);
        AssertThat(cellStats.GetStat("crit_chance")).IsEqualApprox(0.08f, 0.01f);
        vdj.RemovePassiveModifiers();

        // 4.7 Endotoxin Barrier: Armor +3, Block +4% per level
        var endotoxin = SkillFactory.CreatePassive("endotoxin")!;
        endotoxin.Setup(dummyHost);
        AssertThat(cellStats.GetStat("armor")).IsEqual(3.0f);
        AssertThat(cellStats.GetStat("block")).IsEqualApprox(0.04f, 0.01f);
        endotoxin.RemovePassiveModifiers();

        // 4.8 Hematopoietic Reserve: MaxHP +10%, Block +3%
        var hematopoietic = SkillFactory.CreatePassive("hematopoietic")!;
        hematopoietic.Setup(dummyHost);
        AssertThat(cellStats.GetStat("max_health")).IsEqualApprox(110.0f, 0.1f);
        AssertThat(cellStats.GetStat("block")).IsEqualApprox(0.03f, 0.01f);
        hematopoietic.RemovePassiveModifiers();

        GD.Print("[PASS] Step 4: All 8 new Passive traits correctly inject, upgrade, and remove modifiers from ActorStats.");

        // --- 5. Verify PlayerActor Integration (Exp, Impulse, Damage) ---
        var player = new PlayerActor();
        var playerStats = new ActorStats { Name = "ActorStats" };
        player.AddChild(playerStats);
        player.Stats = playerStats;
        Root.AddChild(player);

        // 5.1 AddExp
        float expBefore = player.CurrentExp;
        player.AddExp(15.0f);
        AssertThat(player.CurrentExp).IsEqualApprox(expBefore + 15.0f, 0.01f);
        GD.Print("[PASS] Step 5.1: PlayerActor AddExp works correctly.");

        // 5.2 Armor damping in ApplyImpulse
        playerStats.AddModifier("armor", 50.0f, 0.0f); // 50% DR
        player.Velocity = Vector2.Zero;
        player.ApplyImpulse(new Vector2(100.0f, 0.0f));
        AssertThat(player.Velocity.X).IsEqualApprox(50.0f, 0.5f);
        GD.Print("[PASS] Step 5.2: PlayerActor ApplyImpulse respects armor dampening.");

        // 5.3 Fatal damage
        player.Health = 20.0f;
        player.TakeDamage(100.0f); // Lethal damage
        AssertThat(player.Health).IsEqual(0.0f);
        AssertThat(player.IsDead).IsTrue();
        GD.Print("[PASS] Step 5.3: PlayerActor TakeDamage handles fatal damage correctly.");

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
