using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using Phagocyte.Core;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Phagocyte.Tests;

/// <summary>
/// Phase 0 data contract for the organelle chamber (TODO.md §Phase 0):
/// 24 entries, 6 categories x 4, energy cost in [-1, 4], generators carry a
/// drawback, and every modifier/drawback stat resolves against the universal
/// stat pool. Runtime chamber behaviour is covered by TestOrganelleChamber.
/// </summary>
public partial class TestOrganelleCatalog : TestHarness
{
    private int _frame = 0;
    private bool _done = false;

    public override void _Initialize()
    {
        Banner("STARTING ORGANELLE CATALOG VERIFICATION");
    }

    public override bool _Process(double delta)
    {
        if (_done)
            return true;
        if (!Gate(ref _frame, 2))
            return false;

        _done = true;
        try
        {
            RunCatalogTests();
            RunEquipSmokeTests();
            Finish(true, "ALL ORGANELLE CATALOG TESTS");
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestOrganelleCatalog threw: ", ex);
            Finish(false, "ORGANELLE CATALOG TESTS");
        }
        return true;
    }

    private static void RunCatalogTests()
    {
        var catalog = GameManager.OrganelleCatalog;

        AssertThat(catalog.Count).IsEqual(24);
        GD.Print($"[PASS] Organelle catalog loaded {catalog.Count} entries.");

        var perCategory = new System.Collections.Generic.Dictionary<string, int>();
        var ids = new HashSet<string>();
        foreach (string id in catalog.Keys)
        {
            var entry = catalog[id].AsGodotDictionary();
            ids.Add(id);

            AssertThat(entry["id"].AsString()).IsEqual(id);
            AssertThat(entry.ContainsKey("image_path")).IsTrue();
            AssertThat(entry["image_path"].AsString()).IsEqual(AssetPaths.OrganelleIcon(id));
            AssertThat(CatalogBuilders.OrganelleCategories.Contains(entry["category"].AsString())).IsTrue();

            string category = entry["category"].AsString();
            perCategory[category] = perCategory.TryGetValue(category, out int n) ? n + 1 : 1;

            int cost = entry["energy_cost"].AsInt32();
            AssertThat(cost >= CatalogBuilders.OrganelleMinEnergyCost && cost <= CatalogBuilders.OrganelleMaxEnergyCost).IsTrue();

            var modifiers = entry["modifiers"].AsGodotArray<Dictionary>();
            var drawback = entry["drawback"].AsGodotArray<Dictionary>();

            if (cost < 0)
            {
                AssertThat(drawback.Count).IsGreater(0);
            }
            else
            {
                AssertThat(modifiers.Count).IsGreater(0);
            }

            foreach (var mod in modifiers)
                AssertStatResolves(id, "modifiers", mod);
            foreach (var mod in drawback)
                AssertStatResolves(id, "drawback", mod);
        }

        AssertThat(ids.Count).IsEqual(24);
        AssertThat(perCategory.Count).IsEqual(6);
        foreach (var kv in perCategory)
        {
            AssertThat(kv.Value).IsEqual(4);
        }
        GD.Print("[PASS] 6 categories x 4 entries, unique ids, valid costs and generator drawbacks verified.");

        int generators = 0;
        foreach (string id in catalog.Keys)
        {
            if (catalog[id].AsGodotDictionary()["energy_cost"].AsInt32() < 0)
                generators++;
        }
        AssertThat(generators).IsEqual(4);
        GD.Print("[PASS] Exactly 4 generator organelles (+1 energy) present.");
    }

    /// <summary>
    /// Every catalog entry equips into a solo slot (single-item load is always
    /// within budget), proving the data end-to-end at runtime.
    /// </summary>
    private void RunEquipSmokeTests()
    {
        OrganelleUnlockManager.ResetCache();
        OrganelleUnlockManager.ResetAll();
        OrganelleUnlockManager.UnlockAll();

        var mock = new CharacterBody2D { Name = "CatalogSmokeHost" };
        mock.AddChild(new CellStats { Name = "CellStats" });
        var chamber = new OrganelleChamber { Name = "OrganelleChamber" };
        mock.AddChild(chamber);
        Root.AddChild(mock);
        chamber.Setup(mock);

        int equipped = 0;
        foreach (string id in GameManager.OrganelleCatalog.Keys)
        {
            AssertThat(chamber.AddToBackpack(id)).IsTrue();
            AssertThat(chamber.Equip(id, 0)).IsTrue();
            AssertThat(chamber.GetSlot(0)).IsEqual(id);
            equipped++;
        }
        AssertThat(equipped).IsEqual(GameManager.OrganelleCatalog.Count);
        GD.Print($"[PASS] All {equipped} catalog organelles equip into a solo slot.");

        mock.Free();
    }

    private static void AssertStatResolves(string id, string listName, Dictionary mod)
    {
        string stat = mod["stat"].AsString();
        AssertThat(string.IsNullOrEmpty(stat)).IsFalse();
        AssertThat(PassiveTreeManager.HasStatLabel(stat)).IsTrue();
        string unit = mod["unit"].AsString();
        AssertThat(unit is "flat" or "percent" or "percentagepoints").IsTrue();
        if (!PassiveTreeManager.HasStatLabel(stat))
            GD.PrintErr($"[FAIL] Organelle '{id}' {listName} references unknown stat '{stat}'.");
    }
}
