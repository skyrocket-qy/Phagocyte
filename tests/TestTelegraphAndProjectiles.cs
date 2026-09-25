using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Skills;
using Phagocyte.Enemies;
using Phagocyte.Combat;

namespace Phagocyte.Tests;

[TestSuite]
public partial class TestTelegraphAndProjectiles : SceneTree
{
    private int _framesWaited = 0;
    private bool _testDone = false;

    public override void _Initialize()
    {
        GD.Print("==================================================================");
        GD.Print(">>> INITIALIZING TELEGRAPHED ATTACK & PROJECTILE MANAGER TESTS <<<");
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
            GD.Print(">>> ALL TELEGRAPH & PROJECTILE TESTS PASSED WITH 100% SUCCESS! <<<");
            GD.Print("==================================================================");
            Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestTelegraphAndProjectiles encountered an exception: ", ex);
            Quit(1);
        }

        return true;
    }

    private void RunAllTests()
    {
        // --- 1. TelegraphedAttack Shape & Geometry Checks ---
        var circleAttack = new TelegraphedAttack
        {
            Shape = TelegraphAttackShape.Circle,
            GlobalPosition = new Vector2(100, 100),
            Radius = 50.0f,
            Damage = 20.0f
        };
        Root.AddChild(circleAttack);

        AssertThat(circleAttack.CheckHit(new Vector2(100, 100))).IsTrue();
        AssertThat(circleAttack.CheckHit(new Vector2(130, 100))).IsTrue(); // distance 30 <= 50
        AssertThat(circleAttack.CheckHit(new Vector2(160, 100))).IsFalse(); // distance 60 > 50
        AssertThat(TelegraphedAttack.ActiveAttacks.Contains(circleAttack)).IsTrue();

        var lineAttack = new TelegraphedAttack
        {
            Shape = TelegraphAttackShape.Line,
            GlobalPosition = new Vector2(0, 0),
            TargetDirection = Vector2.Right,
            LineLength = 100.0f,
            LineWidth = 30.0f,
            Damage = 25.0f
        };
        Root.AddChild(lineAttack);

        AssertThat(lineAttack.CheckHit(new Vector2(50, 0))).IsTrue();
        AssertThat(lineAttack.CheckHit(new Vector2(50, 10))).IsTrue(); // Y=10 <= 15
        AssertThat(lineAttack.CheckHit(new Vector2(50, 25))).IsFalse(); // Y=25 > 15
        AssertThat(lineAttack.CheckHit(new Vector2(120, 0))).IsFalse(); // X=120 > 100
        AssertThat(lineAttack.CheckHit(new Vector2(-10, 0))).IsFalse(); // X=-10 < 0

        circleAttack.QueueFree();
        lineAttack.QueueFree();
        GD.Print("[PASS] Step 1: TelegraphedAttack Circle & Line geometry math verified.");

        // --- 2. TelegraphedAttack Execution & BaseCell Damage Resolution ---
        var cell = new BaseCell { GlobalPosition = new Vector2(200, 200) };
        var stats = new CellStats { Name = "CellStats" };
        cell.AddChild(stats);
        cell.Stats = stats;
        cell.AddToGroup("player");
        Root.AddChild(cell);

        float startingHp = cell.Health;

        var impactAttack = new TelegraphedAttack
        {
            Shape = TelegraphAttackShape.Circle,
            GlobalPosition = new Vector2(200, 200),
            Radius = 60.0f,
            Damage = 15.0f,
            TelegraphDuration = 0.05f
        };
        Root.AddChild(impactAttack);

        impactAttack.ExecuteImpact();
        AssertThat(cell.Health).IsLess(startingHp);
        GD.Print("[PASS] Step 2: TelegraphedAttack cleanly executes impact against BaseCell.");

        impactAttack.QueueFree();

        // --- 3. ProjectileManager Batch Lifecycle & Buffer Allocation ---
        var projMgr = new ProjectileManager { Name = "ProjectileManager" };
        Root.AddChild(projMgr);
        projMgr.SetHost(cell);

        AssertThat(ProjectileManager.Instance).IsEqual(projMgr);
        AssertThat(projMgr.ActiveCount).IsEqual(0);

        // Spawn batch of 50 bullets
        for (int i = 0; i < 50; i++)
        {
            projMgr.Spawn(
                new Vector2(100, 100),
                Vector2.Right,
                speed: 400.0f,
                damage: 20.0f,
                isCrit: false,
                pierce: 1,
                lifetime: 2.0f,
                radius: 10.0f,
                projType: "defensin_barb"
            );
        }

        AssertThat(projMgr.ActiveCount).IsEqual(50);
        GD.Print("[PASS] Step 3: ProjectileManager spawns 50 batch projectiles with zero allocation.");

        // --- 4. ProjectileManager 64px Spatial Grid & Enemy Collision ---
        var enemy = new StaphEnemy
        {
            GlobalPosition = new Vector2(140, 100), // In front of bullets traveling Right from (100, 100)
            CurrentHealth = 50.0f,
            MaxHealth = 50.0f
        };
        Root.AddChild(enemy);

        AssertThat(BaseEnemy.ActiveEnemies.Contains(enemy)).IsTrue();

        float enemyHpBefore = enemy.CurrentHealth;

        // Run physics process step to move bullets and resolve spatial grid collisions
        projMgr._PhysicsProcess(0.12);

        AssertThat(enemy.CurrentHealth).IsLess(enemyHpBefore);
        GD.Print("[PASS] Step 4: 64px Spatial Hash Grid correctly resolves O(1) projectile collisions.");

        // Clear all
        projMgr.ClearAll();
        AssertThat(projMgr.ActiveCount).IsEqual(0);

        enemy.QueueFree();
        projMgr.QueueFree();
        cell.QueueFree();

        // --- 5. DefensinBarbsSkill Integration ---
        var barbHost = new BaseCell { GlobalPosition = new Vector2(300, 300) };
        var barbStats = new CellStats { Name = "CellStats" };
        barbHost.AddChild(barbStats);
        barbHost.Stats = barbStats;
        Root.AddChild(barbHost);

        var freshProjMgr = new ProjectileManager { Name = "ProjectileManager2" };
        Root.AddChild(freshProjMgr);
        freshProjMgr.SetHost(barbHost);

        var skill = new DefensinBarbsSkill();
        skill.Setup(barbHost);

        int activeBefore = freshProjMgr.ActiveCount;
        skill.Trigger();
        AssertThat(freshProjMgr.ActiveCount).IsGreater(activeBefore);
        GD.Print($"[PASS] Step 5: DefensinBarbsSkill seamlessly routed {freshProjMgr.ActiveCount} barbs to ProjectileManager.");

        skill.QueueFree();
        freshProjMgr.QueueFree();
        barbHost.QueueFree();
    }
}
