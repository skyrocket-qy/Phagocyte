using Godot;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Senescent red blood cell: neutral drifting cover matter.
/// Flows with the tissue current, blocks enemy projectiles, is ignored by all
/// skills and auto-targeting, and can only be harvested by direct cell engulfment
/// (grants ATP + kill credit, BaseScore stays 0 to prevent score farming).
/// </summary>
public partial class SenescentRBC : Node2D
{
    [Export] public float AtpValue { get; set; } = 25.0f;
    [Export] public float Lifetime { get; set; } = 30.0f;
    [Export] public float DriftSpeed { get; set; } = 12.0f;
    [Export] public float CollisionRadius { get; set; } = 18.0f;

    public bool IsConsumed { get; private set; }

    private Area2D? _hitArea;
    private float _age;
    private float _spin;
    private float _wanderTimer;
    private Vector2 _wanderDir;
    private bool _leaving;

    public override void _Ready()
    {
        AddToGroup("neutral_matter");
        AddToGroup("senescent_rbc");
        ZIndex = 1;

        _wanderDir = Vector2.FromAngle(GD.Randf() * Mathf.Tau);
        _wanderTimer = (float)GD.RandRange(3.0, 6.0);

        // Layer 2 mirrors pathogen hit areas so the player's EngulfArea can detect it.
        _hitArea = new Area2D
        {
            Name = "HitArea",
            CollisionLayer = 2,
            CollisionMask = 0,
            Monitoring = false
        };
        _hitArea.AddChild(new CollisionShape2D
        {
            Name = "CollisionShape2D",
            Shape = new CircleShape2D { Radius = CollisionRadius }
        });
        AddChild(_hitArea);

        QueueRedraw();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_leaving)
            return;

        float dt = (float)delta;
        _age += dt;
        _spin += dt * 0.5f;

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

        QueueRedraw();
    }

    public float GetAtpValue() => AtpValue;
    public float get_atp_value() => AtpValue;

    public int GetBaseScore() => 0;
    public int get_base_score() => 0;

    public void BeEngulfed(Node2D? predator) => be_engulfed(predator);

    public void be_engulfed(Node2D? predator)
    {
        if (IsConsumed || IsQueuedForDeletion())
            return;

        IsConsumed = true;
        _leaving = true;

        Vector2 target = predator != null && GodotObject.IsInstanceValid(predator)
            ? predator.GlobalPosition
            : GlobalPosition;

        var tween = CreateTween().SetParallel(true);
        tween.TweenProperty(this, "global_position", target, 0.18)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.18)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.18);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            // Harvest resolution pays out: neutral matter has no Die().
            if (predator is BaseCell cell && GodotObject.IsInstanceValid(cell))
                cell.AddExp(GetAtpValue());
            QueueFree();
        }));
    }

    /// <summary>
    /// Dissolved by acid/toxins. No kill credit and no ATP: only engulfment harvests it.
    /// </summary>
    public void Dissolve()
    {
        if (IsConsumed || IsQueuedForDeletion())
            return;

        IsConsumed = true;
        _leaving = true;

        var tween = CreateTween().SetParallel(true);
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.15);
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.15);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
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
        // Biconcave senescent erythrocyte: pale rim, dark sunken center, echinocyte spikes
        float pulse = 1.0f + 0.03f * Mathf.Sin(_spin * 2.2f);
        Color rim = new(0.72f, 0.24f, 0.26f, 0.85f);
        Color core = new(0.45f, 0.12f, 0.14f, 0.90f);

        DrawCircle(Vector2.Zero, CollisionRadius * pulse, rim);
        DrawCircle(Vector2.Zero, CollisionRadius * 0.62f * pulse, core);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * (Mathf.Tau / 8.0f) + _spin;
            Vector2 from = Vector2.FromAngle(angle) * CollisionRadius * pulse;
            Vector2 to = Vector2.FromAngle(angle) * (CollisionRadius + 4.0f) * pulse;
            DrawLine(from, to, new Color(0.68f, 0.22f, 0.24f, 0.7f), 1.8f);
        }
    }
}
