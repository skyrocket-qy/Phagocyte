using Godot;
using System;
using Phagocyte.Core;
using Phagocyte.Environment;

namespace Phagocyte.UI;

public partial class SettingsModal : ModalBase
{
    public Button? TabAudioBtn { get; set; }
    public Button? TabGraphicsBtn { get; set; }
    public Button? TabKeysBtn { get; set; }

    public VBoxContainer? AudioPanel { get; set; }
    public VBoxContainer? GraphicsPanel { get; set; }
    public VBoxContainer? KeysPanel { get; set; }
    public VBoxContainer? KeysRows { get; set; }

    public Label? KeysHintLbl { get; set; }
    public Button? KeysResetBtn { get; set; }

    public HSlider? MasterSlider { get; set; }
    public Label? MasterValLbl { get; set; }
    public Label? MasterTitleLbl { get; set; }

    public HSlider? SfxSlider { get; set; }
    public Label? SfxValLbl { get; set; }
    public Label? SfxTitleLbl { get; set; }

    public HSlider? BgmSlider { get; set; }
    public Label? BgmValLbl { get; set; }
    public Label? BgmTitleLbl { get; set; }

    public Button? FullscreenCheck { get; set; }
    public Button? VsyncCheck { get; set; }
    public Button? ShakeCheck { get; set; }
    public Button? PerfCheck { get; set; }
    public Button? FpsCheck { get; set; }
    public Button? MaxFpsBtn { get; set; }

    public Label? FullscreenTitleLbl { get; set; }
    public Label? VsyncTitleLbl { get; set; }
    public Label? ShakeTitleLbl { get; set; }
    public Label? PerfTitleLbl { get; set; }
    public Label? FpsTitleLbl { get; set; }
    public Label? MaxFpsTitleLbl { get; set; }

    public Label? LangTitleLbl { get; set; }
    public OptionButton? LangOption { get; set; }

    /// <summary>Dropdown order: English, 简体中文, 繁體中文, 日本語, Deutsch, Français, Русский, Español.</summary>
    private static readonly string[] LangLocales = { "en", "zh_CN", "zh_TW", "ja", "de", "fr", "ru", "es" };
    private static readonly string[] LangNames = { "English", "简体中文", "繁體中文", "日本語", "Deutsch", "Français", "Русский", "Español" };

    public int CurrentTab { get; set; } = 0;

    public const int TabAudio = 0;
    public const int TabGraphics = 1;
    public const int TabKeys = 2;

    /// <summary>Action currently awaiting a replacement key; "" when idle.</summary>
    public string ListeningAction { get; private set; } = "";
    private readonly System.Collections.Generic.Dictionary<string, Button> _keyRowButtons = new();

    public override void _Ready()
    {
        TabAudioBtn = GetNodeOrNull<Button>("VBox/TabBar/AudioTab");
        TabGraphicsBtn = GetNodeOrNull<Button>("VBox/TabBar/GraphicsTab");

        AudioPanel = GetNodeOrNull<VBoxContainer>("VBox/Content/AudioPanel");
        GraphicsPanel = GetNodeOrNull<VBoxContainer>("VBox/Content/GraphicsPanel");

        MasterSlider = GetNodeOrNull<HSlider>("VBox/Content/AudioPanel/MasterRow/Slider");
        MasterValLbl = GetNodeOrNull<Label>("VBox/Content/AudioPanel/MasterRow/ValLabel");
        MasterTitleLbl = GetNodeOrNull<Label>("VBox/Content/AudioPanel/MasterRow/TitleLabel");

        SfxSlider = GetNodeOrNull<HSlider>("VBox/Content/AudioPanel/SFXRow/Slider");
        SfxValLbl = GetNodeOrNull<Label>("VBox/Content/AudioPanel/SFXRow/ValLabel");
        SfxTitleLbl = GetNodeOrNull<Label>("VBox/Content/AudioPanel/SFXRow/TitleLabel");

        BgmSlider = GetNodeOrNull<HSlider>("VBox/Content/AudioPanel/BGMRow/Slider");
        BgmValLbl = GetNodeOrNull<Label>("VBox/Content/AudioPanel/BGMRow/ValLabel");
        BgmTitleLbl = GetNodeOrNull<Label>("VBox/Content/AudioPanel/BGMRow/TitleLabel");

        FullscreenCheck = GetNodeOrNull<Button>("VBox/Content/GraphicsPanel/FullscreenRow/FullscreenCheck");
        VsyncCheck = GetNodeOrNull<Button>("VBox/Content/GraphicsPanel/VSyncRow/VSyncCheck");
        ShakeCheck = GetNodeOrNull<Button>("VBox/Content/GraphicsPanel/ShakeRow/ShakeCheck");
        PerfCheck = GetNodeOrNull<Button>("VBox/Content/GraphicsPanel/PerfRow/PerfCheck");
        FpsCheck = GetNodeOrNull<Button>("VBox/Content/GraphicsPanel/FpsRow/FpsCheck");
        MaxFpsBtn = GetNodeOrNull<Button>("VBox/Content/GraphicsPanel/MaxFpsRow/MaxFpsBtn");

        FullscreenTitleLbl = GetNodeOrNull<Label>("VBox/Content/GraphicsPanel/FullscreenRow/TitleLabel");
        VsyncTitleLbl = GetNodeOrNull<Label>("VBox/Content/GraphicsPanel/VSyncRow/TitleLabel");
        ShakeTitleLbl = GetNodeOrNull<Label>("VBox/Content/GraphicsPanel/ShakeRow/TitleLabel");
        PerfTitleLbl = GetNodeOrNull<Label>("VBox/Content/GraphicsPanel/PerfRow/TitleLabel");
        FpsTitleLbl = GetNodeOrNull<Label>("VBox/Content/GraphicsPanel/FpsRow/TitleLabel");
        MaxFpsTitleLbl = GetNodeOrNull<Label>("VBox/Content/GraphicsPanel/MaxFpsRow/TitleLabel");

        LangTitleLbl = GetNodeOrNull<Label>("VBox/LanguageRow/LangLabel");
        LangOption = GetNodeOrNull<OptionButton>("VBox/LanguageRow/LangOption");

        if (TabAudioBtn != null)
            TabAudioBtn.Pressed += () => SwitchTab(TabAudio);
        if (TabGraphicsBtn != null)
            TabGraphicsBtn.Pressed += () => SwitchTab(TabGraphics);

        BuildKeysTab();

        // Audio signals
        if (MasterSlider != null)
            MasterSlider.ValueChanged += OnMasterSliderChanged;
        if (SfxSlider != null)
            SfxSlider.ValueChanged += OnSfxSliderChanged;
        if (BgmSlider != null)
            BgmSlider.ValueChanged += OnBgmSliderChanged;

        // Graphics state buttons (right side shows ON/OFF like the keys tab)
        if (FullscreenCheck != null)
            FullscreenCheck.Pressed += OnFullscreenPressed;
        if (VsyncCheck != null)
            VsyncCheck.Pressed += OnVsyncPressed;
        if (ShakeCheck != null)
            ShakeCheck.Pressed += OnShakePressed;
        if (PerfCheck != null)
            PerfCheck.Pressed += OnPerfPressed;
        if (FpsCheck != null)
            FpsCheck.Pressed += OnFpsPressed;
        if (MaxFpsBtn != null)
            MaxFpsBtn.Pressed += OnMaxFpsPressed;

        // Language dropdown (native names, never translated)
        if (LangOption != null)
        {
            LangOption.Clear();
            for (int i = 0; i < LangNames.Length; i++)
                LangOption.AddItem(LangNames[i], i);
            LangOption.ItemSelected += OnLanguageSelected;
        }

        SyncUiFromSettings();
        InitModal();
    }

    public void OpenSettings(int targetTab = 0)
    {
        SyncUiFromSettings();
        ListeningAction = "";
        RefreshKeysPanel();
        Visible = true;
        SwitchTab(targetTab);
    }

    public override void CloseModal()
    {
        ListeningAction = "";
        base.CloseModal();
    }

    public void CloseSettings()
    {
        CloseModal();
    }

    public void SwitchTab(int tabIdx)
    {
        CurrentTab = tabIdx;

        UiBuilders.SetTabActive(TabAudioBtn, tabIdx == TabAudio);
        UiBuilders.SetTabActive(TabGraphicsBtn, tabIdx == TabGraphics);
        UiBuilders.SetTabActive(TabKeysBtn, tabIdx == TabKeys);

        if (AudioPanel != null)
            AudioPanel.Visible = tabIdx == TabAudio;
        if (GraphicsPanel != null)
            GraphicsPanel.Visible = tabIdx == TabGraphics;
        if (KeysPanel != null)
            KeysPanel.Visible = tabIdx == TabKeys;
    }

    /// <summary>
    /// Code-built keys tab: the scene keeps its static tabs/panels, key rows
    /// vary per action so they are built in code like MainMenu profile tabs.
    /// </summary>
    private void BuildKeysTab()
    {
        var tabBar = GetNodeOrNull<HBoxContainer>("VBox/TabBar");
        var content = GetNodeOrNull<Control>("VBox/Content");
        if (tabBar == null || content == null)
            return;

        var template = TabAudioBtn;
        TabKeysBtn = new Button
        {
            Name = "KeysTab",
            CustomMinimumSize = template?.CustomMinimumSize ?? new Vector2(0, 38),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand
        };
        if (template != null)
        {
            TabKeysBtn.AddThemeFontOverride("font", template.GetThemeFont("font"));
            TabKeysBtn.AddThemeFontSizeOverride("font_size", template.GetThemeFontSize("font_size"));
        }
        else
        {
            TabKeysBtn.AddThemeFontSizeOverride("font_size", 14);
        }
        TabKeysBtn.Pressed += () => SwitchTab(TabKeys);
        tabBar.AddChild(TabKeysBtn);

        KeysPanel = new VBoxContainer
        {
            Name = "KeysPanel",
            Visible = false
        };
        KeysPanel.AddThemeConstantOverride("separation", 10);
        content.AddChild(KeysPanel);

        KeysHintLbl = new Label
        {
            Name = "KeysHint",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        KeysHintLbl.AddThemeFontSizeOverride("font_size", 13);
        KeysHintLbl.AddThemeColorOverride("font_color", new Color(0.65f, 0.72f, 0.8f));
        KeysPanel.AddChild(KeysHintLbl);

        // Rows scroll inside the fixed-height content area; hint + reset stay pinned.
        var keysScroll = new ScrollContainer
        {
            Name = "KeysScroll",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        KeysPanel.AddChild(keysScroll);
        KeysRows = new VBoxContainer
        {
            Name = "KeysRows",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        KeysRows.AddThemeConstantOverride("separation", 10);
        keysScroll.AddChild(KeysRows);

        foreach (string action in KeyBindings.Actions)
        {
            var row = new HBoxContainer
            {
                Name = $"KeyRow_{action}"
            };
            row.AddThemeConstantOverride("separation", 12);
            var nameLbl = new Label
            {
                Name = "ActionLabel",
                CustomMinimumSize = new Vector2(200, 0),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center
            };
            nameLbl.AddThemeFontSizeOverride("font_size", 14);
            row.AddChild(nameLbl);
            string localAction = action;
            var rebindBtn = new Button
            {
                Name = "RebindButton",
                CustomMinimumSize = new Vector2(140, 36),
                MouseDefaultCursorShape = Control.CursorShape.PointingHand
            };
            rebindBtn.AddThemeFontSizeOverride("font_size", 14);
            rebindBtn.Pressed += () => BeginRebind(localAction);
            row.AddChild(rebindBtn);
            row.SetMeta("action", action);
            KeysRows.AddChild(row);
            _keyRowButtons[action] = rebindBtn;
        }

        var fixedRow = new HBoxContainer
        {
            Name = "KeyRow_fixed_pause"
        };
        fixedRow.AddThemeConstantOverride("separation", 12);
        var fixedName = new Label
        {
            Name = "ActionLabel",
            CustomMinimumSize = new Vector2(200, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        fixedName.AddThemeFontSizeOverride("font_size", 14);
        fixedRow.AddChild(fixedName);
        var fixedVal = new Label
        {
            Name = "FixedValue",
            CustomMinimumSize = new Vector2(140, 36),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        fixedVal.AddThemeFontSizeOverride("font_size", 14);
        fixedVal.AddThemeColorOverride("font_color", new Color(0.55f, 0.62f, 0.72f));
        fixedRow.AddChild(fixedVal);
        fixedRow.SetMeta("fixed_pause", true);
        KeysRows.AddChild(fixedRow);

        KeysResetBtn = new Button
        {
            Name = "KeysResetButton",
            CustomMinimumSize = new Vector2(0, 36),
            MouseDefaultCursorShape = Control.CursorShape.PointingHand
        };
        KeysResetBtn.AddThemeFontSizeOverride("font_size", 14);
        KeysResetBtn.Pressed += OnKeysResetPressed;
        KeysPanel.AddChild(KeysResetBtn);
        RefreshKeysPanel();
    }

    public void BeginRebind(string action)
    {
        ListeningAction = action;
        RefreshKeysPanel();
    }

    private void OnKeysResetPressed()
    {
        ListeningAction = "";
        KeyBindings.ResetAll();
        RefreshKeysPanel();
    }

    /// <summary>
    /// Captures the replacement key in _Input (ahead of MainMenu's ESC-back).
    /// ESC cancels listening; pause itself is fixed and never rebound.
    /// </summary>
    public override void _Input(InputEvent @event)
    {
        if (string.IsNullOrEmpty(ListeningAction) || !Visible)
            return;
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)
            return;
        string action = ListeningAction;
        if (key.Keycode == Key.Escape)
        {
            ListeningAction = "";
            RefreshKeysPanel();
            GetViewport().SetInputAsHandled();
            return;
        }
        Key code = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
        if (code == Key.None)
            return;
        ListeningAction = "";
        KeyBindings.SetPrimary(action, code);
        RefreshKeysPanel();
        GetViewport().SetInputAsHandled();
    }

    public void RefreshKeysPanel()
    {
        if (KeysRows == null)
            return;
        foreach (var row in KeysRows.GetChildren())
        {
            if (row is HBoxContainer hbox && hbox.HasMeta("fixed_pause"))
            {
                var fixedName = hbox.GetNodeOrNull<Label>("ActionLabel");
                if (fixedName != null)
                    fixedName.Text = Tr("KEYS_ACTION_TOGGLE_PAUSE");
                var fixedVal = hbox.GetNodeOrNull<Label>("FixedValue");
                if (fixedVal != null)
                    fixedVal.Text = "ESC " + Tr("KEYS_FIXED");
                continue;
            }
            if (row is HBoxContainer hbox2 && hbox2.HasMeta("action"))
            {
                string action = hbox2.GetMeta("action").AsString();
                var nameLbl = hbox2.GetNodeOrNull<Label>("ActionLabel");
                if (nameLbl != null)
                    nameLbl.Text = Tr($"KEYS_ACTION_{action.ToUpperInvariant()}");
                if (_keyRowButtons.TryGetValue(action, out var btn))
                {
                    btn.Text = ListeningAction == action
                        ? Tr("KEYS_PRESS_HINT")
                        : KeyBindings.KeyText(KeyBindings.GetPrimary(action));
                }
            }
        }
        if (KeysResetBtn != null)
            KeysResetBtn.Text = Tr("KEYS_RESET");
    }

    public void SyncUiFromSettings()
    {
        if (MasterSlider != null)
        {
            MasterSlider.Value = SettingsManager.MasterVolume * 100.0;
            if (MasterValLbl != null)
                MasterValLbl.Text = $"{(int)MasterSlider.Value}%";
        }

        if (SfxSlider != null)
        {
            SfxSlider.Value = SettingsManager.SfxVolume * 100.0;
            if (SfxValLbl != null)
                SfxValLbl.Text = $"{(int)SfxSlider.Value}%";
        }

        if (BgmSlider != null)
        {
            BgmSlider.Value = SettingsManager.BgmVolume * 100.0;
            if (BgmValLbl != null)
                BgmValLbl.Text = $"{(int)BgmSlider.Value}%";
        }

        RefreshGraphicsStates();

        RefreshLanguageOption();
    }

    /// <summary>Right-side state buttons always show the current ON/OFF value.</summary>
    public void RefreshGraphicsStates()
    {
        if (FullscreenCheck != null)
            FullscreenCheck.Text = Tr(SettingsManager.Fullscreen ? "SETTINGS_STATE_ON" : "SETTINGS_STATE_OFF");
        if (VsyncCheck != null)
            VsyncCheck.Text = Tr(SettingsManager.Vsync ? "SETTINGS_STATE_ON" : "SETTINGS_STATE_OFF");
        if (ShakeCheck != null)
            ShakeCheck.Text = Tr(SettingsManager.ScreenShake ? "SETTINGS_STATE_ON" : "SETTINGS_STATE_OFF");
        if (PerfCheck != null)
            PerfCheck.Text = Tr(SettingsManager.PerformanceMode ? "SETTINGS_STATE_ON" : "SETTINGS_STATE_OFF");
        if (FpsCheck != null)
            FpsCheck.Text = Tr(SettingsManager.ShowFps ? "SETTINGS_STATE_ON" : "SETTINGS_STATE_OFF");
        if (MaxFpsBtn != null)
            MaxFpsBtn.Text = SettingsManager.MaxFps <= 0 ? "∞" : SettingsManager.MaxFps.ToString();
    }

    /// <summary>Selects the dropdown entry matching the current locale.</summary>
    public void RefreshLanguageOption()
    {
        if (LangOption == null)
            return;
        int idx = Array.IndexOf(LangLocales, GameManager.CurrentLanguage);
        LangOption.Selected = idx >= 0 ? idx : 1;
    }

    private void OnLanguageSelected(long index)
    {
        if (index < 0 || index >= LangLocales.Length)
            return;
        string locale = LangLocales[index];
        if (locale != GameManager.CurrentLanguage)
            GameManager.SetLanguage(locale);
    }

    private void OnMasterSliderChanged(double val)
    {
        if (MasterValLbl != null)
            MasterValLbl.Text = $"{(int)val}%";
        SettingsManager.SetMasterVolume((float)(val / 100.0));
    }

    private void OnSfxSliderChanged(double val)
    {
        if (SfxValLbl != null)
            SfxValLbl.Text = $"{(int)val}%";
        SettingsManager.SetSfxVolume((float)(val / 100.0));
    }

    private void OnBgmSliderChanged(double val)
    {
        if (BgmValLbl != null)
            BgmValLbl.Text = $"{(int)val}%";
        SettingsManager.SetBgmVolume((float)(val / 100.0));
    }

    private void OnFullscreenPressed()
    {
        SettingsManager.SetFullscreen(!SettingsManager.Fullscreen);
        RefreshGraphicsStates();
    }

    private void OnVsyncPressed()
    {
        SettingsManager.SetVsync(!SettingsManager.Vsync);
        RefreshGraphicsStates();
    }

    private void OnShakePressed()
    {
        SettingsManager.SetScreenShake(!SettingsManager.ScreenShake);
        RefreshGraphicsStates();
    }

    private void OnPerfPressed()
    {
        bool next = !SettingsManager.PerformanceMode;
        SettingsManager.SetPerformanceMode(next);
        BackdropQuality.ApplyTo(GetTree()?.CurrentScene, next);
        RefreshGraphicsStates();
    }

    private void OnFpsPressed()
    {
        bool next = !SettingsManager.ShowFps;
        SettingsManager.SetShowFps(next);
        GetTree()?.CurrentScene?.GetNodeOrNull<Hud>("HUD")?.SetFpsVisible(next);
        RefreshGraphicsStates();
    }

    /// <summary>Cycle order for the Max FPS value button.</summary>
    private static readonly int[] FpsSteps = { 0, 30, 60, 120 };

    private void OnMaxFpsPressed()
    {
        int idx = Array.IndexOf(FpsSteps, SettingsManager.MaxFps);
        int next = idx < 0 ? 0 : FpsSteps[(idx + 1) % FpsSteps.Length];
        SettingsManager.SetMaxFps(next);
        RefreshGraphicsStates();
    }

    public override void UpdateLocalizedTexts()
    {
        base.UpdateLocalizedTexts();
        if (TitleLabel != null) TitleLabel.Text = Tr("SETTINGS_TITLE");

        if (TabAudioBtn != null) TabAudioBtn.Text = Tr("SETTINGS_TAB_AUDIO");
        if (TabGraphicsBtn != null) TabGraphicsBtn.Text = Tr("SETTINGS_TAB_GRAPHICS");
        if (TabKeysBtn != null) TabKeysBtn.Text = Tr("SETTINGS_TAB_KEYS");

        if (KeysHintLbl != null) KeysHintLbl.Text = Tr("KEYS_HINT");
        RefreshKeysPanel();

        if (MasterTitleLbl != null) MasterTitleLbl.Text = Tr("SETTINGS_MASTER_VOL");
        if (SfxTitleLbl != null) SfxTitleLbl.Text = Tr("SETTINGS_SFX_VOL");
        if (BgmTitleLbl != null) BgmTitleLbl.Text = Tr("SETTINGS_BGM_VOL");

        if (FullscreenTitleLbl != null) FullscreenTitleLbl.Text = Tr("SETTINGS_FULLSCREEN");
        if (VsyncTitleLbl != null) VsyncTitleLbl.Text = Tr("SETTINGS_VSYNC");
        if (ShakeTitleLbl != null) ShakeTitleLbl.Text = Tr("SETTINGS_SCREEN_SHAKE");
        if (PerfTitleLbl != null) PerfTitleLbl.Text = Tr("SETTINGS_PERF_MODE");
        if (FpsTitleLbl != null) FpsTitleLbl.Text = Tr("SETTINGS_SHOW_FPS");
        if (MaxFpsTitleLbl != null) MaxFpsTitleLbl.Text = Tr("SETTINGS_MAX_FPS");
        RefreshGraphicsStates();

        if (LangTitleLbl != null) LangTitleLbl.Text = Tr("SETTINGS_LANGUAGE");
        RefreshLanguageOption();
    }
}
