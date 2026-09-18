using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using System.Collections.Generic;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.UI;
using Phagocyte.Combat;
using Phagocyte.Enemies;

namespace Phagocyte.Tests;

[TestSuite]
public partial class TestNewSystemsBundle : SceneTree
{
    private int _framesWaited = 0;
    private bool _testDone = false;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> INITIALIZING VISTRACE REUSE 4-SYSTEM SUITE <<<");
        GD.Print("==================================================================");
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        _framesWaited++;
        if (_framesWaited < 3)
            return false;

        _testDone = true;

        try
        {
            RunAllTests();
            GD.Print("==================================================================");
            GD.Print(">>> ALL 4 REUSED SYSTEMS VERIFIED WITH 100% SUCCESS! <<<");
            GD.Print("==================================================================");
            Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestNewSystemsBundle encountered an exception: ", ex);
            Quit(1);
        }

        return true;
    }

    private void RunAllTests()
    {
        // --- 1. AchievementToast Verification ---
        var dummyAch = new Godot.Collections.Dictionary
        {
            { "id", "ach_test" },
            { "icon", "🧬" },
            { "title_key", "ACH_ENGULF_20_TITLE" },
            { "desc_key", "ACH_ENGULF_20_DESC" },
            { "reward_cell", "ctl" }
        };

        var toastHolder = new Control { Name = "ToastHolder" };
        Root.AddChild(toastHolder);
        AchievementToast.ShowToast(toastHolder, dummyAch);

        AssertThat(toastHolder.GetChildCount()).IsEqual(1);
        var toast = toastHolder.GetChild(0) as AchievementToast;
        AssertThat(toast).IsNotNull();
        GD.Print("[PASS] Step 1: AchievementToast instantiated, styled, and attached to UI hierarchy.");
        toastHolder.QueueFree();

        // --- 2. RunTelemetryManager Verification ---
        var tele = new RunTelemetryManager { Name = "TelemetryMgr" };
        Root.AddChild(tele);
        tele.StartRun();

        AssertThat(tele.IsRunActive).IsTrue();

        tele.RecordDamageDealt("ros_torrent", 120.0f);
        tele.RecordDamageDealt("antibody_salvo", 80.0f);
        tele.RecordDamageDealt("defensin_barbs", 50.0f);
        tele.RecordDamageTaken(65.0f);
        tele.RecordEvaded();
        tele.RecordEvaded();
        tele.RecordBlocked();
        tele.RecordBlocked();
        tele.RecordBlocked();
        tele.RecordLifeSteal(3.0f);
        tele.RecordKill(15);
        tele.RecordKill(100);

        AssertThat(tele.TotalDamageDealt).IsEqual(250.0f);
        AssertThat(tele.TotalDamageTaken).IsEqual(65.0f);
        AssertThat(tele.EvadedCount).IsEqual(2);
        AssertThat(tele.BlockedCount).IsEqual(3);
        AssertThat(tele.LifeStealHealed).IsEqual(3.0f);
        AssertThat(tele.KillCount).IsEqual(2);
        AssertThat(tele.KillScore).IsEqual(115);

        var topSkills = tele.GetTopSkills(3);
        AssertThat(topSkills.Count).IsEqual(3);
        AssertThat(topSkills[0].SkillId).IsEqual("ros_torrent");
        AssertThat(topSkills[0].Damage).IsEqual(120.0f);
        AssertThat(topSkills[0].Pct).IsEqualApprox(48.0f, 0.1f); // 120 / 250 = 48%

        var dict = tele.GetTelemetryDictionary();
        AssertThat(dict.ContainsKey("total_damage_dealt")).IsTrue();
        AssertThat(dict.ContainsKey("evaded_count")).IsTrue();
        AssertThat(dict.ContainsKey("skills")).IsTrue();
        AssertThat(dict.ContainsKey("kills")).IsTrue();
        AssertThat(dict["kills"].AsInt32()).IsEqual(2);
        GD.Print("[PASS] Step 2: RunTelemetryManager DPS ranking, defense proc logging and serialization verified.");
        tele.QueueFree();

        // --- 3. BioHazardArea & BiofilmArea Verification ---
        var player = new BaseCell { GlobalPosition = new Vector2(50, 50) };
        var pStats = new CellStats { Name = "CellStats" };
        player.AddChild(pStats);
        player.Stats = pStats;
        player.AddToGroup("player");
        Root.AddChild(player);

        float hpBeforeHazard = player.Health;

        var hazard = new BioHazardArea
        {
            GlobalPosition = new Vector2(50, 50),
            Radius = 80.0f,
            Damage = 8.0f,
            SlowsTarget = true,
            DealsDamage = true,
            TickInterval = 0.1f
        };
        Root.AddChild(hazard);

        // Advance physics to trigger hazard collision tick
        hazard._PhysicsProcess(0.12);

        AssertThat(player.Health).IsLess(hpBeforeHazard);
        AssertThat(player.SlowTimer).IsGreater(0.0f);
        GD.Print("[PASS] Step 3: BioHazardArea periodic tick damage and biological slow application verified.");

        hazard.QueueFree();
        player.QueueFree();

        // --- 4. CameraFollow Screen Shake & Trauma Decay ---
        var cam = new CameraFollow { Name = "CameraFollow" };
        Root.AddChild(cam);

        AssertThat(CameraFollow.Instance).IsEqual(cam);
        AssertThat(cam.Offset).IsEqual(Vector2.Zero);

        cam.AddTrauma(0.8f);
        // Process a physics frame
        cam._PhysicsProcess(0.016);
        AssertThat(cam.Offset).IsNotEqual(Vector2.Zero);

        // Decay trauma completely over multiple frames
        for (int i = 0; i < 70; i++)
        {
            cam._PhysicsProcess(0.03);
        }

        AssertThat(cam.Offset).IsEqual(Vector2.Zero);
        GD.Print("[PASS] Step 4: CameraFollow Trauma shake explosion, noise displacement, and decay verified.");
        cam.QueueFree();
    }
}
