using Godot;
using Phagocyte.Core;
using System;

namespace Phagocyte.Enemies;

/// <summary>
/// Aspergillus fumigatus (煙麴黴菌)
/// Conidiophore vesicle structure. Detonates in a toxic spore cloud upon death or digestion.
/// </summary>
public partial class AspergillusEnemy : BaseEnemy
{
    private const float DrawScale = 21.0f / 17.0f; // A-formula visual match: 17px -> 21px

    public AspergillusEnemy()
    {
        EnemyId = "aspergillus";
        DisplayNameKey = "PATHOGEN_ASPERGILLUS_NAME";
        MaxHealth = 38.0f;
        AtpValue = 22.0f;
        BaseScore = 15;
        FloatSpeed = 28.0f;
        ThreatMode = EnemyThreatMode.ChemoChaser;
    }

    protected override float GetCollisionRadius() => Morphology.RealSizeToRadius(2.5f);

    public override void Die(Node2D? killer)
    {
        SpawnToxinCloud();
        base.Die(killer);
    }

    public override void BeEngulfed(Node2D? predator)
    {
        SpawnToxinCloud();
        base.BeEngulfed(predator);
    }

    private void SpawnToxinCloud()
    {
        var parent = GetParent();
        if (parent != null)
        {
            var cloud = new SporeCloud
            {
                GlobalPosition = GlobalPosition
            };
            parent.AddChild(cloud);
        }
    }

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0.0f, new Vector2(DrawScale, DrawScale));
        // 1. Conidiophore stalk
        Color stalkCol = new Color(0.28f, 0.42f, 0.22f, 0.95f);
        DrawLine(new Vector2(0, 8), new Vector2(0, 22), stalkCol, 5.0f);

        // 2. Swollen central vesicle
        Color vesicleCol = new Color(0.35f, 0.55f, 0.25f, 0.95f);
        DrawCircle(new Vector2(0, 6), 9.0f, vesicleCol);

        // 3. Phialides & Chains of round conidia spores radiating around upper dome
        Color sporeCol = new Color(0.18f, 0.32f, 0.15f, 0.95f);
        Color tipCol = new Color(0.45f, 0.85f, 0.30f, 0.95f);

        int chains = 9;
        for (int i = 0; i < chains; i++)
        {
            float ang = Mathf.Pi * 0.85f + (i / (float)(chains - 1)) * Mathf.Pi * 1.3f;
            Vector2 dir = Vector2.FromAngle(ang);

            // Spore bead chain (3 spores per chain)
            for (int s = 1; s <= 3; s++)
            {
                Vector2 pos = new Vector2(0, 6) + dir * (9.0f + s * 4.5f);
                DrawCircle(pos, 2.8f, s == 3 ? tipCol : sporeCol);
            }
        }
        DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
    }
}
