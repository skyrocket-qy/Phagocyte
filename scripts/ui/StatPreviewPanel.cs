using Godot;
using System.Collections.Generic;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Fixed build-stats side panel (loadout + tree pages): final values for all
/// 19 stats, grouped combat / defense / utility, refreshed live on every
/// build change. Rows are built once from <see cref="BuildStatsPreview"/> key
/// lists (same shell-plus-loop pattern as the backpack grid); values come
/// from <see cref="BuildStatsPreview.PreviewStats"/> on each refresh.
/// </summary>
public partial class StatPreviewPanel : PanelContainer
{
    private VBoxContainer? _groupsBox;
    private Label? _headerLabel;
    private readonly Dictionary<string, Label> _valueLabels = new();
    private string _classKey = "";

    public override void _Ready()
    {
        _groupsBox = GetNodeOrNull<VBoxContainer>("Margin/VBox/StatScroll/GroupsBox");
        _headerLabel = GetNodeOrNull<Label>("Margin/VBox/HeaderLabel");
        BuildRows();
        UpdateHeader();
    }

    /// <summary>Recomputes and redraws totals for <paramref name="classKey"/>.</summary>
    public void Refresh(string classKey)
    {
        _classKey = string.IsNullOrEmpty(classKey) ? "macrophage" : classKey;
        if (_valueLabels.Count == 0)
            BuildRows();
        var totals = BuildStatsPreview.PreviewStats(_classKey);
        foreach (var pair in _valueLabels)
        {
            if (totals.TryGetValue(pair.Key, out float value))
                pair.Value.Text = BuildStatsPreview.FormatValue(pair.Key, value);
        }
    }

    public void UpdateLocalizedTexts()
    {
        UpdateHeader();
        // Stat names resolve through the localized label table on next refresh.
        if (!string.IsNullOrEmpty(_classKey))
            Refresh(_classKey);
    }

    private void UpdateHeader()
    {
        if (_headerLabel != null)
            _headerLabel.Text = Tr("TREE_OVERLAY_STATS");
    }

    private void BuildRows()
    {
        if (_groupsBox == null || _valueLabels.Count > 0)
            return;
        AddGroup("◆");
        AddRows(BuildStatsPreview.CombatKeys);
        AddGroup("◆");
        AddRows(BuildStatsPreview.DefenseKeys);
        AddGroup("◆");
        AddRows(BuildStatsPreview.UtilityKeys);
        RefreshGroupTitles();
    }

    private void AddGroup(string marker)
    {
        var header = new Label { Name = $"Group_{_groupsBox!.GetChildCount()}" };
        header.AddThemeFontSizeOverride("font_size", 12);
        header.AddThemeColorOverride("font_color", new Color(0.45f, 0.85f, 0.95f, 1.0f));
        header.SetMeta("marker", marker);
        _groupsBox.AddChild(header);
    }

    private void AddRows(string[] keys)
    {
        foreach (string key in keys)
        {
            var row = new HBoxContainer { Name = $"Row_{key}" };
            row.AddThemeConstantOverride("separation", 8);
            var name = new Label
            {
                Name = "Name",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = PassiveTreeManager.GetStatLabel(key),
            };
            name.AddThemeFontSizeOverride("font_size", 12);
            name.AddThemeColorOverride("font_color", new Color(0.75f, 0.85f, 0.92f, 1.0f));
            var value = new Label
            {
                Name = "Value",
                HorizontalAlignment = HorizontalAlignment.Right,
                Text = "-",
            };
            value.AddThemeFontSizeOverride("font_size", 12);
            value.AddThemeColorOverride("font_color", new Color(0.95f, 0.98f, 1.0f, 1.0f));
            row.AddChild(name);
            row.AddChild(value);
            _groupsBox!.AddChild(row);
            _valueLabels[key] = value;
        }
    }

    private void RefreshGroupTitles()
    {
        if (_groupsBox == null)
            return;
        string[] titles = { Tr("STATS_GROUP_COMBAT"), Tr("STATS_GROUP_DEFENSE"), Tr("STATS_GROUP_UTILITY") };
        int group = 0;
        foreach (var child in _groupsBox.GetChildren())
        {
            if (child is Label label && label.HasMeta("marker") && group < titles.Length)
                label.Text = "◆ " + titles[group++];
        }
    }
}
