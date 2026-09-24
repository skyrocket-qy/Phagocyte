using Godot;
using Phagocyte.Core;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Mycobacterium tuberculosis (結核分枝桿菌)
/// Thick waxy mycolic acid cell wall. Acid-resistant: slows digestion by 70% and burns player cytoplasm.
/// </summary>
public partial class TbEnemy : BaseEnemy
{
    private const float DrawScale = 23.0f / 14.0f; // A-formula visual match: 14px -> 23px

    public TbEnemy()
    {
        EnemyId = "tb";
        DisplayNameKey = "PATHOGEN_TB_NAME";
        MaxHealth = 35.0f;
        AtpValue = 18.0f;
        BaseScore = 100;
        FloatSpeed = 30.0f;
        ThreatMode = EnemyThreatMode.ChemoChaser;
        Armor = 2.0f; // Mycolic wax absorbs damage
    }

    protected override float GetCollisionRadius() => Morphology.RealSizeToRadius(3.0f);

    // Acid-resistant mycolic wall: digestion is slow and burns the predator.
    protected override float EngulfDigestDuration => 0.75f;

    protected override void OnEngulfedBy(Node2D? predator)
    {
        if (predator is BaseCell player)
        {
            // Apply digestion burn: deals 4 dps to player cytoplasm
            player.ApplyTBDigestionBurn(2.5f, 4.0f);
        }
    }

    protected override void PlayDigestionVfx(Vector2 pos)
    {
        // No lysis burst: the waxy wall smoulders out instead.
    }

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0.0f, new Vector2(DrawScale, DrawScale));
        // 1. Thick waxy mycolic acid outer boundary (glossy yellow-magenta)
        Color waxBorder = new Color(0.9f, 0.3f, 0.5f, 0.85f);
        Color acidCore = new Color(0.6f, 0.1f, 0.25f, 0.95f);

        // Slender curved arc body
        Vector2[] points = new Vector2[]
        {
            new Vector2(-15, -4),
            new Vector2(-6, 2),
            new Vector2(6, 2),
            new Vector2(15, -4)
        };

        // Draw thick rod
        for (int i = 0; i < points.Length - 1; i++)
        {
            DrawLine(points[i], points[i + 1], waxBorder, 8.0f);
            DrawLine(points[i], points[i + 1], acidCore, 4.5f);
        }

        // Acid-fast beaded granules
        DrawCircle(new Vector2(-8, 0), 2.0f, Colors.White);
        DrawCircle(new Vector2(0, 2), 2.0f, Colors.White);
        DrawCircle(new Vector2(8, 0), 2.0f, Colors.White);
        DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
    }
}
