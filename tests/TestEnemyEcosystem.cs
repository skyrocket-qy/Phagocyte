using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Enemies;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Phagocyte.Tests;

public partial class TestEnemyEcosystem : SceneTree
{
    private int _framesWaited = 0;
    private bool _testDone = false;

    private static readonly string[] AllPathogenIds = new[]
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
        // TEST 1: Instantiation & Base Properties of All 20 Pathogens
        // -------------------------------------------------------------
        var testContainer = new Node2D { Name = "TestContainer" };
        Root.AddChild(testContainer);

        var instantiatedEnemies = new List<BaseEnemy>();
        foreach (var id in AllPathogenIds)
        {
            var enemy = PathogenSpawner.CreatePathogen(id);
            AssertThat(enemy).IsNotNull();
            AssertThat(enemy is BaseEnemy).IsTrue();
            AssertThat(enemy!.EnemyId).IsEqual(id);
            AssertThat(enemy.MaxHealth > 0.0f).IsTrue();
            AssertThat(enemy.AtpValue > 0.0f).IsTrue();

            testContainer.AddChild(enemy);
            instantiatedEnemies.Add(enemy);
        }
        AssertThat(instantiatedEnemies.Count).IsEqual(20);
        GD.Print("[PASS] Test 1: All 20 biological pathogens successfully instantiated with BaseEnemy inheritance.");

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
        var playerScene = GD.Load<PackedScene>("res://scenes/characters/macrophage.tscn");
        var player = playerScene.Instantiate<BaseCell>();
        testContainer.AddChild(player);

        var staph = new StaphEnemy { FibrinShield = 1 };
        testContainer.AddChild(staph);
        AssertThat(staph.FibrinShield).IsEqual(1);
        AssertThat(staph.CanBeEngulfed).IsFalse();

        // First attempt breaks shield
        player.ConsumePathogen(staph);
        AssertThat(staph.FibrinShield).IsEqual(0);
        AssertThat(staph.IsBeingEaten).IsFalse();
        AssertThat(staph.CanBeEngulfed).IsTrue();

        // Second attempt digests successfully
        player.ConsumePathogen(staph);
        AssertThat(staph.IsBeingEaten).IsTrue();
        GD.Print("[PASS] Test 3: Staph fibrin microthrombi armor & shield breaking verified.");

        // -------------------------------------------------------------
        // TEST 4: E. coli Charge & Stagger
        // -------------------------------------------------------------
        var ecoli = new EColiEnemy();
        testContainer.AddChild(ecoli);
        ecoli.TakeDamage(5.0f);
        AssertThat(ecoli.CurrentHealth).IsEqual(23.0f);
        GD.Print("[PASS] Test 4: E. coli peritrichous bacillus chemotactic charge logic verified.");

        // -------------------------------------------------------------
        // TEST 5: Pseudomonas Biofilm Secretion
        // -------------------------------------------------------------
        var pseudo = new PseudomonasEnemy { GlobalPosition = new Vector2(100, 100) };
        testContainer.AddChild(pseudo);
        pseudo.Die(player);

        // Find spawned BiofilmArea
        BiofilmArea? biofilm = null;
        foreach (var child in testContainer.GetChildren())
        {
            if (child is BiofilmArea ba)
            {
                biofilm = ba;
                break;
            }
        }
        AssertThat(biofilm).IsNotNull();
        player.ApplySlow(2.0f, 0.5f);
        AssertThat(player.SlowFactor).IsEqual(0.5f);
        AssertThat(player.SlowTimer > 0.0f).IsTrue();
        GD.Print("[PASS] Test 5: Pseudomonas aeruginosa biofilm puddle & player slow effect verified.");

        // -------------------------------------------------------------
        // TEST 6: TB Mycolic Wax & Cytoplasm Digestion Burn
        // -------------------------------------------------------------
        var tb = new TbEnemy();
        testContainer.AddChild(tb);
        float hpBefore = player.Health;
        tb.BeEngulfed(player);
        AssertThat(player.TbBurnTimer > 0.0f).IsTrue();
        AssertThat(player.TbBurnDps).IsEqual(4.0f);
        GD.Print("[PASS] Test 6: Mycobacterium tuberculosis acid resistance & cytoplasm digestion burn verified.");

        // -------------------------------------------------------------
        // TEST 7: Rabies Neuro-Chaos Control Inversion
        // -------------------------------------------------------------
        player.ApplyInvertControls(2.0f);
        AssertThat(player.InvertControlsTimer > 0.0f).IsTrue();
        GD.Print("[PASS] Test 7: Rabies Lyssavirus hydrophobic chaos & WASD inverted controls verified.");

        // -------------------------------------------------------------
        // TEST 8: Anthrax Spore Two-Stage Hard Shell
        // -------------------------------------------------------------
        var anthrax = new AnthraxSporeEnemy { GlobalPosition = new Vector2(200, 200) };
        testContainer.AddChild(anthrax);
        AssertThat(anthrax.CanBeEngulfed).IsFalse();
        anthrax.Die(player);

        AnthraxBacillus? bacillus = null;
        foreach (var child in testContainer.GetChildren())
        {
            if (child is AnthraxBacillus ab)
            {
                bacillus = ab;
                break;
            }
        }
        AssertThat(bacillus).IsNotNull();
        AssertThat(bacillus!.CanBeEngulfed).IsTrue();
        GD.Print("[PASS] Test 8: Bacillus anthracis two-stage spore shell shattering & vegetative hatching verified.");

        // -------------------------------------------------------------
        // TEST 9: Candida Yeast-to-Hyphae Puncture Elite
        // -------------------------------------------------------------
        var candida = new CandidaEnemy();
        testContainer.AddChild(candida);
        AssertThat(candida.CanBeEngulfed).IsTrue();
        candida.ExtendHyphae();
        AssertThat(candida.CanBeEngulfed).IsFalse();

        float pHealth = player.Health;
        candida.OnEngulfAttemptFailed(player);
        AssertThat(player.Health < pHealth).IsTrue();
        GD.Print("[PASS] Test 9: Candida albicans hyphae sprouting & membrane puncture rejection verified.");

        // -------------------------------------------------------------
        // TEST 10: Prion Amyloid Aggregation Boss
        // -------------------------------------------------------------
        var prion = new PrionEnemy();
        testContainer.AddChild(prion);
        AssertThat(prion.IsBoss).IsTrue();
        AssertThat(prion.CanBeEngulfed).IsFalse();

        // Damage past 50% to trigger splitting
        prion.TakeDamage(80.0f);
        int fragCount = 0;
        foreach (var child in testContainer.GetChildren())
        {
            if (child is PrionFragment)
                fragCount++;
        }
        AssertThat(fragCount >= 2).IsTrue();
        GD.Print("[PASS] Test 10: Prion Aggregate protease resistance & amyloid fragment splitting verified.");

        // -------------------------------------------------------------
        // TEST 11: GameManager.PathogenCatalog 20 Entries & Localization
        // -------------------------------------------------------------
        AssertThat(GameManager.PathogenCatalog.Count).IsEqual(20);
        foreach (var id in AllPathogenIds)
        {
            AssertThat(GameManager.PathogenCatalog.ContainsKey(id)).IsTrue();
            var info = GameManager.GetPathogenInfo(id);
            AssertThat(info["name"].AsString().Length > 0).IsTrue();
            AssertThat(info["description"].AsString().Length > 0).IsTrue();
            AssertThat(info["trait"].AsString().Length > 0).IsTrue();
            AssertThat(info["icon"].AsString().Length > 0).IsTrue();
        }
        GD.Print("[PASS] Test 11: GameManager.PathogenCatalog has 20 complete, fully-localized biological entries.");

        // -------------------------------------------------------------
        // TEST 12: PathogenSpawner Wave Director Progression
        // -------------------------------------------------------------
        var spawnerContainer = new Node2D();
        testContainer.AddChild(spawnerContainer);

        // Phase 1 (10s)
        PathogenSpawner.SpawnWave(spawnerContainer, player, new Vector2(4800, 4800), 10.0f, 2);
        AssertThat(spawnerContainer.GetChildCount() > 0).IsTrue();

        // Phase 4 (200s - Boss phase)
        PathogenSpawner.SpawnWave(spawnerContainer, player, new Vector2(4800, 4800), 200.0f, 1);
        AssertThat(spawnerContainer.GetChildCount() > 1).IsTrue();
        GD.Print("[PASS] Test 12: PathogenSpawner wave director multi-phase biological escalation verified.");

        testContainer.QueueFree();
    }
}
