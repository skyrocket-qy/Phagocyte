using Godot;
using System;
using Game.Enemies;
using Game.Player;

namespace Game.Combat;

/// <summary>
/// Faction-agnostic stationary timed field: interval damage ticks plus generic
/// <see cref="EffectSpec"/> hits. <see cref="Team.Player"/> zones strike the
/// enemy registry with <see cref="Source"/> as the damage source (preserves
/// life-steal); <see cref="Team.Enemy"/> zones strike the player cell.
/// Mechanism extras ride as data: grow wavefront, enemy vortex pull, mine
/// detonation on expiry, tint. Replaces the HazardZone/ZoneNode faction split.
/// </summary>
public partial class Zone : Node2D
{
    public Team SourceTeam { get; set; } = Team.Enemy;
    public Node2D? Source { get; set; }
    public float Radius { get; set; } = 75.0f;
    public float Duration { get; set; } = 8.0f;
    public float TickInterval { get; set; } = 0.5f;
    public float Damage { get; set; } = 5.0f;
    public bool IsCrit { get; set; }
    /// <summary>Inline on-hit effects (value types: zero heap allocation per spawn).</summary>
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
        if (SourceTeam == Team.Enemy)
        {
            var player = EnemySteering.GetPlayer(this);
            if (player == null || !GodotObject.IsInstanceValid(player) || player.IsDead)
                return;
            if (GlobalPosition.DistanceTo(player.GlobalPosition) > CurrentRadius)
                return;
            if (Damage > 0.0f)
                DamageService.DealDamage(player, Damage, null, false);
            EffectSpec.ApplyAll(player, in _fx0, in _fx1, in _fx2, EffectCount);
            if (SlowFactor >= 0.0f)
                player.Status?.ApplySlow(slowDur, 1.0f - SlowFactor);
            return;
        }

        float radius = CurrentRadius;
        float damage = Damage;
        Node2D? source = Source;
        bool crit = IsCrit;
        float slowFactor = SlowFactor;
        TargetingService.ForEachInRadius(GlobalPosition, radius, enemy =>
        {
            DamageService.DealDamage(enemy, damage, source, crit);
            EffectSpec.ApplyAll(enemy, in _fx0, in _fx1, in _fx2, EffectCount);
            if (slowFactor >= 0.0f && enemy.Status != null)
                enemy.Status.ApplySlow(slowDur, 1.0f - slowFactor);
        });
    }

    // Struct-valued effect props cannot be passed by `in` directly, so the
    // tick snapshots them into backing fields first (still zero-alloc).
    private EffectSpec _fx0;
    private EffectSpec _fx1;
    private EffectSpec _fx2;

    public override void _Draw()
    {
        float alphaRatio = Mathf.Clamp(1.0f - (_lifeTimer / Mathf.Max(0.01f, Duration)), 0.0f, 1.0f);
        Color core = new Color(CoreColor.R, CoreColor.G, CoreColor.B, CoreColor.A * alphaRatio);
        Color rim = new Color(RimColor.R, RimColor.G, RimColor.B, RimColor.A * alphaRatio);

        float pulse = 1.0f + 0.04f * Mathf.Sin(_phase);
        float drawRadius = CurrentRadius;
        DrawCircle(Vector2.Zero, drawRadius * pulse, core);
        DrawArc(Vector2.Zero, drawRadius * pulse, 0, Mathf.Tau, 32, rim, 2.0f);

        for (int i = 0; i < 4; i++)
        {
            float angle = (i * (Mathf.Tau / 4.0f)) + _phase * 0.2f;
            float dist = drawRadius * 0.45f + 8.0f * Mathf.Sin(_phase + i);
            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            float bubbleRadius = 5.0f + 2.0f * Mathf.Sin(_phase * 1.5f + i);

            DrawCircle(pos, bubbleRadius, new Color(rim.R, rim.G, rim.B, 0.4f * alphaRatio));
            DrawArc(pos, bubbleRadius, 0, Mathf.Tau, 12, rim, 1.0f);
        }
    }
}
