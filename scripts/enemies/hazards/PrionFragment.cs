using Godot;
using Phagocyte.Core;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Fragmented amyloid fibril splinter split from Prion Aggregate.
/// Contact punctures player cytoplasm.
/// </summary>
public partial class PrionFragment : BaseEnemy
{
    private const float DrawScale = 6.0f / 12.0f; // A-formula visual match: 12px -> 6px

    public PrionFragment()
    {
        EnemyId = "prion_fragment";
        DisplayNameKey = "PATHOGEN_PRION_NAME";
        MaxHealth = 25.0f;
        AtpValue = 18.0f;
        BaseScore = 5;
        FloatSpeed = 50.0f;
        ThreatMode = EnemyThreatMode.ChemoChaser;
    }

    protected override float GetCollisionRadius() => Morphology.RealSizeToRadius(0.005f);

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0.0f, new Vector2(DrawScale, DrawScale));
        // Jagged refracting crystalline beta-sheet fragment
        Color crystalCol = new Color(0.35f, 0.12f, 0.55f, 0.95f);
        Color edgeCol = new Color(0.85f, 0.35f, 1.0f, 0.85f);

        Vector2[] points = new Vector2[]
        {
            new Vector2(-10, -8),
            new Vector2(2, -12),
            new Vector2(12, -2),
            new Vector2(8, 10),
            new Vector2(-6, 8)
        };
        DrawColoredPolygon(points, crystalCol);
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 p1 = points[i];
            Vector2 p2 = points[(i + 1) % points.Length];
            DrawLine(p1, p2, edgeCol, 1.8f);
        }
        DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
    }
}
