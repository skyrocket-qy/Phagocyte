using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;
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
        // High-pressure peroxide jet stream beam, tinted from the skill icon.
        Color halo = SkillAssetPalette.Accent(SkillIds.RosTorrent, new Color(0.3f, 0.9f, 1.0f));
        Color core = SkillAssetPalette.Core(SkillIds.RosTorrent, new Color(0.9f, 1.0f, 1.0f));
        LaserGlow.DrawBeam(this, new Vector2(-30, 0), new Vector2(30, 0), halo, core, 12.0f, 0.9f);
        LaserGlow.DrawImpactHalo(this, new Vector2(20, 0), 10.0f, halo, core, 0.9f, 2.0f);
    }

    private void OnAreaEntered(Area2D area)
    {
        var enemy = area.GetParent();
        CombatHelper.DealDamage(enemy, Damage, Predator, IsCrit);
    }
}
