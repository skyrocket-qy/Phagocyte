using Godot;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Toxic mycotoxin aerosol cloud released upon Aspergillus death.
/// Deals continuous true damage to player standing within.
/// </summary>
public partial class SporeCloud : Area2D
{
    [Export] public float Duration { get; set; } = 6.0f;
    [Export] public float MaxRadius { get; set; } = 100.0f;
    [Export] public float DamagePerSecond { get; set; } = 8.0f;

    private float _lifeTimer = 0.0f;
    private CollisionShape2D? _colShape;
    private CircleShape2D? _circleShape;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 1; // Player

        _circleShape = new CircleShape2D { Radius = 20.0f };
        _colShape = new CollisionShape2D { Shape = _circleShape };
        // Deferred: clouds are spawned from AreaEntered engulf callbacks, where the
        // physics server is mid-flush and direct child additions are rejected.
        CallDeferred(Node.MethodName.AddChild, _colShape);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _lifeTimer += dt;

        if (_lifeTimer >= Duration)
        {
            QueueFree();
            return;
        }

        // Expand radius
        float progress = Mathf.Clamp(_lifeTimer / 1.5f, 0.0f, 1.0f);
        float currentRadius = Mathf.Lerp(20.0f, MaxRadius, progress);
        if (_circleShape != null)
            _circleShape.Radius = currentRadius;

        // Damage player inside
        foreach (var body in GetOverlappingBodies())
        {
            if (body is BaseCell player)
            {
                player.TakeDamage(DamagePerSecond * dt);
            }
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        float alphaRatio = 1.0f - (_lifeTimer / Duration);
        float currentRadius = _circleShape != null ? _circleShape.Radius : 40.0f;

        Color cloudColor = new Color(0.18f, 0.28f, 0.12f, 0.38f * alphaRatio);
        Color rimColor = new Color(0.45f, 0.65f, 0.25f, 0.55f * alphaRatio);

        DrawCircle(Vector2.Zero, currentRadius, cloudColor);
        DrawArc(Vector2.Zero, currentRadius, 0, Mathf.Tau, 24, rimColor, 1.5f);

        // Toxic spore speckles
        for (int i = 0; i < 8; i++)
        {
            float ang = i * (Mathf.Tau / 8.0f) + _lifeTimer * 0.8f;
            Vector2 sporePos = Vector2.FromAngle(ang) * (currentRadius * 0.65f);
            DrawCircle(sporePos, 2.5f, new Color(0.6f, 0.85f, 0.2f, 0.6f * alphaRatio));
        }
    }
}
