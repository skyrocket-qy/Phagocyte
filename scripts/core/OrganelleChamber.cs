using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Organelle Chamber — the 2x2 equipment system (TODO Phase 0).
/// Four 1x1 slots under a base 6-point energy budget. Generators
/// (<c>energy_cost == -1</c>, +1 energy each) raise the cap but are limited to
/// <see cref="MaxGenerators"/> and must carry a drawback. Owned-but-unequipped
/// organelles live in the run-scoped backpack. Every effect is applied through
/// the universal <see cref="CellStats"/> pool (modifiers + drawback alike).
/// Energy model (used/max): used = sum of positive costs, max = base + generators.
/// </summary>
public partial class OrganelleChamber : Node2D
{
    [Signal]
    public delegate void ChamberChangedEventHandler();

    public const int MaxSlots = 4;
    public const int BaseEnergy = 6;
    public const int MaxGenerators = 2;
    public const int BackpackCap = 24;

    private static readonly string[] ModifierLists = { "modifiers", "drawback" };

    private readonly string[] _slots = new string[MaxSlots];
    private readonly List<string> _backpack = new();

    public CharacterBody2D? Host { get; private set; }
    public CellStats? Stats { get; private set; }

    public OrganelleChamber()
    {
        for (int i = 0; i < MaxSlots; i++)
            _slots[i] = "";
    }

    /// <summary>Binds the chamber to a host cell and resolves its CellStats pool.</summary>
    public void Setup(CharacterBody2D pHost)
    {
        Host = pHost;
        if (pHost == null)
            return;

        if (pHost.HasNode("CellStats"))
        {
            Stats = pHost.GetNode<CellStats>("CellStats");
        }
        else
        {
            var statsProp = pHost.Get("stats");
            if (statsProp.VariantType == Variant.Type.Object && statsProp.AsGodotObject() is CellStats cs)
                Stats = cs;
        }
    }

    // ------------------------------------------------------------------
    // Read-only state
    // ------------------------------------------------------------------

    public string GetSlot(int slot)
    {
        return slot >= 0 && slot < MaxSlots ? _slots[slot] : "";
    }

    public IReadOnlyList<string> Backpack => _backpack;

    public int EquippedCount
    {
        get
        {
            int n = 0;
            foreach (string id in _slots)
            {
                if (!string.IsNullOrEmpty(id))
                    n++;
            }
            return n;
        }
    }

    public int GeneratorCount
    {
        get
        {
            int n = 0;
            foreach (string id in _slots)
            {
                if (EnergyCostOf(id) < 0)
                    n++;
            }
            return n;
        }
    }

    /// <summary>Sum of positive costs across equipped slots (backpack never counts).</summary>
    public int UsedEnergy
    {
        get
        {
            int sum = 0;
            foreach (string id in _slots)
            {
                int cost = EnergyCostOf(id);
                if (cost > 0)
                    sum += cost;
            }
            return sum;
        }
    }

    /// <summary>Base energy plus one point per equipped generator.</summary>
    public int MaxEnergy => BaseEnergy + GeneratorCount;

    public bool IsOverloaded => UsedEnergy > MaxEnergy;

    public bool Owns(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;
        if (_backpack.Contains(id))
            return true;
        foreach (string equipped in _slots)
        {
            if (equipped == id)
                return true;
        }
        return false;
    }

    // ------------------------------------------------------------------
    // Validation
    // ------------------------------------------------------------------

    /// <summary>
    /// Validates placing <paramref name="id"/> into <paramref name="slot"/>
    /// (replace semantics: the slot's current occupant is removed first).
    /// <paramref name="reason"/> is a stable token for UI copy:
    /// empty_id / unknown / bad_slot / already_equipped / copy_cap /
    /// generator_cap / overload.
    /// </summary>
    public bool CanEquip(string id, int slot, out string reason)
    {
        reason = "";
        if (string.IsNullOrEmpty(id))
        {
            reason = "empty_id";
            return false;
        }
        if (!GameManager.OrganelleCatalog.ContainsKey(id))
        {
            reason = "unknown";
            return false;
        }
        if (slot < 0 || slot >= MaxSlots)
        {
            reason = "bad_slot";
            return false;
        }
        if (_slots[slot] == id)
        {
            reason = "already_equipped";
            return false;
        }

        int equippedElsewhere = 0;
        foreach (string equipped in _slots)
        {
            if (equipped == id)
                equippedElsewhere++;
        }
        if (equippedElsewhere >= MaxCopiesOf(id))
        {
            reason = "copy_cap";
            return false;
        }

        int cost = EnergyCostOf(id);
        int replacedCost = EnergyCostOf(_slots[slot]);
        int generators = GeneratorCount;
        if (replacedCost < 0)
            generators--;
        if (cost < 0)
            generators++;
        if (generators > MaxGenerators)
        {
            reason = "generator_cap";
            return false;
        }

        int used = UsedEnergy;
        if (replacedCost > 0)
            used -= replacedCost;
        if (cost > 0)
            used += cost;
        if (used > BaseEnergy + generators)
        {
            reason = "overload";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates a whole slot set (empty strings allowed) against the chamber
    /// rules. Shared by the pre-run loadout manager and the UI so both apply
    /// exactly the same legality contract as <see cref="CanEquip"/>.
    /// <paramref name="reason"/> tokens: bad_slot / unknown / copy_cap /
    /// generator_cap / overload.
    /// </summary>
    public static bool ValidateSlots(IReadOnlyList<string> slots, out string reason)
    {
        reason = "";
        if (slots == null || slots.Count > MaxSlots)
        {
            reason = "bad_slot";
            return false;
        }

        int used = 0;
        int generators = 0;
        var copies = new System.Collections.Generic.Dictionary<string, int>();
        foreach (string id in slots)
        {
            if (string.IsNullOrEmpty(id))
                continue;
            if (!GameManager.OrganelleCatalog.ContainsKey(id))
            {
                reason = "unknown";
                return false;
            }

            copies.TryGetValue(id, out int seen);
            copies[id] = seen + 1;
            if (copies[id] > MaxCopiesOf(id))
            {
                reason = "copy_cap";
                return false;
            }

            int cost = EnergyCostOf(id);
            if (cost < 0)
                generators++;
            else
                used += cost;
        }

        if (generators > MaxGenerators)
        {
            reason = "generator_cap";
            return false;
        }
        if (used > BaseEnergy + generators)
        {
            reason = "overload";
            return false;
        }
        return true;
    }

    // ------------------------------------------------------------------
    // Mutations
    // ------------------------------------------------------------------

    /// <summary>
    /// Equips an owned (backpack) organelle into a slot. A replaced occupant
    /// returns to the backpack; its stat modifiers are removed exactly.
    /// </summary>
    public bool Equip(string id, int slot)
    {
        if (!CanEquip(id, slot, out _))
            return false;
        if (!_backpack.Remove(id))
            return false;

        string previous = _slots[slot];
        if (!string.IsNullOrEmpty(previous))
        {
            RemoveStats(previous);
            _backpack.Add(previous);
        }

        _slots[slot] = id;
        ApplyStats(id);
        EmitSignal(SignalName.ChamberChanged);
        return true;
    }

    /// <summary>
    /// Validates unequipping <paramref name="slot"/>. Removing a generator
    /// shrinks the cap, so it is refused when the remaining load would
    /// overload (<paramref name="reason"/> "overload", mirroring
    /// <see cref="CanEquip"/>). Reason tokens: bad_slot / empty_slot / overload.
    /// </summary>
    public bool CanUnequip(int slot, out string reason)
    {
        reason = "";
        if (slot < 0 || slot >= MaxSlots)
        {
            reason = "bad_slot";
            return false;
        }
        string id = _slots[slot];
        if (string.IsNullOrEmpty(id))
        {
            reason = "empty_slot";
            return false;
        }
        if (EnergyCostOf(id) < 0 && UsedEnergy > BaseEnergy + GeneratorCount - 1)
        {
            reason = "overload";
            return false;
        }
        return true;
    }

    /// <summary>Unequips a slot back into the backpack (never overloads).</summary>
    public bool Unequip(int slot)
    {
        if (!CanUnequip(slot, out _))
            return false;
        if (slot < 0 || slot >= MaxSlots)
            return false;

        string id = _slots[slot];
        if (string.IsNullOrEmpty(id))
            return false;
        if (_backpack.Count >= BackpackCap)
            return false;

        RemoveStats(id);
        _slots[slot] = "";
        _backpack.Add(id);
        EmitSignal(SignalName.ChamberChanged);
        return true;
    }

    /// <summary>Reorders two equipped slots (energy totals are unchanged).</summary>
    public bool Swap(int slotA, int slotB)
    {
        if (slotA < 0 || slotA >= MaxSlots || slotB < 0 || slotB >= MaxSlots || slotA == slotB)
            return false;

        (_slots[slotA], _slots[slotB]) = (_slots[slotB], _slots[slotA]);
        EmitSignal(SignalName.ChamberChanged);
        return true;
    }

    /// <summary>Acquires an organelle into the backpack (dedupe + capacity gated).</summary>
    public bool AddToBackpack(string id)
    {
        if (string.IsNullOrEmpty(id) || !GameManager.OrganelleCatalog.ContainsKey(id))
            return false;
        if (_backpack.Count >= BackpackCap)
            return false;
        if (CountCopies(id) >= MaxCopiesOf(id))
            return false;

        _backpack.Add(id);
        EmitSignal(SignalName.ChamberChanged);
        return true;
    }

    /// <summary>Removes one unequipped organelle from the backpack (discard).</summary>
    public bool Discard(string id)
    {
        if (!_backpack.Remove(id))
            return false;
        EmitSignal(SignalName.ChamberChanged);
        return true;
    }

    public override void _ExitTree()
    {
        foreach (string id in _slots)
        {
            if (!string.IsNullOrEmpty(id))
                RemoveStats(id);
        }
    }

    // ------------------------------------------------------------------
    // UI payload
    // ------------------------------------------------------------------

    public Dictionary GetUiData()
    {
        var slots = new Array<Dictionary>();
        for (int i = 0; i < MaxSlots; i++)
            slots.Add(SlotData(_slots[i], i));

        var backpack = new Array<Dictionary>();
        foreach (string id in _backpack)
            backpack.Add(SlotData(id, -1));

        return new Dictionary
        {
            ["slots"] = slots,
            ["backpack"] = backpack,
            ["used_energy"] = UsedEnergy,
            ["max_energy"] = MaxEnergy,
            ["base_energy"] = BaseEnergy,
            ["generators"] = GeneratorCount,
            ["equipped_count"] = EquippedCount,
            ["backpack_cap"] = BackpackCap,
            ["overloaded"] = IsOverloaded
        };
    }

    public Dictionary get_ui_data() => GetUiData();

    private static Dictionary SlotData(string id, int slot)
    {
        var data = new Dictionary
        {
            ["slot"] = slot,
            ["id"] = id,
            ["empty"] = string.IsNullOrEmpty(id),
            ["is_generator"] = false
        };
        if (string.IsNullOrEmpty(id) || !GameManager.OrganelleCatalog.TryGetValue(id, out var entryVar)
            || entryVar.VariantType != Variant.Type.Dictionary)
        {
            return data;
        }

        var entry = entryVar.AsGodotDictionary();
        data["category"] = entry["category"];
        data["energy_cost"] = entry["energy_cost"];
        data["name_key"] = entry["name_key"];
        data["desc_key"] = entry["desc_key"];
        data["bio_key"] = entry["bio_key"];
        data["icon"] = entry["icon"];
        data["image_path"] = entry["image_path"];
        data["is_generator"] = entry["energy_cost"].AsInt32() < 0;
        return data;
    }

    // ------------------------------------------------------------------
    // Catalog helpers
    // ------------------------------------------------------------------

    private static int EnergyCostOf(string id)
    {
        if (string.IsNullOrEmpty(id) || !GameManager.OrganelleCatalog.TryGetValue(id, out var entryVar)
            || entryVar.VariantType != Variant.Type.Dictionary)
        {
            return 0;
        }
        return entryVar.AsGodotDictionary()["energy_cost"].AsInt32();
    }

    private static int MaxCopiesOf(string id)
    {
        if (string.IsNullOrEmpty(id) || !GameManager.OrganelleCatalog.TryGetValue(id, out var entryVar)
            || entryVar.VariantType != Variant.Type.Dictionary)
        {
            return 1;
        }
        return entryVar.AsGodotDictionary()["max_copies"].AsInt32();
    }

    private int CountCopies(string id)
    {
        int n = 0;
        foreach (string equipped in _slots)
        {
            if (equipped == id)
                n++;
        }
        foreach (string bagged in _backpack)
        {
            if (bagged == id)
                n++;
        }
        return n;
    }

    // ------------------------------------------------------------------
    // Stat application (mirrors the passive-trait modifier convention)
    // ------------------------------------------------------------------

    private void ApplyStats(string id)
    {
        ForEachModifier(id, (stat, flat, pct) => Stats?.AddModifier(stat, flat, pct));
    }

    private void RemoveStats(string id)
    {
        ForEachModifier(id, (stat, flat, pct) => Stats?.RemoveModifier(stat, flat, pct));
    }

    private static void ForEachModifier(string id, Action<string, float, float> action)
    {
        if (string.IsNullOrEmpty(id) || !GameManager.OrganelleCatalog.TryGetValue(id, out var entryVar)
            || entryVar.VariantType != Variant.Type.Dictionary)
        {
            return;
        }

        var entry = entryVar.AsGodotDictionary();
        foreach (string listName in ModifierLists)
        {
            if (!entry.TryGetValue(listName, out var listVar) || listVar.VariantType != Variant.Type.Array)
                continue;

            foreach (var item in listVar.AsGodotArray())
            {
                if (item.VariantType != Variant.Type.Dictionary)
                    continue;
                var mod = item.AsGodotDictionary();
                string stat = mod["stat"].AsString();
                float value = mod["value"].AsSingle();
                string unit = mod["unit"].AsString();
                bool isFlatOrPoints = unit is "flat" or "percentagepoints";
                action(stat, isFlatOrPoints ? value : 0.0f, isFlatOrPoints ? 0.0f : value);
            }
        }
    }
}
