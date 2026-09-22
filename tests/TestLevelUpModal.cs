using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;
using Phagocyte.Skills;
using Phagocyte.UI;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Phagocyte.Tests;

public partial class TestLevelUpModal : TestHarness
{
    private int _frameCount = 0;
    private bool _testDone = false;
    private bool _arenaIsolated = false;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING LEVEL-UP 3-CHOICE MUTATION SYSTEM VERIFICATION <<<");
        GD.Print("==================================================================");

        // --- Test 1: UpgradeManager Card Generation ---
        var mockPlayer = new CharacterBody2D();
        var stats = new CellStats { Name = "CellStats" };
        mockPlayer.AddChild(stats);

        var sm = new SkillManager { Name = "SkillManager" };
        mockPlayer.AddChild(sm);
        sm.Setup(mockPlayer);

        // Equip Slot 0 with ROS Torrent Lv.1
        var ros = new RosTorrentSkill();
        sm.EquipActive(ros, 0);

        var choices = UpgradeManager.GenerateChoices(mockPlayer, 3);
        AssertThat(choices.Count).IsEqual(3);

        // Ensure all 3 generated choices have distinct IDs
        var ids = new List<string>();
        foreach (var c in choices)
        {
            string id = c["id"].AsString();
            AssertThat(ids.Contains(id)).IsFalse();
            ids.Add(id);
            AssertThat(c.ContainsKey("name") && c.ContainsKey("icon") && c.ContainsKey("desc")).IsTrue();
        }
        GD.Print("[PASS] Test 1: UpgradeManager generated 3 distinct valid cards.");

        // --- Test 2: Applying New Active & New Passive Choices ---
        var newActiveChoice = new Dictionary
        {
            { "type", "new_active" },
            { "id", "perforin_lance" },
            { "skill_class", typeof(PerforinLanceSkill).AssemblyQualifiedName ?? "" }
        };
        bool resActive = UpgradeManager.ApplyChoice(mockPlayer, newActiveChoice);
        AssertThat(resActive).IsTrue();
        AssertThat(sm.GetActiveSlot(1)).IsNotNull();
        AssertThat(sm.GetActiveSlot(1)!.SkillId).IsEqual("perforin_lance");

        var newPassiveChoice = new Dictionary
        {
            { "type", "new_passive" },
            { "id", "actin" },
            { "skill_class", typeof(PassiveActinPolymerization).AssemblyQualifiedName ?? "" }
        };
        bool resPassive = UpgradeManager.ApplyChoice(mockPlayer, newPassiveChoice);
        AssertThat(resPassive).IsTrue();
        AssertThat(sm.GetPassiveSlot(0)).IsNotNull();
        AssertThat(sm.GetPassiveSlot(0)!.SkillId).IsEqual("actin");
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("area"), 1.12f)).IsTrue();
        GD.Print("[PASS] Test 2: Applying new active and new passive choices successfully updates slots & stats.");

        // --- Test 3: Upgrading Existing Skill ---
        var upgradeChoice = new Dictionary
        {
            { "type", "upgrade_passive" },
            { "id", "actin" },
            { "skill_ref", sm.GetPassiveSlot(0)! }
        };
        bool resUp = UpgradeManager.ApplyChoice(mockPlayer, upgradeChoice);
        AssertThat(resUp).IsTrue();
        AssertThat(sm.GetPassiveSlot(0)!.Level).IsEqual(2);
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("area"), 1.24f)).IsTrue();
        GD.Print("[PASS] Test 3: Upgrading existing skill properly increments level and updates stats.");

        // --- Test 4: Slot Overflow Boundaries (Max 5 Actives / Max 5 Passives) ---
        sm.EquipActive(new ComplementCascadeSkill());
        sm.EquipActive(new AntibodySalvoSkill());
        sm.EquipActive(new PseudopodLungeSkill());

        foreach (var s in sm.ActiveSlots)
        {
            AssertThat(s).IsNotNull();
        }

        // Now generate choices: should NEVER have "new_active"
        for (int iter = 0; iter < 5; iter++)
        {
            var fullActiveChoices = UpgradeManager.GenerateChoices(mockPlayer, 3);
            foreach (var c in fullActiveChoices)
            {
                AssertThat(c["type"].AsString()).IsNotEqual("new_active");
            }
        }

        // Fill remaining 4 passive slots to reach 5/5
        sm.EquipPassive(new PassiveLysosomePriming());
        sm.EquipPassive(new PassiveMitochondrialOverclock());
        sm.EquipPassive(new PassiveOpsoninAffinity());
        sm.EquipPassive(new PassiveChemokineReceptors());

        foreach (var s in sm.PassiveSlots)
        {
            AssertThat(s).IsNotNull();
        }

        for (int iter = 0; iter < 5; iter++)
        {
            var fullAllChoices = UpgradeManager.GenerateChoices(mockPlayer, 3);
            foreach (var c in fullAllChoices)
            {
                AssertThat(c["type"].AsString()).IsNotEqual("new_active");
                AssertThat(c["type"].AsString()).IsNotEqual("new_passive");
            }
        }
        GD.Print("[PASS] Test 4: Slot overflow boundaries (5/5 Actives, 5/5 Passives) strictly enforced.");

        mockPlayer.QueueFree();

        // --- Test 5: In-game Runtime Integration with Main Scene ---
        var mainScene = AssetLoader.Load<PackedScene>("res://scenes/main.tscn");
        if (mainScene == null)
        {
            GD.PrintErr("[FAIL] Could not load main.tscn");
            Quit(1);
            return;
        }
        var main = mainScene.Instantiate();
        Root.AddChild(main);
    }

    private static void ClearArenaEntities(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is BaseEnemy || child is SenescentRBC || child is DormantToxinVesicle || child is BioHazardArea)
                child.Free();
            else
                ClearArenaEntities(child);
        }
    }

    /// <summary>
    /// Freeze the spawner driver, clear everything Main._Ready spawned and
    /// restore the pristine player baseline. Must run on a live frame: nodes
    /// added during MainLoop._Initialize don't enter the tree (no _Ready)
    /// until the first iteration, so _Initialize-time isolation is a no-op.
    /// Afterwards nothing can accrue ambient EXP/damage/kills.
    /// </summary>
    private void IsolateArena()
    {
        var main = Root.GetNodeOrNull<Main>("Main");
        if (main == null)
            return;
        main.SetPhysicsProcess(false);
        ClearArenaEntities(main);
        var player = (main.Player ?? main.GetNodeOrNull<BaseCell>("Macrophage")) as BaseCell;
        if (player != null)
        {
            player.CurrentLevel = 1;
            player.CurrentExp = 0.0f;
            player.ExpToNextLevel = 30.0f;
            player.Health = player.MaxHealth;
        }
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        if (!_arenaIsolated)
        {
            _arenaIsolated = true;
            IsolateArena();
        }

        _frameCount++;
        if (_frameCount < 4)
            return false;

        _testDone = true;
        var main = Root.GetNodeOrNull<Main>("Main");
        if (main == null)
        {
            GD.PrintErr("[FAIL] Main scene not found");
            Quit(1);
            return true;
        }

        var player = (main.Player ?? main.GetNodeOrNull<BaseCell>("Macrophage")) as BaseCell;
        if (player == null)
        {
            GD.PrintErr("[FAIL] Macrophage not found");
            Quit(1);
            return true;
        }

        var hud = main.HudNode ?? main.GetNodeOrNull<Hud>("HUD");
        if (hud == null)
        {
            GD.PrintErr("[FAIL] HUD not found");
            Quit(1);
            return true;
        }

        AssertThat(hud.CellUpgradeModal).IsNotNull();
        AssertThat(hud.CellUpgradeModal!.Visible).IsFalse();
        AssertThat(Paused).IsFalse();

        // The first level-up normally plays the 0.5s bullet-time cue (docs/tutorial.md Â§2);
        // this runtime check exercises the draft flow directly.
        hud.SkipLevelUpBulletTime = true;

        // Initial level checks
        AssertThat(player.CurrentLevel).IsEqual(1);
        AssertThat(player.CurrentExp).IsEqual(0.0f);

        // Trigger Level-Up by awarding enough EXP
        player.AddExp(35.0f);

        AssertThat(player.CurrentLevel).IsEqual(2);
        AssertThat(hud.FirstLevelUpCuePlayed).IsTrue();
        AssertThat(hud.CellUpgradeModal.Visible).IsTrue();
        AssertThat(Paused).IsTrue();

        // Verify 3 cards populated
        var cardContainer = hud.CellUpgradeModal.CardsContainer;
        AssertThat(cardContainer).IsNotNull();
        AssertThat(cardContainer!.GetChildCount()).IsEqual(3);
        for (int i = 0; i < 3; i++)
        {
            AssertThat(cardContainer.GetChild<Control>(i).Visible).IsTrue();
        }

        hud.CellUpgradeModal.OnCardClicked(0);

        AssertThat(hud.CellUpgradeModal.Visible).IsFalse();
        AssertThat(Paused).IsFalse();

        main.QueueFree();

        GD.Print("[PASS] Test 5: In-game Level-Up trigger, pause, modal display, card selection & resume verified.");
        GD.Print("==================================================================");
        GD.Print(">>> ALL LEVEL-UP 3-CHOICE TESTS PASSED SUCCESSFULLY! <<<");
        GD.Print("==================================================================");
        Quit(0);
        return true;
    }
}
