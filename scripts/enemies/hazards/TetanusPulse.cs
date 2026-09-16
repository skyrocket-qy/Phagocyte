using Godot;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Neurotoxin electric pulse fired by Clostridium tetani.
/// Deals damage and paralyzes/stuns player movement for 0.75s on impact.
/// </summary>
public partial class TetanusPulse : Area2D
{
    [Export] public float Speed { get; set; } = 220.0f;
    [Export] public float Damage { get; set; } = 10.0f;
    [Export] public float StunDuration { get; set; } = 0.75f;
    [Export] public float Lifetime { get; set; } = 4.0f;

    public Vector2 Direction { get; set; } = Vector2.Right;
    private float _age = 0.0f;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 1; // Player

        var col = new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = 10.0f }
        };
        AddChild(col);

        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _age += dt;
        if (_age >= Lifetime)
        {
            QueueFree();
            return;
        }

        Position += Direction * Speed * dt;
        QueueRedraw();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is BaseCell player)
        {
            player.TakeDamage(Damage);
            player.ApplyStun(StunDuration);
            QueueFree();
        }
    }

    public override void _Draw()
    {
        // Crackling electric bio-spark
        Color coreColor = new Color(0.4f, 0.85f, 1.0f, 0.95f);
        Color auraColor = new Color(0.2f, 0.5f, 1.0f, 0.4f);

        DrawCircle(Vector2.Zero, 8.0f, auraColor);
        DrawCircle(Vector2.Zero, 4.0f, coreColor);

        // Electric tendrils
        float pulse = Mathf.Sin(_age * 20.0f) * 6.0f;
        DrawLine(new Vector2(-6, pulse), new Vector2(6, -pulse), Colors.White, 2.0f);
    }
}
