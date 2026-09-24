using Godot;
using Phagocyte.Core;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Clostridium tetani (ç ´å‚·é¢¨æ¢­èŒ)
/// Drumstick terminal spore morphology. Ranged controller firing tetanospasmin electrical pulses.
/// </summary>
public partial class TetanusEnemy : BaseEnemy
{
    private float _attackTimer = 0.0f;
    private const float AttackCooldown = 4.0f;
    private const float DrawScale = 21.0f / 15.0f; // A-formula visual match: 15px -> 21px

    public TetanusEnemy()
    {
        EnemyId = "tetanus";
        DisplayNameKey = "PATHOGEN_TETANUS_NAME";
        MaxHealth = 30.0f;
        AtpValue = 20.0f;
        BaseScore = 35;
        FloatSpeed = 35.0f;
        ThreatMode = EnemyThreatMode.Standoff;
    }

    protected override float GetCollisionRadius() => Morphology.RealSizeToRadius(2.5f);

    // Tetanus keeps its bespoke 350-500px firing band instead of the generic orbit.
    protected override bool UseGenericSteering => false;

    protected override void CustomPhysicsProcess(float dt)
    {
        _attackTimer += dt;
        var player = PlayerRef;
        if (player == null || !GodotObject.IsInstanceValid(player))
            return;

        Vector2 diff = player.GlobalPosition - GlobalPosition;
        float dist = diff.Length();
        Vector2 dir = diff.Normalized();

        Rotation = dir.Angle();

        // Maintain distance of ~380-450px
        if (dist < 350.0f)
        {
            // Back away
            Velocity = Velocity.Lerp(-dir * FloatSpeed * 1.2f, 2.0f * dt);
        }
        else if (dist > 500.0f)
        {
            // Approach
            Velocity = Velocity.Lerp(dir * FloatSpeed, 2.0f * dt);
        }

        // Fire electrical pulse
        if (_attackTimer >= AttackCooldown)
        {
            _attackTimer = 0.0f;
            FirePulse(dir);
        }
    }

    private void FirePulse(Vector2 dir)
    {
        var parent = GetParent();
        if (parent == null)
            return;

        var pulse = new TetanusPulse
        {
            GlobalPosition = GlobalPosition + dir * 20.0f,
            Direction = dir
        };
        parent.AddChild(pulse);
    }

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0.0f, new Vector2(DrawScale, DrawScale));
        // Drumstick / tennis racket morphology
        Color rodColor = new Color(0.35f, 0.28f, 0.45f, 0.95f);
        Color sporeColor = new Color(0.65f, 0.55f, 0.78f, 0.95f);

        // Rod body (-16 to 6)
        DrawLine(new Vector2(-16, 0), new Vector2(6, 0), rodColor, 6.0f);
        // Swollen terminal spore at tip (12, 0)
        DrawCircle(new Vector2(10, 0), 8.5f, sporeColor);
        DrawCircle(new Vector2(10, 0), 5.5f, new Color(0.85f, 0.75f, 0.95f, 0.9f));

        // Ranged charge spark
        if (_attackTimer > AttackCooldown - 0.8f)
        {
            float sparkPulse = Mathf.Sin(_attackTimer * 30.0f) * 4.0f;
            DrawCircle(new Vector2(10, 0), 10.0f + sparkPulse, new Color(0.4f, 0.8f, 1.0f, 0.4f));
        }
        DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
    }
}
