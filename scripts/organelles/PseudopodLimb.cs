using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Enemies;
using Phagocyte.Player;
using Phagocyte.UI;

namespace Phagocyte.Organelles;

/// <summary>
/// Modular Organelle: Pseudopod Limb (偽足捕捉爪).
/// A 2D IK chain solved with FABRIK. The chain idles coiled inside the cytoplasm,
/// snaps out at the nearest pathogen, grabs it, deals impact damage and drags it
/// back into the cell body for phagocytosis.
/// </summary>
public partial class PseudopodLimb : ModularOrganelle
{
    public enum LimbState
    {
        Retracted,
        Extending,
        Retracting,
        Cooldown
    }

    [Export] public int SegmentCount { get; set; } = 6;
    [Export] public float SegmentLength { get; set; } = 26.0f;
    [Export] public float SearchRange { get; set; } = 340.0f;
    [Export] public float ExtendSpeed { get; set; } = 1250.0f;
    [Export] public float PullSpeed { get; set; } = 620.0f;
    [Export] public float RetractSpeed { get; set; } = 900.0f;
    [Export] public float BaseCooldown { get; set; } = 2.4f;
    [Export] public float BaseDamage { get; set; } = 34.0f;
    [Export] public float BaseKnockback { get; set; } = 180.0f;
    [Export] public float BendAmount { get; set; } = 16.0f;
    [Export] public float GrabHoldTime { get; set; } = 0.5f;

    /// <summary>
    /// When false, the limb coils and sways ambiently but never
    /// auto-acquires targets (visual-only mode; explicit ForceLaunch still works).
    /// </summary>
    [Export] public bool CombatEnabled { get; set; } = true;

    public LimbState State { get; private set; } = LimbState.Retracted;
    public Node2D? GrabbedTarget { get; private set; }
    public float TipDistance { get; private set; }
    public float CooldownTimer { get; private set; }
    public bool HasLockedTarget => _target != null && GodotObject.IsInstanceValid(_target);

    private Line2D? _chain;
    private float _chainBaseWidth = 9.0f;

    private Vector2[] _joints = Array.Empty<Vector2>();
    private Node2D? _target;
    private Vector2 _grabDirection = Vector2.Right;
    private float _wanderPhase;
    private float _grabHoldTimer;

    public override void _Ready()
    {
        _chain = GetNodeOrNull<Line2D>("Chain");
        if (_chain != null)
        {
            _chainBaseWidth = _chain.Width;
        }
        RebuildJoints();
    }

    /// <summary>
    /// Immediately launches the limb at a specific target (used by AI and tests).
    /// </summary>
    public void ForceLaunch(Node2D target)
    {
        if (!IsAttached || target == null || !GodotObject.IsInstanceValid(target))
            return;

        _target = target;
        Vector2 offset = target.GlobalPosition - Host!.GlobalPosition;
        _grabDirection = offset.LengthSquared() > 1e-4f ? offset.Normalized() : Vector2.Right;
        TipDistance = Host.CurrentRadius * 0.3f;
        _grabHoldTimer = 0.0f;
        State = LimbState.Extending;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsAttached)
            return;

        float dt = (float)delta;
        float area = Mathf.Max(0.1f, GetStat("area"));
        float segmentLength = SegmentLength * area;

        if (_joints.Length != Mathf.Max(2, SegmentCount) + 1)
            RebuildJoints();

        UpdateState(dt, area);
        SolveChain(ComputeTipTarget(), segmentLength, area);
        UpdateVisuals(area);
    }

    private void RebuildJoints()
    {
        _joints = new Vector2[Mathf.Max(2, SegmentCount) + 1];
    }

    private void UpdateState(float dt, float area)
    {
        float hostRadius = Host!.CurrentRadius;
        float totalReach = Mathf.Max(2, SegmentCount) * SegmentLength * area;
        _wanderPhase = Mathf.Wrap(_wanderPhase, 0.0f, Mathf.Tau);

        switch (State)
        {
            case LimbState.Retracted:
                _wanderPhase += dt * 1.4f;
                if (!CombatEnabled)
                    break;
                _target = AcquireTarget(SearchRange * area);
                if (_target != null)
                {
                    Vector2 offset = _target.GlobalPosition - Host.GlobalPosition;
                    _grabDirection = offset.LengthSquared() > 1e-4f ? offset.Normalized() : Vector2.Right;
                    TipDistance = hostRadius * 0.3f;
                    State = LimbState.Extending;
                }
                break;

            case LimbState.Extending:
                _wanderPhase += dt * 0.4f;
                TipDistance = Mathf.Min(totalReach, TipDistance + ExtendSpeed * dt);

                if (HasLockedTarget)
                {
                    Vector2 tipGlobal = ToGlobal(_grabDirection * TipDistance);
                    if (tipGlobal.DistanceTo(_target!.GlobalPosition) <= GrabRadius(area))
                    {
                        GrabTarget();
                        return;
                    }
                }

                if (TipDistance >= totalReach - 0.01f)
                {
                    _target = null;
                    State = LimbState.Retracting;
                }
                break;

            case LimbState.Retracting:
                _wanderPhase += dt * 0.8f;
                TipDistance = Mathf.Max(hostRadius * 0.35f, TipDistance - RetractSpeed * dt);

                if (GrabbedTarget != null && GodotObject.IsInstanceValid(GrabbedTarget))
                {
                    DragTarget(dt);
                    if (_grabHoldTimer > 0.0f)
                    {
                        _grabHoldTimer -= dt;
                        break;
                    }

                    if (TipDistance <= hostRadius * 0.35f + 8.0f)
                        FinishDrag();
                }
                else if (TipDistance <= hostRadius * 0.35f + 0.5f)
                {
                    GrabbedTarget = null;
                    EnterCooldown();
                }
                break;

            case LimbState.Cooldown:
                CooldownTimer -= dt;
                if (CooldownTimer <= 0.0f)
                {
                    CooldownTimer = 0.0f;
                    State = LimbState.Retracted;
                }
                break;
        }
    }

    private void GrabTarget()
    {
        GrabbedTarget = _target;
        _grabHoldTimer = Mathf.Max(0.0f, GrabHoldTime);

        if (GrabbedTarget != null && GodotObject.IsInstanceValid(GrabbedTarget))
        {
            float damage = BaseDamage * GetStat("might");
            bool isCrit = Host!.Stats?.RollCritical() ?? false;
            if (isCrit)
                damage *= GetStat("crit_damage");

            if (GrabbedTarget is BaseEnemy enemy)
            {
                enemy.TakeDamage(damage, Host, isCrit);
                enemy.ApplyStun(Mathf.Max(_grabHoldTimer, 0.25f));
            }
            else
            {
                DamageNumberSpawner.ShowDamage(GrabbedTarget.GlobalPosition, damage, isCrit);
                CombatHelper.DamageOrEngulf(GrabbedTarget, damage, Host, isCrit);
            }
        }

        State = LimbState.Retracting;
    }

    private void DragTarget(float dt)
    {
        Vector2 tipGlobal = ToGlobal(_grabDirection * TipDistance);
        var target = GrabbedTarget!;

        if (target is BaseEnemy enemy)
        {
            enemy.GlobalPosition = enemy.GlobalPosition.MoveToward(tipGlobal, PullSpeed * dt);
            enemy.Velocity = (tipGlobal - enemy.GlobalPosition).LimitLength(PullSpeed);
        }
        else
        {
            target.GlobalPosition = target.GlobalPosition.MoveToward(tipGlobal, PullSpeed * dt);
        }
    }

    private void FinishDrag()
    {
        var target = GrabbedTarget;
        GrabbedTarget = null;
        EnterCooldown();

        if (target != null && GodotObject.IsInstanceValid(target) && target is IEngulfable engulfable)
        {
            engulfable.BeEngulfed(Host);
        }
        else if (target != null && GodotObject.IsInstanceValid(target) && target.HasMethod("be_engulfed"))
        {
            target.Call("be_engulfed", Host!);
        }
        else if (target != null && GodotObject.IsInstanceValid(target))
        {
            float knockback = BaseKnockback * GetStat("knockback");
            if (target is BaseEnemy enemy)
                enemy.Velocity += _grabDirection * knockback;
            else
                target.GlobalPosition += _grabDirection * 12.0f;
        }
    }

    private void EnterCooldown()
    {
        _target = null;
        State = LimbState.Cooldown;
        float cdr = Mathf.Clamp(GetStat("cooldown_reduction"), 0.0f, 0.9f);
        CooldownTimer = Mathf.Max(0.15f, BaseCooldown * (1.0f - cdr));
    }

    private float GrabRadius(float area)
    {
        return 26.0f * area;
    }

    private Node2D? AcquireTarget(float searchRange)
    {
        Node2D? best = null;
        float bestDistance = searchRange;

        foreach (var enemy in BaseEnemy.ActiveEnemies)
        {
            if (!TargetingService.IsAttackable(enemy))
                continue;

            float distance = Host!.GlobalPosition.DistanceTo(enemy.GlobalPosition);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                best = enemy;
            }
        }

        return best;
    }

    private Vector2 ComputeTipTarget()
    {
        float hostRadius = Host!.CurrentRadius;

        if (State == LimbState.Retracted || State == LimbState.Cooldown)
        {
            return Vector2.FromAngle(_wanderPhase) * (hostRadius * 0.35f);
        }

        return _grabDirection * TipDistance;
    }

    /// <summary>
    /// FABRIK inverse kinematics over a fixed-length chain, with a sinusoidal
    /// whip bend for an organic amoebic silhouette.
    /// </summary>
    private void SolveChain(Vector2 tipTarget, float segmentLength, float area)
    {
        int segments = _joints.Length - 1;
        float totalLength = segments * segmentLength;
        float reachFraction = Mathf.Clamp(tipTarget.Length() / Mathf.Max(1.0f, totalLength), 0.0f, 1.0f);

        if (tipTarget.Length() > totalLength)
            tipTarget = tipTarget.Normalized() * totalLength;

        _joints[0] = Vector2.Zero;
        _joints[segments] = tipTarget;

        for (int iteration = 0; iteration < 4; iteration++)
        {
            for (int i = segments - 1; i >= 0; i--)
            {
                Vector2 dir = _joints[i] - _joints[i + 1];
                dir = dir.LengthSquared() > 1e-6f ? dir.Normalized() : Vector2.Right;
                _joints[i] = _joints[i + 1] + dir * segmentLength;
            }

            for (int i = 1; i <= segments; i++)
            {
                Vector2 dir = _joints[i] - _joints[i - 1];
                dir = dir.LengthSquared() > 1e-6f ? dir.Normalized() : Vector2.Right;
                _joints[i] = _joints[i - 1] + dir * segmentLength;
            }

            _joints[0] = Vector2.Zero;
        }

        Vector2 axis = _joints[segments] - _joints[0];
        if (axis.LengthSquared() > 1e-4f)
        {
            Vector2 perpendicular = axis.Normalized().Orthogonal();
            for (int i = 1; i < segments; i++)
            {
                float t = (float)i / segments;
                _joints[i] += perpendicular * (Mathf.Sin(t * Mathf.Pi) * BendAmount * area * reachFraction);
            }
        }
    }

    private void UpdateVisuals(float area)
    {
        if (_chain != null)
        {
            _chain.Width = _chainBaseWidth * area;
            _chain.Points = _joints;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_joints.Length < 2)
            return;

        Vector2 tip = _joints[^1];
        Vector2 previous = _joints[^2];
        Vector2 direction = tip - previous;
        direction = direction.LengthSquared() > 1e-4f ? direction.Normalized() : _grabDirection;
        Vector2 perpendicular = direction.Orthogonal();

        float area = Mathf.Max(0.1f, GetStat("area"));
        float clawLength = 10.0f * area;
        float clawSpread = clawLength * 0.72f;

        Color membrane = new(0.80f, 0.92f, 1.0f, 0.85f);
        Color core = new(0.55f, 0.85f, 0.95f, 0.95f);

        DrawCircle(tip, 4.2f * area, core);
        DrawLine(tip, tip + direction * clawLength + perpendicular * clawSpread, membrane, 3.0f);
        DrawLine(tip, tip + direction * clawLength - perpendicular * clawSpread, membrane, 3.0f);
    }
}
