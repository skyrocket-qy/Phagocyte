using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using System.Collections.Generic;
using Phagocyte;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Skills;
using Phagocyte.UI;

namespace Phagocyte.Tests;

[TestSuite]
public partial class TestPassiveTree : TestHarness
{
    private int _phase = 0;
    private int _frameCount = 0;
    private MainMenu? _menu = null;
    private Main? _main = null;

    public override void _Initialize()
    {
        Banner("STARTING SHARED PASSIVE TREE VERIFICATION");
        IsolateSaves("passive_tree");
    }

    public override bool _Process(double delta)
    {
        try
        {
            switch (_phase)
            {
                case 0:
                    TestTreeEconomyAndPersistence();
                    TestGridLayoutAndPurchases();
                    TestStackedSkillModifiers();
                    TestBuildProfiles();
                    SetupMenu();
                    _phase = 1;
                    return false;

                case 1:
                    _frameCount++;
                    if (_frameCount < 4)
                        return false;

                    TestMenuFlow();
                    SetupRun();
                    _phase = 2;
                    _frameCount = 0;
                    return false;

                case 2:
                    _frameCount++;
                    if (_frameCount < 5)
                        return false;

                    TestRunIntegration();
                    Cleanup();
                    GD.Print("==================================================================");
                    GD.Print(">>> SHARED PASSIVE TREE SUITE PASSED! <<<");
                    GD.Print("==================================================================");
                    Quit(0);
                    return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[TEST FAILED]: " + ex.ToString());
            Cleanup();
            Quit(1);
            return true;
        }

        return false;
    }

    private static void TestTreeEconomyAndPersistence()
    {
        PassiveTreeManager.ResetAll();

        var starts = new List<string>();
        foreach (var cellId in GameManager.ClassData.Keys)
        {
            string cell = cellId.AsString();
            string start = PassiveTreeManager.GetStartNode(cell);
            AssertThat(PassiveTreeManager.IsKnownNode(start)).IsTrue();
            AssertThat(starts.Contains(start)).IsFalse();
            starts.Add(start);
        }
        AssertThat(starts.Count).IsEqual(5);
        AssertThat(PassiveTreeManager.IsKnownNode("hsc_core")).IsFalse();
        GD.Print("[PASS] All five cells begin at distinct nodes on one shared tree.");

        AssertThat(PassiveTreeManager.Nodes.Length).IsEqual(54);
        var nodeIds = new List<string>();
        int tradeoffCount = 0;
        int comboCount = 0;
        var rarities = new List<PassiveTreeManager.TreeRarity>();
        string[] validStats =
        {
            "might", "area", "cooldown_reduction", "projectile_speed", "duration", "amount",
            "pierce", "knockback", "crit_chance", "crit_damage", "max_health", "health_regen",
            "armor", "move_speed", "evasion", "block", "life_steal", "magnet"
        };
        foreach (var node in PassiveTreeManager.Nodes)
        {
            AssertThat(nodeIds.Contains(node.Id)).IsFalse();
            nodeIds.Add(node.Id);
            if (!rarities.Contains(node.Rarity))
                rarities.Add(node.Rarity);
            AssertThat(PassiveTreeManager.GetMaxStacks(node.Id) >= 1).IsTrue();
            AssertThat(PassiveTreeManager.GetPointCost(node.Id) >= 0).IsTrue();
            AssertThat(PassiveTreeManager.GetNeighbors(node.Id).Count).IsGreaterEqual(1);
            AssertThat(PassiveTreeManager.TryGetTrait(node.TraitId, out var trait)).IsTrue();

            // Node presentation is resolved from its trait: same trait → same icon/name/rarity/modifiers.
            AssertThat(node.Icon).IsEqual(trait.Icon);
            AssertThat(node.NameKey).IsEqual(trait.NameKey);
            AssertThat(node.DescKey).IsEqual(trait.DescKey);
            AssertThat(node.Rarity).IsEqual(trait.Rarity);
            AssertThat(node.Modifiers.Length).IsEqual(trait.Modifiers.Length);

            bool positive = false;
            bool negative = false;
            int effects = node.Modifiers.Length;
            foreach (var effect in node.Modifiers)
            {
                AssertThat(Array.IndexOf(validStats, effect.Stat) >= 0).IsTrue();
                AssertThat(Mathf.IsZeroApprox(effect.Value)).IsFalse();
                if (effect.Value > 0.0f)
                    positive = true;
                else
                    negative = true;
            }
            AssertThat(effects >= 1 || node.Rarity == PassiveTreeManager.TreeRarity.Start).IsTrue();
            if (node.Rarity == PassiveTreeManager.TreeRarity.Normal)
                AssertThat(effects).IsEqual(1);
            if (node.Rarity == PassiveTreeManager.TreeRarity.Magic)
            {
                AssertThat(effects).IsGreaterEqual(1);
                AssertThat(effects).IsLessEqual(2);
            }
            if (node.Rarity == PassiveTreeManager.TreeRarity.Rare)
                AssertThat(effects).IsEqual(2);
            if (node.Rarity == PassiveTreeManager.TreeRarity.Unique)
                AssertThat(effects).IsGreaterEqual(2);
            if (node.Rarity == PassiveTreeManager.TreeRarity.Start)
                AssertThat(effects).IsEqual(0);
            if (effects >= 2)
                comboCount++;
            if (positive && negative)
                tradeoffCount++;
        }
        AssertThat(rarities.Count).IsEqual(5);
        AssertThat(comboCount).IsGreaterEqual(3);
        AssertThat(tradeoffCount).IsGreaterEqual(1);
        AssertThat(PassiveTreeManager.GetMaxStacks("lysosome")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointCost("lysosome")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetMaxStacks("blood_price")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointCost("blood_price")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointCost("assassins_mandate")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointCost("adaptive_overdrive")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointCost("omnipotent_cytoplasm")).IsEqual(1);
        GD.Print("[PASS] The shared tree has 54 validated nodes with rarities, combinations, and trade-offs.");

        // Trait layer invariants: full coverage, no orphan traits, rarity quota,
        // and no two distinct non-hub traits carrying identical content
        // (duplicates must share; the five effect-free start hubs are exempt).
        var allTraits = PassiveTreeManager.Traits;
        AssertThat(allTraits.Count).IsEqual(54);
        var traitContent = new System.Collections.Generic.Dictionary<string, string>();
        var nodeCounts = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var node in PassiveTreeManager.Nodes)
            nodeCounts[node.TraitId] = nodeCounts.TryGetValue(node.TraitId, out int uses) ? uses + 1 : 1;
        foreach (var pair in allTraits)
        {
            AssertThat(nodeCounts.ContainsKey(pair.Key)).IsTrue();
            if (pair.Value.Rarity == PassiveTreeManager.TreeRarity.Start)
                continue;
            var modifierKey = string.Join("|", System.Array.ConvertAll(pair.Value.Modifiers,
                m => $"{m.Stat}:{m.Unit}:{m.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
            string contentKey = $"{pair.Value.Rarity}:{modifierKey}";
            AssertThat(traitContent.ContainsKey(contentKey)).IsFalse();
            traitContent[contentKey] = pair.Key;
        }
        var rarityCounts = new System.Collections.Generic.Dictionary<PassiveTreeManager.TreeRarity, int>();
        foreach (var node in PassiveTreeManager.Nodes)
            rarityCounts[node.Rarity] = rarityCounts.TryGetValue(node.Rarity, out int count) ? count + 1 : 1;
        AssertThat(rarityCounts[PassiveTreeManager.TreeRarity.Unique]).IsEqual(2);
        AssertThat(rarityCounts[PassiveTreeManager.TreeRarity.Rare]).IsEqual(4);
        AssertThat(rarityCounts[PassiveTreeManager.TreeRarity.Magic]).IsEqual(10);
        AssertThat(rarityCounts[PassiveTreeManager.TreeRarity.Normal]).IsEqual(33);
        AssertThat(rarityCounts[PassiveTreeManager.TreeRarity.Start]).IsEqual(5);
        GD.Print("[PASS] Every node resolves through a unique, fully referenced trait (2 legend / 4 rare / 10 magic / 33 normal / 5 start).");

        // Rarities are scattered: every region holds at least one special
        // (rare/unique/start) node, no region hoards more than three specials,
        // the two legendaries sit in different regions, and rares touch at
        // least three regions.
        var regionSpecialCounts = new System.Collections.Generic.Dictionary<string, int>();
        var regionRareCounts = new System.Collections.Generic.Dictionary<string, int>();
        var legendaryRegions = new System.Collections.Generic.HashSet<string>();
        foreach (var node in PassiveTreeManager.Nodes)
        {
            bool special = node.Rarity is PassiveTreeManager.TreeRarity.Rare
                or PassiveTreeManager.TreeRarity.Unique or PassiveTreeManager.TreeRarity.Start;
            if (!special)
                continue;
            regionSpecialCounts[node.Branch] = regionSpecialCounts.TryGetValue(node.Branch, out int specials) ? specials + 1 : 1;
            if (node.Rarity == PassiveTreeManager.TreeRarity.Unique)
                legendaryRegions.Add(node.Branch);
            if (node.Rarity == PassiveTreeManager.TreeRarity.Rare)
                regionRareCounts[node.Branch] = regionRareCounts.TryGetValue(node.Branch, out int rares) ? rares + 1 : 1;
        }
        foreach (string branch in PassiveTreeManager.RegionCenters.Keys)
        {
            AssertThat(regionSpecialCounts.TryGetValue(branch, out int specials) && specials >= 1).IsTrue();
            AssertThat(specials).IsLessEqual(3);
        }
        AssertThat(legendaryRegions.Count).IsEqual(2);
        AssertThat(regionRareCounts.Count).IsGreaterEqual(3);
        GD.Print("[PASS] Rare and legendary nodes are scattered across the regions.");

        string macroStart = PassiveTreeManager.GetStartNode("macrophage");
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", macroStart)).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointCost(macroStart)).IsEqual(1);
        AssertThat(PassiveTreeManager.RefundNode("macrophage", macroStart)).IsFalse();
        AssertThat(PassiveTreeManager.GetSpentPoints("macrophage")).IsEqual(0);
        GD.Print("[PASS] The innate start hub is always active and never consumes points.");

        string prevLang = GameManager.CurrentLanguage;
        GameManager.SetLanguage("en");
        var treeLysosome = GameManager.GetSkillInfo("lysosome");
        var activeLysosome = GameManager.GetSkillInfo("lysosomal_overload");
        AssertThat(treeLysosome["name"].AsString().Contains("Priming")).IsTrue();
        AssertThat(activeLysosome["name"].AsString().Contains("Overload")).IsTrue();
        GameManager.SetLanguage(prevLang);
        GD.Print("[PASS] Tree and active lysosome displays use their distinct names.");

        AssertThat(PassiveTreeManager.GetCellLevel("macrophage")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(0);
        // The start hub is innately lit at 0 cost and can never be bought or refunded
        // (docs/passivetree.md §2 L=0 / §5.1).
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "lysosome")).IsEqual(PassiveTreeManager.InnateStartStacks);
        AssertThat(PassiveTreeManager.Purchase("macrophage", "lysosome")).IsFalse();
        AssertThat(PassiveTreeManager.GetSpentPoints("macrophage")).IsEqual(0);
        GD.Print("[PASS] The macrophage start hub is innately lit and never consumes points.");

        AssertThat(PassiveTreeManager.RecordRunLevel("macrophage", 4)).IsTrue();
        AssertThat(PassiveTreeManager.RecordRunLevel("macrophage", 3)).IsFalse();
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(3);

        AssertThat(PassiveTreeManager.Purchase("macrophage", "thick_cytoplasm")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "bilayer")).IsTrue();
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(1);
        AssertThat(PassiveTreeManager.RefundNode("macrophage", "lysosome")).IsFalse();
        AssertThat(PassiveTreeManager.RefundNode("macrophage", "bilayer")).IsTrue();
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(2);
        GD.Print("[PASS] Points, the innate start anchor, and adjacency purchases behave correctly.");

        AssertThat(PassiveTreeManager.RecordRunLevel("macrophage", 5)).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "bilayer")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "rapid_clotting")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "second_wind")).IsTrue();
        AssertThat(PassiveTreeManager.GetSpentPoints("macrophage")).IsEqual(4);
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(0);
        AssertThat(PassiveTreeManager.Purchase("macrophage", "enduring_march")).IsFalse();
        GD.Print("[PASS] Boundary gate nodes open the way toward the core ring from any start.");

        AssertThat(PassiveTreeManager.RecordRunLevel("ctl", 5)).IsTrue();
        AssertThat(PassiveTreeManager.GetNodeStacks("ctl", "opsonin")).IsEqual(PassiveTreeManager.InnateStartStacks);
        AssertThat(PassiveTreeManager.Purchase("ctl", "vdj")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("ctl", "assassins_mandate")).IsTrue();
        AssertThat(PassiveTreeManager.RefundNode("ctl", "vdj")).IsFalse();
        AssertThat(PassiveTreeManager.RefundNode("ctl", "assassins_mandate")).IsTrue();
        AssertThat(PassiveTreeManager.RefundNode("ctl", "vdj")).IsTrue();
        GD.Print("[PASS] Refunds preserve a continuous path back to the start node.");

        PassiveTreeManager.ResetAllocation("macrophage");
        AssertThat(PassiveTreeManager.RecordRunLevel("macrophage", 8)).IsTrue();
        AssertThat(PassiveTreeManager.GetMaxStacks("lysosome")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "lysosome")).IsEqual(PassiveTreeManager.InnateStartStacks);
        AssertThat(PassiveTreeManager.Purchase("macrophage", "lysosome")).IsFalse();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "thick_cytoplasm")).IsTrue();
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "thick_cytoplasm")).IsEqual(1);
        AssertThat(PassiveTreeManager.Purchase("macrophage", "thick_cytoplasm")).IsFalse();
        GD.Print("[PASS] Each tree node is bought once and then caps out.");

        PassiveTreeManager.SaveToDisk();
        PassiveTreeManager.ReloadFromDisk();
        AssertThat(PassiveTreeManager.GetCellLevel("macrophage")).IsEqual(8);
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "lysosome")).IsEqual(PassiveTreeManager.InnateStartStacks);
        AssertThat(PassiveTreeManager.Purchase("macrophage", "bilayer")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "iron_membrane")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "rapid_clotting")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "second_wind")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "endotoxin")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "contained_fury")).IsTrue();
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(0);
        AssertThat(PassiveTreeManager.Purchase("macrophage", "autophagy")).IsFalse();
        GD.Print("[PASS] Tree levels and presets persist across reloads.");
    }

    private static void TestGridLayoutAndPurchases()
    {
        PassiveTreeManager.ResetAll();

        var cells = new System.Collections.Generic.Dictionary<(int, int), string>();
        foreach (var node in PassiveTreeManager.Nodes)
        {
            AssertThat(node.Ring).IsGreaterEqual(0);
            AssertThat(node.Ring).IsLessEqual(8);
            Vector2 offset = node.Position - PassiveTreeManager.WorldCenter;
            int column = Mathf.RoundToInt(offset.X / PassiveTreeManager.GridStep);
            int row = Mathf.RoundToInt(-offset.Y / PassiveTreeManager.GridStep);
            AssertThat(node.Position.DistanceTo(PassiveTreeManager.GridPosition(column, row))).IsLess(0.5f);
            AssertThat(node.Ring).IsEqual(Math.Abs(column) + Math.Abs(row));
            AssertThat(cells.ContainsKey((column, row))).IsFalse();
            cells[(column, row)] = node.Id;
        }
        GD.Print("[PASS] Every node sits on its own grid cell with a matching layer.");

        // Six 3x3 region squares, one per branch: every node lives inside its own
        // square, cell start hubs sit at the square centre, and no two squares
        // overlap (centres at least 3 grid steps apart on one axis).
        var regionCenters = PassiveTreeManager.RegionCenters;
        AssertThat(regionCenters.Count).IsEqual(6);
        foreach (var node in PassiveTreeManager.Nodes)
        {
            AssertThat(regionCenters.ContainsKey(node.Branch)).IsTrue();
            Vector2 local = (node.Position - regionCenters[node.Branch]) / PassiveTreeManager.GridStep;
            AssertThat(Mathf.Abs(local.X)).IsLessEqual(PassiveTreeManager.RegionRadius + 0.001f);
            AssertThat(Mathf.Abs(local.Y)).IsLessEqual(PassiveTreeManager.RegionRadius + 0.001f);
        }
        var regionBranches = new System.Collections.Generic.List<string>(regionCenters.Keys);
        var regionCells = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<(int, int)>>();
        foreach (var node in PassiveTreeManager.Nodes)
        {
            Vector2 local = (node.Position - regionCenters[node.Branch]) / PassiveTreeManager.GridStep;
            if (!regionCells.TryGetValue(node.Branch, out var seen))
            {
                seen = new System.Collections.Generic.HashSet<(int, int)>();
                regionCells[node.Branch] = seen;
            }
            AssertThat(seen.Add((Mathf.RoundToInt(local.X), Mathf.RoundToInt(local.Y)))).IsTrue();
        }
        foreach (string branch in regionBranches)
        {
            AssertThat(regionCells.TryGetValue(branch, out var seen)).IsTrue();
            AssertThat(seen!.Count).IsEqual(9);
            Rect2 region = PassiveTreeManager.GetRegionBounds(branch);
            AssertThat(region.Size.X).IsEqualApprox(3.0f * PassiveTreeManager.GridStep, 0.01f);
            AssertThat(region.Size.Y).IsEqualApprox(3.0f * PassiveTreeManager.GridStep, 0.01f);
        }
        // Six 3x3 blocks arranged 2 columns x 3 rows form one 6x9 board.
        Rect2 board = PassiveTreeManager.GetRegionBounds(regionBranches[0]);
        for (int i = 1; i < regionBranches.Count; i++)
            board = board.Merge(PassiveTreeManager.GetRegionBounds(regionBranches[i]));
        AssertThat(Mathf.RoundToInt(board.Size.X / PassiveTreeManager.GridStep)).IsEqual(6);
        AssertThat(Mathf.RoundToInt(board.Size.Y / PassiveTreeManager.GridStep)).IsEqual(9);
        AssertThat(cells.Count).IsEqual(54);
        for (int i = 0; i < regionBranches.Count; i++)
        {
            for (int j = i + 1; j < regionBranches.Count; j++)
            {
                Vector2 delta = (regionCenters[regionBranches[i]] - regionCenters[regionBranches[j]]) / PassiveTreeManager.GridStep;
                AssertThat(Mathf.Abs(delta.X) >= 3.0f - 0.001f || Mathf.Abs(delta.Y) >= 3.0f - 0.001f).IsTrue();
            }
        }
        // The six 3x3 squares tile edge-to-edge: every square shares a border
        // with at least one neighbour, so the board is one seamless 6x9 block.
        foreach (string branch in regionBranches)
        {
            bool touching = false;
            foreach (string other in regionBranches)
            {
                if (other == branch)
                    continue;
                Vector2 delta = (regionCenters[branch] - regionCenters[other]) / PassiveTreeManager.GridStep;
                if (Mathf.Abs(Mathf.Abs(delta.X) - 3.0f) < 0.001f && Mathf.Abs(delta.Y) < 0.001f)
                    touching = true;
                if (Mathf.Abs(Mathf.Abs(delta.Y) - 3.0f) < 0.001f && Mathf.Abs(delta.X) < 0.001f)
                    touching = true;
            }
            AssertThat(touching).IsTrue();
        }
        foreach (var cellKey in GameManager.ClassData.Keys)
        {
            string cell = cellKey.AsString();
            AssertThat(PassiveTreeManager.TryGetNode(PassiveTreeManager.GetStartNode(cell), out var startNode)).IsTrue();
            AssertThat(startNode.Position.DistanceTo(regionCenters[startNode.Branch])).IsLess(0.5f);
        }
        GD.Print("[PASS] Every region is a 3x3 square centred on its cell's start hub.");

        var degrees = new System.Collections.Generic.Dictionary<string, int>();
        var segments = new System.Collections.Generic.HashSet<(string, string)>();
        foreach (var edge in PassiveTreeManager.Edges)
        {
            AssertThat(PassiveTreeManager.TryGetNode(edge.From, out var from)).IsTrue();
            AssertThat(PassiveTreeManager.TryGetNode(edge.To, out var to)).IsTrue();
            float dx = Mathf.Abs(from.Position.X - to.Position.X);
            float dy = Mathf.Abs(from.Position.Y - to.Position.Y);
            AssertThat(dx + dy).IsEqualApprox(PassiveTreeManager.GridStep, 0.5f);
            var key = string.CompareOrdinal(edge.From, edge.To) <= 0
                ? (edge.From, edge.To)
                : (edge.To, edge.From);
            AssertThat(segments.Contains(key)).IsFalse();
            segments.Add(key);
            degrees[edge.From] = degrees.TryGetValue(edge.From, out int da) ? da + 1 : 1;
            degrees[edge.To] = degrees.TryGetValue(edge.To, out int db) ? db + 1 : 1;
        }
        foreach (var pair in degrees)
            AssertThat(pair.Value).IsLessEqual(4);
        GD.Print("[PASS] Every connection is one orthogonal grid step and no node exceeds four links.");

        AssertThat(PassiveTreeManager.RecordRunLevel("dendritic", 25)).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "chemokine")).IsFalse(); // innate start hub
        AssertThat(PassiveTreeManager.Purchase("dendritic", "far_sense")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "antigen_harvest")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "scavenger_field")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "lucky_mutation")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "patient_observer")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "swarm_cartography")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "risk_assessment")).IsTrue();
        AssertThat(PassiveTreeManager.GetSpentPoints("dendritic")).IsEqual(7);
        GD.Print("[PASS] A deep lineage path can be purchased point by point.");

        string prevLang = GameManager.CurrentLanguage;
        GameManager.SetLanguage("en");
        AssertThat(PassiveTreeManager.GetNodeTooltipText("dendritic", "thick_cytoplasm").Contains("atrophy")).IsFalse();
        GameManager.SetLanguage(prevLang);

        AssertThat(PassiveTreeManager.Purchase("dendritic", "longevity")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "immortal_culture")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "glycolysis")).IsTrue();
        AssertThat(PassiveTreeManager.CanPurchase("dendritic", "rolling_thunder")).IsTrue();
        GD.Print("[PASS] Cross-lineage portals stay purchasable through the core ring.");
    }

    private static void TestStackedSkillModifiers()
    {
        var host = new CharacterBody2D();
        var stats = new CellStats { Name = "CellStats" };
        host.AddChild(stats);

        var skill = PassiveTreeManager.CreateStackedSkill("opsonin", 2);
        AssertThat(skill).IsNotNull();
        AssertThat(skill!.Level).IsEqual(1);
        host.AddChild(skill);
        skill.Setup(host, -1);
        // Start hubs carry no stat effects: only the CellStats base remains.
        AssertThat(stats.GetStat("crit_chance")).IsEqualApprox(0.05f, 0.001f);

        host.QueueFree();

        var bundleHost = new CharacterBody2D();
        var bundleStats = new CellStats { Name = "CellStats" };
        bundleHost.AddChild(bundleStats);
        var bundle = PassiveTreeManager.CreateStackedSkill("blood_price", 1);
        AssertThat(bundle is TreeStatBundleSkill).IsTrue();
        bundleHost.AddChild(bundle!);
        bundle!.Setup(bundleHost, -1);
        AssertThat(bundleStats.GetStat("might")).IsEqualApprox(1.08f, 0.001f);
        AssertThat(bundleStats.GetStat("max_health")).IsEqualApprox(100.0f, 0.01f);
        bundleHost.QueueFree();
        GD.Print("[PASS] A normal node bundle applies its single stat modifier.");
    }

    private static void TestBuildProfiles()
    {
        PassiveTreeManager.ResetAll();
        AssertThat(PassiveTreeManager.GetProfileCount("macrophage")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetActiveProfile("macrophage")).IsEqual(0);
        AssertThat(PassiveTreeManager.GetProfileCount("nope")).IsEqual(0);
        AssertThat(PassiveTreeManager.GetActiveProfile("nope")).IsEqual(0);
        AssertThat(string.IsNullOrEmpty(PassiveTreeManager.GetProfileName(0))).IsFalse();
        AssertThat(PassiveTreeManager.GetProfileName(0) == PassiveTreeManager.GetProfileName(1)).IsFalse();

        // Grow to the cap; each new slot becomes active.
        AssertThat(PassiveTreeManager.AddProfile("macrophage")).IsTrue();
        AssertThat(PassiveTreeManager.GetProfileCount("macrophage")).IsEqual(2);
        AssertThat(PassiveTreeManager.GetActiveProfile("macrophage")).IsEqual(1);
        AssertThat(PassiveTreeManager.AddProfile("macrophage")).IsTrue();
        AssertThat(PassiveTreeManager.GetProfileCount("macrophage")).IsEqual(3);
        AssertThat(PassiveTreeManager.GetActiveProfile("macrophage")).IsEqual(2);
        AssertThat(PassiveTreeManager.AddProfile("macrophage")).IsFalse();
        AssertThat(PassiveTreeManager.AddProfile("nope")).IsFalse();

        // Out-of-range activation is refused and never moves the active slot.
        AssertThat(PassiveTreeManager.SetActiveProfile("macrophage", 9)).IsFalse();
        AssertThat(PassiveTreeManager.SetActiveProfile("macrophage", -1)).IsFalse();
        AssertThat(PassiveTreeManager.SetActiveProfile("nope", 0)).IsFalse();
        AssertThat(PassiveTreeManager.GetActiveProfile("macrophage")).IsEqual(2);

        // Slots are isolated per profile.
        AssertThat(PassiveTreeManager.RecordRunLevel("macrophage", 8)).IsTrue();
        AssertThat(PassiveTreeManager.SetActiveProfile("macrophage", 0)).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "thick_cytoplasm")).IsTrue();
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "thick_cytoplasm")).IsEqual(1);
        AssertThat(PassiveTreeManager.SetActiveProfile("macrophage", 2)).IsTrue();
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "thick_cytoplasm")).IsEqual(0);
        AssertThat(PassiveTreeManager.SetActiveProfile("macrophage", 0)).IsTrue();
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "thick_cytoplasm")).IsEqual(1);

        // Reset clears only the active slot and keeps the slot structure.
        PassiveTreeManager.ResetAllocation("macrophage");
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "thick_cytoplasm")).IsEqual(0);
        AssertThat(PassiveTreeManager.GetProfileCount("macrophage")).IsEqual(3);

        // Delete: the last slot is kept; the active index follows the deletion.
        AssertThat(PassiveTreeManager.DeleteProfile("macrophage", 2)).IsTrue();
        AssertThat(PassiveTreeManager.GetProfileCount("macrophage")).IsEqual(2);
        AssertThat(PassiveTreeManager.GetActiveProfile("macrophage")).IsEqual(0);
        AssertThat(PassiveTreeManager.SetActiveProfile("macrophage", 1)).IsTrue();
        AssertThat(PassiveTreeManager.DeleteProfile("macrophage", 9)).IsFalse();
        AssertThat(PassiveTreeManager.DeleteProfile("macrophage", 1)).IsTrue();
        AssertThat(PassiveTreeManager.GetProfileCount("macrophage")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetActiveProfile("macrophage")).IsEqual(0);
        AssertThat(PassiveTreeManager.DeleteProfile("macrophage", 0)).IsFalse();
        AssertThat(PassiveTreeManager.DeleteProfile("nope", 0)).IsFalse();
        GD.Print("[PASS] Build profiles grow, isolate, persist, and delete correctly.");

        // Persistence round-trip keeps every slot and the active index.
        AssertThat(PassiveTreeManager.Purchase("macrophage", "thick_cytoplasm")).IsTrue();
        AssertThat(PassiveTreeManager.AddProfile("macrophage")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "iron_membrane")).IsTrue();
        PassiveTreeManager.SaveToDisk();
        PassiveTreeManager.ReloadFromDisk();
        AssertThat(PassiveTreeManager.GetProfileCount("macrophage")).IsEqual(2);
        AssertThat(PassiveTreeManager.GetActiveProfile("macrophage")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "iron_membrane")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "thick_cytoplasm")).IsEqual(0);
        AssertThat(PassiveTreeManager.SetActiveProfile("macrophage", 0)).IsTrue();
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "thick_cytoplasm")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "iron_membrane")).IsEqual(0);
        GD.Print("[PASS] Build profiles persist across save/load with the active index.");

        // Legacy single-slot saves migrate into profile slot 0.
        PassiveTreeManager.ResetAll();
        var legacyPayload = new Godot.Collections.Dictionary
        {
            { "cell_levels", new Godot.Collections.Dictionary { { "macrophage", 4 } } },
            { "allocations", new Godot.Collections.Dictionary { { "macrophage", new Godot.Collections.Dictionary { { "thick_cytoplasm", 1 } } } } },
            { "bonus_points", 0 }
        };
        JsonStore.Write(PassiveTreeManager.SavePath, legacyPayload);
        PassiveTreeManager.ReloadFromDisk();
        AssertThat(PassiveTreeManager.GetProfileCount("macrophage")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetActiveProfile("macrophage")).IsEqual(0);
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "thick_cytoplasm")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetCellLevel("macrophage")).IsEqual(4);
        GD.Print("[PASS] Legacy single-slot saves migrate into profile slot 0.");

        // Deleting a slot before the active one shifts the active index along.
        AssertThat(PassiveTreeManager.AddProfile("macrophage")).IsTrue();
        AssertThat(PassiveTreeManager.AddProfile("macrophage")).IsTrue();
        AssertThat(PassiveTreeManager.SetActiveProfile("macrophage", 2)).IsTrue();
        AssertThat(PassiveTreeManager.DeleteProfile("macrophage", 0)).IsTrue();
        AssertThat(PassiveTreeManager.GetProfileCount("macrophage")).IsEqual(2);
        AssertThat(PassiveTreeManager.GetActiveProfile("macrophage")).IsEqual(1);
        GD.Print("[PASS] Deleting an earlier profile shifts the active index along.");

        PassiveTreeManager.ResetAll();
    }

    private void SetupMenu()
    {
        PassiveTreeManager.ResetAll();
        GameManager.SetLanguage("en");
        GameManager.SelectedClass = "macrophage";
        GameManager.SelectedMap = "acute_wound";

        var menuScene = AssetLoader.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
        AssertThat(menuScene).IsNotNull();
        _menu = menuScene!.Instantiate<MainMenu>();
        Root.AddChild(_menu);
    }

    private void TestMenuFlow()
    {
        AssertThat(_menu).IsNotNull();
        _menu!.OnStartPressed();
        _menu.SelectClass("macrophage");
        _menu.OnClassConfirmPressed();
        AssertThat(_menu.PassiveView!.Visible).IsTrue();
        AssertThat(_menu.MapView!.Visible).IsFalse();

        AssertThat(PassiveTreeManager.RecordRunLevel("macrophage", 4)).IsTrue();
        _menu.SelectPassiveBuild("macrophage");
        AssertThat(_menu.TreeLevelLbl!.Text.Contains("4")).IsTrue();
        AssertThat(_menu.TreeCanvas!.RenderedNodeCount).IsEqual(PassiveTreeManager.Nodes.Length);
        AssertThat(PassiveTreeManager.TryGetNode("lysosome", out var lysosomeNode)).IsTrue();
        AssertThat(PassiveTreeView.HitTestWorldPosition(lysosomeNode.Position)).IsEqual("lysosome");
        // The full 10x15 board now covers the world origin (core region cell -2,0).
        AssertThat(PassiveTreeView.HitTestWorldPosition(PassiveTreeManager.WorldCenter)).IsNotNull();
        AssertThat(PassiveTreeView.HitTestWorldPosition(new Vector2(-1000, -1000))).IsNull();
        var startButton = _menu.TreeCanvas.GetNodeOrNull<Button>("ZoomRoot/TreeNode_lysosome");
        AssertThat(startButton).IsNotNull();
        AssertThat(startButton!.Text.Contains("🧪")).IsTrue();

        _menu.OnTreeNodeActivated("thick_cytoplasm");
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "thick_cytoplasm")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(2);
        AssertThat(_menu.ActiveTreeNodeId).IsEqual("thick_cytoplasm");
        GD.Print("[PASS] The menu flow purchases and preserves a macrophage tree node.");

        _menu.OnTreeNodeHovered("blood_price");
        AssertThat(_menu.ActiveTreeNodeId).IsEqual("blood_price");

        string tooltip = PassiveTreeManager.GetNodeTooltipText("macrophage", "blood_price");
        AssertThat(tooltip.Contains("Blood Price")).IsTrue();
        AssertThat(tooltip.Contains("+8% Might")).IsTrue();
        AssertThat(tooltip.Contains("Max Health")).IsFalse();
        GD.Print("[PASS] Hover tooltips expose the node's single stat modifier.");

        float zoomBefore = _menu.TreeCanvas.ZoomLevel;
        _menu.TreeCanvas.ZoomStep(1.2f);
        AssertThat(_menu.TreeCanvas.ZoomLevel).IsGreater(zoomBefore);
        _menu.TreeCanvas.FitTree();
        AssertThat(_menu.TreeCanvas.ZoomLevel).IsGreater(0.2f);
        AssertThat(_menu.TreeCanvas.ZoomLevel).IsLessEqual(1.75f);

        var treeView = _menu.TreeCanvas;
        Vector2 cameraBeforeStep = treeView.CameraPosition;
        treeView.ZoomStep(1.1f);
        AssertThat(treeView.CameraPosition.DistanceTo(cameraBeforeStep)).IsLess(0.01f);

        Vector2 probe = treeView.Size * 0.5f + new Vector2(60.0f, -40.0f);
        treeView.ZoomAt(probe, treeView.ZoomLevel * 1.25f);
        Vector2 cameraAfterFirst = treeView.CameraPosition;
        float zoomAfterFirst = treeView.ZoomLevel;
        treeView.ZoomAt(probe, zoomAfterFirst);
        AssertThat(treeView.CameraPosition.DistanceTo(cameraAfterFirst)).IsLess(0.01f);
        AssertThat(treeView.ZoomLevel).IsEqual(zoomAfterFirst);
        GD.Print("[PASS] Camera stays anchored across zoom steps (no drift or jitter feedback).");

        string hoverNodeId = "blood_price";
        AssertThat(PassiveTreeManager.TryGetNode(hoverNodeId, out var hoverNode)).IsTrue();
        Vector2 hoverLocal = treeView.Size * 0.5f
            + (hoverNode.Position - treeView.CameraPosition) * treeView.ZoomLevel;
        treeView.UpdateHoverAt(hoverLocal);
        var tooltipPanel = treeView.GetNodeOrNull<PanelContainer>("TreeTooltip");
        var tooltipLabel = tooltipPanel?.GetNodeOrNull<RichTextLabel>("TreeTooltipText");
        AssertThat(tooltipPanel).IsNotNull();
        AssertThat(tooltipPanel!.Visible).IsTrue();
        AssertThat(tooltipLabel).IsNotNull();
        AssertThat(tooltipLabel!.Text.Contains("Blood Price")).IsTrue();
        AssertThat(tooltipLabel.Text.Contains("Cost")).IsFalse();
        AssertThat(tooltipLabel.Text.Contains("Normal")).IsFalse();
        AssertThat(tooltipLabel.Text.Contains("Effects")).IsFalse();

        AssertThat(PassiveTreeManager.TryGetNode("adaptive_overdrive", out var rareNode)).IsTrue();
        Vector2 rareLocal = treeView.Size * 0.5f
            + (rareNode.Position - treeView.CameraPosition) * treeView.ZoomLevel;
        treeView.UpdateHoverAt(rareLocal);
        AssertThat(tooltipPanel.Visible).IsTrue();
        AssertThat(tooltipLabel.Text.Contains("Overdrive")).IsTrue();
        AssertThat(tooltipLabel.Text.Contains("Rare")).IsFalse();
        AssertThat(tooltipLabel.Text.Contains("Cost")).IsFalse();
        GD.Print("[PASS] Hovering a tree node shows its tooltip immediately (no click, no rarity/cost text).");

        _menu.OnTreeNodeRefundRequested("thick_cytoplasm");
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "thick_cytoplasm")).IsEqual(0);
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(3);
        // The start hub stays innately lit regardless of refunds.
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "lysosome")).IsEqual(PassiveTreeManager.InnateStartStacks);
        AssertThat(PassiveTreeManager.RefundNode("macrophage", "lysosome")).IsFalse();
        _menu.OnTreeNodeActivated("thick_cytoplasm");
        _menu.OnPassiveConfirmPressed();
        AssertThat(_menu.PassiveView.Visible).IsFalse();
        AssertThat(_menu.MapView.Visible).IsTrue();
        GD.Print("[PASS] Refunds and the tree-to-map transition behave correctly.");
    }

    private void SetupRun()
    {
        PassiveTreeManager.ResetAll();
        AssertThat(PassiveTreeManager.RecordRunLevel("ctl", 5)).IsTrue();
        // The CTL start hub (opsonin) is innate, so the first purchase is its neighbor.
        AssertThat(PassiveTreeManager.Purchase("ctl", "precise_edge")).IsTrue();

        GameManager.SelectedClass = "ctl";
        var mainScene = AssetLoader.Load<PackedScene>("res://scenes/main.tscn");
        AssertThat(mainScene).IsNotNull();
        _main = mainScene!.Instantiate<Main>();
        Root.AddChild(_main);
    }

    private void TestRunIntegration()
    {
        AssertThat(_main).IsNotNull();
        AssertThat(_main!.Player is BaseCell).IsTrue();
        var player = (BaseCell)_main.Player!;

        // CTL base 15% + Precise Edge 2% = 17% (the opsonin hub carries no stats).
        AssertThat(player.Stats!.GetStat("crit_chance")).IsEqualApprox(0.17f, 0.001f);
        AssertThat(player.CellSkillManager!.GetPassiveSlot(0)).IsNull();
        GD.Print("[PASS] A saved tree preset modifies the run without using passive slots.");

        var hud = _main.GetNodeOrNull<Hud>("HUD");
        AssertThat(hud).IsNotNull();
        hud!.ToggleTreeOverlay();
        AssertThat(hud.IsTreeOverlayVisible).IsTrue();
        AssertThat(hud.TreeOverlayText!.Text.Contains("17.0%") || hud.TreeOverlayText!.Text.Contains("0.17")).IsTrue();
        string activeProfileName = PassiveTreeManager.GetProfileName(PassiveTreeManager.GetActiveProfile("ctl"));
        AssertThat(hud.TreeOverlayText!.Text.Contains(activeProfileName)).IsTrue();
        AssertThat(InputMap.HasAction("toggle_tree")).IsTrue();
        GD.Print("[PASS] The in-game overlay exposes the active tree build and current stats.");

        var mockPlayer = new CharacterBody2D();
        var stats = new CellStats { Name = "CellStats" };
        mockPlayer.AddChild(stats);
        var sm = new SkillManager { Name = "SkillManager" };
        mockPlayer.AddChild(sm);
        sm.Setup(mockPlayer);
        sm.EquipActive(new RosTorrentSkill(), 0);
        for (int iter = 0; iter < 30; iter++)
        {
            foreach (var choice in UpgradeManager.GenerateChoices(mockPlayer, 3))
            {
                if (choice.TryGetValue("type", out var typeVal) && typeVal.AsString() == "new_passive")
                    AssertThat(choice["id"].AsString()).IsNotEqual("opsonin");
            }
        }
        mockPlayer.QueueFree();
        GD.Print("[PASS] Tree-owned passives are excluded from in-run new-passive offers.");
    }

    private void Cleanup()
    {
        FreeMain(_main);
        _main = null;
        if (_menu != null && IsInstanceValid(_menu))
            _menu.QueueFree();

        GameManager.SelectedClass = "macrophage";
        GameManager.SetLanguage("zh_CN");
        PassiveTreeManager.ResetAll();
        RestoreSaves();
    }
}
