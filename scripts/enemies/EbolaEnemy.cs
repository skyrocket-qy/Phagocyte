using Godot;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Ebolavirus (ä¼Šæ³¢æ‹‰çµ²ç‹€ç—…æ¯’)
/// Long filamentous flexible virion with shepherd's crook morphology. Sweeping tail attacks bypass armor.
/// </summary>
public partial class EbolaEnemy : BaseEnemy
{
    private float _undulateTime = 0.0f;
    private const int SegmentCount = 10;
    private readonly Vector2[] _bodySegments = new Vector2[SegmentCount];
    private float _tailSweepCooldown = 0.0f;

    public EbolaEnemy()
    {
        EnemyId = "ebola";
        DisplayNameKey = "PATHOGEN_EBOLA_NAME";
        MaxHealth = 42.0f;
        CurrentHealth = 42.0f;
        AtpValue = 24.0f;
        BaseScore = 35;
        FloatSpeed = 38.0f;
        ThreatMode = EnemyThreatMode.ChemoChaser;
    }

    protected override float GetCollisionRadius() => 16.0f;

    protected override void SetupEnemy()
    {
        for (int i = 0; i < SegmentCount; i++)
        {
            _bodySegments[i] = new Vector2(-i * 7.0f, 0);
        }
    }

    protected override void CustomPhysicsProcess(float dt)
    {
        _undulateTime += dt * 5.0f;
        _tailSweepCooldown -= dt;

        if (Velocity.LengthSquared() > 1.0f)
        {
            Rotation = Mathf.LerpAngle(Rotation, Velocity.Angle(), 3.0f * dt);
        }

        // Filamentous chain inverse kinematics / organic tail undulation
        _bodySegments[0] = Vector2.Zero;
        for (int i = 1; i < SegmentCount; i++)
        {
            float wave = Mathf.Sin(_undulateTime + i * 0.6f) * (i * 2.2f);
            Vector2 target = new Vector2(-i * 8.0f, wave);
            _bodySegments[i] = _bodySegments[i].Lerp(target, 8.0f * dt);
        }

        // Tail sweep check with player
        var player = PlayerRef;
        if (player != null && GodotObject.IsInstanceValid(player) && _tailSweepCooldown <= 0.0f)
        {
            // Check tail tip position in global space
            Vector2 tailGlobal = ToGlobal(_bodySegments[SegmentCount - 1]);
            if (tailGlobal.DistanceTo(player.GlobalPosition) < (player.CurrentRadius + 14.0f))
            {
                _tailSweepCooldown = 2.0f;
                // Direct core true damage bypass
                player.TakeDamage(14.0f);
            }
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        Color filamentColor = new Color(0.72f, 0.12f, 0.22f, 0.95f);
        Color coreRnaColor = new Color(0.95f, 0.35f, 0.45f, 0.95f);

        // Draw segmented filamentous body
        for (int i = 0; i < SegmentCount - 1; i++)
        {
            DrawLine(_bodySegments[i], _bodySegments[i + 1], filamentColor, 7.0f);
            DrawLine(_bodySegments[i], _bodySegments[i + 1], coreRnaColor, 3.0f);
        }

        // Shepherd's crook terminal hook at head
        DrawArc(new Vector2(6, -4), 6.0f, 0, Mathf.Pi * 1.5f, 12, filamentColor, 6.0f);
        DrawCircle(new Vector2(6, -4), 3.0f, coreRnaColor);
    }
}
