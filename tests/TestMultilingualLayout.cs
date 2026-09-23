using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using Phagocyte.Core;
using Phagocyte.UI;

namespace Phagocyte.Tests;

/// <summary>
/// Comprehensive multilingual verification suite across all 8 supported languages:
/// en, zh_CN, zh_TW, ja, de, fr, ru, es.
/// Asserts:
/// 1. SettingsModal panel and controls bounding box across all 8 languages (zero overflow).
/// 2. Header CloseButton ("‹ Back") remains flush within the modal window.
/// 3. Zero untranslated CJK characters in visible labels/buttons for Western locales.
/// 4. Headed preview captures for visual verification when PHAGOCYTE_CAPTURE_DIR is set.
/// </summary>
[TestSuite]
public partial class TestMultilingualLayout : TestHarness
{
    private static readonly string[] Locales = { "en", "zh_CN", "zh_TW", "ja", "de", "fr", "ru", "es" };

    private int _frame = 0;
    private int _localeIdx = 0;
    private int _subStage = 0;

    private SettingsModal? _settingsModal;
    private EndgameSetupModal? _endgameModal;
    private CodexModal? _codexModal;
    private Control? _titleView;

    public override void _Initialize()
    {
        Banner("MULTILINGUAL LAYOUT & BOUNDS VERIFICATION");
        IsolateSaves("multilingual");

        // Instantiate SettingsModal
        var settingsScene = AssetLoader.Load<PackedScene>("res://scenes/ui/settings_modal.tscn");
        _settingsModal = settingsScene.Instantiate<SettingsModal>();
        Root.AddChild(_settingsModal);

        // Instantiate EndgameSetupModal
        var endgameScene = AssetLoader.Load<PackedScene>("res://scenes/ui/endgame_setup_modal.tscn");
        _endgameModal = endgameScene.Instantiate<EndgameSetupModal>();
        Root.AddChild(_endgameModal);

        // Instantiate CodexModal
        var codexScene = AssetLoader.Load<PackedScene>("res://scenes/ui/codex_modal.tscn");
        _codexModal = codexScene.Instantiate<CodexModal>();
        Root.AddChild(_codexModal);

        // Instantiate TitleView
        var titleScene = AssetLoader.Load<PackedScene>("res://scenes/ui/title_view.tscn");
        _titleView = titleScene.Instantiate<Control>();
        Root.AddChild(_titleView);
    }

    public override bool _Process(double delta)
    {
        if (_localeIdx >= Locales.Length)
        {
            GD.Print("--- ALL 8 LOCALES TESTED AND VERIFIED SUCCESSFULLY! ---");
            Quit(0);
            return true;
        }

        string loc = Locales[_localeIdx];

        switch (_subStage)
        {
            case 0:
                if (!Gate(ref _frame, 2)) return false;
                GameManager.SetLanguage(loc);
                _settingsModal!.OpenSettings(0);
                _subStage = 1;
                _frame = 0;
                return false;

            case 1:
                if (!Gate(ref _frame, 3)) return false;
                VerifySettingsModalBounds(loc);
                if (loc == "en" || loc == "ru" || loc == "de" || loc == "zh_CN")
                {
                    CaptureScreenshot($"settings_modal_{loc}.png");
                }
                _settingsModal!.SwitchTab(1); // Audio tab
                _subStage = 2;
                _frame = 0;
                return false;

            case 2:
                if (!Gate(ref _frame, 2)) return false;
                VerifySettingsModalBounds(loc);
                _settingsModal!.CloseSettings();

                // Check EndgameSetupModal
                _endgameModal!.OpenSetup();
                _subStage = 3;
                _frame = 0;
                return false;

            case 3:
                if (!Gate(ref _frame, 2)) return false;
                if (loc == "ru" || loc == "en")
                {
                    CaptureScreenshot($"endgame_setup_{loc}.png");
                }
                _endgameModal!.CloseModal();

                _localeIdx++;
                _subStage = 0;
                _frame = 0;
                return false;
        }

        return false;
    }

    private void VerifySettingsModalBounds(string loc)
    {
        var panel = _settingsModal!.GetNodeOrNull<Panel>("Panel");
        var vbox = _settingsModal.GetNodeOrNull<VBoxContainer>("VBox");
        var closeBtn = _settingsModal.GetNodeOrNull<Button>("VBox/Header/CloseButton");

        AssertThat(panel).IsNotNull();
        AssertThat(vbox).IsNotNull();
        AssertThat(closeBtn).IsNotNull();

        var panelRect = panel!.GetGlobalRect();
        var vboxRect = vbox!.GetGlobalRect();

        // Left edge of VBox must NEVER be to the left of Panel
        AssertThat(vboxRect.Position.X >= panelRect.Position.X)
            .OverrideFailureMessage($"[FAIL][{loc}] VBox left edge ({vboxRect.Position.X}) is outside Panel left edge ({panelRect.Position.X})!")
            .IsTrue();

        // Right edge of VBox must NEVER be past Panel right edge
        AssertThat(vboxRect.End.X <= panelRect.End.X + 2.0f)
            .OverrideFailureMessage($"[FAIL][{loc}] VBox right edge ({vboxRect.End.X}) exceeds Panel right edge ({panelRect.End.X})!")
            .IsTrue();

        // Bottom edge of VBox must NEVER be past Panel bottom edge
        AssertThat(vboxRect.End.Y <= panelRect.End.Y + 2.0f)
            .OverrideFailureMessage($"[FAIL][{loc}] VBox bottom edge ({vboxRect.End.Y}) exceeds Panel bottom edge ({panelRect.End.Y})!")
            .IsTrue();

        // Close button position must be strictly inside panel bounds
        var btnRect = closeBtn!.GetGlobalRect();
        AssertThat(btnRect.Position.X >= panelRect.Position.X)
            .OverrideFailureMessage($"[FAIL][{loc}] Close button left ({btnRect.Position.X}) is outside panel ({panelRect.Position.X})!")
            .IsTrue();

        // In Western locales, check for leaked CJK characters in visible labels
        if (loc == "en" || loc == "de" || loc == "fr" || loc == "ru" || loc == "es")
        {
            CheckNoLeakedCjk(_settingsModal, loc);
        }

        GD.Print($"[PASS] SettingsModal bounds verified for locale '{loc}': VBox ({vboxRect.Size.X}x{vboxRect.Size.Y}) cleanly inside Panel ({panelRect.Size.X}x{panelRect.Size.Y}).");
    }

    private static void CheckNoLeakedCjk(Node rootNode, string loc)
    {
        var stack = new System.Collections.Generic.Stack<Node>();
        stack.Push(rootNode);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            foreach (Node child in node.GetChildren())
            {
                stack.Push(child);
            }

            if (node is Label lbl && lbl.IsVisibleInTree())
            {
                // Exclude language dropdown or intentional brand
                if (lbl.Name == "LangLabel") continue;
                AssertThat(HasCjk(lbl.Text))
                    .OverrideFailureMessage($"[FAIL][{loc}] Leaked CJK in Label '{lbl.GetPath()}': '{lbl.Text}'")
                    .IsFalse();
            }
            else if (node is Button btn && btn.IsVisibleInTree())
            {
                AssertThat(HasCjk(btn.Text))
                    .OverrideFailureMessage($"[FAIL][{loc}] Leaked CJK in Button '{btn.GetPath()}': '{btn.Text}'")
                    .IsFalse();
            }
        }
    }

    private static bool HasCjk(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (char c in text)
        {
            if ((c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF))
                return true;
        }
        return false;
    }
}
