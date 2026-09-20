using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Combat;
using Phagocyte.Enemies;
using Phagocyte.Player;

namespace Phagocyte.Organelles;

/// <summary>
/// Modular Organelle: Receptor Spikes (受體棘刺陣列).
/// A ring of receptor stalks hovering just outside the cell membrane. The array
/// rotates continuously; pathogens sweeping across a stalk take contact damage
/// (反傷) and are shoved away by a rotational interception impulse (旋轉攔截).
/// </summary>
public partial class ReceptorSpikes : ModularOrganelle
{
    [Export] public int SpikeCount { get; set; } = 10;
    [Export] public float HoverOffset { get; set; } = 10.0f;
    [Export] public float SpikeLength { get; set; } = 15.0f;
    [Export] public float RotationSpeed { get; set; } = 1.6f;
    [Export] public float ContactDamage { get; set; } = 12.0f;
    [Export] public float HitInterval { get; set; } = 0.55f;
    [Export] public float InterceptKnockback { get; set; } = 220.0f;
    [Export] public float StunOnIntercept { get; set; } = 0.12f;

    public float Angle { get; private set; }
    public int InterceptedCount { get; private set; }

    private readonly Dictionary<ulong, float> _hitReadyAt = new();
    private float _time;

    public override void _PhysicsProcess(double delta)
    {
        if (!IsAttached)
            return;

        float dt = (float)delta;
        _time += dt;

        float area = Mathf.Max(0.1f, GetStat("area"));
        Angle = Mathf.Wrap(Angle + RotationSpeed * dt, 0.0f, Mathf.Tau);

        float ringRadius = CurrentRingRadius(area);
        float spikeLength = SpikeLength * area;
        float innerBand = ringRadius - 16.0f;
        float outerBand = ringRadius + spikeLength + 18.0f;

        float step = Mathf.Tau / Mathf.Max(4, SpikeCount);
        float halfHitAngle = step * 0.42f;

        foreach (var enemy in BaseEnemy.ActiveEnemies)
        {
            if (!TargetingService.IsAttackable(enemy))
                continue;

            Vector2 offset = enemy.GlobalPosition - Host!.GlobalPosition;
            float distance = offset.Length();
            if (distance < innerBand || distance > outerBand)
                continue;

            float enemyAngle = Mathf.Wrap(offset.Angle(), 0.0f, Mathf.Tau);
            float angularDistance = Mathf.Abs(Mathf.Wrap(enemyAngle - Angle, -Mathf.Pi, Mathf.Pi));
            float offsetWithinStep = angularDistance - step * Mathf.Floor(angularDistance / step);
            float toNearestSpike = Mathf.Min(offsetWithinStep, step - offsetWithinStep);
            if (toNearestSpike > halfHitAngle)
                continue;

            ulong id = enemy.GetInstanceId();
            if (_hitReadyAt.TryGetValue(id, out float readyAt) && _time < readyAt)
                continue;

            _hitReadyAt[id] = _time + HitInterval;
            Intercept(enemy, offset / Mathf.Max(0.0001f, distance));
        }

        PruneCooldowns();
        QueueRedraw();
    }

    /// <summary>
    /// Radius of the receptor ring measured from the host center.
    /// </summary>
    public float CurrentRingRadius(float area)
    {
        return (Host?.CurrentRadius ?? 24.0f) + HoverOffset * area;
    }

    private void Intercept(Node2D enemy, Vector2 outward)
    {
        float damage = ContactDamage * GetStat("might");
        bool isCrit = Host!.Stats?.RollCritical() ?? false;
        if (isCrit)
            damage *= GetStat("crit_damage");

        if (enemy is BaseEnemy baseEnemy)
        {
            baseEnemy.TakeDamage(damage, Host, isCrit);
        }
        else if (enemy.HasMethod("take_damage"))
        {
            enemy.Call("take_damage", damage, Host, isCrit);
        }
        else
        {
            enemy.Call("be_engulfed", Host);
        }

        float knockback = InterceptKnockback * Mathf.Max(0.2f, GetStat("knockback"));
        float spinSign = Mathf.Sign(RotationSpeed == 0.0f ? 1.0f : RotationSpeed);
        Vector2 tangent = new Vector2(-outward.Y, outward.X) * spinSign;
        Vector2 impulse = (outward * 0.7f + tangent * 0.3f).Normalized() * knockback;

        if (enemy is BaseEnemy pushed)
        {
            pushed.Velocity += impulse;
            if (StunOnIntercept > 0.0f)
                pushed.ApplyStun(StunOnIntercept);
        }
        else
        {
            enemy.GlobalPosition += outward * 6.0f;
        }

        InterceptedCount++;
    }

    private void PruneCooldowns()
    {
        if (_hitReadyAt.Count < 128)
            return;

        var stale = new List<ulong>();
        foreach (var pair in _hitReadyAt)
        {
            if (_time >= pair.Value)
                stale.Add(pair.Key);
        }

        foreach (ulong id in stale)
            _hitReadyAt.Remove(id);
    }

    public override void _Draw()
    {
        if (!IsAttached)
            return;

        float area = Mathf.Max(0.1f, GetStat("area"));
        float ringRadius = CurrentRingRadius(area);
        float spikeLength = SpikeLength * area;
        int count = Mathf.Max(4, SpikeCount);
        float step = Mathf.Tau / count;

        Color stalk = new(0.55f, 0.85f, 1.0f, 0.55f);
        Color head = new(0.85f, 0.97f, 1.0f, 0.92f);

        for (int i = 0; i < count; i++)
        {
            float angle = Angle + i * step;
            Vector2 direction = Vector2.FromAngle(angle);
            Vector2 perpendicular = direction.Orthogonal();
            Vector2 inner = direction * ringRadius;
            Vector2 outer = direction * (ringRadius + spikeLength);

            DrawLine(inner, outer, stalk, 2.6f);

            DrawLine(outer, outer + direction * 3.0f + perpendicular * 4.0f, head, 2.0f);
            DrawLine(outer, outer + direction * 3.0f - perpendicular * 4.0f, head, 2.0f);
            DrawCircle(outer, 2.4f, head);
        }
    }
}
