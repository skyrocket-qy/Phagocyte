using Godot;
using System;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the screen active cap (300 / 450) and the kill-driven dynamic backfill loop.
/// </summary>
[TestSuite]
public partial class TestDynamicBackfill : SceneTree
{
    private int _frame = 0;
    private bool _done = false;

    public override bool _Process(double delta)
    {
        if (_done)
            return true;

        _frame++;
        if (_frame < 3)
            return false;

        _done = true;
        try
        {
            RunTests();
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestDynamicBackfill threw: ", ex);
            Quit(1);
            return true;
        }

        GD.Print("==================================================================");
        GD.Print(">>> ALL DYNAMIC BACKFILL TESTS PASSED SUCCESSFULLY! <<<");
        GD.Print("==================================================================");
        Quit(0);
        return true;
    }

    private void RunTests()
    {
        // 1. Screen cap constants
        AssertThat(PathogenSpawner.MaxActiveNormal).IsEqual(300);
        AssertThat(PathogenSpawner.MaxActiveSwarm).IsEqual(450);

        // 2. Offscreen spawn points always land outside the expanded camera view
        var arena = new Vector2(4800.0f, 4800.0f);
        var view = new Vector2(1200.0f, 700.0f);
        for (int i = 0; i < 40; i++)
        {
            Vector2 point = PathogenSpawner.GetOffscreenSpawnPoint(Vector2.Zero, arena, view, 200.0f);
            bool outside = Mathf.Abs(point.X) > view.X * 0.5f + 100.0f
                        || Mathf.Abs(point.Y) > view.Y * 0.5f + 100.0f;
            AssertThat(outside).IsTrue();
        }
        GD.Print("[PASS] Offscreen spawn points confirmed beyond the camera view boundary.");

        // 3. Backfill spawns the exact deficit amount
        var container = new Node2D { Name = "BackfillContainer" };
        Root.AddChild(container);
        var host = new BaseCell { Name = "BackfillHost" };
        container.AddChild(host);

        int spawned = PathogenSpawner.Backfill(container, host, arena, view, 200.0f, 7);
        AssertThat(spawned).IsEqual(7);
        AssertThat(container.GetChildCount()).IsEqual(8);

        // 4. Main integration: kill-driven refill to normal and swarm caps
        GameManager.SelectedClass = "macrophage";
        GameManager.SelectedMap = "acute_wound";

        var main = GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>();
        main.ScreenCapNormal = 10;
        main.ScreenCapSwarm = 14;
        Root.AddChild(main);
        main.SetPhysicsProcess(false);

        var enemies = main.EnemyContainer!;
        FreeChildren(enemies);

        main._PhysicsProcess(0.02f);
        AssertThat(main.ActiveScreenCap).IsEqual(10);
        AssertThat(main.ActivePathogenCount).IsEqual(10);
        GD.Print("[PASS] Normal cap backfilled instantly from an empty arena (300 base cap constant verified).");

        // Kills release slots and are refilled on the next poll (<0.15s)
        FreeChildren(enemies);
        main._PhysicsProcess(0.02f);
        AssertThat(main.ActivePathogenCount).IsEqual(10);
        GD.Print("[PASS] Mass kill deficit refilled within a single poll.");

        // 06:00 swarm event raises the cap and refills to it
        main.EnvironmentTime = PathogenSpawner.EscalationInterval * 2.0f - 0.01f;
        main._PhysicsProcess(0.02f);
        AssertThat(main.FirstSwarmTriggered).IsTrue();
        AssertThat(main.ActiveScreenCap).IsEqual(14);
        FreeChildren(enemies);
        main._PhysicsProcess(0.02f);
        AssertThat(main.ActivePathogenCount).IsEqual(14);

        // Swarm window expiry drops the cap back to normal
        main._PhysicsProcess(PathogenSpawner.SwarmWindowSeconds + 0.1f);
        AssertThat(main.ActiveScreenCap).IsEqual(10);
        AssertThat(main.SwarmWindowTimer).IsEqual(0.0f);
        GD.Print("[PASS] Swarm window dynamically raised the cap (450 base constant) and expired back to normal.");

        main.QueueFree();
        container.QueueFree();
    }

    private static void FreeChildren(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.Free();
        }
    }
}
