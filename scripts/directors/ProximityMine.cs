using Godot;
using System;
using Game.Combat;
using Game.Enemies;
using Game.Player;

namespace Game.Directors;

/// <summary>
/// Dormant toxin vesicle: drifting neutral matter (see <see cref="NeutralPropManager"/>).
/// Detonates into a toxic acid cloud when the cell or a pathogen touches it.
/// </summary>
public partial class ProximityMine : Node2D
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
        AddToGroup("neutral_props");
        AddToGroup("proximity_mine");
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
        var player = EnemySteering.GetPlayer(this);
        if (player != null && GodotObject.IsInstanceValid(player) && !player.IsDead)
        {
            if (GlobalPosition.DistanceTo(player.GlobalPosition) <= TriggerRadius + player.CurrentRadius * 0.5f)
            {
                Explode();
                return true;
            }
        }

        if (TargetingService.AnyInRadius(GlobalPosition, TriggerRadius + 16.0f))
        {
            Explode();
            return true;
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
            var cloud = new Zone
            {
                SourceTeam = Team.Enemy,
                GlobalPosition = GlobalPosition,
                Duration = 5.0f,
                Radius = BlastRadius * 0.85f,
                Damage = 5.0f,
                TickInterval = 0.6f,
                Effect0 = new EffectSpec
                {
                    EffectId = AilmentController.AgglutinationId,
                    Magnitude = 1.0f - 0.55f,
                    Duration = 0.6f * 1.5f
                },
                EffectCount = 1,
                CoreColor = new Color(0.45f, 0.30f, 0.55f, 0.35f),
                RimColor = new Color(0.75f, 0.45f, 0.95f, 0.65f)
            };
            parent.AddChild(cloud);

            var burst = new ShockRing
            {
                GlobalPosition = GlobalPosition,
                MaxRadius = BlastRadius
            };
            parent.AddChild(burst);

            var player = EnemySteering.GetPlayer(this);
            if (player != null && GodotObject.IsInstanceValid(player) && !player.IsDead
                && GlobalPosition.DistanceTo(player.GlobalPosition) <= BlastRadius)
            {
                player.TakeDamage(PlayerBlastDamage);
            }

            TargetingService.ForEachInRadius(
                GlobalPosition,
                BlastRadius,
                enemy => enemy.TakeDamage(EnemyBlastDamage, null));
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


