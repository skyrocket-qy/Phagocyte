using Godot;
using System;
using Phagocyte.UI;
using GdUnit4;
using static GdUnit4.Assertions;

using Phagocyte.Core;

namespace Phagocyte.Tests;

public partial class TestCardUniformSize : SceneTree
{
    private int _frameCount = 0;
    private bool _testDone = false;

    public override void _Initialize()
    {
        GD.Print("--- TESTING UPGRADE MODAL CARD SIZE UNIFORMITY ---");
        var modalScene = AssetLoader.Load<PackedScene>("res://scenes/ui/upgrade_modal.tscn");
        if (modalScene == null)
        {
            GD.PrintErr("Failed to load upgrade_modal.tscn");
            Quit(1);
            return;
        }
        var modal = modalScene.Instantiate();
        Root.AddChild(modal);
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        _frameCount++;
        if (_frameCount < 3)
            return false;

        _testDone = true;
        var modal = Root.GetNodeOrNull<UpgradeModal>("UpgradeModal");
        if (modal == null)
        {
            GD.PrintErr("Modal not found");
            Quit(1);
            return true;
        }

        var cardsContainer = modal.GetNodeOrNull<HBoxContainer>("CenterContainer/VBox/CardsContainer");
        AssertThat(cardsContainer).IsNotNull();
        var cards = cardsContainer!.GetChildren();
        AssertThat(cards.Count).IsEqual(3);

        // Card 0: Tiny text
        cards[0].GetNode<Label>("VBox/TitleLabel").Text = "短";
        cards[0].GetNode<Label>("VBox/BadgeLabel").Text = "[ 升 ]";
        cards[0].GetNode<Label>("VBox/DescLabel").Text = "微量效果";

        // Card 1: Very long English and Chinese text
        cards[1].GetNode<Label>("VBox/TitleLabel").Text = "線粒體超頻呼吸 (Mitochondrial Overclock Cascade Ultra)";
        cards[1].GetNode<Label>("VBox/BadgeLabel").Text = "[ NEW PASSIVE ORGANELLE LEVEL 5 ]";
        cards[1].GetNode<Label>("VBox/DescLabel").Text = "Cooldown Reduction +8% (max 75%), Skill Duration +10% per level. Highly accelerated oxidative phosphorylation turnover rate in mitochondrial matrix.";

        // Card 2: Standard medium text
        cards[2].GetNode<Label>("VBox/TitleLabel").Text = "活性氧射流 (ROS Torrent)";
        cards[2].GetNode<Label>("VBox/BadgeLabel").Text = "[ 新主动武器 ]";
        cards[2].GetNode<Label>("VBox/DescLabel").Text = "向前方喷射高压酸雾，造成持续破甲腐蚀。";

        // Force layout update
        modal.GetNode<CenterContainer>("CenterContainer").QueueSort();

        // Check min sizes
        var c0 = (Control)cards[0];
        var c1 = (Control)cards[1];
        var c2 = (Control)cards[2];

        var s0 = c0.GetCombinedMinimumSize();
        var s1 = c1.GetCombinedMinimumSize();
        var s2 = c2.GetCombinedMinimumSize();

        GD.Print("Card 0 min size: ", s0);
        GD.Print("Card 1 min size: ", s1);
        GD.Print("Card 2 min size: ", s2);

        AssertThat(Mathf.IsEqualApprox(s0.X, s1.X) && Mathf.IsEqualApprox(s1.X, s2.X)).IsTrue();
        AssertThat(Mathf.IsEqualApprox(s0.Y, s1.Y) && Mathf.IsEqualApprox(s1.Y, s2.Y)).IsTrue();

        modal.QueueFree();
        GD.Print("[PASS] All 3 cards have 100% IDENTICAL dimensions!");
        Quit(0);
        return true;
    }
}
