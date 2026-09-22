using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using Phagocyte.Core;
using Phagocyte.Endgame;

namespace Phagocyte.UI;

/// <summary>
/// Medical record panel. Shows the settlement of the just-finished run
/// (victory / defeat) and the persistent history of past runs.
/// </summary>
public partial class RunRecordsModal : ModalBase
{
    public Label? BannerLabel { get; set; }
    public VBoxContainer? SummaryBox { get; set; }
    public ScrollContainer? SummaryScroll { get; set; }
    public HSeparator? SectionSeparator { get; set; }
    public Label? HistoryHeader { get; set; }
    public VBoxContainer? HistoryList { get; set; }
    public ScrollContainer? HistoryScroll { get; set; }
    public TabBar? HistoryTabs { get; set; }
    public Button? RetryBtn { get; set; }
    public Button? MenuBtn { get; set; }

    public bool SettlementMode { get; private set; } = false;

    /// <summary>History chart selected for tactical review (null when none).</summary>
    public Dictionary? SelectedRecord { get; private set; } = null;

    /// <summary>Highest-score chart of the currently displayed tab, pinned on top.</summary>
    public Dictionary? PinnedBest { get; private set; } = null;

    private VBoxContainer? _rootBox = null;
    private Dictionary? _record = null;

    public override void _Ready()
    {
        BannerLabel = GetNodeOrNull<Label>("VBox/Banner");
        SummaryBox = GetNodeOrNull<VBoxContainer>("VBox/SummaryScroll/SummaryBox");
        SummaryScroll = GetNodeOrNull<ScrollContainer>("VBox/SummaryScroll");
        SectionSeparator = GetNodeOrNull<HSeparator>("VBox/Separator");
        HistoryHeader = GetNodeOrNull<Label>("VBox/HistoryHeader");
        HistoryScroll = GetNodeOrNull<ScrollContainer>("VBox/Scroll");
        HistoryList = GetNodeOrNull<VBoxContainer>("VBox/Scroll/HistoryList");
        RetryBtn = GetNodeOrNull<Button>("VBox/Buttons/RetryButton");
        MenuBtn = GetNodeOrNull<Button>("VBox/Buttons/MenuButton");
        _rootBox = GetNodeOrNull<VBoxContainer>("VBox");

        SetupHistoryTabs();

        if (RetryBtn != null)
            RetryBtn.Pressed += OnRetryPressed;
        if (MenuBtn != null)
            MenuBtn.Pressed += OnMenuPressed;

        InitModal();
    }

    private void SetupHistoryTabs()
    {
        if (_rootBox == null || HistoryTabs != null)
            return;

        HistoryTabs = new TabBar { Name = "HistoryTabs" };
        HistoryTabs.AddTab(Tr("RECORDS_TAB_VICTORIES"));
        HistoryTabs.AddTab(Tr("RECORDS_TAB_DEFEATS"));
        HistoryTabs.TabChanged += OnHistoryTabChanged;

        _rootBox.AddChild(HistoryTabs);
        if (HistoryHeader != null)
            _rootBox.MoveChild(HistoryTabs, HistoryHeader.GetIndex());
    }

    public void OpenSettlement(Dictionary record)
    {
        _record = record;
        SettlementMode = true;
        SelectedRecord = null;
        Visible = true;
        UpdateLocalizedTexts();
    }

    public void OpenHistory()
    {
        _record = null;
        SettlementMode = false;
        SelectedRecord = null;
        if (HistoryTabs != null)
            HistoryTabs.CurrentTab = 0;
        Visible = true;
        UpdateLocalizedTexts();
    }

    /// <summary>
    /// Tactical review (docs/record.md §5): show the full clinical chart of a
    /// stored run. Ignored while the settlement panel owns the summary area.
    /// </summary>
    public void SelectRecord(Dictionary record)
    {
        if (SettlementMode || record == null)
            return;

        SelectedRecord = record;
        RefreshSummary();
        RenderHistory();
        if (HistoryScroll != null)
            HistoryScroll.ScrollVertical = 0;
    }

    /// <summary>Switch classification tab (0 = victories, 1 = defeats) and re-render.</summary>
    public void SetHistoryTab(int index)
    {
        if (HistoryTabs != null && index >= 0 && index < HistoryTabs.TabCount)
            HistoryTabs.CurrentTab = index;

        SelectedRecord = null;
        RefreshSummary();
        RenderHistory();
    }

    private void OnHistoryTabChanged(long tab)
    {
        SetHistoryTab((int)tab);
    }

    public override void UpdateLocalizedTexts()
    {
        base.UpdateLocalizedTexts();
        if (TitleLabel != null) TitleLabel.Text = Tr("RECORDS_TITLE");
        if (RetryBtn != null) RetryBtn.Text = Tr("RECORDS_RETRY");
        if (MenuBtn != null) MenuBtn.Text = Tr("RECORDS_MENU");

        if (HistoryTabs != null)
        {
            HistoryTabs.SetTabTitle(0, Tr("RECORDS_TAB_VICTORIES"));
            HistoryTabs.SetTabTitle(1, Tr("RECORDS_TAB_DEFEATS"));
        }

        if (HistoryHeader != null)
        {
            HistoryHeader.Text = TextFormatter.Format(
                Tr("RECORDS_HISTORY_FMT"),
                RunRecordManager.GetRunCount(),
                RunRecordManager.GetVictoryCount(),
                RunRecordManager.FormatTime(RunRecordManager.GetBestSurvivalTime()));
        }

        if (CloseBtn != null) CloseBtn.Visible = !SettlementMode;
        if (RetryBtn != null) RetryBtn.Visible = SettlementMode;
        if (MenuBtn != null) MenuBtn.Visible = SettlementMode;

        // Settlement mode owns the whole panel: the archive list stays
        // rendered (tests read it) but hidden, so the ~20-row summary plus
        // buttons always fit inside the viewport.
        if (HistoryHeader != null) HistoryHeader.Visible = !SettlementMode;
        if (HistoryTabs != null) HistoryTabs.Visible = !SettlementMode;
        if (HistoryScroll != null) HistoryScroll.Visible = !SettlementMode;
        if (SectionSeparator != null) SectionSeparator.Visible = !SettlementMode;

        RefreshSummary();
        RenderHistory();
    }

    private void RefreshSummary()
    {
        if (SummaryBox == null)
            return;

        foreach (var child in SummaryBox.GetChildren())
        {
            SummaryBox.RemoveChild(child);
            child.QueueFree();
        }

        var detail = SettlementMode ? _record : SelectedRecord;

        if (detail == null)
        {
            if (BannerLabel != null)
                BannerLabel.Visible = false;
            SummaryBox.Visible = false;
            if (SummaryScroll != null)
                SummaryScroll.Visible = false;
            return;
        }

        if (BannerLabel != null)
        {
            BannerLabel.Visible = true;
            bool detailVictory = detail.GetValueOrDefault("result", "").AsString() == RunRecordManager.ResultVictory;
            bool chronic = detail.GetValueOrDefault("endless", false).AsBool();
            if (chronic)
            {
                // Golden holographic chronic chart (docs/endgame.md §5.1)
                BannerLabel.Text = Tr("RECORDS_SETTLEMENT_CHRONIC");
                BannerLabel.Modulate = new Color(1.0f, 0.82f, 0.35f);
            }
            else
            {
                BannerLabel.Text = detailVictory ? Tr("RECORDS_SETTLEMENT_VICTORY") : Tr("RECORDS_SETTLEMENT_DEFEAT");
                BannerLabel.Modulate = detailVictory ? new Color(0.45f, 1.0f, 0.55f) : new Color(1.0f, 0.45f, 0.45f);
            }
        }

        SummaryBox.Visible = true;
        if (SummaryScroll != null)
            SummaryScroll.Visible = true;
        string classId = detail.GetValueOrDefault("class_id", "").AsString();
        string mapId = detail.GetValueOrDefault("map_id", "").AsString();
        string rank = GetRank(detail);
        bool chronicChart = detail.GetValueOrDefault("endless", false).AsBool();

        AddSummaryRow("RECORDS_RANK",
            string.IsNullOrEmpty(rank) ? "-" : GetRankLabel(rank),
            string.IsNullOrEmpty(rank) ? null : GetRankColor(rank));
        if (chronicChart)
        {
            AddSummaryRow("RECORDS_CHRONIC_HEADER", Tr("RECORDS_CHRONIC_DIAGNOSIS"), new Color(1.0f, 0.85f, 0.45f));
        }
        AddSummaryRow("RECORDS_CLASS", GetClassName(classId));
        AddSummaryRow("RECORDS_MAP", GetMapName(mapId));
        AddSummaryRow("RECORDS_DIFFICULTY", GetDifficultyName(detail.GetValueOrDefault("difficulty", RunRecordManager.DifficultyNormal).AsString()));
        AddSummaryRow("RECORDS_TIME", RunRecordManager.FormatTime(detail.GetValueOrDefault("survival_time", 0.0f).AsSingle()));
        AddSummaryRow("RECORDS_DATE", RunRecordManager.FormatTimestamp(detail.GetValueOrDefault("timestamp", 0.0).AsDouble()));
        if (detail.ContainsKey("cause"))
            AddSummaryRow("RECORDS_CAUSE", GetCauseName(detail.GetValueOrDefault("cause", "").AsString()));
        AddSummaryRow("RECORDS_LEVEL", detail.GetValueOrDefault("level", 1).AsInt32().ToString());
        AddSummaryRow("RECORDS_KILLS", detail.GetValueOrDefault("kills", 0).AsInt32().ToString());
        AddSummaryRow("RECORDS_KPM", $"{detail.GetValueOrDefault("kpm", 0.0f).AsSingle():F1}");
        AddSummaryRow("RECORDS_DIGESTED", detail.GetValueOrDefault("digested", 0).AsInt32().ToString());
        AddSummaryRow("RECORDS_KILL_SCORE", detail.GetValueOrDefault("kill_score", 0).AsInt32().ToString());
        AddSummaryRow("RECORDS_SCORE", detail.GetValueOrDefault("score", 0).AsInt32().ToString());

        float afflictionMultiplier = detail.GetValueOrDefault("affliction_multiplier", 1.0f).AsSingle();
        if (afflictionMultiplier > 1.0f)
            AddSummaryRow("RECORDS_AFFLICTION_MULT", $"×{afflictionMultiplier:F2}");
        if (detail.TryGetValue("afflictions", out var affVal) && affVal.VariantType == Variant.Type.Array
            && affVal.AsGodotArray().Count > 0)
        {
            AddSummaryRow("RECORDS_AFFLICTIONS", GetAfflictionNames(affVal));
        }

        AddSummaryRow("RECORDS_POINTS", detail.GetValueOrDefault("points_spent", 0).AsInt32().ToString());
        AddSummaryRow("RECORDS_SKILLS", GetSkillNames(detail.GetValueOrDefault("active_skills", new Array<string>())));

        if (detail.TryGetValue("telemetry", out var telVal) && telVal.VariantType == Variant.Type.Dictionary)
        {
            var tel = telVal.AsGodotDictionary();
            float dmgDealt = tel.TryGetValue("total_damage_dealt", out var dd) ? dd.AsSingle() : 0.0f;
            float dmgTaken = tel.TryGetValue("total_damage_taken", out var dt) ? dt.AsSingle() : 0.0f;
            int evaded = tel.TryGetValue("evaded_count", out var ev) ? ev.AsInt32() : 0;
            int blocked = tel.TryGetValue("blocked_count", out var bl) ? bl.AsInt32() : 0;
            float lifeSteal = tel.TryGetValue("lifesteal_healed", out var ls) ? ls.AsSingle() : 0.0f;

            AddSummaryRow("RECORDS_TOTAL_DMG", TextFormatter.Format(Tr("RECORDS_DMG_FMT"), dmgDealt, dmgTaken));
            AddSummaryRow("RECORDS_DEFENSE_PROCS", TextFormatter.Format(Tr("RECORDS_DEFENSE_FMT"), evaded, blocked, $"{lifeSteal:F0}"));
        }
    }

    private void AddSummaryRow(string labelKey, string value, Color? valueColor = null)
    {
        if (SummaryBox == null)
            return;

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);

        var keyLabel = new Label
        {
            Text = Tr(labelKey),
            CustomMinimumSize = new Vector2(170, 0),
            Modulate = new Color(0.55f, 0.8f, 0.95f)
        };
        row.AddChild(keyLabel);

        var valueLabel = new Label
        {
            Text = value,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        if (valueColor.HasValue)
            valueLabel.Modulate = valueColor.Value;
        row.AddChild(valueLabel);

        SummaryBox.AddChild(row);
    }

    private void RenderHistory()
    {
        if (HistoryList == null)
            return;

        foreach (var child in HistoryList.GetChildren())
        {
            HistoryList.RemoveChild(child);
            child.QueueFree();
        }

        PinnedBest = null;

        var records = RunRecordManager.GetAll();
        if (records.Count == 0)
        {
            HistoryList.AddChild(CreateEmptyLabel("RECORDS_EMPTY"));
            return;
        }

        bool wantVictory = HistoryTabs == null || HistoryTabs.CurrentTab == 0;
        var filtered = new List<Dictionary>();
        foreach (var rec in records)
        {
            bool victory = rec.GetValueOrDefault("result", "").AsString() == RunRecordManager.ResultVictory;
            if (victory == wantVictory)
                filtered.Add(rec);
        }

        if (filtered.Count == 0)
        {
            HistoryList.AddChild(CreateEmptyLabel(wantVictory ? "RECORDS_EMPTY_VICTORIES" : "RECORDS_EMPTY_DEFEATS"));
            return;
        }

        // Highest-score chart of this classification is pinned on top (docs/record.md §5)
        foreach (var rec in filtered)
        {
            if (PinnedBest == null || ScoreOf(rec) > ScoreOf(PinnedBest))
                PinnedBest = rec;
        }

        HistoryList.AddChild(CreateHistoryRow(PinnedBest!, isBest: true));
        foreach (var rec in filtered)
        {
            if (ReferenceEquals(rec, PinnedBest))
                continue;
            HistoryList.AddChild(CreateHistoryRow(rec, isBest: false));
        }
    }

    private Label CreateEmptyLabel(string key)
    {
        return new Label
        {
            Text = Tr(key),
            Modulate = new Color(0.7f, 0.7f, 0.7f)
        };
    }

    private int ScoreOf(Dictionary rec)
    {
        return rec.GetValueOrDefault("score", 0).AsInt32();
    }

    private Control CreateHistoryRow(Dictionary rec, bool isBest)
    {
        bool victory = rec.GetValueOrDefault("result", "").AsString() == RunRecordManager.ResultVictory;
        bool chronic = rec.GetValueOrDefault("endless", false).AsBool();
        string classId = rec.GetValueOrDefault("class_id", "").AsString();
        string mapId = rec.GetValueOrDefault("map_id", "").AsString();
        bool selected = !SettlementMode && ReferenceEquals(rec, SelectedRecord);

        var row = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.PointingHand
        };

        var style = UiBuilders.PanelStyle(
            isBest
                ? new Color(0.16f, 0.14f, 0.05f, 0.85f)
                : chronic
                    ? new Color(0.15f, 0.12f, 0.04f, 0.78f)
                    : victory ? new Color(0.08f, 0.18f, 0.12f, 0.7f) : new Color(0.18f, 0.08f, 0.1f, 0.7f),
            cornerRadius: 6, marginH: 12, marginV: 8);
        if (isBest || chronic)
        {
            style.BorderWidthLeft = 2;
            style.BorderWidthTop = 2;
            style.BorderWidthRight = 2;
            style.BorderWidthBottom = 2;
            style.BorderColor = isBest
                ? new Color(1.0f, 0.84f, 0.35f, 0.9f)
                : new Color(0.95f, 0.75f, 0.3f, 0.65f);
        }
        if (selected)
        {
            style.BorderWidthLeft = 2;
            style.BorderWidthTop = 2;
            style.BorderWidthRight = 2;
            style.BorderWidthBottom = 2;
            style.BorderColor = new Color(0.35f, 0.8f, 1.0f);
        }
        row.AddThemeStyleboxOverride("panel", style);

        row.GuiInput += (InputEvent @event) =>
        {
            if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                SelectRecord(rec);
        };

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);

        var label = new Label
        {
            Text = string.Format(
                "{0} {1}\n⏱ {2}  ·  " + Tr("RECORDS_LEVEL_ABBR") + "{3}  ·  🦠 {4}  ·  {5}",
                chronic ? "🌡️" : victory ? "✅" : "☠️",
                chronic ? Tr("RECORDS_CHRONIC_TAG") : Tr(victory ? "RECORDS_VICTORY_TAG" : "RECORDS_DEFEAT_TAG"),
                RunRecordManager.FormatTime(rec.GetValueOrDefault("survival_time", 0.0f).AsSingle()),
                rec.GetValueOrDefault("level", 1).AsInt32(),
                rec.GetValueOrDefault("digested", 0).AsInt32(),
                RunRecordManager.FormatTimestamp(rec.GetValueOrDefault("timestamp", 0.0).AsDouble())),
            VerticalAlignment = VerticalAlignment.Center
        };
        if (chronic)
            label.Modulate = new Color(1.0f, 0.88f, 0.55f);
        vbox.AddChild(label);

        var second = new Label
        {
            Text = $"   {GetClassName(classId)} · {GetMapName(mapId)}",
            Modulate = new Color(0.65f, 0.75f, 0.85f)
        };
        vbox.AddChild(second);

        var meta = new HBoxContainer();
        meta.AddThemeConstantOverride("separation", 8);

        if (isBest)
        {
            var bestTag = new Label
            {
                Text = Tr("RECORDS_BEST_TAG"),
                Modulate = new Color(1.0f, 0.84f, 0.35f),
                VerticalAlignment = VerticalAlignment.Center
            };
            meta.AddChild(bestTag);
        }

        string rank = GetRank(rec);
        var rankLabel = new Label
        {
            Text = string.IsNullOrEmpty(rank) ? "◆ --" : $"◆ {rank}",
            Modulate = GetRankColor(rank),
            VerticalAlignment = VerticalAlignment.Center
        };
        meta.AddChild(rankLabel);

        int kills = rec.GetValueOrDefault("kills", 0).AsInt32();
        float kpm = rec.GetValueOrDefault("kpm", 0.0f).AsSingle();
        int score = rec.GetValueOrDefault("score", 0).AsInt32();
        var stats = new Label
        {
            Text = TextFormatter.Format(Tr("RECORDS_KPM_FMT"), kills, kpm, score) + "  ·  " + GetDifficultyName(rec.GetValueOrDefault("difficulty", RunRecordManager.DifficultyNormal).AsString()),
            Modulate = new Color(0.6f, 0.7f, 0.8f),
            VerticalAlignment = VerticalAlignment.Center
        };
        meta.AddChild(stats);

        bool locked = RunRecordManager.IsLocked(rec);
        var lockBtn = new Button
        {
            Text = "🔒",
            Modulate = locked ? new Color(1.0f, 0.84f, 0.35f) : new Color(0.35f, 0.42f, 0.5f),
            MouseDefaultCursorShape = CursorShape.PointingHand,
            FocusMode = FocusModeEnum.None,
            TooltipText = Tr("RECORDS_LOCK_TOGGLE")
        };
        lockBtn.Pressed += () => ToggleRecordLock(rec);
        meta.AddChild(lockBtn);

        vbox.AddChild(meta);

        row.AddChild(vbox);
        return row;
    }

    private string GetAfflictionNames(Variant afflictionsVariant)
    {
        if (afflictionsVariant.VariantType != Variant.Type.Array)
            return "-";

        var names = new List<string>();
        foreach (var idVar in afflictionsVariant.AsGodotArray())
        {
            var def = AfflictionManager.GetDefinition(idVar.AsString());
            if (def != null)
                names.Add($"{def.Icon} {Tr(def.NameKey)}");
        }

        return names.Count > 0 ? string.Join(" · ", names) : "-";
    }

    private string GetCauseName(string cause)
    {
        return cause switch
        {
            RunRecordManager.CauseSpecificNeutralization => Tr("CAUSE_SPECIFIC_NEUTRALIZATION"),
            RunRecordManager.CauseMembraneRupture => Tr("CAUSE_MEMBRANE_RUPTURE"),
            RunRecordManager.CauseSystemFailure => Tr("CAUSE_SYSTEM_FAILURE"),
            _ => cause
        };
    }

    private static string GetRank(Dictionary rec)
    {
        return rec.GetValueOrDefault("rank", "").AsString();
    }

    private string GetRankLabel(string rank)
    {
        return $"{rank} · {Tr($"RANK_{rank}_TITLE")}";
    }

    private string GetDifficultyName(string difficulty)
    {
        return Tr(difficulty == RunRecordManager.DifficultyHard ? "DIFFICULTY_HARD" : "DIFFICULTY_NORMAL");
    }

    private static Color GetRankColor(string rank)
    {
        return rank switch
        {
            RunRecordManager.RankEX => new Color(0.8f, 0.95f, 1.0f),
            RunRecordManager.RankSSS => new Color(1.0f, 0.9f, 0.5f),
            RunRecordManager.RankS => new Color(1.0f, 0.84f, 0.35f),
            RunRecordManager.RankA => new Color(0.45f, 0.95f, 1.0f),
            RunRecordManager.RankB => new Color(0.55f, 1.0f, 0.6f),
            RunRecordManager.RankC => new Color(1.0f, 0.75f, 0.4f),
            RunRecordManager.RankD => new Color(1.0f, 0.5f, 0.5f),
            _ => new Color(0.6f, 0.6f, 0.65f)
        };
    }

    private static string GetClassName(string classId)
    {
        var info = GameManager.GetClassInfo(classId);
        return info.TryGetValue("name", out var nameVal) ? nameVal.AsString() : classId;
    }

    private static string GetMapName(string mapId)
    {
        var info = GameManager.GetMapInfo(mapId);
        return info.TryGetValue("name", out var nameVal) ? nameVal.AsString() : mapId;
    }

    private static string GetSkillNames(Variant skillsVariant)
    {
        if (skillsVariant.VariantType != Variant.Type.Array)
            return "-";

        var names = new List<string>();
        foreach (var idVar in skillsVariant.AsGodotArray())
        {
            string id = idVar.AsString();
            if (string.IsNullOrEmpty(id))
                continue;
            var info = GameManager.GetSkillInfo(id);
            names.Add(info.TryGetValue("name", out var nameVal) ? nameVal.AsString() : id);
        }

        return names.Count > 0 ? string.Join(" · ", names) : "-";
    }

    /// <summary>
    /// Archive lock (docs/record.md §5): pinned charts are immune to FIFO
    /// auto-trim. The row button consumes its own click so toggling never
    /// triggers SelectRecord on the parent row.
    /// </summary>
    private void ToggleRecordLock(Dictionary rec)
    {
        RunRecordManager.SetLocked(rec, !RunRecordManager.IsLocked(rec));
        RenderHistory();
    }

    private void OnRetryPressed()
    {
        GameManager.RestartGame(GetTree());
    }

    private void OnMenuPressed()
    {
        GameManager.GoToMenu(GetTree());
    }
}
