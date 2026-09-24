using Godot;
using Godot.Collections;
using System;
using Phagocyte.Core;

namespace Phagocyte.UI;

public partial class CodexModal : ModalBase
{
    public Button? TabSkillsBtn { get; set; }
    public Button? TabPassivesBtn { get; set; }
    public Button? TabCellsBtn { get; set; }
    public Button? TabPathogensBtn { get; set; }
    public Button? TabMapsBtn { get; set; }
    public Button? TabOrganellesBtn { get; set; }
    public Button? TabBossesBtn { get; set; }

    public VBoxContainer? ItemList { get; set; }
    public TextureRect? DetailIcon { get; set; }
    public Label? DetailTitle { get; set; }
    public Label? DetailBadge { get; set; }
    public Label? DetailStats { get; set; }
    public Label? DetailDesc { get; set; }
    public Label? DetailBio { get; set; }

    public int CurrentTab { get; set; } = 0;
    public string ActiveItemKey { get; set; } = "";

    private static readonly Font ItemFont =
        AssetLoader.Load<Font>("res://assets/fonts/BodyMediumFont.tres");
    private static readonly Color ItemFontColor = new Color(0.94f, 0.99f, 0.98f);
    private static readonly Color ItemHoverColor = new Color(0.39f, 1.0f, 0.85f);

    /// <summary>Tab item buttons by catalog key, for active-selection highlight.</summary>
    private readonly System.Collections.Generic.Dictionary<string, Button> _itemButtons = new();

    public const int TabActives = 0;
    public const int TabPassives = 1;
    public const int TabCells = 2;
    public const int TabPathogens = 3;
    public const int TabMaps = 4;
    public const int TabOrganelles = 5;
    public const int TabBosses = 6;

    public override void _Ready()
    {
        TabSkillsBtn = GetNodeOrNull<Button>("VBox/TabBar/SkillsTab");
        TabPassivesBtn = GetNodeOrNull<Button>("VBox/TabBar/PassivesTab");
        TabCellsBtn = GetNodeOrNull<Button>("VBox/TabBar/CellsTab");
        TabPathogensBtn = GetNodeOrNull<Button>("VBox/TabBar/PathogensTab");
        TabMapsBtn = GetNodeOrNull<Button>("VBox/TabBar/MapsTab");
        TabOrganellesBtn = GetNodeOrNull<Button>("VBox/TabBar/OrganellesTab");
        TabBossesBtn = GetNodeOrNull<Button>("VBox/TabBar/BossesTab");

        ItemList = GetNodeOrNull<VBoxContainer>("VBox/HBox/Scroll/ItemList");
        DetailIcon = GetNodeOrNull<TextureRect>("VBox/HBox/DetailPanel/VBox/TopRow/DetailImageWrap/DetailIcon");
        DetailTitle = GetNodeOrNull<Label>("VBox/HBox/DetailPanel/VBox/TopRow/TitleVBox/DetailTitle")
            ?? GetNodeOrNull<Label>("VBox/HBox/DetailPanel/VBox/DetailTitle");
        DetailBadge = GetNodeOrNull<Label>("VBox/HBox/DetailPanel/VBox/TopRow/TitleVBox/DetailBadge")
            ?? GetNodeOrNull<Label>("VBox/HBox/DetailPanel/VBox/DetailBadge");
        DetailStats = GetNodeOrNull<Label>("VBox/HBox/DetailPanel/VBox/TopRow/TitleVBox/DetailStats")
            ?? GetNodeOrNull<Label>("VBox/HBox/DetailPanel/VBox/DetailStats");
        DetailDesc = GetNodeOrNull<Label>("VBox/HBox/DetailPanel/VBox/DetailDesc");
        DetailBio = GetNodeOrNull<Label>("VBox/HBox/DetailPanel/VBox/DetailBio");

        if (TabSkillsBtn != null)
            TabSkillsBtn.Pressed += () => SwitchTab(TabActives);
        if (TabPassivesBtn != null)
            TabPassivesBtn.Pressed += () => SwitchTab(TabPassives);
        if (TabCellsBtn != null)
            TabCellsBtn.Pressed += () => SwitchTab(TabCells);
        if (TabPathogensBtn != null)
            TabPathogensBtn.Pressed += () => SwitchTab(TabPathogens);
        if (TabMapsBtn != null)
            TabMapsBtn.Pressed += () => SwitchTab(TabMaps);
        if (TabOrganellesBtn != null)
            TabOrganellesBtn.Pressed += () => SwitchTab(TabOrganelles);
        if (TabBossesBtn != null)
            TabBossesBtn.Pressed += () => SwitchTab(TabBosses);

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

    private static readonly StyleBoxFlat ItemPlateNormal =
        MakeItemPlate(new Color(0.04f, 0.08f, 0.13f, 0.88f), new Color(0.2f, 0.38f, 0.52f, 0.65f), false);
    private static readonly StyleBoxFlat ItemPlateActive =
        MakeItemPlate(new Color(0.06f, 0.18f, 0.24f, 0.96f), new Color(0.35f, 0.95f, 0.85f, 1.0f), true);
    private static readonly StyleBoxFlat ItemPlateHover =
        MakeItemPlate(new Color(0.07f, 0.16f, 0.22f, 0.94f), new Color(0.35f, 0.85f, 0.75f, 0.9f), false);

    /// <summary>
    /// Paints the active entry GFP so selection never reads as "select A, view B".
    /// Called at the end of every Select* method.
    /// </summary>
    private void RefreshItemSelection()
    {
        foreach (var kv in _itemButtons)
        {
            bool isSelected = kv.Key == ActiveItemKey;
            kv.Value.AddThemeColorOverride("font_color", isSelected ? ItemHoverColor : ItemFontColor);
            kv.Value.AddThemeStyleboxOverride("normal", isSelected ? ItemPlateActive : ItemPlateNormal);
        }
    }

    private static StyleBoxFlat MakeItemPlate(Color bg, Color border, bool active)
    {
        var sb = new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            ContentMarginLeft = 14.0f,
            ContentMarginTop = 6.0f,
            ContentMarginRight = 10.0f,
            ContentMarginBottom = 6.0f,
            BorderWidthLeft = active ? 3 : 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = active ? 2 : 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomRight = 10,
            CornerRadiusBottomLeft = 2,
        };
        if (active)
        {
            sb.ShadowColor = new Color(0.15f, 0.92f, 0.82f, 0.35f);
            sb.ShadowSize = 6;
        }
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
            CustomMinimumSize = new Vector2(0, 36),
            Alignment = HorizontalAlignment.Left,
            Text = line1
        };
        btn.AddThemeFontOverride("font", ItemFont);
        btn.AddThemeFontSizeOverride("font_size", 14);
        btn.AddThemeColorOverride("font_color", ItemFontColor);
        btn.AddThemeColorOverride("font_hover_color", ItemHoverColor);
        btn.AddThemeColorOverride("font_pressed_color", new Color(0.3f, 0.95f, 0.8f));
        btn.AddThemeColorOverride("font_outline_color", new Color(0.01f, 0.03f, 0.06f, 0.95f));
        btn.AddThemeConstantOverride("outline_size", 2);
        btn.AddThemeStyleboxOverride("normal", ItemPlateNormal);
        btn.AddThemeStyleboxOverride("hover", ItemPlateHover);
        btn.AddThemeStyleboxOverride("pressed", ItemPlateActive);
        return btn;
    }

    public override void UpdateLocalizedTexts()
    {
        base.UpdateLocalizedTexts();
        if (TitleLabel != null) TitleLabel.Text = Tr("CODEX_TITLE");
        if (TabSkillsBtn != null) TabSkillsBtn.Text = Tr("CODEX_TAB_ACTIVES");
        if (TabPassivesBtn != null) TabPassivesBtn.Text = Tr("CODEX_TAB_PASSIVES");
        if (TabCellsBtn != null) TabCellsBtn.Text = Tr("CODEX_TAB_CELLS");
        if (TabPathogensBtn != null) TabPathogensBtn.Text = Tr("CODEX_TAB_PATHOGENS");
        if (TabMapsBtn != null) TabMapsBtn.Text = Tr("CODEX_TAB_MAPS");
        if (TabOrganellesBtn != null) TabOrganellesBtn.Text = Tr("CODEX_TAB_ORGANELLES");
        if (TabBossesBtn != null) TabBossesBtn.Text = Tr("CODEX_TAB_BOSSES");

        RenderCurrentTab();
    }

    public void SwitchTab(int tabIdx)
    {
        if (tabIdx < TabActives || tabIdx > TabBosses)
            return;
        CurrentTab = tabIdx;

        UiBuilders.SetTabActive(TabSkillsBtn, tabIdx == TabActives);
        UiBuilders.SetTabActive(TabPassivesBtn, tabIdx == TabPassives);
        UiBuilders.SetTabActive(TabCellsBtn, tabIdx == TabCells);
        UiBuilders.SetTabActive(TabPathogensBtn, tabIdx == TabPathogens);
        UiBuilders.SetTabActive(TabMapsBtn, tabIdx == TabMaps);
        UiBuilders.SetTabActive(TabOrganellesBtn, tabIdx == TabOrganelles);
        UiBuilders.SetTabActive(TabBossesBtn, tabIdx == TabBosses);

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
            case TabActives:
                RenderSkillsTab(passiveOnly: false);
                break;
            case TabPassives:
                RenderSkillsTab(passiveOnly: true);
                break;
            case TabCells:
                RenderCellsTab();
                break;
            case TabPathogens:
                RenderPathogensTab();
                break;
            case TabMaps:
                RenderMapsTab();
                break;
            case TabOrganelles:
                RenderOrganellesTab();
                break;
            case TabBosses:
                RenderBossesTab();
                break;
        }
    }

    /// <summary>
    /// Skills are split by function only: actives (active + cell innates,
    /// which are active weapons) vs passives. No innate/exclusive grouping.
    /// </summary>
    private void RenderSkillsTab(bool passiveOnly)
    {
        if (ItemList == null)
            return;

        string firstKey = "";
        foreach (var keyVar in GameManager.SkillCatalog.Keys)
        {
            string key = keyVar.AsString();
            var skillInfo = GameManager.GetSkillInfo(key);
            bool isPassive = skillInfo["type"].AsString() == "passive";
            if (isPassive != passiveOnly)
                continue;
            if (firstKey == "")
                firstKey = key;
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

        if (DetailIcon != null)
        {
            string iconPath = AssetPaths.SkillIcon(key);
            DetailIcon.Texture = AssetLoader.TryLoad<Texture2D>(iconPath)
                ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
        }

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

        if (DetailIcon != null)
        {
            string unlockAch = d.TryGetValue("unlock_achievement", out var achVal) ? achVal.AsString() : "";
            string iconPath = !string.IsNullOrEmpty(unlockAch)
                ? AssetPaths.AchievementSprite(unlockAch)
                : AssetPaths.AchievementSprite("first_evolution");
            DetailIcon.Texture = AssetLoader.TryLoad<Texture2D>(iconPath)
                ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
        }

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

    public void SelectPathogen(string key)
    {
        ActiveItemKey = key;
        var d = GameManager.GetPathogenInfo(key);
        if (!d.ContainsKey("name"))
            d = GameManager.GetBossInfo(key);
        if (DetailTitle != null) DetailTitle.Text = d["name"].AsString();

        if (DetailIcon != null)
        {
            string iconPath = key switch
            {
                "malignant_cell" => AssetPaths.AchievementSprite("prion_cleared"),
                "e_coli" or "pseudomonas" => AssetPaths.SkillIcon("endotoxin"),
                "tb" or "tb_behemoth" => AssetPaths.SkillIcon("lysosome"),
                "flu_drift" or "s_virus" or "fludust_cyclone" => AssetPaths.SkillIcon("autophagy"),
                _ => AssetPaths.UiIcon("badge_antigen")
            };
            DetailIcon.Texture = AssetLoader.TryLoad<Texture2D>(iconPath)
                ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.UiIcon("reticle_danger"))
                ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
        }

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

        if (DetailIcon != null)
        {
            string iconPath = key switch
            {
                "acute_wound" => AssetPaths.AchievementSprite("wound_clear"),
                "alveolar_space" => AssetPaths.AchievementSprite("alveolar_clear"),
                "hepatic_sinusoid" => AssetPaths.AchievementSprite("hepatic_clear"),
                "gastric_cavity" => AssetPaths.AchievementSprite("gastric_clear"),
                "blood_brain_barrier" => AssetPaths.AchievementSprite("bbb_clear"),
                _ => AssetPaths.PlaceholderIcon
            };
            DetailIcon.Texture = AssetLoader.TryLoad<Texture2D>(iconPath)
                ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
        }

        if (DetailBadge != null)
        {
            DetailBadge.Text = "[ " + Tr("CODEX_STAGE_STATUS_OPEN") + " ]";
            DetailBadge.Modulate = new Color(0.4f, 0.8f, 1.0f);
        }
        if (DetailStats != null) DetailStats.Text = Tr("LABEL_ENV") + d["environment"].AsString();
        if (DetailDesc != null) DetailDesc.Text = Tr("CODEX_HEADER_MECH") + "\n" + d["mechanic"].AsString();
        if (DetailBio != null) DetailBio.Text = Tr("CODEX_HEADER_THREAT") + "\n" + d["threat"].AsString()
            + (d.TryGetValue("biochemistry", out var mapBio) && mapBio.AsString() != ""
                ? "\n\n" + Tr("CODEX_HEADER_BIO") + "\n" + UiBuilders.StripLeadingLabel(mapBio.AsString()) : "");
        RefreshItemSelection();
    }

    private void RenderBossesTab()
    {
        if (ItemList == null)
            return;

        string firstKey = "";
        foreach (var keyVar in GameManager.BossCatalog.Keys)
        {
            string key = keyVar.AsString();
            if (firstKey == "")
                firstKey = key;
            var bd = GameManager.GetBossInfo(key);
            var bbtn = MakeItemButton("👑 " + bd["name"].AsString());
            string localBossKey = key;
            bbtn.Pressed += () => SelectPathogen(localBossKey);
            ItemList.AddChild(bbtn);
            _itemButtons[localBossKey] = bbtn;
        }

        string target = ActiveItemKey != "" && GameManager.BossCatalog.ContainsKey(ActiveItemKey) ? ActiveItemKey : firstKey;
        if (target != "")
        {
            SelectPathogen(target);
        }
    }

    private void RenderOrganellesTab()
    {
        if (ItemList == null)
            return;

        string firstKey = "";
        foreach (var keyVar in GameManager.OrganelleCatalog.Keys)
        {
            string key = keyVar.AsString();
            if (firstKey == "")
                firstKey = key;
            var entry = (Dictionary)GameManager.OrganelleCatalog[key];
            var btn = MakeItemButton(" " + Tr(entry["name_key"].AsString()));
            string localKey = key;
            btn.Pressed += () => SelectOrganelle(localKey);
            ItemList.AddChild(btn);
            _itemButtons[localKey] = btn;
        }

        string target = ActiveItemKey != "" && GameManager.OrganelleCatalog.ContainsKey(ActiveItemKey) ? ActiveItemKey : firstKey;
        if (target != "")
        {
            SelectOrganelle(target);
        }
    }

    private void SelectOrganelle(string key)
    {
        ActiveItemKey = key;
        if (!GameManager.OrganelleCatalog.ContainsKey(key))
            return;
        var entry = (Dictionary)GameManager.OrganelleCatalog[key];

        if (DetailTitle != null) DetailTitle.Text = Tr(entry["name_key"].AsString());

        if (DetailIcon != null)
        {
            DetailIcon.Texture = AssetLoader.TryLoad<Texture2D>(AssetPaths.OrganelleIcon(key))
                ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
        }

        string category = entry["category"].AsString();
        if (DetailBadge != null)
        {
            DetailBadge.Text = "[ " + Tr("ORGANELLE_CAT_" + category.ToUpperInvariant()) + " ]";
            DetailBadge.Modulate = OrganelleSlot.CategoryColor(category);
        }
        int cost = entry["energy_cost"].AsInt32();
        string costText = cost < 0 ? $"+{-cost}" : cost.ToString();
        if (DetailStats != null) DetailStats.Text = Tr("LOADOUT_COST_LABEL") + ": " + costText;
        if (DetailDesc != null) DetailDesc.Text = Tr("CODEX_HEADER_TACTICAL") + "\n" + Tr(entry["desc_key"].AsString());
        if (DetailBio != null) DetailBio.Text = Tr("CODEX_HEADER_BIO") + "\n" + UiBuilders.StripLeadingLabel(Tr(entry["bio_key"].AsString()));
        RefreshItemSelection();
    }

}
