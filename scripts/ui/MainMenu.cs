using Godot;
using Godot.Collections;
using System;
using Phagocyte.Core;

namespace Phagocyte.UI;

public partial class MainMenu : Control
{
    // Views
    public Control? TitleView { get; set; }
    public Control? ClassView { get; set; }
    public Control? PassiveView { get; set; }
    public Control? MapView { get; set; }
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
    public Label? ClassBadgeLbl { get; set; }
    public Label? ClassNameLbl { get; set; }
    public Label? ClassBioLbl { get; set; }
    public Label? ClassRoleLbl { get; set; }
    public Label? ClassStatsHeaderLbl { get; set; }
    public Label? ClassStatsLbl { get; set; }
    public Label? ClassSkillHeaderLbl { get; set; }
    public Label? ClassSkillLbl { get; set; }
    public Label? ClassStatusLbl { get; set; }
    public Button? ClassConfirmBtn { get; set; }

    // Passive Tree View Controls
    public Label? PassiveHeaderLbl { get; set; }
    public Label? TreeLevelLbl { get; set; }
    public Label? TreePointsLbl { get; set; }
    public PassiveTreeView? TreeCanvas { get; set; }
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

    // Map View Controls
    public Label? MapHeaderLbl { get; set; }
    public VBoxContainer? MapListContainer { get; set; }
    public Label? OrganBadgeLbl { get; set; }
    public Label? DifficultyLbl { get; set; }
    public Label? MapNameLbl { get; set; }
    public Label? MapSubtitleLbl { get; set; }
    public Label? MapEnvLbl { get; set; }
    public Label? MapMechLbl { get; set; }
    public Label? MapThreatLbl { get; set; }
    public Label? MapLockStatusLbl { get; set; }
    public HoloBodyScanner? HoloScanner { get; set; }
    public Button? DeployBtn { get; set; }
    public Button? EndlessBtn { get; set; }
    public OptionButton? DifficultyToggle { get; set; }

    public string ActiveClassKey { get; set; } = "macrophage";
    public string ActiveTreeClassKey { get; set; } = "macrophage";
    public string ActiveTreeNodeId { get; set; } = "";
    public string ActiveMapKey { get; set; } = "acute_wound";

    /// <summary>True when the currently inspected organ map is still locked.</summary>
    public bool IsActiveMapLocked => !GameManager.IsMapUnlocked(ActiveMapKey);

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
        PassiveView = GetNodeOrNull<Control>("PassiveView");
        MapView = GetNodeOrNull<Control>("MapView");
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

        ClassHeaderLbl = GetNodeOrNull<Label>("ClassView/HeaderLabel");
        ClassListContainer = GetNodeOrNull<VBoxContainer>("ClassView/HBox/ClassList");
        ClassBadgeLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassBadgeLabel");
        ClassNameLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassNameLabel");
        ClassBioLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassBioLabel");
        ClassRoleLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassRoleLabel");
        ClassStatsHeaderLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassStatsHeader");
        ClassStatsLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassStatsLabel");
        ClassSkillHeaderLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassSkillHeader");
        ClassSkillLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassSkillLabel");
        ClassStatusLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassStatusLabel");
        ClassConfirmBtn = GetNodeOrNull<Button>("ClassView/Buttons/ConfirmButton");

        PassiveHeaderLbl = GetNodeOrNull<Label>("PassiveView/HeaderLabel");
        TreeLevelLbl = GetNodeOrNull<Label>("PassiveView/InfoHBox/TreeLevelLabel");
        TreePointsLbl = GetNodeOrNull<Label>("PassiveView/InfoHBox/TreePointsLabel");
        TreeCanvas = GetNodeOrNull<PassiveTreeView>("PassiveView/ContentHBox/TreeView");
        PassiveResetBtn = GetNodeOrNull<Button>("PassiveView/Buttons/ResetButton");
        PassiveConfirmBtn = GetNodeOrNull<Button>("PassiveView/Buttons/ConfirmButton");
        TreeZoomOutBtn = GetNodeOrNull<Button>("PassiveView/Buttons/ZoomOutButton");
        TreeZoomInBtn = GetNodeOrNull<Button>("PassiveView/Buttons/ZoomInButton");

        MapHeaderLbl = GetNodeOrNull<Label>("MapView/HeaderLabel");
        MapListContainer = GetNodeOrNull<VBoxContainer>("MapView/HBox/MapList");
        OrganBadgeLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/TopHBox/OrganBadge");
        DifficultyLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/TopHBox/DifficultyLabel");
        MapNameLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/TitleVBox/MapNameLabel")
            ?? GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/MapNameLabel");
        MapSubtitleLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/TitleVBox/MapSubtitleLabel");
        MapEnvLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/MapEnvLabel");
        MapMechLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/MapMechLabel");
        MapThreatLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/MapThreatLabel");
        HoloScanner = GetNodeOrNull<HoloBodyScanner>("MapView/HBox/HoloBodyScanner");
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
                if (!GameManager.IsMapUnlocked(ActiveMapKey))
                {
                    SelectMap(ActiveMapKey);
                    return;
                }
                GameManager.SelectedMap = ActiveMapKey;
                EndlessSetupModal?.OpenSetup();
            };
            deployRow.AddChild(EndlessBtn);
            deployRow.MoveChild(EndlessBtn, DeployBtn.GetIndex() + 1);
        }

        // Dual-track difficulty toggle (docs/map.md §2): Normal vs Hard (急性危象),
        // unlocked per organ by clearing the prerequisite map on Normal.
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
        EndlessSetupModal = new EndgameSetupModal { Name = "EndgameSetupModal" };
        AddChild(EndlessSetupModal);

        // Lock status readout injected under the threat rows (MapData-driven)
        if (MapThreatLbl != null && MapThreatLbl.GetParent() is Control detailPanel)
        {
            MapLockStatusLbl = new Label
            {
                Name = "MapLockStatusLabel",
                Visible = false,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(0, 36)
            };
            MapLockStatusLbl.AddThemeColorOverride("font_color", new Color(1.0f, 0.45f, 0.4f));
            MapLockStatusLbl.AddThemeFontSizeOverride("font_size", 13);
            detailPanel.AddChild(MapLockStatusLbl);
        }

        if (HoloScanner != null)
        {
            HoloScanner.OrganSelected += (string mapKey) => SelectMap(mapKey);
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

        if (TreeCanvas != null)
        {
            TreeCanvas.TreeNodeActivated += OnTreeNodeActivated;
            TreeCanvas.TreeNodeHovered += OnTreeNodeHovered;
            TreeCanvas.TreeNodeRefundRequested += OnTreeNodeRefundRequested;
        }
        // Build-profile tabs (docs/passivetree.md §5.4): dynamic slots, up to MaxProfiles.
        // Shifted right (+130px) so the tabs clear the shared top-left
        // GlobalBackButton (24,24)-(164,64), and shifted down (+40px) so the
        // tabs sit below HeaderLabel (8-38) + InfoHBox (42-64) instead of
        // overlapping the title row.
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
                OffsetTop = -292.0f,
                OffsetRight = 100.0f,
                OffsetBottom = -250.0f
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

        UpdateAllTexts();
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

        if (PassiveHeaderLbl != null) PassiveHeaderLbl.Text = Tr("HEADER_SELECT_PASSIVE");
        if (PassiveResetBtn != null) PassiveResetBtn.Text = Tr("TREE_RESET");
        if (PassiveConfirmBtn != null) PassiveConfirmBtn.Text = Tr("BTN_CONFIRM_MAP");
        if (TreeZoomOutBtn != null) TreeZoomOutBtn.Text = Tr("TREE_ZOOM_OUT");
        RefreshProfileTexts();
        if (TreeZoomInBtn != null) TreeZoomInBtn.Text = Tr("TREE_ZOOM_IN");

        if (MapHeaderLbl != null) MapHeaderLbl.Text = Tr("HEADER_SELECT_MAP");
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
        SetupMapButtons();
        SelectClass(ActiveClassKey);
        RefreshPassiveView();
        SelectMap(ActiveMapKey);
    }

    private void SwitchToView(Control targetView)
    {
        Control?[] views = { TitleView, ClassView, PassiveView, MapView, AchievementView };
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
    /// The single back affordance for all five views. Targets mirror the
    /// removed per-view buttons: Map re-enters the passive build (refreshing
    /// the tree), Passive returns to class selection, everything else to title.
    /// </summary>
    public void OnGlobalBackPressed()
    {
        if (_currentView == MapView && PassiveView != null)
        {
            SelectPassiveBuild(ActiveTreeClassKey);
            SwitchToView(PassiveView);
        }
        else if (_currentView == PassiveView && ClassView != null)
        {
            SwitchToView(ClassView);
        }
        else if (TitleView != null)
        {
            SwitchToView(TitleView);
        }
    }

    public void SetupClassButtons()
    {
        if (ClassListContainer == null)
            return;

        foreach (var child in ClassListContainer.GetChildren())
        {
            ClassListContainer.RemoveChild(child);
            child.QueueFree();
        }

        foreach (var keyVar in GameManager.ClassData.Keys)
        {
            string key = keyVar.AsString();
            var data = GameManager.GetClassInfo(key);
            var btn = new Button
            {
                CustomMinimumSize = new Vector2(280, 48),
                Alignment = HorizontalAlignment.Left,
                Text = (data["unlocked"].AsBool() ? " ✅ " : " 🔒 ") + data["name"].AsString()
            };
            string localKey = key;
            btn.Pressed += () => SelectClass(localKey);
            ClassListContainer.AddChild(btn);
        }
    }

    public void SelectClass(string key)
    {
        ActiveClassKey = key;
        var data = GameManager.GetClassInfo(key);
        bool unlocked = data.TryGetValue("unlocked", out Variant unlockedVal) && unlockedVal.AsBool();

        if (ClassBadgeLbl != null)
        {
            ClassBadgeLbl.Text = "[ " + Tr(unlocked ? "CLASS_BADGE_READY" : "CLASS_BADGE_LOCKED") + " ]";
            ClassBadgeLbl.Modulate = unlocked ? new Color(0.3f, 1.0f, 0.4f) : new Color(1.0f, 0.68f, 0.25f);
        }
        if (ClassNameLbl != null && data.TryGetValue("name", out Variant nameVal))
            ClassNameLbl.Text = nameVal.AsString();
        if (ClassBioLbl != null)
        {
            string bioKey = data.TryGetValue("bio_key", out Variant bioKeyVal) ? bioKeyVal.AsString() : "";
            ClassBioLbl.Text = Tr("CODEX_HEADER_BIO") + "\n" + (string.IsNullOrEmpty(bioKey) ? "" : Tr(bioKey));
        }
        if (ClassRoleLbl != null && data.TryGetValue("role", out Variant roleVal))
            ClassRoleLbl.Text = Tr("LABEL_ROLE") + roleVal.AsString();
        if (ClassStatsHeaderLbl != null)
            ClassStatsHeaderLbl.Text = Tr("CLASS_STATS_HEADER");
        if (ClassStatsLbl != null)
            ClassStatsLbl.Text = BuildClassVitalsText(data);
        if (ClassSkillHeaderLbl != null)
            ClassSkillHeaderLbl.Text = Tr("CLASS_SKILL_HEADER");
        if (ClassSkillLbl != null)
        {
            var (skillText, skillImagePath) = BuildClassSkillText(key);
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
                ClassStatusLbl.Text = Tr("STATUS_UNLOCKED");
                ClassStatusLbl.Modulate = new Color(0.3f, 1.0f, 0.4f);
            }
            else
            {
                ClassStatusLbl.Text = AchievementManager.GetCellUnlockRequirementText(key);
                ClassStatusLbl.Modulate = new Color(1.0f, 0.68f, 0.25f);
            }
        }

        if (ClassConfirmBtn != null)
        {
            ClassConfirmBtn.Disabled = !unlocked;
            ClassConfirmBtn.Text = unlocked ? Tr("BTN_CONFIRM_PASSIVE") : Tr("BTN_CONFIRM_CLASS_LOCKED");
        }
    }

    /// <summary>
    /// Baseline vitals block: raw HP / speed / armor numbers plus the class
    /// signature stat with a plain-language label (no academic stat names).
    /// </summary>
    private string BuildClassVitalsText(Dictionary data)
    {
        float hp = data.TryGetValue("base_hp", out Variant hpVal) ? hpVal.AsSingle() : 100.0f;
        float speed = data.TryGetValue("base_speed", out Variant spVal) ? spVal.AsSingle() : 230.0f;
        float armor = data.TryGetValue("base_armor", out Variant arVal) ? arVal.AsSingle() : 0.0f;

        var lines = new System.Collections.Generic.List<string>
        {
            $"{PassiveTreeManager.GetStatLabel("max_health")} {hp:F0}",
            $"{PassiveTreeManager.GetStatLabel("move_speed")} {speed:F0}",
            $"{PassiveTreeManager.GetStatLabel("armor")} {armor:F0}"
        };

        string sigStat = data.TryGetValue("trait_stat", out Variant sigVal) ? sigVal.AsString() : "";
        if (!string.IsNullOrEmpty(sigStat))
        {
            float sigNum = data.TryGetValue("trait_stat_value", out Variant signVal) ? signVal.AsSingle() : 0.0f;
            lines.Add($"{SignatureStatLabel(sigStat)} {FormatSignatureStat(sigStat, sigNum)}");
        }
        return string.Join("\n", lines);
    }

    private string SignatureStatLabel(string stat)
    {
        string key = stat switch
        {
            "block" => "CLASS_SIG_BLOCK",
            "crit_chance" => "CLASS_SIG_CRIT",
            "might" => "CLASS_SIG_MIGHT",
            "projectile_speed" => "CLASS_SIG_PROJSPEED",
            "magnet" => "CLASS_SIG_MAGNET",
            _ => ""
        };
        return string.IsNullOrEmpty(key) ? PassiveTreeManager.GetStatLabel(stat) : Tr(key);
    }

    private static string FormatSignatureStat(string stat, float value) => stat switch
    {
        "crit_chance" or "evasion" or "block" or "life_steal" or "cooldown_reduction" => $"{value * 100.0f:F0}%",
        "might" or "area" or "projectile_speed" or "duration" or "amount" or "knockback" or "crit_damage" => $"×{value:F1}".TrimEnd('0').TrimEnd('.'),
        _ => value % 1.0f == 0.0f ? $"{value:F0}" : $"{value:F1}"
    };

    /// <summary>Innate skill line: name + full description from the skill catalog (art goes to ClassSkillTexture).</summary>
    private (string Text, string ImagePath) BuildClassSkillText(string classKey)
    {
        foreach (string id in GameManager.SkillCatalog.Keys)
        {
            var s = (Dictionary)GameManager.SkillCatalog[id];
            if (s.TryGetValue("type", out Variant typeVal) && typeVal.AsString() == "innate"
                && s.TryGetValue("class_id", out Variant cidVal) && cidVal.AsString() == classKey)
            {
                string nameKey = s.TryGetValue("name_key", out Variant nVal) ? nVal.AsString() : "";
                string descKey = s.TryGetValue("desc_key", out Variant dVal) ? dVal.AsString() : "";
                string imagePath = s.TryGetValue("image_path", out Variant ipVal) ? ipVal.AsString() : AssetPaths.SkillIcon(id);
                return ($"{Tr(nameKey)}\n{Tr(descKey)}", imagePath);
            }
        }
        return ("-", "");
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
        if (TreeLevelLbl != null) TreeLevelLbl.Text = TextFormatter.Format(Tr("TREE_LEVEL"), level);
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
        if (MapView != null)
            SwitchToView(MapView);
        SelectMap(ActiveMapKey);
    }

    public void SetupMapButtons()
    {
        if (MapListContainer == null)
            return;

        foreach (var child in MapListContainer.GetChildren())
        {
            MapListContainer.RemoveChild(child);
            child.QueueFree();
        }

        foreach (var keyVar in GameManager.MapData.Keys)
        {
            string key = keyVar.AsString();
            var data = GameManager.GetMapInfo(key);
            string icon = data.TryGetValue("organ_icon", out var icVal) ? icVal.AsString() : "🌐";
            string name = data.TryGetValue("name", out var nmVal) ? nmVal.AsString() : key;
            int diff = data.TryGetValue("difficulty", out var diffVal) ? diffVal.AsInt32() : 1;
            string stars = new string('★', diff) + new string('☆', 5 - diff);
            bool unlocked = GameManager.IsMapUnlocked(key);

            var btn = new Button
            {
                CustomMinimumSize = new Vector2(250, 52),
                Alignment = HorizontalAlignment.Left,
                Text = unlocked
                    ? $" {icon} {name}\n   {stars}"
                    : $" 🔒 {icon} {name}\n   {stars}"
            };
            btn.Name = $"MapBtn_{key}";
            btn.SetMeta("map_locked", !unlocked);
            btn.Modulate = unlocked ? Colors.White : new Color(0.65f, 0.66f, 0.72f, 0.85f);
            string localKey = key;
            btn.Pressed += () => SelectMap(localKey);
            MapListContainer.AddChild(btn);
        }
    }

    public void SelectMap(string key)
    {
        ActiveMapKey = key;
        var data = GameManager.GetMapInfo(key);

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

        if (MapNameLbl != null)
        {
            MapNameLbl.Text = name;
            MapNameLbl.Modulate = col;
        }

        if (MapSubtitleLbl != null)
        {
            MapSubtitleLbl.Text = subtitle;
        }

        if (MapEnvLbl != null)
        {
            MapEnvLbl.Text = $"🔬 {Tr("LABEL_ECO_SLICE")}{data["environment"].AsString()}";
        }

        if (MapMechLbl != null)
        {
            MapMechLbl.Text = $"🌊 {Tr("LABEL_FLUID_MECH")}{data["mechanic"].AsString()}";
        }

        if (MapThreatLbl != null)
        {
            MapThreatLbl.Text = $"☣️ {Tr("LABEL_KEY_THREATS")}{data["threat"].AsString()}";
        }

        if (HoloScanner != null)
        {
            HoloScanner.SelectOrgan(key);
        }

        bool unlocked = GameManager.IsMapUnlocked(key);
        if (MapLockStatusLbl != null)
        {
            MapLockStatusLbl.Visible = !unlocked;
            MapLockStatusLbl.Text = unlocked
                ? ""
                : $"🔒 {Tr("MAP_LOCKED_HINT")}\n{AchievementManager.GetMapUnlockRequirementText(key)}";
        }

        if (DeployBtn != null)
        {
            DeployBtn.Disabled = !unlocked;
            DeployBtn.TooltipText = unlocked ? "" : Tr("MAP_LOCKED_DEPLOY");
        }

        UpdateEndlessAvailability();
        RefreshDifficultyToggle();

        if (MapListContainer != null)
        {
            foreach (var child in MapListContainer.GetChildren())
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
        if (!GameManager.IsMapUnlocked(ActiveMapKey))
        {
            // Locked organ: refresh the requirement readout instead of deploying
            SelectMap(ActiveMapKey);
            return;
        }

        GameManager.SelectedMap = ActiveMapKey;
        GameManager.SelectedDifficulty = DifficultyToggle != null && DifficultyToggle.Selected == 1
            ? RunRecordManager.DifficultyHard
            : RunRecordManager.DifficultyNormal;
        GameManager.StartGame(GetTree());
    }

    /// <summary>
    /// Hard (Acute Crisis) is per-organ: selectable only once the prerequisite
    /// map has been cleared on Normal (docs/map.md §2).
    /// </summary>
    private void RefreshDifficultyToggle()
    {
        if (DifficultyToggle == null)
            return;

        bool hardUnlocked = GameManager.IsMapHardUnlocked(ActiveMapKey);
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
        if (index == 1 && !GameManager.IsMapHardUnlocked(ActiveMapKey))
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
    /// lock reason when the map itself is locked, and a one-line mode
    /// summary once the button is actually usable.
    /// </summary>
    private void UpdateEndlessAvailability()
    {
        if (EndlessBtn == null)
            return;

        bool endlessReady = GameManager.IsEndlessAvailable();
        EndlessBtn.Disabled = !endlessReady || IsActiveMapLocked;
        if (!endlessReady)
            EndlessBtn.TooltipText = Tr("ENDLESS_LOCKED_HINT");
        else if (IsActiveMapLocked)
            EndlessBtn.TooltipText = Tr("MAP_LOCKED_DEPLOY");
        else
            EndlessBtn.TooltipText = Tr("ENDLESS_ABOUT_HINT");
    }
}
