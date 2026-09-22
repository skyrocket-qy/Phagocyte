using Godot;
using System;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;

namespace Phagocyte.Tests;

/// <summary>
/// Shared scaffolding for the headless SceneTree test suites: frame-gated
/// execution, isolated save paths, invulnerable-player setup and the
/// boss-kill settlement sequence.
/// </summary>
public abstract partial class TestHarness : SceneTree
{
    private static readonly System.Collections.Generic.List<string> _isolatedPaths = new();

    /// <summary>
    /// Every suite extending the harness gets isolated save paths before it runs,
    /// and those files are cleaned up when the suite exits, so test runs can never
    /// touch the player's real profile.
    /// </summary>
    protected TestHarness()
    {
        string tag = GetType().Name.Replace("Test", "").ToLowerInvariant();
        IsolateSaves(string.IsNullOrEmpty(tag) ? "suite" : tag);
    }

    public override void _Finalize()
    {
        RestoreSaves();
    }

    /// <summary>
    /// Simple frame gate: call once per _Process; returns true after the given
    /// number of frames have elapsed.
    /// </summary>
    protected static bool Gate(ref int frame, int settleFrames)
    {
        frame++;
        return frame >= settleFrames;
    }

    // ------------------------------------------------------------------
    // Save isolation
    // ------------------------------------------------------------------

    /// <summary>
    /// Redirects the persistent manager saves to isolated user://test_* files and
    /// deletes any leftovers from a previous run. Called automatically by the
    /// constructor; call again from _Initialize only to choose a custom tag.
    /// </summary>
    protected static void IsolateSaves(string tag)
    {
        // Drop any previously tracked isolated files, then register the new ones.
        DeleteIsolatedFiles();

        PassiveTreeManager.SavePath = $"user://test_{tag}_tree.json";
        AchievementManager.SavePath = $"user://test_{tag}_achievements.json";
        RunRecordManager.SavePath = $"user://test_{tag}_records.json";
        SettingsManager.SavePath = $"user://test_{tag}_settings.json";
        LoadoutManager.SavePath = $"user://test_{tag}_loadouts.json";
        OrganelleUnlockManager.SavePath = $"user://test_{tag}_organelle_unlocks.json";

        // Clear leftovers from crashed earlier runs with the same tag.
        DeleteIfExists(PassiveTreeManager.SavePath);
        DeleteIfExists(AchievementManager.SavePath);
        DeleteIfExists(RunRecordManager.SavePath);
        DeleteIfExists(SettingsManager.SavePath);
        DeleteIfExists(LoadoutManager.SavePath);
        DeleteIfExists(OrganelleUnlockManager.SavePath);

        _isolatedPaths.Add(PassiveTreeManager.SavePath);
        _isolatedPaths.Add(AchievementManager.SavePath);
        _isolatedPaths.Add(RunRecordManager.SavePath);
        _isolatedPaths.Add(SettingsManager.SavePath);
        _isolatedPaths.Add(LoadoutManager.SavePath);
        _isolatedPaths.Add(OrganelleUnlockManager.SavePath);

        // Determinism: organelle drops must never fire unscripted in a suite.
        OrganelleUnlockManager.DropsEnabled = false;
    }

    /// <summary>Restores the real save paths and removes the isolated files.</summary>
    protected static void RestoreSaves()
    {
        DeleteIsolatedFiles();
        PassiveTreeManager.SavePath = "";
        AchievementManager.SavePath = "";
        RunRecordManager.SavePath = "";
        SettingsManager.SavePath = "";
        LoadoutManager.SavePath = "";
        OrganelleUnlockManager.SavePath = "";
        LoadoutManager.ResetCache();
        OrganelleUnlockManager.ResetCache();
        OrganelleUnlockManager.DropsEnabled = true;
        OrganelleUnlockManager.DropChance = OrganelleUnlockManager.DefaultDropChance;
    }

    private static void DeleteIsolatedFiles()
    {
        foreach (string path in _isolatedPaths)
            DeleteIfExists(path);
        _isolatedPaths.Clear();
    }

    protected static void DeleteIfExists(string path)
    {
        if (!string.IsNullOrEmpty(path) && FileAccess.FileExists(path))
            DirAccess.RemoveAbsolute(path);
    }

    // ------------------------------------------------------------------
    // Run setup / settlement
    // ------------------------------------------------------------------

    /// <summary>
    /// Instantiates the main gameplay scene with physics disabled, for suites that
    /// drive Main manually.
    /// </summary>
    protected Main InstantiateMain(string classId = "macrophage", string mapId = "acute_wound",
        string difficulty = RunRecordManager.DifficultyNormal, bool endless = false)
    {
        GameManager.SelectedClass = classId;
        GameManager.SelectedMap = mapId;
        GameManager.SelectedDifficulty = difficulty;
        GameManager.EndlessMode = endless;

        var main = AssetLoader.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>();
        Root.AddChild(main);
        main.SetPhysicsProcess(false);
        return main;
    }

    /// <summary>Removes a Main instance created by <see cref="InstantiateMain"/>.</summary>
    protected static void FreeMain(Main? main)
    {
        if (main != null && IsInstanceValid(main))
        {
            if (main.GetParent() != null)
                main.GetParent().RemoveChild(main);
            main.Free();
        }
    }

    /// <summary>Makes the run's player unkillable so settlement tests never die early.</summary>
    protected static BaseCell? MakePlayerInvulnerable(Main main)
    {
        var player = main.Player as BaseCell;
        if (player == null)
            return null;

        player.Stats!.SetBase("max_health", 999999.0f);
        player.Health = 999999.0f;
        return player;
    }

    /// <summary>
    /// Drives the run past the clear threshold, kills the terminal boss and returns
    /// once the settlement has been recorded.
    /// </summary>
    protected static void ForceVictory(Main main)
    {
        main.EnvironmentTime = 900.1f;
        main._PhysicsProcess(0.02f);
        main.TerminalBoss?.TakeDamage(9999999.0f);
    }

    /// <summary>Resets the run-scoped global flags the harness suites toggle.</summary>
    protected static void ResetRunGlobals()
    {
        GameManager.SelectedDifficulty = RunRecordManager.DifficultyNormal;
        GameManager.SelectedMap = "acute_wound";
        GameManager.EndlessMode = false;
        PathogenSpawner.Reset();
    }

    /// <summary>Prints the standard suite banner.</summary>
    protected static void Banner(string title)
    {
        GD.Print("==================================================================");
        GD.Print($">>> {title} <<<");
        GD.Print("==================================================================");
    }

    /// <summary>
    /// Output directory for the screenshot suites: PHAGOCYTE_CAPTURE_DIR when
    /// set, otherwise user://captures (no absolute developer paths).
    /// </summary>
    protected static string CaptureDir
    {
        get
        {
            string dir = OS.GetEnvironment("PHAGOCYTE_CAPTURE_DIR");
            if (string.IsNullOrEmpty(dir))
                dir = "user://captures";
            DirAccess.MakeDirRecursiveAbsolute(dir);
            return dir;
        }
    }

    /// <summary>Saves the current viewport image to the capture directory.</summary>
    protected void CaptureScreenshot(string fileName)
    {
        var img = Root.GetViewport().GetTexture().GetImage();
        if (img == null || img.IsEmpty())
            return;

        string path = $"{CaptureDir}/{fileName}";
        img.SavePng(path);
        GD.Print($"[SUCCESS] Captured {fileName} -> {ProjectSettings.GlobalizePath(path)}");
    }

    /// <summary>Prints the standard success footer and quits with the given code.</summary>
    protected void Finish(bool success, string title)
    {
        if (success)
        {
            GD.Print("==================================================================");
            GD.Print($">>> {title} PASSED SUCCESSFULLY! <<<");
            GD.Print("==================================================================");
        }
        Quit(success ? 0 : 1);
    }
}
