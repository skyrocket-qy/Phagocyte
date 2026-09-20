using Godot;
using Phagocyte.Combat;

namespace Phagocyte.Skills;

/// <summary>
/// Y-shaped IgG missile: Brownian wobble in flight, Fab-first homing,
/// Fc-outward opsonization stick on the target membrane.
/// </summary>
public partial class AntibodyMissile : Area2D
{
    public float Damage { get; set; } = 18.0f;
    public bool IsCrit { get; set; } = false;
    public float Speed { get; set; } = 420.0f;
    public float Lifetime { get; set; } = 2.0f;
    public Node2D? Target { get; set; }
    public CharacterBody2D? HostRef { get; set; }

    private const float StickDuration = 0.4f;
    private const float TurnRate = 6.0f;

    private Vector2 _velocity = Vector2.Right;
    private float _wobblePhase = 0.0f;
    private float _age = 0.0f;
    private bool _stuck = false;
    private float _stickTimer = 0.0f;
    private float _stickAngle = 0.0f;

    public void Launch(Node2D? target, Vector2 origin, Vector2 initialDir)
    {
        Target = target;
        GlobalPosition = origin;
        _velocity = initialDir.Normalized() * Speed;
        Rotation = _velocity.Angle();
        _wobblePhase = GD.Randf() * Mathf.Tau;
    }

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 2; // Pathogens
        ZIndex = 6;
        AreaEntered += OnAreaEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (_stuck)
        {
            _stickTimer -= dt;
            if (_stickTimer <= 0.0f)
            {
                QueueFree();
                return;
            }
            // Ride the target membrane while opsonized.
            if (Target != null && GodotObject.IsInstanceValid(Target))
                GlobalPosition = Target.GlobalPosition + Vector2.FromAngle(_stickAngle) * 20.0f;
            else
                QueueFree();
            return;
        }

        _age += dt;
        if (_age >= Lifetime)
        {
            QueueFree();
            return;
        }

        // Homing with limited turn rate + Brownian jitter.
        if (Target != null && GodotObject.IsInstanceValid(Target))
        {
            Vector2 desired = (Target.GlobalPosition - GlobalPosition).Normalized() * Speed;
            _velocity = _velocity.Lerp(desired, Mathf.Clamp(TurnRate * dt, 0.0f, 1.0f));
        }

        _wobblePhase += dt * 14.0f;
        Vector2 perp = _velocity.Normalized().Orthogonal();
        Vector2 step = _velocity + perp * Mathf.Sin(_wobblePhase) * Speed * 0.25f;
        GlobalPosition += step * dt;
        Rotation = _velocity.Angle();
        QueueRedraw();
    }

    private void OnAreaEntered(Area2D area)
    {
        if (_stuck)
            return;

        var node = area.GetParent();
        // Faithful to the salvo assignment: only the designated target binds.
        if (Target == null || !GodotObject.IsInstanceValid(Target) || node != Target)
            return;

        if (node is Enemies.BaseEnemy be && be.IsBeingEaten)
            return;

        CombatHelper.DealDamage(node, Damage, HostRef, IsCrit);
        VfxManager.Instance?.Play(VfxType.OpsoninBind, GlobalPosition);

        // Opsonization stick: Fab tips in, Fc stem outward.
        _stuck = true;
        _stickTimer = StickDuration;
        Vector2 outDir = (GlobalPosition - Target.GlobalPosition);
        _stickAngle = outDir.LengthSquared() > 1.0f ? outDir.Angle() : Rotation;
        Rotation = _stickAngle + Mathf.Pi * 0.5f;
        SetDeferred(Area2D.PropertyName.Monitoring, false);
        QueueRedraw();
    }

    public override void _Draw()
    {
        // IgG silhouette: two Fab arms + Fc stem, gold-white.
        Color armColor = new Color(1.0f, 0.88f, 0.55f, _stuck ? 1.0f : 0.9f);
        Color stemColor = new Color(1.0f, 0.95f, 0.8f, 0.95f);
        float armLen = 9.0f;
        float armSpread = 0.6f;

        // Arms open toward +X (flight direction / membrane).
        DrawLine(Vector2.Zero, new Vector2(armLen, -armLen * armSpread), armColor, 2.5f);
        DrawLine(Vector2.Zero, new Vector2(armLen, armLen * armSpread), armColor, 2.5f);
        DrawCircle(new Vector2(armLen, -armLen * armSpread), 2.2f, armColor);
        DrawCircle(new Vector2(armLen, armLen * armSpread), 2.2f, armColor);
        // Fc stem trails behind.
        DrawLine(Vector2.Zero, new Vector2(-armLen * 1.1f, 0.0f), stemColor, 3.0f);

        if (_stuck)
            DrawArc(Vector2.Zero, 12.0f, 0.0f, Mathf.Tau, 20, new Color(1.0f, 0.85f, 0.4f, 0.6f), 1.5f);
    }
}
