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
/// per-skill hit effects (mark, agglutination, burn). Radial uniform
/// patterns may route through the pooled ProjectileManager.
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
        public bool IsCrit;
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
        GetDamage(baseDmg, out float dmg, out bool crit);

        string pattern = ParamString(p, "pattern", "fan");
        string sfx = ParamString(p, "sfx");
        if (sfx != "")
            AudioManager.Instance?.PlaySfx(sfx);

        if (pattern == "radial")
        {
            FireRadial(p, count, speed, dmg, crit);
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
                QueueShot(staggerBase + i * staggerStep, dir, target, dmg, crit);
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
            QueueShot(staggerBase + i * staggerStep, dir, null, dmg, crit);
        }
    }

    private Vector2 AimDirection(Dictionary p)
    {
        if (ParamString(p, "pattern") == "fan" || ParamString(p, "pattern") == "random")
            return TargetingService.FindTargetDirection(Host!, ParamFloat(p, "range", 650.0f), Host!.Velocity.Length() > 20.0f ? Host.Velocity.Normalized() : Vector2.Right);
        return Host!.Velocity.Length() > 20.0f ? Host.Velocity.Normalized() : Vector2.Right;
    }

    private void QueueShot(float delay, Vector2 dir, EnemyActor? target, float dmg, bool crit)
    {
        if (delay <= 0.0f)
        {
            FireOne(dir, target, dmg, crit);
            return;
        }
        _pending.Add(new PendingShot { Delay = delay, Dir = dir, Target = target, Damage = dmg, IsCrit = crit });
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
            FireOne(shot.Dir, shot.Target, shot.Damage, shot.IsCrit);
        }
    }

    private void FireRadial(Dictionary p, int count, float speed, float dmg, bool crit)
    {
        if (ParamBool(p, "via_manager", false) && ProjectileManager.Instance != null)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Vector2.FromAngle(i * Mathf.Tau / count);
                ProjectileManager.Instance.Spawn(Host!.GlobalPosition, dir, speed, dmg, crit,
                    GetCalculatedPierce(ParamInt(p, "pierce", 2)),
                    ParamFloat(p, "lifetime", 1.6f), ParamFloat(p, "hit_radius", 20.0f), "defensin_barb");
            }
            return;
        }
        for (int i = 0; i < count; i++)
            FireOne(Vector2.FromAngle(i * Mathf.Tau / count), null, dmg, crit);
    }

    private void FireOne(Vector2 dir, EnemyActor? target, float dmg, bool crit)
    {
        if (!HasValidHost())
            return;
        var p = SkillParams();
        var parent = Host!.GetParent();
        if (parent == null)
            return;
        parent.AddChild(new SalvoProjectile
        {
            SkillRef = this,
            GlobalPosition = Host.GlobalPosition,
            Direction = dir,
            AssignedTarget = target,
            Speed = GetCalculatedSpeed(ParamFloat(p, "speed", 420.0f)),
            Damage = dmg,
            IsCrit = crit,
            Lifetime = GetCalculatedDuration(ParamFloat(p, "lifetime", 2.0f)),
            HitRadius = ParamFloat(p, "hit_radius", 20.0f),
            BouncesLeft = GetCalculatedPierce(ParamInt(p, "pierce", ParamInt(p, "bounces", 0)))
        });
    }

    private bool ParamBool(Dictionary p, string key, bool fallback)
    {
        return CatalogLoader.GetBool(p, key, fallback);
    }

    /// <summary>Single salvo projectile: linear / homing / chaining flight with hit effects.</summary>
    public partial class SalvoProjectile : Area2D
    {
        public SalvoSkill? SkillRef { get; set; }
        public Vector2 Direction { get; set; } = Vector2.Right;
        public EnemyActor? AssignedTarget { get; set; }
        public float Speed { get; set; } = 420.0f;
        public float Damage { get; set; }
        public bool IsCrit { get; set; }
        public float Lifetime { get; set; } = 2.0f;
        public float HitRadius { get; set; } = 20.0f;
        public int BouncesLeft { get; set; }

        private float _age;
        private float _phase;
        private float _stickTimer;
        private Vector2 _stickOffset = Vector2.Zero;
        private readonly HashSet<EnemyActor> _visited = new();

        public override void _Ready()
        {
            CollisionLayer = 0;
            CollisionMask = 2;
            AddChild(new CollisionShape2D
            {
                Name = "CollisionShape2D",
                Shape = new CircleShape2D { Radius = 8.0f }
            });
            BodyEntered += OnBodyEntered;
            ZIndex = 4;
        }

        public override void _PhysicsProcess(double delta)
        {
            float dt = (float)delta;
            _age += dt;
            if (_age >= Lifetime)
            {
                QueueFree();
                return;
            }
            var skill = SkillRef;
            var p = skill?.SkillParams() ?? new Dictionary();
            string steering = skill != null ? skill.ParamString(p, "steering", "linear") : "linear";

            if (_stickTimer > 0.0f)
            {
                _stickTimer -= dt;
                if (AssignedTarget != null && GodotObject.IsInstanceValid(AssignedTarget))
                    GlobalPosition = AssignedTarget.GlobalPosition + _stickOffset;
                if (_stickTimer <= 0.0f)
                    QueueFree();
                return;
            }

            if (steering == "homing" && AssignedTarget != null)
            {
                if (!GodotObject.IsInstanceValid(AssignedTarget))
                {
                    QueueFree();
                    return;
                }
                Vector2 desired = (AssignedTarget.GlobalPosition - GlobalPosition).Normalized();
                Direction = Direction.Lerp(desired, Mathf.Clamp(skill!.ParamFloat(p, "turn", 6.0f) * dt, 0.0f, 1.0f)).Normalized();
                _phase += dt * skill.ParamFloat(p, "wobble_freq", 14.0f);
                Vector2 perp = Direction.Orthogonal();
                Position += (Direction * Speed + perp * Mathf.Sin(_phase) * skill.ParamFloat(p, "wobble_amp", 0.25f) * Speed) * dt;
            }
            else
            {
                Position += Direction * Speed * dt;
            }
            Rotation = Direction.Angle();

            if (steering == "chain")
                ScanChain(p, skill);
            else
                ScanHit(p, skill);
        }

        private void ScanHit(Dictionary p, SalvoSkill? skill)
        {
            var target = AssignedTarget;
            if (target != null)
            {
                if (!GodotObject.IsInstanceValid(target))
                {
                    QueueFree();
                    return;
                }
                if (GlobalPosition.DistanceSquaredTo(target.GlobalPosition) <= HitRadius * HitRadius)
                    HitTarget(target, p, skill);
                return;
            }
            var found = TargetingService.FindNearest(this, HitRadius, e => e != null && !_visited.Contains(e));
            if (found == null)
                return;
            _visited.Add(found);
            HitTarget(found, p, skill);
        }

        private void ScanChain(Dictionary p, SalvoSkill? skill)
        {
            var found = TargetingService.FindNearest(this, HitRadius, e => e != null && !_visited.Contains(e));
            if (found == null)
                return;
            _visited.Add(found);
            HitTarget(found, p, skill);
            if (BouncesLeft <= 0)
                return;
            var next = TargetingService.FindNearest(found, skill?.ParamFloat(p, "reacquire", 350.0f) ?? 350.0f,
                e => e != null && e != found && !_visited.Contains(e));
            if (next != null)
                Direction = (next.GlobalPosition - GlobalPosition).Normalized();
            else
                Direction = Direction.Rotated((float)GD.RandRange(1.8f, 2.5f));
        }

        private void HitTarget(EnemyActor target, Dictionary p, SalvoSkill? skill)
        {
            DamageService.DealDamage(target, Damage, skill?.Host, IsCrit);
            if (skill != null && skill.ParamBool(p, "mark", false) && target.Ailments != null)
            {
                target.Ailments.ApplyMarkation();
                VfxManager.Instance?.Play(VfxType.MarkBind, target.GlobalPosition);
            }
            if (skill != null && p.ContainsKey("agglutinate_duration") && target.Ailments != null)
                target.Ailments.ApplyAgglutination(skill.ParamFloat(p, "agglutinate_duration", 1.5f), skill.ParamFloat(p, "agglutinate_slow", 0.35f));
            if (skill != null && p.ContainsKey("burn_mult") && target.Ailments != null)
                target.Ailments.ApplyOxidativeBurn(Damage * skill.ParamFloat(p, "burn_mult", 0.35f), skill.ParamFloat(p, "burn_duration", 2.0f));

            AssignedTarget = target;
            _visited.Add(target);
            float stick = skill?.ParamFloat(p, "stick_time", 0.0f) ?? 0.0f;
            if (stick > 0.0f)
            {
                _stickTimer = stick;
                _stickOffset = (GlobalPosition - target.GlobalPosition).Normalized() * skill!.ParamFloat(p, "stick_dist", 20.0f);
                SetDeferred("monitoring", false);
                return;
            }
            if (BouncesLeft > 0 && (skill?.ParamString(p, "steering", "linear") == "chain" || skill?.ParamString(p, "steering", "linear") == "linear"))
            {
                BouncesLeft--;
                if (BouncesLeft <= 0)
                {
                    QueueFree();
                    return;
                }
                return;
            }
            QueueFree();
        }

        private void OnBodyEntered(Node2D body)
        {
            if (body is EnemyActor enemy && GodotObject.IsInstanceValid(enemy))
            {
                var skill = SkillRef;
                var p = skill?.SkillParams() ?? new Dictionary();
                HitTarget(enemy, p, skill);
            }
            else
            {
                QueueFree();
            }
        }

        public override void _Draw()
        {
            var skill = SkillRef;
            Color accent = skill != null ? SkillAssetPalette.Accent(skill.SkillId, new Color(0.5f, 0.9f, 1.0f)) : new Color(0.5f, 0.9f, 1.0f);
            Color core = skill != null ? SkillAssetPalette.Core(skill.SkillId, Colors.White) : Colors.White;
            Vector2 fwd = Vector2.FromAngle(Rotation);
            DrawLine(-fwd * 12.0f, Vector2.Zero, new Color(accent, 0.35f), 5.0f);
            DrawLine(-fwd * 10.0f, fwd * 8.0f, accent, 3.0f);
            DrawLine(-fwd * 10.0f, fwd * 8.0f, core, 1.4f);
            DrawCircle(Vector2.Zero, 4.0f, core);
        }
    }
}
