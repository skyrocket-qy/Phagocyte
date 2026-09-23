using Godot;
using Godot.Collections;
using System;
using Phagocyte.Core;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Phagocyte.Tests;

/// <summary>
/// Phase 4 balance regression guards (TODO.md §Phase 4):
/// opening-loadout impact on the first 3-minute waves, double-generator
/// tradeoff (cripple-flow vs blood-for-power), amount+1 locked at 4 cost,
/// and CDR/evasion/block hard caps. Energy stays a constraint layer,
/// never a 19th stat.
/// </summary>
public partial class TestOrganelleBalance : TestHarness
{
    private int _frame = 0;
    private bool _done = false;

    public override void _Initialize()
    {
        Banner("STARTING ORGANELLE BALANCE VERIFICATION (PHASE 4)");
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
            RunEnergyBudgetTests();
            RunGeneratorTradeoffTests();
            RunAmountLockTests();
            RunHardCapTests();
            RunOpenerBoundTests();
            Finish(true, "ALL ORGANELLE BALANCE TESTS");
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestOrganelleBalance threw: ", ex);
            Finish(false, "ORGANELLE BALANCE TESTS");
        }
        return true;
    }

    private static OrganelleChamber NewChamber(out CellStats stats)
    {
        var mock = new CharacterBody2D { Name = "BalanceHost" };
        stats = new CellStats { Name = "CellStats" };
        // Determinism: zero the RNG-gated avoidance stats first.
        stats.SetBase("block", 0.0f);
        stats.SetBase("evasion", 0.0f);
        mock.AddChild(stats);
        var chamber = new OrganelleChamber { Name = "OrganelleChamber" };
        mock.AddChild(chamber);
        // Not added to Root: pure-logic chamber needs no scene tree.
        chamber.Setup(mock);
        return chamber;
    }

    /// <summary>Base 6 budget: 4+3 overloads, 4+1+1 fits (first-wave opener bound).</summary>
    private void RunEnergyBudgetTests()
    {
        AssertThat(OrganelleChamber.BaseEnergy).IsEqual(6);
        AssertThat(OrganelleChamber.MaxSlots).IsEqual(4);

        AssertThat(OrganelleChamber.ValidateSlots(
            new[] { "rough_er", "acidic_lysosome", "", "" }, out string overload)).IsFalse();
        AssertThat(overload).IsEqual("overload");
        AssertThat(OrganelleChamber.ValidateSlots(
            new[] { "rough_er", "chemokine_patch", "microtubule_anchor", "" }, out _)).IsTrue();
        GD.Print("[PASS] Base-6 budget: 4+3 overloads, 4+1+1 fits.");
    }

    /// <summary>Double generators reach 8 energy but stack both drawbacks.</summary>
    private void RunGeneratorTradeoffTests()
    {
        var chamber = NewChamber(out var stats);
        foreach (string id in new[] { "symbiotic_flora", "phage_fragment", "rough_er", "acidic_lysosome" })
            AssertThat(chamber.AddToBackpack(id)).IsTrue();

        AssertThat(chamber.Equip("symbiotic_flora", 0)).IsTrue();
        AssertThat(chamber.Equip("phage_fragment", 1)).IsTrue();
        AssertThat(chamber.GeneratorCount).IsEqual(2);
        AssertThat(chamber.MaxEnergy).IsEqual(8);

        // Cripple-flow (殘廢流): -30% move speed, -15% might.
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("move_speed"), 230.0f * 0.7f)).IsTrue();
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("might"), 1.0f - 0.15f)).IsTrue();
        // Blood-for-power (血換電): -20% max HP for +0.05 CDR.
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("max_health"), 100.0f * 0.8f)).IsTrue();
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("cooldown_reduction"), 0.05f)).IsTrue();

        // 8 energy funds the greed combo 4+3 (used 7, headroom 1); the drawbacks are the price.
        AssertThat(chamber.Equip("rough_er", 2)).IsTrue();
        AssertThat(chamber.Equip("acidic_lysosome", 3)).IsTrue();
        AssertThat(chamber.UsedEnergy).IsEqual(7);
        AssertThat(chamber.IsOverloaded).IsFalse();
        GD.Print("[PASS] Double generators: 8 energy with stacked move/might/HP drawbacks.");

        chamber.GetParent().Free();
    }

    /// <summary>amount+1 lives only on the 4-cost rough_er (opportunity-cost lock).</summary>
    private void RunAmountLockTests()
    {
        var catalog = GameManager.OrganelleCatalog;
        string amountSource = "";
        foreach (string id in catalog.Keys)
        {
            var entry = catalog[id].AsGodotDictionary();
            foreach (var mod in entry["modifiers"].AsGodotArray<Dictionary>())
            {
                if (mod["stat"].AsString() == "amount")
                    amountSource = id;
            }
        }
        AssertThat(amountSource).IsEqual("rough_er");
        AssertThat(catalog["rough_er"].AsGodotDictionary()["energy_cost"].AsInt32()).IsEqual(4);
        GD.Print("[PASS] amount+1 is locked on the 4-cost rough_er (2/3 of the base budget).");
    }

    /// <summary>CDR/evasion/block hard caps hold under full chamber + passive stacking.</summary>
    private void RunHardCapTests()
    {
        var stats = new CellStats { Name = "CapStats" };
        stats.SetBase("block", 0.0f);
        stats.SetBase("evasion", 0.0f);

        // Chamber max CDR (0.16 + 0.05 + 0.05) + Lv5 passive (0.40) + B-cell base (0.10).
        stats.SetBase("cooldown_reduction", 0.10f);
        stats.AddModifier("cooldown_reduction", 0.40f + 0.16f + 0.05f + 0.05f, 0.0f);
        AssertThat(stats.GetStat("cooldown_reduction")).IsEqual(0.75f);

        // Chamber evasion/block are small nudges, far below their caps.
        var chamber = NewChamber(out var chamberStats);
        foreach (string id in new[] { "chemokine_patch", "ion_channel_array" })
            AssertThat(chamber.AddToBackpack(id)).IsTrue();
        AssertThat(chamber.Equip("chemokine_patch", 0)).IsTrue();
        AssertThat(chamber.Equip("ion_channel_array", 1)).IsTrue();
        AssertThat(chamberStats.GetStat("evasion")).IsLess(0.60f);
        AssertThat(chamberStats.GetStat("block")).IsLess(0.75f);

        // Caps clamp even under adversarial stacking.
        stats.AddModifier("evasion", 0.90f, 0.0f);
        AssertThat(stats.GetStat("evasion")).IsEqual(0.60f);
        stats.AddModifier("block", 0.90f, 0.0f);
        AssertThat(stats.GetStat("block")).IsEqual(0.75f);
        GD.Print("[PASS] CDR 0.75 / evasion 0.60 / block 0.75 caps hold; chamber adds nudges only.");

        chamber.GetParent().Free();
        stats.Free();
    }

    /// <summary>
    /// Opening loadout impact bound: at most +12% might, gated behind the
    /// 2% drop unlock (vault starts locked, so fresh runs deploy empty).
    /// Energy is a constraint layer, never a stat.
    /// </summary>
    private void RunOpenerBoundTests()
    {
        float maxMight = 0.0f;
        foreach (string id in GameManager.OrganelleCatalog.Keys)
        {
            var entry = GameManager.OrganelleCatalog[id].AsGodotDictionary();
            if (entry["energy_cost"].AsInt32() < 0)
                continue;
            foreach (var mod in entry["modifiers"].AsGodotArray<Dictionary>())
            {
                if (mod["stat"].AsString() == "might" && mod["unit"].AsString() == "percent")
                    maxMight = Mathf.Max(maxMight, mod["value"].AsSingle());
            }
        }
        AssertThat(maxMight).IsEqualApprox(0.12f, 0.001f);

        // Energy is not a stat: no entry references it and the pool has no slot.
        foreach (string id in GameManager.OrganelleCatalog.Keys)
        {
            var entry = GameManager.OrganelleCatalog[id].AsGodotDictionary();
            foreach (string listName in new[] { "modifiers", "drawback" })
            {
                foreach (var mod in entry[listName].AsGodotArray<Dictionary>())
                    AssertThat(mod["stat"].AsString()).IsNotEqual("energy");
            }
        }
        var probe = new CellStats { Name = "EnergyProbe" };
        AssertThat(probe.GetStatObj("energy")).IsNull();
        probe.Free();

        GD.Print("[PASS] Opener might uplift bounded at +12%; energy is a constraint, not a stat.");
    }
}
