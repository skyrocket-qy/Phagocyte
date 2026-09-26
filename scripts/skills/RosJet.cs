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

    private float _phase = 0.0f;

    public override void _PhysicsProcess(double delta)
    {
        Position += Direction * Speed * (float)delta;
        Lifetime -= (float)delta;
        _phase += (float)delta * 22.0f;
        QueueRedraw();
        if (Lifetime <= 0.0f)
        {
            QueueFree();
        }
    }

    public override void _Draw()
    {
        Color halo = SkillAssetPalette.Accent(SkillIds.RosTorrent, new Color(0.17f, 0.82f, 0.72f));
        Color core = SkillAssetPalette.Core(SkillIds.RosTorrent, new Color(0.9f, 1.0f, 1.0f));

        // 1. Ambient oxidative cloud (diffuse mist)
        DrawLine(new Vector2(-45, 0), new Vector2(40, 0), halo with { A = 0.22f }, 24.0f);

        // 2. High-pressure peroxide jet stream
        LaserGlow.DrawBeam(this, new Vector2(-40, 0), new Vector2(40, 0), halo, core, 16.0f, 0.95f);

        // 3. Fluid shear boundary lines (undulating cavitation envelope)
        float wave1 = Mathf.Sin(_phase) * 3.5f;
        float wave2 = Mathf.Cos(_phase * 1.3f) * 3.5f;
        DrawLine(new Vector2(-35, -8.0f + wave1), new Vector2(30, -5.0f - wave1), halo with { A = 0.6f }, 2.0f);
        DrawLine(new Vector2(-35, 8.0f - wave2), new Vector2(30, 5.0f + wave2), halo with { A = 0.6f }, 2.0f);

        // 4. Oscillating cavitation micro-bubbles
        for (int i = 0; i < 5; i++)
        {
            float t = (i / 4.0f);
            float bx = Mathf.Lerp(-30.0f, 25.0f, t);
            float by = Mathf.Sin(_phase + i * 1.6f) * (6.0f * (1.0f - t * 0.5f));
            float r = (i % 2 == 0) ? 2.5f : 1.8f;
            DrawCircle(new Vector2(bx, by), r + 1.0f, halo with { A = 0.45f });
            DrawCircle(new Vector2(bx, by), r, core with { A = 0.9f });
        }

        // 5. Leading oxidative shock cone & burst halo
        LaserGlow.DrawImpactHalo(this, new Vector2(35, 0), 14.0f, halo, core, 0.95f, 2.2f);
    }

    private void OnAreaEntered(Area2D area)
    {
        var enemy = area.GetParent();
        CombatHelper.DealDamage(enemy, Damage, Predator, IsCrit);
        VfxManager.Instance?.Play(VfxType.AcidOxidationSparks, GlobalPosition);

        if (enemy is Node node && node.GetNodeOrNull<AilmentController>("AilmentController") is AilmentController ac)
        {
            ac.ApplyOxidativeBurn(Damage * 0.35f, 2.0f);
        }
    }
}
