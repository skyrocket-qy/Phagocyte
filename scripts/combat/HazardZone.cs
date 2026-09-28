using Godot;
using System;
using Game.Core;
using Game.Player;

namespace Game.Combat;

/// <summary>
/// Generalized environmental biohazard zone (acidic exudates, biofilms, toxic puddles).
/// Features periodic ticking damage/slow, bio-gel procedural rendering, and smooth fadeout.
/// </summary>
public partial class HazardZone : Node2D
{
    [Export] public float Duration { get; set; } = 8.0f;
    [Export] public float Radius { get; set; } = 75.0f;
    [Export] public float Damage { get; set; } = 5.0f;
    [Export] public float TickInterval { get; set; } = 0.5f;
    [Export] public float SlowFactor { get; set; } = 0.5f;
    [Export] public bool SlowsTarget { get; set; } = true;
    [Export] public bool DealsDamage { get; set; } = true;

    [Export] public Color CoreColor { get; set; } = new Color(0.15f, 0.65f, 0.25f, 0.35f);
    [Export] public Color RimColor { get; set; } = new Color(0.35f, 0.95f, 0.45f, 0.70f);

    /// <summary>Growing wavefront: when non-negative, the hazard expands from
    /// this radius to <see cref="Radius"/>. Linear <see cref="GrowRate"/>
    /// (units/s) wins when positive, else <see cref="GrowTime"/> seconds.</summary>
    [Export] public float GrowFrom { get; set; } = -1.0f;
    [Export] public float GrowTime { get; set; } = 1.5f;
    [Export] public float GrowRate { get; set; } = 0.0f;

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

    private float _lifeTimer = 0.0f;
    private float _tickTimer = 0.0f;
    private float _bubblePhase = 0.0f;
    private float _redrawAccum = 0.0f;

    public override void _Ready()
    {
        AddToGroup("hazards");
        ZIndex = 5;
        _bubblePhase = GD.Randf() * 10.0f;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _lifeTimer += dt;
        _bubblePhase += dt * 3.0f;

        if (_lifeTimer >= Duration)
        {
            QueueFree();
            return;
        }

        _tickTimer -= dt;
        if (_tickTimer <= 0.0f)
        {
            _tickTimer = TickInterval;
            CheckHazardCollision();
        }

        _redrawAccum += dt;
        if (_redrawAccum >= 1.0f / 30.0f)
        {
            _redrawAccum = 0.0f;
            QueueRedraw();
        }
    }

    private void CheckHazardCollision()
    {
        var player = (PlayerActor?)GetTree().GetFirstNodeInGroup("player");
        if (player == null || !GodotObject.IsInstanceValid(player) || player.IsDead)
            return;

        if (GlobalPosition.DistanceTo(player.GlobalPosition) <= CurrentRadius)
        {
            if (DealsDamage && Damage > 0.0f)
            {
                player.TakeDamage(Damage);
            }

            if (SlowsTarget)
            {
                player.ApplySlow(TickInterval * 1.5f, SlowFactor);
            }
        }
    }

    public override void _Draw()
    {
        float alphaRatio = Mathf.Clamp(1.0f - (_lifeTimer / Duration), 0.0f, 1.0f);
        Color core = new Color(CoreColor.R, CoreColor.G, CoreColor.B, CoreColor.A * alphaRatio);
        Color rim = new Color(RimColor.R, RimColor.G, RimColor.B, RimColor.A * alphaRatio);

        // Pulsating biofilm gel
        float pulse = 1.0f + 0.04f * Mathf.Sin(_bubblePhase);
        float drawRadius = CurrentRadius;
        DrawCircle(Vector2.Zero, drawRadius * pulse, core);
        DrawArc(Vector2.Zero, drawRadius * pulse, 0, Mathf.Tau, 32, rim, 2.0f);

        // Bubbling micro-vesicles
        for (int i = 0; i < 4; i++)
        {
            float angle = (i * (Mathf.Tau / 4.0f)) + _bubblePhase * 0.2f;
            float dist = drawRadius * 0.45f + 8.0f * Mathf.Sin(_bubblePhase + i);
            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            float bubbleRadius = 5.0f + 2.0f * Mathf.Sin(_bubblePhase * 1.5f + i);

            DrawCircle(pos, bubbleRadius, new Color(rim.R, rim.G, rim.B, 0.4f * alphaRatio));
            DrawArc(pos, bubbleRadius, 0, Mathf.Tau, 12, rim, 1.0f);
        }
    }
}
