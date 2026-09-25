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
    private float _redrawAccum = 0.0f;

    public partial class HaloVisual : Node2D
    {
        public NitricOxideHaloSkill? Skill { get; set; }
        public float Radius { get; set; } = 95.0f;
        public float Phase { get; set; } = 0.0f;

        public override void _Draw()
        {
            if (Skill == null || Skill.Host == null)
                return;

            Color accent = SkillAssetPalette.Accent(SkillIds.NitricOxideHalo, new Color(0.3f, 0.79f, 0.89f));
            Color core = SkillAssetPalette.Core(SkillIds.NitricOxideHalo, Colors.White);

            float pulse = 1.0f + 0.05f * Mathf.Sin(Phase);
            float r = Radius * pulse;

            // Diffuse inner toxic gas body
            DrawCircle(Vector2.Zero, r * 0.85f, new Color(accent.R, accent.G, accent.B, 0.12f));

            // Soft core excitation halo
            LaserGlow.DrawImpactHalo(this, Vector2.Zero, r * 0.45f, accent, core, 0.22f, 1.6f);

            // Multi-harmonic fluctuating diffusion rings
            DrawArc(Vector2.Zero, r * 0.55f, 0.0f, Mathf.Tau, 32, new Color(core.R, core.G, core.B, 0.18f), 1.5f);
            DrawArc(Vector2.Zero, r * 0.82f, 0.0f, Mathf.Tau, 40, new Color(accent.R, accent.G, accent.B, 0.28f), 2.0f);
            DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 48, new Color(accent.R, accent.G, accent.B, 0.45f), 2.8f);

            // Brownian undulating gas puffs along perimeter
            const int Puffs = 10;
            for (int i = 0; i < Puffs; i++)
            {
                float angle = (i / (float)Puffs) * Mathf.Tau + Phase * 0.6f;
                float puffOffset = 6.0f * Mathf.Sin(Phase * 2.0f + i * 1.7f);
                float puffR = r + puffOffset;
                Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * puffR;
                DrawCircle(pos, 5.0f + 2.0f * Mathf.Cos(Phase + i), new Color(accent.R, accent.G, accent.B, 0.20f));
            }
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

    public override void Setup(CharacterBody2D pHost)
    {
        base.Setup(pHost);
        if (_visual == null)
        {
            _visual = new HaloVisual { Skill = this };
            AddChild(_visual);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!HasValidHost())
            return;

        GlobalPosition = Host.GlobalPosition;
        _pulsePhase += (float)delta * 4.0f;

        // Position follows every frame; the slow pulse redraws at 30 Hz.
        _redrawAccum += (float)delta;
        if (_redrawAccum >= 1.0f / 30.0f)
        {
            _redrawAccum = 0.0f;
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
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        float hostR = Host.Get("CurrentRadius").VariantType == Variant.Type.Float
            ? (float)Host.Get("CurrentRadius")
            : 48.0f;
        float currentRadius = hostR + GetCalculatedArea(BaseRadius - 48.0f);
        GetDamage(BaseDamage, out float dmg, out _);

        TargetingService.ForEachInRadius(Host.GlobalPosition, currentRadius, n =>
        {
            CombatHelper.DealDamage(n, dmg, Host, false);
        });
    }
}
