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

            Color accent = SkillAssetPalette.Accent(SkillIds.NitricOxideHalo, new Color(0.30f, 0.79f, 0.89f));
            Color core = SkillAssetPalette.Core(SkillIds.NitricOxideHalo, Colors.White);

            float pulse = 1.0f + 0.06f * Mathf.Sin(Phase * 1.5f);
            float r = Radius * pulse;

            // 1. Diffuse inner reactive nitrogen gas cloud
            DrawCircle(Vector2.Zero, r * 0.9f, accent with { A = 0.16f });

            // 2. Core excitation glow near host membrane
            LaserGlow.DrawImpactHalo(this, Vector2.Zero, r * 0.4f, accent, core, 0.35f, 2.0f);

            // 3. Fluctuating middle acoustic/diffusion ripples
            DrawArc(Vector2.Zero, r * 0.65f, 0.0f, Mathf.Tau, 36, accent with { A = 0.35f }, 1.8f);
            DrawArc(Vector2.Zero, r * 0.65f, 0.0f, Mathf.Tau, 36, core with { A = 0.5f }, 1.0f);

            // 4. Primary outer toxic gas boundary (high-contrast dual arc)
            DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 48, accent with { A = 0.85f }, 3.5f);
            DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 48, core with { A = 0.95f }, 1.6f);

            // 5. Undulating radical gas puffs with core excitation along perimeter
            const int Puffs = 12;
            for (int i = 0; i < Puffs; i++)
            {
                float angle = (i / (float)Puffs) * Mathf.Tau + Phase * 0.5f;
                float puffOffset = 7.0f * Mathf.Sin(Phase * 2.2f + i * 1.5f);
                float puffR = r + puffOffset;
                Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * puffR;
                float pRadius = 5.5f + 2.0f * Mathf.Cos(Phase + i * 0.8f);

                DrawCircle(pos, pRadius + 2.0f, accent with { A = 0.45f });
                DrawCircle(pos, pRadius, core with { A = 0.85f });
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
        float baseDmg = GetBaseDamageForLevel(BaseDamage);
        GetDamage(baseDmg, out float dmg, out _);

        int hitAny = 0;
        TargetingService.ForEachInRadius(Host.GlobalPosition, currentRadius, n =>
        {
            hitAny++;
            CombatHelper.DealDamage(n, dmg, Host, false);

            if (n is Node node && node.GetNodeOrNull<AilmentController>("AilmentController") is AilmentController ac)
            {
                ac.ApplyMembraneLeak(dmg * 0.25f, 1.5f);
            }
        });

        if (hitAny > 0)
        {
            AudioManager.Instance?.PlaySfx("shock_nova");
        }
    }
}
