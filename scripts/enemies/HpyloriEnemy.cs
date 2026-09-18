using Godot;
using System;

namespace Phagocyte.Enemies;

/// <summary>
/// Helicobacter pylori (幽門螺旋桿菌)
/// Helical corkscrew morphology drilling with alkaline urease shield neutralizing acid damage.
/// </summary>
public partial class HpyloriEnemy : BaseEnemy
{
    private float _spinAngle = 0.0f;

    public HpyloriEnemy()
    {
        EnemyId = "h_pylori";
        DisplayNameKey = "PATHOGEN_HPYLORI_NAME";
        MaxHealth = 32.0f;
        CurrentHealth = 32.0f;
        AtpValue = 16.0f;
        BaseScore = 35;
        FloatSpeed = 58.0f;
        Armor = 1.0f;
    }

    protected override float GetCollisionRadius() => 14.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _spinAngle += dt * 12.0f;
        Rotation += dt * 3.0f;
    }

    public override void _Draw()
    {
        // 1. Alkaline urease cloud / halo
        Color alkalineAura = new Color(0.35f, 0.75f, 0.95f, 0.25f);
        DrawCircle(Vector2.Zero, 18.0f, alkalineAura);

        // 2. Helical corkscrew body
        Color bodyColor = new Color(0.85f, 0.42f, 0.22f, 0.95f);
        Color coreColor = new Color(1.0f, 0.65f, 0.35f, 0.95f);

        Vector2 prev = new Vector2(-16, Mathf.Sin(_spinAngle) * 5.0f);
        for (int i = 1; i <= 10; i++)
        {
            float t = (float)i / 10.0f;
            float x = -16.0f + t * 32.0f;
            float y = Mathf.Sin(_spinAngle + t * Mathf.Tau * 2.0f) * 6.0f;
            Vector2 curr = new Vector2(x, y);
            DrawLine(prev, curr, bodyColor, 5.0f);
            DrawLine(prev, curr, coreColor, 2.5f);
            prev = curr;
        }

        // 3. Unipolar sheathed flagella tuft at head (16, 0)
        for (int f = -2; f <= 2; f++)
        {
            Vector2 fTip = new Vector2(24, f * 3.5f + Mathf.Sin(_spinAngle * 1.5f + f) * 4.0f);
            DrawLine(new Vector2(16, 0), fTip, new Color(0.9f, 0.6f, 0.3f, 0.7f), 1.4f);
        }
    }
}
