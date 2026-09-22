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

        SetupTreeOverlay();
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

        if (@event.IsActionPressed("toggle_tree") || (@event is InputEventKey treeKey && treeKey.Pressed && treeKey.Keycode == Key.C))
        {
            ToggleTreeOverlay();
            return;
        }

        if (@event.IsActionPressed("toggle_pause") || (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape))
        {
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
        bool paused = !GetTree().Paused;
        GetTree().Paused = paused;
        if (PauseModal != null) PauseModal.Visible = paused;
        if (!paused)
        {
            if (CellCodexModal != null) CellCodexModal.Visible = false;
            if (CellSettingsModal != null) CellSettingsModal.Visible = false;
        }
    }

    public void ResumeGame()
    {
        GetTree().Paused = false;
        if (PauseModal != null) PauseModal.Visible = false;
        if (CellCodexModal != null) CellCodexModal.Visible = false;
        if (CellSettingsModal != null) CellSettingsModal.Visible = false;
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

    public void ToggleTreeOverlay()
    {
        if (TreeOverlayPanel == null)
            return;

        bool show = !TreeOverlayPanel.Visible;
        if (show)
            RefreshTreeOverlay();
        TreeOverlayPanel.Visible = show;
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
                if (!allocation.TryGetValue(node.Id, out int stacks))
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
        text.AppendLine("[b]" + Tr("TREE_OVERLAY_STATS") + "[/b]");
        if (PlayerRef is BaseCell bc && bc.Stats != null)
        {
            string[] statKeys =
            {
                "might", "area", "cooldown_reduction", "projectile_speed", "duration", "amount",
                "pierce", "knockback", "crit_chance", "crit_damage", "ailment_damage",
                "max_health", "health_regen", "armor", "move_speed", "evasion", "block", "life_steal",
                "magnet"
            };
            foreach (string statKey in statKeys)
                text.AppendLine(PassiveTreeManager.GetStatLabel(statKey) + ": " + FormatTreeStat(statKey, bc.Stats.GetStat(statKey)));
        }
        else
        {
            text.AppendLine(Tr("TREE_OVERLAY_NO_PLAYER"));
        }
        text.AppendLine();
        text.AppendLine(Tr("TREE_OVERLAY_HINT"));

        TreeOverlayText.Text = text.ToString();
    }

    private static string FormatTreeStat(string statKey, float value)
    {
        return statKey switch
        {
            "max_health" or "health_regen" or "move_speed" or "magnet" => $"{value:F1}",
            "amount" or "pierce" or "armor" => $"{value:F0}",
            "cooldown_reduction" or "crit_chance" or "evasion" or "block" or "life_steal" => $"{value * 100.0f:F1}%",
            "might" or "area" or "projectile_speed" or "duration" or "knockback" or "crit_damage" or "ailment_damage" => $"{value * 100.0f:F0}%",
            _ => $"{value:F2}"
        };
    }

    private void SetupTreeOverlay()
    {
        TreeOverlayPanel = new PanelContainer
        {
            Name = "TreeOverlayPanel",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(400, 480),
            AnchorLeft = 1.0f,
            AnchorRight = 1.0f,
            AnchorTop = 0.0f,
            AnchorBottom = 0.0f,
            OffsetLeft = -420.0f,
            OffsetRight = -20.0f,
            OffsetTop = 80.0f,
            OffsetBottom = 620.0f
        };

        var style = UiBuilders.PanelStyle(
            new Color(0.05f, 0.08f, 0.13f, 0.95f),
            border: new Color(0.45f, 0.85f, 0.65f, 0.9f),
            borderWidth: 2, cornerRadius: 10, marginH: 16, marginV: 12);
        TreeOverlayPanel.AddThemeStyleboxOverride("panel", style);

        TreeOverlayText = new RichTextLabel
        {
            Name = "TreeOverlayText",
            BbcodeEnabled = true,
            ScrollActive = true,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        TreeOverlayText.AddThemeFontSizeOverride("normal_font_size", 13);
        TreeOverlayText.AddThemeFontSizeOverride("bold_font_size", 14);
        TreeOverlayPanel.AddChild(TreeOverlayText);
        AddChild(TreeOverlayPanel);
        RefreshTreeOverlay();
    }
}
