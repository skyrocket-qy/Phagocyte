using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Medical record panel. Shows the settlement of the just-finished run
/// (victory / defeat) and the persistent history of past runs.
/// </summary>
public partial class RunRecordsModal : PanelContainer
{
    [Signal]
    public delegate void ClosedEventHandler();

    public Label? TitleLabel { get; set; }
    public Button? CloseBtn { get; set; }
    public Label? BannerLabel { get; set; }
    public VBoxContainer? SummaryBox { get; set; }
    public Label? HistoryHeader { get; set; }
    public VBoxContainer? HistoryList { get; set; }
    public Button? ClearBtn { get; set; }
    public Button? RetryBtn { get; set; }
    public Button? MenuBtn { get; set; }

    public bool SettlementMode { get; private set; } = false;

    private Dictionary? _record = null;
    private bool _clearArmed = false;
    private Callable _langCallback;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        TitleLabel = GetNodeOrNull<Label>("VBox/Header/Title");
        CloseBtn = GetNodeOrNull<Button>("VBox/Header/CloseButton");
        BannerLabel = GetNodeOrNull<Label>("VBox/Banner");
        SummaryBox = GetNodeOrNull<VBoxContainer>("VBox/SummaryBox");
        HistoryHeader = GetNodeOrNull<Label>("VBox/HistoryHeader");
        HistoryList = GetNodeOrNull<VBoxContainer>("VBox/Scroll/HistoryList");
        ClearBtn = GetNodeOrNull<Button>("VBox/Buttons/ClearButton");
        RetryBtn = GetNodeOrNull<Button>("VBox/Buttons/RetryButton");
        MenuBtn = GetNodeOrNull<Button>("VBox/Buttons/MenuButton");

        if (CloseBtn != null)
            CloseBtn.Pressed += Close;
        if (ClearBtn != null)
            ClearBtn.Pressed += OnClearPressed;
        if (RetryBtn != null)
            RetryBtn.Pressed += OnRetryPressed;
        if (MenuBtn != null)
            MenuBtn.Pressed += OnMenuPressed;

        _langCallback = Callable.From((string _) => UpdateLocalizedTexts());
        GameManager.AddLanguageListener(_langCallback);

        UpdateLocalizedTexts();
    }

    public override void _ExitTree()
    {
        GameManager.RemoveLanguageListener(_langCallback);
    }

    public void OpenSettlement(Dictionary record)
    {
        _record = record;
        SettlementMode = true;
        _clearArmed = false;
        Visible = true;
        UpdateLocalizedTexts();
    }

    public void OpenHistory()
    {
        _record = null;
        SettlementMode = false;
        _clearArmed = false;
        Visible = true;
        UpdateLocalizedTexts();
    }

    public void Close()
    {
        if (SettlementMode)
            return; // settlement must resolve through Retry / Menu
        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    public void UpdateLocalizedTexts()
    {
        if (TitleLabel != null) TitleLabel.Text = Tr("RECORDS_TITLE");
        if (CloseBtn != null) CloseBtn.Text = "✕";
        if (RetryBtn != null) RetryBtn.Text = Tr("RECORDS_RETRY");
        if (MenuBtn != null) MenuBtn.Text = Tr("RECORDS_MENU");

        if (HistoryHeader != null)
        {
            HistoryHeader.Text = TextFormatter.Format(
                Tr("RECORDS_HISTORY_FMT"),
                RunRecordManager.GetRunCount(),
                RunRecordManager.GetVictoryCount(),
                RunRecordManager.FormatTime(RunRecordManager.GetBestSurvivalTime()));
        }

        if (ClearBtn != null)
        {
            _clearArmed = false;
            ClearBtn.Text = Tr("RECORDS_CLEAR");
        }

        if (CloseBtn != null) CloseBtn.Visible = !SettlementMode;
        if (ClearBtn != null) ClearBtn.Visible = !SettlementMode;
        if (RetryBtn != null) RetryBtn.Visible = SettlementMode;
        if (MenuBtn != null) MenuBtn.Visible = SettlementMode;

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

        if (!SettlementMode || _record == null)
        {
            if (BannerLabel != null)
                BannerLabel.Visible = false;
            SummaryBox.Visible = false;
            return;
        }

        if (BannerLabel != null)
        {
            BannerLabel.Visible = true;
            bool victory = _record.GetValueOrDefault("result", "").AsString() == RunRecordManager.ResultVictory;
            BannerLabel.Text = victory ? Tr("RECORDS_SETTLEMENT_VICTORY") : Tr("RECORDS_SETTLEMENT_DEFEAT");
            BannerLabel.Modulate = victory ? new Color(0.45f, 1.0f, 0.55f) : new Color(1.0f, 0.45f, 0.45f);
        }

        SummaryBox.Visible = true;
        string classId = _record.GetValueOrDefault("class_id", "").AsString();
        string mapId = _record.GetValueOrDefault("map_id", "").AsString();
        string rank = GetRank(_record);

        AddSummaryRow("RECORDS_RANK",
            string.IsNullOrEmpty(rank) ? "-" : GetRankLabel(rank),
            string.IsNullOrEmpty(rank) ? null : GetRankColor(rank));
        AddSummaryRow("RECORDS_CLASS", GetClassName(classId));
        AddSummaryRow("RECORDS_MAP", GetMapName(mapId));
        AddSummaryRow("RECORDS_DIFFICULTY", GetDifficultyName(_record.GetValueOrDefault("difficulty", RunRecordManager.DifficultyNormal).AsString()));
        AddSummaryRow("RECORDS_TIME", RunRecordManager.FormatTime(_record.GetValueOrDefault("survival_time", 0.0f).AsSingle()));
        AddSummaryRow("RECORDS_LEVEL", _record.GetValueOrDefault("level", 1).AsInt32().ToString());
        AddSummaryRow("RECORDS_KILLS", _record.GetValueOrDefault("kills", 0).AsInt32().ToString());
        AddSummaryRow("RECORDS_KPM", $"{_record.GetValueOrDefault("kpm", 0.0f).AsSingle():F1}");
        AddSummaryRow("RECORDS_DIGESTED", _record.GetValueOrDefault("digested", 0).AsInt32().ToString());
        AddSummaryRow("RECORDS_KILL_SCORE", _record.GetValueOrDefault("kill_score", 0).AsInt32().ToString());
        AddSummaryRow("RECORDS_SCORE", _record.GetValueOrDefault("score", 0).AsInt32().ToString());
        AddSummaryRow("RECORDS_POINTS", _record.GetValueOrDefault("points_spent", 0).AsInt32().ToString());
        AddSummaryRow("RECORDS_SKILLS", GetSkillNames(_record.GetValueOrDefault("active_skills", new Array<string>())));

        if (_record.TryGetValue("telemetry", out var telVal) && telVal.VariantType == Variant.Type.Dictionary)
        {
            var tel = telVal.AsGodotDictionary();
            float dmgDealt = tel.TryGetValue("total_damage_dealt", out var dd) ? dd.AsSingle() : 0.0f;
            float dmgTaken = tel.TryGetValue("total_damage_taken", out var dt) ? dt.AsSingle() : 0.0f;
            int evaded = tel.TryGetValue("evaded_count", out var ev) ? ev.AsInt32() : 0;
            int blocked = tel.TryGetValue("blocked_count", out var bl) ? bl.AsInt32() : 0;
            float lifeSteal = tel.TryGetValue("lifesteal_healed", out var ls) ? ls.AsSingle() : 0.0f;

            AddSummaryRow("RECORDS_TOTAL_DMG", $"{dmgDealt:F0} (Taken: {dmgTaken:F0})");
            AddSummaryRow("RECORDS_DEFENSE_PROCS", $"💨 {evaded} Evaded | 🛡️ {blocked} Blocked | 💚 +{lifeSteal:F0} HP");
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

        var records = RunRecordManager.GetAll();
        if (records.Count == 0)
        {
            var empty = new Label
            {
                Text = Tr("RECORDS_EMPTY"),
                Modulate = new Color(0.7f, 0.7f, 0.7f)
            };
            HistoryList.AddChild(empty);
            return;
        }

        foreach (var rec in records)
        {
            HistoryList.AddChild(CreateHistoryRow(rec));
        }
    }

    private Control CreateHistoryRow(Dictionary rec)
    {
        bool victory = rec.GetValueOrDefault("result", "").AsString() == RunRecordManager.ResultVictory;
        string classId = rec.GetValueOrDefault("class_id", "").AsString();
        string mapId = rec.GetValueOrDefault("map_id", "").AsString();

        var row = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = victory ? new Color(0.08f, 0.18f, 0.12f, 0.7f) : new Color(0.18f, 0.08f, 0.1f, 0.7f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        row.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);

        var label = new Label
        {
            Text = string.Format(
                "{0} {1}\n⏱ {2}  ·  Lv.{3}  ·  🦠 {4}  ·  {5}",
                victory ? "✅" : "☠️",
                Tr(victory ? "RECORDS_VICTORY_TAG" : "RECORDS_DEFEAT_TAG"),
                RunRecordManager.FormatTime(rec.GetValueOrDefault("survival_time", 0.0f).AsSingle()),
                rec.GetValueOrDefault("level", 1).AsInt32(),
                rec.GetValueOrDefault("digested", 0).AsInt32(),
                RunRecordManager.FormatTimestamp(rec.GetValueOrDefault("timestamp", 0.0).AsDouble())),
            VerticalAlignment = VerticalAlignment.Center
        };
        vbox.AddChild(label);

        var second = new Label
        {
            Text = $"   {GetClassName(classId)} · {GetMapName(mapId)}",
            Modulate = new Color(0.65f, 0.75f, 0.85f)
        };
        vbox.AddChild(second);

        var meta = new HBoxContainer();
        meta.AddThemeConstantOverride("separation", 8);

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
            Text = $"🎯 {kills}  ·  {kpm:F0} KPM  ·  Σ {score}  ·  {GetDifficultyName(rec.GetValueOrDefault("difficulty", RunRecordManager.DifficultyNormal).AsString())}",
            Modulate = new Color(0.6f, 0.7f, 0.8f),
            VerticalAlignment = VerticalAlignment.Center
        };
        meta.AddChild(stats);

        vbox.AddChild(meta);

        row.AddChild(vbox);
        return row;
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

    private void OnClearPressed()
    {
        if (!_clearArmed)
        {
            _clearArmed = true;
            if (ClearBtn != null) ClearBtn.Text = Tr("RECORDS_CLEAR_CONFIRM");
            return;
        }

        RunRecordManager.ClearRecords();
        UpdateLocalizedTexts();
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
