using Godot;
using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Pause ownership: the tree pause flag is shared by the pause menu, the
/// in-run C tree overlay, level-up drafts and run settlement. A direct
/// <c>Paused = false</c> by any one of them used to stomp the others (e.g.
/// closing a draft unpaused an open tree overlay, letting enemies hit a
/// reading player). Holds are reference-counted by token; the tree stays
/// paused until every holder releases. Scene transitions clear all holds.
/// </summary>
public static class PauseManager
{
    public const string PauseMenu = "pause_menu";
    public const string TreeOverlay = "tree_overlay";
    public const string UpgradeDraft = "upgrade_draft";
    public const string Settlement = "settlement";

    private static readonly HashSet<string> _holds = new();

    public static int HoldCount => _holds.Count;

    public static void PushHold(SceneTree tree, string token)
    {
        _holds.Add(token);
        tree.Paused = true;
    }

    public static void PopHold(SceneTree tree, string token)
    {
        _holds.Remove(token);
        if (_holds.Count == 0)
            tree.Paused = false;
    }

    /// <summary>Clears all holds (scene transitions; never call mid-run).</summary>
    public static void Clear(SceneTree tree)
    {
        _holds.Clear();
        tree.Paused = false;
    }
}
