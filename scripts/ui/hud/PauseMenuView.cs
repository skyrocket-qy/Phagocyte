using Godot;
using Godot.Collections;
using System.Text;
using Phagocyte.Core;
using Phagocyte.Player;

namespace Phagocyte.UI;

/// <summary>
/// Pause menu + codex/settings modals + passive-tree overlay (former <c>Hud</c>
/// pause block). Owns its nodes and the programmatic tree overlay; input is
/// offered as <see cref="HandleInput"/> for the coordinator's <c>_Input</c>.
/// </summary>
public partial class PauseMenuView : Node
{
    public PanelContainer? PauseModal { get; set; }
    public Label? PauseTitle { get; set; }
    public Button? ResumeBtn { get; set; }
    public Button? SettingsBtn { get; set; }
    public Button? ManualBtn { get; set; }
    public Button? RestartBtn { get; set; }
    public Button? MenuBtn { get; set; }

    public CodexModal? CellCodexModal { get; set; }
    public SettingsModal? CellSettingsModal { get; set; }

    public PanelContainer? TreeOverlayPanel { get; set; }
    public RichTextLabel? TreeOverlayText { get; set; }

    // Blocks tree/pause hotkeys once the settlement screen is up.
    public bool PauseInputSuppressed { get; set; } = false;

    public Node2D? PlayerRef { get; set; }
    public int LastLevel { get; set; } = 1;

    public bool IsTreeOverlayVisible => TreeOverlayPanel?.Visible ?? false;

    /// <summary>Wires pause nodes, buttons and the tree overlay. Call once from Hud._Ready.</summary>
    public void Bind(Node root)
    {
        PauseModal = root.GetNodeOrNull<PanelContainer>("PauseModal");
        PauseTitle = root.GetNodeOrNull<Label>("PauseModal/VBox/Title");
        ResumeBtn = root.GetNodeOrNull<Button>("PauseModal/VBox/ResumeButton");
        SettingsBtn = root.GetNodeOrNull<Button>("PauseModal/VBox/SettingsButton");
        ManualBtn = root.GetNodeOrNull<Button>("PauseModal/VBox/ManualButton");
        RestartBtn = root.GetNodeOrNull<Button>("PauseModal/VBox/RestartButton");
        MenuBtn = root.GetNodeOrNull<Button>("PauseModal/VBox/MenuButton");

        CellCodexModal = root.GetNodeOrNull<CodexModal>("CodexModal");
        CellSettingsModal = root.GetNodeOrNull<SettingsModal>("SettingsModal");

        if (PauseModal != null) PauseModal.Visible = false;

        if (ResumeBtn != null) ResumeBtn.Pressed += ResumeGame;
        if (SettingsBtn != null) SettingsBtn.Pressed += OnSettingsPressed;
        if (ManualBtn != null) ManualBtn.Pressed += OnManualPressed;
        if (RestartBtn != null) RestartBtn.Pressed += OnRestartPressed;
        if (MenuBtn != null) MenuBtn.Pressed += OnMenuPressed;

        SetupTreeOverlay(root);
    }

    public void UpdateLocalizedTexts()
    {
        RefreshTreeOverlay();

        if (PauseTitle != null) PauseTitle.Text = Tr("PAUSE_TITLE");
        if (ResumeBtn != null) ResumeBtn.Text = Tr("PAUSE_RESUME");
        if (SettingsBtn != null) SettingsBtn.Text = Tr("BTN_SETTINGS");
        if (ManualBtn != null) ManualBtn.Text = Tr("PAUSE_MANUAL");
        if (RestartBtn != null) RestartBtn.Text = Tr("PAUSE_RESTART");
        if (MenuBtn != null) MenuBtn.Text = Tr("PAUSE_MENU");
    }

    public void HandleInput(InputEvent @event)
    {
        if (PauseInputSuppressed)
            return;

        if (@event.IsActionPressed("toggle_tree"))
        {
            ToggleTreeOverlay();
            return;
        }

        if (@event.IsActionPressed("toggle_pause") || (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape))
        {
            // The C tree overlay behaves like any modal: ESC backs out of it
            // first (restoring unpaused play) before touching pause.
            if (TreeOverlayPanel != null && TreeOverlayPanel.Visible)
            {
                ToggleTreeOverlay();
                return;
            }
            if (CellSettingsModal != null && CellSettingsModal.Visible)
            {
                CellSettingsModal.CloseSettings();
                return;
            }
            if (CellCodexModal != null && CellCodexModal.Visible)
            {
                CellCodexModal.CloseCodex();
                return;
            }
            TogglePause();
        }
    }

    public void TogglePause()
    {
        var tree = GetTree();
        if (PauseModal != null && PauseModal.Visible)
        {
            PauseModal.Visible = false;
            PauseManager.PopHold(tree, PauseManager.PauseMenu);
        }
        else
        {
            if (PauseModal != null) PauseModal.Visible = true;
            PauseManager.PushHold(tree, PauseManager.PauseMenu);
        }
        if (PauseManager.HoldCount == 0)
        {
            if (CellCodexModal != null) CellCodexModal.Visible = false;
            if (CellSettingsModal != null) CellSettingsModal.Visible = false;
        }
    }

    public void ResumeGame()
    {
        var tree = GetTree();
        if (PauseModal != null) PauseModal.Visible = false;
        if (TreeOverlayPanel != null) TreeOverlayPanel.Visible = false;
        if (CellCodexModal != null) CellCodexModal.Visible = false;
        if (CellSettingsModal != null) CellSettingsModal.Visible = false;
        PauseManager.PopHold(tree, PauseManager.PauseMenu);
        PauseManager.PopHold(tree, PauseManager.TreeOverlay);
    }

    public void OnRestartPressed()
    {
        GameManager.RestartGame(GetTree());
    }

    public void OnMenuPressed()
    {
        GameManager.GoToMenu(GetTree());
    }

    public void OnManualPressed()
    {
        CellCodexModal?.OpenCodex(0);
    }

    public void OnSettingsPressed()
    {
        CellSettingsModal?.OpenSettings(0);
    }

    /// <summary>
    /// In-run C overlay: opening it fully pauses the run so the build can be
    /// read safely; closing restores play unless the pause menu owns the
    /// pause. ESC backs out through <see cref="HandleInput"/>.
    /// </summary>
    public void ToggleTreeOverlay()
    {
        if (TreeOverlayPanel == null)
            return;

        bool show = !TreeOverlayPanel.Visible;
        if (show)
            RefreshTreeOverlay();
        TreeOverlayPanel.Visible = show;
        if (show)
            PauseManager.PushHold(GetTree(), PauseManager.TreeOverlay);
        else
            PauseManager.PopHold(GetTree(), PauseManager.TreeOverlay);
    }

    public void RefreshTreeOverlay()
    {
        if (TreeOverlayPanel == null || TreeOverlayText == null)
            return;

        string classId = GameManager.SelectedClass;
        var classInfo = GameManager.GetClassInfo(classId);
        string className = classInfo.TryGetValue("name", out var nameVal) ? nameVal.AsString() : classId;
        var allocation = PassiveTreeManager.GetAllocation(classId);
        var text = new StringBuilder();

        text.AppendLine("[b]" + Tr("TREE_OVERLAY_TITLE") + "[/b]");
        text.AppendLine(className);
        text.AppendLine(TextFormatter.Format(Tr("TREE_OVERLAY_PROFILE"),
            PassiveTreeManager.GetProfileName(PassiveTreeManager.GetActiveProfile(classId))));
        text.AppendLine(TextFormatter.Format(Tr("TREE_OVERLAY_LEVEL"), PassiveTreeManager.GetCellLevel(classId), LastLevel));
        text.AppendLine(TextFormatter.Format(Tr("TREE_POINTS"), PassiveTreeManager.GetPointsAvailable(classId), PassiveTreeManager.GetSpentPoints(classId)));
        text.AppendLine();

        if (allocation.Count == 0)
        {
            text.AppendLine(Tr("TREE_OVERLAY_EMPTY"));
        }
        else
        {
            foreach (var node in PassiveTreeManager.Nodes)
            {
                if (!allocation.Contains(node.Id))
                    continue;

                string nodeName = PassiveTreeManager.GetNodeName(node.Id);
                text.AppendLine("[img=32x32]" + AssetPaths.TraitIcon(node.TraitId) + "[/img] " + nodeName);
                string description = PassiveTreeManager.GetNodeDescription(node.Id);
                if (!string.IsNullOrEmpty(description))
                {
                    foreach (string line in description.Split("\n"))
                        text.AppendLine("  " + line);
                }
            }
        }

        text.AppendLine();
        if (PlayerRef is BaseCell bc && bc.Stats != null)
        {
            AppendStatGroup(text, "STATS_GROUP_COMBAT", BuildStatsPreview.CombatKeys, bc);
            AppendStatGroup(text, "STATS_GROUP_DEFENSE", BuildStatsPreview.DefenseKeys, bc);
            AppendStatGroup(text, "STATS_GROUP_UTILITY", BuildStatsPreview.UtilityKeys, bc);
        }
        else
        {
            text.AppendLine(Tr("TREE_OVERLAY_NO_PLAYER"));
        }
        text.AppendLine(Tr("TREE_OVERLAY_HINT"));

        TreeOverlayText.Text = text.ToString();
    }

    private void AppendStatGroup(StringBuilder text, string titleKey, string[] statKeys, BaseCell bc)
    {
        text.AppendLine("[b]◆ " + Tr(titleKey) + "[/b]");
        foreach (string statKey in statKeys)
            text.AppendLine(PassiveTreeManager.GetStatLabel(statKey) + ": "
                + BuildStatsPreview.FormatValue(statKey, bc.Stats!.GetStat(statKey)));
    }

    private void SetupTreeOverlay(Node root)
    {
        // Overlay shell lives in hud.tscn (after SettingsModal); only the text is dynamic.
        TreeOverlayPanel = root.GetNodeOrNull<PanelContainer>("TreeOverlayPanel");
        TreeOverlayText = root.GetNodeOrNull<RichTextLabel>("TreeOverlayPanel/TreeOverlayText");
        if (TreeOverlayPanel == null || TreeOverlayText == null)
            return;
        RefreshTreeOverlay();
    }
}
