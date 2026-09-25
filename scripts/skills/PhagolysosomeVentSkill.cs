using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Phagolysosome Vent (胞吐酸性排毒)
/// Drops acidic mucus puddles along cell trajectory, burning enemies stepping into them.
/// </summary>
public partial class PhagolysosomeVentSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 14.0f;
    [Export] public float BaseDuration { get; set; } = 3.5f;
    [Export] public float BaseRadius { get; set; } = 42.0f;

    public PhagolysosomeVentSkill()
    {
        SkillId = SkillIds.PhagolysosomeVent;
        NameKey = "SKILL_PHAGO_VENT_NAME";
        DescKey = "SKILL_PHAGO_VENT_DESC";
        BioKey = "SKILL_PHAGO_VENT_BIO";
        IconSymbol = "🛢️";
        IsInnate = false;
        IsPassive = false;
        Cooldown = 2.0f;
        CooldownTimer = 0.5f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        float duration = GetCalculatedDuration(BaseDuration);
        float radius = GetCalculatedArea(BaseRadius);
        GetDamage(BaseDamage, out float dmg, out _);

        var puddle = new AcidPuddle
        {
            GlobalPosition = Host.GlobalPosition,
            Duration = duration,
            Radius = radius,
            DamagePerTick = dmg,
            HostRef = Host
        };
        Host.GetParent().AddChild(puddle);
    }

    public partial class AcidPuddle : Node2D
    {
        public float Duration { get; set; } = 3.5f;
        public float Radius { get; set; } = 42.0f;
        public float DamagePerTick { get; set; } = 14.0f;
        public CharacterBody2D? HostRef { get; set; }

        private float _age = 0.0f;
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

            _tickTimer += dt;
            if (_tickTimer >= 0.35f)
            {
                _tickTimer = 0.0f;
                DamageEnemiesInPuddle();
            }

            QueueRedraw();
        }

        private void DamageEnemiesInPuddle()
        {
            if (HostRef == null)
                return;

            TargetingService.ForEachInRadius(GlobalPosition, Radius, n =>
            {
                CombatHelper.DealDamage(n, DamagePerTick, HostRef, false);
            });
        }

        public override void _Draw()
        {
            float alpha = Mathf.Clamp(1.0f - (_age / Duration), 0.0f, 1.0f);
            Color accent = SkillAssetPalette.Accent(SkillIds.PhagolysosomeVent, new Color(0.93f, 0.27f, 0.26f));
            Color core = SkillAssetPalette.Core(SkillIds.PhagolysosomeVent, new Color(1.0f, 0.70f, 0.55f));

            Color puddleBase = new Color(accent.R * 0.4f, accent.G * 0.1f, accent.B * 0.1f, alpha * 0.55f);
            Color puddleGlow = new Color(accent.R, accent.G, accent.B, alpha * 0.75f);

            // Organic amoeboid acidic puddle boundary (12 harmonic lobes)
            Vector2[] lobes = new Vector2[16];
            for (int i = 0; i < 16; i++)
            {
                float a = i * (Mathf.Tau / 16.0f);
                float rOffset = Mathf.Sin(a * 3.0f + _age * 4.0f) * 4.0f;
                lobes[i] = Vector2.FromAngle(a) * (Radius + rOffset);
            }
            DrawColoredPolygon(lobes, puddleBase);
            for (int i = 0; i < 16; i++)
            {
                DrawLine(lobes[i], lobes[(i + 1) % 16], puddleGlow, 2.0f);
            }

            // Boiling effervescent enzymatic bubbles
            float bPhase = _age * 6.0f;
            for (int b = 0; b < 4; b++)
            {
                float ba = b * (Mathf.Tau / 4.0f) + bPhase * 0.3f;
                float br = Radius * (0.3f + 0.35f * Mathf.Sin(bPhase + b * 1.5f));
                Vector2 bPos = Vector2.FromAngle(ba) * br;
                float bSize = 2.5f + 1.5f * Mathf.Sin(bPhase * 2.0f + b);
                DrawCircle(bPos, bSize, new Color(core.R, core.G, core.B, alpha * 0.9f));
                DrawArc(bPos, bSize + 1.0f, 0.0f, Mathf.Tau, 12, puddleGlow, 1.0f);
            }

            LaserGlow.DrawImpactHalo(this, Vector2.Zero, Radius * 0.45f, accent, core, alpha * 0.5f, 1.5f);
        }
    }
}
