using Godot;
using Godot.Collections;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;

namespace Game.Skills;

/// <summary>
/// Projectile-salvo archetype (data: assets/data/skill/active.json).
/// Covers homing volleys, radial bursts, chaining shots and fan sprays:
/// count projectiles fly with linear / homing / chain steering and apply
/// generic <see cref="EffectSpec"/> hits (mark, chill, burn). All
/// shots route through the pooled <see cref="ProjectileManager"/> (batched
/// structs, zero per-shot nodes); steering and effects ride as spawn data.
/// </summary>
public partial class SalvoSkill : BaseSkill
{
    private readonly List<PendingShot> _pending = new();
    private readonly List<EnemyActor> _scratch = new();

    private sealed class PendingShot
    {
        public float Delay;
        public Vector2 Dir;
        public EnemyActor? Target;
        public float Damage;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;
        var p = SkillParams();
        int count = GetCalculatedAmount(ParamInt(p, "count", 1));
        float speed = GetCalculatedSpeed(ParamFloat(p, "speed", 420.0f));
        float baseDmg = GetBaseDamageForLevel(ParamFloat(p, "base_damage", 18.0f));
        float dmg = GetCalculatedDamage(baseDmg);

        string pattern = ParamString(p, "pattern", "fan");
        string sfx = ParamString(p, "sfx");
        if (sfx != "")
            AudioManager.Instance?.PlaySfx(sfx);

        if (pattern == "radial")
        {
            FireRadial(p, count, speed, dmg);
            return;
        }

        Vector2 baseDir = AimDirection(p);
        float spread = ParamFloat(p, "spread", 0.0f);
        float staggerBase = ParamFloat(p, "stagger_base", 0.0f);
        float staggerStep = ParamFloat(p, "stagger_step", 0.0f);

        if (pattern == "round_robin")
        {
            _scratch.Clear();
            TargetingService.CollectInRadius(Host.GlobalPosition, ParamFloat(p, "range", 600.0f), _scratch);
            for (int i = 0; i < count; i++)
            {
                EnemyActor? target = _scratch.Count > 0 ? _scratch[i % _scratch.Count] : null;
                Vector2 dir = target != null
                    ? (target.GlobalPosition - Host.GlobalPosition).Normalized()
                    : baseDir.Rotated((i - (count - 1) * 0.5f) * spread);
                if (dir == Vector2.Zero)
                    dir = Vector2.Right;
                QueueShot(staggerBase + i * staggerStep, dir, target, dmg);
            }
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 dir = pattern == "fan" && count > 1
                ? baseDir.Rotated((i - (count - 1) * 0.5f) * spread)
                : baseDir.Rotated((float)GD.RandRange(-spread, spread));
            if (dir == Vector2.Zero)
                dir = Vector2.Right;
            QueueShot(staggerBase + i * staggerStep, dir, null, dmg);
        }
    }

    private Vector2 AimDirection(Dictionary p)
    {
        if (ParamString(p, "pattern") == "fan" || ParamString(p, "pattern") == "random")
            return TargetingService.FindTargetDirection(Host!, ParamFloat(p, "range", 650.0f), Host!.Velocity.Length() > 20.0f ? Host.Velocity.Normalized() : Vector2.Right);
        return Host!.Velocity.Length() > 20.0f ? Host.Velocity.Normalized() : Vector2.Right;
    }

    private void QueueShot(float delay, Vector2 dir, EnemyActor? target, float dmg)
    {
        if (delay <= 0.0f)
        {
            FireOne(dir, target, dmg);
            return;
        }
        _pending.Add(new PendingShot { Delay = delay, Dir = dir, Target = target, Damage = dmg });
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_pending.Count == 0 || !HasValidHost())
            return;
        float dt = (float)delta;
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            var shot = _pending[i];
            shot.Delay -= dt;
            if (shot.Delay > 0.0f)
                continue;
            _pending.RemoveAt(i);
            if (shot.Target != null && !GodotObject.IsInstanceValid(shot.Target))
                shot.Target = null;
            FireOne(shot.Dir, shot.Target, shot.Damage);
        }
    }

    private void FireRadial(Dictionary p, int count, float speed, float dmg)
    {
        var mgr = ProjectileManager.Instance;
        if (mgr == null || !HasValidHost())
        {
            GD.PushWarning("[SalvoSkill] No ProjectileManager: radial salvo dropped.");
            return;
        }
        float critChance = GetCalculatedCritChance();
        float critMult = GetCalculatedCritDamage();
        float pen = GetCalculatedArmorPenetration();
        float ailChance = GetCalculatedAilmentChance();
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Vector2.FromAngle(i * Mathf.Tau / count);
            mgr.Spawn(Host!.GlobalPosition, dir, speed,
                new HitPayload
                {
                    RawDamage = dmg,
                    CritChance = critChance,
                    CritMultiplier = critMult,
                    ArmorPenetration = pen,
                    AilmentChance = ailChance,
                    AttackerId = Host.GetInstanceId(),
                },
                GetCalculatedPierce(ParamInt(p, "pierce", 2)),
                ParamFloat(p, "lifetime", 1.6f), ParamFloat(p, "hit_radius", 20.0f), "defensin_barb");
        }
    }

    private void FireOne(Vector2 dir, EnemyActor? target, float dmg)
    {
        if (!HasValidHost())
            return;
        var mgr = ProjectileManager.Instance;
        if (mgr == null)
        {
            GD.PushWarning("[SalvoSkill] No ProjectileManager: salvo shot dropped.");
            return;
        }
        var p = SkillParams();
        ulong lockId = target != null && GodotObject.IsInstanceValid(target) ? target.GetInstanceId() : 0;
        int steering = ParamString(p, "steering", "linear") switch
        {
            "homing" => ProjectileData.SteeringHoming,
            "chain" => ProjectileData.SteeringChain,
            _ => ProjectileData.SteeringLinear,
        };
        var fx0 = default(EffectSpec);
        var fx1 = default(EffectSpec);
        var fx2 = default(EffectSpec);
        int fxCount = BuildOnHitEffects(p, dmg, out fx0, out fx1, out fx2);
        float singleCrit = GetCalculatedCritChance();
        float singleMult = GetCalculatedCritDamage();
        float singlePen = GetCalculatedArmorPenetration();
        float singleAil = GetCalculatedAilmentChance();
        mgr.Spawn(
            Host!.GlobalPosition, dir,
            GetCalculatedSpeed(ParamFloat(p, "speed", 420.0f)),
            new HitPayload
            {
                RawDamage = dmg,
                CritChance = singleCrit,
                CritMultiplier = singleMult,
                ArmorPenetration = singlePen,
                AilmentChance = singleAil,
                AttackerId = Host.GetInstanceId(),
                Effect0 = fx0,
                Effect1 = fx1,
                Effect2 = fx2,
                EffectCount = fxCount,
            },
            GetCalculatedPierce(ParamInt(p, "pierce", 0)),
            GetCalculatedDuration(ParamFloat(p, "lifetime", 2.0f)),
            ParamFloat(p, "hit_radius", 20.0f), "generic", steering,
            ParamFloat(p, "turn", 6.0f), ParamFloat(p, "wobble_freq", 0.0f), ParamFloat(p, "wobble_amp", 0.0f),
            ParamFloat(p, "reacquire", 350.0f), lockId);
    }

}
