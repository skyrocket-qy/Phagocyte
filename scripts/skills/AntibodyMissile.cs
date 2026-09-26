using Godot;
using Phagocyte.Combat;
using Phagocyte.Core;

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

        CombatHelper.DealDamage(node, Damage, HostRef, IsCrit);
        VfxManager.Instance?.Play(VfxType.OpsoninBind, GlobalPosition);

        if (node is Node n && n.GetNodeOrNull<AilmentController>("AilmentController") is AilmentController ac)
        {
            ac.ApplyAgglutination(1.5f, 0.35f);
        }

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
        Color accent = SkillAssetPalette.Accent(SkillIds.AntibodySalvo, new Color(0.15f, 0.69f, 0.75f));
        Color core = SkillAssetPalette.Core(SkillIds.AntibodySalvo, Colors.White);
        float armLen = 11.0f;
        float armSpread = 0.65f;

        Vector2 leftFab = new Vector2(armLen, -armLen * armSpread);
        Vector2 rightFab = new Vector2(armLen, armLen * armSpread);
        Vector2 stemEnd = new Vector2(-armLen * 1.2f, 0.0f);

        // 1. Layer 1: Ambient Fluorophore Diffusion Halo (wide, soft)
        Color haloColor = accent with { A = _stuck ? 0.35f : 0.25f };
        DrawLine(Vector2.Zero, leftFab, haloColor, 6.0f);
        DrawLine(Vector2.Zero, rightFab, haloColor, 6.0f);
        DrawLine(Vector2.Zero, stemEnd, haloColor, 6.5f);

        // 2. Layer 2: Fluorescent Emission Body (saturated)
        Color midColor = accent with { A = _stuck ? 1.0f : 0.85f };
        DrawLine(Vector2.Zero, leftFab, midColor, 3.0f);
        DrawLine(Vector2.Zero, rightFab, midColor, 3.0f);
        DrawLine(Vector2.Zero, stemEnd, midColor, 3.2f);

        // 3. Layer 3: High-Energy Excitation Core (sharp white-hot center)
        Color coreColor = core with { A = 0.95f };
        DrawLine(Vector2.Zero, leftFab, coreColor, 1.2f);
        DrawLine(Vector2.Zero, rightFab, coreColor, 1.2f);
        DrawLine(Vector2.Zero, stemEnd, coreColor, 1.3f);

        // 4. Fab Antigen-Binding Tips (Confocal 3-pass blooms)
        LaserGlow.DrawImpactHalo(this, leftFab, 3.8f, accent, core, 0.9f, 1.2f);
        LaserGlow.DrawImpactHalo(this, rightFab, 3.8f, accent, core, 0.9f, 1.2f);

        // 5. Opsonization anchor ring when bound to target
        if (_stuck)
        {
            LaserGlow.DrawImpactHalo(this, Vector2.Zero, 14.0f, accent, core, 0.85f, 2.0f);
            DrawArc(Vector2.Zero, 16.0f, 0.0f, Mathf.Tau, 24, accent with { A = 0.5f }, 1.5f);
        }
    }
}
