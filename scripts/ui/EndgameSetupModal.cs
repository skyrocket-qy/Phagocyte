using Godot;
using System.Collections.Generic;
using Phagocyte.Core;
using Phagocyte.Endgame;

namespace Phagocyte.UI;

/// <summary>
/// Pre-run setup for the Endless Cytokine Storm (docs/endgame.md §4): pick any
/// combination of the six Pathological Overload Afflictions. Bonuses are
/// additive and previewed as the live settlement score multiplier.
/// </summary>
public partial class EndgameSetupModal : ModalBase
{
    public Label? HintLabel { get; set; }
    public VBoxContainer? AfflictionList { get; set; }
    public Label? TotalLabel { get; set; }
    public Button? ConfirmBtn { get; set; }
    public Button? CancelBtn { get; set; }

    private readonly Dictionary<string, CheckBox> _checks = new();
    private readonly Dictionary<string, Label> _descLabels = new();

    // Fully code-built modal: no header node paths to resolve.
    protected override string? TitleLabelPath => null;
    protected override string? CloseButtonPath => null;

    public override void _Ready()
    {
        // Static shell (FullRect + gold panel + labels + buttons) lives in
        // endgame_setup_modal.tscn; only the affliction rows are code-built.
        TitleLabel = GetNodeOrNull<Label>("Center/Box/VBox/Title");
        HintLabel = GetNodeOrNull<Label>("Center/Box/VBox/Hint");
        AfflictionList = GetNodeOrNull<VBoxContainer>("Center/Box/VBox/AfflictionList");
        TotalLabel = GetNodeOrNull<Label>("Center/Box/VBox/Total");
        CancelBtn = GetNodeOrNull<Button>("Center/Box/VBox/Buttons/Cancel");
        ConfirmBtn = GetNodeOrNull<Button>("Center/Box/VBox/Buttons/Confirm");
        if (CancelBtn != null)
            CancelBtn.Pressed += OnCancelPressed;
        if (ConfirmBtn != null)
            ConfirmBtn.Pressed += OnConfirmPressed;

        BuildRows();
        InitModal();
    }

    private void BuildRows()
    {
        if (AfflictionList == null)
            return;

        foreach (var def in AfflictionManager.Definitions)
        {
            var row = new VBoxContainer();
            row.AddThemeConstantOverride("separation", 0);

            var check = new CheckBox();
            check.Toggled += (bool pressed) =>
            {
                AfflictionManager.SetSelected(def.Id, pressed);
                UpdateTotal();
            };
            _checks[def.Id] = check;
            row.AddChild(check);

            var desc = new Label
            {
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Modulate = new Color(0.62f, 0.68f, 0.76f)
            };
            desc.AddThemeFontSizeOverride("font_size", 12);
            _descLabels[def.Id] = desc;
            row.AddChild(desc);

            AfflictionList.AddChild(row);
        }
    }

    /// <summary>Opens the setup, restoring the previous selection state.</summary>
    public void OpenSetup()
    {
        foreach (var (id, check) in _checks)
        {
            check.SetPressedNoSignal(AfflictionManager.IsActive(id));
        }

        UpdateLocalizedTexts();
        Visible = true;
    }

    public override void UpdateLocalizedTexts()
    {
        base.UpdateLocalizedTexts();
        if (TitleLabel != null) TitleLabel.Text = Tr("ENDLESS_SETUP_TITLE");
        if (HintLabel != null) HintLabel.Text = Tr("ENDLESS_SETUP_HINT");
        if (ConfirmBtn != null) ConfirmBtn.Text = Tr("BTN_ENDLESS_CONFIRM");
        if (CancelBtn != null) CancelBtn.Text = Tr("BTN_ENDLESS_CANCEL");

        foreach (var def in AfflictionManager.Definitions)
        {
            if (_checks.TryGetValue(def.Id, out var check))
                check.Text = $"{def.Icon} {Tr(def.NameKey)}  (+{def.BonusPercent}%)";
            if (_descLabels.TryGetValue(def.Id, out var desc))
                desc.Text = "    " + Tr(def.DescKey);
        }

        UpdateTotal();
    }

    private void UpdateTotal()
    {
        if (TotalLabel != null)
            TotalLabel.Text = $"{Tr("ENDLESS_SETUP_TOTAL")}  ×{AfflictionManager.ScoreMultiplier:F2}";
    }

    private void OnConfirmPressed()
    {
        Visible = false;
        // Selection is owned by the checkboxes; pass it explicitly so the launch
        // can never wipe it via a null default.
        GameManager.StartEndlessGame(GetTree(), AfflictionManager.SelectedIds);
    }

    private void OnCancelPressed()
    {
        CloseModal();
    }
}
