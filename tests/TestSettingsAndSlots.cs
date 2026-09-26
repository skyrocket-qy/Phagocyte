using Godot;
using System;
using Phagocyte.Core;
using Phagocyte.Skills;
using Phagocyte.UI;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Phagocyte.Tests;

public partial class TestSettingsAndSlots : TestHarness
{
    private int _framesWaited = 0;
    private bool _testDone = false;

    public override void _Initialize()
    {
        GD.Print("--- BEGINNING SETTINGS & DUAL-ROW SLOTS AUTOMATED VERIFICATION ---");
        IsolateSaves("settings_slots");
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

        // =========================================================================
        // TEST 1: HelpContainer is completely removed from HUD
        // =========================================================================
        var helpContainer = hud.GetNodeOrNull<Node>("HelpContainer");
        AssertThat(helpContainer).IsNull();
        GD.Print("[PASS] 1. HelpContainer instruction panel has been successfully removed from HUD.");

        // =========================================================================
        // TEST 2: Dual-row 10-Slot HUD GridContainer
        // =========================================================================
        var slotsContainer = hud.GetNodeOrNull<GridContainer>("SkillContainer/VBox/SlotsContainer");
        AssertThat(slotsContainer).IsNotNull();
        AssertThat(slotsContainer!.Columns).IsEqual(5);
        AssertThat(slotsContainer.GetChildCount()).IsEqual(10);
        GD.Print("[PASS] 2. HUD contains 10-slot dual-row GridContainer (5 columns x 2 rows).");

        for (int i = 0; i < 5; i++)
        {
            var slot = slotsContainer.GetChild(i);
            AssertThat(slot.Name.ToString()).IsEqual("Slot" + i);
        }
        for (int i = 5; i < 10; i++)
        {
            var slot = slotsContainer.GetChild(i);
            AssertThat(slot.Name.ToString()).IsEqual("Slot" + i);
        }
        GD.Print("[PASS] 3. Slot0..Slot4 (Active) and Slot5..Slot9 (Passive) nodes confirmed.");

        // =========================================================================
        // TEST 3: Hover Tooltip for Passive Slots (Slot 5)
        // =========================================================================
        var card5 = slotsContainer.GetChild<Control>(5);
        hud.OnSlotMouseEntered(5, card5);
        AssertThat(hud.SkillTooltip != null && hud.SkillTooltip.Visible).IsTrue();
        string badgeText = hud.TooltipBadge?.Text ?? "";
        // Passive slots start empty (the innate start hub carries no effects and is
        // not a slotted skill — see TestSkillSystem/TestTalentPipeline),
        // so the hover badge shows the empty-passive tag, not the innate tag.
        AssertThat(badgeText.Contains("被动") || badgeText.Contains("Passive") || badgeText.Contains("PASSIVE")).IsTrue();

        hud.OnSlotMouseExited(5);
        AssertThat(hud.SkillTooltip != null && hud.SkillTooltip.Visible).IsFalse();
        GD.Print("[PASS] 4. Passive slot 5 hover tooltip displays passive slot details and hides on exit.");

        // =========================================================================
        // TEST 4: Equip Passive Trait and Verify Slot 5 Updates
        // =========================================================================
        var player = main.GetNodeOrNull<Node2D>("Player") ?? hud.PlayerRef;
        if (player == null)
            player = Root.GetTree().GetFirstNodeInGroup("player") as Node2D;
        AssertThat(player).IsNotNull();

        var skillMgr = player!.GetNodeOrNull<SkillManager>("SkillManager");
        AssertThat(skillMgr).IsNotNull();

        var mito = new PassiveMitochondrialOverclock();
        // Passive slot 0 is the cell's innate trait; equip into the next free slot
        // (slot 1 in passives = Slot 6 in the UI grid).
        bool equipped = skillMgr!.EquipPassive(mito, 1);
        AssertThat(equipped).IsTrue();

        hud.UpdateSkillSlots();
        var card6 = slotsContainer.GetChild<Control>(6);
        var slot6Tex = card6.GetNodeOrNull<TextureRect>("IconTexture");
        var slot6Badge = card6.GetNodeOrNull<Label>("BadgeLabel");
        var slot6Cd = card6.GetNodeOrNull<ProgressBar>("CooldownBar");

        AssertThat(slot6Tex).IsNotNull();
        AssertThat(slot6Tex!.Texture).IsNotNull();
        AssertThat(slot6Badge).IsNotNull();
        AssertThat(slot6Badge!.Text.Contains("1")).IsTrue();
        if (slot6Cd != null)
        {
            AssertThat(slot6Cd.Visible).IsFalse();
        }
        GD.Print("[PASS] 5. Equipped passive skill correctly populates Slot 6 with icon, Lv.1, and no cooldown bar.");

        // =========================================================================
        // TEST 5: SettingsManager Persistence and Core API
        // =========================================================================
        SettingsManager.SetMasterVolume(0.65f);
        SettingsManager.SetSfxVolume(0.45f);
        SettingsManager.SetBgmVolume(0.80f);
        SettingsManager.SetFullscreen(true);
        SettingsManager.SetVsync(false);

        AssertThat(Mathf.Abs(SettingsManager.MasterVolume - 0.65f) < 0.01f).IsTrue();
        AssertThat(Mathf.Abs(SettingsManager.SfxVolume - 0.45f) < 0.01f).IsTrue();
        AssertThat(Mathf.Abs(SettingsManager.BgmVolume - 0.80f) < 0.01f).IsTrue();
        AssertThat(SettingsManager.Fullscreen).IsTrue();
        AssertThat(SettingsManager.Vsync).IsFalse();

        AssertThat(FileAccess.FileExists(SettingsManager.SavePath)).IsTrue();

        SettingsManager.LoadFromDisk();
        AssertThat(Mathf.Abs(SettingsManager.MasterVolume - 0.65f) < 0.01f).IsTrue();
        AssertThat(SettingsManager.Fullscreen).IsTrue();
        GD.Print("[PASS] 6. SettingsManager volume/graphics persistence to the isolated save verified.");

        // =========================================================================
        // TEST 6: SettingsModal in HUD (Pause Menu)
        // =========================================================================
        var pauseSettingsBtn = hud.GetNodeOrNull<Button>("PauseModal/VBox/SettingsButton");
        AssertThat(pauseSettingsBtn).IsNotNull();

        var hudSettingsModal = hud.CellSettingsModal;
        AssertThat(hudSettingsModal).IsNotNull();

        pauseSettingsBtn!.EmitSignal(Button.SignalName.Pressed);
        AssertThat(hudSettingsModal!.Visible).IsTrue();

        var closeBtn = hudSettingsModal.GetNodeOrNull<Button>("VBox/Header/CloseButton");
        AssertThat(closeBtn).IsNotNull();
        // Unified top-left back: first header child with the NAV_BACK label
        AssertThat(closeBtn!.GetIndex()).IsEqual(0);
        AssertThat(closeBtn.Text.Contains("返回") || closeBtn.Text.Contains("Back") || closeBtn.Text.Contains("Zurück")).IsTrue();

        closeBtn.EmitSignal(Button.SignalName.Pressed);
        AssertThat(hudSettingsModal.Visible).IsFalse();
        GD.Print("[PASS] 7. SettingsModal in in-game Pause Menu opens and closes via top-left back button.");

        // =========================================================================
        // TEST 7: SettingsModal in Main Menu
        // =========================================================================
        var menuScene = AssetLoader.Load<PackedScene>("res://scenes/ui/main_menu.tscn");
        AssertThat(menuScene).IsNotNull();
        var menuNode = menuScene!.Instantiate<MainMenu>();
        Root.AddChild(menuNode);

        var menuSettingsBtn = menuNode.GetNodeOrNull<Button>("TitleView/VBox/SettingsButton");
        AssertThat(menuSettingsBtn).IsNotNull();

        var menuSettingsModal = menuNode.CellSettingsModal;
        AssertThat(menuSettingsModal).IsNotNull();

        menuSettingsBtn!.EmitSignal(Button.SignalName.Pressed);
        AssertThat(menuSettingsModal!.Visible).IsTrue();

        var tabAudio = menuSettingsModal.GetNodeOrNull<Button>("VBox/TabBar/AudioTab");
        var tabGraphics = menuSettingsModal.GetNodeOrNull<Button>("VBox/TabBar/GraphicsTab");

        AssertThat(tabAudio).IsNotNull();
        AssertThat(tabGraphics).IsNotNull();
        AssertThat(menuSettingsModal.TabKeysBtn).IsNotNull();

        tabAudio!.EmitSignal(Button.SignalName.Pressed);
        var audioContent = menuSettingsModal.GetNodeOrNull<Control>("VBox/Content/AudioPanel");
        AssertThat(audioContent != null && audioContent.Visible).IsTrue();

        tabGraphics!.EmitSignal(Button.SignalName.Pressed);
        var graphicsContent = menuSettingsModal.GetNodeOrNull<Control>("VBox/Content/GraphicsPanel");
        AssertThat(graphicsContent != null && graphicsContent.Visible).IsTrue();

        menuSettingsModal.TabKeysBtn!.EmitSignal(Button.SignalName.Pressed);
        AssertThat(menuSettingsModal.KeysPanel != null && menuSettingsModal.KeysPanel.Visible).IsTrue();
        AssertThat(menuSettingsModal.GetNodeOrNull<Button>("VBox/TabBar/ControlsTab")).IsNull();
        AssertThat(menuSettingsModal.GetNodeOrNull<Control>("VBox/Content/ControlsPanel")).IsNull();

        var menuCloseBtn = menuSettingsModal.GetNodeOrNull<Button>("VBox/Header/CloseButton");
        AssertThat(menuCloseBtn).IsNotNull();
        menuCloseBtn!.EmitSignal(Button.SignalName.Pressed);
        AssertThat(menuSettingsModal.Visible).IsFalse();
        GD.Print("[PASS] 8. Main Menu Settings button, tabs (Audio/Graphics/Keys), and top-left back button verified.");

        menuNode.QueueFree();
        main.QueueFree();

        RestoreSaves();
        GD.Print("--- ALL SETTINGS & DUAL-ROW SLOTS TESTS PASSED SUCCESSFULLY! ---");
        Quit(0);
        return true;
    }
}
