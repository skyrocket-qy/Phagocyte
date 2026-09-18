using Godot;
using System;
using System.Collections.Generic;

namespace Phagocyte.Enemies;

/// <summary>
/// Staphylococcus aureus (金黃色葡萄球菌)
/// Coagulase clustered growth, secretes fibrin forming microthrombi armor.
/// </summary>
public partial class StaphEnemy : BaseEnemy
{
    public record SphereData(Vector2 Offset, float Radius, Color Color);
    private readonly List<SphereData> _clusterSpheres = new();

    public StaphEnemy()
    {
        EnemyId = "staph";
        DisplayNameKey = "PATHOGEN_STAPH_NAME";
        MaxHealth = 25.0f;
        CurrentHealth = 25.0f;
        AtpValue = 12.0f;
        BaseScore = 15;
        FloatSpeed = 35.0f;
        FibrinShield = 0; // Default 0 for basic coccus; cluster spawns set FibrinShield = 1
    }

    protected override void SetupEnemy()
    {
        // Generate 3-5 golden cocci spheres to form a staph cluster
        int count = (int)GD.RandRange(3, 5);
        Color baseGolden = new Color(0.95f, 0.78f, 0.18f, 0.95f);
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = new Vector2((float)GD.RandRange(-10.0, 10.0), (float)GD.RandRange(-10.0, 10.0));
            float radius = (float)GD.RandRange(6.5, 9.5);
            Color color = baseGolden.Lightened((float)GD.RandRange(-0.1, 0.1));
            _clusterSpheres.Add(new SphereData(offset, radius, color));
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        // 1. If Fibrin shield is intact, draw shimmering fibrin microthrombi outer armor
        if (FibrinShield > 0)
        {
            float pulse = 18.0f + Mathf.Sin((float)Time.GetTicksMsec() * 0.005f) * 2.5f;
            DrawArc(Vector2.Zero, pulse, 0, Mathf.Tau, 24, new Color(0.9f, 0.85f, 0.6f, 0.55f), 2.0f);
            for (int a = 0; a < 6; a++)
            {
                float ang = a * (Mathf.Tau / 6.0f) + (float)Time.GetTicksMsec() * 0.001f;
                Vector2 p1 = Vector2.FromAngle(ang) * (pulse - 3.0f);
                Vector2 p2 = Vector2.FromAngle(ang + 0.4f) * (pulse + 3.0f);
                DrawLine(p1, p2, new Color(1.0f, 0.95f, 0.7f, 0.7f), 1.5f);
            }
        }

        // 2. Draw golden cocci cluster with 3D sphere gradient and electron specular highlights
        foreach (var s in _clusterSpheres)
        {
            Vector2 pos = s.Offset;
            float rad = s.Radius;
            Color col = s.Color;

            // Peptidoglycan cell wall / capsule
            DrawCircle(pos, rad + 1.6f, new Color(0.65f, 0.42f, 0.05f, 0.85f));

            // Main spherical cytoplasm
            DrawCircle(pos, rad, col);

            // 3D Spherical volume light gradient
            DrawCircle(pos + new Vector2(-rad * 0.15f, -rad * 0.15f), rad * 0.72f, col.Lightened(0.18f));

            // Glossy specular highlight (electron microscope vibe with HDR glow)
            DrawCircle(pos + new Vector2(-rad * 0.32f, -rad * 0.32f), rad * 0.28f, new Color(1.4f, 1.35f, 0.9f, 0.85f));
        }
    }
}
