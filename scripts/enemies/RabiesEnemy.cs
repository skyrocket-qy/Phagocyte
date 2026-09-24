using Godot;
using Phagocyte.Core;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Rabies Lyssavirus (ç‹‚çŠ¬ç—…ç—…æ¯’)
/// Bullet-shaped virion. High-frequency 90-degree zig-zag trajectory; inverts player movement keys on collision.
/// </summary>
public partial class RabiesEnemy : BaseEnemy
{
    private float _zigZagTimer = 0.0f;
    private Vector2 _currentTrajectory = Vector2.Right;
    private float _attackCooldown = 0.0f;
    private const float DrawScale = 10.0f / 13.0f; // A-formula visual match: 13px -> 10px

    public RabiesEnemy()
    {
        EnemyId = "rabies";
        DisplayNameKey = "PATHOGEN_RABIES_NAME";
        MaxHealth = 22.0f;
        AtpValue = 16.0f;
        BaseScore = 35;
        FloatSpeed = 65.0f;
        ThreatMode = EnemyThreatMode.Interceptor;
    }

    protected override float GetCollisionRadius() => Morphology.RealSizeToRadius(0.18f);

    protected override void SetupEnemy()
    {
        _currentTrajectory = Vector2.FromAngle(GD.Randf() * Mathf.Tau);
    }

    protected override void CustomPhysicsProcess(float dt)
    {
        _zigZagTimer += dt;
        _attackCooldown -= dt;

        // Sharp 90-degree turn every 0.4s
        if (_zigZagTimer >= 0.4f)
        {
            _zigZagTimer = 0.0f;
            // Turn left or right 90 degrees with forward bias towards player
            var player = PlayerRef;
            float turnSign = GD.Randf() > 0.5f ? 1.0f : -1.0f;
            _currentTrajectory = _currentTrajectory.Rotated(Mathf.Pi * 0.5f * turnSign);

            if (player != null && GodotObject.IsInstanceValid(player))
            {
                Vector2 toPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
                _currentTrajectory = (_currentTrajectory + toPlayer * 0.6f).Normalized();
            }
            Rotation = _currentTrajectory.Angle();
        }

        Velocity = _currentTrajectory * FloatSpeed;
        Position += Velocity * dt;

        // Check collision with player
        var p = PlayerRef;
        if (p != null && GodotObject.IsInstanceValid(p) && _attackCooldown <= 0.0f)
        {
            if (IsTouchingPlayer(12.0f))
            {
                _attackCooldown = 2.5f;
                p.TakeDamage(8.0f);
                p.ApplyInvertControls(2.0f); // Invert player movement keys for 2 seconds!
            }
        }
    }

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0.0f, new Vector2(DrawScale, DrawScale));
        // Bullet-shaped virion (flat base at left, hemispherical dome at right)
        Color envelopeCol = new Color(0.95f, 0.55f, 0.12f, 0.95f);
        Color rnpCoreCol = new Color(1.0f, 0.85f, 0.25f, 0.95f);

        // Cylindrical trunk
        DrawRect(new Rect2(-10, -6, 12, 12), envelopeCol);
        // Hemispherical bullet nose
        DrawCircle(new Vector2(2, 0), 6.0f, envelopeCol);
        // Flat base cap
        DrawLine(new Vector2(-10, -6), new Vector2(-10, 6), envelopeCol.Darkened(0.2f), 2.0f);

        // Striated RNP core helical coil
        for (int x = -8; x <= 0; x += 3)
        {
            DrawLine(new Vector2(x, -4), new Vector2(x, 4), rnpCoreCol, 1.5f);
        }

        // Tiny surface spikes
        for (int y = -6; y <= 6; y += 4)
        {
            DrawLine(new Vector2(-10, y), new Vector2(-13, y), envelopeCol, 1.2f);
        }
        DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
    }
}
