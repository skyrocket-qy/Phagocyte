using Godot;
using System;
using Phagocyte.Core;

namespace Phagocyte.UI;

public partial class MainMenu : Control
{
    // Views
    public Control? TitleView { get; set; }
    public Control? ClassView { get; set; }
    public Control? MapView { get; set; }
    public CodexModal? CellCodexModal { get; set; }
    public SettingsModal? CellSettingsModal { get; set; }

    // Language Switcher
    public Button? LangBtn { get; set; }

    // Title View Controls
    public Label? TitleLbl { get; set; }
    public Label? SubtitleLbl { get; set; }
    public Button? StartBtn { get; set; }
    public Button? CodexBtn { get; set; }
    public Button? SettingsBtn { get; set; }
    public Button? QuitBtn { get; set; }

    // Class View Controls
    public Label? ClassHeaderLbl { get; set; }
    public VBoxContainer? ClassListContainer { get; set; }
    public Label? ClassNameLbl { get; set; }
    public Label? ClassRoleLbl { get; set; }
    public Label? ClassTraitLbl { get; set; }
    public Label? ClassPassiveLbl { get; set; }
    public Label? ClassBurstLbl { get; set; }
    public Label? ClassStatusLbl { get; set; }
    public Button? ClassConfirmBtn { get; set; }
    public Button? ClassBackBtn { get; set; }

    // Map View Controls
    public Label? MapHeaderLbl { get; set; }
    public VBoxContainer? MapListContainer { get; set; }
    public Label? MapNameLbl { get; set; }
    public Label? MapEnvLbl { get; set; }
    public Label? MapMechLbl { get; set; }
    public Label? MapThreatLbl { get; set; }
    public Button? DeployBtn { get; set; }
    public Button? MapBackBtn { get; set; }

    public string ActiveClassKey { get; set; } = "macrophage";
    public string ActiveMapKey { get; set; } = "acute_wound";

    private Callable _langCallback;

    public override void _Ready()
    {
        TitleView = GetNodeOrNull<Control>("TitleView");
        ClassView = GetNodeOrNull<Control>("ClassView");
        MapView = GetNodeOrNull<Control>("MapView");
        CellCodexModal = GetNodeOrNull<CodexModal>("CodexModal");
        CellSettingsModal = GetNodeOrNull<SettingsModal>("SettingsModal");

        LangBtn = GetNodeOrNull<Button>("LangButton");

        TitleLbl = GetNodeOrNull<Label>("TitleView/TitleLabel");
        SubtitleLbl = GetNodeOrNull<Label>("TitleView/SubtitleLabel");
        StartBtn = GetNodeOrNull<Button>("TitleView/VBox/StartButton");
        CodexBtn = GetNodeOrNull<Button>("TitleView/VBox/CodexButton");
        SettingsBtn = GetNodeOrNull<Button>("TitleView/VBox/SettingsButton");
        QuitBtn = GetNodeOrNull<Button>("TitleView/VBox/QuitButton");

        ClassHeaderLbl = GetNodeOrNull<Label>("ClassView/HeaderLabel");
        ClassListContainer = GetNodeOrNull<VBoxContainer>("ClassView/HBox/ClassList");
        ClassNameLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassNameLabel");
        ClassRoleLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassRoleLabel");
        ClassTraitLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassTraitLabel");
        ClassPassiveLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassPassiveLabel");
        ClassBurstLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassBurstLabel");
        ClassStatusLbl = GetNodeOrNull<Label>("ClassView/HBox/DetailPanel/VBox/ClassStatusLabel");
        ClassConfirmBtn = GetNodeOrNull<Button>("ClassView/Buttons/ConfirmButton");
        ClassBackBtn = GetNodeOrNull<Button>("ClassView/Buttons/BackButton");

        MapHeaderLbl = GetNodeOrNull<Label>("MapView/HeaderLabel");
        MapListContainer = GetNodeOrNull<VBoxContainer>("MapView/HBox/MapList");
        MapNameLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/MapNameLabel");
        MapEnvLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/MapEnvLabel");
        MapMechLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/MapMechLabel");
        MapThreatLbl = GetNodeOrNull<Label>("MapView/HBox/DetailPanel/VBox/MapThreatLabel");
        DeployBtn = GetNodeOrNull<Button>("MapView/Buttons/DeployButton");
        MapBackBtn = GetNodeOrNull<Button>("MapView/Buttons/BackButton");

        if (TitleView != null)
            SwitchToView(TitleView);
        if (CellCodexModal != null)
            CellCodexModal.Visible = false;
        if (CellSettingsModal != null)
            CellSettingsModal.Visible = false;

        if (LangBtn != null)
            LangBtn.Pressed += OnLangTogglePressed;

        _langCallback = Callable.From((string _) => UpdateAllTexts());
        GameManager.AddLanguageListener(_langCallback);

        if (StartBtn != null)
            StartBtn.Pressed += OnStartPressed;
        if (CodexBtn != null)
            CodexBtn.Pressed += () => CellCodexModal?.OpenCodex(0);
        if (SettingsBtn != null)
            SettingsBtn.Pressed += () => CellSettingsModal?.OpenSettings(0);
        if (QuitBtn != null)
            QuitBtn.Pressed += () => GetTree().Quit();

        if (ClassBackBtn != null)
            ClassBackBtn.Pressed += () => { if (TitleView != null) SwitchToView(TitleView); };
        if (ClassConfirmBtn != null)
            ClassConfirmBtn.Pressed += OnClassConfirmPressed;

        if (MapBackBtn != null)
            MapBackBtn.Pressed += () => { if (ClassView != null) SwitchToView(ClassView); };
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
        if (SettingsBtn != null) SettingsBtn.Text = Tr("BTN_SETTINGS");
        if (QuitBtn != null) QuitBtn.Text = Tr("BTN_QUIT");

        if (ClassHeaderLbl != null) ClassHeaderLbl.Text = Tr("HEADER_SELECT_CLASS");
        if (ClassBackBtn != null) ClassBackBtn.Text = Tr("BTN_BACK_TITLE");

        if (MapHeaderLbl != null) MapHeaderLbl.Text = Tr("HEADER_SELECT_MAP");
        if (MapBackBtn != null) MapBackBtn.Text = Tr("BTN_BACK_CLASS");
        if (DeployBtn != null) DeployBtn.Text = Tr("BTN_DEPLOY");

        SetupClassButtons();
        SetupMapButtons();
        SelectClass(ActiveClassKey);
        SelectMap(ActiveMapKey);
    }

    private void SwitchToView(Control targetView)
    {
        if (TitleView != null) TitleView.Visible = targetView == TitleView;
        if (ClassView != null) ClassView.Visible = targetView == ClassView;
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
        if (ClassPassiveLbl != null) ClassPassiveLbl.Text = Tr("LABEL_PASSIVE") + data["passive"].AsString();
        if (ClassBurstLbl != null) ClassBurstLbl.Text = Tr("LABEL_BURST") + data["burst"].AsString();

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
            ClassConfirmBtn.Text = unlocked ? Tr("BTN_CONFIRM_CLASS") : Tr("BTN_CONFIRM_CLASS_LOCKED");
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
            var btn = new Button
            {
                CustomMinimumSize = new Vector2(280, 52),
                Alignment = HorizontalAlignment.Left,
                Text = " 🌐 " + data["name"].AsString()
            };
            string localKey = key;
            btn.Pressed += () => SelectMap(localKey);
            MapListContainer.AddChild(btn);
        }
    }

    public void SelectMap(string key)
    {
        ActiveMapKey = key;
        var data = GameManager.GetMapInfo(key);
        if (MapNameLbl != null) MapNameLbl.Text = data["name"].AsString();
        if (MapEnvLbl != null) MapEnvLbl.Text = Tr("LABEL_ENV") + data["environment"].AsString();
        if (MapMechLbl != null) MapMechLbl.Text = Tr("LABEL_MECH") + data["mechanic"].AsString();
        if (MapThreatLbl != null) MapThreatLbl.Text = Tr("LABEL_THREAT") + data["threat"].AsString();
    }

    private void OnDeployPressed()
    {
        GameManager.SelectedMap = ActiveMapKey;
        GameManager.StartGame(GetTree());
    }
}
