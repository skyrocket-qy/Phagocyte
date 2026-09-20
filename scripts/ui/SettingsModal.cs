using Godot;
using System;
using Phagocyte.Core;

namespace Phagocyte.UI;

public partial class SettingsModal : ModalBase
{
    public Button? TabControlsBtn { get; set; }
    public Button? TabAudioBtn { get; set; }
    public Button? TabGraphicsBtn { get; set; }

    public VBoxContainer? ControlsPanel { get; set; }
    public VBoxContainer? AudioPanel { get; set; }
    public VBoxContainer? GraphicsPanel { get; set; }

    public Label? ControlsHeaderLbl { get; set; }
    public Label? CtrlMoveLbl { get; set; }
    public Label? CtrlPauseLbl { get; set; }
    public Label? CtrlPhagoLbl { get; set; }
    public Label? CtrlTreeLbl { get; set; }


    public HSlider? MasterSlider { get; set; }
    public Label? MasterValLbl { get; set; }
    public Label? MasterTitleLbl { get; set; }

    public HSlider? SfxSlider { get; set; }
    public Label? SfxValLbl { get; set; }
    public Label? SfxTitleLbl { get; set; }

    public HSlider? BgmSlider { get; set; }
    public Label? BgmValLbl { get; set; }
    public Label? BgmTitleLbl { get; set; }

    public CheckBox? FullscreenCheck { get; set; }
    public CheckBox? VsyncCheck { get; set; }

    public int CurrentTab { get; set; } = 0;

    public override void _Ready()
    {
        TabControlsBtn = GetNodeOrNull<Button>("VBox/TabBar/ControlsTab");
        TabAudioBtn = GetNodeOrNull<Button>("VBox/TabBar/AudioTab");
        TabGraphicsBtn = GetNodeOrNull<Button>("VBox/TabBar/GraphicsTab");

        ControlsPanel = GetNodeOrNull<VBoxContainer>("VBox/Content/ControlsPanel");
        AudioPanel = GetNodeOrNull<VBoxContainer>("VBox/Content/AudioPanel");
        GraphicsPanel = GetNodeOrNull<VBoxContainer>("VBox/Content/GraphicsPanel");

        ControlsHeaderLbl = GetNodeOrNull<Label>("VBox/Content/ControlsPanel/HeaderLabel");
        CtrlMoveLbl = GetNodeOrNull<Label>("VBox/Content/ControlsPanel/MoveRow/DescLabel");
        CtrlPauseLbl = GetNodeOrNull<Label>("VBox/Content/ControlsPanel/PauseRow/DescLabel");
        CtrlPhagoLbl = GetNodeOrNull<Label>("VBox/Content/ControlsPanel/PhagoRow/DescLabel");
        CtrlTreeLbl = GetNodeOrNull<Label>("VBox/Content/ControlsPanel/TreeRow/DescLabel");

        MasterSlider = GetNodeOrNull<HSlider>("VBox/Content/AudioPanel/MasterRow/Slider");
        MasterValLbl = GetNodeOrNull<Label>("VBox/Content/AudioPanel/MasterRow/ValLabel");
        MasterTitleLbl = GetNodeOrNull<Label>("VBox/Content/AudioPanel/MasterRow/TitleLabel");

        SfxSlider = GetNodeOrNull<HSlider>("VBox/Content/AudioPanel/SFXRow/Slider");
        SfxValLbl = GetNodeOrNull<Label>("VBox/Content/AudioPanel/SFXRow/ValLabel");
        SfxTitleLbl = GetNodeOrNull<Label>("VBox/Content/AudioPanel/SFXRow/TitleLabel");

        BgmSlider = GetNodeOrNull<HSlider>("VBox/Content/AudioPanel/BGMRow/Slider");
        BgmValLbl = GetNodeOrNull<Label>("VBox/Content/AudioPanel/BGMRow/ValLabel");
        BgmTitleLbl = GetNodeOrNull<Label>("VBox/Content/AudioPanel/BGMRow/TitleLabel");

        FullscreenCheck = GetNodeOrNull<CheckBox>("VBox/Content/GraphicsPanel/FullscreenCheck");
        VsyncCheck = GetNodeOrNull<CheckBox>("VBox/Content/GraphicsPanel/VSyncCheck");

        if (TabControlsBtn != null)
            TabControlsBtn.Pressed += () => SwitchTab(0);
        if (TabAudioBtn != null)
            TabAudioBtn.Pressed += () => SwitchTab(1);
        if (TabGraphicsBtn != null)
            TabGraphicsBtn.Pressed += () => SwitchTab(2);

        // Audio signals
        if (MasterSlider != null)
            MasterSlider.ValueChanged += OnMasterSliderChanged;
        if (SfxSlider != null)
            SfxSlider.ValueChanged += OnSfxSliderChanged;
        if (BgmSlider != null)
            BgmSlider.ValueChanged += OnBgmSliderChanged;

        // Graphics signals
        if (FullscreenCheck != null)
            FullscreenCheck.Toggled += OnFullscreenToggled;
        if (VsyncCheck != null)
            VsyncCheck.Toggled += OnVsyncToggled;

        SyncUiFromSettings();
        InitModal();
    }

    public void OpenSettings(int targetTab = 0)
    {
        SyncUiFromSettings();
        Visible = true;
        SwitchTab(targetTab);
    }

    public void CloseSettings()
    {
        CloseModal();
    }

    public void SwitchTab(int tabIdx)
    {
        CurrentTab = tabIdx;

        Color activeColor = new Color(1, 1, 1);
        Color inactiveColor = new Color(0.7f, 0.7f, 0.7f);

        if (TabControlsBtn != null)
            TabControlsBtn.Modulate = tabIdx == 0 ? activeColor : inactiveColor;
        if (TabAudioBtn != null)
            TabAudioBtn.Modulate = tabIdx == 1 ? activeColor : inactiveColor;
        if (TabGraphicsBtn != null)
            TabGraphicsBtn.Modulate = tabIdx == 2 ? activeColor : inactiveColor;

        if (ControlsPanel != null)
            ControlsPanel.Visible = tabIdx == 0;
        if (AudioPanel != null)
            AudioPanel.Visible = tabIdx == 1;
        if (GraphicsPanel != null)
            GraphicsPanel.Visible = tabIdx == 2;
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

        if (FullscreenCheck != null)
            FullscreenCheck.ButtonPressed = SettingsManager.Fullscreen;
        if (VsyncCheck != null)
            VsyncCheck.ButtonPressed = SettingsManager.Vsync;
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

    private void OnFullscreenToggled(bool toggledOn)
    {
        SettingsManager.SetFullscreen(toggledOn);
    }

    private void OnVsyncToggled(bool toggledOn)
    {
        SettingsManager.SetVsync(toggledOn);
    }

    public override void UpdateLocalizedTexts()
    {
        base.UpdateLocalizedTexts();
        if (TitleLabel != null) TitleLabel.Text = Tr("SETTINGS_TITLE");

        if (TabControlsBtn != null) TabControlsBtn.Text = Tr("SETTINGS_TAB_CONTROLS");
        if (TabAudioBtn != null) TabAudioBtn.Text = Tr("SETTINGS_TAB_AUDIO");
        if (TabGraphicsBtn != null) TabGraphicsBtn.Text = Tr("SETTINGS_TAB_GRAPHICS");

        if (ControlsHeaderLbl != null) ControlsHeaderLbl.Text = Tr("CONTROLS_TITLE");
        if (CtrlMoveLbl != null) CtrlMoveLbl.Text = Tr("CONTROLS_MOVE_DESC");
        if (CtrlPauseLbl != null) CtrlPauseLbl.Text = Tr("CONTROLS_PAUSE_DESC");
        if (CtrlPhagoLbl != null) CtrlPhagoLbl.Text = Tr("CONTROLS_PHAGO_DESC");
        if (CtrlTreeLbl != null) CtrlTreeLbl.Text = Tr("CONTROLS_TREE_DESC");

        if (MasterTitleLbl != null) MasterTitleLbl.Text = Tr("SETTINGS_MASTER_VOL");
        if (SfxTitleLbl != null) SfxTitleLbl.Text = Tr("SETTINGS_SFX_VOL");
        if (BgmTitleLbl != null) BgmTitleLbl.Text = Tr("SETTINGS_BGM_VOL");

        if (FullscreenCheck != null) FullscreenCheck.Text = Tr("SETTINGS_FULLSCREEN");
        if (VsyncCheck != null) VsyncCheck.Text = Tr("SETTINGS_VSYNC");
    }
}
