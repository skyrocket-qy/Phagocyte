using Godot;
using System;

namespace Phagocyte.Enemies;

/// <summary>
/// Malignant Mutated Tumor Cell (異變腫瘤細胞)
/// Large volume, downregulates surface MHC-I. Immune to ranged auto-targeting; only destructible via melee engulfment or lysis.
/// </summary>
public partial class MalignantCellEnemy : BaseEnemy
{
    private float _membranePhase = 0.0f;

    public MalignantCellEnemy()
    {
        EnemyId = "malignant_cell";
        DisplayNameKey = "PATHOGEN_MALIGNANT_NAME";
        MaxHealth = 120.0f;
        CurrentHealth = 120.0f;
        AtpValue = 48.0f;
        FloatSpeed = 26.0f;
        Armor = 3.0f;
        IsElite = true;
    }

    protected override float GetCollisionRadius() => 34.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _membranePhase += dt * 3.5f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // 1. Irregular deformed cytoplasm with cancerous blebbing
        Color cytoplasmColor = new Color(0.32f, 0.12f, 0.18f, 0.85f);
        Color membraneColor = new Color(0.75f, 0.22f, 0.32f, 0.95f);

        int pointsCount = 24;
        Vector2[] poly = new Vector2[pointsCount];
        for (int i = 0; i < pointsCount; i++)
        {
            float ang = i * (Mathf.Tau / pointsCount);
            float deform = Mathf.Sin(_membranePhase + i * 1.5f) * 4.5f + Mathf.Cos(_membranePhase * 0.7f + i * 2.1f) * 3.0f;
            poly[i] = Vector2.FromAngle(ang) * (32.0f + deform);
        }

        DrawColoredPolygon(poly, cytoplasmColor);
        for (int i = 0; i < pointsCount; i++)
        {
            Vector2 p1 = poly[i];
            Vector2 p2 = poly[(i + 1) % pointsCount];
            DrawLine(p1, p2, membraneColor, 2.5f);
        }

        // 2. Multi-nucleated pleomorphic dark nuclei (cancerous hallmarks)
        Color nucleusCol = new Color(0.18f, 0.05f, 0.22f, 0.95f);
        Color chromatinCol = new Color(0.55f, 0.15f, 0.65f, 0.90f);

        Vector2[] nucleusPositions = new Vector2[]
        {
            new Vector2(-10, -6),
            new Vector2(8, -8),
            new Vector2(2, 9)
        };

        foreach (var np in nucleusPositions)
        {
            DrawCircle(np, 9.0f, nucleusCol);
            DrawCircle(np, 5.5f, chromatinCol);
        }
    }
}
