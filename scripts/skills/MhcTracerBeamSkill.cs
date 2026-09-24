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

    public override void Setup(CharacterBody2D pHost, int pSlot)
    {
        base.Setup(pHost, pSlot);
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
                GetDamage(BaseDps * (float)delta, out float dmg, out _);
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

        return TargetingService.FindNearest(Host, SearchRange, skipEaten: false);
    }

    public partial class TracerVisual : Node2D
    {
        public MhcTracerBeamSkill? Skill { get; set; }
        public bool IsActive { get; set; } = false;
        public Vector2 TargetGlobalPos { get; set; }

        public override void _Draw()
        {
            if (!IsActive || Skill == null || Skill.Host == null)
                return;

            Vector2 localTarget = ToLocal(TargetGlobalPos);
            Color accent = SkillAssetPalette.Accent(SkillIds.MhcTracerBeam, new Color(0.11f, 0.77f, 0.34f));
            Color core = SkillAssetPalette.Core(SkillIds.MhcTracerBeam, new Color(0.85f, 1.0f, 0.90f));

            // Central confocal laser tracer beam
            LaserGlow.DrawBeam(this, Vector2.Zero, localTarget, accent, core, 6.0f, 0.9f);

            // Twin parallel pilot laser lines
            Vector2 perp = (localTarget).Normalized().Orthogonal() * 4.5f;
            DrawLine(perp, localTarget + perp, new Color(accent.R, accent.G, accent.B, 0.45f), 1.5f);
            DrawLine(-perp, localTarget - perp, new Color(accent.R, accent.G, accent.B, 0.45f), 1.5f);

            // Emitter lens flare at host
            LaserGlow.DrawImpactHalo(this, Vector2.Zero, 8.0f, accent, core, 0.85f, 1.5f);

            // Animated molecular targeting reticle at pathogen
            float reticleRadius = 18.0f;
            DrawArc(localTarget, reticleRadius, 0.0f, Mathf.Tau, 32, accent, 2.0f);
            DrawArc(localTarget, reticleRadius * 0.55f, 0.0f, Mathf.Tau, 16, core, 1.5f);

            // Precision crosshairs
            DrawLine(localTarget + new Vector2(-24, 0), localTarget + new Vector2(-6, 0), core, 1.5f);
            DrawLine(localTarget + new Vector2(6, 0), localTarget + new Vector2(24, 0), core, 1.5f);
            DrawLine(localTarget + new Vector2(0, -24), localTarget + new Vector2(0, -6), core, 1.5f);
            DrawLine(localTarget + new Vector2(0, 6), localTarget + new Vector2(0, 24), core, 1.5f);

            LaserGlow.DrawImpactHalo(this, localTarget, 10.0f, accent, core, 0.95f, 2.0f);
        }
    }
}
