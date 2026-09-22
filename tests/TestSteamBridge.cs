using Godot;
using System;
using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Core;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the reserved Steamworks bridge: safe no-op behavior without the
/// USE_STEAMWORKS symbol, Steam API naming and offline achievement sync.
/// </summary>
[TestSuite]
public partial class TestSteamBridge : TestHarness
{
    private int _frame = 0;
    private bool _done = false;

    public override void _Initialize()
    {
        Banner("STARTING STEAMWORKS BRIDGE RESERVATION VERIFICATION");

        IsolateSaves("steam_bridge");
        AchievementManager.ResetAll();
    }

    public override bool _Process(double delta)
    {
        if (_done)
            return true;

        if (!Gate(ref _frame, 3))
            return false;

        _done = true;
        try
        {
            RunTests();
            Cleanup();
            GD.Print("==================================================================");
            GD.Print(">>> ALL STEAMWORKS BRIDGE TESTS PASSED SUCCESSFULLY! <<<");
            GD.Print("==================================================================");
            Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestSteamBridge threw: ", ex);
            Cleanup();
            Quit(1);
        }
        return true;
    }

    private void RunTests()
    {
        // Without the USE_STEAMWORKS symbol the bridge must be inert
        AssertThat(SteamBridge.IsAvailable).IsFalse();
        SteamBridge.Initialize();
        AssertThat(SteamBridge.IsAvailable).IsFalse();
        SteamBridge.PushUnlock("engulf_20");
        SteamBridge.PushUnlocks(new List<string> { "engulf_20", "devour_50" });
        AssertThat(SteamBridge.PullUnlocked(new List<string> { "engulf_20" }).Count).IsEqual(0);
        SteamBridge.Shutdown();
        GD.Print("[PASS] Steam bridge is a safe no-op when USE_STEAMWORKS is undefined.");

        // Steam API naming mirrors the local ids (docs/achievement.md §3)
        AssertThat(SteamBridge.GetSteamApiName("engulf_20")).IsEqual("ENGULF_20");
        AssertThat(SteamBridge.GetSteamApiName("prpsc_amyloid")).IsEqual("PRPSC_AMYLOID");
        AssertThat(SteamBridge.GetSteamApiName("")).IsEqual("");
        GD.Print("[PASS] Steam API name normalization verified.");

        // Two-way sync degrades to 0 offline and local unlocks still persist
        AssertThat(AchievementManager.SyncWithSteam()).IsEqual(0);
        AssertThat(AchievementManager.Unlock("engulf_20")).IsTrue();
        AssertThat(AchievementManager.IsUnlocked("engulf_20")).IsTrue();
        AssertThat(AchievementManager.IsUnlocked("devour_50")).IsFalse();

        // Persisted unlocks survive a reload (offline catch-up source of truth)
        AchievementManager.SaveToDisk();
        AchievementManager.UnlockedIds.Clear();
        AchievementManager.LoadFromDisk();
        AssertThat(AchievementManager.IsUnlocked("engulf_20")).IsTrue();
        GD.Print("[PASS] Offline persistence and unlock flow work with the bridge disabled.");
    }

    private static void Cleanup()
    {
        AchievementManager.ResetAll();
        RestoreSaves();
    }
}
