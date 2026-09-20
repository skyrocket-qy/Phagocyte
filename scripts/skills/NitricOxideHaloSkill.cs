using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Nitric Oxide Halo (一氧化氮光環)
/// Permanent circular toxic gas aura around the cell membrane, burning close-range pathogens.
/// </summary>
public partial class NitricOxideHaloSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 8.0f;
    [Export] public float BaseRadius { get; set; } = 95.0f;

    private HaloVisual? _visual;
    private float _pulsePhase = 0.0f;

    public partial class HaloVisual : Node2D
    {
        public NitricOxideHaloSkill? Skill { get; set; }
        public float Radius { get; set; } = 95.0f;
        public float Phase { get; set; } = 0.0f;

        public override void _Draw()
        {
            if (Skill == null || Skill.Host == null)
                return;

            float pulse = 1.0f + 0.06f * Mathf.Sin(Phase);
            float r = Radius * pulse;

            // Outer soft toxic gas aura
            Color outerColor = new Color(0.15f, 0.95f, 0.65f, 0.15f);
            DrawCircle(Vector2.Zero, r, outerColor);

            // Shimmering boundary rings
            Color ringColor = new Color(0.25f, 1.0f, 0.75f, 0.45f);
            DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 48, ringColor, 2.5f);

            Color innerRing = new Color(0.1f, 0.8f, 0.5f, 0.25f);
            DrawArc(Vector2.Zero, r * 0.82f, 0.0f, Mathf.Tau, 36, innerRing, 1.5f);
        }
    }

    public NitricOxideHaloSkill()
    {
        SkillId = SkillIds.NitricOxideHalo;
        NameKey = "SKILL_NO_NAME";
        DescKey = "SKILL_NO_DESC";
        BioKey = "SKILL_NO_BIO";
        IconSymbol = "⭕";
        IsInnate = false;
        IsPassive = false;
        Cooldown = 0.25f; // fast continuous tick
        CooldownTimer = 0.1f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Setup(CharacterBody2D pHost, int pSlot)
    {
        base.Setup(pHost, pSlot);
        if (_visual == null)
        {
            _visual = new HaloVisual { Skill = this };
            AddChild(_visual);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;

        GlobalPosition = Host.GlobalPosition;
        _pulsePhase += (float)delta * 4.0f;

        if (_visual != null)
        {
            float hostR = Host.Get("CurrentRadius").VariantType == Variant.Type.Float
                ? (float)Host.Get("CurrentRadius")
                : 48.0f;
            _visual.Radius = hostR + GetCalculatedArea(BaseRadius - 48.0f);
            _visual.Phase = _pulsePhase;
            _visual.QueueRedraw();
        }
    }

    public override void Trigger()
    {
        base.Trigger();
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;

        float hostR = Host.Get("CurrentRadius").VariantType == Variant.Type.Float
            ? (float)Host.Get("CurrentRadius")
            : 48.0f;
        float currentRadius = hostR + GetCalculatedArea(BaseRadius - 48.0f);
        var damageData = GetCalculatedDamage(BaseDamage);
        float dmg = (float)damageData["damage"];

        TargetingService.ForEachInRadius(Host.GlobalPosition, currentRadius, n =>
        {
            if (n.HasMethod("take_damage"))
            {
                CombatHelper.DealDamage(n, dmg);
            }
            else if (n.HasMethod("be_engulfed"))
            {
                var hpVar = n.Get("health");
                if (hpVar.VariantType == Variant.Type.Float && (float)hpVar <= dmg * 1.5f)
                {
                    n.Call("be_engulfed", Host);
                }
            }
        });
    }
}
