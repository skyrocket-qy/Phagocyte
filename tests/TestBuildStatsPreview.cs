using Godot;
using System;
using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Skills;
using Phagocyte.UI;

namespace Phagocyte.Tests;

/// <summary>
/// Build stats preview: menu totals must equal run-assembled stats (same
/// rules, two paths), panel refreshes on every build change, and the in-run
/// C overlay fully pauses. Uses deterministic fixtures: redox_symbiont
/// (generator, might +10%), thick_cytoplasm + iron_membrane (1-cost lysosome
/// neighbors), macrophage level 6.
/// </summary>
[TestSuite]
public partial class TestBuildStatsPreview : TestHarness
{
    private int _phase = 0;
    private int _frameCount = 0;
    private Node2D? _arena;
    private MainMenu? _menu;

    private static readonly string[] AllStatKeys =
    {
        "might", "area", "cooldown_reduction", "projectile_speed", "duration",
        "amount", "pierce", "knockback", "crit_chance", "crit_damage", "ailment_damage",
        "max_health", "health_regen", "armor", "move_speed", "evasion", "block", "life_steal",
        "magnet"
    };

    public override void _Initialize()
    {
        Banner("STARTING BUILD STATS PREVIEW VERIFICATION");
        IsolateSaves("build_stats");
    }

    public override bool _Process(double delta)
    {
        try
        {
            switch (_phase)
            {
                case 0:
                    _frameCount++;
                    if (_frameCount < 3)
                        return false;
                    _frameCount = 0;
                    SetupBuild();
                    StageRunMirror();
                    StageMenu();
                    _phase = 1;
                    return false;

                case 1:
                    _frameCount++;
                    if (_frameCount < 5)
                        return false;
                    _frameCount = 0;
                    AssertPreviewMatchesRunAssembly();
                    AssertMenuRefreshWiring();
                    AssertOverlayPauses();
                    Teardown();
                    Finish(true, "ALL BUILD STATS PREVIEW TESTS");
                    return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestBuildStatsPreview threw: ", ex);
            Teardown();
            Finish(false, "ALL BUILD STATS PREVIEW TESTS");
            return true;
        }
        return false;
    }

    private void SetupBuild()
    {
        TestCheats.LockToBaseline();
        GearUnlockManager.UnlockAll();
        AssertThat(PassiveTreeManager.RecordRunLevel("macrophage", 6)).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "thick_cytoplasm")).IsTrue();
        AssertThat(PassiveTreeManager.Purchase("macrophage", "iron_membrane")).IsTrue();
        int profile = LoadoutManager.GetActiveProfile("macrophage");
        AssertThat(LoadoutManager.SetSlots("macrophage", profile,
            new[] { "redox_symbiont", "", "", "" })).IsTrue();
        GD.Print("[PASS] Test 0: deterministic build staged (generator + 2 tree nodes).");
    }

    private Macrophage? _player;

    private void StageRunMirror()
    {
        // Run-side mirror: real player + Main's loadout/tree application order.
        _arena = new Node2D { Name = "PreviewArena" };
        Root.AddChild(_arena);
        var playerScene = GameManager.GetCellScene("macrophage");
        _player = playerScene.Instantiate<Macrophage>();
        _arena.AddChild(_player);
        _player.SetPhysicsProcess(false);
    }

    private void StageMenu()
    {
        var menuScene = AssetLoader.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
        AssertThat(menuScene).IsNotNull();
        _menu = menuScene!.Instantiate<MainMenu>();
        Root.AddChild(_menu);
    }

    private void AssertPreviewMatchesRunAssembly()
    {
        var preview = BuildStatsPreview.PreviewStats("macrophage");
        AssertThat(preview.Count).IsEqual(AllStatKeys.Length);
        AssertThat(preview["might"]).IsGreater(1.0f);
        GD.Print($"[PASS] Test 1: preview totals computed (might={preview["might"]:F3}).");

        AssertThat(_player).IsNotNull();
        AssertThat(GodotObject.IsInstanceValid(_player)).IsTrue();
        var player = _player!;
        string[] slots = LoadoutManager.GetActiveSlots("macrophage");
        var chamber = player.CellGearChamber;
        AssertThat(chamber).IsNotNull();
        for (int i = 0; i < slots.Length && i < GearChamber.MaxSlots; i++)
        {
            string id = slots[i];
            if (string.IsNullOrEmpty(id) || !GearUnlockManager.IsUnlocked(id))
                continue;
            if (!chamber!.AddToBackpack(id))
                continue;
            chamber.Equip(id, i);
        }
        foreach (var node in PassiveTreeManager.Nodes)
        {
            if (!PassiveTreeManager.IsPlaced("macrophage", node.Id))
                continue;
            var skill = PassiveTreeManager.CreateSkill(node.Id);
            if (skill == null)
                continue;
            player.AddChild(skill);
            skill.Setup(player);
        }

        AssertThat(player.Stats).IsNotNull();
        foreach (string key in AllStatKeys)
        {
            AssertThat(player.Stats!.GetStat(key)).IsEqualApprox(preview[key], 0.001f);
        }
        GD.Print("[PASS] Test 2: preview equals run-assembled stats on all 19 keys.");
    }

    private void AssertMenuRefreshWiring()
    {
        AssertThat(_menu).IsNotNull();
        AssertThat(GodotObject.IsInstanceValid(_menu)).IsTrue();
        _menu!.OnStartPressed();
        _menu.OnClassConfirmPressed();
        var loadout = _menu.LoadoutView;
        AssertThat(loadout).IsNotNull();
        AssertThat(loadout!.StatPanel).IsNotNull();

        string before = StatValue(loadout.StatPanel!, "might");
        AssertThat(loadout.ToggleGear("redox_symbiont")).IsTrue();
        string unequipped = StatValue(loadout.StatPanel!, "might");
        AssertThat(unequipped).IsNotEqual(before);
        AssertThat(loadout.ToggleGear("redox_symbiont")).IsTrue();
        AssertThat(StatValue(loadout.StatPanel!, "might")).IsEqual(before);
        GD.Print($"[PASS] Test 3: loadout panel refreshes on equip/unequip (might {unequipped} <-> {before}).");

        _menu.OnLoadoutConfirmPressed();
        AssertThat(_menu.TreeStatPanel).IsNotNull();
        string treeBefore = StatValue(_menu.TreeStatPanel!, "max_health");
        _menu.OnTreeNodeRefundRequested("thick_cytoplasm");
        AssertThat(StatValue(_menu.TreeStatPanel!, "max_health")).IsNotEqual(treeBefore);
        GD.Print("[PASS] Test 4: tree panel refreshes on refund.");
    }

    private void AssertOverlayPauses()
    {
        var view = new PauseMenuView { Name = "PauseProbe" };
        var panel = new PanelContainer { Name = "TreeOverlayPanel", Visible = false };
        Root.AddChild(panel);
        Root.AddChild(view);
        view.TreeOverlayPanel = panel;

        AssertThat(Paused).IsFalse();
        view.ToggleTreeOverlay();
        AssertThat(panel.Visible).IsTrue();
        AssertThat(Paused).IsTrue();
        view.ToggleTreeOverlay();
        AssertThat(panel.Visible).IsFalse();
        AssertThat(Paused).IsFalse();
        GD.Print("[PASS] Test 5: C overlay pauses on open, restores on close.");

        // Stacked holds: a draft opening over the overlay must survive it.
        view.ToggleTreeOverlay();
        PauseManager.PushHold(this, PauseManager.UpgradeDraft);
        view.ToggleTreeOverlay();
        AssertThat(panel.Visible).IsFalse();
        AssertThat(Paused).IsTrue();
        PauseManager.PopHold(this, PauseManager.UpgradeDraft);
        AssertThat(Paused).IsFalse();
        GD.Print("[PASS] Test 6: draft close no longer stomps the overlay pause.");
    }

    private static string StatValue(StatPreviewPanel panel, string key)
    {
        var label = panel.GetNodeOrNull<Label>($"Margin/VBox/StatScroll/GroupsBox/Row_{key}/Value");
        AssertThat(label).IsNotNull();
        return label!.Text;
    }

    private void Teardown()
    {
        if (_menu != null && GodotObject.IsInstanceValid(_menu))
            _menu.QueueFree();
        _menu = null;
        if (_arena != null && GodotObject.IsInstanceValid(_arena))
            _arena.QueueFree();
        _arena = null;
        Paused = false;
        PauseManager.Clear(this);
        TestCheats.LockToBaseline();
        RestoreSaves();
    }
}
