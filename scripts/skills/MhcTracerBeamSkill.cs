using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: MHC Tracer Beam (MHC弱點標定光束)
/// Continuous tracking presentation beam stripping stealth and granting 100% crit chance on target.
/// </summary>
public partial class MhcTracerBeamSkill : BaseSkill
{
    [Export] public float BaseDps { get; set; } = 32.0f;
    [Export] public float BaseDuration { get; set; } = 2.0f;
    [Export] public float SearchRange { get; set; } = 520.0f;

    private TracerVisual? _tracer;
    private Node2D? _currentTarget;
    private float _activeChannelTimer = 0.0f;

    public MhcTracerBeamSkill()
    {
        SkillId = SkillIds.MhcTracerBeam;
        NameKey = "SKILL_MHC_TRACER_NAME";
        DescKey = "SKILL_MHC_TRACER_DESC";
        BioKey = "SKILL_MHC_TRACER_BIO";
        IconSymbol = "🎯";
        IsInnate = true;
        IsPassive = false;
        Cooldown = 4.0f;
        CooldownTimer = 1.0f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Setup(CharacterBody2D pHost)
    {
        base.Setup(pHost);
        if (_tracer == null)
        {
            _tracer = new TracerVisual { Skill = this };
            AddChild(_tracer);
        }
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        _currentTarget = FindBestTarget();
        if (_currentTarget != null)
        {
            _activeChannelTimer = GetCalculatedDuration(BaseDuration);
            AudioManager.Instance?.PlaySfx("scorching_ray");
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!HasValidHost())
            return;

        GlobalPosition = Host.GlobalPosition;

        if (_activeChannelTimer > 0.0f)
        {
            _activeChannelTimer -= (float)delta;
            if (_currentTarget == null || !GodotObject.IsInstanceValid(_currentTarget))
            {
                _currentTarget = FindBestTarget();
            }

            if (_currentTarget != null && GodotObject.IsInstanceValid(_currentTarget))
            {
                float baseDps = GetBaseDamageForLevel(BaseDps);
                GetDamage(baseDps * (float)delta, out float dmg, out _);
                CombatHelper.DealDamage(_currentTarget, dmg);

                // Apply vulnerability tag (metadata flag, clearable by antigenic drift)
                _currentTarget.SetMeta("mhc_marked", true);

                if (_tracer != null)
                {
                    _tracer.IsActive = true;
                    _tracer.TargetGlobalPos = _currentTarget.GlobalPosition;
                    _tracer.QueueRedraw();
                }
            }
            else
            {
                if (_tracer != null)
                    _tracer.IsActive = false;
            }
        }
        else
        {
            if (_tracer != null && _tracer.IsActive)
            {
                _tracer.IsActive = false;
                _tracer.QueueRedraw();
            }
        }
    }

    private Node2D? FindBestTarget()
    {
        if (Host == null)
            return null;

        return TargetingService.FindNearest(Host, SearchRange);
    }

    public partial class TracerVisual : Node2D
    {
        public MhcTracerBeamSkill? Skill { get; set; }
        public bool IsActive { get; set; } = false;
        public Vector2 TargetGlobalPos { get; set; }

        private float _animTime = 0.0f;

        public override void _Process(double delta)
        {
            if (IsActive)
            {
                _animTime += (float)delta * 6.0f;
                QueueRedraw();
            }
        }

        public override void _Draw()
        {
            if (!IsActive || Skill == null || Skill.Host == null)
                return;

            Vector2 localTarget = ToLocal(TargetGlobalPos);
            Color accent = SkillAssetPalette.Accent(SkillIds.MhcTracerBeam, new Color(0.11f, 0.77f, 0.34f));
            Color core = SkillAssetPalette.Core(SkillIds.MhcTracerBeam, new Color(0.85f, 1.0f, 0.90f));

            // 1. Ambient fluorophore diffusion corridor
            DrawLine(Vector2.Zero, localTarget, accent with { A = 0.25f }, 22.0f);

            // 2. Central confocal laser tracer beam
            LaserGlow.DrawBeam(this, Vector2.Zero, localTarget, accent, core, 10.0f, 0.95f);

            // 3. Dynamic peptide scanning guide rails with wave pulse
            Vector2 dir = localTarget.Normalized();
            Vector2 perp = dir.Orthogonal() * 7.0f;
            DrawLine(perp, localTarget + perp, accent with { A = 0.55f }, 2.0f);
            DrawLine(-perp, localTarget - perp, accent with { A = 0.55f }, 2.0f);

            // Scanning pulse packet traveling along beam
            float travelT = (_animTime * 0.4f) % 1.0f;
            Vector2 pulsePos = Vector2.Zero.Lerp(localTarget, travelT);
            LaserGlow.DrawImpactHalo(this, pulsePos, 8.0f, accent, core, 0.9f, 2.0f);

            // 4. Emitter lens flare at host
            LaserGlow.DrawImpactHalo(this, Vector2.Zero, 14.0f, accent, core, 0.95f, 2.5f);

            // 5. Peptide presentation scanning aperture at pathogen
            float reticleRadius = 22.0f + Mathf.Sin(_animTime * 2.0f) * 2.5f;
            DrawArc(localTarget, reticleRadius, 0.0f, Mathf.Tau, 36, accent, 2.5f);
            DrawArc(localTarget, reticleRadius * 0.6f, 0.0f, Mathf.Tau, 24, core, 1.8f);

            // Counter-rotating aperture ticks
            for (int i = 0; i < 4; i++)
            {
                float a = _animTime + i * (Mathf.Tau / 4.0f);
                Vector2 tickStart = localTarget + Vector2.FromAngle(a) * (reticleRadius - 4.0f);
                Vector2 tickEnd = localTarget + Vector2.FromAngle(a) * (reticleRadius + 6.0f);
                DrawLine(tickStart, tickEnd, core, 2.0f);
            }

            LaserGlow.DrawImpactHalo(this, localTarget, 16.0f, accent, core, 0.95f, 2.5f);
        }
    }
}
