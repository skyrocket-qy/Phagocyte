using Godot;
using System;

namespace Phagocyte.Enemies;

/// <summary>
/// Bacillus anthracis Spore (炭疽桿菌芽孢)
/// Two-stage armored endospore. Extremely resistant to biochemical damage; shatters to hatch vegetative bacillus.
/// </summary>
public partial class AnthraxSporeEnemy : BaseEnemy
{
    public override bool CanBeEngulfed => false; // Dormant hard shell cannot be engulfed

    public AnthraxSporeEnemy()
    {
        EnemyId = "anthrax_spore";
        DisplayNameKey = "PATHOGEN_ANTHRAX_NAME";
        MaxHealth = 45.0f;
        CurrentHealth = 45.0f;
        Armor = 3.0f;
        FloatSpeed = 25.0f;
        AtpValue = 10.0f;
    }

    protected override float GetCollisionRadius() => 16.0f;

    public override void Die(Node2D? killer)
    {
        // Hatch into virulent AnthraxBacillus
        var parent = GetParent();
        if (parent != null)
        {
            var bacillus = new AnthraxBacillus
            {
                GlobalPosition = GlobalPosition
            };
            parent.AddChild(bacillus);
        }
        base.Die(killer);
    }

    public override void OnEngulfAttemptFailed(Node2D? predator)
    {
        // Spore deflects engulfment and chips for 5 damage to spore shell
        TakeDamage(5.0f, predator);
    }

    public override void _Draw()
    {
        // Thick multi-layered endospore shell (exosporium + cortex)
        Color outerExosporium = new Color(0.25f, 0.22f, 0.28f, 0.95f);
        Color cortexColor = new Color(0.55f, 0.48f, 0.62f, 0.95f);
        Color coreDnaColor = new Color(0.85f, 0.78f, 0.95f, 0.90f);

        // Outer hexagon/oval shell
        DrawCircle(Vector2.Zero, 16.0f, outerExosporium);
        DrawCircle(Vector2.Zero, 12.0f, cortexColor);
        DrawCircle(Vector2.Zero, 7.0f, coreDnaColor);

        // Armor cracks when damaged
        float healthPct = CurrentHealth / MaxHealth;
        if (healthPct < 0.7f)
        {
            DrawLine(new Vector2(-12, -4), new Vector2(4, 8), Colors.White, 1.5f);
        }
        if (healthPct < 0.35f)
        {
            DrawLine(new Vector2(-6, 10), new Vector2(10, -6), Colors.White, 1.5f);
        }

        // Heavy shell rim
        DrawArc(Vector2.Zero, 16.0f, 0, Mathf.Tau, 16, new Color(0.12f, 0.10f, 0.15f, 1.0f), 2.5f);
    }
}
