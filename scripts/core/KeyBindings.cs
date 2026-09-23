using Godot;
using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Remappable primary keys for the six gameplay actions (pause/back stays
/// fixed on ESC and is intentionally not listed here). Only user-customized
/// actions are persisted; untouched actions keep their project.godot bindings
/// verbatim (WASD + arrows, Space + joypad, C). A rebound action owns exactly
/// one primary key, so the old key stops working.
/// </summary>
public static class KeyBindings
{
    public static readonly string[] Actions =
    {
        "move_up", "move_down", "move_left", "move_right",
        "dodge", "toggle_tree",
    };

    private static readonly Dictionary<string, Key> Defaults = new()
    {
        ["move_up"] = Key.W,
        ["move_down"] = Key.S,
        ["move_left"] = Key.A,
        ["move_right"] = Key.D,
        ["dodge"] = Key.Space,
        ["toggle_tree"] = Key.C,
    };

    private static readonly Dictionary<string, Key> ArrowSecondary = new()
    {
        ["move_up"] = Key.Up,
        ["move_down"] = Key.Down,
        ["move_left"] = Key.Left,
        ["move_right"] = Key.Right,
    };

    private static readonly Dictionary<string, Key> Current = new(Defaults);

    /// <summary>Actions the player has rebound (the only ones persisted).</summary>
    private static readonly HashSet<string> Customized = new();

    public static Key GetPrimary(string action)
    {
        return Current.TryGetValue(action, out var key) ? key : Key.None;
    }

    public static Key GetDefault(string action)
    {
        return Defaults.TryGetValue(action, out var key) ? key : Key.None;
    }

    public static string KeyText(Key key)
    {
        if (key == Key.None)
            return "-";
        string label = OS.GetKeycodeString(key);
        return string.IsNullOrEmpty(label) ? key.ToString() : label;
    }

    /// <summary>
    /// Assigns a new primary key. A key owned by another action is swapped
    /// with this action's old primary, so two actions never share one.
    /// Returns false for unknown actions or Key.None.
    /// </summary>
    public static bool SetPrimary(string action, Key key)
    {
        if (key == Key.None || !Current.TryGetValue(action, out var old))
            return false;
        if (old == key)
            return true;
        foreach (var other in new List<string>(Current.Keys))
        {
            if (other != action && Current[other] == key)
                Current[other] = old;
        }
        Current[action] = key;
        Customized.Add(action);
        Apply(action);
        SettingsManager.SaveToDisk();
        return true;
    }

    public static void ResetAll()
    {
        foreach (var kv in Defaults)
            Current[kv.Key] = kv.Value;
        Customized.Clear();
        ApplyAll();
        SettingsManager.SaveToDisk();
    }

    /// <summary>Re-applies every action to the live InputMap (call after load).</summary>
    public static void ApplyAll()
    {
        foreach (var action in Actions)
            Apply(action);
    }

    private static void Apply(string action)
    {
        if (!InputMap.HasAction(action) || !Current.TryGetValue(action, out var primary))
            return;
        var keep = new List<InputEvent>();
        foreach (var ev in InputMap.ActionGetEvents(action))
        {
            if (ev is not InputEventKey)
                keep.Add(ev);
        }
        InputMap.ActionEraseEvents(action);
        foreach (var ev in keep)
            InputMap.ActionAddEvent(action, ev);
        if (Customized.Contains(action))
        {
            // Rebound: exactly one key, the old binding dies.
            AddKey(action, primary);
            return;
        }
        // Untouched: restore project defaults verbatim.
        AddKey(action, Defaults[action]);
        if (ArrowSecondary.TryGetValue(action, out var arrow))
            AddKey(action, arrow);
    }

    private static void AddKey(string action, Key key)
    {
        var ev = new InputEventKey { Keycode = key };
        InputMap.ActionAddEvent(action, ev);
    }

    public static Godot.Collections.Dictionary ToDict()
    {
        var d = new Godot.Collections.Dictionary();
        foreach (var action in Customized)
            d[action] = (int)Current[action];
        return d;
    }

    public static void FromDict(Godot.Collections.Dictionary? d)
    {
        if (d == null)
            return;
        foreach (var action in Actions)
        {
            if (d.TryGetValue(action, out var v))
            {
                int code = v.AsInt32();
                if (code > 0)
                {
                    Current[action] = (Key)code;
                    Customized.Add(action);
                }
            }
        }
        ApplyAll();
    }
}
