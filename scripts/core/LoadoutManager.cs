using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Persistent pre-run gear loadouts (TODO Phase 1). Each cell keeps up to
/// <see cref="MaxProfiles"/> named builds of four chamber slots. A fresh cell
/// deploys with <b>no equipment at all</b>: the default profile is four empty
/// slots. Legality is delegated to <see cref="GearChamber.ValidateSlots"/>
/// so the page, the apply step and the runtime chamber share one contract.
/// </summary>
public static class LoadoutManager
{
    /// <summary>Maximum build profiles per cell (mirrors the passive tree).</summary>
    public const int MaxProfiles = 3;

    /// <summary>Chamber slot count (2x2).</summary>
    public const int SlotCount = GearChamber.MaxSlots;

    private static readonly JsonStore.SavePathSlot _savePath = new("gear_loadouts.json");
    public static string SavePath
    {
        get => _savePath.Value;
        set => _savePath.Value = value;
    }

    /// <summary>cellId → ordered profiles; each profile is exactly SlotCount ids ("" = empty).</summary>
    private static readonly System.Collections.Generic.Dictionary<string, List<string[]>> Profiles = new();

    /// <summary>cellId → index of the profile currently edited and deployed.</summary>
    private static readonly System.Collections.Generic.Dictionary<string, int> ActiveProfiles = new();

    private static bool _loaded;

    public static void EnsureLoaded()
    {
        if (_loaded)
            return;
        _loaded = true;
        LoadFromDisk();
    }

    public static bool IsKnownCell(string cellId)
    {
        return !string.IsNullOrEmpty(cellId) && GameManager.ClassData.ContainsKey(cellId);
    }

    /// <summary>An all-empty slot row — the default "no equipment" deployment.</summary>
    public static string[] EmptySlots()
    {
        var row = new string[SlotCount];
        for (int i = 0; i < SlotCount; i++)
            row[i] = "";
        return row;
    }

    /// <summary>Profile rows for a cell, guaranteeing at least one (default empty) profile.</summary>
    private static List<string[]> GetProfileList(string cellId)
    {
        if (!Profiles.TryGetValue(cellId, out var list) || list.Count == 0)
        {
            list = new List<string[]> { EmptySlots() };
            Profiles[cellId] = list;
        }
        return list;
    }

    public static int GetProfileCount(string cellId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return 0;
        return GetProfileList(cellId).Count;
    }

    public static int GetActiveProfile(string cellId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return 0;
        var list = GetProfileList(cellId);
        if (!ActiveProfiles.TryGetValue(cellId, out int active) || active < 0 || active >= list.Count)
        {
            active = 0;
            ActiveProfiles[cellId] = 0;
        }
        return active;
    }

    public static bool SetActiveProfile(string cellId, int index)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return false;
        var list = GetProfileList(cellId);
        if (index < 0 || index >= list.Count)
            return false;
        if (!ActiveProfiles.TryGetValue(cellId, out int current) || current != index)
        {
            ActiveProfiles[cellId] = index;
            SaveToDisk();
        }
        return true;
    }

    /// <summary>Append an empty profile and switch to it. Refused at <see cref="MaxProfiles"/>.</summary>
    public static bool AddProfile(string cellId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return false;
        var list = GetProfileList(cellId);
        if (list.Count >= MaxProfiles)
            return false;
        list.Add(EmptySlots());
        ActiveProfiles[cellId] = list.Count - 1;
        SaveToDisk();
        return true;
    }

    /// <summary>Delete one profile; the last remaining profile is kept.</summary>
    public static bool DeleteProfile(string cellId, int index)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return false;
        var list = GetProfileList(cellId);
        if (list.Count <= 1 || index < 0 || index >= list.Count)
            return false;
        int active = GetActiveProfile(cellId);
        list.RemoveAt(index);
        if (active == index)
            active = Math.Max(0, index - 1);
        else if (active > index)
            active -= 1;
        ActiveProfiles[cellId] = active;
        SaveToDisk();
        return true;
    }

    /// <summary>Display name for a profile slot (配置一/二/三), shared with the talent tree.</summary>
    public static string GetProfileName(int index)
    {
        return TranslationServer.Translate("TREE_PROFILE_" + (index + 1));
    }

    /// <summary>
    /// Slot ids of one profile (copied). Falls back to the active profile for an
    /// out-of-range index and to four empty slots for an unknown cell.
    /// </summary>
    public static string[] GetSlots(string cellId, int profileIndex)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return EmptySlots();
        var list = GetProfileList(cellId);
        if (profileIndex < 0 || profileIndex >= list.Count)
            profileIndex = GetActiveProfile(cellId);
        return (string[])list[profileIndex].Clone();
    }

    /// <summary>Active profile's slot ids (what a run deploys).</summary>
    public static string[] GetActiveSlots(string cellId)
    {
        return GetSlots(cellId, GetActiveProfile(cellId));
    }

    /// <summary>
    /// Replaces one profile. Refused when the set breaks the chamber contract
    /// (overload / generator cap / duplicate copies / unknown ids) or references
    /// a gear item that has not dropped yet.
    /// </summary>
    public static bool SetSlots(string cellId, int profileIndex, IReadOnlyList<string> slots)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return false;
        if (!GearChamber.ValidateSlots(slots, out _))
            return false;
        foreach (string id in slots)
        {
            if (!string.IsNullOrEmpty(id) && !GearUnlockManager.IsUnlocked(id))
                return false;
        }
        var list = GetProfileList(cellId);
        if (profileIndex < 0 || profileIndex >= list.Count)
            return false;

        var row = EmptySlots();
        for (int i = 0; i < SlotCount && i < slots.Count; i++)
            row[i] = slots[i] ?? "";
        list[profileIndex] = row;
        SaveToDisk();
        return true;
    }

    public static void SaveToDisk()
    {
        var profilesOut = new Dictionary();
        var activesOut = new Dictionary();
        foreach (var pair in Profiles)
        {
            bool anyNonEmpty = false;
            var rowsOut = new Godot.Collections.Array();
            foreach (string[] row in pair.Value)
            {
                var rowOut = new Array<string>();
                foreach (string id in row)
                {
                    string value = id ?? "";
                    rowOut.Add(value);
                    if (!string.IsNullOrEmpty(value))
                        anyNonEmpty = true;
                }
                rowsOut.Add(rowOut);
            }
            if (!anyNonEmpty)
                continue;

            profilesOut[pair.Key] = rowsOut;
            activesOut[pair.Key] = GetActiveProfile(pair.Key);
        }

        JsonStore.Write(SavePath, new Dictionary
        {
            { "profiles", profilesOut },
            { "active_profiles", activesOut }
        });
    }

    public static void LoadFromDisk()
    {
        _loaded = true;
        Profiles.Clear();
        ActiveProfiles.Clear();

        var data = JsonStore.Read(SavePath);
        if (data == null)
            return;

        if (data.TryGetValue("profiles", out var profilesVal) && profilesVal.VariantType == Variant.Type.Dictionary)
        {
            var profiles = profilesVal.AsGodotDictionary();
            foreach (var key in profiles.Keys)
            {
                string cellId = key.AsString();
                if (!IsKnownCell(cellId))
                    continue;

                var raw = profiles[key];
                if (raw.VariantType != Variant.Type.Array)
                    continue;

                var list = new List<string[]>();
                foreach (var rowVar in raw.AsGodotArray())
                {
                    if (list.Count >= MaxProfiles)
                        break;
                    if (rowVar.VariantType != Variant.Type.Array)
                        continue;
                    list.Add(ParseRow(rowVar.AsGodotArray()));
                }
                if (list.Count == 0)
                    list.Add(EmptySlots());
                Profiles[cellId] = list;
            }
        }

        if (data.TryGetValue("active_profiles", out var activeVal) && activeVal.VariantType == Variant.Type.Dictionary)
        {
            var actives = activeVal.AsGodotDictionary();
            foreach (var key in actives.Keys)
            {
                string cellId = key.AsString();
                if (!IsKnownCell(cellId) || !Profiles.TryGetValue(cellId, out var list))
                    continue;
                ActiveProfiles[cellId] = Mathf.Clamp(actives[key].AsInt32(), 0, list.Count - 1);
            }
        }
    }

    /// <summary>
    /// Sanitizes one persisted row: unknown or still-locked ids are dropped and
    /// an illegal set (hand-edited file) falls back to empty rather than
    /// blocking deployment.
    /// </summary>
    private static string[] ParseRow(Godot.Collections.Array raw)
    {
        var row = EmptySlots();
        for (int i = 0; i < SlotCount && i < raw.Count; i++)
        {
            string id = raw[i].AsString();
            if (!string.IsNullOrEmpty(id) && GameManager.GearCatalog.ContainsKey(id)
                && GearUnlockManager.IsUnlocked(id))
            {
                row[i] = id;
            }
        }

        if (!GearChamber.ValidateSlots(row, out string reason))
        {
            GD.PushWarning($"[Loadout] Illegal saved profile sanitized to empty (reason: {reason}).");
            return EmptySlots();
        }
        return row;
    }

    /// <summary>Test / reset hook: forget in-memory state so the next call reloads.</summary>
    public static void ResetCache()
    {
        _loaded = false;
        Profiles.Clear();
        ActiveProfiles.Clear();
    }
}
