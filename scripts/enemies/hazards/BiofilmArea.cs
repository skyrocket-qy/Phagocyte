using Godot;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Sticky alginate extracellular polymeric substance (EPS) biofilm puddle left by Pseudomonas aeruginosa.
/// Slows player movement by 50% and protects bacteria inside.
/// </summary>
public partial class BiofilmArea : Area2D
{
    [Export] public float Duration { get; set; } = 8.0f;
    [Export] public float Radius { get; set; } = 75.0f;

    private float _lifeTimer = 0.0f;
    private CollisionShape2D? _colShape;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 1 | 2; // Detect player (1) and enemies (2)

        _colShape = new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = Radius }
        };
        AddChild(_colShape);

        QueueRedraw();
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

        // Apply slow to any overlapping player
        foreach (var body in GetOverlappingBodies())
        {
            if (body is BaseCell player)
            {
                player.ApplySlow(0.3f, 0.5f); // 50% slow
            }
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        float alphaRatio = 1.0f - (_lifeTimer / Duration);
        Color baseGreen = new Color(0.12f, 0.55f, 0.22f, 0.35f * alphaRatio);
        Color rimGreen = new Color(0.25f, 0.85f, 0.35f, 0.65f * alphaRatio);

        // Draw irregular bio-puddle
        DrawCircle(Vector2.Zero, Radius, baseGreen);
        DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 32, rimGreen, 2.0f);

        // Procedural micro-bubbles
        for (int i = 0; i < 5; i++)
        {
            float ang = i * (Mathf.Tau / 5.0f) + _lifeTimer * 0.5f;
            Vector2 bubPos = Vector2.FromAngle(ang) * (Radius * 0.55f);
            DrawCircle(bubPos, 4.0f, new Color(0.4f, 0.95f, 0.5f, 0.4f * alphaRatio));
        }
    }
}
