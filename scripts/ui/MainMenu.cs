using Godot;
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
    public CodexModal? CellCodexModal { get; set; }
    public SettingsModal? CellSettingsModal { get; set; }
    public RunRecordsModal? RecordsModal { get; set; }
    public EndgameSetupModal? EndlessSetupModal { get; set; }

    // Language Switcher
    public Button? LangBtn { get; set; }

    // Title View Controls
    public Label? TitleLbl { get; set; }
    public Label? SubtitleLbl { get; set; }
    public Button? StartBtn { get; set; }
    public Button? CodexBtn { get; set; }
    public Button? RecordsBtn { get; set; }
    public Button? SettingsBtn { get; set; }
    public Button? QuitBtn { get; set; }

    // Class View Controls
    public Label? ClassHeaderLbl { get; set; }
    public VBoxContainer? ClassListContainer { get; set; }
    public Label? ClassNameLbl { get; set; }
    public Label? ClassRoleLbl { get; set; }
    public Label? ClassTraitLbl { get; set; }
    public Label? ClassStatusLbl { get; set; }
    public Button? ClassConfirmBtn { get; set; }
    public Button? ClassBackBtn { get; set; }

    // Passive Tree View Controls
    public Label? PassiveHeaderLbl { get; set; }
    public Label? TreeClassLbl { get; set; }
    public Label? TreeLevelLbl { get; set; }
    public Label? TreePointsLbl { get; set; }
    public PassiveTreeView? TreeCanvas { get; set; }
    public Label? TreeStatusLbl { get; set; }
    public Button? PassiveBackBtn { get; set; }
    public Button? PassiveResetBtn { get; set; }
    public Button? PassiveConfirmBtn { get; set; }
    public Button? TreeZoomOutBtn { get; set; }
    public Button? TreeZoomInBtn { get; set; }
    public Button? TreeFitBtn { get; set; }

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
    public Button? MapBackBtn { get; set; }

    public string ActiveClassKey { get; set; } = "macrophage";
    public string ActiveTreeClassKey { get; set; } = "macrophage";
    public string ActiveTreeNodeId { get; set; } = "";
    public string ActiveMapKey { get; set; } = "acute_wound";

    /// <summary>True when the currently inspected organ map is still locked.</summary>
    public bool IsActiveMapLocked => !GameManager.IsMapUnlocked(ActiveMapKey);

    private Callable _langCallback;

    public override void _Ready()
    {
        TitleView = GetNodeOrNull<Control>("TitleView");
        ClassView = GetNodeOrNull<Control>("ClassView");
        PassiveView = GetNodeOrNull<Control>("PassiveView");
        MapView = GetNodeOrNull<Control>("MapView");
        CellCodexModal = GetNodeOrNull<CodexModal>("CodexModal");
        CellSettingsModal = GetNodeOrNull<SettingsModal>("SettingsModal");
        RecordsModal = GetNodeOrNull<RunRecordsModal>("RunRecordsModal");

        LangBtn = GetNodeOrNull<Button>("LangButton");

        TitleLbl = GetNodeOrNull<Label>("TitleView/TitleLabel");
        SubtitleLbl = GetNodeOrNull<Label>("TitleView/SubtitleLabel");
        StartBtn = GetNodeOrNull<Button>("TitleView/VBox/StartButton");
        CodexBtn = GetNodeOrNull<Button>("TitleView/VBox/CodexButton");
        RecordsBtn = GetNodeOrNull<Button>("TitleView/VBox/RecordsButton");
        SettingsBtn = GetNodeOrNull<Button>("TitleView/VBox/SettingsButton");
        QuitBtn = GetNodeOrNull<Button>("TitleView/VBox/QuitButton");

        ClassHeaderLbl = GetNodeOrNull<Label>("ClassView/HeaderLabel");
        ClassListContainer = GetNodeOrNull<VBoxContainer>("ClassView/HBox/ClassList");
        ClassNameLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassNameLabel");
        ClassRoleLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassRoleLabel");
        ClassTraitLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassTraitLabel");
        ClassStatusLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassStatusLabel");
        ClassConfirmBtn = GetNodeOrNull<Button>("ClassView/Buttons/ConfirmButton");
        ClassBackBtn = GetNodeOrNull<Button>("ClassView/Buttons/BackButton");

        PassiveHeaderLbl = GetNodeOrNull<Label>("PassiveView/HeaderLabel");
        TreeClassLbl = GetNodeOrNull<Label>("PassiveView/InfoHBox/TreeClassLabel");
        TreeLevelLbl = GetNodeOrNull<Label>("PassiveView/InfoHBox/TreeLevelLabel");
        TreePointsLbl = GetNodeOrNull<Label>("PassiveView/InfoHBox/TreePointsLabel");
        TreeCanvas = GetNodeOrNull<PassiveTreeView>("PassiveView/ContentHBox/TreeView");
        TreeStatusLbl = GetNodeOrNull<Label>("PassiveView/TreeStatusLabel");
        PassiveBackBtn = GetNodeOrNull<Button>("PassiveView/Buttons/BackButton");
        PassiveResetBtn = GetNodeOrNull<Button>("PassiveView/Buttons/ResetButton");
        PassiveConfirmBtn = GetNodeOrNull<Button>("PassiveView/Buttons/ConfirmButton");
        TreeZoomOutBtn = GetNodeOrNull<Button>("PassiveView/Buttons/ZoomOutButton");
        TreeZoomInBtn = GetNodeOrNull<Button>("PassiveView/Buttons/ZoomInButton");
        TreeFitBtn = GetNodeOrNull<Button>("PassiveView/Buttons/FitButton");

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
        MapBackBtn = GetNodeOrNull<Button>("MapView/Buttons/BackButton");

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

        if (LangBtn != null)
            LangBtn.Pressed += OnLangTogglePressed;

        _langCallback = Callable.From((string _) => UpdateAllTexts());
        GameManager.AddLanguageListener(_langCallback);

        if (StartBtn != null)
            StartBtn.Pressed += OnStartPressed;
        if (CodexBtn != null)
            CodexBtn.Pressed += () => CellCodexModal?.OpenCodex(0);
        if (RecordsBtn != null)
            RecordsBtn.Pressed += () => RecordsModal?.OpenHistory();
        if (SettingsBtn != null)
            SettingsBtn.Pressed += () => CellSettingsModal?.OpenSettings(0);
        if (QuitBtn != null)
            QuitBtn.Pressed += () => GetTree().Quit();

        if (ClassBackBtn != null)
            ClassBackBtn.Pressed += () => { if (TitleView != null) SwitchToView(TitleView); };
        if (ClassConfirmBtn != null)
            ClassConfirmBtn.Pressed += OnClassConfirmPressed;

        if (TreeCanvas != null)
        {
            TreeCanvas.TreeNodeActivated += OnTreeNodeActivated;
            TreeCanvas.TreeNodeHovered += OnTreeNodeHovered;
            TreeCanvas.TreeNodeRefundRequested += OnTreeNodeRefundRequested;
        }
        if (PassiveBackBtn != null)
            PassiveBackBtn.Pressed += () => { if (ClassView != null) SwitchToView(ClassView); };
        if (PassiveResetBtn != null)
            PassiveResetBtn.Pressed += OnTreeResetPressed;
        if (PassiveConfirmBtn != null)
            PassiveConfirmBtn.Pressed += OnPassiveConfirmPressed;
        if (TreeZoomOutBtn != null)
            TreeZoomOutBtn.Pressed += () => TreeCanvas?.ZoomStep(1.0f / 1.2f);
        if (TreeZoomInBtn != null)
            TreeZoomInBtn.Pressed += () => TreeCanvas?.ZoomStep(1.2f);
        if (TreeFitBtn != null)
            TreeFitBtn.Pressed += () => TreeCanvas?.FitTree();

        if (MapBackBtn != null)
            MapBackBtn.Pressed += () => { if (PassiveView != null) { SelectPassiveBuild(ActiveTreeClassKey); SwitchToView(PassiveView); } };
        if (DeployBtn != null)
            DeployBtn.Pressed += OnDeployPressed;

        UpdateAllTexts();
    }

    public override void _ExitTree()
    {
        GameManager.RemoveLanguageListener(_langCallback);
    }

    private void OnLangTogglePressed()
    {
        GameManager.ToggleLanguage();
    }

    public void UpdateAllTexts()
    {
        if (LangBtn != null)
            LangBtn.Text = GameManager.CurrentLanguage == "zh_CN" ? "🌐 English" : "🌐 简体中文";

        if (TitleLbl != null) TitleLbl.Text = Tr("TITLE_MAIN");
        if (SubtitleLbl != null) SubtitleLbl.Text = Tr("SUBTITLE_MAIN");
        if (StartBtn != null) StartBtn.Text = Tr("BTN_START");
        if (CodexBtn != null) CodexBtn.Text = Tr("BTN_CODEX");
        if (RecordsBtn != null) RecordsBtn.Text = Tr("BTN_RECORDS");
        if (SettingsBtn != null) SettingsBtn.Text = Tr("BTN_SETTINGS");
        if (QuitBtn != null) QuitBtn.Text = Tr("BTN_QUIT");

        if (ClassHeaderLbl != null) ClassHeaderLbl.Text = Tr("HEADER_SELECT_CLASS");
        if (ClassBackBtn != null) ClassBackBtn.Text = Tr("BTN_BACK_TITLE");

        if (PassiveHeaderLbl != null) PassiveHeaderLbl.Text = Tr("HEADER_SELECT_PASSIVE");
        if (PassiveBackBtn != null) PassiveBackBtn.Text = Tr("BTN_BACK_CLASS");
        if (PassiveResetBtn != null) PassiveResetBtn.Text = Tr("TREE_RESET");
        if (PassiveConfirmBtn != null) PassiveConfirmBtn.Text = Tr("BTN_CONFIRM_MAP");
        if (TreeZoomOutBtn != null) TreeZoomOutBtn.Text = Tr("TREE_ZOOM_OUT");
        if (TreeZoomInBtn != null) TreeZoomInBtn.Text = Tr("TREE_ZOOM_IN");
        if (TreeFitBtn != null) TreeFitBtn.Text = Tr("TREE_FIT_VIEW");

        if (MapHeaderLbl != null) MapHeaderLbl.Text = Tr("HEADER_SELECT_MAP");
        if (MapBackBtn != null) MapBackBtn.Text = Tr("BTN_BACK_PASSIVE");
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
        if (TitleView != null) TitleView.Visible = targetView == TitleView;
        if (ClassView != null) ClassView.Visible = targetView == ClassView;
        if (PassiveView != null) PassiveView.Visible = targetView == PassiveView;
        if (MapView != null) MapView.Visible = targetView == MapView;
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
        if (ClassNameLbl != null) ClassNameLbl.Text = data["name"].AsString();
        if (ClassRoleLbl != null) ClassRoleLbl.Text = Tr("LABEL_ROLE") + data["role"].AsString();
        if (ClassTraitLbl != null) ClassTraitLbl.Text = Tr("LABEL_TRAIT") + data["trait"].AsString();

        bool unlocked = data["unlocked"].AsBool();
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
        var classInfo = GameManager.GetClassInfo(ActiveTreeClassKey);
        string className = classInfo.TryGetValue("name", out var nameVal) ? nameVal.AsString() : ActiveTreeClassKey;
        int level = PassiveTreeManager.GetCellLevel(ActiveTreeClassKey);
        int available = PassiveTreeManager.GetPointsAvailable(ActiveTreeClassKey);
        int spent = PassiveTreeManager.GetSpentPoints(ActiveTreeClassKey);

        TreeCanvas?.Render(ActiveTreeClassKey);
        if (TreeClassLbl != null) TreeClassLbl.Text = className;
        if (TreeLevelLbl != null) TreeLevelLbl.Text = TextFormatter.Format(Tr("TREE_LEVEL"), level);
        if (TreePointsLbl != null) TreePointsLbl.Text = TextFormatter.Format(Tr("TREE_POINTS"), available, spent);
        UpdateTreeStatus();
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
        UpdateTreeStatus();
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

    private void UpdateTreeStatus()
    {
        if (TreeStatusLbl == null)
            return;

        if (!PassiveTreeManager.TryGetNode(ActiveTreeNodeId, out var node))
        {
            TreeStatusLbl.Text = Tr("TREE_SELECT_NODE");
            return;
        }

        int stacks = PassiveTreeManager.GetNodeStacks(ActiveTreeClassKey, node.Id);
        string status = PassiveTreeManager.GetNodeName(node.Id);
        if (PassiveTreeManager.IsInnateStartNode(ActiveTreeClassKey, node.Id))
            status += " • " + Tr("TREE_START_INNATE");
        else if (stacks >= node.MaxStacks)
            status += " • " + Tr("TREE_MAXED");
        else if (PassiveTreeManager.CanPurchase(ActiveTreeClassKey, node.Id))
            status += " • " + Tr("TREE_PURCHASE_HINT");
        else if (PassiveTreeManager.GetPointsAvailable(ActiveTreeClassKey) >= node.PointCost)
            status += " • " + Tr("TREE_LOCKED");
        TreeStatusLbl.Text = status;
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
    /// (ach_wound_hard_clear) and an unlocked organ.
    /// </summary>
    private void UpdateEndlessAvailability()
    {
        if (EndlessBtn == null)
            return;

        bool endlessReady = GameManager.IsEndlessAvailable();
        EndlessBtn.Disabled = !endlessReady || IsActiveMapLocked;
        EndlessBtn.TooltipText = endlessReady ? "" : Tr("ENDLESS_LOCKED_HINT");
    }
}
