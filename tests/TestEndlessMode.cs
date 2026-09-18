using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the Endless Cytokine Storm entry (docs/endgame.md §2-3):
/// Hard-clear unlock gate, no forced settlement past 15:00, terminal boss
/// neutralization continuing the run, and membrane rupture as the only end.
/// </summary>
[TestSuite]
public partial class TestEndlessMode : SceneTree
{
    private const string TestRecordsPath = "user://test_endless_records.json";
    private const string TestAchievementsPath = "user://test_endless_achievements.json";

    private int _phase = 0;
    private Main? _main = null;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING ENDLESS CYTOKINE STORM VERIFICATION <<<");
        GD.Print("==================================================================");

        // Isolate persistence from the player's real saves
        RunRecordManager.SavePath = TestRecordsPath;
        if (FileAccess.FileExists(TestRecordsPath))
            DirAccess.RemoveAbsolute(TestRecordsPath);
        RunRecordManager.LoadFromDisk();

        AchievementManager.SavePath = TestAchievementsPath;
        if (FileAccess.FileExists(TestAchievementsPath))
            DirAccess.RemoveAbsolute(TestAchievementsPath);

        GameManager.EndlessMode = false;
    }

    public override bool _Process(double delta)
    {
        try
        {
            switch (_phase)
            {
            case 0:
                _phase++;
                return false; // let the scene tree settle
            case 1:
                RunUnlockGateTests();
                _phase++;
                return false;
            case 2:
                RunOverdriveLadderTests();
                _phase++;
                return false;
            case 3:
                RunEndlessRunIntegrationTests();
                _phase++;
                return false;
            default:
                Cleanup();
                GD.Print("==================================================================");
                GD.Print(">>> ALL ENDLESS CYTOKINE STORM TESTS PASSED SUCCESSFULLY! <<<");
                GD.Print("==================================================================");
                Quit(0);
                return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestEndlessMode threw: ", ex);
            Cleanup();
            Quit(1);
            return true;
        }
    }

    private void RunUnlockGateTests()
    {
        AchievementManager.ResetAll();

        AssertThat(GameManager.IsEndlessAvailable()).IsFalse();
        AssertThat(AchievementManager.IsEndlessUnlocked()).IsFalse();

        // A locked entry must refuse to launch and must not flip the run flag
        AssertThat(GameManager.StartEndlessGame(this)).IsFalse();
        AssertThat(GameManager.EndlessMode).IsFalse();

        // Clearing any organ on Hard unlocks the entry (docs/endgame.md §2)
        AchievementManager.RecordMapClear("acute_wound", true);
        AssertThat(AchievementManager.IsEndlessUnlocked()).IsTrue();
        AssertThat(GameManager.IsEndlessAvailable()).IsTrue();

        GD.Print("[PASS] Endless entry stays locked until a Hard clear and unlocks afterwards.");
    }

    private void RunOverdriveLadderTests()
    {
        // Cycle boundaries: 15:00-18:00, 18:00-21:00, 21:00-24:00, 24:00-27:00, 27:00+
        AssertThat(PathogenSpawner.GetOverdriveCycle(899.9f)).IsEqual(0);
        AssertThat(PathogenSpawner.GetOverdriveCycle(900.0f)).IsEqual(1);
        AssertThat(PathogenSpawner.GetOverdriveCycle(1079.9f)).IsEqual(1);
        AssertThat(PathogenSpawner.GetOverdriveCycle(1080.0f)).IsEqual(2);
        AssertThat(PathogenSpawner.GetOverdriveCycle(1260.0f)).IsEqual(3);
        AssertThat(PathogenSpawner.GetOverdriveCycle(1440.0f)).IsEqual(4);
        AssertThat(PathogenSpawner.GetOverdriveCycle(1620.0f)).IsEqual(5);

        // HP ladder: +50% / +120% / +220% / +360%, then exponential (×2 per extra cycle)
        AssertThat(PathogenSpawner.GetOverdriveHealthMultiplier(1000.0f)).IsEqualApprox(1.5f, 0.001f);
        AssertThat(PathogenSpawner.GetOverdriveHealthMultiplier(1100.0f)).IsEqualApprox(2.2f, 0.001f);
        AssertThat(PathogenSpawner.GetOverdriveHealthMultiplier(1300.0f)).IsEqualApprox(3.2f, 0.001f);
        AssertThat(PathogenSpawner.GetOverdriveHealthMultiplier(1500.0f)).IsEqualApprox(4.6f, 0.001f);
        AssertThat(PathogenSpawner.GetOverdriveHealthMultiplier(1700.0f)).IsEqualApprox(9.2f, 0.001f);
        AssertThat(PathogenSpawner.GetOverdriveHealthMultiplier(1900.0f)).IsEqualApprox(18.4f, 0.001f);

        // Speed ladder: +15% / +30% / +50% / +70%, capped at +100%
        AssertThat(PathogenSpawner.GetOverdriveSpeedMultiplier(1000.0f)).IsEqualApprox(1.15f, 0.001f);
        AssertThat(PathogenSpawner.GetOverdriveSpeedMultiplier(1100.0f)).IsEqualApprox(1.30f, 0.001f);
        AssertThat(PathogenSpawner.GetOverdriveSpeedMultiplier(1300.0f)).IsEqualApprox(1.50f, 0.001f);
        AssertThat(PathogenSpawner.GetOverdriveSpeedMultiplier(1500.0f)).IsEqualApprox(1.70f, 0.001f);
        AssertThat(PathogenSpawner.GetOverdriveSpeedMultiplier(1700.0f)).IsEqualApprox(2.00f, 0.001f);
        AssertThat(PathogenSpawner.GetOverdriveSpeedMultiplier(2600.0f)).IsEqualApprox(2.00f, 0.001f);

        // Endless screen cap constant (docs/endgame.md §3.4)
        AssertThat(PathogenSpawner.MaxActiveEndless).IsEqual(500);

        // Scaling is gated on the overdrive flag and uses tier 2 (+120% HP) at 18:00
        PathogenSpawner.ConfigureOverdrive(false);
        var unscaled = PathogenSpawner.CreatePathogen("staph");
        AssertThat(unscaled).IsNotNull();
        float baselineHp = unscaled!.MaxHealth;
        PathogenSpawner.ApplyOverdriveScaling(unscaled, 1080.0f);
        AssertThat(unscaled.MaxHealth).IsEqualApprox(baselineHp, 0.001f);
        unscaled.Free();

        PathogenSpawner.ConfigureOverdrive(true);
        var scaled = PathogenSpawner.CreatePathogen("staph");
        AssertThat(scaled).IsNotNull();
        float scaledBaseHp = scaled!.MaxHealth;
        PathogenSpawner.ApplyOverdriveScaling(scaled, 1080.0f);
        AssertThat(scaled.MaxHealth).IsEqualApprox(scaledBaseHp * 2.2f, 0.01f);
        scaled.Free();
        PathogenSpawner.ConfigureOverdrive(false);

        GD.Print("[PASS] Overdrive ladder cycles, exponential terminal tier, speed cap and 500 screen cap verified.");
    }

    private void RunEndlessRunIntegrationTests()
    {
        GameManager.SelectedClass = "macrophage";
        GameManager.SelectedMap = "acute_wound";
        GameManager.SelectedDifficulty = RunRecordManager.DifficultyHard;
        GameManager.EndlessMode = true;

        var main = GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>();
        _main = main;
        Root.AddChild(main);
        main.SetPhysicsProcess(false);

        AssertThat(main.IsEndlessRun).IsTrue();
        AssertThat(main.IsHardRun).IsTrue();
        AssertThat(main.HudNode).IsNotNull();
        AssertThat(main.HudNode!.EndlessMode).IsTrue();

        // Crossing 15:00 starts the boss lockdown without any forced settlement
        main.EnvironmentTime = main.RunGoalSeconds - 0.01f;
        main._PhysicsProcess(0.02f);
        AssertThat(main.BossLockdownActive).IsTrue();
        AssertThat(main.TerminalBoss).IsNotNull();
        AssertThat(main.RunEnded).IsFalse();
        AssertThat(main.OverdriveCycle).IsEqual(1);
        AssertThat(main.OverdriveHealthMultiplier).IsEqualApprox(1.5f, 0.001f);
        AssertThat(main.ActiveScreenCap).IsEqual(PathogenSpawner.MaxActiveEndless);

        // Neutralizing the primary boss continues the overdrive instead of ending the run
        main.TerminalBoss!.TakeDamage(9999999.0f);
        AssertThat(main.TerminalBossNeutralized).IsTrue();
        AssertThat(main.RunEnded).IsFalse();
        AssertThat(main.BossLockdownActive).IsFalse();
        AssertThat(RunRecordManager.GetRunCount()).IsEqual(0);

        // The uncapped timeline keeps rolling past the standard clear (15:01 / 18:00 / 24:00)
        main.EnvironmentTime = 1080.1f;
        main._PhysicsProcess(0.02f);
        AssertThat(main.RunEnded).IsFalse();
        AssertThat(main.OverdriveCycle).IsEqual(2);
        AssertThat(main.OverdriveHealthMultiplier).IsEqualApprox(2.2f, 0.001f);
        AssertThat(main.OverdriveSpeedMultiplier).IsEqualApprox(1.30f, 0.001f);

        // 24:00 flood tide: safe radius starts shrinking and burns outside the zone
        main.EnvironmentTime = 1440.1f;
        main._PhysicsProcess(0.02f);
        AssertThat(main.AcidSafeRadius).IsGreater(800.0f);

        main.EnvironmentTime = 1620.0f;
        var player = main.GetNodeOrNull<BaseCell>("Macrophage");
        AssertThat(player).IsNotNull();
        player!.GlobalPosition = new Vector2(1500.0f, 0.0f);
        float hpBeforeTide = player.Health;
        main._PhysicsProcess(1.2f); // one full acid tick outside the safe zone
        AssertThat(player.Health).IsLess(hpBeforeTide);
        player.GlobalPosition = Vector2.Zero;

        // Guard: an endless run can never settle as a victory
        main.EndRun(true, RunRecordManager.CauseSpecificNeutralization);
        AssertThat(main.RunEnded).IsFalse();
        AssertThat(RunRecordManager.GetRunCount()).IsEqual(0);

        // Only membrane rupture ends an endless overdrive
        player.TakeDamage(999999.0f);
        AssertThat(main.RunEnded).IsTrue();
        AssertThat(RunRecordManager.GetRunCount()).IsEqual(1);
        AssertThat(RunRecordManager.Records[0]["result"].AsString()).IsEqual(RunRecordManager.ResultDefeat);
        AssertThat(RunRecordManager.Records[0]["cause"].AsString()).IsEqual(RunRecordManager.CauseMembraneRupture);
        AssertThat(RunRecordManager.Records[0]["difficulty"].AsString()).IsEqual(RunRecordManager.DifficultyHard);
        AssertThat(RunRecordManager.Records[0]["endless"].AsBool()).IsTrue();
        AssertThat(RunRecordManager.Records[0]["victory_criteria"].AsGodotDictionary()["met"].AsBool()).IsFalse();

        GD.Print("[PASS] Endless overdrive survives 15:00 and only settles on membrane rupture.");
    }

    private void Cleanup()
    {
        Paused = false;
        GameManager.EndlessMode = false;
        PathogenSpawner.ConfigureOverdrive(false);

        if (_main != null && IsInstanceValid(_main))
        {
            if (_main.GetParent() != null)
                _main.GetParent().RemoveChild(_main);
            _main.Free();
            _main = null;
        }

        if (FileAccess.FileExists(TestRecordsPath))
            DirAccess.RemoveAbsolute(TestRecordsPath);
        if (FileAccess.FileExists(TestAchievementsPath))
            DirAccess.RemoveAbsolute(TestAchievementsPath);

        RunRecordManager.SavePath = "";
        AchievementManager.SavePath = "";
    }
}
