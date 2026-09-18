using Godot;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Generic enemy projectile (spike virions, toxin micro-particles) fired by
/// standoff artillery pathogens. Soft-capped by the <c>enemy_shots</c> group so
/// high-density fights cannot flood the physics server.
/// </summary>
public partial class EnemyPellet : Area2D
{
    public const int MaxActivePellets = 120;

    public static int ActiveCount { get; private set; }

    public float Speed { get; set; } = 220.0f;
    public float Damage { get; set; } = 8.0f;
    public float Lifetime { get; set; } = 5.0f;
    public Color CoreColor { get; set; } = new(0.95f, 0.45f, 0.45f, 0.95f);
    public Color AuraColor { get; set; } = new(0.9f, 0.25f, 0.35f, 0.35f);

    public Vector2 Direction { get; set; } = Vector2.Right;

    private float _age;

    public static bool CanSpawn()
    {
        return ActiveCount < MaxActivePellets;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        ActiveCount++;
    }

    public override void _ExitTree()
    {
        ActiveCount = Mathf.Max(0, ActiveCount - 1);
        base._ExitTree();
    }

    public override void _Ready()
    {
        AddToGroup("enemy_shots");
        CollisionLayer = 0;
        CollisionMask = 1; // Player

        AddChild(new CollisionShape2D
        {
            Name = "CollisionShape2D",
            Shape = new CircleShape2D { Radius = 8.0f }
        });

        BodyEntered += OnBodyEntered;
        ZIndex = 4;
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
        Rotation = Direction.Angle();
        QueueRedraw();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is BaseCell player && !player.IsDead)
        {
            player.TakeDamage(Damage);
        }
        QueueFree();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 8.0f, AuraColor);
        DrawCircle(Vector2.Zero, 4.5f, CoreColor);

        // Trimeric spike tips for a viral pellet silhouette
        Vector2 forward = Vector2.FromAngle(Rotation);
        Vector2 perp = forward.Orthogonal();
        DrawLine(Vector2.Zero, forward * 8.0f, CoreColor, 1.6f);
        DrawLine(Vector2.Zero, perp * 7.0f, AuraColor, 1.4f);
        DrawLine(Vector2.Zero, -perp * 7.0f, AuraColor, 1.4f);
    }
}
