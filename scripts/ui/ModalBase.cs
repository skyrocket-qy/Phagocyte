using Godot;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Shared scaffolding for the panel modals: hidden-on-ready state, the header
/// ✕ button, the language-listener lifetime and the Closed signal.
/// Subclasses resolve their own nodes, then call <see cref="InitModal"/> at the
/// end of _Ready. Control (not PanelContainer) so both scene-rooted panel
/// modals and code-built Control modals can extend it.
/// </summary>
public partial class ModalBase : Control
{
    [Signal]
    public delegate void ClosedEventHandler();

    /// <summary>Header title node path; null when the modal builds its own title.</summary>
    protected virtual string? TitleLabelPath => "VBox/Header/Title";

    /// <summary>Header close button node path; null when the modal has no ✕ button.</summary>
    protected virtual string? CloseButtonPath => "VBox/Header/CloseButton";

    public Label? TitleLabel { get; protected set; }
    public Button? CloseBtn { get; protected set; }

    private Callable _langCallback;

    /// <summary>
    /// Applies the shared modal state, hooks the close button and language
    /// listener, then refreshes the localized text. Call at the end of _Ready.
    /// </summary>
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
    /// The close affordance is a shared top-left "‹ 返回" text button
    /// (NAV_BACK) on every modal header; subclasses must not override it.
    /// </remarks>
    public virtual void UpdateLocalizedTexts()
    {
        if (CloseBtn != null)
            CloseBtn.Text = Tr("NAV_BACK");
    }
}
