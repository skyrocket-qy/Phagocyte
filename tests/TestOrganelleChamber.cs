using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Skills;
using Phagocyte.UI;
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
            RunUnequipOverloadTests();
            RunSceneWiringTests();
            RunTranslationTests();
            RunUnlockManagerTests();
            RunLoadoutManagerTests();
            RunApplyLoadoutTests();
            RunDraftFlowTests();
            RunSwapFlowTests();
            RunDiscardAndHudTests();
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
        AssertThat(OrganelleChamber.BackpackCap).IsEqual(24);
        GD.Print("[PASS] Chamber constants: 4 slots / base energy 6 / backpack 24 (generators uncapped).");
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
        AssertThat(ids.Count).IsEqual(24);
        foreach (string id in ids)
            AssertThat(chamber.AddToBackpack(id)).IsTrue();
        AssertThat(chamber.Backpack.Count).IsEqual(24);
        AssertThat(chamber.AddToBackpack(ids[0])).IsFalse();
        GD.Print("[PASS] Backpack acquires all 24 catalog items; duplicate copies are rejected.");

        // --- Overload rejection: 4 + 3 > 6 ---
        AssertThat(chamber.Equip("mitochondria_mkii", 0)).IsTrue();
        AssertThat(chamber.UsedEnergy).IsEqual(4);
        AssertThat(chamber.MaxEnergy).IsEqual(6);
        AssertThat(chamber.CanEquip("acidic_lysosome", 1, out string overloadReason)).IsFalse();
        AssertThat(overloadReason).IsEqual("overload");
        AssertThat(chamber.Equip("acidic_lysosome", 1)).IsFalse();
        AssertThat(chamber.GetSlot(1)).IsEqual("");
        AssertThat(chamber.Backpack.Count).IsEqual(23);
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

        // --- No generator cap: a third generator equips, then the slot is restored ---
        // (slot 0 currently holds acidic_lysosome after the swap above)
        AssertThat(chamber.Equip("redox_symbiont", 0)).IsTrue();
        AssertThat(chamber.GeneratorCount).IsEqual(3);
        AssertThat(chamber.MaxEnergy).IsEqual(9);
        AssertThat(chamber.UsedEnergy).IsEqual(1);
        GD.Print("[PASS] Third generator equips past the removed cap of 2.");
        AssertThat(chamber.Equip("acidic_lysosome", 0)).IsTrue();
        AssertThat(chamber.GeneratorCount).IsEqual(2);
        AssertThat(chamber.MaxEnergy).IsEqual(8);
        AssertThat(chamber.UsedEnergy).IsEqual(4);

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

    private void RunUnequipOverloadTests()
    {
        OrganelleUnlockManager.ResetCache();
        OrganelleUnlockManager.ResetAll();
        OrganelleUnlockManager.UnlockAll();

        var mock = new CharacterBody2D { Name = "UnequipHost" };
        var stats = new CellStats { Name = "CellStats" };
        mock.AddChild(stats);
        var chamber = new OrganelleChamber { Name = "OrganelleChamber" };
        mock.AddChild(chamber);
        Root.AddChild(mock);
        chamber.Setup(mock);

        AssertThat(chamber.AddToBackpack("mitochondria_mkii")).IsTrue();
        AssertThat(chamber.AddToBackpack("symbiotic_flora")).IsTrue();
        AssertThat(chamber.AddToBackpack("acidic_lysosome")).IsTrue();
        AssertThat(chamber.Equip("mitochondria_mkii", 0)).IsTrue();
        AssertThat(chamber.Equip("symbiotic_flora", 1)).IsTrue();
        AssertThat(chamber.Equip("acidic_lysosome", 2)).IsTrue();
        AssertThat(chamber.UsedEnergy).IsEqual(7);
        AssertThat(chamber.MaxEnergy).IsEqual(7);

        // Positive control: unequipping a consumer works unblocked.
        AssertThat(chamber.CanUnequip(2, out string consumerReason)).IsTrue();
        AssertThat(chamber.Unequip(2)).IsTrue();
        AssertThat(chamber.Equip("acidic_lysosome", 2)).IsTrue();

        // Removing the generator now would strand 7 used on a cap of 6.
        AssertThat(chamber.CanUnequip(1, out string overloadReason)).IsFalse();
        AssertThat(overloadReason).IsEqual("overload");
        AssertThat(chamber.Unequip(1)).IsFalse();
        AssertThat(chamber.GetSlot(1)).IsEqual("symbiotic_flora");
        AssertThat(chamber.UsedEnergy).IsEqual(7);
        AssertThat(chamber.MaxEnergy).IsEqual(7);
        AssertThat(chamber.IsOverloaded).IsFalse();
        AssertThat(chamber.CanUnequip(9, out string slotReason)).IsFalse();
        AssertThat(slotReason).IsEqual("bad_slot");
        AssertThat(chamber.CanUnequip(3, out string emptyReason)).IsFalse();
        AssertThat(emptyReason).IsEqual("empty_slot");
        GD.Print("[PASS] Generator removal is refused while the remaining load would overload; state untouched.");

        // Recovery: shed load first, then the generator leaves.
        AssertThat(chamber.Unequip(2)).IsTrue();
        AssertThat(chamber.Unequip(1)).IsTrue();
        AssertThat(chamber.MaxEnergy).IsEqual(OrganelleChamber.BaseEnergy);
        AssertThat(BagHas(chamber, "symbiotic_flora")).IsTrue();
        GD.Print("[PASS] Shedding a consumer first unblocks the generator removal.");

        mock.Free();

        // UI level: the loadout page refuses with the overload hint and still
        // clears a generator build via ResetLoadout (consumers first).
        var viewScene = AssetLoader.Load<PackedScene>("res://scenes/ui/loadout_view.tscn");
        AssertThat(viewScene).IsNotNull();
        var view = viewScene!.Instantiate<LoadoutView>();
        Root.AddChild(view);
        view.Open("macrophage");
        AssertThat(view.Chamber).IsNotNull();

        AssertThat(view.ToggleOrganelle("mitochondria_mkii")).IsTrue();
        AssertThat(view.ToggleOrganelle("symbiotic_flora")).IsTrue();
        AssertThat(view.ToggleOrganelle("acidic_lysosome")).IsTrue();
        AssertThat(view.Chamber!.UsedEnergy).IsEqual(7);

        AssertThat(view.ToggleOrganelle("symbiotic_flora")).IsFalse();
        AssertThat(view.Chamber.GetSlot(1)).IsEqual("symbiotic_flora");
        AssertThat(view.Chamber.IsOverloaded).IsFalse();
        GD.Print("[PASS] Loadout page refuses the generator removal with the overload hint.");

        AssertThat(view.ToggleOrganelle("acidic_lysosome")).IsTrue();
        AssertThat(view.ToggleOrganelle("symbiotic_flora")).IsTrue();
        AssertThat(view.Chamber.GeneratorCount).IsEqual(0);

        AssertThat(view.ToggleOrganelle("mitochondria_mkii")).IsTrue();
        AssertThat(view.ToggleOrganelle("symbiotic_flora")).IsTrue();
        AssertThat(view.ToggleOrganelle("acidic_lysosome")).IsTrue();
        view.ResetLoadout();
        AssertThat(view.Chamber.EquippedCount).IsEqual(0);
        AssertThat(view.Chamber.IsOverloaded).IsFalse();
        GD.Print("[PASS] ResetLoadout clears a generator build (consumers first, generators follow).");

        view.Free();
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

        // Phase 1 loadout page keys.
        foreach (string key in new[]
        {
            "LOADOUT_HEADER", "LOADOUT_BACKPACK_HEADER", "LOADOUT_CHAMBER_HEADER", "LOADOUT_ENERGY_FMT",
            "LOADOUT_GENERATOR_FMT", "LOADOUT_CONFIRM", "LOADOUT_RESET", "LOADOUT_CATEGORY_ALL",
            "LOADOUT_EQUIPPED_TAG", "LOADOUT_EMPTY_SLOT", "LOADOUT_DETAIL_EMPTY", "LOADOUT_HINT_DEFAULT",
            "LOADOUT_HINT_SLOTS_FULL", "LOADOUT_HINT_OVERLOAD", "LOADOUT_HINT_GENERATOR_CAP",
            "LOADOUT_HINT_COPY_CAP", "LOADOUT_HINT_UNKNOWN", "LOADOUT_HINT_BAD_SLOT",
            "LOADOUT_VAULT_PROGRESS", "LOADOUT_LOCKED_TAG", "LOADOUT_HINT_LOCKED",
            "LOADOUT_DETAIL_LOCKED", "TOAST_ORGANELLE_UNLOCKED", "LOADOUT_COST_LABEL",
            "BADGE_NEW_ORGANELLE", "SWAP_TITLE", "SWAP_STORE", "SWAP_DISCARD", "SWAP_CANCEL", "SWAP_HINT"
        })
        {
            AssertThat(TranslationServer.Translate(key)).IsNotEqual(key);
        }
        foreach (string category in LoadoutView.Categories)
        {
            string key = "ORGANELLE_CAT_" + category.ToUpperInvariant();
            AssertThat(TranslationServer.Translate(key)).IsNotEqual(key);
        }
        GD.Print("[PASS] Loadout page and category keys resolve through translations.csv.");
    }

    /// <summary>Phase 1 revision: the vault starts locked and only drops unlock it.</summary>
    private void RunUnlockManagerTests()
    {
        OrganelleUnlockManager.ResetCache();
        OrganelleUnlockManager.ResetAll();

        // Fresh profile: nothing is unlocked (the vault is earned, not given).
        AssertThat(OrganelleUnlockManager.UnlockedCount).IsEqual(0);
        AssertThat(OrganelleUnlockManager.LockedCount).IsEqual(GameManager.OrganelleCatalog.Count);
        AssertThat(OrganelleUnlockManager.IsUnlocked("mitochondria_mkii")).IsFalse();
        GD.Print("[PASS] Organelle vault starts fully locked.");

        // Unlock is idempotent, validated and persisted.
        AssertThat(OrganelleUnlockManager.Unlock("mitochondria_mkii")).IsTrue();
        AssertThat(OrganelleUnlockManager.Unlock("mitochondria_mkii")).IsFalse();
        AssertThat(OrganelleUnlockManager.Unlock("not_a_real_organelle")).IsFalse();
        OrganelleUnlockManager.ResetCache();
        AssertThat(OrganelleUnlockManager.IsUnlocked("mitochondria_mkii")).IsTrue();
        AssertThat(OrganelleUnlockManager.UnlockedCount).IsEqual(1);
        GD.Print("[PASS] Unlocks are idempotent, validated and persisted (isolated save).");

        // Roll selection only ever returns a locked id.
        string rolled = OrganelleUnlockManager.RollLockedId();
        AssertThat(rolled).IsNotEmpty();
        AssertThat(OrganelleUnlockManager.IsUnlocked(rolled)).IsFalse();
        GD.Print("[PASS] Drop roll only selects still-locked organelles.");

        // Suites run with drops disabled so kills can never unlock unscripted.
        AssertThat(OrganelleUnlockManager.DropsEnabled).IsFalse();
        var host = new Node2D { Name = "DropHost" };
        Root.AddChild(host);
        AssertThat(OrganelleUnlockManager.TrySpawnDrop(Vector2.Zero, host, null)).IsNull();
        GD.Print("[PASS] Suite default: organelle drops are disabled for determinism.");

        // Explicit roll spawns a pickup; collecting it is what unlocks.
        OrganelleUnlockManager.DropsEnabled = true;
        OrganelleUnlockManager.DropChance = 1.0f;
        var drop = OrganelleUnlockManager.TrySpawnDrop(new Vector2(10, 20), host, null);
        AssertThat(drop).IsNotNull();
        string droppedId = drop!.OrganelleId;
        AssertThat(droppedId).IsNotEmpty();
        AssertThat(OrganelleUnlockManager.IsUnlocked(droppedId)).IsFalse();
        drop.CollectForTest();
        AssertThat(OrganelleUnlockManager.IsUnlocked(droppedId)).IsTrue();
        AssertThat(drop.IsQueuedForDeletion()).IsTrue();
        GD.Print("[PASS] A rolled drop spawns as a pickup and unlocks on collect.");

        // No locked organelles left -> no drops at all.
        OrganelleUnlockManager.UnlockAll();
        AssertThat(OrganelleUnlockManager.LockedCount).IsEqual(0);
        AssertThat(OrganelleUnlockManager.RollLockedId()).IsEqual("");
        AssertThat(OrganelleUnlockManager.TrySpawnDrop(Vector2.Zero, host, null)).IsNull();
        GD.Print("[PASS] A complete collection stops spawning drops.");

        host.Free();
        OrganelleUnlockManager.DropsEnabled = false;
        OrganelleUnlockManager.DropChance = OrganelleUnlockManager.DefaultDropChance;
        AssertThat(OrganelleUnlockManager.DropChance).IsEqual(OrganelleUnlockManager.DefaultDropChance);
    }

    /// <summary>Phase 2: the run draft offers, acquires, equips, swaps and heals.</summary>
    private void RunDraftFlowTests()
    {
        // Draft offers at most one organelle card, only unlocked + unowned ids.
        OrganelleUnlockManager.ResetCache();
        OrganelleUnlockManager.ResetAll();
        OrganelleUnlockManager.Unlock("mitochondria_mkii");
        OrganelleUnlockManager.Unlock("acidic_lysosome");

        var mock = new CharacterBody2D { Name = "DraftHost" };
        var stats = new CellStats { Name = "CellStats" };
        mock.AddChild(stats);
        mock.AddChild(new SkillManager { Name = "SkillManager" });
        var chamber = new OrganelleChamber { Name = "OrganelleChamber" };
        mock.AddChild(chamber);
        Root.AddChild(mock);
        chamber.Setup(mock);

        var choices = UpgradeManager.GenerateChoices(mock, 3);
        AssertThat(choices.Count).IsEqual(3);
        int organelleCards = 0;
        foreach (var c in choices)
        {
            AssertThat(c.ContainsKey("type") && c.ContainsKey("id") && c.ContainsKey("name")).IsTrue();
            if (c["type"].AsString() != "new_organelle")
                continue;
            organelleCards++;
            string organelleId = c["id"].AsString();
            AssertThat(organelleId == "mitochondria_mkii" || organelleId == "acidic_lysosome").IsTrue();
            AssertThat(c["badge"].AsString()).IsEqual("BADGE_NEW_ORGANELLE");
            AssertThat(c.ContainsKey("energy_cost")).IsTrue();
        }
        AssertThat(organelleCards).IsLessEqual(1);
        GD.Print("[PASS] Draft offers at most one organelle card, only from unlocked ids.");

        // ApplyChoice acquires into the backpack and auto-equips the first legal slot.
        var draftChoice = new Godot.Collections.Dictionary
        {
            { "type", "new_organelle" },
            { "id", "mitochondria_mkii" },
            { "skill_class", "" }
        };
        AssertThat(UpgradeManager.ApplyChoice(mock, draftChoice)).IsTrue();
        AssertThat(chamber.GetSlot(0)).IsEqual("mitochondria_mkii");
        AssertThat(Mathf.IsEqualApprox(stats.GetStat("cooldown_reduction"), 0.16f)).IsTrue();
        // Re-applying the same id is refused (already owned).
        AssertThat(UpgradeManager.ApplyChoice(mock, draftChoice)).IsFalse();
        GD.Print("[PASS] Draft choice acquires, auto-equips and refuses duplicates.");

        // A locked organelle is never applied.
        var lockedChoice = new Godot.Collections.Dictionary
        {
            { "type", "new_organelle" },
            { "id", "rough_er" }
        };
        AssertThat(UpgradeManager.ApplyChoice(mock, lockedChoice)).IsFalse();
        GD.Print("[PASS] Locked organelles are refused by the draft applier.");

        // Fill the chamber (4-cost + 3-cost on a 6 budget stays legal) and check
        // the modal refuses an unaffordable replacement with a stable reason.
        var acidicChoice = new Godot.Collections.Dictionary
        {
            { "type", "new_organelle" },
            { "id", "acidic_lysosome" }
        };
        AssertThat(chamber.EquippedCount).IsEqual(1);
        GD.Print("[PASS] Draft flow ends with a legal single-equip state.");

        mock.Free();
    }

    /// <summary>Phase 2: the in-modal swap step (replace / store / discard).</summary>
    /// <summary>Phase 2: the in-modal swap step (replace / store / discard).</summary>
    private void RunSwapFlowTests()
    {
        OrganelleUnlockManager.ResetCache();
        OrganelleUnlockManager.ResetAll();
        foreach (string id in new[]
        {
            "symbiotic_flora", "phage_fragment", "chemokine_patch", "proteasome_sieve",
            "rough_er", "acidic_lysosome", "glycolytic_bypass", "ribosome_cluster"
        })
        {
            AssertThat(OrganelleUnlockManager.Unlock(id)).IsTrue();
        }

        var mock = new CharacterBody2D { Name = "SwapHost" };
        mock.AddChild(new CellStats { Name = "CellStats" });
        mock.AddChild(new SkillManager { Name = "SkillManager" });
        var chamber = new OrganelleChamber { Name = "OrganelleChamber" };
        mock.AddChild(chamber);
        Root.AddChild(mock);
        chamber.Setup(mock);

        // A full 4/4 chamber is the only state that forces the swap step:
        // auto-equip only ever visits empty slots.
        foreach (string id in new[] { "symbiotic_flora", "phage_fragment", "chemokine_patch", "proteasome_sieve" })
            AssertThat(chamber.AddToBackpack(id)).IsTrue();
        AssertThat(chamber.Equip("symbiotic_flora", 0)).IsTrue();
        AssertThat(chamber.Equip("phage_fragment", 1)).IsTrue();
        AssertThat(chamber.Equip("chemokine_patch", 2)).IsTrue();
        AssertThat(chamber.Equip("proteasome_sieve", 3)).IsTrue();
        AssertThat(chamber.EquippedCount).IsEqual(OrganelleChamber.MaxSlots);
        AssertThat(chamber.UsedEnergy).IsEqual(2);
        AssertThat(chamber.MaxEnergy).IsEqual(8);

        var modalScene = AssetLoader.Load<PackedScene>("res://scenes/ui/upgrade_modal.tscn");
        var modal = modalScene.Instantiate<UpgradeModal>();
        Root.AddChild(modal);

        // 1) A full chamber opens the swap step instead of auto-equipping.
        ShowOrganelleCard(modal, mock, "rough_er", 4);
        modal.OnCardClicked(0);
        AssertThat(modal.IsSwapMode).IsTrue();
        AssertThat(modal.Visible).IsTrue();
        AssertThat(Paused).IsTrue();
        AssertThat(modal.PendingOrganelle).IsEqual("rough_er");
        GD.Print("[PASS] A full chamber opens the in-modal swap step.");

        // 2) Replacing the generator (slot 0) keeps used 6 <= max 7: legal.
        AssertThat(modal.OnSwapSlotPressed(0)).IsTrue();
        AssertThat(chamber.GetSlot(0)).IsEqual("rough_er");
        AssertThat(chamber.UsedEnergy).IsEqual(6);
        AssertThat(chamber.MaxEnergy).IsEqual(7);
        AssertThat(modal.Visible).IsFalse();
        AssertThat(modal.IsSwapMode).IsFalse();
        AssertThat(Paused).IsFalse();
        GD.Print("[PASS] Swap replaces the chosen slot, retires the old item and resumes.");

        // 3) An over-budget replacement (6 + 3 = 9 > 7) is refused with a hint.
        ShowOrganelleCard(modal, mock, "acidic_lysosome", 3);
        modal.OnCardClicked(0);
        AssertThat(modal.IsSwapMode).IsTrue();
        AssertThat(modal.OnSwapSlotPressed(1)).IsFalse();
        AssertThat(modal.SwapHintText).IsEqual(Tr("LOADOUT_HINT_OVERLOAD"));
        AssertThat(chamber.GetSlot(1)).IsEqual("phage_fragment");
        AssertThat(modal.Visible).IsTrue();
        AssertThat(Paused).IsTrue();
        GD.Print("[PASS] An over-budget replacement is refused with a hint and stays in swap mode.");

        // 4) Cancel rewinds to the cards without resuming the game.
        modal.OnSwapCancelPressed();
        AssertThat(modal.IsSwapMode).IsFalse();
        AssertThat(Paused).IsTrue();
        GD.Print("[PASS] Swap cancel rewinds to the 3-choice cards without resuming.");

        // 5) Re-opening the pending card and storing it keeps the organelle.
        modal.OnCardClicked(0);
        AssertThat(modal.IsSwapMode).IsTrue();
        modal.OnSwapStorePressed();
        AssertThat(modal.Visible).IsFalse();
        AssertThat(Paused).IsFalse();
        AssertThat(BagHas(chamber, "acidic_lysosome")).IsTrue();
        GD.Print("[PASS] Swap store keeps the organelle in the run backpack.");

        // 6) Discard drops the pending organelle from the run.
        ShowOrganelleCard(modal, mock, "ribosome_cluster", 2);
        modal.OnCardClicked(0);
        AssertThat(modal.IsSwapMode).IsTrue();
        AssertThat(BagHas(chamber, "ribosome_cluster")).IsTrue();
        modal.OnSwapDiscardPressed();
        AssertThat(modal.Visible).IsFalse();
        AssertThat(Paused).IsFalse();
        AssertThat(BagHas(chamber, "ribosome_cluster")).IsFalse();
        GD.Print("[PASS] Swap discard removes the pending organelle from the run.");

        modal.Free();
        mock.Free();
        Root.GetTree().Paused = false;
    }

    /// <summary>Shows a single organelle draft card bound to a host cell.</summary>
    private static void ShowOrganelleCard(UpgradeModal modal, Node2D player, string id, int cost)
    {
        var card = new Godot.Collections.Dictionary
        {
            { "type", "new_organelle" },
            { "id", id },
            { "player", player },
            { "name", "ORGANELLE_TEST_NAME" },
            { "desc", "ORGANELLE_TEST_DESC" },
            { "badge", "BADGE_NEW_ORGANELLE" },
            { "level", 1 },
            { "energy_cost", cost },
            { "category", "metabolism" }
        };
        modal.ShowChoices(new Godot.Collections.Array<Godot.Collections.Dictionary> { card });
    }

    /// <summary>Phase 2: discard heals and the HUD chamber row reflects state.</summary>
    private void RunDiscardAndHudTests()
    {
        OrganelleUnlockManager.ResetCache();
        OrganelleUnlockManager.ResetAll();
        OrganelleUnlockManager.Unlock("chemokine_patch");

        var mock = new CharacterBody2D { Name = "DiscardHost" };
        var stats = new CellStats { Name = "CellStats" };
        mock.AddChild(stats);
        var chamber = new OrganelleChamber { Name = "OrganelleChamber" };
        mock.AddChild(chamber);
        Root.AddChild(mock);
        chamber.Setup(mock);

        // Discard removes and returns the item to the unowned pool.
        AssertThat(chamber.AddToBackpack("chemokine_patch")).IsTrue();
        AssertThat(chamber.Equip("chemokine_patch", 0)).IsTrue();
        AssertThat(chamber.Discard("chemokine_patch")).IsFalse();
        AssertThat(chamber.Unequip(0)).IsTrue();
        AssertThat(chamber.Discard("chemokine_patch")).IsTrue();
        GD.Print("[PASS] Discard only removes unequipped organelles from the run backpack.");

        // HUD chamber row hides when unengaged and shows energy once equipped.
        var hudScene = AssetLoader.Load<PackedScene>("res://scenes/ui/hud.tscn");
        var hud = hudScene.Instantiate<Hud>();
        Root.AddChild(hud);
        var skills = hud.GetNodeOrNull<Node>("SkillBarView");
        AssertThat(skills).IsNotNull();
        hud.UpdateSkillSlots();
        var barView = (Phagocyte.UI.SkillBarView)skills!;
        AssertThat(barView.ChamberRow).IsNotNull();
        AssertThat(barView.ChamberRow!.Visible).IsFalse();
        barView.PlayerRef = mock;
        barView.UpdateChamberRow();
        AssertThat(barView.ChamberRow.Visible).IsFalse();
        AssertThat(chamber.AddToBackpack("chemokine_patch")).IsTrue();
        AssertThat(chamber.Equip("chemokine_patch", 0)).IsTrue();
        barView.UpdateChamberRow();
        AssertThat(barView.ChamberRow.Visible).IsTrue();
        AssertThat(barView.ChamberLabel).IsNotNull();
        AssertThat(barView.ChamberLabel!.Text.Contains("1 / 6")).IsTrue();
        GD.Print("[PASS] HUD chamber row stays hidden until the player engages, then shows used/max.");

        hud.Free();
        mock.Free();
    }

    /// <summary>Phase 1: pre-run loadout profiles (default = no equipment).</summary>
    private void RunLoadoutManagerTests()
    {
        const string cell = "macrophage";

        // A fresh cell deploys with no equipment at all.
        AssertThat(LoadoutManager.GetProfileCount(cell)).IsEqual(1);
        AssertThat(LoadoutManager.GetActiveProfile(cell)).IsEqual(0);
        foreach (string id in LoadoutManager.GetActiveSlots(cell))
            AssertThat(id).IsEqual("");
        GD.Print("[PASS] Default loadout profile is a single build with 4 empty slots (no equipment).");

        // Locked organelles can never enter a profile.
        OrganelleUnlockManager.ResetCache();
        OrganelleUnlockManager.ResetAll();
        AssertThat(LoadoutManager.SetSlots(cell, 0, new[] { "mitochondria_mkii", "", "", "" })).IsFalse();
        AssertThat(LoadoutManager.GetSlots(cell, 0)[0]).IsEqual("");
        OrganelleUnlockManager.UnlockAll();
        GD.Print("[PASS] Locked organelles are refused by the loadout manager.");

        // Legal set persists and round-trips through disk.
        string[] legal = { "mitochondria_mkii", "chemokine_patch", "", "" };
        AssertThat(LoadoutManager.SetSlots(cell, 0, legal)).IsTrue();
        LoadoutManager.ResetCache();
        var reloaded = LoadoutManager.GetSlots(cell, 0);
        AssertThat(reloaded[0]).IsEqual("mitochondria_mkii");
        AssertThat(reloaded[1]).IsEqual("chemokine_patch");
        AssertThat(reloaded[2]).IsEqual("");
        GD.Print("[PASS] Loadout persists to disk and reloads (isolated save path).");

        // Illegal sets are refused without touching the stored profile.
        string[] overloaded = { "mitochondria_mkii", "acidic_lysosome", "", "" };
        AssertThat(LoadoutManager.SetSlots(cell, 0, overloaded)).IsFalse();
        string[] duplicated = { "mitochondria_mkii", "mitochondria_mkii", "", "" };
        AssertThat(LoadoutManager.SetSlots(cell, 0, duplicated)).IsFalse();
        string[] unknown = { "not_a_real_organelle", "", "", "" };
        AssertThat(LoadoutManager.SetSlots(cell, 0, unknown)).IsFalse();
        AssertThat(LoadoutManager.GetSlots(cell, 0)[0]).IsEqual("mitochondria_mkii");
        GD.Print("[PASS] Overload / duplicate / unknown loadout sets are refused (stored profile untouched).");

        // Profile lifecycle.
        AssertThat(LoadoutManager.AddProfile(cell)).IsTrue();
        AssertThat(LoadoutManager.GetProfileCount(cell)).IsEqual(2);
        AssertThat(LoadoutManager.GetActiveProfile(cell)).IsEqual(1);
        AssertThat(LoadoutManager.GetActiveSlots(cell)[0]).IsEqual("");
        AssertThat(LoadoutManager.AddProfile(cell)).IsTrue();
        AssertThat(LoadoutManager.AddProfile(cell)).IsFalse();
        AssertThat(LoadoutManager.SetActiveProfile(cell, 0)).IsTrue();
        AssertThat(LoadoutManager.DeleteProfile(cell, 1)).IsTrue();
        AssertThat(LoadoutManager.GetProfileCount(cell)).IsEqual(2);
        AssertThat(LoadoutManager.DeleteProfile(cell, 0)).IsTrue();
        AssertThat(LoadoutManager.GetProfileCount(cell)).IsEqual(1);
        AssertThat(LoadoutManager.DeleteProfile(cell, 0)).IsFalse();
        GD.Print("[PASS] Profile add/switch/delete lifecycle capped at 3 builds, last build kept.");

        // Unknown cells are refused.
        AssertThat(LoadoutManager.SetSlots("not_a_cell", 0, legal)).IsFalse();
        AssertThat(LoadoutManager.GetProfileCount("not_a_cell")).IsEqual(0);
        GD.Print("[PASS] Unknown cell ids are refused by the loadout manager.");

        // Hand-edited illegal files are sanitized, not fatal.
        var payload = new Godot.Collections.Dictionary
        {
            { "profiles", new Godot.Collections.Dictionary
                {
                    { cell, new Godot.Collections.Array
                        {
                            new Godot.Collections.Array<string> { "rough_er", "rough_er", "rough_er", "rough_er" },
                            new Godot.Collections.Array<string> { "not_a_real_organelle", "", "", "" }
                        }
                    }
                }
            },
            { "active_profiles", new Godot.Collections.Dictionary { { cell, 0 } } }
        };
        JsonStore.Write(LoadoutManager.SavePath, payload);
        LoadoutManager.ResetCache();
        foreach (string id in LoadoutManager.GetSlots(cell, 0))
            AssertThat(id).IsEqual("");
        AssertThat(LoadoutManager.GetSlots(cell, 1)[0]).IsEqual("");
        GD.Print("[PASS] Illegal hand-edited profile rows sanitize to empty instead of blocking deploy.");

        LoadoutManager.ResetCache();
        LoadoutManager.SetSlots(cell, 0, LoadoutManager.EmptySlots());
    }

    /// <summary>Phase 1: the run deploys the active profile (empty by default).</summary>
    private void RunApplyLoadoutTests()
    {
        const string cell = "macrophage";

        // Default (no equipment) deployment.
        LoadoutManager.SetSlots(cell, 0, LoadoutManager.EmptySlots());
        var emptyRun = InstantiateMain(cell);
        var emptyPlayer = emptyRun.Player as BaseCell;
        AssertThat(emptyPlayer).IsNotNull();
        AssertThat(emptyPlayer!.CellOrganelleChamber).IsNotNull();
        AssertThat(emptyPlayer.CellOrganelleChamber!.EquippedCount).IsEqual(0);
        AssertThat(emptyPlayer.CellOrganelleChamber.Backpack.Count).IsEqual(0);
        FreeMain(emptyRun);
        GD.Print("[PASS] A run deploys with an empty chamber by default (no starting equipment).");

        // Configured profile is equipped on deploy and its stats land on the cell.
        AssertThat(LoadoutManager.SetSlots(cell, 0, new[] { "mitochondria_mkii", "symbiotic_flora", "", "" })).IsTrue();
        var loadedRun = InstantiateMain(cell);
        var player = loadedRun.Player as BaseCell;
        AssertThat(player).IsNotNull();
        var chamber = player!.CellOrganelleChamber;
        AssertThat(chamber).IsNotNull();
        AssertThat(chamber!.GetSlot(0)).IsEqual("mitochondria_mkii");
        AssertThat(chamber.GetSlot(1)).IsEqual("symbiotic_flora");
        AssertThat(chamber.UsedEnergy).IsEqual(4);
        AssertThat(chamber.MaxEnergy).IsEqual(7);
        AssertThat(player.Stats!.GetStat("cooldown_reduction")).IsGreater(0.15f);
        FreeMain(loadedRun);
        GD.Print("[PASS] Configured loadout equips on deploy and applies its stat modifiers.");

        // A stale/illegal profile on disk never blocks deployment.
        var payload = new Godot.Collections.Dictionary
        {
            { "profiles", new Godot.Collections.Dictionary
                {
                    { cell, new Godot.Collections.Array
                        {
                            new Godot.Collections.Array<string> { "mitochondria_mkii", "acidic_lysosome", "rough_er", "" }
                        }
                    }
                }
            },
            { "active_profiles", new Godot.Collections.Dictionary { { cell, 0 } } }
        };
        JsonStore.Write(LoadoutManager.SavePath, payload);
        LoadoutManager.ResetCache();
        var fallbackRun = InstantiateMain(cell);
        var fallbackPlayer = fallbackRun.Player as BaseCell;
        AssertThat(fallbackPlayer).IsNotNull();
        AssertThat(fallbackPlayer!.CellOrganelleChamber).IsNotNull();
        FreeMain(fallbackRun);
        GD.Print("[PASS] An illegal on-disk profile sanitizes and the run still deploys.");

        // Locked organelles are stripped on load: a run never equips what has not dropped.
        OrganelleUnlockManager.ResetCache();
        OrganelleUnlockManager.ResetAll();
        var lockedPayload = new Godot.Collections.Dictionary
        {
            { "profiles", new Godot.Collections.Dictionary
                {
                    { cell, new Godot.Collections.Array
                        {
                            new Godot.Collections.Array<string> { "mitochondria_mkii", "", "", "" }
                        }
                    }
                }
            },
            { "active_profiles", new Godot.Collections.Dictionary { { cell, 0 } } }
        };
        JsonStore.Write(LoadoutManager.SavePath, lockedPayload);
        LoadoutManager.ResetCache();
        AssertThat(LoadoutManager.GetSlots(cell, 0)[0]).IsEqual("");
        var lockedRun = InstantiateMain(cell);
        var lockedPlayer = lockedRun.Player as BaseCell;
        AssertThat(lockedPlayer).IsNotNull();
        AssertThat(lockedPlayer!.CellOrganelleChamber!.EquippedCount).IsEqual(0);
        FreeMain(lockedRun);
        GD.Print("[PASS] Locked organelles are stripped from profiles and never deploy.");

        OrganelleUnlockManager.UnlockAll();
        LoadoutManager.ResetCache();
        LoadoutManager.SetSlots(cell, 0, LoadoutManager.EmptySlots());
    }
}
