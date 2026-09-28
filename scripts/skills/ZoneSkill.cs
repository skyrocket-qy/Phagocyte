using Godot;
using Godot.Collections;
using Game.Combat;
using Game.Core;
using Game.Enemies;

namespace Game.Skills;

/// <summary>
/// Timed-area zone archetype (data: assets/data/skill/active.json).
/// Deploys a stationary field at self / nearest target / forward offset
/// that ticks damage, applies ailments and optionally pulls. Mine rows
/// detonate a nova sphere on expiry instead of ticking.
/// </summary>
public partial class ZoneSkill : BaseSkill
{
    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;
        var p = SkillParams();
        string sfx = ParamString(p, "sfx");
        if (sfx != "")
            AudioManager.Instance?.PlaySfx(sfx);

        Vector2 center = DeployPoint(p);
        float baseDmg = GetBaseDamageForLevel(ParamFloat(p, "base_damage", 14.0f));
        GetDamage(baseDmg, out float dmg, out bool crit);

        int count = GetCalculatedAmount(ParamInt(p, "count", 1));
        for (int i = 0; i < count; i++)
        {
            Vector2 at = center;
            if (ParamString(p, "deploy", "target") == "scatter")
                at = center + Vector2.FromAngle((float)GD.RandRange(0.0, Mathf.Tau))
                    * (float)GD.RandRange(ParamFloat(p, "drop_min", 50.0f), ParamFloat(p, "drop_max", 220.0f));
            var parent = Host!.GetParent();
            if (parent == null)
                return;
            parent.AddChild(new ZoneNode
            {
                SkillRef = this,
                GlobalPosition = at,
                Radius = GetCalculatedArea(ParamFloat(p, "radius", 65.0f)),
                Duration = GetCalculatedDuration(ParamFloat(p, "duration", ParamFloat(p, "fuse", 4.0f))),
                TickInterval = ParamFloat(p, "tick", 0.35f),
                Damage = dmg,
                IsCrit = crit
            });
        }
    }

    private Vector2 DeployPoint(Dictionary p)
    {
        string deploy = ParamString(p, "deploy", "target");
        if (deploy == "self")
            return Host!.GlobalPosition;
        var target = TargetingService.FindNearest(Host!, ParamFloat(p, "deploy_range", 450.0f));
        if (target != null)
            return target.GlobalPosition;
        Vector2 fwd = Host!.Velocity.Length() > 20.0f ? Host.Velocity.Normalized() : Vector2.Right;
        return Host.GlobalPosition + fwd * ParamFloat(p, "deploy_fallback", 100.0f);
    }

    /// <summary>Deploys the zone nova on expiry (mine rows).</summary>
    public void ExpireNova(Vector2 center, Dictionary p, float dmg, bool crit)
    {
        var nova = new NovaSkill
        {
            SkillId = SkillId,
            Level = Level,
            MaxLevel = MaxLevel,
            Cooldown = 0.0f
        };
        nova.Setup(Host!);
        string novaSfx = ParamString(p, "nova_sfx");
        if (novaSfx != "")
            AudioManager.Instance?.PlaySfx(novaSfx);
        float radius = GetCalculatedArea(ParamFloat(p, "nova_radius", 80.0f));
        TargetingService.ForEachInRadius(center, radius, enemy =>
        {
            DamageService.DealDamage(enemy, dmg, Host, crit);
        });
        var parent = Host!.GetParent();
        if (parent != null)
            parent.AddChild(new NovaSkill.NovaVisual
            {
                SkillRef = nova,
                GlobalPosition = center,
                Radius = radius,
                ConeHalfAngle = -1.0f,
                Aim = Vector2.Right
            });
    }

    /// <summary>Stationary timed field: damage ticks, ailments, pull, mine expiry.</summary>
    public partial class ZoneNode : Node2D
    {
        public ZoneSkill? SkillRef { get; set; }
        public float Radius { get; set; } = 65.0f;
        public float Duration { get; set; } = 4.0f;
        public float TickInterval { get; set; } = 0.35f;
        public float Damage { get; set; }
        public bool IsCrit { get; set; }

        private float _age;
        private float _tickTimer;
        private float _phase;
        private float _pullAccum;

        public override void _PhysicsProcess(double delta)
        {
            float dt = (float)delta;
            _age += dt;
            _phase += dt;
            if (_age >= Duration)
            {
                Expire();
                return;
            }

            var skill = SkillRef;
            var p = skill?.SkillParams() ?? new Dictionary();

            float pull = skill?.ParamFloat(p, "pull", 0.0f) ?? 0.0f;
            if (pull > 0.0f)
            {
                _pullAccum += dt;
                if (_pullAccum >= 1.0f / 30.0f)
                {
                    float step = pull * (skill?.ParamFloat(p, "pull_eff", 0.4f) ?? 0.4f) * _pullAccum;
                    TargetingService.ForEachInRadius(GlobalPosition, Radius, enemy =>
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
                return;
            _tickTimer = TickInterval;
            if (skill == null || !skill.HasValidHost())
                return;
            if (p.ContainsKey("nova_radius"))
                return; // mine rows only detonate on expiry
            TargetingService.ForEachInRadius(GlobalPosition, Radius, enemy =>
            {
                DamageService.DealDamage(enemy, Damage, skill.Host, IsCrit);
                if (enemy.Ailments == null)
                    return;
                if (p.ContainsKey("burn_mult"))
                    enemy.Ailments.ApplyOxidativeBurn(Damage * skill.ParamFloat(p, "burn_mult", 0.4f), skill.ParamFloat(p, "burn_duration", 1.5f));
                if (p.ContainsKey("leak_mult"))
                    enemy.Ailments.ApplyMembraneLeak(Damage * skill.ParamFloat(p, "leak_mult", 0.3f), skill.ParamFloat(p, "leak_duration", 2.0f));
            });
        }

        private void Expire()
        {
            var skill = SkillRef;
            var p = skill?.SkillParams() ?? new Dictionary();
            if (skill != null && p.ContainsKey("nova_radius"))
                skill.ExpireNova(GlobalPosition, p, Damage, IsCrit);
            QueueFree();
        }

        public override void _Process(double delta)
        {
            QueueRedraw();
        }

        public override void _Draw()
        {
            var skill = SkillRef;
            Color accent = skill != null ? SkillAssetPalette.Accent(skill.SkillId, new Color(0.4f, 0.9f, 0.7f)) : new Color(0.4f, 0.9f, 0.7f);
            Color core = skill != null ? SkillAssetPalette.Core(skill.SkillId, Colors.White) : Colors.White;
            float alpha = Mathf.Clamp(1.0f - _age / Mathf.Max(0.01f, Duration), 0.0f, 1.0f);
            float breathe = 1.0f + 0.04f * Mathf.Sin(_phase * 4.0f);
            DrawCircle(Vector2.Zero, Radius * breathe, new Color(accent, 0.12f * alpha));
            DrawArc(Vector2.Zero, Radius * breathe, 0.0f, Mathf.Tau, 40, new Color(accent, 0.6f * alpha), 2.5f);
            DrawCircle(Vector2.Zero, Radius * 0.45f * breathe, new Color(core, 0.20f * alpha));
        }
    }
}
