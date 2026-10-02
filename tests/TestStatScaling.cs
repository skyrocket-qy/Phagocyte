using Godot;
using Godot.Collections;
using System;
using Game.Core;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Game.Tests;

/// <summary>
/// Engine verification for Vistrace-style stat scaling on top of the
/// (Base + Flat) * (1 + Pct) pool: per-modifier scaling_stat/scale_per
/// with live recompute, plus built-in cross-stat rules. Unscaled behavior
/// must stay bit-identical (exact rollback suites guard it).
/// </summary>
[TestSuite]
public partial class TestStatScaling : TestHarness
{
    private int _frame = 0;
    private bool _done = false;

    public override void _Initialize()
    {
        Banner("STARTING STAT SCALING VERIFICATION");
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
            RunBaselineTests();
            RunScaledMathTests();
            RunLiveRecomputeTests();
            RunRemovalTests();
            RunGuardTests();
            RunRuleTests();
            RunCycleBoundTests();
            RunCapTests();
            RunChamberIntegrationTests();
            RunTreeSplitTests();
            RunProfileTests();
            RunPocoParityTests();
            Finish(true, "ALL STAT SCALING TESTS");
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestStatScaling threw: ", ex);
            Finish(false, "STAT SCALING TESTS");
        }
        return true;
    }

    private static ActorStats NewStats()
    {
        return new ActorStats { Name = "ScaleStats" };
    }

    /// <summary>Unscaled path is untouched: add/remove round-trips exactly.</summary>
    private void RunBaselineTests()
    {
        var stats = NewStats();
        AssertThat(stats.GetStat("damage")).IsEqual(1.0f);
        stats.AddModifier("damage", 0.5f, 0.25f);
        AssertThat(stats.GetStat("damage")).IsEqual(1.875f);
        stats.RemoveModifier("damage", 0.5f, 0.25f);
        AssertThat(stats.GetStat("damage")).IsEqual(1.0f);
        GD.Print("[PASS] Unscaled add/remove round-trips exactly.");
        stats.Free();
    }

    /// <summary>Scaled value = amount * (source / scalePer) into each channel.</summary>
    private void RunScaledMathTests()
    {
        var stats = NewStats();
        // max_health base is 100: armor += 1 * (100 / 50) = 2.
        stats.AddScaledModifier("armor", 1.0f, 0.0f, "max_health", 50.0f);
        AssertThat(stats.GetStat("armor")).IsEqual(2.0f);
        // Pct channel: drop the armor record first for isolation, set armor
        // base 20, damage = 1 * (1 + 0.25 * (20 / 10)) = 1.5.
        stats.RemoveScaledModifier("armor", 1.0f, 0.0f, "max_health", 50.0f);
        stats.SetBase("armor", 20.0f);
        stats.AddScaledModifier("damage", 0.0f, 0.25f, "armor", 10.0f);
        AssertThat(stats.GetStat("damage")).IsEqual(1.5f);
        GD.Print("[PASS] Scaled flat + pct channels resolve against the source stat.");
        stats.Free();
    }

    /// <summary>Source changes propagate live (no stale snapshots).</summary>
    private void RunLiveRecomputeTests()
    {
        var stats = NewStats();
        stats.AddScaledModifier("armor", 1.0f, 0.0f, "max_health", 50.0f);
        AssertThat(stats.GetStat("armor")).IsEqual(2.0f);
        stats.SetBase("max_health", 200.0f);
        AssertThat(stats.GetStat("armor")).IsEqual(4.0f);
        stats.AddModifier("max_health", 100.0f, 0.0f);
        AssertThat(stats.GetStat("armor")).IsEqual(6.0f);
        GD.Print("[PASS] Scaled bonuses recompute live on source base/modifier changes.");
        stats.Free();
    }

    /// <summary>Removing the scaled record restores the exact baseline.</summary>
    private void RunRemovalTests()
    {
        var stats = NewStats();
        stats.AddModifier("armor", 3.0f, 0.0f);
        stats.AddScaledModifier("armor", 1.0f, 0.0f, "max_health", 50.0f);
        AssertThat(stats.GetStat("armor")).IsEqual(5.0f);
        stats.RemoveScaledModifier("armor", 1.0f, 0.0f, "max_health", 50.0f);
        AssertThat(stats.GetStat("armor")).IsEqual(3.0f);
        stats.RemoveModifier("armor", 3.0f, 0.0f);
        AssertThat(stats.GetStat("armor")).IsEqual(0.0f);
        // Unknown removal is silent (mirrors RemoveModifier).
        stats.RemoveScaledModifier("armor", 9.0f, 0.0f, "max_health", 50.0f);
        AssertThat(stats.GetStat("armor")).IsEqual(0.0f);
        GD.Print("[PASS] Scaled removal is exact; unknown removal is silent.");
        stats.Free();
    }

    /// <summary>Bad scaling input warns and degrades safely, never throws.</summary>
    private void RunGuardTests()
    {
        var stats = NewStats();
        stats.AddScaledModifier("no_such_stat", 1.0f, 0.0f, "max_health", 50.0f);
        stats.AddScaledModifier("armor", 1.0f, 0.0f, "no_such_source", 50.0f);
        AssertThat(stats.GetStat("armor")).IsEqual(0.0f);
        // Non-positive scalePer falls back to 1.0 (Vistrace guard).
        stats.AddScaledModifier("armor", 1.0f, 0.0f, "max_health", 0.0f);
        AssertThat(stats.GetStat("armor")).IsEqual(100.0f);
        stats.RemoveScaledModifier("armor", 1.0f, 0.0f, "max_health", 1.0f);
        AssertThat(stats.GetStat("armor")).IsEqual(0.0f);
        GD.Print("[PASS] Unknown stats ignored; non-positive scalePer falls back to 1.0.");
        stats.Free();
    }

    /// <summary>Built-in rules contribute flat ratio * source, removable exactly.</summary>
    private void RunRuleTests()
    {
        var stats = NewStats();
        stats.AddStatRule("health_regen", "max_health", 0.5f);
        AssertThat(stats.GetStat("health_regen")).IsEqual(50.0f);
        stats.SetBase("max_health", 150.0f);
        AssertThat(stats.GetStat("health_regen")).IsEqual(75.0f);
        stats.RemoveStatRule("health_regen", "max_health", 0.5f);
        AssertThat(stats.GetStat("health_regen")).IsEqual(0.0f);
        // Unknown rule endpoints warn and are ignored.
        stats.AddStatRule("health_regen", "no_such_source", 0.5f);
        AssertThat(stats.GetStat("health_regen")).IsEqual(0.0f);
        GD.Print("[PASS] Cross-stat rules apply live and remove exactly.");
        stats.Free();
    }

    /// <summary>Source cycles terminate (bounded fixed point, no hang).</summary>
    private void RunCycleBoundTests()
    {
        var stats = NewStats();
        stats.AddScaledModifier("armor", 1.0f, 0.0f, "block", 1.0f);
        stats.AddScaledModifier("block", 1.0f, 0.0f, "armor", 1.0f);
        float armor = stats.GetStat("armor");
        float block = stats.GetStat("block");
        AssertThat(float.IsFinite(armor)).IsTrue();
        AssertThat(float.IsFinite(block)).IsTrue();
        stats.ClearScaled();
        AssertThat(stats.GetStat("armor")).IsEqual(0.0f);
        AssertThat(stats.GetStat("block")).IsEqual(0.0f);
        GD.Print("[PASS] Scaling cycles terminate with finite values; ClearScaled resets.");
        stats.Free();
    }

    /// <summary>Existing caps still clamp scaled results.</summary>
    private void RunCapTests()
    {
        var stats = NewStats();
        stats.AddScaledModifier("life_steal", 1.0f, 0.0f, "max_health", 100.0f);
        AssertThat(stats.GetStat("life_steal")).IsEqual(0.2f);
        stats.AddScaledModifier("cooldown_reduction", 1.0f, 0.0f, "max_health", 100.0f);
        AssertThat(stats.GetStat("cooldown_reduction")).IsEqual(0.75f);
        GD.Print("[PASS] Caps clamp scaled results (life_steal 0.20, CDR 0.75).");
        stats.Free();
    }

    /// <summary>Chamber equip/unequip routes scaled entries end to end.</summary>
    private void RunChamberIntegrationTests()
    {
        const string testId = "__test_scaled_gear";
        var mods = new Array<Dictionary>
        {
            new Dictionary
            {
                { "stat", "armor" },
                { "value", 1.0f },
                { "unit", "flat" },
                { "scaling_stat", "max_health" },
                { "scale_per", 50.0f }
            }
        };
        GameManager.EquipmentCatalog[testId] = new Dictionary
        {
            { "id", testId },
            { "category", "metabolism" },
            { "energy_cost", 1 },
            { "max_copies", 1 },
            { "modifiers", mods },
            { "drawback", new Array<Dictionary>() },
            { "name_key", "GEAR_TEST_NAME" },
            { "desc_key", "GEAR_TEST_DESC" },
            { "bio_key", "GEAR_TEST_BIO" },
            { "icon", "🧪" },
            { "image_path", AssetPaths.GearIcon(testId) }
        };

        var mock = new CharacterBody2D { Name = "ScaleHost" };
        var stats = new ActorStats { Name = "ActorStats" };
        mock.AddChild(stats);
        var chamber = new EquipmentChamber { Name = "EquipmentChamber" };
        mock.AddChild(chamber);
        chamber.Setup(mock);
        try
        {
            AssertThat(EquipmentUnlockManager.Unlock(testId)).IsTrue();
            AssertThat(chamber.AddToBackpack(testId)).IsTrue();
            AssertThat(chamber.Equip(testId, 0)).IsTrue();
            // max_health base 100: armor = 1 * (100 / 50) = 2.
            AssertThat(stats.GetStat("armor")).IsEqual(2.0f);
            AssertThat(chamber.Unequip(0)).IsTrue();
            AssertThat(stats.GetStat("armor")).IsEqual(0.0f);
            GD.Print("[PASS] Chamber equip/unequip routes scaled entries with exact rollback.");
        }
        finally
        {
            GameManager.EquipmentCatalog.Remove(testId);
            EquipmentUnlockManager.ResetCache();
            EquipmentUnlockManager.ResetAll();
            mock.QueueFree();
        }
    }

    /// <summary>Tree split keeps the unscaled contract; scaled split carries source.</summary>
    private void RunTreeSplitTests()
    {
        var plain = new PassiveTreeManager.TreeStatModifier("armor", 2.0f, PassiveTreeManager.TreeModifierUnit.Flat);
        var (flat, pct) = PassiveTreeManager.SplitModifier(plain);
        AssertThat(flat).IsEqual(2.0f);
        AssertThat(pct).IsEqual(0.0f);
        var split = PassiveTreeManager.SplitScaledModifier(plain);
        AssertThat(split.HasScaling).IsFalse();

        var scaled = new PassiveTreeManager.TreeStatModifier("armor", 2.0f, PassiveTreeManager.TreeModifierUnit.Flat, "max_health", 50.0f);
        var s2 = PassiveTreeManager.SplitScaledModifier(scaled);
        AssertThat(s2.Flat).IsEqual(2.0f);
        AssertThat(s2.ScalingStat).IsEqual("max_health");
        AssertThat(s2.ScalePer).IsEqual(50.0f);
        AssertThat(s2.HasScaling).IsTrue();
        GD.Print("[PASS] TreeStatModifier split covers plain and scaled entries.");
    }

    /// <summary>Subset schemas register only their keys; unknown keys fail safe.</summary>
    private void RunProfileTests()
    {
        var enemy = new StatBlock(StatProfiles.Enemy);
        AssertThat(enemy.HasStat("max_health")).IsTrue();
        AssertThat(enemy.HasStat("armor")).IsTrue();
        AssertThat(enemy.HasStat("move_speed")).IsTrue();
        AssertThat(enemy.HasStat("damage")).IsFalse();
        AssertThat(enemy.GetStat("damage")).IsEqual(0.0f);
        enemy.AddModifier("damage", 1.0f, 0.0f);
        AssertThat(enemy.GetStat("damage")).IsEqual(0.0f);
        AssertThat(enemy.GetStat("max_health")).IsEqual(100.0f);

        var minion = new StatBlock(StatProfiles.Minion);
        AssertThat(minion.HasStat("armor")).IsFalse();
        AssertThat(minion.HasStat("move_speed")).IsTrue();

        var full = new StatBlock();
        AssertThat(full.HasStat("magnet")).IsTrue();
        GD.Print("[PASS] Subset schemas register only their keys; unknown keys fail safe.");
    }

    /// <summary>POCO/node parity: same mutation sequence, identical finals.</summary>
    private void RunPocoParityTests()
    {
        var block = new StatBlock();
        var node = NewStats();
        try
        {
            block.AddModifier("damage", 0.5f, 0.25f);
            node.AddModifier("damage", 0.5f, 0.25f);
            block.AddScaledModifier("armor", 1.0f, 0.0f, "max_health", 50.0f);
            node.AddScaledModifier("armor", 1.0f, 0.0f, "max_health", 50.0f);
            block.AddStatRule("health_regen", "max_health", 0.5f);
            node.AddStatRule("health_regen", "max_health", 0.5f);
            block.SetBase("max_health", 140.0f);
            node.SetBase("max_health", 140.0f);
            // The POCO never recomputes on its own (the Node adapter does it
            // in its change notification); the owner recomputes explicitly.
            block.RecomputeScaled();
            foreach (string key in StatProfiles.Full)
                AssertThat(block.GetStat(key)).IsEqual(node.GetStat(key));
            AssertThat(block.RemoveScaledModifier("armor", 1.0f, 0.0f, "max_health", 50.0f)).IsTrue();
            AssertThat(node.RemoveScaledModifier("armor", 1.0f, 0.0f, "max_health", 50.0f)).IsTrue();
            block.RecomputeScaled();
            AssertThat(block.GetStat("armor")).IsEqual(node.GetStat("armor"));
            GD.Print("[PASS] StatBlock POCO and ActorStats node compute identical finals.");
        }
        finally
        {
            node.Free();
        }
    }
}
