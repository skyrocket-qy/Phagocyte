using Godot;
using Godot.Collections;
using System;
using Game.Core;
using Game.Debug;

namespace Game.UI;

public partial class MainMenu : Control
{
    // Views
    public Control? TitleView { get; set; }
    public Control? ClassView { get; set; }
    public LoadoutView? LoadoutView { get; set; }
    public Control? PassiveView { get; set; }
    public Control? StageView { get; set; }
    public AchievementGalleryView? AchievementView { get; set; }
    public CodexModal? CellCodexModal { get; set; }
    public SettingsModal? CellSettingsModal { get; set; }
    public RunRecordsModal? RecordsModal { get; set; }
    public EndgameSetupModal? EndlessSetupModal { get; set; }

    // Title View Controls

    // Title View Controls
    public Label? TitleLbl { get; set; }
    public Button? StartBtn { get; set; }
    public Button? CodexBtn { get; set; }
    public Button? RecordsBtn { get; set; }
    public Button? AchievementsBtn { get; set; }

    /// <summary>
    /// Shared top-left back button: the only back affordance across all five
    /// views. Hidden on the title view; per-view target resolved by
    /// <see cref="OnGlobalBackPressed"/> from <c>_currentView</c>.
    /// </summary>
    public Button? GlobalBackBtn { get; set; }
    public Button? SettingsBtn { get; set; }
    public Button? QuitBtn { get; set; }

    // Class View Controls
    public Label? ClassHeaderLbl { get; set; }
    public VBoxContainer? ClassListContainer { get; set; }
    public Label? ClassNameLbl { get; set; }
    public Label? ClassBioLbl { get; set; }
    public Label? ClassStatsHeaderLbl { get; set; }
    public Label? ClassStatsLbl { get; set; }
    public Label? ClassSkillHeaderLbl { get; set; }
    public Label? ClassSkillLbl { get; set; }
    public Label? ClassStatusLbl { get; set; }
    public Button? ClassConfirmBtn { get; set; }
    public RadarChart? ClassRadarChart { get; set; }

    // Passive Tree View Controls
    public Label? PassiveHeaderLbl { get; set; }
    public Label? TreeLevelLbl { get; set; }
    public Label? TreePointsLbl { get; set; }
    public PassiveTreeView? TreeCanvas { get; set; }
    public StatPreviewPanel? TreeStatPanel { get; set; }
    public HBoxContainer? ProfileHBox { get; set; }
    public Button? ProfileAddBtn { get; set; }
    public Button? ProfileDeleteBtn { get; set; }
    private readonly System.Collections.Generic.List<Button> _profileTabBtns = new();
    private ButtonGroup? _profileButtonGroup;

    /// <summary>Number of build-profile tab buttons currently shown.</summary>
    public int ProfileTabCount => _profileTabBtns.Count;
    public Button? PassiveResetBtn { get; set; }
    public Button? PassiveConfirmBtn { get; set; }
    public Button? TreeZoomOutBtn { get; set; }
    public Button? TreeZoomInBtn { get; set; }

    // Stage View Controls
    public Label? StageHeaderLbl { get; set; }
    public VBoxContainer? StageListContainer { get; set; }
    public Label? OrganBadgeLbl { get; set; }
    public Label? DifficultyLbl { get; set; }
    public Label? StageNameLbl { get; set; }
    public Label? StageSubtitleLbl { get; set; }
    public Label? StageEnvLbl { get; set; }
    public Label? StageMechLbl { get; set; }
    public Label? StageThreatLbl { get; set; }
    public Label? StageLockStatusLbl { get; set; }
    public StageSelectView? HoloScanner { get; set; }
    public Button? DeployBtn { get; set; }
    public Button? EndlessBtn { get; set; }
    public OptionButton? DifficultyToggle { get; set; }

    public string ActiveClassKey { get; set; } = "macrophage";
    public string ActiveTreeClassKey { get; set; } = "macrophage";
    public string ActiveTreeNodeId { get; set; } = "";
    public string ActiveStageKey { get; set; } = "acute_wound";

    /// <summary>True when the currently inspected organ stage is still locked.</summary>
    public bool IsActiveStageLocked => !GameManager.IsStageUnlocked(ActiveStageKey);

    // Title LabelSettings per locale: EN tracking 6px, ZH tracking 9px.
    private static readonly LabelSettings TitleSettingsEn =
        AssetLoader.Load<LabelSettings>("res://assets/fonts/TitleLabelSettings.tres");
    private static readonly LabelSettings TitleSettingsZh =
        AssetLoader.Load<LabelSettings>("res://assets/fonts/TitleZhLabelSettings.tres");

    private Callable _langCallback;

    /// <summary>View most recently shown by <see cref="SwitchToView"/>; drives the global back target.</summary>
    private Control? _currentView;

    /// <summary>GFP watermark pulse state (alpha 0.2-0.3 breathing + slow drift).</summary>
    public TextureRect? Watermark { get; set; }
    private Vector2 _watermarkBasePos;
    private double _watermarkTime;

    public override void _Process(double delta)
    {
        if (Watermark == null)
            return;
        _watermarkTime += delta;
        double phase = _watermarkTime * Math.Tau / 4.0;
        var mod = Watermark.Modulate;
        mod.A = 0.25f + 0.05f * (float)Math.Sin(phase);
        Watermark.Modulate = mod;
        Watermark.Position = _watermarkBasePos
            + new Vector2(8.0f * (float)Math.Sin(phase * 0.5), 6.0f * (float)Math.Cos(phase * 0.37));
    }

    public override void _Ready()
    {
        TitleView = GetNodeOrNull<Control>("TitleView");
        ClassView = GetNodeOrNull<Control>("ClassView");
        LoadoutView = GetNodeOrNull<LoadoutView>("LoadoutView");
        PassiveView = GetNodeOrNull<Control>("PassiveView");
        StageView = GetNodeOrNull<Control>("MapView");
        AchievementView = GetNodeOrNull<AchievementGalleryView>("AchievementView");
        CellCodexModal = GetNodeOrNull<CodexModal>("CodexModal");
        CellSettingsModal = GetNodeOrNull<SettingsModal>("SettingsModal");
        RecordsModal = GetNodeOrNull<RunRecordsModal>("RunRecordsModal");

        Watermark = GetNodeOrNull<TextureRect>("Watermark");
        if (Watermark != null)
            _watermarkBasePos = Watermark.Position;

        TitleLbl = GetNodeOrNull<Label>("TitleView/TitleLabel");
        StartBtn = GetNodeOrNull<Button>("TitleView/VBox/StartButton");
        CodexBtn = GetNodeOrNull<Button>("TitleView/VBox/CodexButton");
        RecordsBtn = GetNodeOrNull<Button>("TitleView/VBox/RecordsButton");
        AchievementsBtn = GetNodeOrNull<Button>("TitleView/VBox/AchievementsButton");
        GlobalBackBtn = GetNodeOrNull<Button>("GlobalBackButton");
        SettingsBtn = GetNodeOrNull<Button>("TitleView/VBox/SettingsButton");
        QuitBtn = GetNodeOrNull<Button>("TitleView/VBox/QuitButton");

        ClassHeaderLbl = GetNodeOrNull<Label>("ClassView/PageHeader/Title");
        ClassListContainer = GetNodeOrNull<VBoxContainer>("ClassView/HBox/ClassList");
        ClassNameLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassNameLabel");
        ClassBioLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassBioLabel");
        ClassStatsHeaderLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassStatsHeader");
        ClassStatsLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/StatsRow/ClassStatsLabel")
            ?? GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassStatsLabel");
        ClassSkillHeaderLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassSkillHeader");
        ClassSkillLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassSkillLabel");
        ClassStatusLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassStatusLabel");
        ClassConfirmBtn = GetNodeOrNull<Button>("ClassView/Buttons/ConfirmButton");
        ClassRadarChart = GetNodeOrNull<RadarChart>("ClassView/HBox/DetailPanel/VBox/StatsRow/RadarChart")
            ?? GetNodeOrNull<RadarChart>("ClassView/HBox/DetailPanel/VBox/RadarChart");

        PassiveHeaderLbl = GetNodeOrNull<Label>("PassiveView/PageHeader/Title");
        TreeLevelLbl = GetNodeOrNull<Label>("PassiveView/InfoHBox/TreeLevelLabel");
        TreePointsLbl = GetNodeOrNull<Label>("PassiveView/InfoHBox/TreePointsLabel");
        TreeCanvas = GetNodeOrNull<PassiveTreeView>("PassiveView/ContentHBox/TreeView");
        TreeStatPanel = GetNodeOrNull<StatPreviewPanel>("PassiveView/ContentHBox/StatPreview");
        PassiveResetBtn = GetNodeOrNull<Button>("PassiveView/Buttons/ResetButton");
        PassiveConfirmBtn = GetNodeOrNull<Button>("PassiveView/Buttons/ConfirmButton");
        TreeZoomOutBtn = GetNodeOrNull<Button>("PassiveView/Buttons/ZoomOutButton");
        TreeZoomInBtn = GetNodeOrNull<Button>("PassiveView/Buttons/ZoomInButton");

        StageHeaderLbl = GetNodeOrNull<Label>("MapView/PageHeader/Title");
        StageListContainer = GetNodeOrNull<VBoxContainer>("MapView/HBox/MapList");
        OrganBadgeLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/TopHBox/OrganBadge");
        DifficultyLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/TopHBox/DifficultyLabel");
        StageNameLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/TitleVBox/MapNameLabel")
            ?? GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/MapNameLabel");
        StageSubtitleLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/TitleVBox/MapSubtitleLabel");
        StageEnvLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/MapEnvLabel");
        StageMechLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/MapMechLabel");
        StageThreatLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/MapThreatLabel");
        HoloScanner = GetNodeOrNull<StageSelectView>("MapView/HBox/StageSelectMap");
        DeployBtn = GetNodeOrNull<Button>("MapView/Buttons/DeployButton");

        // Endless Cytokine Storm entry (docs/endgame.md §2), injected beside Deploy.
        // Stays locked until any organ has been cleared on Hard.
        if (DeployBtn != null && DeployBtn.GetParent() is Container deployRow)
        {
            EndlessBtn = new Button
            {
                Name = "EndlessButton",
                CustomMinimumSize = new Vector2(230, 40)
            };
            EndlessBtn.AddThemeColorOverride("font_color", new Color(1.0f, 0.78f, 0.35f));
            EndlessBtn.Pressed += () =>
            {
                if (!GameManager.IsStageUnlocked(ActiveStageKey))
                {
                    SelectStage(ActiveStageKey);
                    return;
                }
                GameManager.SelectedStage = ActiveStageKey;
                EndlessSetupModal?.OpenSetup();
            };
            deployRow.AddChild(EndlessBtn);
            deployRow.MoveChild(EndlessBtn, DeployBtn.GetIndex() + 1);
        }

        // Dual-track difficulty toggle (docs/stages.md §2): Normal vs Hard,
        // unlocked per organ by clearing the prerequisite stage on Normal.
        if (DeployBtn != null && DeployBtn.GetParent() is Container difficultyRow)
        {
            DifficultyToggle = new OptionButton
            {
                Name = "DifficultyToggle",
                CustomMinimumSize = new Vector2(170, 40)
            };
            DifficultyToggle.AddItem("", 0);
            DifficultyToggle.AddItem("", 1);
            DifficultyToggle.ItemSelected += OnDifficultySelected;
            difficultyRow.AddChild(DifficultyToggle);
            difficultyRow.MoveChild(DifficultyToggle, DeployBtn.GetIndex());
        }

        // Pre-run affliction setup for the endless overdrive (docs/endgame.md §4).
        EndlessSetupModal = AssetLoader.Load<PackedScene>("res://scenes/ui/endgame_setup_modal.tscn").Instantiate<EndgameSetupModal>();
        EndlessSetupModal.Name = "EndgameSetupModal";
        AddChild(EndlessSetupModal);

        // Lock status readout injected under the threat rows (StageData-driven)
        if (StageThreatLbl != null && StageThreatLbl.GetParent() is Control detailPanel)
        {
            StageLockStatusLbl = new Label
            {
                Name = "MapLockStatusLabel",
                Visible = false,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(0, 36)
            };
            StageLockStatusLbl.AddThemeColorOverride("font_color", new Color(1.0f, 0.45f, 0.4f));
            StageLockStatusLbl.AddThemeFontSizeOverride("font_size", 13);
            detailPanel.AddChild(StageLockStatusLbl);
        }

        if (HoloScanner != null)
        {
            HoloScanner.StageSelected += (string stageKey) => SelectStage(stageKey);
        }

        if (TitleView != null)
            SwitchToView(TitleView);
        if (CellCodexModal != null)
            CellCodexModal.Visible = false;
        if (CellSettingsModal != null)
            CellSettingsModal.Visible = false;
        if (RecordsModal != null)
            RecordsModal.Visible = false;

        AudioManager.Instance?.PlayBgm("menu");

        _langCallback = Callable.From((string _) => UpdateAllTexts());
        GameManager.AddLanguageListener(_langCallback);

        if (StartBtn != null)
            StartBtn.Pressed += OnStartPressed;
        if (CodexBtn != null)
            CodexBtn.Pressed += () => CellCodexModal?.OpenCodex(0);
        if (RecordsBtn != null)
            RecordsBtn.Pressed += () => RecordsModal?.OpenHistory();
        if (AchievementsBtn != null)
            AchievementsBtn.Pressed += () =>
            {
                if (AchievementView != null)
                {
                    SwitchToView(AchievementView);
                    AchievementView.Open();
                }
            };
        if (GlobalBackBtn != null)
            GlobalBackBtn.Pressed += OnGlobalBackPressed;
        if (SettingsBtn != null)
            SettingsBtn.Pressed += () => CellSettingsModal?.OpenSettings(0);
        if (QuitBtn != null)
            QuitBtn.Pressed += () => GetTree().Quit();

        if (ClassConfirmBtn != null)
            ClassConfirmBtn.Pressed += OnClassConfirmPressed;

        if (LoadoutView != null)
            LoadoutView.Confirmed += OnLoadoutConfirmPressed;

        if (TreeCanvas != null)
        {
            TreeCanvas.TreeNodeActivated += OnTreeNodeActivated;
            TreeCanvas.TreeNodeHovered += OnTreeNodeHovered;
            TreeCanvas.TreeNodeRefundRequested += OnTreeNodeRefundRequested;
        }
        // Build-profile tabs (docs/passivetree.md §5.4): dynamic slots, up to MaxProfiles.
        // Shifted right (+130px) so the tabs clear the shared top-left
        // GlobalBackButton (24,24)-(164,64). They sit in their own row below
        // HeaderLabel (40-85) + InfoHBox (90-110), above ContentHBox (160+).
        if (PassiveView != null)
        {
            _profileButtonGroup = new ButtonGroup { AllowUnpress = false };
            ProfileHBox = new HBoxContainer
            {
                Name = "ProfileHBox",
                MouseFilter = Control.MouseFilterEnum.Pass,
                AnchorLeft = 0.5f,
                AnchorTop = 0.5f,
                AnchorRight = 0.5f,
                AnchorBottom = 0.5f,
                OffsetLeft = -460.0f,
                OffsetTop = -244.0f,
                OffsetRight = 100.0f,
                OffsetBottom = -204.0f
            };
            ProfileHBox.AddThemeConstantOverride("separation", 8);
            ProfileAddBtn = new Button
            {
                Name = "ProfileAddButton",
                CustomMinimumSize = new Vector2(56, 40),
                MouseDefaultCursorShape = Control.CursorShape.PointingHand
            };
            ProfileAddBtn.Pressed += OnProfileAddPressed;
            ProfileDeleteBtn = new Button
            {
                Name = "ProfileDeleteButton",
                CustomMinimumSize = new Vector2(96, 40),
                MouseDefaultCursorShape = Control.CursorShape.PointingHand
            };
            ProfileDeleteBtn.AddThemeColorOverride("font_color", new Color(1.0f, 0.45f, 0.4f));
            ProfileDeleteBtn.Pressed += OnProfileDeletePressed;
            ProfileHBox.AddChild(ProfileAddBtn);
            ProfileHBox.AddChild(ProfileDeleteBtn);
            PassiveView.AddChild(ProfileHBox);
        }
        // Level + points readouts share the top-right corner, clear of the
        // profile tabs docked at the tree canvas top-left.
        if (TreeLevelLbl?.GetParent() is HBoxContainer infoRow
            && TreePointsLbl != null)
        {
            infoRow.Alignment = BoxContainer.AlignmentMode.End;
            TreeLevelLbl.SizeFlagsHorizontal = Control.SizeFlags.Fill;
            TreePointsLbl.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        }
        if (PassiveResetBtn != null)
            PassiveResetBtn.Pressed += OnTreeResetPressed;
        if (PassiveConfirmBtn != null)
            PassiveConfirmBtn.Pressed += OnPassiveConfirmPressed;
        if (TreeZoomOutBtn != null)
            TreeZoomOutBtn.Pressed += () => TreeCanvas?.ZoomStep(1.0f / 1.2f);
        if (TreeZoomInBtn != null)
            TreeZoomInBtn.Pressed += () => TreeCanvas?.ZoomStep(1.2f);

        if (DeployBtn != null)
            DeployBtn.Pressed += OnDeployPressed;

        // Test-demand full-unlock cheat (debug builds only, --cheats=all / --cheats-reset).
        CheatTools.ApplyHeadedMenuCheats();

        UpdateAllTexts();

        AudioManager.Instance?.WireClicks(this);
    }

    /// <summary>
    /// Debug-only test hotkeys (no CLI args needed, so plain editor F5 runs
    /// work): F9 unlocks all meta progression, F10 resets to a fresh
    /// profile. No-op in release builds.
    /// ESC (toggle_pause) acts as the global back affordance: it closes the
    /// topmost open modal first, otherwise it triggers <see cref="OnGlobalBackPressed"/>.
    /// </summary>
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (IsEscapePressed(@event))
        {
            if (TryHandleEscapeAsBack())
                GetViewport().SetInputAsHandled();
            return;
        }
        if (!OS.IsDebugBuild() || @event is not InputEventKey key || !key.Pressed || key.Echo)
            return;
        if (key.Keycode == Key.F9)
        {
            CheatTools.UnlockAllMeta();
            GD.Print("[Cheats] All meta progression unlocked (F9).");
            RefreshCheatViews();
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.F10)
        {
            CheatTools.LockToBaseline();
            GD.Print("[Cheats] Meta progression reset to a fresh profile (F10).");
            RefreshCheatViews();
            GetViewport().SetInputAsHandled();
        }
    }

    private void RefreshCheatViews()
    {
        UpdateAllTexts();
        if (ClassView != null && ClassView.Visible)
            SetupClassButtons();
        if (StageView != null && StageView.Visible)
            SelectStage(ActiveStageKey);
    }

    private static bool IsEscapePressed(InputEvent @event)
    {
        if (@event is InputEventKey echoKey && echoKey.Echo)
            return false;
        if (@event.IsActionPressed("toggle_pause"))
            return true;
        return @event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape;
    }

    /// <summary>
    /// ESC-as-back: close the topmost open modal first; otherwise trigger the
    /// shared back button when it is visible. Settlement-mode records must pick
    /// Retry/Menu, so ESC there is consumed without navigating behind the modal.
    /// Returns true when the key was consumed.
    /// </summary>
    public bool TryHandleEscapeAsBack()
    {
        if (EndlessSetupModal != null && EndlessSetupModal.Visible)
        {
            EndlessSetupModal.CloseModal();
            return true;
        }
        if (CellCodexModal != null && CellCodexModal.Visible)
        {
            CellCodexModal.CloseModal();
            return true;
        }
        if (CellSettingsModal != null && CellSettingsModal.Visible)
        {
            CellSettingsModal.CloseModal();
            return true;
        }
        if (RecordsModal != null && RecordsModal.Visible)
        {
            if (!RecordsModal.SettlementMode)
                RecordsModal.CloseModal();
            return true;
        }
        if (GlobalBackBtn != null && GlobalBackBtn.Visible)
        {
            OnGlobalBackPressed();
            return true;
        }
        return false;
    }

    public override void _ExitTree()
    {
        GameManager.RemoveLanguageListener(_langCallback);
    }

    public void UpdateAllTexts()
    {
        if (TitleLbl != null)
        {
            TitleLbl.Text = Tr("TITLE_MAIN");
            TitleLbl.LabelSettings = GameManager.CurrentLanguage == "en"
                ? TitleSettingsEn
                : TitleSettingsZh;
        }
        if (StartBtn != null) StartBtn.Text = Tr("BTN_START");
        if (CodexBtn != null) CodexBtn.Text = Tr("BTN_CODEX");
        if (RecordsBtn != null) RecordsBtn.Text = Tr("BTN_RECORDS");
        if (AchievementsBtn != null) AchievementsBtn.Text = Tr("BTN_ACHIEVEMENTS");
        if (GlobalBackBtn != null) GlobalBackBtn.Text = Tr("NAV_BACK");
        if (SettingsBtn != null) SettingsBtn.Text = Tr("BTN_SETTINGS");
        if (QuitBtn != null) QuitBtn.Text = Tr("BTN_QUIT");

        if (ClassHeaderLbl != null) ClassHeaderLbl.Text = Tr("HEADER_SELECT_CLASS");

        LoadoutView?.UpdateLocalizedTexts();

        if (PassiveHeaderLbl != null) PassiveHeaderLbl.Text = Tr("HEADER_SELECT_PASSIVE");
        if (PassiveResetBtn != null) PassiveResetBtn.Text = Tr("TREE_RESET");
        if (PassiveConfirmBtn != null) PassiveConfirmBtn.Text = Tr("BTN_CONFIRM_MAP");
        if (TreeZoomOutBtn != null) TreeZoomOutBtn.Text = Tr("TREE_ZOOM_OUT");
        RefreshProfileTexts();
        if (TreeZoomInBtn != null) TreeZoomInBtn.Text = Tr("TREE_ZOOM_IN");

        if (StageHeaderLbl != null) StageHeaderLbl.Text = Tr("HEADER_SELECT_MAP");
        if (DeployBtn != null) DeployBtn.Text = Tr("BTN_DEPLOY");
        if (DifficultyToggle != null)
        {
            DifficultyToggle.SetItemText(0, Tr("DIFFICULTY_NORMAL"));
            DifficultyToggle.SetItemText(1, Tr("DIFFICULTY_HARD"));
            RefreshDifficultyToggle();
        }
        if (EndlessBtn != null)
        {
            EndlessBtn.Text = Tr("BTN_ENDLESS");
            UpdateEndlessAvailability();
        }

        SetupClassButtons();
        SetupStageButtons();
        SelectClass(ActiveClassKey);
        RefreshPassiveView();
        RefreshLoadoutView();
        SelectStage(ActiveStageKey);
    }

    private void SwitchToView(Control targetView)
    {
        Control?[] views = { TitleView, ClassView, LoadoutView, PassiveView, StageView, AchievementView };
        foreach (var view in views)
        {
            if (view != null)
                view.Visible = view == targetView;
        }
        _currentView = targetView;
        if (GlobalBackBtn != null)
            GlobalBackBtn.Visible = targetView != TitleView;
    }

    /// <summary>
    /// The single back affordance for all views. Targets mirror the forward
    /// flow: Stage re-enters the passive build (refreshing the tree), Passive
    /// returns to the gear loadout, Loadout returns to class selection,
    /// everything else to title.
    /// </summary>
    public void OnGlobalBackPressed()
    {
        if (_currentView == StageView && PassiveView != null)
        {
            SelectPassiveBuild(ActiveTreeClassKey);
            SwitchToView(PassiveView);
        }
        else if (_currentView == PassiveView && LoadoutView != null)
        {
            RefreshLoadoutView();
            SwitchToView(LoadoutView);
        }
        else if (_currentView == LoadoutView && ClassView != null)
        {
            SwitchToView(ClassView);
        }
        else if (TitleView != null)
        {
            SwitchToView(TitleView);
        }
    }

    private readonly System.Collections.Generic.Dictionary<string, Button> _classButtons = new();

    private static StyleBoxFlat MakeClassBtnStyle(Color bg, Color border, float borderWidth = 1.5f)
    {
        return new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = (int)borderWidth,
            BorderWidthTop = (int)borderWidth,
            BorderWidthRight = (int)borderWidth,
            BorderWidthBottom = (int)borderWidth,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomRight = 10,
            CornerRadiusBottomLeft = 3,
            ContentMarginLeft = 14.0f,
            ContentMarginRight = 14.0f
        };
    }

    public void SetupClassButtons()
    {
        if (ClassListContainer == null)
            return;

        foreach (var keyVar in GameManager.PlayerClassData.Keys)
        {
            string key = keyVar.AsString();
            var data = GameManager.GetPlayerClass(key);
            bool unlocked = data.TryGetValue("unlocked", out Variant uVal) && uVal.AsBool();

            // Wire once; texts/styles refresh on every call (language/cheat changes).
            if (!_classButtons.TryGetValue(key, out var btn) || !IsInstanceValid(btn))
            {
                btn = ClassListContainer.GetNodeOrNull<Button>($"ClassBtn_{key}");
                if (btn == null)
                    continue;
                btn.AddThemeFontSizeOverride("font_size", 15);
                string localKey = key;
                btn.Pressed += () => SelectClass(localKey);
                _classButtons[key] = btn;
            }
            btn.Text = (unlocked ? " ✅ " : " 🔒 ") + data["name"].AsString();
            btn.AddThemeStyleboxOverride("normal", MakeClassBtnStyle(new Color(0.04f, 0.08f, 0.12f, 0.75f), new Color(0.18f, 0.40f, 0.60f, 0.5f)));
            btn.AddThemeStyleboxOverride("hover", MakeClassBtnStyle(new Color(0.06f, 0.13f, 0.19f, 0.85f), new Color(0.35f, 0.85f, 1.0f, 0.85f)));
            btn.AddThemeStyleboxOverride("pressed", MakeClassBtnStyle(new Color(0.08f, 0.18f, 0.26f, 0.95f), new Color(0.50f, 0.95f, 1.0f, 1.0f)));
            btn.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        }
        AudioManager.Instance?.WireClicks(ClassListContainer);
    }

    public void SelectClass(string key)
    {
        ActiveClassKey = key;
        var data = GameManager.GetPlayerClass(key);
        bool unlocked = data.TryGetValue("unlocked", out Variant unlockedVal) && unlockedVal.AsBool();

        // Highlight selected button
        foreach (var (k, b) in _classButtons)
        {
            bool sel = k == key;
            b.AddThemeStyleboxOverride("normal", sel
                ? MakeClassBtnStyle(new Color(0.08f, 0.18f, 0.26f, 0.90f), new Color(0.35f, 0.95f, 1.0f, 0.95f), 2.0f)
                : MakeClassBtnStyle(new Color(0.04f, 0.08f, 0.12f, 0.75f), new Color(0.18f, 0.40f, 0.60f, 0.5f)));
            b.AddThemeColorOverride("font_color", sel ? Colors.White : new Color(0.75f, 0.85f, 0.90f));
        }

        if (ClassNameLbl != null && data.TryGetValue("name", out Variant nameVal))
            ClassNameLbl.Text = nameVal.AsString();
        if (ClassBioLbl != null)
        {
            string bioKey = data.TryGetValue("bio_key", out Variant bioKeyVal) ? bioKeyVal.AsString() : "";
            ClassBioLbl.Text = Tr("CODEX_HEADER_BIO") + "\n" + (string.IsNullOrEmpty(bioKey) ? "" : Tr(bioKey));
        }
        // No fixed cell roles: builds are player-defined.
        if (ClassStatsHeaderLbl != null)
            ClassStatsHeaderLbl.Text = Tr("CLASS_STATS_HEADER");
        if (ClassStatsLbl != null)
            ClassStatsLbl.Text = UiBuilders.BuildClassVitalsText(data);

        // Update Bio-Radar Chart
        if (ClassRadarChart != null)
        {
            float hp = data.TryGetValue("base_hp", out Variant hpVal) ? hpVal.AsSingle() : 100.0f;
            float speed = data.TryGetValue("base_speed", out Variant spVal) ? spVal.AsSingle() : 230.0f;
            float armor = data.TryGetValue("base_armor", out Variant arVal) ? arVal.AsSingle() : 0.0f;
            string sigStat = data.TryGetValue("trait_stat", out Variant sigVal) ? sigVal.AsString() : "";
            float sigNum = data.TryGetValue("trait_stat_value", out Variant signVal) ? signVal.AsSingle() : 0.0f;

            float normHp = Mathf.Clamp((hp - 60.0f) / 100.0f, 0.15f, 1.0f);
            float normArmor = Mathf.Clamp(armor / 15.0f, 0.12f, 1.0f);
            float normSpeed = Mathf.Clamp((speed - 180.0f) / 90.0f, 0.15f, 1.0f);
            float normTrait = sigStat switch
            {
                "block" => Mathf.Clamp(sigNum / 0.10f, 0.25f, 1.0f),
                "crit_chance" => Mathf.Clamp(sigNum / 0.15f, 0.25f, 1.0f),
                "might" => Mathf.Clamp((sigNum - 1.0f) / 0.5f, 0.25f, 1.0f),
                "projectile_speed" => Mathf.Clamp((sigNum - 1.0f) / 0.5f, 0.25f, 1.0f),
                "magnet" => Mathf.Clamp((sigNum - 1.0f) / 0.5f, 0.25f, 1.0f),
                _ => 0.45f
            };

            string vitalsVal = $"{hp:F0}";
            string armorVal = $"{armor:F0}";
            string speedVal = $"{speed:F0}";
            string traitVal = UiBuilders.FormatSignatureStat(sigStat, sigNum);

            ClassRadarChart.SetStats(
                normHp, normArmor, normSpeed, normTrait,
                vitalsVal, armorVal, speedVal, traitVal,
                PassiveTreeManager.GetStatLabel("max_health"),
                PassiveTreeManager.GetStatLabel("armor"),
                PassiveTreeManager.GetStatLabel("move_speed"),
                UiBuilders.SignatureStatLabel(sigStat));
        }

        if (ClassSkillHeaderLbl != null)
            ClassSkillHeaderLbl.Text = Tr("CLASS_SKILL_HEADER");
        if (ClassSkillLbl != null)
        {
            var (skillText, skillImagePath) = UiBuilders.BuildClassSkillText(key);
            ClassSkillLbl.Text = skillText;
            var parent = ClassSkillLbl.GetParent() as VBoxContainer;
            var skillIconTex = parent?.GetNodeOrNull<TextureRect>("ClassSkillTexture");
            if (skillIconTex == null && parent != null)
            {
                skillIconTex = new TextureRect
                {
                    Name = "ClassSkillTexture",
                    CustomMinimumSize = new Vector2(48, 48),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                    MouseFilter = MouseFilterEnum.Ignore
                };
                parent.AddChild(skillIconTex);
                parent.MoveChild(skillIconTex, ClassSkillLbl.GetIndex());
            }
            if (skillIconTex != null)
                skillIconTex.Texture = AssetLoader.TryLoad<Texture2D>(skillImagePath)
                    ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
        }

        if (ClassStatusLbl != null)
        {
            if (unlocked)
            {
                ClassStatusLbl.Text = "";
                ClassStatusLbl.Visible = false;
            }
            else
            {
                ClassStatusLbl.Text = AchievementManager.GetCellUnlockRequirementText(key);
                ClassStatusLbl.Modulate = new Color(1.0f, 0.68f, 0.25f);
                ClassStatusLbl.Visible = true;
            }
        }

        if (ClassConfirmBtn != null)
        {
            ClassConfirmBtn.Disabled = !unlocked;
            ClassConfirmBtn.Text = unlocked ? Tr("BTN_CONFIRM_PASSIVE") : Tr("BTN_CONFIRM_CLASS_LOCKED");
        }
    }

    public void OnStartPressed()
    {
        if (ClassView != null)
            SwitchToView(ClassView);
        SelectClass(ActiveClassKey);
    }

    public void OnClassConfirmPressed()
    {
        GameManager.SelectedClass = ActiveClassKey;
        ActiveTreeNodeId = "";
        RefreshLoadoutView();
        if (LoadoutView != null)
        {
            SwitchToView(LoadoutView);
            return;
        }
        SelectPassiveBuild(ActiveClassKey);
        if (PassiveView != null)
            SwitchToView(PassiveView);
    }

    /// <summary>Loads the active cell's loadout profile into the page.</summary>
    public void RefreshLoadoutView()
    {
        LoadoutView?.Open(ActiveClassKey);
    }

    /// <summary>Loadout page confirm: advance to the talent tree.</summary>
    public void OnLoadoutConfirmPressed()
    {
        SelectPassiveBuild(ActiveClassKey);
        if (PassiveView != null)
            SwitchToView(PassiveView);
    }

    public void SelectPassiveBuild(string classKey)
    {
        ActiveTreeClassKey = string.IsNullOrEmpty(classKey) ? ActiveClassKey : classKey;
        string startNode = PassiveTreeManager.GetStartNode(ActiveTreeClassKey);
        if (!PassiveTreeManager.IsKnownNode(ActiveTreeNodeId))
            ActiveTreeNodeId = startNode;

        RefreshPassiveView();
    }

    public void RefreshPassiveView()
    {
        int level = PassiveTreeManager.GetCellLevel(ActiveTreeClassKey);
        int available = PassiveTreeManager.GetPointsAvailable(ActiveTreeClassKey);
        int spent = PassiveTreeManager.GetSpentPoints(ActiveTreeClassKey);

        TreeCanvas?.Render(ActiveTreeClassKey);
        TreeStatPanel?.Refresh(ActiveTreeClassKey);
        if (TreeLevelLbl != null) TreeLevelLbl.Text = TextFormatter.Format(Tr("TREE_LEVEL"), level)
            + (level >= PassiveTreeManager.MaxCellLevel ? " MAX" : "");
        if (TreePointsLbl != null) TreePointsLbl.Text = TextFormatter.Format(Tr("TREE_POINTS"), available, spent);
        RefreshProfileTabs();
    }

    private static readonly StyleBoxFlat ProfileTabActiveStyle = MakeProfileTabStyle(true, false);
    private static readonly StyleBoxFlat ProfileTabInactiveStyle = MakeProfileTabStyle(false, false);
    private static readonly StyleBoxFlat ProfileTabHoverStyle = MakeProfileTabStyle(false, true);
    private static readonly Color ProfileTabActiveFont = new(0.94f, 0.99f, 0.98f);
    private static readonly Color ProfileTabInactiveFont = new(0.55f, 0.62f, 0.72f);

    /// <summary>
    /// Connected-tab look: square bottom corners and a background close to the
    /// tree canvas top, so the active tab merges into the panel. The active tab
    /// additionally drops its bottom border; inactive tabs keep a dim one.
    /// </summary>
    private static StyleBoxFlat MakeProfileTabStyle(bool active, bool hover)
    {
        float alpha = active ? 0.5f : 0.22f;
        var style = new StyleBoxFlat
        {
            BgColor = active || hover
                ? new Color(0.032f, 0.068f, 0.118f, 1.0f)
                : new Color(0.014f, 0.030f, 0.052f, 1.0f),
            BorderColor = new Color(0.35f, 0.92f, 1.0f, hover ? 0.65f : alpha)
        };
        style.SetBorderWidthAll(1);
        if (active)
            style.BorderWidthBottom = 0;
        style.SetCornerRadiusAll(6);
        style.CornerRadiusBottomLeft = 0;
        style.CornerRadiusBottomRight = 0;
        return style;
    }

    private void RefreshProfileTabs()
    {
        if (ProfileHBox == null)
            return;

        foreach (var old in _profileTabBtns)
        {
            if (IsInstanceValid(old))
            {
                ProfileHBox.RemoveChild(old);
                old.QueueFree();
            }
        }
        _profileTabBtns.Clear();

        int count = PassiveTreeManager.GetProfileCount(ActiveTreeClassKey);
        int active = PassiveTreeManager.GetActiveProfile(ActiveTreeClassKey);
        for (int i = 0; i < count; i++)
        {
            int index = i;
            var tab = new Button
            {
                Name = $"ProfileTab{index}",
                CustomMinimumSize = new Vector2(120, 40),
                ToggleMode = true,
                ButtonGroup = _profileButtonGroup,
                ButtonPressed = index == active,
                MouseDefaultCursorShape = Control.CursorShape.PointingHand
            };
            tab.AddThemeStyleboxOverride("normal", ProfileTabInactiveStyle);
            tab.AddThemeStyleboxOverride("pressed", ProfileTabActiveStyle);
            tab.AddThemeStyleboxOverride("hover", ProfileTabHoverStyle);
            tab.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
            tab.AddThemeFontSizeOverride("font_size", 13);
            tab.AddThemeColorOverride("font_color", ProfileTabInactiveFont);
            tab.AddThemeColorOverride("font_hover_color", ProfileTabActiveFont);
            tab.AddThemeColorOverride("font_pressed_color", ProfileTabActiveFont);
            tab.Pressed += () => OnProfileTabPressed(index);
            _profileTabBtns.Add(tab);
            ProfileHBox.AddChild(tab);
            ProfileHBox.MoveChild(tab, index);
        }
        RefreshProfileTexts();
    }

    private void RefreshProfileTexts()
    {
        int count = PassiveTreeManager.GetProfileCount(ActiveTreeClassKey);
        int active = PassiveTreeManager.GetActiveProfile(ActiveTreeClassKey);
        for (int i = 0; i < _profileTabBtns.Count && i < count; i++)
        {
            _profileTabBtns[i].Text = PassiveTreeManager.GetProfileName(i);
            _profileTabBtns[i].ButtonPressed = i == active;
        }
        if (ProfileAddBtn != null)
        {
            ProfileAddBtn.Text = Tr("TREE_PROFILE_ADD");
            ProfileAddBtn.Visible = count < PassiveTreeManager.MaxProfiles;
        }
        if (ProfileDeleteBtn != null)
        {
            ProfileDeleteBtn.Text = Tr("TREE_PROFILE_DELETE");
            ProfileDeleteBtn.Disabled = count <= 1;
        }
    }

    public void OnProfileTabPressed(int index)
    {
        PassiveTreeManager.SetActiveProfile(ActiveTreeClassKey, index);
        ActiveTreeNodeId = PassiveTreeManager.GetStartNode(ActiveTreeClassKey);
        RefreshPassiveView();
    }

    public void OnProfileAddPressed()
    {
        if (!PassiveTreeManager.AddProfile(ActiveTreeClassKey))
            return;
        ActiveTreeNodeId = PassiveTreeManager.GetStartNode(ActiveTreeClassKey);
        RefreshPassiveView();
    }

    public void OnProfileDeletePressed()
    {
        int active = PassiveTreeManager.GetActiveProfile(ActiveTreeClassKey);
        if (!PassiveTreeManager.DeleteProfile(ActiveTreeClassKey, active))
            return;
        ActiveTreeNodeId = PassiveTreeManager.GetStartNode(ActiveTreeClassKey);
        RefreshPassiveView();
    }

    public void OnTreeNodeActivated(string nodeId)
    {
        ActiveTreeNodeId = nodeId;
        TreeCanvas?.SelectNode(nodeId);
        PassiveTreeManager.Purchase(ActiveTreeClassKey, nodeId);
        RefreshPassiveView();
    }

    public void OnTreeNodeHovered(string nodeId)
    {
        if (PassiveTreeManager.IsKnownNode(nodeId))
            ActiveTreeNodeId = nodeId;
    }

    public void OnTreeNodeRefundRequested(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
            return;
        ActiveTreeNodeId = nodeId;
        TreeCanvas?.SelectNode(nodeId);
        PassiveTreeManager.RefundNode(ActiveTreeClassKey, nodeId);
        RefreshPassiveView();
    }

    public void OnTreeResetPressed()
    {
        PassiveTreeManager.ResetAllocation(ActiveTreeClassKey);
        ActiveTreeNodeId = PassiveTreeManager.GetStartNode(ActiveTreeClassKey);
        RefreshPassiveView();
    }

    public void OnPassiveConfirmPressed()
    {
        GameManager.SelectedClass = ActiveTreeClassKey;
        if (StageView != null)
            SwitchToView(StageView);
        SelectStage(ActiveStageKey);
    }

    public void SetupStageButtons()
    {
        if (StageListContainer == null)
            return;

        foreach (var keyVar in GameManager.StageData.Keys)
        {
            string key = keyVar.AsString();
            var data = GameManager.GetStageInfo(key);
            string icon = data.TryGetValue("organ_icon", out var icVal) ? icVal.AsString() : "🌐";
            string name = data.TryGetValue("name", out var nmVal) ? nmVal.AsString() : key;
            int diff = data.TryGetValue("difficulty", out var diffVal) ? diffVal.AsInt32() : 1;
            string stars = new string('★', diff) + new string('☆', 5 - diff);
            bool unlocked = GameManager.IsStageUnlocked(key);

            var btn = StageListContainer.GetNodeOrNull<Button>($"MapBtn_{key}");
            if (btn == null)
                continue;
            btn.Text = unlocked
                ? $" {icon} {name}\n   {stars}"
                : $" 🔒 {icon} {name}\n   {stars}";
            btn.SetMeta("map_locked", !unlocked);
            btn.Modulate = unlocked ? Colors.White : new Color(0.65f, 0.66f, 0.72f, 0.85f);
            if (!btn.HasMeta("_select_wired"))
            {
                btn.SetMeta("_select_wired", true);
                string localKey = key;
                btn.Pressed += () => SelectStage(localKey);
            }
        }
        AudioManager.Instance?.WireClicks(StageListContainer);
    }

    public void SelectStage(string key)
    {
        ActiveStageKey = key;
        var data = GameManager.GetStageInfo(key);

        string organ = data.TryGetValue("organ", out var ogVal) ? ogVal.AsString() : "";
        string icon = data.TryGetValue("organ_icon", out var icVal) ? icVal.AsString() : "🌐";
        string name = data.TryGetValue("name", out var nmVal) ? nmVal.AsString() : key;
        string subtitle = data.TryGetValue("subtitle", out var stVal) ? stVal.AsString() : "";
        int diff = data.TryGetValue("difficulty", out var dfVal) ? dfVal.AsInt32() : 1;
        string stars = new string('★', diff) + new string('☆', 5 - diff);
        Color col = data.TryGetValue("color_code", out var ccVal) ? ccVal.AsColor() : new Color(0.9f, 0.75f, 0.3f);

        if (OrganBadgeLbl != null)
        {
            OrganBadgeLbl.Text = $"[ {icon} {Tr("LABEL_HOST_REGION")}{organ} ]";
            OrganBadgeLbl.Modulate = col;
        }

        if (DifficultyLbl != null)
        {
            DifficultyLbl.Text = $"{Tr("LABEL_DIFFICULTY")}{stars}";
        }

        if (StageNameLbl != null)
        {
            StageNameLbl.Text = name;
            StageNameLbl.Modulate = col;
        }

        if (StageSubtitleLbl != null)
        {
            StageSubtitleLbl.Text = subtitle;
        }

        if (StageEnvLbl != null)
        {
            StageEnvLbl.Text = $"🔬 {Tr("LABEL_ECO_SLICE")}{data["environment"].AsString()}";
        }

        if (StageMechLbl != null)
        {
            StageMechLbl.Text = $"🌊 {Tr("LABEL_FLUID_MECH")}{data["mechanic"].AsString()}";
        }

        if (StageThreatLbl != null)
        {
            StageThreatLbl.Text = $"☣️ {Tr("LABEL_KEY_THREATS")}{data["threat"].AsString()}";
        }

        if (HoloScanner != null)
        {
            HoloScanner.SelectStage(key);
        }

        bool unlocked = GameManager.IsStageUnlocked(key);
        if (StageLockStatusLbl != null)
        {
            StageLockStatusLbl.Visible = !unlocked;
            StageLockStatusLbl.Text = unlocked
                ? ""
                : $"🔒 {Tr("MAP_LOCKED_HINT")}\n{AchievementManager.GetStageUnlockRequirementText(key)}";
        }

        if (DeployBtn != null)
        {
            DeployBtn.Disabled = !unlocked;
            DeployBtn.TooltipText = unlocked ? "" : Tr("MAP_LOCKED_DEPLOY");
        }

        UpdateEndlessAvailability();
        RefreshDifficultyToggle();

        if (StageListContainer != null)
        {
            foreach (var child in StageListContainer.GetChildren())
            {
                if (child is Button b)
                {
                    bool isCur = b.Name == $"MapBtn_{key}";
                    bool lockedBtn = b.HasMeta("map_locked") && b.GetMeta("map_locked").AsBool();
                    b.Modulate = isCur
                        ? (lockedBtn ? new Color(1.1f, 0.75f, 0.75f, 1.0f) : new Color(1.2f, 1.2f, 1.2f, 1.0f))
                        : (lockedBtn ? new Color(0.6f, 0.62f, 0.7f, 0.8f) : new Color(0.75f, 0.85f, 0.95f, 0.75f));
                }
            }
        }
    }

    private void OnDeployPressed()
    {
        if (!GameManager.IsStageUnlocked(ActiveStageKey))
        {
            // Locked organ: refresh the requirement readout instead of deploying
            SelectStage(ActiveStageKey);
            AudioManager.Instance?.PlayError();
            return;
        }

        GameManager.SelectedStage = ActiveStageKey;
        GameManager.SelectedDifficulty = DifficultyToggle != null && DifficultyToggle.Selected == 1
            ? RunRecordManager.DifficultyHard
            : RunRecordManager.DifficultyNormal;
        GameManager.StartGame(GetTree());
    }

    /// <summary>
    /// Hard (Acute Crisis) is per-organ: selectable only once the prerequisite
    /// stage has been cleared on Normal (docs/stages.md §2).
    /// </summary>
    private void RefreshDifficultyToggle()
    {
        if (DifficultyToggle == null)
            return;

        bool hardUnlocked = GameManager.IsStageHardUnlocked(ActiveStageKey);
        DifficultyToggle.SetItemDisabled(1, !hardUnlocked);

        if (!hardUnlocked && GameManager.SelectedDifficulty == RunRecordManager.DifficultyHard)
            GameManager.SelectedDifficulty = RunRecordManager.DifficultyNormal;

        bool hardSelected = hardUnlocked
            && GameManager.SelectedDifficulty == RunRecordManager.DifficultyHard;
        DifficultyToggle.Selected = hardSelected ? 1 : 0;
        DifficultyToggle.TooltipText = hardUnlocked ? "" : Tr("HARD_LOCKED_HINT");
    }

    private void OnDifficultySelected(long index)
    {
        if (index == 1 && !GameManager.IsStageHardUnlocked(ActiveStageKey))
        {
            if (DifficultyToggle != null)
                DifficultyToggle.Selected = 0;
            GameManager.SelectedDifficulty = RunRecordManager.DifficultyNormal;
            return;
        }

        GameManager.SelectedDifficulty = index == 1
            ? RunRecordManager.DifficultyHard
            : RunRecordManager.DifficultyNormal;
    }

    /// <summary>
    /// Endless availability readout: requires the Hard clear achievement
    /// (wound_hard_clear) and an unlocked organ. The tooltip always
    /// explains something: the unlock requirement when locked, the organ
    /// lock reason when the stage itself is locked, and a one-line mode
    /// summary once the button is actually usable.
    /// </summary>
    private void UpdateEndlessAvailability()
    {
        if (EndlessBtn == null)
            return;

        bool endlessReady = GameManager.IsEndlessAvailable();
        EndlessBtn.Disabled = !endlessReady || IsActiveStageLocked;
        if (!endlessReady)
            EndlessBtn.TooltipText = Tr("ENDLESS_LOCKED_HINT");
        else if (IsActiveStageLocked)
            EndlessBtn.TooltipText = Tr("MAP_LOCKED_DEPLOY");
        else
            EndlessBtn.TooltipText = Tr("ENDLESS_ABOUT_HINT");
    }
}

