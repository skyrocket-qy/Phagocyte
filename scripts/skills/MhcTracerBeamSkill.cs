using Godot;
using System;
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
        SkillId = "mhc_tracer_beam";
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
        if (Host == null || !GodotObject.IsInstanceValid(Host))
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
        if (Host == null || !GodotObject.IsInstanceValid(Host))
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
                var dmgData = GetCalculatedDamage(BaseDps * (float)delta);
                float dmg = (float)dmgData["damage"];
                if (_currentTarget.HasMethod("take_damage"))
                    _currentTarget.Call("take_damage", dmg);

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

        var pathogens = Host.GetTree().GetNodesInGroup("pathogens");
        Node2D? best = null;
        float minDist = SearchRange;

        foreach (var p in pathogens)
        {
            if (p is Node2D n && GodotObject.IsInstanceValid(n))
            {
                float d = Host.GlobalPosition.DistanceTo(n.GlobalPosition);
                if (d < minDist)
                {
                    minDist = d;
                    best = n;
                }
            }
        }
        return best;
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
            Color beamGlow = new Color(1.0f, 0.25f, 0.35f, 0.45f);
            Color beamCore = new Color(1.0f, 0.85f, 0.85f, 0.95f);

            DrawLine(Vector2.Zero, localTarget, beamGlow, 5.0f);
            DrawLine(Vector2.Zero, localTarget, beamCore, 2.0f);

            // Reticle at target
            DrawArc(localTarget, 16.0f, 0.0f, Mathf.Tau, 24, new Color(1.0f, 0.2f, 0.3f, 0.8f), 2.0f);
            DrawLine(localTarget + new Vector2(-20, 0), localTarget + new Vector2(20, 0), beamCore, 1.5f);
            DrawLine(localTarget + new Vector2(0, -20), localTarget + new Vector2(0, 20), beamCore, 1.5f);
        }
    }
}
