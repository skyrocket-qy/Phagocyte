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
        AssertThat(PassiveTreeManager.IsKnownNode("tree_hsc_core")).IsFalse();
        GD.Print("[PASS] All five cells begin at distinct nodes on one shared tree.");

        AssertThat(PassiveTreeManager.Nodes.Length).IsGreaterEqual(51);
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

            bool positive = false;
            bool negative = false;
            int effects = node.SkillType != null ? 1 : 0;
            foreach (var effect in node.Modifiers)
            {
                AssertThat(Array.IndexOf(validStats, effect.Stat) >= 0).IsTrue();
                AssertThat(Mathf.IsZeroApprox(effect.Value)).IsFalse();
                effects++;
                if (effect.Value > 0.0f)
                    positive = true;
                else
                    negative = true;
            }
            AssertThat(effects >= 1).IsTrue();
            if (node.Rarity == PassiveTreeManager.TreeRarity.Normal)
                AssertThat(node.Modifiers.Length).IsEqual(1);
            if (node.Rarity == PassiveTreeManager.TreeRarity.Magic && node.SkillType == null)
                AssertThat(node.Modifiers.Length).IsEqual(1);
            if (node.Rarity == PassiveTreeManager.TreeRarity.Rare)
                AssertThat(node.Modifiers.Length).IsEqual(2);
            if (effects >= 2)
                comboCount++;
            if (positive && negative)
                tradeoffCount++;
        }
        AssertThat(rarities.Count).IsEqual(4);
        AssertThat(comboCount).IsGreaterEqual(3);
        AssertThat(tradeoffCount).IsGreaterEqual(1);
        AssertThat(PassiveTreeManager.GetMaxStacks("passive_lysosome")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointCost("passive_lysosome")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetMaxStacks("tree_blood_price")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointCost("tree_blood_price")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointCost("tree_assassins_mandate")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointCost("tree_adaptive_overdrive")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointCost("tree_omnipotent_cytoplasm")).IsEqual(1);
        GD.Print("[PASS] The shared tree has 51 validated nodes with rarities, combinations, and trade-offs.");

        string macroStart = PassiveTreeManager.GetStartNode("macrophage");
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", macroStart)).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointCost(macroStart)).IsEqual(1);
        AssertThat(PassiveTreeManager.RefundNode("macrophage", macroStart)).IsFalse();
        AssertThat(PassiveTreeManager.GetSpentPoints("macrophage")).IsEqual(0);
        GD.Print("[PASS] The innate start hub is always active and never consumes points.");

        string prevLang = GameManager.CurrentLanguage;
        GameManager.SetLanguage("en");
        var treeLysosome = GameManager.GetSkillInfo("passive_lysosome");
        var activeLysosome = GameManager.GetSkillInfo("lysosomal_overload");
        AssertThat(treeLysosome["name"].AsString().Contains("Priming")).IsTrue();
        AssertThat(activeLysosome["name"].AsString().Contains("Overload")).IsTrue();
        GameManager.SetLanguage(prevLang);
        GD.Print("[PASS] Tree and active lysosome displays use their distinct names.");

        AssertThat(PassiveTreeManager.GetCellLevel("macrophage")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(0);
        // The start hub is innately lit at 0 cost and can never be bought or refunded
        // (docs/passivetree.md §2 L=0 / §5.1).
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "passive_lysosome")).IsEqual(PassiveTreeManager.InnateStartStacks);
        AssertThat(PassiveTreeManager.Purchase("macrophage", "passive_lysosome")).IsFalse();
        AssertThat(PassiveTreeManager.GetSpentPoints("macrophage")).IsEqual(0);
        GD.Print("[PASS] The macrophage start hub is innately lit and never consumes points.");

        AssertThat(PassiveTreeManager.RecordRunLevel("macrophage", 4)).IsTrue();
        AssertThat(PassiveTreeManager.RecordRunLevel("macrophage", 3)).IsFalse();
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(3);

        AssertThat(PassiveTreeManager.Purchase("macrophage", "tree_thick_cytoplasm")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "tree_lysosomal_appetite")).IsTrue();
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(1);
        AssertThat(PassiveTreeManager.RefundNode("macrophage", "passive_lysosome")).IsFalse();
        AssertThat(PassiveTreeManager.RefundNode("macrophage", "tree_lysosomal_appetite")).IsTrue();
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(2);
        GD.Print("[PASS] Points, the innate start anchor, and adjacency purchases behave correctly.");

        AssertThat(PassiveTreeManager.RecordRunLevel("macrophage", 5)).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "passive_glycolysis")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "passive_actin")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "tree_cytoskeletal_drift")).IsTrue();
        AssertThat(PassiveTreeManager.GetSpentPoints("macrophage")).IsEqual(4);
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(0);
        AssertThat(PassiveTreeManager.Purchase("macrophage", "tree_slipstream")).IsFalse();
        GD.Print("[PASS] Lineages open through the core metabolism ring from any start.");

        AssertThat(PassiveTreeManager.RecordRunLevel("ctl", 5)).IsTrue();
        AssertThat(PassiveTreeManager.GetNodeStacks("ctl", "passive_opsonin")).IsEqual(PassiveTreeManager.InnateStartStacks);
        AssertThat(PassiveTreeManager.Purchase("ctl", "passive_vdj")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("ctl", "tree_assassins_mandate")).IsTrue();
        AssertThat(PassiveTreeManager.RefundNode("ctl", "passive_vdj")).IsFalse();
        AssertThat(PassiveTreeManager.RefundNode("ctl", "tree_assassins_mandate")).IsTrue();
        AssertThat(PassiveTreeManager.RefundNode("ctl", "passive_vdj")).IsTrue();
        GD.Print("[PASS] Refunds preserve a continuous path back to the start node.");

        PassiveTreeManager.ResetAllocation("macrophage");
        AssertThat(PassiveTreeManager.RecordRunLevel("macrophage", 8)).IsTrue();
        AssertThat(PassiveTreeManager.GetMaxStacks("passive_lysosome")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "passive_lysosome")).IsEqual(PassiveTreeManager.InnateStartStacks);
        AssertThat(PassiveTreeManager.Purchase("macrophage", "passive_lysosome")).IsFalse();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "tree_thick_cytoplasm")).IsTrue();
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "tree_thick_cytoplasm")).IsEqual(1);
        AssertThat(PassiveTreeManager.Purchase("macrophage", "tree_thick_cytoplasm")).IsFalse();
        GD.Print("[PASS] Each tree node is bought once and then caps out.");

        PassiveTreeManager.SaveToDisk();
        PassiveTreeManager.ReloadFromDisk();
        AssertThat(PassiveTreeManager.GetCellLevel("macrophage")).IsEqual(8);
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "passive_lysosome")).IsEqual(PassiveTreeManager.InnateStartStacks);
        AssertThat(PassiveTreeManager.Purchase("macrophage", "passive_bilayer")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "tree_iron_membrane")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "tree_rapid_clotting")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "passive_endotoxin")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "tree_second_wind")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "tree_contained_fury")).IsTrue();
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(0);
        AssertThat(PassiveTreeManager.Purchase("macrophage", "passive_autophagy")).IsFalse();
        GD.Print("[PASS] Tree levels and presets persist across reloads.");
    }

    private static void TestGridLayoutAndPurchases()
    {
        PassiveTreeManager.ResetAll();

        int layerOne = 0;
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
            if (node.Ring == 1)
                layerOne++;
        }
        AssertThat(layerOne).IsEqual(4);
        GD.Print("[PASS] Every node sits on its own grid cell with a matching layer.");

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
        AssertThat(PassiveTreeManager.Purchase("dendritic", "passive_chemokine")).IsFalse(); // innate start hub
        AssertThat(PassiveTreeManager.Purchase("dendritic", "tree_far_sense")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "tree_antigen_harvest")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "tree_scavenger_field")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "tree_lucky_mutation")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "tree_patient_observer")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "tree_swarm_cartography")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "tree_risk_assessment")).IsTrue();
        AssertThat(PassiveTreeManager.GetSpentPoints("dendritic")).IsEqual(7);
        GD.Print("[PASS] A deep lineage path can be purchased point by point.");

        string prevLang = GameManager.CurrentLanguage;
        GameManager.SetLanguage("en");
        AssertThat(PassiveTreeManager.GetNodeTooltipText("dendritic", "tree_thick_cytoplasm").Contains("atrophy")).IsFalse();
        GameManager.SetLanguage(prevLang);

        AssertThat(PassiveTreeManager.Purchase("dendritic", "passive_hematopoietic")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "passive_lysosome")).IsTrue();
        AssertThat(PassiveTreeManager.CanPurchase("dendritic", "tree_thick_cytoplasm")).IsTrue();
        GD.Print("[PASS] Cross-lineage portals stay purchasable through the core ring.");
    }

    private static void TestStackedSkillModifiers()
    {
        var host = new CharacterBody2D();
        var stats = new CellStats { Name = "CellStats" };
        host.AddChild(stats);

        var skill = PassiveTreeManager.CreateStackedSkill("passive_opsonin", 2);
        AssertThat(skill).IsNotNull();
        AssertThat(skill!.Level).IsEqual(1);
        host.AddChild(skill);
        skill.Setup(host, -1);
        AssertThat(stats.GetStat("crit_chance")).IsEqualApprox(0.10f, 0.001f);

        host.QueueFree();

        var bundleHost = new CharacterBody2D();
        var bundleStats = new CellStats { Name = "CellStats" };
        bundleHost.AddChild(bundleStats);
        var bundle = PassiveTreeManager.CreateStackedSkill("tree_blood_price", 1);
        AssertThat(bundle is TreeStatBundleSkill).IsTrue();
        bundleHost.AddChild(bundle!);
        bundle!.Setup(bundleHost, -1);
        AssertThat(bundleStats.GetStat("might")).IsEqualApprox(1.08f, 0.001f);
        AssertThat(bundleStats.GetStat("max_health")).IsEqualApprox(100.0f, 0.01f);
        bundleHost.QueueFree();
        GD.Print("[PASS] A normal node bundle applies its single stat modifier.");
    }

    private void SetupMenu()
    {
        PassiveTreeManager.ResetAll();
        GameManager.SetLanguage("en");
        GameManager.SelectedClass = "macrophage";
        GameManager.SelectedMap = "acute_wound";

        var menuScene = GD.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
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
        AssertThat(PassiveTreeManager.TryGetNode("passive_lysosome", out var lysosomeNode)).IsTrue();
        AssertThat(PassiveTreeView.HitTestWorldPosition(lysosomeNode.Position)).IsEqual("passive_lysosome");
        AssertThat(PassiveTreeView.HitTestWorldPosition(PassiveTreeManager.WorldCenter)).IsNull();
        AssertThat(PassiveTreeView.HitTestWorldPosition(new Vector2(-1000, -1000))).IsNull();
        var startButton = _menu.TreeCanvas.GetNodeOrNull<Button>("ZoomRoot/TreeNode_passive_lysosome");
        AssertThat(startButton).IsNotNull();
        AssertThat(startButton!.Text.Contains("🧪")).IsTrue();

        _menu.OnTreeNodeActivated("tree_thick_cytoplasm");
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "tree_thick_cytoplasm")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(2);
        AssertThat(_menu.ActiveTreeNodeId).IsEqual("tree_thick_cytoplasm");
        GD.Print("[PASS] The menu flow purchases and preserves a macrophage tree node.");

        _menu.OnTreeNodeHovered("tree_blood_price");
        AssertThat(_menu.ActiveTreeNodeId).IsEqual("tree_blood_price");

        string tooltip = PassiveTreeManager.GetNodeTooltipText("macrophage", "tree_blood_price");
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

        string hoverNodeId = "tree_blood_price";
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

        AssertThat(PassiveTreeManager.TryGetNode("tree_adaptive_overdrive", out var rareNode)).IsTrue();
        Vector2 rareLocal = treeView.Size * 0.5f
            + (rareNode.Position - treeView.CameraPosition) * treeView.ZoomLevel;
        treeView.UpdateHoverAt(rareLocal);
        AssertThat(tooltipPanel.Visible).IsTrue();
        AssertThat(tooltipLabel.Text.Contains("Overdrive")).IsTrue();
        AssertThat(tooltipLabel.Text.Contains("Rare")).IsFalse();
        AssertThat(tooltipLabel.Text.Contains("Cost")).IsFalse();
        GD.Print("[PASS] Hovering a tree node shows its tooltip immediately (no click, no rarity/cost text).");

        _menu.OnTreeNodeRefundRequested("tree_thick_cytoplasm");
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "tree_thick_cytoplasm")).IsEqual(0);
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(3);
        // The start hub stays innately lit regardless of refunds.
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "passive_lysosome")).IsEqual(PassiveTreeManager.InnateStartStacks);
        AssertThat(PassiveTreeManager.RefundNode("macrophage", "passive_lysosome")).IsFalse();
        _menu.OnTreeNodeActivated("tree_thick_cytoplasm");
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
        AssertThat(PassiveTreeManager.Purchase("ctl", "tree_precise_edge")).IsTrue();

        GameManager.SelectedClass = "ctl";
        var mainScene = GD.Load<PackedScene>("res://scenes/main.tscn");
        AssertThat(mainScene).IsNotNull();
        _main = mainScene!.Instantiate<Main>();
        Root.AddChild(_main);
    }

    private void TestRunIntegration()
    {
        AssertThat(_main).IsNotNull();
        AssertThat(_main!.Player is BaseCell).IsTrue();
        var player = (BaseCell)_main.Player!;

        // CTL base 15% + Opsonin Lv.1 5% + Precise Edge 2% = 22%
        AssertThat(player.Stats!.GetStat("crit_chance")).IsEqualApprox(0.22f, 0.001f);
        AssertThat(player.CellSkillManager!.GetPassiveSlot(0)).IsNull();
        GD.Print("[PASS] A saved tree preset modifies the run without using passive slots.");

        var hud = _main.GetNodeOrNull<Hud>("HUD");
        AssertThat(hud).IsNotNull();
        hud!.ToggleTreeOverlay();
        AssertThat(hud.IsTreeOverlayVisible).IsTrue();
        AssertThat(hud.TreeOverlayText!.Text.Contains("22.0%") || hud.TreeOverlayText!.Text.Contains("0.22")).IsTrue();
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
                    AssertThat(choice["id"].AsString()).IsNotEqual("passive_opsonin");
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
