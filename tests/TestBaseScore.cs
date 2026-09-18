using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Enemies;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the fixed pathological BaseScore table from docs/record.md §4.1.
/// </summary>
[TestSuite]
public partial class TestBaseScore : SceneTree
{
    private static readonly string[] AllPathogenIds = new[]
    {
        "staph", "s_virus", "flu_drift", "malignant_cell", "pseudomonas",
        "e_coli", "tb", "tetanus", "h_pylori", "anthrax_spore",
        "hiv", "rabies", "ebola", "norovirus", "varicella_zoster",
        "candida", "aspergillus", "plasmodium", "toxoplasma", "prion"
    };

    private static readonly string[] MapIds = new[]
    {
        "acute_wound", "alveolar_space", "hepatic_sinusoid", "gastric_lumen", "blood_brain_barrier"
    };

    private int _frame = 0;
    private bool _done = false;

    public override bool _Process(double delta)
    {
        if (_done)
            return true;

        _frame++;
        if (_frame < 3)
            return false;

        _done = true;
        try
        {
            RunTests();
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestBaseScore threw: ", ex);
            Quit(1);
            return true;
        }

        GD.Print("==================================================================");
        GD.Print(">>> ALL BASESCORE TABLE TESTS PASSED SUCCESSFULLY! <<<");
        GD.Print("==================================================================");
        Quit(0);
        return true;
    }

    private void RunTests()
    {
        // Micro swarm tier: 5
        AssertThat(Score("norovirus")).IsEqual(5);
        AssertThat(Score("plasmodium_merozoite")).IsEqual(5);
        AssertThat(Score("prion_fragment")).IsEqual(5);
        AssertThat(Score("tachyzoite")).IsEqual(5);

        // Standard tier: 15
        AssertThat(Score("staph")).IsEqual(15);
        AssertThat(Score("s_virus")).IsEqual(15);
        AssertThat(Score("e_coli")).IsEqual(15);
        AssertThat(Score("plasmodium")).IsEqual(15);
        AssertThat(Score("varicella_zoster")).IsEqual(15);
        AssertThat(Score("aspergillus")).IsEqual(15);

        // Dangerous / agile tier: 35
        AssertThat(Score("pseudomonas")).IsEqual(35);
        AssertThat(Score("h_pylori")).IsEqual(35);
        AssertThat(Score("rabies")).IsEqual(35);
        AssertThat(Score("tetanus")).IsEqual(35);
        AssertThat(Score("hiv")).IsEqual(35);
        AssertThat(Score("ebola")).IsEqual(35);
        AssertThat(Score("candida")).IsEqual(35);
        AssertThat(Score("toxoplasma")).IsEqual(35);

        // Elite tank tier: 100
        AssertThat(Score("flu_drift")).IsEqual(100);
        AssertThat(Score("tb")).IsEqual(100);
        AssertThat(Score("anthrax_spore")).IsEqual(100);
        AssertThat(Score("anthrax_bacillus")).IsEqual(100);
        AssertThat(Score("malignant_cell")).IsEqual(100);
        AssertThat(Score("prion")).IsEqual(100);

        // Every standard pathogen exposes a positive score and compat accessor
        foreach (string id in AllPathogenIds)
        {
            var enemy = PathogenSpawner.CreatePathogen(id);
            AssertThat(enemy).IsNotNull();
            AssertThat(enemy!.BaseScore).IsGreater(0);
            AssertThat(enemy.GetBaseScore()).IsEqual(enemy.BaseScore);
            enemy.Free();
        }
        GD.Print("[PASS] 20 pathogen BaseScore tiers (5 / 15 / 35 / 100) verified.");

        // 09:00 sub-boss (600) and 15:00 terminal boss (3,000) tiers
        foreach (string mapId in MapIds)
        {
            var subBoss = PathogenSpawner.CreateSubBoss(mapId);
            AssertThat(subBoss).IsNotNull();
            AssertThat(subBoss!.BaseScore).IsEqual(600);
            subBoss.Free();

            var terminalBoss = PathogenSpawner.CreateTerminalBoss(mapId);
            AssertThat(terminalBoss).IsNotNull();
            AssertThat(terminalBoss!.BaseScore).IsEqual(3000);
            terminalBoss.Free();
        }

        // MRSA split children stay in the dangerous tier
        var elite = new MrsaEnragedElite();
        AssertThat(elite.BaseScore).IsEqual(35);
        elite.Free();

        GD.Print("[PASS] Sub-boss (600) and terminal boss (3,000) score tiers verified across all five maps.");
    }

    private static int Score(string enemyId)
    {
        var enemy = PathogenSpawner.CreatePathogen(enemyId);
        AssertThat(enemy).IsNotNull();
        int score = enemy!.BaseScore;
        enemy.Free();
        return score;
    }
}
