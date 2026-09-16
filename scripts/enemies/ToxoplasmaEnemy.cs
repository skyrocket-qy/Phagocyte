using Godot;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Toxoplasma gondii (剛地弓形蟲)
/// Crescent pseudocyst parasite. Periodically emits psychic neurotransmitter waves pulling player.
/// </summary>
public partial class ToxoplasmaEnemy : BaseEnemy
{
    private float _pulseTimer = 0.0f;
    private const float PulseInterval = 5.5f;
    private float _pullDuration = 0.0f;

    public ToxoplasmaEnemy()
    {
        EnemyId = "toxoplasma";
        DisplayNameKey = "PATHOGEN_TOXOPLASMA_NAME";
        MaxHealth = 40.0f;
        CurrentHealth = 40.0f;
        AtpValue = 25.0f;
        FloatSpeed = 32.0f;
    }

    protected override float GetCollisionRadius() => 16.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _pulseTimer += dt;
        if (_pulseTimer >= PulseInterval)
        {
            _pulseTimer = 0.0f;
            _pullDuration = 1.6f;
            QueueRedraw();
        }

        if (_pullDuration > 0.0f)
        {
            _pullDuration -= dt;
            var player = (BaseCell?)GetTree().GetFirstNodeInGroup("player");
            if (player != null && GodotObject.IsInstanceValid(player))
            {
                // Pull player towards this parasite or hazard
                Vector2 pullDir = (GlobalPosition - player.GlobalPosition).Normalized();
                player.Velocity += pullDir * 180.0f * dt;
            }
        }
    }

    public override void _Draw()
    {
        // 1. Psychic gravitational wave ripple when pulling
        if (_pullDuration > 0.0f)
        {
            float rippleRad = 20.0f + (1.6f - _pullDuration) * 60.0f;
            DrawArc(Vector2.Zero, rippleRad, 0, Mathf.Tau, 32, new Color(0.6f, 0.2f, 0.9f, 0.6f * (_pullDuration / 1.6f)), 2.5f);
        }

        // 2. Crescent banana-shaped tachyzoite body
        Color bodyColor = new Color(0.45f, 0.25f, 0.65f, 0.95f);
        Color coreColor = new Color(0.75f, 0.45f, 0.95f, 0.95f);
        Color conoidColor = new Color(1.0f, 0.7f, 0.3f, 1.0f);

        Vector2[] crescentPoints = new Vector2[]
        {
            new Vector2(-12, -8),
            new Vector2(-4, -14),
            new Vector2(8, -10),
            new Vector2(14, 0),
            new Vector2(10, 10),
            new Vector2(2, 6),
            new Vector2(-4, -2)
        };
        DrawColoredPolygon(crescentPoints, bodyColor);

        // Nucleus
        DrawCircle(new Vector2(2, -2), 4.5f, coreColor);
        // Conoid apical complex at anterior tip
        DrawCircle(new Vector2(-12, -8), 3.0f, conoidColor);
    }
}
