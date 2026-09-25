using Godot;
using Phagocyte.Core;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Prion Aggregate (普利昂蛋白聚集體)
/// Assimilation Boss. Misfolded beta-sheet amyloid fibrils resistant to proteases; fragments upon damage.
/// </summary>
public partial class PrionEnemy : BaseEnemy
{
    private const float DrawScale = 6.0f / 28.0f; // A-formula visual match: 28px -> 6px

    public PrionEnemy()
    {
        EnemyId = "prion";
        DisplayNameKey = "PATHOGEN_PRION_NAME";
        MaxHealth = 150.0f;
        Armor = 4.0f;
        AtpValue = 60.0f;
        BaseScore = 100;
        FloatSpeed = 30.0f;
        ThreatMode = EnemyThreatMode.ChemoChaser;
        IsBoss = true;
    }

    protected override float GetCollisionRadius() => Morphology.RealSizeToRadius(0.005f);

    protected override void OnPostDamage(float damage, Node2D? source, bool isCrit)
    {
        if (CurrentHealth <= MaxHealth * 0.5f && TryConsumeSplitBurst())
            SpawnFragments();
    }

    private void SpawnFragments()
    {
        var parent = GetParent();
        if (parent == null)
            return;

        for (int i = 0; i < 2; i++)
        {
            var frag = new PrionFragment
            {
                GlobalPosition = GlobalPosition + new Vector2((i == 0 ? -25 : 25), (float)GD.RandRange(-15, 15)),
                Velocity = new Vector2(i == 0 ? -60 : 60, 0)
            };
            parent.AddChild(frag);
        }
    }

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0.0f, new Vector2(DrawScale, DrawScale));
        // 1. Unearthly violet amyloid aura
        DrawCircle(Vector2.Zero, 34.0f, new Color(0.5f, 0.1f, 0.8f, 0.22f));

        // 2. Jagged intersecting beta-sheet crystalline facets
        Color baseObsidian = new Color(0.18f, 0.08f, 0.28f, 0.95f);
        Color crystalEdge = new Color(0.85f, 0.35f, 1.0f, 0.95f);

        Vector2[] facet1 = new Vector2[]
        {
            new Vector2(-22, -14),
            new Vector2(4, -26),
            new Vector2(26, -6),
            new Vector2(10, 18),
            new Vector2(-16, 12)
        };
        DrawColoredPolygon(facet1, baseObsidian);
        for (int i = 0; i < facet1.Length; i++)
        {
            DrawLine(facet1[i], facet1[(i + 1) % facet1.Length], crystalEdge, 2.2f);
        }

        Vector2[] facet2 = new Vector2[]
        {
            new Vector2(-12, -20),
            new Vector2(18, -16),
            new Vector2(22, 14),
            new Vector2(-8, 22),
            new Vector2(-24, 0)
        };
        for (int i = 0; i < facet2.Length; i++)
        {
            DrawLine(facet2[i], facet2[(i + 1) % facet2.Length], new Color(1.0f, 0.6f, 1.0f, 0.8f), 1.5f);
        }
        DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
    }
}
