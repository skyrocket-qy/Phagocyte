using Godot;
using System;
using Game.Enemies;
using Game.Player;

namespace Game.Combat;

/// <summary>Stationary timed area hazard dealing interval damage and on-hit effects.</summary>
public partial class Zone : Node2D
{
    public Team SourceTeam { get; set; } = Team.Enemy;
    public Node2D? Source { get; set; }
    public float Radius { get; set; } = 75.0f;
    public float Duration { get; set; } = 8.0f;
    public float TickInterval { get; set; } = 0.5f;
    public float Damage { get; set; } = 5.0f;
    public EffectSpec Effect0 { get; set; }
    public EffectSpec Effect1 { get; set; }
    public EffectSpec Effect2 { get; set; }
    public int EffectCount { get; set; }

    /// <summary>Growing wavefront: non-negative expands from this to <see cref="Radius"/>.</summary>
    public float GrowFrom { get; set; } = -1.0f;
    public float GrowTime { get; set; } = 1.5f;
    public float GrowRate { get; set; } = 0.0f;

    /// <summary>Enemy vortex pull (player-skill mechanic; enemy zones leave it 0).</summary>
    public float Pull { get; set; }
    public float PullEff { get; set; } = 0.4f;

    /// <summary>
    /// Channel slow as a 0-1 speed multiplier (applied via the slow-carrier
    /// def, no ailment id anywhere). Negative = no slow.
    /// </summary>
    public float SlowFactor { get; set; } = -1.0f;
    /// <summary>Slow duration; negative falls back to tick × 1.5.</summary>
    public float SlowDuration { get; set; } = -1.0f;

    /// <summary>Mine rows skip ticking and detonate via <see cref="Expired"/> on expiry.</summary>
    public bool NovaOnExpiry { get; set; }
    public float NovaRadius { get; set; }
    public Action<Vector2>? Expired { get; set; }

    public Color CoreColor { get; set; } = new Color(0.15f, 0.65f, 0.25f, 0.35f);
    public Color RimColor { get; set; } = new Color(0.35f, 0.95f, 0.45f, 0.70f);

    /// <summary>Effective radius right now (grows toward <see cref="Radius"/>).</summary>
    public float CurrentRadius
    {
        get
        {
            if (GrowFrom < 0.0f)
                return Radius;
            if (GrowRate > 0.0f)
                return Mathf.Min(Radius, GrowFrom + GrowRate * _lifeTimer);
            if (GrowTime <= 0.0f)
                return Radius;
            return Mathf.Lerp(GrowFrom, Radius, Mathf.Clamp(_lifeTimer / GrowTime, 0.0f, 1.0f));
        }
    }

    private float _lifeTimer;
    private float _tickTimer;
    private float _phase;
    private float _pullAccum;
    private float _redrawAccum;

    public override void _Ready()
    {
        ZIndex = 5;
        _phase = GD.Randf() * 10.0f;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _lifeTimer += dt;
        _phase += dt * 3.0f;

        if (_lifeTimer >= Duration)
        {
            if (NovaOnExpiry)
                Expired?.Invoke(GlobalPosition);
            QueueFree();
            return;
        }

        if (Pull > 0.0f && SourceTeam == Team.Player)
        {
            _pullAccum += dt;
            if (_pullAccum >= 1.0f / 30.0f)
            {
                float step = Pull * PullEff * _pullAccum;
                TargetingService.ForEachInRadius(GlobalPosition, CurrentRadius, enemy =>
                {
                    Vector2 toCenter = GlobalPosition - enemy.GlobalPosition;
                    if (toCenter.Length() > 10.0f)
                        enemy.Position += toCenter.Normalized() * step;
                });
                _pullAccum = 0.0f;
            }
        }

        _tickTimer -= dt;
        if (_tickTimer > 0.0f)
        {
            RedrawTick(dt);
            return;
        }
        _tickTimer = TickInterval;
        if (!NovaOnExpiry)
            TickTargets();
        RedrawTick(dt);
    }

    private void RedrawTick(float dt)
    {
        _redrawAccum += dt;
        if (_redrawAccum >= 1.0f / 30.0f)
        {
            _redrawAccum = 0.0f;
            QueueRedraw();
        }
    }

    private void TickTargets()
    {
        _fx0 = Effect0;
        _fx1 = Effect1;
        _fx2 = Effect2;
        float slowDur = SlowDuration < 0.0f ? TickInterval * 1.5f : SlowDuration;
        ulong attackerId = Source != null && GodotObject.IsInstanceValid(Source) ? Source.GetInstanceId() : 0;
        if (SourceTeam == Team.Enemy)
        {
            var player = EnemySteering.GetPlayer(this);
            if (player == null || !GodotObject.IsInstanceValid(player) || player.IsDead)
                return;
            if (GlobalPosition.DistanceTo(player.GlobalPosition) > CurrentRadius)
                return;
            if (Damage > 0.0f)
                HitPipeline.ResolveHit(new HitPayload
                {
                    RawDamage = Damage,
                    SourceFaction = Team.Enemy,
                    AttackerId = attackerId,
                    Effect0 = _fx0,
                    Effect1 = _fx1,
                    Effect2 = _fx2,
                    EffectCount = EffectCount,
                }, player);
            if (SlowFactor >= 0.0f)
                player.Status?.ApplySlow(slowDur, 1.0f - SlowFactor);
            return;
        }

        float radius = CurrentRadius;
        float damage = Damage;
        TargetingService.ForEachInRadius(GlobalPosition, radius, enemy =>
        {
            HitPipeline.ResolveHit(new HitPayload
            {
                RawDamage = damage,
                AttackerId = attackerId,
                Effect0 = _fx0,
                Effect1 = _fx1,
                Effect2 = _fx2,
                EffectCount = EffectCount,
            }, enemy);
            if (SlowFactor >= 0.0f && enemy.Status != null)
                enemy.Status.ApplySlow(slowDur, 1.0f - SlowFactor);
        });
    }

    // Struct-valued effect props cannot be passed by `in` directly, so the
    // tick snapshots them into backing fields first (still zero-alloc).
    private EffectSpec _fx0;
    private EffectSpec _fx1;
    private EffectSpec _fx2;

    public override void _Draw()
    {
        ZoneVisualPresenter.Draw(this, new ZoneVisualState(_lifeTimer, Duration, CurrentRadius, _phase, CoreColor, RimColor));
    }
}
