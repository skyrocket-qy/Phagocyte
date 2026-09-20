using Godot;
using System;

namespace Phagocyte.Enemies;

/// <summary>
/// Plasmodium falciparum infected RBC Carrier (惡性瘧原蟲)
/// Disguised as a floating host erythrocyte. Ruptures into 6-8 merozoites upon damage.
/// </summary>
public partial class PlasmodiumCarrierEnemy : BaseEnemy
{
    private bool _hasRuptured = false;

    public PlasmodiumCarrierEnemy()
    {
        EnemyId = "plasmodium";
        DisplayNameKey = "PATHOGEN_PLASMODIUM_NAME";
        MaxHealth = 30.0f;
        AtpValue = 28.0f;
        BaseScore = 15;
        FloatSpeed = 25.0f;
        ThreatMode = EnemyThreatMode.ChemoChaser;
    }

    protected override float GetCollisionRadius() => 18.0f;

    protected override void OnPreDamage(float damage, Node2D? source, bool isCrit)
    {
        RuptureIntoMerozoites();
    }

    public override void Die(Node2D? killer)
    {
        RuptureIntoMerozoites();
        base.Die(killer);
    }

    private void RuptureIntoMerozoites()
    {
        if (_hasRuptured)
            return;
        _hasRuptured = true;

        var parent = GetParent();
        if (parent == null)
            return;

        int count = (int)GD.RandRange(6, 8);
        for (int i = 0; i < count; i++)
        {
            float ang = i * (Mathf.Tau / count) + (float)GD.RandRange(-0.3, 0.3);
            Vector2 spawnOffset = Vector2.FromAngle(ang) * (float)GD.RandRange(10, 25);

            var merozoite = new PlasmodiumMerozoite
            {
                GlobalPosition = GlobalPosition + spawnOffset,
                Velocity = Vector2.FromAngle(ang) * 90.0f
            };
            parent.AddChild(merozoite);
        }
    }

    public override void _Draw()
    {
        // 1. Erythrocyte biconcave outer ring
        Color rbcRim = new Color(0.78f, 0.18f, 0.18f, 0.95f);
        Color rbcCenter = new Color(0.55f, 0.12f, 0.12f, 0.95f);

        DrawCircle(Vector2.Zero, 17.0f, rbcRim);
        DrawCircle(Vector2.Zero, 11.0f, rbcCenter);

        // 2. Intraerythrocytic parasite ring/trophozoite dots inside
        Color parasiteCol = new Color(0.35f, 0.15f, 0.55f, 0.95f);
        Color chromatinDot = new Color(0.95f, 0.25f, 0.35f, 1.0f);

        DrawCircle(new Vector2(-3, -2), 4.5f, parasiteCol);
        DrawCircle(new Vector2(-3, -2), 2.5f, rbcCenter);
        DrawCircle(new Vector2(-1, -4), 1.8f, chromatinDot);

        DrawCircle(new Vector2(4, 3), 3.5f, parasiteCol);
        DrawCircle(new Vector2(5, 2), 1.5f, chromatinDot);
    }
}
