using Godot;
using System;
using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the high-concurrency performance pipeline (docs/spec.md Â§9 /
/// TODO module 12): the 2D QuadTree spatial index, the MultiMeshInstance2D GPU
/// batch renderer for microscopic swarms, and the passive enemy collision setup.
/// </summary>
[TestSuite]
public partial class TestPerformancePipeline : TestHarness
{
    private int _phase = 0;
    private Main? _main = null;
    private ProjectileManager? _projectiles = null;

    private static readonly Vector2 ArenaHalf = new(500.0f, 500.0f);

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> STARTING PERFORMANCE PIPELINE (QUADTREE + MULTIMESH) VERIFICATION <<<");
        GD.Print("==================================================================");
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
                RunQuadTreeTests();
                _phase++;
                return false;
            case 2:
                RunSwarmBatchTests();
                _phase++;
                return false;
            case 3:
                RunProjectileIntegrationTests();
                _phase++;
                return false;
            case 4:
                RunCollisionSetupTests();
                _phase++;
                return false;
            default:
                Cleanup();
                GD.Print("==================================================================");
                GD.Print(">>> ALL PERFORMANCE PIPELINE TESTS PASSED SUCCESSFULLY! <<<");
                GD.Print("==================================================================");
                Quit(0);
                return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestPerformancePipeline threw: ", ex);
            Cleanup();
            Quit(1);
            return true;
        }
    }

    private void RunQuadTreeTests()
    {
        var bounds = new Rect2(-ArenaHalf, ArenaHalf * 2.0f);
        var tree = new QuadTree<Vector2>(bounds, capacity: 4, maxDepth: 5);

        // Deterministic point cloud + brute-force ground truth
        var random = new Random(1234);
        var points = new List<Vector2>(600);
        for (int i = 0; i < 600; i++)
        {
            points.Add(new Vector2(
                (float)(random.NextDouble() * 1000.0 - 500.0),
                (float)(random.NextDouble() * 1000.0 - 500.0)));
        }

        foreach (var point in points)
            tree.Insert(point, point);
        AssertThat(tree.Count).IsEqual(600);

        // Circle queries must match the brute-force scan
        var results = new List<Vector2>(64);
        for (int q = 0; q < 25; q++)
        {
            Vector2 center = new(
                (float)(random.NextDouble() * 900.0 - 450.0),
                (float)(random.NextDouble() * 900.0 - 450.0));
            float radius = (float)(random.NextDouble() * 120.0 + 10.0);

            results.Clear();
            tree.QueryCircle(center, radius, results);

            float r2 = radius * radius;
            var expected = new List<Vector2>();
            foreach (var point in points)
            {
                if (point.DistanceSquaredTo(center) <= r2)
                    expected.Add(point);
            }

            AssertThat(results.Count).IsEqual(expected.Count);
            foreach (var expectedPoint in expected)
                AssertThat(results.Contains(expectedPoint)).IsTrue();
        }

        // Rect queries must match too
        for (int q = 0; q < 10; q++)
        {
            Vector2 origin = new(
                (float)(random.NextDouble() * 700.0 - 350.0),
                (float)(random.NextDouble() * 700.0 - 350.0));
            var area = new Rect2(origin, new Vector2(140.0f, 90.0f));

            results.Clear();
            tree.QueryRect(area, results);

            var expected = new List<Vector2>();
            foreach (var point in points)
            {
                if (area.HasPoint(point))
                    expected.Add(point);
            }

            AssertThat(results.Count).IsEqual(expected.Count);
        }

        // Nearest lookup agrees with brute force
        var scratch = new List<Vector2>(64);
        for (int q = 0; q < 10; q++)
        {
            Vector2 center = new(
                (float)(random.NextDouble() * 800.0 - 400.0),
                (float)(random.NextDouble() * 800.0 - 400.0));

            // Radius covers the whole cloud from any query center (±450 + ±500 corner).
            bool found = tree.TryFindNearest(center, 2500.0f, scratch, out Vector2 nearest, positionOf: p => p);

            float bestSq = float.MaxValue;
            foreach (var point in points)
                bestSq = Mathf.Min(bestSq, point.DistanceSquaredTo(center));

            AssertThat(found).IsTrue();
            AssertThat(nearest.DistanceSquaredTo(center)).IsEqualApprox(bestSq, 0.01f);
        }

        // Points outside the root bounds remain queryable
        tree.Insert(new Vector2(2400.0f, 0.0f), new Vector2(2400.0f, 0.0f));
        results.Clear();
        tree.QueryCircle(new Vector2(2400.0f, 0.0f), 20.0f, results);
        AssertThat(results.Count).IsEqual(1);

        // Clear resets the index and the pooled nodes stay reusable
        tree.Clear();
        AssertThat(tree.Count).IsEqual(0);
        results.Clear();
        tree.QueryCircle(Vector2.Zero, 5000.0f, results);
        AssertThat(results.Count).IsEqual(0);

        tree.Insert(Vector2.Zero, Vector2.Zero);
        results.Clear();
        tree.QueryCircle(Vector2.Zero, 10.0f, results);
        AssertThat(results.Count).IsEqual(1);

        GD.Print("[PASS] QuadTree circle/rect/nearest queries match brute force and reuse pooled nodes.");
    }

    private void RunSwarmBatchTests()
    {
        GameManager.SelectedClass = "macrophage";
        GameManager.SelectedMap = "acute_wound";

        var main = AssetLoader.Load<PackedScene>("res://scenes/main.tscn").Instantiate<Main>();
        _main = main;
        Root.AddChild(main);
        main.SetPhysicsProcess(false);
        main.SetProcess(false);

        AssertThat(main.SwarmRenderer).IsNotNull();
        var renderer = main.SwarmRenderer!;
        AssertThat(renderer.IsSpeciesBatched("norovirus")).IsTrue();
        AssertThat(renderer.IsSpeciesBatched("flu_drift")).IsTrue();
        AssertThat(renderer.SpeciesCount).IsEqual(2);

        // Isolate a clean arena so the counts are deterministic
        foreach (var child in main.EnemyContainer!.GetChildren())
        {
            if (child is BaseEnemy)
                child.Free();
        }

        var noroviruses = new List<BaseEnemy>();
        for (int i = 0; i < 40; i++)
        {
            var noro = new NorovirusEnemy { GlobalPosition = new Vector2(i * 4.0f, 0.0f) };
            main.EnemyContainer.AddChild(noro);
            noroviruses.Add(noro);
        }

        var flu = new FluDriftEnemy { GlobalPosition = new Vector2(200.0f, 120.0f) };
        main.EnemyContainer.AddChild(flu);

        renderer.Enabled = true;
        renderer.Sync();

        AssertThat(renderer.GetVisibleCount("norovirus")).IsEqual(40);
        AssertThat(renderer.GetVisibleCount("flu_drift")).IsEqual(1);
        AssertThat(renderer.LastBatchedCount).IsEqual(41);

        foreach (var noro in noroviruses)
            AssertThat(noro.Visible).IsFalse();
        AssertThat(flu.Visible).IsFalse();

        // Instance transform mirrors the node transform
        var multimesh = main.GetNode<MultiMeshInstance2D>("PathogenSwarmRenderer/SwarmBatchRoot/SwarmBatch_norovirus").Multimesh;
        Vector2 instanceOrigin = multimesh.GetInstanceTransform2D(0).Origin;
        AssertThat(instanceOrigin.IsEqualApprox(noroviruses[0].GlobalPosition)).IsTrue();

        // Disabling restores per-node drawing
        main.SwarmBatchingEnabled = false;
        renderer.Enabled = false;
        renderer.Sync();
        AssertThat(renderer.GetVisibleCount("norovirus")).IsEqual(0);
        foreach (var noro in noroviruses)
            AssertThat(noro.Visible).IsTrue();

        GD.Print("[PASS] MultiMesh swarm batching hides/supplies micro viruses and restores per-node drawing.");
    }

    private void RunProjectileIntegrationTests()
    {
        AssertThat(_main).IsNotNull();
        var main = _main!;

        // Isolate a clean arena: phase 2 leaves its 40 batched noroviruses in
        // the container, and a pierce-0 projectile dies on the first leftover
        // it touches, so the target would never take the hit without this reset.
        foreach (var child in main.EnemyContainer!.GetChildren())
        {
            if (child is BaseEnemy)
                child.Free();
        }

        var renderer = main.SwarmRenderer!;

        // Batched (invisible) micro virus must still collide with projectiles.
        var target = new NorovirusEnemy { GlobalPosition = new Vector2(60.0f, 0.0f) };
        main.EnemyContainer!.AddChild(target);
        renderer.Enabled = true;
        renderer.Sync();
        AssertThat(target.Visible).IsFalse();

        var manager = ProjectileManager.Instance;
        AssertThat(manager).IsNotNull();
        AssertThat(manager!.ActiveCount).IsEqual(0);

        float hpBefore = target.CurrentHealth;
        manager.Spawn(new Vector2(0.0f, 0.0f), Vector2.Right, 600.0f, 5.0f, false, 0, 1.0f, 10.0f, "generic");

        // One manual physics step carries the projectile 36px -> inside hit range
        manager._PhysicsProcess(0.06);
        AssertThat(target.CurrentHealth).IsLess(hpBefore);

        manager.ClearAll();
        target.QueueFree();

        GD.Print("[PASS] QuadTree-driven projectile collision hits batched (invisible) pathogens.");
    }

    private void RunCollisionSetupTests()
    {
        var probe = new StaphEnemy { GlobalPosition = new Vector2(40.0f, 40.0f) };
        Root.AddChild(probe);

        AssertThat(probe.HitArea).IsNotNull();
        AssertThat(probe.HitArea!.Monitoring).IsFalse();
        AssertThat(probe.HitArea.Monitorable).IsTrue();
        AssertThat(probe.HitArea.CollisionLayer).IsEqual(2);
        AssertThat(probe.HitArea.CollisionMask).IsEqual(0);
        probe.QueueFree();

        GD.Print("[PASS] Enemy hit areas are passive targets (no self-monitoring) to cut physics broadphase cost.");
    }

    private void Cleanup()
    {
        Paused = false;

        if (_projectiles != null && IsInstanceValid(_projectiles))
        {
            _projectiles.QueueFree();
            _projectiles = null;
        }

        if (_main != null && IsInstanceValid(_main))
        {
            if (_main.GetParent() != null)
                _main.GetParent().RemoveChild(_main);
            _main.Free();
            _main = null;
        }
    }
}
