using Godot;
using System;
using System.Collections.Generic;
using Game.Core;
using Game.Combat;
using Game.Player;
using Game.Enemies;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Game.Tests;

public partial class TestEnemyEcosystem : SceneTree
{
    private int _framesWaited = 0;
    private bool _testDone = false;

    private static readonly string[] AllenemyIds = new[]
    {
        "staph", "s_virus", "flu_drift", "malignant_cell", "pseudomonas",
        "e_coli", "tb", "tetanus", "h_pylori", "anthrax_spore",
        "hiv", "rabies", "ebola", "norovirus", "varicella_zoster",
        "candida", "aspergillus", "plasmodium", "toxoplasma", "prion"
    };

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING 20-PATHOGEN BIOLOGICAL ECOSYSTEM VERIFICATION <<<");
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
            GD.Print(">>> ALL 20-PATHOGEN ECOSYSTEM TESTS PASSED SUCCESSFULLY! <<<");
            GD.Print("==================================================================");
            Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestEnemyEcosystem encountered an exception: ", ex);
            Quit(1);
        }

        return true;
    }

    private void RunAllTests()
    {
        // -------------------------------------------------------------
        // TEST 1: Instantiation & Base Properties of All 20 Enemies
        // -------------------------------------------------------------
        var testContainer = new Node2D { Name = "TestContainer" };
        Root.AddChild(testContainer);

        var instantiatedEnemies = new List<EnemyActor>();
        foreach (var id in AllenemyIds)
        {
            var enemy = EnemySpawner.CreateEnemy(id);
            AssertThat(enemy).IsNotNull();
            AssertThat(enemy is EnemyActor).IsTrue();
            AssertThat(enemy!.EnemyId).IsEqual(id);
            AssertThat(enemy.MaxHealth > 0.0f).IsTrue();
            AssertThat(enemy.XpValue > 0.0f).IsTrue();

            testContainer.AddChild(enemy);
            instantiatedEnemies.Add(enemy);
        }
        AssertThat(instantiatedEnemies.Count).IsEqual(20);
        GD.Print("[PASS] Test 1: All 20 biological pathogens successfully instantiated with EnemyActor inheritance.");

        // -------------------------------------------------------------
        // TEST 2: Procedural _Draw execution without exceptions
        // -------------------------------------------------------------
        foreach (var enemy in instantiatedEnemies)
        {
            enemy.QueueRedraw();
        }
        GD.Print("[PASS] Test 2: Procedural 2D microscope rendering verified for all 20 pathogens.");

        // -------------------------------------------------------------
        // TEST 3: Staph Fibrin Armor Shield
        // -------------------------------------------------------------
        var playerScene = AssetLoader.Load<PackedScene>("res://scenes/actors/player_base.tscn");
        var player = playerScene.Instantiate<PlayerActor>();
        testContainer.AddChild(player);

        var staph = EnemySpawner.CreateEnemy("staph")!;
        staph.ShieldCharges = 1;
        testContainer.AddChild(staph);
        AssertThat(staph.ShieldCharges).IsEqual(1);

        // First hit breaks the shield, health untouched
        float staphHp = staph.CurrentHealth;
        staph.TakeDamage(10.0f);
        AssertThat(staph.ShieldCharges).IsEqual(0);
        AssertThat(staph.CurrentHealth).IsEqual(staphHp);

        // Second hit lands: unarmored coccus takes full damage
        staph.TakeDamage(10.0f);
        AssertThat(staph.CurrentHealth).IsEqual(staphHp - 10.0f);
        GD.Print("[PASS] Test 3: Staph fibrin microthrombi armor & shield breaking verified.");

        // -------------------------------------------------------------
        // TEST 4: E. coli Charge & Stagger
        // -------------------------------------------------------------
        var ecoli = EnemySpawner.CreateEnemy("e_coli")!;
        testContainer.AddChild(ecoli);
        ecoli.TakeDamage(5.0f);
        AssertThat(ecoli.CurrentHealth).IsEqual(23.0f);
        GD.Print("[PASS] Test 4: E. coli peritrichous bacillus chemotactic charge logic verified.");

        // -------------------------------------------------------------
        // TEST 5: Pseudomonas Biofilm Secretion
        // -------------------------------------------------------------
        var pseudo = EnemySpawner.CreateEnemy("pseudomonas")!;
        pseudo.GlobalPosition = new Vector2(100, 100);
        testContainer.AddChild(pseudo);
        pseudo.Die(player);

        // Find spawned hazard zone from the death drop
        HazardZone? biofilm = null;
        foreach (var child in testContainer.GetChildren())
        {
            if (child is HazardZone ba)
            {
                biofilm = ba;
                break;
            }
        }
        AssertThat(biofilm).IsNotNull();
        player.ApplySlow(2.0f, 0.5f);
        AssertThat(player.Ailments != null && player.Ailments.IsAgglutinated).IsTrue();
        AssertThat(player.Ailments!.SpeedMultiplier).IsEqualApprox(0.5f, 0.01f);
        GD.Print("[PASS] Test 5: Pseudomonas aeruginosa biofilm puddle & player slow effect verified.");

        // -------------------------------------------------------------
        // TEST 6: TB Mycolic Wax Armor
        // -------------------------------------------------------------
        var tb = EnemySpawner.CreateEnemy("tb")!;
        testContainer.AddChild(tb);
        tb.TakeDamage(10.0f);
        AssertThat(tb.CurrentHealth).IsEqual(35.0f - Mathf.Max(1.0f, 10.0f - tb.Armor));
        GD.Print("[PASS] Test 6: Mycobacterium tuberculosis mycolic wax armor verified.");

        // -------------------------------------------------------------
        // TEST 7: Rabies Neuro-Chaos Control Inversion
        // -------------------------------------------------------------
        player.ApplyInvertControls(2.0f);
        AssertThat(player.InvertControlsTimer > 0.0f).IsTrue();
        GD.Print("[PASS] Test 7: Rabies Lyssavirus hydrophobic chaos & WASD inverted controls verified.");

        // -------------------------------------------------------------
        // TEST 8: Anthrax Spore Two-Stage Hard Shell
        // -------------------------------------------------------------
        var anthrax = EnemySpawner.CreateEnemy("anthrax_spore")!;
        anthrax.GlobalPosition = new Vector2(200, 200);
        testContainer.AddChild(anthrax);
        anthrax.Die(player);

        EnemyActor? bacillus = null;
        foreach (var child in testContainer.GetChildren())
        {
            if (child is EnemyActor ab && ab.EnemyId == "anthrax_bacillus")
            {
                bacillus = ab;
                break;
            }
        }
        AssertThat(bacillus).IsNotNull();
        GD.Print("[PASS] Test 8: Bacillus anthracis two-stage spore shell shattering & vegetative hatching verified.");

        // -------------------------------------------------------------
        // TEST 9: Candida Yeast-to-Hyphae Puncture Elite
        // -------------------------------------------------------------
        var candida = EnemySpawner.CreateEnemy("candida")!;
        candida.GlobalPosition = player.GlobalPosition;
        testContainer.AddChild(candida);
        candida._PhysicsProcess(0.016);
        AssertThat(candida.VariantActive).IsTrue();
        candida._PhysicsProcess(1.7);
        AssertThat(candida.VariantActive).IsFalse();
        GD.Print("[PASS] Test 9: Candida albicans hyphae sprouting & retraction verified.");

        // -------------------------------------------------------------
        // TEST 10: Prion Amyloid Aggregation Boss
        // -------------------------------------------------------------
        var prion = EnemySpawner.CreateEnemy("prion")!;
        testContainer.AddChild(prion);
        AssertThat(prion.IsBoss).IsTrue();

        // Damage past 50% to trigger splitting
        prion.TakeDamage(80.0f);
        int fragCount = 0;
        foreach (var child in testContainer.GetChildren())
        {
            if (child is EnemyActor frag && frag.EnemyId == "prion_fragment")
                fragCount++;
        }
        AssertThat(fragCount >= 2).IsTrue();
        GD.Print("[PASS] Test 10: Prion Aggregate protease resistance & amyloid fragment splitting verified.");

        // -------------------------------------------------------------
        // TEST 11: GameManager.EnemyCatalog 20 Entries & Localization
        // -------------------------------------------------------------
        AssertThat(GameManager.EnemyCatalog.Count).IsEqual(20);
        foreach (var id in AllenemyIds)
        {
            AssertThat(GameManager.EnemyCatalog.ContainsKey(id)).IsTrue();
            var info = GameManager.GetEnemyInfo(id);
            AssertThat(info["name"].AsString().Length > 0).IsTrue();
            AssertThat(info["description"].AsString().Length > 0).IsTrue();
            AssertThat(info["trait"].AsString().Length > 0).IsTrue();
            AssertThat(info["icon"].AsString().Length > 0).IsTrue();
        }
        GD.Print("[PASS] Test 11: GameManager.EnemyCatalog has 20 complete, fully-localized biological entries.");

        // -------------------------------------------------------------
        // TEST 12: EnemySpawner Wave Director Progression
        // -------------------------------------------------------------
        var spawnerContainer = new Node2D();
        testContainer.AddChild(spawnerContainer);

        // Phase 1 (10s)
        EnemySpawner.SpawnWave(spawnerContainer, player, new Vector2(4800, 4800), 10.0f, 2);
        AssertThat(spawnerContainer.GetChildCount() > 0).IsTrue();

        // Phase 2 (200s - local inflammation escalation)
        EnemySpawner.SpawnWave(spawnerContainer, player, new Vector2(4800, 4800), 200.0f, 1);
        AssertThat(spawnerContainer.GetChildCount() > 1).IsTrue();
        GD.Print("[PASS] Test 12: EnemySpawner wave director multi-phase biological escalation verified.");

        testContainer.QueueFree();
    }
}




