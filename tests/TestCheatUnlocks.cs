using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Skills;

namespace Phagocyte.Tests;

public partial class TestCheatUnlocks : TestHarness
{
    private static readonly string[] AllClasses = { "macrophage", "ctl", "neutrophil", "b_cell", "dendritic" };
    private static readonly string[] AllMaps = { "acute_wound", "alveolar_space", "hepatic_sinusoid", "gastric_lumen", "blood_brain_barrier" };

    private int _frame = 0;
    private bool _done = false;
    private Main? _main;

    public override void _Initialize()
    {
        Banner("CHEAT UNLOCKS SELF-CHECK");
        IsolateSaves("cheat_unlocks");
    }

    public override bool _Process(double delta)
    {
        if (_done)
            return true;

        _frame++;
        if (_frame == 1)
        {
            ResetAndAssertBaseline("fresh");
            TestCheats.UnlockAllMeta();
            AssertFullUnlock();
            _main = InstantiateMain();
            return false;
        }
        if (_frame < 4)
            return false;

        _done = true;
        AssertThat(_main).IsNotNull();
        var player = _main!.Player as BaseCell;
        AssertThat(player).IsNotNull();

        AssertThat(TestCheats.MaxOutPlayer(_main)).IsTrue();
        AssertThat(player!.CurrentLevel).IsEqual(TestCheats.DefaultRunLevel);
        AssertThat(player.CurrentExp).IsEqual(0.0f);
        AssertThat(player.Stats).IsNotNull();
        AssertThat(player.Stats!.GetStatObj("block")!.BaseValue).IsEqual(0.0f);
        AssertThat(player.Stats.GetStatObj("evasion")!.BaseValue).IsEqual(0.0f);
        GD.Print("[PASS] MaxOutPlayer sets run level and zeroes block/evasion RNG.");

        var sm = player.CellSkillManager;
        AssertThat(sm).IsNotNull();
        int actives = 0;
        foreach (var s in sm!.ActiveSlots)
        {
            if (s == null)
                continue;
            actives++;
            AssertThat(s.Level).IsEqual(s.MaxLevel);
        }
        int passives = 0;
        foreach (var s in sm.PassiveSlots)
        {
            if (s == null)
                continue;
            passives++;
            AssertThat(s.Level).IsEqual(s.MaxLevel);
        }
        AssertThat(actives).IsEqual(SkillManager.MaxActiveSlots);
        AssertThat(passives).IsEqual(SkillManager.MaxPassiveSlots);
        GD.Print("[PASS] MaxOutPlayer fills and maxes 5 actives + 5 passives.");

        var chamber = player.CellOrganelleChamber;
        AssertThat(chamber).IsNotNull();
        foreach (var keyVar in GameManager.OrganelleCatalog.Keys)
            AssertThat(chamber!.Owns(keyVar.AsString())).IsTrue();
        GD.Print("[PASS] MaxOutPlayer unlocks the full organelle vault.");

        AssertThat(player.Health).IsLess(999999.0f);
        GD.Print("[PASS] Godmode defaults to off.");
        AssertThat(TestCheats.MaxOutPlayer(_main, godmode: true)).IsTrue();
        AssertThat(player.Health).IsEqual(999999.0f);
        GD.Print("[PASS] Godmode opt-in makes the player invulnerable.");

        ResetAndAssertBaseline("restored");

        FreeMain(_main);
        ResetRunGlobals();
        RestoreSaves();
        Finish(true, "CHEAT UNLOCKS SELF-CHECK");
        return true;
    }

    private static void ResetAndAssertBaseline(string stage)
    {
        TestCheats.LockToBaseline();
        AssertThat(GameManager.IsClassUnlocked("macrophage")).IsTrue();
        foreach (string cell in AllClasses)
        {
            if (cell == "macrophage")
                continue;
            AssertThat(GameManager.IsClassUnlocked(cell)).IsFalse();
        }
        AssertThat(GameManager.IsMapUnlocked("acute_wound")).IsTrue();
        foreach (string map in AllMaps)
        {
            if (map != "acute_wound")
                AssertThat(GameManager.IsMapUnlocked(map)).IsFalse();
            AssertThat(GameManager.IsMapHardUnlocked(map)).IsFalse();
        }
        AssertThat(OrganelleUnlockManager.UnlockedCount).IsEqual(0);
        AssertThat(PassiveTreeManager.GetCellLevel("macrophage")).IsEqual(PassiveTreeManager.BaseCellLevel);
        AssertThat(PassiveTreeManager.BonusPoints).IsEqual(0);
        GD.Print($"[PASS] Baseline {stage}: fresh profile (macrophage-only, wound-only, vault locked).");
    }

    private static void AssertFullUnlock()
    {
        foreach (string cell in AllClasses)
            AssertThat(GameManager.IsClassUnlocked(cell)).IsTrue();
        foreach (string map in AllMaps)
        {
            AssertThat(GameManager.IsMapUnlocked(map)).IsTrue();
            AssertThat(GameManager.IsMapHardUnlocked(map)).IsTrue();
        }
        AssertThat(AchievementManager.IsEndlessUnlocked()).IsTrue();
        AssertThat(OrganelleUnlockManager.UnlockedCount).IsEqual(GameManager.OrganelleCatalog.Count);
        AssertThat(PassiveTreeManager.GetCellLevel("macrophage")).IsEqual(TestCheats.DefaultMetaTreeLevel);
        AssertThat(PassiveTreeManager.BonusPoints >= TestCheats.DefaultBonusPoints).IsTrue();
        GD.Print("[PASS] UnlockAllMeta unlocks all classes, maps+hard, endless, organelles and tree levels.");
    }
}
