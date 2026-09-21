using Godot;
using Godot.Collections;
using System;
using Phagocyte.Core;

namespace Phagocyte.UI;

public partial class CodexModal : ModalBase
{
    public Button? TabSkillsBtn { get; set; }
    public Button? TabCellsBtn { get; set; }
    public Button? TabPathogensBtn { get; set; }
    public Button? TabMapsBtn { get; set; }
    public Button? TabAchievementsBtn { get; set; }

    public VBoxContainer? ItemList { get; set; }
    public Label? DetailTitle { get; set; }
    public Label? DetailBadge { get; set; }
    public Label? DetailStats { get; set; }
    public Label? DetailDesc { get; set; }
    public Label? DetailBio { get; set; }

    public int CurrentTab { get; set; } = 0;
    public string ActiveItemKey { get; set; } = "";

    private static readonly Font ItemFont =
        GD.Load<Font>("res://assets/fonts/BodyMediumFont.tres");
    private static readonly Color ItemFontColor = new Color(0.94f, 0.99f, 0.98f);
    private static readonly Color ItemHoverColor = new Color(0.39f, 1.0f, 0.85f);

    /// <summary>Tab item buttons by catalog key, for active-selection highlight.</summary>
    private readonly System.Collections.Generic.Dictionary<string, Button> _itemButtons = new();

    public override void _Ready()
    {
        TabSkillsBtn = GetNodeOrNull<Button>("VBox/TabBar/SkillsTab");
        TabCellsBtn = GetNodeOrNull<Button>("VBox/TabBar/CellsTab");
        TabPathogensBtn = GetNodeOrNull<Button>("VBox/TabBar/PathogensTab");
        TabMapsBtn = GetNodeOrNull<Button>("VBox/TabBar/MapsTab");
        TabAchievementsBtn = GetNodeOrNull<Button>("VBox/TabBar/AchievementsTab");

        ItemList = GetNodeOrNull<VBoxContainer>("VBox/HBox/Scroll/ItemList");
        DetailTitle = GetNodeOrNull<Label>("VBox/HBox/DetailPanel/VBox/DetailTitle");
        DetailBadge = GetNodeOrNull<Label>("VBox/HBox/DetailPanel/VBox/DetailBadge");
        DetailStats = GetNodeOrNull<Label>("VBox/HBox/DetailPanel/VBox/DetailStats");
        DetailDesc = GetNodeOrNull<Label>("VBox/HBox/DetailPanel/VBox/DetailDesc");
        DetailBio = GetNodeOrNull<Label>("VBox/HBox/DetailPanel/VBox/DetailBio");

        if (TabSkillsBtn != null)
            TabSkillsBtn.Pressed += () => SwitchTab(0);
        if (TabCellsBtn != null)
            TabCellsBtn.Pressed += () => SwitchTab(1);
        if (TabPathogensBtn != null)
            TabPathogensBtn.Pressed += () => SwitchTab(2);
        if (TabMapsBtn != null)
            TabMapsBtn.Pressed += () => SwitchTab(3);
        if (TabAchievementsBtn != null)
            TabAchievementsBtn.Pressed += () => SwitchTab(4);

        InitModal();
    }

    public void OpenCodex(int targetTab = 0)
    {
        Visible = true;
        SwitchTab(targetTab);
    }

    public void CloseCodex()
    {
        CloseModal();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible)
            return;
        bool esc = @event.IsActionPressed("toggle_pause")
            || (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.Escape);
        if (esc)
        {
            CloseCodex();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Paints the active entry GFP so selection never reads as "select A, view B".
    /// Called at the end of every Select* method.
    /// </summary>
    private void RefreshItemSelection()
    {
        foreach (var kv in _itemButtons)
            kv.Value.AddThemeColorOverride("font_color",
                kv.Key == ActiveItemKey ? ItemHoverColor : ItemFontColor);
    }

    private static StyleBoxFlat MakePlate(Color bg, Color border)
    {
        var sb = new StyleBoxFlat { BgColor = bg, BorderColor = border };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(6);
        return sb;
    }

    /// <summary>
    /// Compact single-line archive entry: glyph + display name.
    /// Code-built (not tscn) so all five tabs share one style.
    /// </summary>
    private static Button MakeItemButton(string line1)
    {
        var btn = new Button
        {
            CustomMinimumSize = new Vector2(0, 40),
            Alignment = HorizontalAlignment.Left,
            Text = line1
        };
        btn.AddThemeFontOverride("font", ItemFont);
        btn.AddThemeFontSizeOverride("font_size", 14);
        btn.AddThemeColorOverride("font_color", ItemFontColor);
        btn.AddThemeColorOverride("font_hover_color", ItemHoverColor);
        btn.AddThemeColorOverride("font_pressed_color", new Color(0f, 0.79f, 0.33f));
        btn.AddThemeStyleboxOverride("normal",
            MakePlate(new Color(0.07f, 0.12f, 0.18f, 0.9f), new Color(0.25f, 0.45f, 0.6f, 0.7f)));
        btn.AddThemeStyleboxOverride("hover",
            MakePlate(new Color(0.07f, 0.2f, 0.16f, 0.95f), new Color(0.39f, 1.0f, 0.85f, 1.0f)));
        return btn;
    }

    public override void UpdateLocalizedTexts()
    {
        base.UpdateLocalizedTexts();
        if (CloseBtn != null) CloseBtn.Text = Tr("CODEX_BACK");
        if (TitleLabel != null) TitleLabel.Text = Tr("CODEX_TITLE");
        if (TabSkillsBtn != null) TabSkillsBtn.Text = Tr("CODEX_TAB_SKILLS");
        if (TabCellsBtn != null) TabCellsBtn.Text = Tr("CODEX_TAB_CELLS");
        if (TabPathogensBtn != null) TabPathogensBtn.Text = Tr("CODEX_TAB_PATHOGENS");
        if (TabMapsBtn != null) TabMapsBtn.Text = Tr("CODEX_TAB_MAPS");
        if (TabAchievementsBtn != null) TabAchievementsBtn.Text = Tr("CODEX_TAB_ACHIEVEMENTS");

        RenderCurrentTab();
    }

    public void SwitchTab(int tabIdx)
    {
        CurrentTab = tabIdx;

        UiBuilders.SetTabActive(TabSkillsBtn, tabIdx == 0);
        UiBuilders.SetTabActive(TabCellsBtn, tabIdx == 1);
        UiBuilders.SetTabActive(TabPathogensBtn, tabIdx == 2);
        UiBuilders.SetTabActive(TabMapsBtn, tabIdx == 3);
        UiBuilders.SetTabActive(TabAchievementsBtn, tabIdx == 4);

        ActiveItemKey = "";
        RenderCurrentTab();
    }

    private void RenderCurrentTab()
    {
        if (ItemList == null)
            return;

        _itemButtons.Clear();
        foreach (var child in ItemList.GetChildren())
        {
            ItemList.RemoveChild(child);
            child.QueueFree();
        }

        switch (CurrentTab)
        {
            case 0:
                RenderSkillsTab();
                break;
            case 1:
                RenderCellsTab();
                break;
            case 2:
                RenderPathogensTab();
                break;
            case 3:
                RenderMapsTab();
                break;
            case 4:
                RenderAchievementsTab();
                break;
        }
    }

    private void RenderSkillsTab()
    {
        if (ItemList == null)
            return;

        string firstKey = "";
        foreach (var keyVar in GameManager.SkillCatalog.Keys)
        {
            string key = keyVar.AsString();
            if (firstKey == "")
                firstKey = key;
            var skillInfo = GameManager.GetSkillInfo(key);
            var btn = MakeItemButton(" " + skillInfo["name"].AsString());
            string localKey = key;
            btn.Pressed += () => SelectSkill(localKey);
            ItemList.AddChild(btn);
            _itemButtons[localKey] = btn;
        }

        string target = ActiveItemKey != "" && GameManager.SkillCatalog.ContainsKey(ActiveItemKey) ? ActiveItemKey : firstKey;
        if (target != "")
        {
            SelectSkill(target);
        }
    }

    public void SelectSkill(string key)
    {
        ActiveItemKey = key;
        var d = GameManager.GetSkillInfo(key);
        if (DetailTitle != null) DetailTitle.Text = d["name"].AsString();

        string type = d["type"].AsString();
        UiBuilders.BuildSkillBadge(type, d["cooldown"].AsSingle(), 1, d["max_level"].AsInt32(),
            out string badgeText, out Color badgeColor, out string statsText);
        if (DetailBadge != null)
        {
            DetailBadge.Text = badgeText;
            DetailBadge.Modulate = badgeColor;
        }
        if (DetailStats != null) DetailStats.Text = statsText;

        if (DetailDesc != null) DetailDesc.Text = Tr("CODEX_HEADER_TACTICAL") + "\n" + d["description"].AsString();
        if (DetailBio != null) DetailBio.Text = Tr("CODEX_HEADER_BIO") + "\n" + UiBuilders.StripLeadingLabel(d["biochemistry"].AsString());
        RefreshItemSelection();
    }

    private void RenderCellsTab()
    {
        if (ItemList == null)
            return;

        string firstKey = "";
        foreach (var keyVar in GameManager.ClassData.Keys)
        {
            string key = keyVar.AsString();
            if (firstKey == "")
                firstKey = key;
            var d = GameManager.GetClassInfo(key);
            bool unlocked = d["unlocked"].AsBool();
            var btn = MakeItemButton((unlocked ? " ✅ " : " 🔒 ") + d["name"].AsString());
            string localKey = key;
            btn.Pressed += () => SelectCell(localKey);
            ItemList.AddChild(btn);
            _itemButtons[localKey] = btn;
        }

        string target = ActiveItemKey != "" && GameManager.ClassData.ContainsKey(ActiveItemKey) ? ActiveItemKey : firstKey;
        if (target != "")
        {
            SelectCell(target);
        }
    }

    public void SelectCell(string key)
    {
        ActiveItemKey = key;
        var d = GameManager.GetClassInfo(key);
        bool unlocked = d["unlocked"].AsBool();

        if (DetailTitle != null) DetailTitle.Text = d["name"].AsString();
        if (DetailBadge != null)
        {
            DetailBadge.Text = "[ " + (unlocked ? Tr("STATUS_UNLOCKED") : Tr("STATUS_LOCKED")) + " ]";
            DetailBadge.Modulate = unlocked ? new Color(0.3f, 1.0f, 0.4f) : new Color(0.9f, 0.6f, 0.2f);
        }
        if (DetailStats != null) DetailStats.Text = Tr("LABEL_ROLE") + d["role"].AsString();

        if (DetailDesc != null)
        {
            if (unlocked)
            {
                DetailDesc.Text = Tr("CODEX_HEADER_TRAIT") + "\n" + d["trait"].AsString();
            }
            else
            {
                DetailDesc.Text = AchievementManager.GetCellUnlockRequirementText(key) + "\n\n" + Tr("CODEX_HEADER_TRAIT") + "\n" + d["trait"].AsString();
            }
        }

        if (DetailBio != null)
        {
            DetailBio.Text = "";
        }
        RefreshItemSelection();
    }

    private void RenderPathogensTab()
    {
        if (ItemList == null)
            return;

        string firstKey = "";
        foreach (var keyVar in GameManager.PathogenCatalog.Keys)
        {
            string key = keyVar.AsString();
            if (firstKey == "")
                firstKey = key;
            var d = GameManager.GetPathogenInfo(key);
            var btn = MakeItemButton(" " + d["name"].AsString());
            string localKey = key;
            btn.Pressed += () => SelectPathogen(localKey);
            ItemList.AddChild(btn);
            _itemButtons[localKey] = btn;
        }

        string target = ActiveItemKey != "" && GameManager.PathogenCatalog.ContainsKey(ActiveItemKey) ? ActiveItemKey : firstKey;
        if (target != "")
        {
            SelectPathogen(target);
        }
    }

    private void SelectPathogen(string key)
    {
        ActiveItemKey = key;
        var d = GameManager.GetPathogenInfo(key);
        if (DetailTitle != null) DetailTitle.Text = d["name"].AsString();

        string dangerLv = d.TryGetValue("danger_level", out var dlVal) ? dlVal.AsString() : (d.TryGetValue("threat_level", out var tlVal) ? tlVal.AsString() : "I");
        if (DetailBadge != null)
        {
            DetailBadge.Text = "[ " + TextFormatter.Format(Tr("CODEX_THREAT_LV"), dangerLv) + " ]";
            DetailBadge.Modulate = new Color(1.0f, 0.4f, 0.4f);
        }
        if (DetailStats != null) DetailStats.Text = d["trait"].AsString();
        if (DetailDesc != null) DetailDesc.Text = Tr("CODEX_HEADER_PATHOGEN_TRAIT") + "\n" + d["description"].AsString();
        if (DetailBio != null) DetailBio.Text = Tr("CODEX_HEADER_TACTIC_ADVICE") + "\n" + Tr("CODEX_PATHOGEN_ADVICE_DEFAULT");
        RefreshItemSelection();
    }

    private void RenderMapsTab()
    {
        if (ItemList == null)
            return;

        string firstKey = "";
        foreach (var keyVar in GameManager.MapData.Keys)
        {
            string key = keyVar.AsString();
            if (firstKey == "")
                firstKey = key;
            var d = GameManager.GetMapInfo(key);
            var btn = MakeItemButton(" " + d["name"].AsString());
            string localKey = key;
            btn.Pressed += () => SelectMap(localKey);
            ItemList.AddChild(btn);
            _itemButtons[localKey] = btn;
        }

        string target = ActiveItemKey != "" && GameManager.MapData.ContainsKey(ActiveItemKey) ? ActiveItemKey : firstKey;
        if (target != "")
        {
            SelectMap(target);
        }
    }

    private void SelectMap(string key)
    {
        ActiveItemKey = key;
        var d = GameManager.GetMapInfo(key);
        if (DetailTitle != null) DetailTitle.Text = d["name"].AsString();
        if (DetailBadge != null)
        {
            DetailBadge.Text = "[ " + Tr("CODEX_STAGE_STATUS_OPEN") + " ]";
            DetailBadge.Modulate = new Color(0.4f, 0.8f, 1.0f);
        }
        if (DetailStats != null) DetailStats.Text = Tr("LABEL_ENV") + d["environment"].AsString();
        if (DetailDesc != null) DetailDesc.Text = Tr("CODEX_HEADER_MECH") + "\n" + d["mechanic"].AsString();
        if (DetailBio != null) DetailBio.Text = Tr("CODEX_HEADER_THREAT") + "\n" + d["threat"].AsString();
        RefreshItemSelection();
    }

    private void RenderAchievementsTab()
    {
        if (ItemList == null)
            return;

        var achList = AchievementManager.GetAllAchievements();
        string firstId = "";
        foreach (var ach in achList)
        {
            string aid = ach["id"].AsString();
            if (firstId == "")
                firstId = aid;
            bool unlocked = ach["unlocked"].AsBool();
            var btn = MakeItemButton((unlocked ? " ✅ " : " 🔒 ") + ach["title"].AsString());
            string localAid = aid;
            btn.Pressed += () => SelectAchievement(localAid);
            ItemList.AddChild(btn);
            _itemButtons[localAid] = btn;
        }

        string target = ActiveItemKey != "" && AchievementManager.Achievements.ContainsKey(ActiveItemKey) ? ActiveItemKey : firstId;
        if (target != "")
        {
            SelectAchievement(target);
        }
    }

    private void SelectAchievement(string achId)
    {
        ActiveItemKey = achId;
        var d = AchievementManager.GetAchievementInfo(achId);
        bool unlocked = d["unlocked"].AsBool();
        if (DetailTitle != null) DetailTitle.Text = d["title"].AsString();
        if (DetailBadge != null)
        {
            DetailBadge.Text = "[ " + (unlocked ? Tr("STATUS_ACH_COMPLETED") : Tr("STATUS_ACH_LOCKED")) + " ]";
            DetailBadge.Modulate = unlocked ? new Color(0.3f, 1.0f, 0.4f) : new Color(0.9f, 0.6f, 0.2f);
        }

        float curVal = d["current_value"].AsSingle();
        float targetVal = d["target_value"].AsSingle();
        string curValStr = targetVal >= 1.0f ? ((int)curVal).ToString() : curVal.ToString("F1");
        string targetValStr = targetVal >= 1.0f ? ((int)targetVal).ToString() : targetVal.ToString("F1");
        int pct = (int)(d["progress_ratio"].AsSingle() * 100);

        if (DetailStats != null)
        {
            DetailStats.Text = $"{(unlocked ? Tr("STATUS_ACH_COMPLETED") : Tr("STATUS_ACH_LOCKED"))}: {curValStr} / {targetValStr} ({pct}%)";
        }

        if (DetailDesc != null) DetailDesc.Text = Tr("LABEL_UNLOCK_REQ") + "\n" + d["desc"].AsString();
        if (DetailBio != null)
        {
            string reward = d["reward"].AsString();
            DetailBio.Text = !string.IsNullOrEmpty(reward) ? Tr("LABEL_REWARD") + "\n" + reward : "";
        }
        RefreshItemSelection();
    }
}
