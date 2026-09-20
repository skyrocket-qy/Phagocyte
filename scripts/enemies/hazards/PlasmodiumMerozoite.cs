using Godot;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Merozoite stage of Plasmodium falciparum released upon RBC rupture.
/// Rapidly swarms and attacks the player.
/// </summary>
public partial class PlasmodiumMerozoite : BaseEnemy
{
    public PlasmodiumMerozoite()
    {
        EnemyId = "plasmodium_merozoite";
        DisplayNameKey = "PATHOGEN_PLASMODIUM_NAME";
        MaxHealth = 10.0f;
        AtpValue = 4.0f;
        BaseScore = 5;
        FloatSpeed = 85.0f;
        ThreatMode = EnemyThreatMode.ChemoChaser;
    }

    protected override float GetCollisionRadius() => 7.0f;

    public override void _Draw()
    {
        // Teardrop pear shape
        Color bodyColor = new Color(0.75f, 0.22f, 0.35f, 0.95f);
        Color apicalColor = new Color(1.0f, 0.4f, 0.5f, 1.0f);

        DrawCircle(Vector2.Zero, 6.0f, bodyColor);
        DrawCircle(new Vector2(0, -3), 2.5f, apicalColor);
    }
}
