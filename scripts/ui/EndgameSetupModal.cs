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
        ZIndex = 70;
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        AddThemeStyleboxOverride("panel", UiBuilders.PanelStyle(
            new Color(0.02f, 0.03f, 0.06f, 0.82f)));

        BuildLayout();
        InitModal();
    }

    private void BuildLayout()
    {
        var center = new CenterContainer();
        AddChild(center);

        var box = new PanelContainer { CustomMinimumSize = new Vector2(720, 0) };
        box.AddThemeStyleboxOverride("panel", UiBuilders.PanelStyle(
            new Color(0.05f, 0.05f, 0.09f, 0.98f),
            border: new Color(1.0f, 0.72f, 0.25f, 0.9f),
            borderWidth: 2, cornerRadius: 10, marginH: 24, marginV: 20));
        center.AddChild(box);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        box.AddChild(vbox);

        TitleLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(1.0f, 0.78f, 0.35f)
        };
        TitleLabel.AddThemeFontSizeOverride("font_size", 22);
        vbox.AddChild(TitleLabel);

        HintLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = new Color(0.72f, 0.78f, 0.85f)
        };
        HintLabel.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(HintLabel);

        vbox.AddChild(new HSeparator());

        AfflictionList = new VBoxContainer();
        AfflictionList.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(AfflictionList);

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

        vbox.AddChild(new HSeparator());

        TotalLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(1.0f, 0.85f, 0.45f)
        };
        TotalLabel.AddThemeFontSizeOverride("font_size", 16);
        vbox.AddChild(TotalLabel);

        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 16);

        CancelBtn = new Button { CustomMinimumSize = new Vector2(180, 40) };
        CancelBtn.Pressed += OnCancelPressed;
        buttons.AddChild(CancelBtn);

        ConfirmBtn = new Button { CustomMinimumSize = new Vector2(230, 40) };
        ConfirmBtn.AddThemeColorOverride("font_color", new Color(1.0f, 0.78f, 0.35f));
        ConfirmBtn.Pressed += OnConfirmPressed;
        buttons.AddChild(ConfirmBtn);

        vbox.AddChild(buttons);
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
