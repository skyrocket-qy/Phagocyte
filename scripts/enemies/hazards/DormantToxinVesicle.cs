using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Dormant toxin vesicle: a drifting microscopic mine.
/// Detonates into a toxic acid cloud when any entity (cell, pathogen, projectile
/// or red blood cell) touches it.
/// </summary>
public partial class DormantToxinVesicle : Node2D
{
    [Export] public float TriggerRadius { get; set; } = 22.0f;
    [Export] public float BlastRadius { get; set; } = 90.0f;
    [Export] public float PlayerBlastDamage { get; set; } = 18.0f;
    [Export] public float EnemyBlastDamage { get; set; } = 30.0f;
    [Export] public float Lifetime { get; set; } = 40.0f;
    [Export] public float DriftSpeed { get; set; } = 10.0f;

    public bool HasExploded { get; private set; }

    private float _age;
    private float _pulse;
    private float _wanderTimer;
    private Vector2 _wanderDir;
    private bool _leaving;

    public override void _Ready()
    {
        AddToGroup("neutral_matter");
        AddToGroup("toxin_vesicle");
        ZIndex = 1;

        _wanderDir = Vector2.FromAngle(GD.Randf() * Mathf.Tau);
        _wanderTimer = (float)GD.RandRange(3.0, 6.0);
        QueueRedraw();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_leaving)
            return;

        float dt = (float)delta;
        _age += dt;
        _pulse += dt * 3.0f;

        _wanderTimer -= dt;
        if (_wanderTimer <= 0.0f)
        {
            _wanderTimer = (float)GD.RandRange(3.0, 6.0);
            _wanderDir = Vector2.FromAngle(GD.Randf() * Mathf.Tau);
        }

        Position += _wanderDir * DriftSpeed * dt;

        if (_age >= Lifetime)
        {
            FadeOutAndFree();
            return;
        }

        if (CheckTriggers())
            return;

        QueueRedraw();
    }

    private bool CheckTriggers()
    {
        var tree = GetTree();

        var player = EnemySteering.GetPlayer(this);
        if (player != null && GodotObject.IsInstanceValid(player) && !player.IsDead)
        {
            if (GlobalPosition.DistanceTo(player.GlobalPosition) <= TriggerRadius + player.CurrentRadius * 0.5f)
            {
                Explode();
                return true;
            }
        }

        if (TargetingService.AnyInRadius(GlobalPosition, TriggerRadius + 16.0f, skipEaten: false))
        {
            Explode();
            return true;
        }

        foreach (var node in tree.GetNodesInGroup("enemy_shots"))
        {
            if (node is Node2D shot && GodotObject.IsInstanceValid(shot)
                && GlobalPosition.DistanceTo(shot.GlobalPosition) <= TriggerRadius + 8.0f)
            {
                Explode();
                return true;
            }
        }

        foreach (var node in tree.GetNodesInGroup("senescent_rbc"))
        {
            if (node is Node2D rbc && GodotObject.IsInstanceValid(rbc)
                && GlobalPosition.DistanceTo(rbc.GlobalPosition) <= TriggerRadius + 18.0f)
            {
                Explode();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Detonates the vesicle into a toxic acid cloud, damaging everything nearby (friendly fire included).
    /// </summary>
    public void Explode()
    {
        if (HasExploded || IsQueuedForDeletion())
            return;

        HasExploded = true;
        _leaving = true;

        var parent = GetParent();
        if (parent != null)
        {
            var cloud = new BioHazardArea
            {
                GlobalPosition = GlobalPosition,
                Duration = 5.0f,
                Radius = BlastRadius * 0.85f,
                Damage = 5.0f,
                TickInterval = 0.6f,
                SlowFactor = 0.55f,
                SlowsTarget = true,
                DealsDamage = true,
                CoreColor = new Color(0.45f, 0.30f, 0.55f, 0.35f),
                RimColor = new Color(0.75f, 0.45f, 0.95f, 0.65f)
            };
            parent.AddChild(cloud);

            var burst = new DriftWave
            {
                GlobalPosition = GlobalPosition,
                MaxRadius = BlastRadius
            };
            parent.AddChild(burst);

            var player = EnemySteering.GetPlayer(this);
            if (player != null && GodotObject.IsInstanceValid(player) && !player.IsDead
                && GlobalPosition.DistanceTo(player.GlobalPosition) <= BlastRadius)
            {
                player.TakeEnvironmentalDamage(PlayerBlastDamage);
            }

            TargetingService.ForEachInRadius(
                GlobalPosition,
                BlastRadius,
                enemy => enemy.TakeDamage(EnemyBlastDamage, null),
                skipEaten: false);
        }

        QueueFree();
    }

    private void FadeOutAndFree()
    {
        _leaving = true;
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.8);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    public override void _Draw()
    {
        float pulse = 1.0f + 0.08f * Mathf.Sin(_pulse);
        Color shell = new(0.42f, 0.28f, 0.52f, 0.85f);
        Color toxin = new(0.78f, 0.45f, 0.95f, 0.95f);

        DrawCircle(Vector2.Zero, 16.0f * pulse, new Color(0.75f, 0.45f, 0.95f, 0.18f));
        DrawCircle(Vector2.Zero, 11.0f, shell);
        DrawCircle(Vector2.Zero, 7.0f * pulse, toxin);
        DrawCircle(new Vector2(-3, -3), 2.2f, new Color(1.0f, 0.9f, 1.0f, 0.85f));
    }
}
