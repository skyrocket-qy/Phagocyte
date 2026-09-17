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
public partial class TestPassiveTree : SceneTree
{
    private int _phase = 0;
    private int _frameCount = 0;
    private MainMenu? _menu = null;
    private Main? _main = null;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING SHARED PASSIVE TREE VERIFICATION <<<");
        GD.Print("==================================================================");
    }

    public override bool _Process(double delta)
    {
        try
        {
            switch (_phase)
            {
                case 0:
                    TestTreeEconomyAndPersistence();
                    TestPolarLayoutAndAutophagy();
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
        AssertThat(starts.Contains(PassiveTreeManager.NucleusNodeId)).IsFalse();
        GD.Print("[PASS] All five cells begin at distinct nodes on one shared tree.");

        AssertThat(PassiveTreeManager.Nodes.Length).IsGreaterEqual(52);
        var nodeIds = new List<string>();
        int tradeoffCount = 0;
        int comboCount = 0;
        var rarities = new List<PassiveTreeManager.TreeRarity>();
        string[] validStats =
        {
            "might", "area", "cooldown_reduction", "projectile_speed", "duration", "amount",
            "pierce", "knockback", "crit_chance", "crit_damage", "max_health", "health_regen",
            "armor", "move_speed", "revival", "knockback_resist", "magnet", "growth", "luck", "curse"
        };
        foreach (var node in PassiveTreeManager.Nodes)
        {
            bool isNucleus = PassiveTreeManager.IsNucleus(node.Id);
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
            if (!isNucleus)
                AssertThat(effects >= 1).IsTrue();
            if (effects >= 2)
                comboCount++;
            if (positive && negative)
                tradeoffCount++;
        }
        AssertThat(rarities.Count).IsEqual(4);
        AssertThat(comboCount).IsGreaterEqual(30);
        AssertThat(tradeoffCount).IsGreaterEqual(20);
        AssertThat(PassiveTreeManager.GetMaxStacks("passive_lysosome")).IsEqual(5);
        AssertThat(PassiveTreeManager.GetPointCost("passive_lysosome")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetMaxStacks("tree_blood_price")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointCost("tree_blood_price")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointCost("tree_assassins_mandate")).IsEqual(2);
        AssertThat(PassiveTreeManager.GetPointCost("tree_adaptive_overdrive")).IsEqual(3);
        AssertThat(PassiveTreeManager.GetPointCost("tree_omnipotent_cytoplasm")).IsEqual(5);
        GD.Print("[PASS] The shared tree has 52 validated nodes with rarities, combinations, and trade-offs.");

        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", PassiveTreeManager.NucleusNodeId)).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointCost(PassiveTreeManager.NucleusNodeId)).IsEqual(0);
        AssertThat(PassiveTreeManager.GetNeighbors(PassiveTreeManager.NucleusNodeId).Count).IsEqual(9);
        AssertThat(PassiveTreeManager.RefundNode("macrophage", PassiveTreeManager.NucleusNodeId)).IsFalse();
        AssertThat(PassiveTreeManager.GetSpentPoints("macrophage")).IsEqual(0);
        GD.Print("[PASS] The innate HSC nucleus is always active and never consumes points.");

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
        AssertThat(PassiveTreeManager.Purchase("macrophage", "passive_lysosome")).IsFalse();
        AssertThat(PassiveTreeManager.RecordRunLevel("macrophage", 4)).IsTrue();
        AssertThat(PassiveTreeManager.RecordRunLevel("macrophage", 3)).IsFalse();
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(3);

        AssertThat(PassiveTreeManager.Purchase("macrophage", "passive_lysosome")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "tree_thick_cytoplasm")).IsTrue();
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(1);
        AssertThat(PassiveTreeManager.RefundNode("macrophage", "passive_lysosome")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "tree_lysosomal_appetite")).IsTrue();
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(1);
        GD.Print("[PASS] Points, start anchors, and adjacency purchases behave correctly.");

        AssertThat(PassiveTreeManager.Purchase("macrophage", "passive_actin")).IsTrue();
        AssertThat(PassiveTreeManager.GetBranchInvestment("macrophage", "motility")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetBranchInvestment("macrophage", "vitality")).IsEqual(2);
        AssertThat(PassiveTreeManager.Purchase("macrophage", "tree_cytoskeletal_drift")).IsFalse();
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(0);
        GD.Print("[PASS] The living nucleus lets any lineage portal be opened from the start.");

        AssertThat(PassiveTreeManager.RecordRunLevel("ctl", 5)).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("ctl", "passive_opsonin")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("ctl", "passive_vdj")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("ctl", "tree_assassins_mandate")).IsTrue();
        AssertThat(PassiveTreeManager.RefundNode("ctl", "passive_vdj")).IsFalse();
        AssertThat(PassiveTreeManager.RefundNode("ctl", "tree_assassins_mandate")).IsTrue();
        AssertThat(PassiveTreeManager.RefundNode("ctl", "passive_vdj")).IsTrue();
        GD.Print("[PASS] Refunds preserve a continuous path back to the start node.");

        PassiveTreeManager.ResetAllocation("macrophage");
        AssertThat(PassiveTreeManager.RecordRunLevel("macrophage", 8)).IsTrue();
        int lysosomeCap = PassiveTreeManager.GetMaxStacks("passive_lysosome");
        for (int i = 0; i < lysosomeCap; i++)
            AssertThat(PassiveTreeManager.Purchase("macrophage", "passive_lysosome")).IsTrue();
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "passive_lysosome")).IsEqual(lysosomeCap);
        AssertThat(PassiveTreeManager.Purchase("macrophage", "passive_lysosome")).IsFalse();
        GD.Print("[PASS] Tree nodes stack to the configured maximum and then stop.");

        PassiveTreeManager.SaveToDisk();
        PassiveTreeManager.ReloadFromDisk();
        AssertThat(PassiveTreeManager.GetCellLevel("macrophage")).IsEqual(8);
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "passive_lysosome")).IsEqual(lysosomeCap);
        AssertThat(PassiveTreeManager.Purchase("macrophage", "passive_bilayer")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "passive_autophagy")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "passive_hematopoietic")).IsFalse();
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(0);
        GD.Print("[PASS] Tree levels and presets persist across reloads.");
    }

    private static void TestPolarLayoutAndAutophagy()
    {
        PassiveTreeManager.ResetAll();

        int ringOne = 0;
        foreach (var node in PassiveTreeManager.Nodes)
        {
            AssertThat(node.Ring).IsGreaterEqual(0);
            AssertThat(node.Ring).IsLessEqual(5);
            AssertThat(node.Position.DistanceTo(PassiveTreeManager.WorldCenter))
                .IsEqualApprox(PassiveTreeManager.GetRingRadius(node.Ring), 0.5f);
            if (node.Ring == 1)
                ringOne++;
            if (!PassiveTreeManager.IsNucleus(node.Id) && node.Branch != "core")
            {
                float baseAngle = PassiveTreeManager.BranchBaseAngles[node.Branch];
                float angle = Mathf.RadToDeg((node.Position - PassiveTreeManager.WorldCenter).Angle());
                float delta = Mathf.Abs(Mathf.Wrap(angle - baseAngle, -180.0f, 180.0f));
                AssertThat(delta).IsLessEqual(31.0f);
            }
        }
        AssertThat(ringOne).IsEqual(9);
        GD.Print("[PASS] Every node sits on a polar ring inside its lineage sector.");

        AssertThat(PassiveTreeManager.RecordRunLevel("dendritic", 25)).IsTrue();
        for (int i = 0; i < 4; i++)
            AssertThat(PassiveTreeManager.Purchase("dendritic", "passive_chemokine")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "tree_far_sense")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "tree_antigen_harvest")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "tree_scavenger_field")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "tree_lucky_mutation")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "tree_patient_observer")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "tree_swarm_cartography")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("dendritic", "tree_risk_assessment")).IsTrue();
        AssertThat(PassiveTreeManager.GetSpentPoints("dendritic")).IsEqual(12);
        AssertThat(PassiveTreeManager.GetAtrophyThreshold("dendritic")).IsEqual(2);
        GD.Print("[PASS] Committing twelve points raises autophagic pressure to the second ring.");

        var atrophic = PassiveTreeManager.GetAtrophicNodes("dendritic");
        AssertThat(atrophic.Contains(PassiveTreeManager.NucleusNodeId)).IsFalse();
        AssertThat(atrophic.Contains("passive_chemokine")).IsFalse();
        AssertThat(atrophic.Contains("tree_thick_cytoplasm")).IsTrue();
        AssertThat(atrophic.Contains("tree_bulwark_metabolism")).IsTrue();
        AssertThat(atrophic.Contains("tree_rolling_thunder")).IsTrue();
        AssertThat(PassiveTreeManager.GetNodeAtrophy("dendritic", "tree_thick_cytoplasm")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetNodeAtrophy("dendritic", "tree_bulwark_metabolism")).IsEqual(2);
        AssertThat(PassiveTreeManager.IsAtrophic("dendritic", "tree_rolling_thunder")).IsTrue();
        AssertThat(PassiveTreeManager.CanPurchase("dendritic", "tree_thick_cytoplasm")).IsFalse();
        string prevLang = GameManager.CurrentLanguage;
        GameManager.SetLanguage("en");
        AssertThat(PassiveTreeManager.GetNodeName(PassiveTreeManager.NucleusNodeId).Contains("HSC")).IsTrue();
        AssertThat(PassiveTreeManager.GetNodeTooltipText("dendritic", "tree_thick_cytoplasm").Contains("atrophy")).IsTrue();
        AssertThat(PassiveTreeManager.GetNodeTooltipText("dendritic", PassiveTreeManager.NucleusNodeId).Contains("Innate")).IsTrue();
        GameManager.SetLanguage(prevLang);
        GD.Print("[PASS] Uncommitted lineages autophagocytose from the membrane inward.");

        AssertThat(PassiveTreeManager.Purchase("dendritic", "passive_lysosome")).IsTrue();
        AssertThat(PassiveTreeManager.IsAtrophic("dendritic", "tree_thick_cytoplasm")).IsFalse();
        AssertThat(PassiveTreeManager.CanPurchase("dendritic", "tree_thick_cytoplasm")).IsTrue();
        AssertThat(PassiveTreeManager.GetBranchInvestment("dendritic", "vitality")).IsEqual(1);
        GD.Print("[PASS] Opening a lineage portal restores its silenced nodes.");
    }

    private static void TestStackedSkillModifiers()
    {
        var host = new CharacterBody2D();
        var stats = new CellStats { Name = "CellStats" };
        host.AddChild(stats);

        var skill = PassiveTreeManager.CreateStackedSkill("passive_opsonin", 2);
        AssertThat(skill).IsNotNull();
        AssertThat(skill!.Level).IsEqual(2);
        host.AddChild(skill);
        skill.Setup(host, -1);
        AssertThat(stats.GetStat("crit_chance")).IsEqualApprox(0.15f, 0.001f);

        host.QueueFree();

        var tradeoffHost = new CharacterBody2D();
        var tradeoffStats = new CellStats { Name = "CellStats" };
        tradeoffHost.AddChild(tradeoffStats);
        var bundle = PassiveTreeManager.CreateStackedSkill("tree_blood_price", 1);
        AssertThat(bundle is TreeStatBundleSkill).IsTrue();
        tradeoffHost.AddChild(bundle!);
        bundle!.Setup(tradeoffHost, -1);
        AssertThat(tradeoffStats.GetStat("might")).IsEqualApprox(1.08f, 0.001f);
        AssertThat(tradeoffStats.GetStat("max_health")).IsEqualApprox(93.0f, 0.01f);
        tradeoffHost.QueueFree();
        GD.Print("[PASS] A generic tree bundle applies both sides of a stat trade-off.");
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
        AssertThat(PassiveTreeView.HitTestWorldPosition(PassiveTreeManager.WorldCenter)).IsEqual(PassiveTreeManager.NucleusNodeId);
        AssertThat(PassiveTreeView.HitTestWorldPosition(new Vector2(-1000, -1000))).IsNull();
        var startButton = _menu.TreeCanvas.GetNodeOrNull<Button>("ZoomRoot/TreeNode_passive_lysosome");
        AssertThat(startButton).IsNotNull();
        AssertThat(startButton!.Text.Contains("🧪")).IsTrue();
        var nucleusButton = _menu.TreeCanvas.GetNodeOrNull<Button>("ZoomRoot/TreeNode_" + PassiveTreeManager.NucleusNodeId);
        AssertThat(nucleusButton).IsNotNull();
        AssertThat(nucleusButton!.Text.Contains("🧫")).IsTrue();

        _menu.OnTreeNodeActivated("passive_lysosome");
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "passive_lysosome")).IsEqual(1);
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(2);
        AssertThat(_menu.ActiveTreeNodeId).IsEqual("passive_lysosome");
        AssertThat(_menu.TreeStatusLbl!.Text.Contains("Lysosome")).IsTrue();
        GD.Print("[PASS] The menu flow purchases and preserves a macrophage tree node.");

        _menu.OnTreeNodeHovered("tree_blood_price");
        AssertThat(_menu.ActiveTreeNodeId).IsEqual("tree_blood_price");
        AssertThat(_menu.TreeStatusLbl.Text.Contains("Blood Price")).IsTrue();

        string tooltip = PassiveTreeManager.GetNodeTooltipText("macrophage", "tree_blood_price");
        AssertThat(tooltip.Contains("Blood Price")).IsTrue();
        AssertThat(tooltip.Contains("Normal")).IsTrue();
        AssertThat(tooltip.Contains("Cost: 1 point(s)")).IsTrue();
        AssertThat(tooltip.Contains("+8% Might")).IsTrue();
        AssertThat(tooltip.Contains("-7% Max Health")).IsTrue();
        GD.Print("[PASS] Hover tooltips expose rarity, cost, combinations, and trade-offs.");

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

        _menu.OnTreeNodeRefundRequested("passive_lysosome");
        AssertThat(PassiveTreeManager.GetNodeStacks("macrophage", "passive_lysosome")).IsEqual(0);
        AssertThat(PassiveTreeManager.GetPointsAvailable("macrophage")).IsEqual(3);
        _menu.OnTreeNodeActivated("passive_lysosome");
        _menu.OnPassiveConfirmPressed();
        AssertThat(_menu.PassiveView.Visible).IsFalse();
        AssertThat(_menu.MapView.Visible).IsTrue();
        GD.Print("[PASS] Refunds and the tree-to-map transition behave correctly.");
    }

    private void SetupRun()
    {
        PassiveTreeManager.ResetAll();
        AssertThat(PassiveTreeManager.RecordRunLevel("ctl", 5)).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("ctl", "passive_opsonin")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("ctl", "passive_opsonin")).IsTrue();
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

        AssertThat(player.Stats!.GetStat("crit_chance")).IsEqualApprox(0.17f, 0.001f);
        AssertThat(player.CellSkillManager!.GetPassiveSlot(0)).IsNull();
        GD.Print("[PASS] A saved tree preset modifies the run without using passive slots.");

        var hud = _main.GetNodeOrNull<Hud>("HUD");
        AssertThat(hud).IsNotNull();
        hud!.ToggleTreeOverlay();
        AssertThat(hud.IsTreeOverlayVisible).IsTrue();
        AssertThat(hud.TreeOverlayText).IsNotNull();
        AssertThat(hud.TreeOverlayText!.Text.Contains("Critical Chance: 0.17")).IsTrue();
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
        if (_main != null && IsInstanceValid(_main))
            _main.QueueFree();
        if (_menu != null && IsInstanceValid(_menu))
            _menu.QueueFree();

        GameManager.SelectedClass = "macrophage";
        GameManager.SetLanguage("zh_CN");
        PassiveTreeManager.ResetAll();
    }
}
