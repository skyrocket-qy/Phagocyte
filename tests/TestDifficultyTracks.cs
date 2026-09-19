using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.UI;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the dual-track difficulty system (docs/map.md §2 / TODO module 3):
/// per-organ Normal / Hard selection, Hard spawn modifiers (+40% HP / +20% speed),
/// environment frequency scaling and the unlock gating.
/// </summary>
[TestSuite]
public partial class TestDifficultyTracks : TestHarness
{
    private int _phase = 0;
    private Main? _main = null;

    public override void _Initialize()
    {
        Banner("STARTING DUAL-TRACK DIFFICULTY VERIFICATION");

        IsolateSaves("difficulty");
        RunRecordManager.LoadFromDisk();

        AchievementManager.ResetAll();
        PassiveTreeManager.ResetAll();
        ResetRunGlobals();
    }

    public override bool _Process(double delta)
    {
        try
        {
            switch (_phase)
            {
            case 0:
                _phase++;
                return false;
            case 1:
                RunHardSpawnScalingTests();
                _phase++;
                return false;
            case 2:
                RunHardRunIntegrationTests();
                _phase++;
                return false;
            case 3:
                RunDifficultyToggleTests();
                _phase++;
                return false;
            default:
                Cleanup();
                GD.Print("==================================================================");
                GD.Print(">>> ALL DUAL-TRACK DIFFICULTY TESTS PASSED SUCCESSFULLY! <<<");
                GD.Print("==================================================================");
                Quit(0);
                return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestDifficultyTracks threw: ", ex);
            Cleanup();
            Quit(1);
            return true;
        }
    }

    private void RunHardSpawnScalingTests()
    {
        PathogenSpawner.ConfigureHardMode(false);
        PathogenSpawner.ConfigureOverdrive(false);

        var baseline = PathogenSpawner.CreatePathogen("staph");
        AssertThat(baseline).IsNotNull();
        float baseHp = baseline!.MaxHealth;
        float baseSpeed = baseline.FloatSpeed;
        baseline.Free();

        // Hard: +40% health, +20% move speed.
        PathogenSpawner.ConfigureHardMode(true);
        var hard = PathogenSpawner.CreatePathogen("staph");
        AssertThat(hard).IsNotNull();
        PathogenSpawner.ApplySpawnScaling(hard!, 0.0f);
        AssertThat(hard!.MaxHealth).IsEqualApprox(baseHp * PathogenSpawner.HardHealthMultiplier, 0.01f);
        AssertThat(hard.FloatSpeed).IsEqualApprox(baseSpeed * PathogenSpawner.HardSpeedMultiplier, 0.01f);
        hard.Free();

        // Hard + endless overdrive stack multiplicatively (18:00 cycle = +120% HP).
        PathogenSpawner.ConfigureOverdrive(true);
        var compounded = PathogenSpawner.CreatePathogen("staph");
        AssertThat(compounded).IsNotNull();
        PathogenSpawner.ApplySpawnScaling(compounded!, 1080.0f);
        AssertThat(compounded!.MaxHealth).IsEqualApprox(
            baseHp * PathogenSpawner.HardHealthMultiplier * 2.2f, 0.05f);
        AssertThat(compounded.FloatSpeed).IsEqualApprox(
            baseSpeed * PathogenSpawner.HardSpeedMultiplier * 1.3f, 0.05f);
        compounded.Free();

        PathogenSpawner.ConfigureHardMode(false);
        PathogenSpawner.ConfigureOverdrive(false);
        GD.Print("[PASS] Hard adds +40% HP / +20% speed and stacks with the endless ladder.");
    }

    private void RunHardRunIntegrationTests()
    {
        var main = InstantiateMain(difficulty: RunRecordManager.DifficultyHard);
        _main = main;

        AssertThat(main.IsHardRun).IsTrue();
        AssertThat(main.RunDifficulty).IsEqual(RunRecordManager.DifficultyHard);
        AssertThat(PathogenSpawner.HardMode).IsTrue();
        AssertThat(main.OrganEnvironment).IsNotNull();
        AssertThat(main.OrganEnvironment!.HardMode).IsTrue();

        // Environment hazards run on the +50% frequency timer under Hard.
        var wound = (Phagocyte.Environment.AcuteWoundEnvironment)main.OrganEnvironment;
        AssertThat(wound.HardMode).IsTrue();

        // A Hard clear settles as a Hard record, unlocks its Hard achievement and
        // awards the Hard talent point (docs/map.md §2 / docs/passivetree.md §5.2).
        int bonusBefore = PassiveTreeManager.BonusPoints;
        var player = MakePlayerInvulnerable(main);
        AssertThat(player).IsNotNull();
        ForceVictory(main);
        AssertThat(main.TerminalBossNeutralized).IsTrue();
        AssertThat(main.RunEnded).IsTrue();
        AssertThat(RunRecordManager.GetRunCount()).IsEqual(1);
        AssertThat(RunRecordManager.Records[0]["difficulty"].AsString()).IsEqual(RunRecordManager.DifficultyHard);
        AssertThat(RunRecordManager.Records[0]["result"].AsString()).IsEqual(RunRecordManager.ResultVictory);
        AssertThat(AchievementManager.IsUnlocked("ach_wound_hard_clear")).IsTrue();
        AssertThat(PassiveTreeManager.BonusPoints).IsEqual(bonusBefore + 1);

        GD.Print("[PASS] Hard runs flag the spawner, the environment and settle as a Hard record with rewards.");
        CleanupMain();
        GameManager.SelectedDifficulty = RunRecordManager.DifficultyNormal;
        PathogenSpawner.ConfigureHardMode(false);
    }

    private void RunDifficultyToggleTests()
    {
        GameManager.ResetMapUnlocks();
        GameManager.SelectedDifficulty = RunRecordManager.DifficultyNormal;

        var menu = GD.Load<PackedScene>("res://scenes/ui/main_menu.tscn").Instantiate<MainMenu>();
        Root.AddChild(menu);

        AssertThat(menu.DifficultyToggle).IsNotNull();
        menu.SelectMap("acute_wound");
        AssertThat(menu.DifficultyToggle!.IsItemDisabled(1)).IsTrue();
        AssertThat(menu.DifficultyToggle.TooltipText).IsEqual(menu.Tr("HARD_LOCKED_HINT"));

        // Locked Hard cannot be committed even if the option is forced.
        menu.DifficultyToggle.Selected = 1;
        menu.DifficultyToggle.EmitSignal(OptionButton.SignalName.ItemSelected, 1);
        AssertThat(GameManager.SelectedDifficulty).IsEqual(RunRecordManager.DifficultyNormal);

        // Clearing the organ on Normal unlocks its Hard mode (docs/map.md §2).
        AchievementManager.RecordMapClear("acute_wound");
        menu.SelectMap("acute_wound");
        AssertThat(GameManager.IsMapHardUnlocked("acute_wound")).IsTrue();
        AssertThat(menu.DifficultyToggle.IsItemDisabled(1)).IsFalse();
        AssertThat(menu.DifficultyToggle.TooltipText).IsEqual("");

        menu.DifficultyToggle.Selected = 1;
        menu.DifficultyToggle.EmitSignal(OptionButton.SignalName.ItemSelected, 1);
        AssertThat(GameManager.SelectedDifficulty).IsEqual(RunRecordManager.DifficultyHard);

        // Switching to an organ whose Hard is still locked falls back to Normal.
        menu.SelectMap("alveolar_space");
        AssertThat(menu.DifficultyToggle.IsItemDisabled(1)).IsTrue();
        AssertThat(GameManager.SelectedDifficulty).IsEqual(RunRecordManager.DifficultyNormal);

        menu.QueueFree();
        GD.Print("[PASS] Per-organ Normal/Hard toggle, lock gating and fallback verified.");
    }

    private void CleanupMain()
    {
        FreeMain(_main);
        _main = null;
    }

    private void Cleanup()
    {
        Paused = false;
        CleanupMain();
        ResetRunGlobals();

        AchievementManager.ResetAll();
        PassiveTreeManager.ResetAll();
        RestoreSaves();
    }
}
