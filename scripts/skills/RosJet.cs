using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Enemies;

namespace Phagocyte.Skills;

public partial class RosJet : Area2D
{
    public Vector2 Direction { get; set; } = Vector2.Right;
    public float Speed { get; set; } = 520.0f;
    public float Lifetime { get; set; } = 0.9f;
    public float Damage { get; set; } = 25.0f;
    public bool IsCrit { get; set; } = false;
    public Node2D? Predator { get; set; } = null;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 2; // Pathogens
        AreaEntered += OnAreaEntered;
        QueueRedraw();
    }

    public void Setup(Node2D pPredator, Vector2 pPos, Vector2 pDir)
    {
        Predator = pPredator;
        GlobalPosition = pPos;
        Direction = pDir.Normalized();
        Rotation = Direction.Angle();
    }

    public override void _PhysicsProcess(double delta)
    {
        Position += Direction * Speed * (float)delta;
        Lifetime -= (float)delta;
        if (Lifetime <= 0.0f)
        {
            QueueFree();
        }
    }

    public override void _Draw()
    {
        // High-pressure peroxide jet stream beam
        DrawLine(new Vector2(-24, 0), new Vector2(24, 0), new Color(0.9f, 1.0f, 1.0f, 0.95f), 8.0f);
        DrawLine(new Vector2(-30, 0), new Vector2(30, 0), new Color(0.3f, 0.9f, 1.0f, 0.6f), 14.0f);
        DrawCircle(new Vector2(20, 0), 6.0f, new Color(1, 1, 1, 0.9f));
    }

    private void OnAreaEntered(Area2D area)
    {
        var enemy = area.GetParent();
        CombatHelper.DealDamage(enemy, Damage, Predator, IsCrit);
    }
}
