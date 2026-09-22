using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using Phagocyte.Core;
using Phagocyte.Player;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Phagocyte.Tests;

/// <summary>
/// Phase 0 verification for the organelle chamber (TODO.md §Phase 0):
/// slot/energy/generator accounting, overload rejection, exact stat rollback,
/// backpack dedupe and the scene/code wiring of every immune cell.
/// UI flows (loadout page, upgrade draft) land in later phases.
/// </summary>
public partial class TestOrganelleChamber : TestHarness
{
    private int _frame = 0;
    private bool _done = false;

    public override void _Initialize()
    {
        Banner("STARTING ORGANELLE CHAMBER VERIFICATION (PHASE 0)");
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
            RunConstantsTest();
            RunChamberLogicTests();
            RunSceneWiringTests();
            RunTranslationTests();
            Finish(true, "ALL ORGANELLE CHAMBER TESTS");
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestOrganelleChamber threw: ", ex);
            Finish(false, "ORGANELLE CHAMBER TESTS");
        }
        return true;
    }

    private static bool BagHas(OrganelleChamber chamber, string id)
    {
        foreach (string bagged in chamber.Backpack)
        {
            if (bagged == id)
                return true;
        }
        return false;
    }

    private void RunConstantsTest()
    {
        AssertThat(OrganelleChamber.MaxSlots).IsEqual(4);
        AssertThat(OrganelleChamber.BaseEnergy).IsEqual(6);
        AssertThat(OrganelleChamber.MaxGenerators).IsEqual(2);
        AssertThat(OrganelleChamber.BackpackCap).IsEqual(12);
        GD.Print("[PASS] Chamber constants: 4 slots / base energy 6 / 2 generators / backpack 12.");
    }

    private void RunChamberLogicTests()
    {
        var mock = new CharacterBody2D { Name = "ChamberHost" };
        var stats = new CellStats { Name = "CellStats" };
        mock.AddChild(stats);
        var chamber = new OrganelleChamber { Name = "OrganelleChamber" };
        mock.AddChild(chamber);
        Root.AddChild(mock);
        chamber.Setup(mock);

        AssertThat(chamber.Stats).IsEqual(stats);
        AssertThat(chamber.UsedEnergy).IsEqual(0);
        AssertThat(chamber.MaxEnergy).IsEqual(OrganelleChamber.BaseEnergy);
        AssertThat(chamber.EquippedCount).IsEqual(0);

        // --- Acquisition + dedupe (max_copies = 1 across equipped + backpack) ---
        var ids = new List<string>();
        foreach (string id in GameManager.OrganelleCatalog.Keys)
            ids.Add(id);
        AssertThat(ids.Count).IsEqual(12);
        foreach (string id in ids)
            AssertThat(chamber.AddToBackpack(id)).IsTrue();
        AssertThat(chamber.Backpack.Count).IsEqual(12);
        AssertThat(chamber.AddToBackpack(ids[0])).IsFalse();
        GD.Print("[PASS] Backpack acquires all 12 catalog items; duplicate copies are rejected.");

        // --- Overload rejection: 4 + 3 > 6 ---
        AssertThat(chamber.Equip("mitochondria_mkii", 0)).IsTrue();
        AssertThat(chamber.UsedEnergy).IsEqual(4);
        AssertThat(chamber.MaxEnergy).IsEqual(6);
        AssertThat(chamber.CanEquip("acidic_lysosome", 1, out string overloadReason)).IsFalse();
        AssertThat(overloadReason).IsEqual("overload");
        AssertThat(chamber.Equip("acidic_lysosome", 1)).IsFalse();
        AssertThat(chamber.GetSlot(1)).IsEqual("");
        AssertThat(chamber.Backpack.Count).IsEqual(11);
        GD.Print("[PASS] Energy overload (4+3 > 6) rejected; no slot/backpack mutation.");

        // --- Generator expands the cap: 6/6 -> 7/7, then the 3-cost fits ---
        AssertThat(chamber.Equip("symbiotic_flora", 1)).IsTrue();
        AssertThat(chamber.GeneratorCount).IsEqual(1);
        AssertThat(chamber.MaxEnergy).IsEqual(7);
        AssertThat(chamber.Equip("acidic_lysosome", 2)).IsTrue();
        AssertThat(chamber.UsedEnergy).IsEqual(7);
        AssertThat(chamber.MaxEnergy).IsEqual(7);
        AssertThat(chamber.IsOverloaded).IsFalse();
        GD.Print("[PASS] Generator raises the cap 6 -> 7; the formerly-overloaded 3-cost now equips.");

        // --- Generator drawback applies immediately (acidic lysosome adds Might +12%) ---
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("move_speed"), 230.0f * 0.7f)).IsTrue();
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("might"), 1.0f + 0.12f - 0.15f)).IsTrue();
        GD.Print("[PASS] Generator drawback active: Move Speed -30%, Might -15% (stacked with lysosome +12%).");

        // --- Replace semantics: 4-cost out, 1-cost in; stats roll back exactly ---
        AssertThat(chamber.Equip("glycolytic_bypass", 0)).IsTrue();
        AssertThat(chamber.UsedEnergy).IsEqual(4);
        AssertThat(BagHas(chamber, "mitochondria_mkii")).IsTrue();
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("cooldown_reduction"), 0.05f)).IsTrue();
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("duration"), 1.0f)).IsTrue();
        GD.Print("[PASS] Slot replace: replaced organelle returns to backpack, its stats removed exactly.");

        // --- Unequip rollback + backpack return ---
        AssertThat(chamber.Unequip(1)).IsTrue();
        AssertThat(chamber.GetSlot(1)).IsEqual("");
        AssertThat(chamber.MaxEnergy).IsEqual(6);
        AssertThat(BagHas(chamber, "symbiotic_flora")).IsTrue();
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("move_speed"), 230.0f * 1.03f)).IsTrue();
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("might"), 1.0f + 0.12f)).IsTrue();
        GD.Print("[PASS] Unequip rolls the -30% back (glycolytic +3% and lysosome Might remain).");

        // --- Backpack never counts toward energy ---
        AssertThat(chamber.UsedEnergy).IsEqual(4);
        GD.Print("[PASS] Backpack contents do not consume energy.");

        // --- Swap reorders slots without changing the budget ---
        AssertThat(chamber.Swap(0, 2)).IsTrue();
        AssertThat(chamber.GetSlot(0)).IsEqual("acidic_lysosome");
        AssertThat(chamber.GetSlot(2)).IsEqual("glycolytic_bypass");
        AssertThat(chamber.UsedEnergy).IsEqual(4);
        GD.Print("[PASS] Swap reorders equipped slots with an unchanged energy budget.");

        // --- Reason tokens for UI copy ---
        AssertThat(chamber.CanEquip("acidic_lysosome", 1, out string copyReason)).IsFalse();
        AssertThat(copyReason).IsEqual("copy_cap");
        AssertThat(chamber.CanEquip("glycolytic_bypass", 9, out string slotReason)).IsFalse();
        AssertThat(slotReason).IsEqual("bad_slot");
        AssertThat(chamber.CanEquip("not_a_real_organelle", 1, out string unknownReason)).IsFalse();
        AssertThat(unknownReason).IsEqual("unknown");
        AssertThat(chamber.CanEquip("glycolytic_bypass", 2, out string sameReason)).IsFalse();
        AssertThat(sameReason).IsEqual("already_equipped");
        GD.Print("[PASS] CanEquip reason tokens: copy_cap / bad_slot / unknown / already_equipped.");

        // --- Two generators -> cap 8; both drawbacks stack ---
        AssertThat(chamber.Equip("symbiotic_flora", 1)).IsTrue();
        AssertThat(chamber.Equip("phage_fragment", 3)).IsTrue();
        AssertThat(chamber.GeneratorCount).IsEqual(2);
        AssertThat(chamber.MaxEnergy).IsEqual(8);
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("max_health"), 100.0f * 0.8f)).IsTrue();
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("cooldown_reduction"), 0.10f)).IsTrue();
        GD.Print("[PASS] Two generators cap at 8 energy; both drawbacks stack on the stat pool.");

        // --- UI payload ---
        var ui = chamber.GetUiData();
        AssertThat(ui.ContainsKey("slots")).IsTrue();
        AssertThat(ui.ContainsKey("backpack")).IsTrue();
        AssertThat(ui["slots"].AsGodotArray<Dictionary>().Count).IsEqual(4);
        AssertThat(ui["used_energy"].AsInt32()).IsEqual(4);
        AssertThat(ui["max_energy"].AsInt32()).IsEqual(8);
        AssertThat(ui["generators"].AsInt32()).IsEqual(2);
        AssertThat(ui["equipped_count"].AsInt32()).IsEqual(4);
        AssertThat(ui["overloaded"].AsBool()).IsFalse();
        GD.Print("[PASS] GetUiData payload exposes slots/backpack/energy/generators for the HUD.");

        // --- Discard removes only from the backpack ---
        AssertThat(chamber.Discard("rough_er")).IsTrue();
        AssertThat(chamber.Discard("rough_er")).IsFalse();
        AssertThat(BagHas(chamber, "rough_er")).IsFalse();
        AssertThat(chamber.Discard("acidic_lysosome")).IsFalse();
        GD.Print("[PASS] Discard removes an unequipped organelle only.");

        // --- _ExitTree rolls every equipped modifier back ---
        chamber.Free();
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("max_health"), 100.0f)).IsTrue();
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("cooldown_reduction"), 0.0f)).IsTrue();
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("move_speed"), 230.0f)).IsTrue();
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("might"), 1.0f)).IsTrue();
        mock.Free();
        GD.Print("[PASS] _ExitTree cleanup removes all equipped modifiers (no stat drift).");
    }

    private void RunSceneWiringTests()
    {
        string[] cellIds = { "macrophage", "ctl", "neutrophil", "b_cell", "dendritic" };
        foreach (string cellId in cellIds)
        {
            var scene = GameManager.GetCellScene(cellId);
            AssertThat(scene).IsNotNull();
            var cell = (BaseCell)scene.Instantiate();
            Root.AddChild(cell);

            AssertThat(cell.GetNodeOrNull<OrganelleChamber>("OrganelleChamber")).IsNotNull();
            AssertThat(cell.CellOrganelleChamber).IsNotNull();
            AssertThat(cell.CellOrganelleChamber!.Stats).IsEqual(cell.Stats);

            float cdrBefore = cell.Stats!.GetStat("cooldown_reduction");
            AssertThat(cell.CellOrganelleChamber.AddToBackpack("mitochondria_mkii")).IsTrue();
            AssertThat(cell.CellOrganelleChamber.Equip("mitochondria_mkii", 0)).IsTrue();
            AssertThat(cell.Stats.GetStat("cooldown_reduction")).IsGreater(cdrBefore);

            cell.Free();
        }
        GD.Print("[PASS] All 5 immune cell scenes resolve their OrganelleChamber and apply stats.");

        // Code fallback when the scene lacks the node (mirrors the CellStats pattern).
        var fallbackScene = GameManager.GetCellScene("macrophage");
        var bare = (BaseCell)fallbackScene.Instantiate();
        bare.GetNode("OrganelleChamber").Free();
        Root.AddChild(bare);
        AssertThat(bare.GetNodeOrNull<OrganelleChamber>("OrganelleChamber")).IsNotNull();
        AssertThat(bare.CellOrganelleChamber).IsNotNull();
        AssertThat(bare.CellOrganelleChamber!.Stats).IsEqual(bare.Stats);
        bare.Free();
        GD.Print("[PASS] Missing scene node falls back to a code-created OrganelleChamber.");
    }

    private void RunTranslationTests()
    {
        foreach (string id in GameManager.OrganelleCatalog.Keys)
        {
            var entry = GameManager.OrganelleCatalog[id].AsGodotDictionary();
            foreach (string key in new[] { "name_key", "desc_key", "bio_key" })
            {
                string trKey = entry[key].AsString();
                AssertThat(string.IsNullOrEmpty(trKey)).IsFalse();
                AssertThat(TranslationServer.Translate(trKey)).IsNotEqual(trKey);
            }
        }
        GD.Print("[PASS] All organelle name/desc/bio keys resolve through translations.csv.");
    }
}
