using Godot;
using System;

namespace Phagocyte.Enemies;

/// <summary>
/// Pseudomonas aeruginosa (綠膿桿菌)
/// Secretes alginate extracellular polysaccharides, forming sticky biofilms upon death.
/// </summary>
public partial class PseudomonasEnemy : BaseEnemy
{
    private float _flagellumTime = 0.0f;

    public PseudomonasEnemy()
    {
        EnemyId = "pseudomonas";
        DisplayNameKey = "PATHOGEN_PSEUDOMONAS_NAME";
        MaxHealth = 30.0f;
        CurrentHealth = 30.0f;
        AtpValue = 14.0f;
        FloatSpeed = 42.0f;
    }

    protected override float GetCollisionRadius() => 15.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _flagellumTime += dt * 12.0f;
        if (Velocity.LengthSquared() > 1.0f)
        {
            Rotation = Mathf.LerpAngle(Rotation, Velocity.Angle(), 4.0f * dt);
        }
    }

    public override void Die(Node2D? killer)
    {
        // Drop sticky biofilm puddle on death
        var parent = GetParent();
        if (parent != null)
        {
            var biofilm = new BiofilmArea
            {
                GlobalPosition = GlobalPosition
            };
            parent.AddChild(biofilm);
        }
        base.Die(killer);
    }

    public override void BeEngulfed(Node2D? predator)
    {
        // When engulfed and digested, also spawn a biofilm residue
        var parent = GetParent();
        if (parent != null)
        {
            var biofilm = new BiofilmArea
            {
                GlobalPosition = GlobalPosition
            };
            parent.AddChild(biofilm);
        }
        base.BeEngulfed(predator);
    }

    public override void _Draw()
    {
        // 1. Pyocyanin blue-green rod capsule
        Color capsuleColor = new Color(0.15f, 0.65f, 0.45f, 0.95f);
        Color coreColor = new Color(0.25f, 0.85f, 0.60f, 0.90f);
        Color glowColor = new Color(0.40f, 1.0f, 0.70f, 0.40f);

        DrawRect(new Rect2(-14, -7, 28, 14), capsuleColor);
        DrawRect(new Rect2(-11, -4, 22, 8), coreColor);
        DrawCircle(new Vector2(14, 0), 7.0f, capsuleColor);
        DrawCircle(new Vector2(-14, 0), 7.0f, capsuleColor);

        // Highlight specular
        DrawLine(new Vector2(-8, -3), new Vector2(8, -3), new Color(1.2f, 1.5f, 1.3f, 0.7f), 1.8f);

        // 2. Single polar flagellum at tail (-14, 0)
        Vector2 prevPoint = new Vector2(-14, 0);
        for (int i = 1; i <= 6; i++)
        {
            float wave = Mathf.Sin(_flagellumTime + i * 0.8f) * 4.5f;
            Vector2 nextPoint = new Vector2(-14 - i * 5.0f, wave);
            DrawLine(prevPoint, nextPoint, new Color(0.3f, 0.8f, 0.5f, 0.65f), 1.6f);
            prevPoint = nextPoint;
        }
    }
}
