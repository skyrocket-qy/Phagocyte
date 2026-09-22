using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Core;
using Phagocyte.Player;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the map first-clear talent point pipeline and the innately lit start
/// hub (docs/passivetree.md §2/§5, docs/achievement.md §2.1):
/// 5 organs x Normal/Hard clears award talent points, and every deployed cell's
/// start hub is permanently active at zero cost.
/// </summary>
[TestSuite]
public partial class TestTalentPipeline : TestHarness
{
    private static readonly string[] MapIds =
    {
        "acute_wound", "alveolar_space", "hepatic_sinusoid", "gastric_lumen", "blood_brain_barrier"
    };

    private static readonly string[] CellIds =
    {
        "macrophage", "ctl", "neutrophil", "b_cell", "dendritic"
    };

    private int _phase = 0;
    private Main? _main = null;

    public override void _Initialize()
    {
        Banner("STARTING TALENT POINT PIPELINE & INNATE START HUB VERIFICATION");

        IsolateSaves("talent");

        AchievementManager.ResetAll();
        PassiveTreeManager.ResetAll();
    }

    public override bool _Process(double delta)
    {
        try
        {
            switch (_phase)
            {
            case 0:
                _phase++;
                return false; // let the scene tree settle
            case 1:
                RunMapClearPointTests();
                _phase++;
                return false;
            case 2:
                RunInnateStartHubTests();
                _phase++;
                return false;
            case 3:
                RunInnateHubRunIntegrationTests();
                _phase++;
                return false;
            default:
                Cleanup();
                GD.Print("==================================================================");
                GD.Print(">>> ALL TALENT PIPELINE TESTS PASSED SUCCESSFULLY! <<<");
                GD.Print("==================================================================");
                Quit(0);
                return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestTalentPipeline threw: ", ex);
            Cleanup();
            Quit(1);
            return true;
        }
    }

    private void RunMapClearPointTests()
    {
        // Catalog: all 5 organs have a Normal and a Hard first-clear achievement,
        // each awarding at least one microtube talent point.
        int normalClears = 0;
        int hardClears = 0;
        foreach (string mapId in MapIds)
        {
            bool foundNormal = false;
            bool foundHard = false;
            foreach (string achId in AchievementManager.Achievements.Keys)
            {
                var ach = AchievementManager.Achievements[achId].AsGodotDictionary();
                if (ach.GetValueOrDefault("map_id", "").AsString() != mapId)
                    continue;

                bool hard = ach.GetValueOrDefault("difficulty", "normal").AsString() == "hard";
                int points = ach.GetValueOrDefault("talent_points", 0).AsInt32();
                AssertThat(points).IsGreaterEqual(1);

                if (hard)
                {
                    foundHard = true;
                    hardClears++;
                }
                else
                {
                    foundNormal = true;
                    normalClears++;
                }
            }
            AssertThat(foundNormal).IsTrue();
            AssertThat(foundHard).IsTrue();
        }
        AssertThat(normalClears).IsEqual(5);
        AssertThat(hardClears).IsEqual(5);

        // The final organ clear carries its documented +2 capstone reward.
        var bbb = AchievementManager.Achievements["bbb_clear"].AsGodotDictionary();
        AssertThat(bbb.GetValueOrDefault("talent_points", 0).AsInt32()).IsEqual(2);

        // First clears grant points exactly once each.
        AssertThat(PassiveTreeManager.BonusPoints).IsEqual(0);
        AchievementManager.RecordMapClear("acute_wound");
        AssertThat(PassiveTreeManager.BonusPoints).IsEqual(1);

        AchievementManager.RecordMapClear("acute_wound");
        AssertThat(PassiveTreeManager.BonusPoints).IsEqual(1);

        AchievementManager.RecordMapClear("alveolar_space", true);
        AssertThat(PassiveTreeManager.BonusPoints).IsEqual(2);
        AssertThat(AchievementManager.IsUnlocked("alveolar_hard_clear")).IsTrue();

        AchievementManager.RecordMapClear("blood_brain_barrier");
        AssertThat(PassiveTreeManager.BonusPoints).IsEqual(4);

        GD.Print("[PASS] 5 organs x Normal/Hard first clears award talent points exactly once.");
    }

    private void RunInnateStartHubTests()
    {
        PassiveTreeManager.ResetAll();

        foreach (string cellId in CellIds)
        {
            string start = PassiveTreeManager.GetStartNode(cellId);
            AssertThat(start).IsNotEmpty();
            AssertThat(PassiveTreeManager.IsInnateStartNode(cellId, start)).IsTrue();

            // Innately lit, free, permanent, and never purchasable/refundable.
            AssertThat(PassiveTreeManager.GetNodeStacks(cellId, start)).IsEqual(PassiveTreeManager.InnateStartStacks);
            AssertThat(PassiveTreeManager.GetSpentPoints(cellId)).IsEqual(0);
            AssertThat(PassiveTreeManager.Purchase(cellId, start)).IsFalse();
            AssertThat(PassiveTreeManager.CanPurchase(cellId, start)).IsFalse();
            AssertThat(PassiveTreeManager.RefundNode(cellId, start)).IsFalse();
            AssertThat(PassiveTreeManager.IsFullyConnected(cellId, start)).IsTrue();
        }

        // The hub opens the first neighbor for 1 point, charging only for purchases.
        string macrophageStart = PassiveTreeManager.GetStartNode("macrophage");
        var neighbors = PassiveTreeManager.GetNeighbors(macrophageStart);
        AssertThat(neighbors.Count).IsGreater(0);
        string firstNeighbor = neighbors[0];
        AssertThat(PassiveTreeManager.CanPurchase("macrophage", firstNeighbor)).IsFalse(); // no points yet
        AssertThat(PassiveTreeManager.RecordRunLevel("macrophage", 2)).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", firstNeighbor)).IsTrue();
        AssertThat(PassiveTreeManager.GetSpentPoints("macrophage")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(0);

        // The innate hub survives persistence and is not written into the save payload.
        PassiveTreeManager.SaveToDisk();
        PassiveTreeManager.ReloadFromDisk();
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", macrophageStart)).IsEqual(PassiveTreeManager.InnateStartStacks);
        AssertThat(PassiveTreeManager.GetSpentPoints("macrophage")).IsEqual(1);
        AssertThat(PassiveTreeManager.IsInnateStartNode("macrophage", macrophageStart)).IsTrue();

        GD.Print("[PASS] Every cell's start hub is permanently lit at zero cost across save/load.");
    }

    private void RunInnateHubRunIntegrationTests()
    {
        PassiveTreeManager.ResetAll();
        var main = InstantiateMain();
        _main = main;

        var player = main.GetNodeOrNull<BaseCell>("Macrophage");
        AssertThat(player).IsNotNull();

        // The innate hub is instantiated for free but carries no stat effects.
        AssertThat(player!.GetNodeOrNull<Node>("TreeLoadout_lysosome")).IsNotNull();
        AssertThat(player.Stats).IsNotNull();
        AssertThat(player.Stats!.GetStat("might")).IsEqualApprox(1.0f, 0.001f);
        AssertThat(PassiveTreeManager.GetSpentPoints("macrophage")).IsEqual(0);

        GD.Print("[PASS] The innate start hub is present in the run at zero point cost with no stat effects.");
    }

    private void Cleanup()
    {
        Paused = false;

        FreeMain(_main);
        _main = null;

        AchievementManager.ResetAll();
        PassiveTreeManager.ResetAll();
        RestoreSaves();
        GameManager.SelectedClass = "macrophage";
    }
}
