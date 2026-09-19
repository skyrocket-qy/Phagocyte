using Godot;
using System;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Skills;
using Phagocyte.UI;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Phagocyte.Tests;

public partial class TestCodexAndTooltip : TestHarness
{
    private int _framesWaited = 0;
    private bool _testDone = false;

    public override void _Initialize()
    {
        GD.Print("--- BEGINNING CODEX & TOOLTIP AUTOMATED VERIFICATION ---");
        var mainScene = GD.Load<PackedScene>("res://scenes/main.tscn");
        if (mainScene == null)
        {
            GD.PrintErr("[FAIL] Failed to load main.tscn");
            Quit(1);
            return;
        }
        var main = mainScene.Instantiate();
        Root.AddChild(main);
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        _framesWaited++;
        if (_framesWaited < 4)
            return false;

        _testDone = true;
        var main = Root.GetNodeOrNull<Node>("Main");
        if (main == null)
        {
            GD.PrintErr("[FAIL] Main scene not found");
            Quit(1);
            return true;
        }

        var hud = main.GetNodeOrNull<Hud>("HUD");
        if (hud == null)
        {
            GD.PrintErr("[FAIL] HUD not found");
            Quit(1);
            return true;
        }

        // 1. Verify Catalogs in GameManager
        AssertThat(GameManager.SkillCatalog.Count >= 9).IsTrue();
        AssertThat(GameManager.SkillCatalog.ContainsKey("macrophage_pseudopods")).IsTrue();
        AssertThat(GameManager.SkillCatalog.ContainsKey("ros_torrent")).IsTrue();
        GD.Print("[PASS] GameManager.SkillCatalog contains complete skill definitions.");

        AssertThat(GameManager.PathogenCatalog.Count >= 20).IsTrue();
        GD.Print($"[PASS] GameManager.PathogenCatalog contains {GameManager.PathogenCatalog.Count} complete pathogen definitions.");

        // 2. Verify In-Game Skill Tooltip
        var skillTooltip = hud.SkillTooltip;
        AssertThat(skillTooltip).IsNotNull();

        // Macrophage starts with the innate passive in slot 0; draft a ranged weapon to test the active tooltip
        var player = main.GetNodeOrNull<BaseCell>("Macrophage");
        AssertThat(player).IsNotNull();
        AssertThat(player!.CellSkillManager!.EquipActive(new RosTorrentSkill(), 0)).IsTrue();
        hud.UpdateSkillSlots();

        var slotsContainer = hud.SlotsContainer;
        AssertThat(slotsContainer != null && slotsContainer.GetChildCount() >= 6).IsTrue();

        // Test Hover Slot 0 (Active Weapon)
        var card0 = slotsContainer!.GetChild<Control>(0);
        hud.OnSlotMouseEntered(0, card0);
        AssertThat(skillTooltip!.Visible).IsTrue();

        var tIcon = hud.TooltipIcon?.Text ?? "";
        var tTitle = hud.TooltipTitle?.Text ?? "";
        var tBadge = hud.TooltipBadge?.Text ?? "";
        AssertThat(tIcon).IsEqual("ðŸ’¨");
        AssertThat(tBadge.Contains("ä¸»åŠ¨") || tBadge.Contains("ACTIVE")).IsTrue();
        AssertThat(hud.TooltipStats?.Text.Contains("3.2") ?? false).IsTrue();
        GD.Print($"[PASS] Slot 0 Active Weapon Tooltip verified: '{tIcon} {tTitle}' ({tBadge})");

        // Test Hover Slot 5 (Innate Passive å¾®çµ²è®Šå½¢)
        var card5 = slotsContainer!.GetChild<Control>(5);
        hud.OnSlotMouseEntered(5, card5);
        AssertThat(skillTooltip.Visible).IsTrue();
        AssertThat(hud.TooltipIcon?.Text).IsEqual("ðŸ¦ ");
        var innateBadge = hud.TooltipBadge?.Text ?? "";
        AssertThat(innateBadge.Contains("å›ºæœ‰") || innateBadge.Contains("INNATE")).IsTrue();
        GD.Print($"[PASS] Slot 5 Innate Passive Tooltip verified: '{hud.TooltipIcon?.Text} {hud.TooltipTitle?.Text}' ({innateBadge})");

        // Test Hover Slot 1 (Empty Slot)
        var card1 = slotsContainer.GetChild<Control>(1);
        hud.OnSlotMouseEntered(1, card1);
        AssertThat(hud.TooltipIcon?.Text).IsEqual("+");
        AssertThat(hud.TooltipBio != null && hud.TooltipBio.Visible).IsFalse();
        GD.Print($"[PASS] Slot 1 Empty Slot Tooltip verified: '{hud.TooltipIcon?.Text} {hud.TooltipTitle?.Text}'");

        // Test Hover Slot 2 (Empty Slot)
        var card2 = slotsContainer.GetChild<Control>(2);
        hud.OnSlotMouseEntered(2, card2);
        AssertThat(hud.TooltipIcon?.Text).IsEqual("+");
        AssertThat(hud.TooltipBio != null && hud.TooltipBio.Visible).IsFalse();
        GD.Print($"[PASS] Slot 2 Empty Slot Tooltip verified: '{hud.TooltipIcon?.Text} {hud.TooltipTitle?.Text}'");

        // Test Mouse Exited
        hud.OnSlotMouseExited(2);
        AssertThat(skillTooltip.Visible).IsFalse();
        GD.Print("[PASS] Tooltip hide on mouse exit verified.");

        // 3. Verify Codex Modal in HUD
        var codexModal = hud.CellCodexModal;
        AssertThat(codexModal).IsNotNull();

        // Open Codex to Tab 0 (Skills Manual)
        codexModal!.OpenCodex(0);
        AssertThat(codexModal.Visible).IsTrue();
        AssertThat(codexModal.CurrentTab).IsEqual(0);

        var itemList = codexModal.ItemList;
        AssertThat(itemList != null && itemList.GetChildCount() >= 9).IsTrue();
        GD.Print($"[PASS] Codex Skill Manual tab displays {itemList!.GetChildCount()} skills.");

        // Switch to Tab 1 (Cells)
        codexModal.SwitchTab(1);
        AssertThat(itemList.GetChildCount() >= 5).IsTrue();
        GD.Print($"[PASS] Codex Immune Cells tab displays {itemList.GetChildCount()} cells.");

        // Switch to Tab 2 (Pathogens)
        codexModal.SwitchTab(2);
        AssertThat(itemList.GetChildCount() >= 4).IsTrue();
        GD.Print($"[PASS] Codex Pathogen Catalog tab displays {itemList.GetChildCount()} pathogens.");

        // Switch to Tab 3 (Maps)
        codexModal.SwitchTab(3);
        AssertThat(itemList.GetChildCount() >= 2).IsTrue();
        GD.Print($"[PASS] Codex Pathological Stages tab displays {itemList.GetChildCount()} maps.");

        // Switch to Tab 4 (Achievements)
        codexModal.SwitchTab(4);
        AssertThat(itemList.GetChildCount() >= 7).IsTrue();
        GD.Print($"[PASS] Codex Achievements tab displays {itemList.GetChildCount()} achievements.");

        // Close Codex
        codexModal.CloseCodex();
        AssertThat(codexModal.Visible).IsFalse();
        GD.Print("[PASS] CodexModal open/close and 5-tab switching verified.");

        // 4. Verify Pause Menu Manual Button
        hud.TogglePause();
        AssertThat(hud.PauseModal != null && hud.PauseModal.Visible).IsTrue();

        // Click Manual Button inside Pause Menu
        hud.OnManualPressed();
        AssertThat(codexModal.Visible).IsTrue();
        GD.Print("[PASS] Codex / Skill Manual accessible directly from in-game Pause Menu.");

        // Close codex from pause menu
        codexModal.CloseCodex();
        hud.ResumeGame();
        AssertThat(hud.PauseModal.Visible || codexModal.Visible).IsFalse();
        GD.Print("[PASS] In-game pause modal and manual cleanly resumed.");

        // 5. Bilingual Localization on Tooltip and Manual
        GameManager.SetLanguage("en");
        hud.OnSlotMouseEntered(0, card0);
        var enBadge = hud.TooltipBadge?.Text ?? "";
        AssertThat(enBadge.Contains("ACTIVE")).IsTrue();
        AssertThat(hud.TooltipTitle?.Text.Contains("ROS Torrent") ?? false).IsTrue();
        AssertThat(hud.TooltipDesc?.Text.Contains("æˆ˜æœ¯æœºåˆ¶") ?? false).IsFalse();

        // Test Codex in English: ensure NO Chinese headers
        codexModal.OpenCodex(1); // Cells tab
        codexModal.SelectCell("macrophage");
        AssertThat(codexModal.DetailDesc?.Text.Contains("å˜å½¢ç‰¹æ€§") ?? false).IsFalse();
        AssertThat(codexModal.DetailDesc?.Text.Contains("Deformation Trait") ?? false).IsTrue();

        codexModal.SwitchTab(0); // Skills tab
        codexModal.SelectSkill("macrophage_pseudopods");
        AssertThat(codexModal.DetailDesc?.Text.Contains("æˆ˜æœ¯æœºåˆ¶") ?? false).IsFalse();
        AssertThat(codexModal.DetailDesc?.Text.Contains("Tactical Effect") ?? false).IsTrue();

        codexModal.CloseCodex();

        GameManager.SetLanguage("zh_CN");
        hud.OnSlotMouseEntered(0, card0);
        var zhBadge = hud.TooltipBadge?.Text ?? "";
        AssertThat(zhBadge.Contains("ä¸»åŠ¨")).IsTrue();
        AssertThat(hud.TooltipTitle?.Text.Contains("æ´»æ€§æ°§å°„æµ") ?? false).IsTrue();
        AssertThat(hud.TooltipDesc?.Text.Contains("æˆ˜æœ¯æœºåˆ¶") ?? false).IsTrue();
        hud.OnSlotMouseExited(0);
        GD.Print("[PASS] Tooltip and Manual dynamic bilingual switching verified without residual Chinese.");

        main.QueueFree();
        GD.Print("--- ALL CODEX & TOOLTIP TESTS PASSED SUCCESSFULLY! ---");
        Quit(0);
        return true;
    }
}
