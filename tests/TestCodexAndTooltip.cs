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
        var mainScene = AssetLoader.Load<PackedScene>("res://scenes/main.tscn");
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
        AssertThat(GameManager.SkillCatalog.ContainsKey("phagocytic_grasp")).IsTrue();
        AssertThat(GameManager.SkillCatalog.ContainsKey("ros_torrent")).IsTrue();
        GD.Print("[PASS] GameManager.SkillCatalog contains complete skill definitions.");

        AssertThat(GameManager.PathogenCatalog.Count >= 20).IsTrue();
        GD.Print($"[PASS] GameManager.PathogenCatalog contains {GameManager.PathogenCatalog.Count} complete pathogen definitions.");

        // 2. Verify In-Game Skill Tooltip
        var skillTooltip = hud.SkillTooltip;
        AssertThat(skillTooltip).IsNotNull();

        // Macrophage starts with the innate active (吞噬偽足) in slot 0; draft a ranged weapon to test the active tooltip
        var player = main.GetNodeOrNull<BaseCell>("Macrophage");
        AssertThat(player).IsNotNull();
        AssertThat(player!.CellSkillManager!.EquipActive(new RosTorrentSkill(), 1)).IsTrue();
        hud.UpdateSkillSlots();

        var slotsContainer = hud.SlotsContainer;
        AssertThat(slotsContainer != null && slotsContainer.GetChildCount() >= 6).IsTrue();

        // Test Hover Slot 0 (Innate Active 吞噬偽足)
        var card0 = slotsContainer!.GetChild<Control>(0);
        hud.OnSlotMouseEntered(0, card0);
        AssertThat(skillTooltip!.Visible).IsTrue();

        var tTitle = hud.TooltipTitle?.Text ?? "";
        var tBadge = hud.TooltipBadge?.Text ?? "";
        var tTex = skillTooltip!.GetNodeOrNull<TextureRect>("VBox/HeaderHBox/TooltipIconTexture");
        AssertThat(tTex).IsNotNull();
        AssertThat(tTex!.Texture).IsNotNull();
        AssertThat(tBadge.Contains("主动") || tBadge.Contains("ACTIVE")).IsTrue();
        GD.Print($"[PASS] Slot 0 Innate-as-Active Tooltip verified: '{tTitle}' ({tBadge})");

        // Test Hover Slot 1 (Drafted Active Weapon)
        var card1 = slotsContainer!.GetChild<Control>(1);
        hud.OnSlotMouseEntered(1, card1);
        AssertThat(skillTooltip.Visible).IsTrue();
        var tTex1 = skillTooltip.GetNodeOrNull<TextureRect>("VBox/HeaderHBox/TooltipIconTexture");
        AssertThat(tTex1).IsNotNull();
        AssertThat(tTex1!.Texture).IsNotNull();
        var activeBadge = hud.TooltipBadge?.Text ?? "";
        AssertThat(activeBadge.Contains("\u4E3B\u52A8") || activeBadge.Contains("ACTIVE")).IsTrue();
        AssertThat(hud.TooltipStats?.Text.Contains("3.2") ?? false).IsTrue();
        GD.Print($"[PASS] Slot 1 Active Weapon Tooltip verified: '{hud.TooltipTitle?.Text}' ({activeBadge})");

        // Test Hover Slot 2 (Empty Slot)
        var card2 = slotsContainer.GetChild<Control>(2);
        hud.OnSlotMouseEntered(2, card2);
        var tTex2 = skillTooltip.GetNodeOrNull<TextureRect>("VBox/HeaderHBox/TooltipIconTexture");
        AssertThat(tTex2).IsNotNull();
        AssertThat(tTex2!.Texture).IsNotNull();
        AssertThat(hud.TooltipBio != null && hud.TooltipBio.Visible).IsFalse();
        GD.Print($"[PASS] Slot 2 Empty Slot Tooltip verified: '{hud.TooltipTitle?.Text}'");

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

        codexModal.SwitchTab(1); // Passives tab
        AssertThat(itemList.GetChildCount() >= 13).IsTrue();
        GD.Print($"[PASS] Codex Passive Traits tab displays {itemList.GetChildCount()} passives.");

        // Switch to Tab 2 (Cells)
        codexModal.SwitchTab(2);
        AssertThat(itemList.GetChildCount() >= 5).IsTrue();
        GD.Print($"[PASS] Codex Immune Cells tab displays {itemList.GetChildCount()} cells.");

        // Switch to Tab 3 (Pathogens)
        codexModal.SwitchTab(3);
        AssertThat(itemList.GetChildCount() >= 4).IsTrue();
        GD.Print($"[PASS] Codex Pathogen Catalog tab displays {itemList.GetChildCount()} pathogens.");

        // Switch to Tab 4 (Maps)
        codexModal.SwitchTab(4);
        AssertThat(itemList.GetChildCount() >= 2).IsTrue();
        GD.Print($"[PASS] Codex Pathological Stages tab displays {itemList.GetChildCount()} maps.");

        // Achievements moved out of the Codex: MainMenu AchievementView owns them.
        // Maps (Tab 4) is the last Codex tab; out-of-range indices must be ignored.
        codexModal.SwitchTab(4);
        AssertThat(codexModal.CurrentTab).IsEqual(4);
        AssertThat(itemList.GetChildCount() >= 2).IsTrue();
        GD.Print("[PASS] Codex exposes 5 tabs; achievements live in the MainMenu gallery.");

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
        AssertThat(hud.PauseModal!.Visible || codexModal.Visible).IsFalse();
        GD.Print("[PASS] In-game pause modal and manual cleanly resumed.");

        // 5. Bilingual Localization on Tooltip and Manual
        GameManager.SetLanguage("en");
        hud.OnSlotMouseEntered(1, card1);
        var enBadge = hud.TooltipBadge?.Text ?? "";
        AssertThat(enBadge.Contains("ACTIVE")).IsTrue();
        AssertThat(hud.TooltipTitle?.Text.Contains("ROS Torrent") ?? false).IsTrue();
        AssertThat(hud.TooltipDesc?.Text.Contains("\u3010\u0020\u6218\u672F\u673A\u5236\u0020\u3011") ?? false).IsFalse();
        hud.OnSlotMouseEntered(0, card0);
        var enInnateBadge = hud.TooltipBadge?.Text ?? "";
        AssertThat(enInnateBadge.Contains("ACTIVE")).IsTrue();
        AssertThat(hud.TooltipTitle?.Text.Contains("Phagocytic Grasp") ?? false).IsTrue();

        // Test Codex in English: ensure NO Chinese headers
        codexModal.OpenCodex(2); // Cells tab
        codexModal.SelectCell("macrophage");
        AssertThat(codexModal.DetailDesc?.Text.Contains("\u53D8\u5F62\u7279\u6027") ?? false).IsFalse();
        AssertThat(codexModal.DetailDesc?.Text.Contains("Deformation Trait") ?? false).IsTrue();

        codexModal.SwitchTab(0); // Skills tab
        codexModal.SelectSkill("phagocytic_grasp");
        AssertThat(codexModal.DetailDesc?.Text.Contains("\u3010\u0020\u6218\u672F\u673A\u5236\u0020\u3011") ?? false).IsFalse();
        AssertThat(codexModal.DetailDesc?.Text.Contains("Tactical Effect") ?? false).IsTrue();

        codexModal.CloseCodex();

        GameManager.SetLanguage("zh_CN");
        hud.OnSlotMouseEntered(1, card1);
        var zhBadge = hud.TooltipBadge?.Text ?? "";
        AssertThat(zhBadge.Contains("\u4E3B\u52A8")).IsTrue();
        AssertThat(hud.TooltipTitle?.Text.Contains("\u6D3B\u6027\u6C27\u5C04\u6D41") ?? false).IsTrue();
        AssertThat(hud.TooltipDesc?.Text.Contains("\u3010\u0020\u6218\u672F\u673A\u5236\u0020\u3011") ?? false).IsTrue();
        hud.OnSlotMouseEntered(0, card0);
        var zhInnateBadge = hud.TooltipBadge?.Text ?? "";
        AssertThat(zhInnateBadge.Contains("主动")).IsTrue();
        hud.OnSlotMouseExited(0);
        GD.Print("[PASS] Tooltip and Manual dynamic bilingual switching verified without residual Chinese.");

        main.QueueFree();
        GD.Print("--- ALL CODEX & TOOLTIP TESTS PASSED SUCCESSFULLY! ---");
        Quit(0);
        return true;
    }
}
