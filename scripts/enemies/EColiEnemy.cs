using Godot;
using Phagocyte.Core;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Escherichia coli (éž­æ¯›å¤§è…¸æ¡¿èŒ)
/// Peritrichous flagella rotation with chemotactic high-speed straight-line charging dashes.
/// </summary>
public partial class EColiEnemy : BaseEnemy
{
    private enum State { Drifting, Windup, Charging, Cooldown }
    private State _currentState = State.Drifting;

    private float _stateTimer = 0.0f;
    private Vector2 _chargeTargetDir = Vector2.Zero;
    private float _flagellaPhase = 0.0f;
    private const float DrawScale = 20.0f / 15.0f; // A-formula visual match: 15px -> 20px

    public EColiEnemy()
    {
        EnemyId = "e_coli";
        DisplayNameKey = "PATHOGEN_ECOLI_NAME";
        MaxHealth = 28.0f;
        AtpValue = 15.0f;
        BaseScore = 15;
        FloatSpeed = 45.0f;
        ThreatMode = EnemyThreatMode.ChemoChaser;
    }

    protected override float GetCollisionRadius() => Morphology.RealSizeToRadius(2.0f);

    protected override void CustomPhysicsProcess(float dt)
    {
        _stateTimer += dt;
        _flagellaPhase += dt * 10.0f;

        var player = PlayerRef;

        switch (_currentState)
        {
            case State.Drifting:
                if (_stateTimer >= 3.0f && player != null && GodotObject.IsInstanceValid(player))
                {
                    float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
                    if (dist < 500.0f)
                    {
                        _currentState = State.Windup;
                        _stateTimer = 0.0f;
                        _chargeTargetDir = (player.GlobalPosition - GlobalPosition).Normalized();
                        Rotation = _chargeTargetDir.Angle();
                        Velocity = Vector2.Zero;
                    }
                }
                break;

            case State.Windup:
                Velocity = Velocity.MoveToward(Vector2.Zero, 100.0f * dt);
                if (_stateTimer >= 0.6f)
                {
                    _currentState = State.Charging;
                    _stateTimer = 0.0f;
                    Velocity = _chargeTargetDir * 320.0f;
                }
                break;

            case State.Charging:
                Position += Velocity * dt;
                // Check collision with player
                if (player != null && GodotObject.IsInstanceValid(player))
                {
                    if (GlobalPosition.DistanceTo(player.GlobalPosition) < (player.CurrentRadius + GetCollisionRadius()))
                    {
                        // Ram impact: knockback player and distort membrane
                        player.Velocity += _chargeTargetDir * 350.0f;
                        player.TakeDamage(12.0f);
                        _currentState = State.Cooldown;
                        _stateTimer = 0.0f;
                        Velocity = -_chargeTargetDir * 50.0f;
                    }
                }

                if (_stateTimer >= 1.0f)
                {
                    _currentState = State.Cooldown;
                    _stateTimer = 0.0f;
                }
                break;

            case State.Cooldown:
                Velocity = Velocity.Lerp(Vector2.Zero, 4.0f * dt);
                if (_stateTimer >= 1.5f)
                {
                    _currentState = State.Drifting;
                    _stateTimer = 0.0f;
                }
                break;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0.0f, new Vector2(DrawScale, DrawScale));
        // 1. Telegraph line during windup
        if (_currentState == State.Windup)
        {
            float alpha = Mathf.Sin(_stateTimer * 25.0f) * 0.4f + 0.5f;
            DrawLine(Vector2.Zero, new Vector2(300, 0), new Color(1.0f, 0.2f, 0.2f, alpha), 2.5f);
        }

        // 2. Cinnamon rod body
        Color capsuleColor = new Color(0.72f, 0.45f, 0.25f, 0.95f);
        Color innerColor = new Color(0.90f, 0.62f, 0.38f, 0.90f);

        DrawRect(new Rect2(-13, -6, 26, 12), capsuleColor);
        DrawRect(new Rect2(-10, -4, 20, 8), innerColor);
        DrawCircle(new Vector2(13, 0), 6.0f, capsuleColor);
        DrawCircle(new Vector2(-13, 0), 6.0f, capsuleColor);

        // 3. Peritrichous flagella (radiating wavy hairs all around)
        Color hairCol = new Color(0.85f, 0.65f, 0.45f, 0.6f);
        int hairCount = 8;
        for (int i = 0; i < hairCount; i++)
        {
            float ang = i * (Mathf.Tau / hairCount);
            Vector2 root = new Vector2(Mathf.Cos(ang) * 14.0f, Mathf.Sin(ang) * 7.0f);
            float wave = Mathf.Sin(_flagellaPhase + i * 1.2f) * 4.0f;
            Vector2 tip = root + Vector2.FromAngle(ang) * 12.0f + new Vector2(-wave * 0.5f, wave);
            DrawLine(root, tip, hairCol, 1.2f);
        }
        DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
    }
}
