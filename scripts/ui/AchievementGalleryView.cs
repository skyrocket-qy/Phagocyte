using Godot;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Full-screen achievement gallery: the fifth MainMenu view (Title → Achievements).
/// Card list + detail panel with per-achievement artwork. Artwork resolves from
/// <c>AssetPaths.AchievementSprite(id)</c> (res://assets/gen/achievement/{id}.png);
/// missing files fall back to the emoji <c>icon</c> on a category-tinted plate,
/// so shipping art later requires zero code changes.
/// Filter is intentionally three-state only: all / unlocked / locked.
/// </summary>
public partial class AchievementGalleryView : Control
{
    public const int FilterAll = 0;
    public const int FilterUnlocked = 1;
    public const int FilterLocked = 2;

    public int FilterMode { get; private set; } = FilterAll;
    public string ActiveAchievementId { get; private set; } = "";

    /// <summary>Cards currently shown (after filter). Used by tests.</summary>
    public int CardCount { get; private set; } = 0;

    // Header
    public Label? HeaderLabel { get; private set; }
    public ProgressBar? HeaderProgress { get; private set; }
    public Label? ProgressLabel { get; private set; }

    // Filter
    public Button? FilterAllBtn { get; private set; }
    public Button? FilterUnlockedBtn { get; private set; }
    public Button? FilterLockedBtn { get; private set; }

    // List
    public VBoxContainer? CardList { get; private set; }

    // Detail
    public TextureRect? DetailImage { get; private set; }
    public Label? DetailFallbackIcon { get; private set; }
    public Label? DetailTitle { get; private set; }
    public Label? DetailBadge { get; private set; }
    public Label? DetailProgressLabel { get; private set; }
    public ProgressBar? DetailProgress { get; private set; }
    public Label? DetailDesc { get; private set; }
    public Label? DetailReward { get; private set; }
    public Label? DetailSteam { get; private set; }

    private readonly System.Collections.Generic.Dictionary<string, PanelContainer> _cards = new();
    private Callable _langCallback;
    private Callable _achUnlockCallback;

    private static readonly Color LockedDim = new(0.45f, 0.47f, 0.55f);

    public override void _Ready()
    {
        HeaderLabel = GetNodeOrNull<Label>("HeaderLabel");
        HeaderProgress = GetNodeOrNull<ProgressBar>("ProgressHBox/ProgressBar");
        ProgressLabel = GetNodeOrNull<Label>("ProgressHBox/ProgressLabel");

        FilterAllBtn = GetNodeOrNull<Button>("FilterHBox/FilterAllButton");
        FilterUnlockedBtn = GetNodeOrNull<Button>("FilterHBox/FilterUnlockedButton");
        FilterLockedBtn = GetNodeOrNull<Button>("FilterHBox/FilterLockedButton");

        CardList = GetNodeOrNull<VBoxContainer>("HBox/CardScroll/CardList");

        DetailImage = GetNodeOrNull<TextureRect>("HBox/DetailPanel/DetailVBox/DetailTopHBox/DetailImageWrap/DetailImage");
        DetailFallbackIcon = GetNodeOrNull<Label>("HBox/DetailPanel/DetailVBox/DetailTopHBox/DetailImageWrap/DetailFallbackIcon");
        // Card thumbnails already show the art; the detail-side image is
        // redundant, so collapse the whole wrap instead of rendering it.
        var detailWrap = GetNodeOrNull<Control>("HBox/DetailPanel/DetailVBox/DetailTopHBox/DetailImageWrap");
        if (detailWrap != null)
            detailWrap.Visible = false;
        DetailTitle = GetNodeOrNull<Label>("HBox/DetailPanel/DetailVBox/DetailTopHBox/DetailTitleVBox/DetailTitle");
        DetailBadge = GetNodeOrNull<Label>("HBox/DetailPanel/DetailVBox/DetailTopHBox/DetailTitleVBox/DetailBadge");
        DetailProgressLabel = GetNodeOrNull<Label>("HBox/DetailPanel/DetailVBox/DetailProgressLabel");
        DetailProgress = GetNodeOrNull<ProgressBar>("HBox/DetailPanel/DetailVBox/DetailProgress");
        DetailDesc = GetNodeOrNull<Label>("HBox/DetailPanel/DetailVBox/DetailDesc");
        DetailReward = GetNodeOrNull<Label>("HBox/DetailPanel/DetailVBox/DetailReward");
        DetailSteam = GetNodeOrNull<Label>("HBox/DetailPanel/DetailVBox/DetailSteam");

        if (FilterAllBtn != null)
            FilterAllBtn.Pressed += () => SetFilter(FilterAll);
        if (FilterUnlockedBtn != null)
            FilterUnlockedBtn.Pressed += () => SetFilter(FilterUnlocked);
        if (FilterLockedBtn != null)
            FilterLockedBtn.Pressed += () => SetFilter(FilterLocked);

        // Same listener route as ToastView: works even before the autoload
        // Instance is assigned, and survives scene swaps via the static list.
        _achUnlockCallback = Callable.From((string achId, Godot.Collections.Dictionary _info) => OnAchievementUnlocked(achId));
        AchievementManager.AddUnlockListener(_achUnlockCallback);

        _langCallback = Callable.From((string _) => UpdateLocalizedTexts());
        GameManager.AddLanguageListener(_langCallback);

        UpdateLocalizedTexts();
    }

    public override void _ExitTree()
    {
        GameManager.RemoveLanguageListener(_langCallback);
        AchievementManager.RemoveUnlockListener(_achUnlockCallback);
        base._ExitTree();
    }

    private void OnAchievementUnlocked(string _achId)
    {
        if (Visible)
            Refresh();
    }

    /// <summary>Called by MainMenu when switching to this view.</summary>
    public void Open()
    {
        Refresh();
    }

    public void UpdateLocalizedTexts()
    {
        if (HeaderLabel != null)
            HeaderLabel.Text = Tr("HEADER_ACHIEVEMENTS");
        if (FilterAllBtn != null)
            FilterAllBtn.Text = Tr("ACH_FILTER_ALL");
        if (FilterUnlockedBtn != null)
            FilterUnlockedBtn.Text = Tr("ACH_FILTER_UNLOCKED");
        if (FilterLockedBtn != null)
            FilterLockedBtn.Text = Tr("ACH_FILTER_LOCKED");
        Refresh();
    }

    public void SetFilter(int mode)
    {
        if (mode < FilterAll || mode > FilterLocked)
            return;
        FilterMode = mode;
        UiBuilders.SetTabActive(FilterAllBtn, mode == FilterAll);
        UiBuilders.SetTabActive(FilterUnlockedBtn, mode == FilterUnlocked);
        UiBuilders.SetTabActive(FilterLockedBtn, mode == FilterLocked);
        Refresh();
    }

    public void Refresh()
    {
        if (CardList == null)
            return;

        _cards.Clear();
        foreach (var child in CardList.GetChildren())
        {
            CardList.RemoveChild(child);
            child.QueueFree();
        }

        var all = AchievementManager.GetAllAchievements();
        int unlockedTotal = 0;
        foreach (var ach in all)
        {
            if (ach["unlocked"].AsBool())
                unlockedTotal++;
        }

        int shown = 0;
        string firstId = "";
        foreach (var ach in all)
        {
            string aid = ach["id"].AsString();
            bool unlocked = ach["unlocked"].AsBool();
            if (FilterMode == FilterUnlocked && !unlocked)
                continue;
            if (FilterMode == FilterLocked && unlocked)
                continue;
            if (firstId == "")
                firstId = aid;
            CardList.AddChild(BuildCard(ach));
            shown++;
        }
        CardCount = shown;

        if (shown == 0)
        {
            var empty = new Label { Text = Tr("ACH_EMPTY_FILTER") };
            empty.AddThemeColorOverride("font_color", new Color(0.6f, 0.65f, 0.72f));
            CardList.AddChild(empty);
        }

        if (HeaderProgress != null)
        {
            HeaderProgress.MinValue = 0;
            HeaderProgress.MaxValue = 100;
            HeaderProgress.Value = all.Count > 0 ? unlockedTotal * 100.0 / all.Count : 0;
            HeaderProgress.ShowPercentage = false;
        }
        if (ProgressLabel != null)
            ProgressLabel.Text = TextFormatter.Format(Tr("ACH_PROGRESS_FMT"), unlockedTotal, all.Count);

        string target = !string.IsNullOrEmpty(ActiveAchievementId) && AchievementManager.Achievements.ContainsKey(ActiveAchievementId)
            ? ActiveAchievementId : firstId;
        if (!string.IsNullOrEmpty(target))
            SelectAchievement(target);
        else
            ClearDetail();
    }

    public void SelectAchievement(string achId)
    {
        if (!AchievementManager.Achievements.ContainsKey(achId))
            return;
        ActiveAchievementId = achId;
        var d = AchievementManager.GetAchievementInfo(achId);
        bool unlocked = d["unlocked"].AsBool();
        Color accent = CategoryColor(d);

        // Detail-side art retired: the card thumbnail on the left already
        // shows it. Keep the nodes hidden if the scene provides them.
        if (DetailImage != null)
            DetailImage.Visible = false;
        if (DetailFallbackIcon != null)
            DetailFallbackIcon.Visible = false;

        if (DetailTitle != null)
            DetailTitle.Text = d["title"].AsString();
        if (DetailBadge != null)
        {
            DetailBadge.Text = "[ " + (unlocked ? Tr("STATUS_ACH_COMPLETED") : Tr("STATUS_ACH_LOCKED")) + " ]";
            DetailBadge.Modulate = unlocked ? new Color(0.3f, 1.0f, 0.4f) : accent;
        }

        float curVal = d["current_value"].AsSingle();
        float targetVal = d["target_value"].AsSingle();
        string curValStr = targetVal >= 1.0f ? ((int)curVal).ToString() : curVal.ToString("F1");
        string targetValStr = targetVal >= 1.0f ? ((int)targetVal).ToString() : targetVal.ToString("F1");
        int pct = (int)(d["progress_ratio"].AsSingle() * 100);
        if (DetailProgressLabel != null)
            DetailProgressLabel.Text = $"{curValStr} / {targetValStr} ({pct}%)";
        if (DetailProgress != null)
        {
            DetailProgress.MinValue = 0;
            DetailProgress.MaxValue = 100;
            DetailProgress.Value = pct;
            DetailProgress.ShowPercentage = false;
        }

        if (DetailDesc != null)
            DetailDesc.Text = Tr("LABEL_UNLOCK_REQ") + "\n" + d["desc"].AsString();
        if (DetailReward != null)
        {
            string reward = d["reward"].AsString();
            DetailReward.Text = !string.IsNullOrEmpty(reward) ? Tr("LABEL_REWARD") + "\n" + reward : "";
            DetailReward.Visible = !string.IsNullOrEmpty(reward);
        }
        if (DetailSteam != null)
        {
            string apiName = d.TryGetValue("steam_api_name", out var snVal) ? snVal.AsString() : achId.ToUpperInvariant();
            string syncState = unlocked
                ? (SteamBridge.IsAvailable ? Tr("ACH_STEAM_SYNCED") : Tr("ACH_STEAM_LOCAL_ONLY"))
                : Tr("STATUS_ACH_LOCKED");
            DetailSteam.Text = TextFormatter.Format(Tr("ACH_STEAM_ID_FMT"), apiName) + " · " + syncState;
        }

        RefreshCardSelection();
    }

    private void ClearDetail()
    {
        ActiveAchievementId = "";
        if (DetailTitle != null)
            DetailTitle.Text = "";
        if (DetailBadge != null)
            DetailBadge.Text = "";
        if (DetailProgressLabel != null)
            DetailProgressLabel.Text = "";
        if (DetailDesc != null)
            DetailDesc.Text = "";
        if (DetailReward != null)
            DetailReward.Text = "";
        if (DetailSteam != null)
            DetailSteam.Text = "";
        if (DetailImage != null)
            DetailImage.Visible = false;
        if (DetailFallbackIcon != null)
            DetailFallbackIcon.Visible = false;
    }

    /// <summary>
    /// Accent color by achievement family: Hard clears read red, cell-unlock
    /// rewards green, normal map clears blue, generic milestones gold.
    /// Derived from the info dict so no extra catalog lookups are needed.
    /// </summary>
    public static Color CategoryColor(Godot.Collections.Dictionary info)
    {
        string id = info.TryGetValue("id", out var idVal) ? idVal.AsString() : "";
        string rewardCell = info.TryGetValue("reward_cell", out var rcVal) ? rcVal.AsString() : "";
        if (id.EndsWith("_hard_clear"))
            return new Color(1.0f, 0.45f, 0.4f);
        if (!string.IsNullOrEmpty(rewardCell))
            return new Color(0.3f, 1.0f, 0.4f);
        if (id.EndsWith("_clear"))
            return new Color(0.4f, 0.8f, 1.0f);
        return new Color(1.0f, 0.84f, 0.35f);
    }

    private PanelContainer BuildCard(Godot.Collections.Dictionary ach)
    {
        string aid = ach["id"].AsString();
        bool unlocked = ach["unlocked"].AsBool();
        Color accent = CategoryColor(ach);
        int pct = (int)(ach["progress_ratio"].AsSingle() * 100);

        var card = new PanelContainer
        {
            Name = $"AchCard_{aid}",
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.PointingHand
        };
        var style = UiBuilders.PanelStyle(
            unlocked ? new Color(0.07f, 0.12f, 0.18f, 0.92f) : new Color(0.05f, 0.07f, 0.11f, 0.92f),
            accent, cornerRadius: 8, marginH: 12, marginV: 10);
        style.BorderWidthLeft = 4;
        style.BorderWidthTop = 1;
        style.BorderWidthRight = 1;
        style.BorderWidthBottom = 1;
        card.AddThemeStyleboxOverride("panel", style);
        if (!unlocked)
            card.Modulate = new Color(0.85f, 0.87f, 0.92f);

        string localAid = aid;
        card.GuiInput += (InputEvent @event) =>
        {
            if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                SelectAchievement(localAid);
        };

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 12);
        card.AddChild(hbox);

        var thumbWrap = new Control { CustomMinimumSize = new Vector2(64, 64) };
        thumbWrap.MouseFilter = MouseFilterEnum.Ignore;
        hbox.AddChild(thumbWrap);

        Texture2D? tex = AssetLoader.TryLoad<Texture2D>(ach.TryGetValue("image_path", out var ipVal) ? ipVal.AsString() : "")
            ?? AssetLoader.TryLoad<Texture2D>(AssetPaths.PlaceholderIcon);
        if (tex != null)
        {
            var thumb = new TextureRect
            {
                Texture = tex,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                AnchorRight = 1.0f,
                AnchorBottom = 1.0f,
                MouseFilter = MouseFilterEnum.Ignore
            };
            if (!unlocked)
                thumb.Modulate = LockedDim;
            thumbWrap.AddChild(thumb);
        }

        var vbox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        vbox.AddThemeConstantOverride("separation", 4);
        vbox.MouseFilter = MouseFilterEnum.Ignore;
        hbox.AddChild(vbox);

        var title = new Label
        {
            Text = (unlocked ? "✅ " : "🔒 ") + ach["title"].AsString(),
            MouseFilter = MouseFilterEnum.Ignore
        };
        title.AddThemeFontSizeOverride("font_size", 15);
        title.AddThemeColorOverride("font_color",
            unlocked ? new Color(0.94f, 0.99f, 0.98f) : new Color(0.62f, 0.66f, 0.72f));
        vbox.AddChild(title);

        var mini = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = pct,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 10),
            MouseFilter = MouseFilterEnum.Ignore
        };
        vbox.AddChild(mini);

        var sub = new Label
        {
            Text = $"{pct}% · {ach["desc"].AsString()}",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore
        };
        sub.AddThemeFontSizeOverride("font_size", 12);
        sub.AddThemeColorOverride("font_color", new Color(0.6f, 0.68f, 0.76f));
        vbox.AddChild(sub);

        _cards[aid] = card;
        return card;
    }

    private void RefreshCardSelection()
    {
        foreach (var kv in _cards)
        {
            if (!GodotObject.IsInstanceValid(kv.Value))
                continue;
            kv.Value.AddThemeColorOverride("font_color", Colors.White);
            // Selected card glows: full modulate; others slightly dimmed.
            var baseMod = kv.Value.Modulate;
            baseMod.A = kv.Key == ActiveAchievementId ? 1.0f : 0.92f;
            kv.Value.Modulate = baseMod;
        }
    }
}
