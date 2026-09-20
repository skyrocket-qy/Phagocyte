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
        GetDamage(BaseDamage, out float dmg, out _);

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
    }

    private Vector2 FindClusteredTargetPosition()
    {
        if (Host == null)
            return Vector2.Zero;

        var closest = TargetingService.FindNearest(Host, 450.0f, skipEaten: false);
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
            bool doTick = false;
            if (_tickTimer >= 0.4f)
            {
                _tickTimer = 0.0f;
                doTick = true;
            }

            PullEnemies(dt, doTick);
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
                    if (n.HasMethod("take_damage"))
                        CombatHelper.DealDamage(n, DamagePerTick);
                    else if (n.HasMethod("be_engulfed") && dist <= 24.0f)
                        n.Call("be_engulfed", HostRef);
                }
            }, skipEaten: false);
        }

        public override void _Draw()
        {
            float alpha = Mathf.Clamp(1.0f - (_age / Duration), 0.0f, 1.0f);
            Color vortexColor = new Color(0.6f, 0.2f, 0.9f, alpha * 0.65f);
            Color coreColor = new Color(0.2f, 0.05f, 0.4f, alpha * 0.9f);

            // Dark event horizon core
            DrawCircle(Vector2.Zero, 20.0f, coreColor);
            DrawArc(Vector2.Zero, Radius, 0.0f, Mathf.Tau, 36, new Color(vortexColor.R, vortexColor.G, vortexColor.B, alpha * 0.3f), 1.5f);

            // 3 spiral vortex arms
            for (int i = 0; i < 3; i++)
            {
                float baseA = _spinAngle + i * (Mathf.Tau / 3.0f);
                for (int s = 0; s < 12; s++)
                {
                    float t = (float)s / 12.0f;
                    float armR = Radius * t;
                    float armA = baseA + t * 2.5f;
                    Vector2 p = Vector2.FromAngle(armA) * armR;
                    DrawCircle(p, 3.0f * (1.0f - t * 0.5f), vortexColor);
                }
            }
        }
    }
}
