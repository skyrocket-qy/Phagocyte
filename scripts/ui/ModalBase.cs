using Godot;
using Game.Core;

namespace Game.UI;

/// <summary>Base class for panel modals with header close handling and localization.</summary>
public partial class ModalBase : Control
{
    [Signal]
    public delegate void ClosedEventHandler();

    protected virtual string? TitleLabelPath => "VBox/Header/Title";
    protected virtual string? CloseButtonPath => "VBox/Header/CloseButton";

    public Label? TitleLabel { get; protected set; }
    public Button? CloseBtn { get; protected set; }

    private Callable _langCallback;

    protected void InitModal()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        if (TitleLabelPath is { } titlePath)
            TitleLabel = GetNodeOrNull<Label>(titlePath);

        if (CloseButtonPath is { } closePath)
        {
            CloseBtn = GetNodeOrNull<Button>(closePath);
            if (CloseBtn != null)
                CloseBtn.Pressed += CloseModal;
        }

        _langCallback = Callable.From((string _) => UpdateLocalizedTexts());
        GameManager.AddLanguageListener(_langCallback);

        UpdateLocalizedTexts();
    }

    public override void _ExitTree()
    {
        GameManager.RemoveLanguageListener(_langCallback);
        base._ExitTree();
    }

    /// <summary>Hides the modal and notifies listeners.</summary>
    public virtual void CloseModal()
    {
        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    /// <summary>Refreshes every translated label; subclasses extend this.</summary>
    /// <remarks>
    /// The close affordance is a shared top-left text button
    /// (NAV_BACK) on every modal header; subclasses must not override it.
    /// </remarks>
    public virtual void UpdateLocalizedTexts()
    {
        if (CloseBtn != null)
            CloseBtn.Text = Tr("NAV_BACK");
    }
}
