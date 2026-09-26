using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Meta-progression unlocks for gear chamber equipment (TODO Phase 1
/// revision): the vault starts fully locked and gear are only obtained
/// from pathogen kills at a deliberately low drop chance. Unlocks are
/// account-wide and persisted; a collected drop is the only way in.
/// </summary>
public static class GearUnlockManager
{
    /// <summary>Base chance per pathogen kill. Deliberately low.</summary>
    public const float DefaultDropChance = 0.02f;

    /// <summary>Roll chance per kill; tests override this seam.</summary>
    public static float DropChance = DefaultDropChance;

    /// <summary>Master switch so headless suites stay deterministic.</summary>
    public static bool DropsEnabled = true;

    private static readonly JsonStore.SavePathSlot _savePath = new("gear_unlocks.json");
    public static string SavePath
    {
        get => _savePath.Value;
        set => _savePath.Value = value;
    }

    private static readonly HashSet<string> Unlocked = new();
    private static readonly List<Callable> _listeners = new();
    private static bool _loaded;

    public static void EnsureLoaded()
    {
        if (_loaded)
            return;
        _loaded = true;
        LoadFromDisk();
    }

    /// <summary>True when the gear exists in the catalog and has dropped at least once.</summary>
    public static bool IsUnlocked(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;
        EnsureLoaded();
        return Unlocked.Contains(id);
    }

    public static int UnlockedCount
    {
        get
        {
            EnsureLoaded();
            return Unlocked.Count;
        }
    }

    public static int LockedCount
    {
        get
        {
            EnsureLoaded();
            return Math.Max(0, GameManager.GearCatalog.Count - Unlocked.Count);
        }
    }

    /// <summary>Unlocks a gear item. Returns true only when it was newly unlocked.</summary>
    public static bool Unlock(string id)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(id) || !GameManager.GearCatalog.ContainsKey(id))
            return false;
        if (!Unlocked.Add(id))
            return false;

        SaveToDisk();
        NotifyListeners(id);
        return true;
    }

    /// <summary>Unlocks everything (debug/tests).</summary>
    public static void UnlockAll()
    {
        EnsureLoaded();
        foreach (string id in GameManager.GearCatalog.Keys)
            Unlocked.Add(id);
        SaveToDisk();
    }

    /// <summary>Clears every unlock (tests).</summary>
    public static void ResetAll()
    {
        EnsureLoaded();
        Unlocked.Clear();
        JsonStore.Delete(SavePath);
    }

    /// <summary>A uniformly random locked gear id, or "" when all are unlocked.</summary>
    public static string RollLockedId()
    {
        EnsureLoaded();
        var locked = new List<string>();
        foreach (string id in GameManager.GearCatalog.Keys)
        {
            if (!Unlocked.Contains(id))
                locked.Add(id);
        }
        if (locked.Count == 0)
            return "";
        return locked[(int)(GD.Randi() % (uint)locked.Count)];
    }

    /// <summary>
    /// Rolls <see cref="DropChance"/> once and spawns a pickup at
    /// <paramref name="position"/> when it hits and a locked gear remains.
    /// </summary>
    public static GearDrop? TrySpawnDrop(Vector2 position, Node? parent, Node2D? target)
    {
        if (!DropsEnabled || DropChance <= 0.0f || parent == null || !GodotObject.IsInstanceValid(parent))
            return null;
        if (LockedCount == 0)
            return null;
        if (GD.Randf() >= DropChance)
            return null;

        string id = RollLockedId();
        if (string.IsNullOrEmpty(id))
            return null;

        var drop = new GearDrop
        {
            Name = "GearDrop_" + id,
            GearId = id,
            Target = target,
            GlobalPosition = position
        };
        parent.AddChild(drop);
        return drop;
    }

    // ------------------------------------------------------------------
    // Unlock notifications (mirrors AchievementManager's listener list)
    // ------------------------------------------------------------------

    public static void AddUnlockListener(Callable callback)
    {
        if (!_listeners.Contains(callback))
            _listeners.Add(callback);
    }

    public static void RemoveUnlockListener(Callable callback)
    {
        _listeners.Remove(callback);
    }

    private static void NotifyListeners(string id)
    {
        if (!GameManager.GearCatalog.TryGetValue(id, out var entryVar))
            return;
        var entry = entryVar.AsGodotDictionary();
        foreach (Callable cb in _listeners)
        {
            if (cb.Target is null || GodotObject.IsInstanceValid(cb.Target as GodotObject))
                cb.Call(id, entry);
        }
    }

    // ------------------------------------------------------------------
    // Persistence
    // ------------------------------------------------------------------

    public static void SaveToDisk()
    {
        var ids = new Array<string>();
        foreach (string id in Unlocked)
            ids.Add(id);
        JsonStore.Write(SavePath, new Dictionary { { "unlocked", ids } });
    }

    public static void LoadFromDisk()
    {
        _loaded = true;
        Unlocked.Clear();

        var data = JsonStore.Read(SavePath);
        if (data == null)
            return;

        if (data.TryGetValue("unlocked", out var unlockedVal) && unlockedVal.VariantType == Variant.Type.Array)
        {
            foreach (var idVar in unlockedVal.AsGodotArray())
            {
                string id = idVar.AsString();
                if (!string.IsNullOrEmpty(id) && GameManager.GearCatalog.ContainsKey(id))
                    Unlocked.Add(id);
            }
        }
    }

    /// <summary>Test / reset hook: forget in-memory state so the next call reloads.</summary>
    public static void ResetCache()
    {
        _loaded = false;
        Unlocked.Clear();
    }
}
