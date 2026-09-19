using Godot;
using System;

namespace Phagocyte.Enemies;

/// <summary>
/// Norovirus (諾羅病毒)
/// Non-enveloped tiny icosahedral capsid. Extreme swarm waves testing AoE and multi-engulfment.
/// </summary>
public partial class NorovirusEnemy : BaseEnemy
{
    public NorovirusEnemy()
    {
        EnemyId = "norovirus";
        DisplayNameKey = "PATHOGEN_NOROVIRUS_NAME";
        MaxHealth = 8.0f;
        CurrentHealth = 8.0f;
        AtpValue = 3.5f;
        BaseScore = 5;
        FloatSpeed = 50.0f;
        ThreatMode = EnemyThreatMode.ChemoChaser;
    }

    protected override float GetCollisionRadius() => 7.0f;

    /// <summary>Cyan capsid tint for the GPU swarm batch renderer (docs/spec.md §9).</summary>
    public override Color SwarmBatchColor => new(0.3f, 1.0f, 1.05f, 1.0f);

    public override void _Draw()
    {
        // Minuscule icosahedral capsid (hexagon/diamond)
        Color capsidColor = new Color(0.2f, 0.85f, 0.95f, 0.95f);
        Color coreColor = new Color(0.85f, 1.0f, 1.0f, 0.95f);

        Vector2[] points = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            points[i] = Vector2.FromAngle(i * (Mathf.Tau / 6.0f)) * 6.5f;
        }

        DrawColoredPolygon(points, capsidColor);
        DrawCircle(Vector2.Zero, 3.0f, coreColor);
    }
}
