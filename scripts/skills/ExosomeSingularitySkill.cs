using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Exosome Singularity (外泌體引力阱)
/// High-density exosome vesicle vortex sucking in nearby mobs for 3s to cluster them.
/// </summary>
public partial class ExosomeSingularitySkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 12.0f;
    [Export] public float BaseDuration { get; set; } = 3.0f;
    [Export] public float BaseRadius { get; set; } = 175.0f;
    [Export] public float BasePullForce { get; set; } = 220.0f;

    public ExosomeSingularitySkill()
    {
        SkillId = SkillIds.ExosomeSingularity;
        NameKey = "SKILL_EXOSOME_NAME";
        DescKey = "SKILL_EXOSOME_DESC";
        BioKey = "SKILL_EXOSOME_BIO";
        IconSymbol = "🧲";
        IsInnate = false;
        IsPassive = false;
        Cooldown = 5.5f;
        CooldownTimer = 2.0f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        Vector2 targetPos = FindClusteredTargetPosition();
        float dur = GetCalculatedDuration(BaseDuration);
        float rad = GetCalculatedArea(BaseRadius);
        float baseDmg = GetBaseDamageForLevel(BaseDamage);
        GetDamage(baseDmg, out float dmg, out _);

        var singularity = new SingularityVortex
        {
            GlobalPosition = targetPos,
            Duration = dur,
            Radius = rad,
            PullForce = BasePullForce,
            DamagePerTick = dmg,
            HostRef = Host
        };
        Host.GetParent().AddChild(singularity);
        AudioManager.Instance?.PlaySfx("vortex");
    }

    private Vector2 FindClusteredTargetPosition()
    {
        if (Host == null)
            return Vector2.Zero;

        var closest = TargetingService.FindNearest(Host, 450.0f);
        if (closest != null)
            return closest.GlobalPosition;

        return Host.GlobalPosition + (Host.Velocity.Length() > 20.0f ? Host.Velocity.Normalized() * 140.0f : Vector2.Right * 140.0f);
    }

    public partial class SingularityVortex : Node2D
    {
        public float Duration { get; set; } = 3.0f;
        public float Radius { get; set; } = 175.0f;
        public float PullForce { get; set; } = 220.0f;
        public float DamagePerTick { get; set; } = 12.0f;
        public CharacterBody2D? HostRef { get; set; }

        private float _age = 0.0f;
        private float _spinAngle = 0.0f;
        private float _tickTimer = 0.0f;
        private float _pullAccum = 0.0f;
        private bool _pendingTick;

        public override void _Process(double delta)
        {
            float dt = (float)delta;
            _age += dt;
            if (_age >= Duration)
            {
                QueueFree();
                return;
            }

            _spinAngle += dt * 6.0f;
            _tickTimer += dt;
            if (_tickTimer >= 0.4f)
            {
                _tickTimer = 0.0f;
                _pendingTick = true;
            }

            // Full-arena enemy scan at half rate (damage ticks ride along,
            // delayed by at most one 30 Hz step); pull displacement integrates
            // the scaled step so total suction is unchanged.
            _pullAccum += dt;
            if (_pullAccum >= 1.0f / 30.0f)
            {
                float step = _pullAccum;
                _pullAccum = 0.0f;
                PullEnemies(step, _pendingTick);
                _pendingTick = false;
            }
            QueueRedraw();
        }

        private void PullEnemies(float dt, bool doTick)
        {
            if (HostRef == null)
                return;

            TargetingService.ForEachInRadius(GlobalPosition, Radius, n =>
            {
                float dist = GlobalPosition.DistanceTo(n.GlobalPosition);
                if (dist <= 10.0f)
                    return;

                Vector2 pullDir = (GlobalPosition - n.GlobalPosition).Normalized();
                n.GlobalPosition += pullDir * PullForce * 0.4f * dt;

                if (doTick)
                {
                    CombatHelper.DealDamage(n, DamagePerTick, HostRef, false);
                }
            });
        }

        public override void _Draw()
        {
            float alpha = Mathf.Clamp(1.0f - (_age / Duration), 0.0f, 1.0f);
            Color accent = SkillAssetPalette.Accent(SkillIds.ExosomeSingularity, new Color(0.13f, 0.68f, 0.83f));
            Color core = SkillAssetPalette.Core(SkillIds.ExosomeSingularity, new Color(0.75f, 0.96f, 1.0f));

            // Deep central vesicular void
            DrawCircle(Vector2.Zero, 22.0f, new Color(0.02f, 0.08f, 0.12f, alpha * 0.95f));
            LaserGlow.DrawImpactHalo(this, Vector2.Zero, 22.0f, accent, core, alpha * 0.9f, 2.5f);

            // Outer gravitational suction limit
            DrawArc(Vector2.Zero, Radius, 0.0f, Mathf.Tau, 48, new Color(accent.R, accent.G, accent.B, alpha * 0.35f), 1.5f);

            // 4 swirling lipid vesicular arms matching icon art
            for (int i = 0; i < 4; i++)
            {
                float baseA = _spinAngle + i * (Mathf.Tau / 4.0f);
                for (int s = 1; s <= 10; s++)
                {
                    float t = (float)s / 10.0f;
                    float armR = 24.0f + (Radius - 24.0f) * t;
                    float armA = baseA + t * 2.8f;
                    Vector2 p = Vector2.FromAngle(armA) * armR;

                    // Swirling exosome vesicle sphere
                    float vesicleSize = 3.5f * (1.0f - t * 0.4f);
                    DrawCircle(p, vesicleSize, new Color(core.R, core.G, core.B, alpha * 0.85f));
                    DrawArc(p, vesicleSize + 1.2f, 0.0f, Mathf.Tau, 12, new Color(accent.R, accent.G, accent.B, alpha * 0.6f), 1.0f);
                }
            }
        }
    }
}
