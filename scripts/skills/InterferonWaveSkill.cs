using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Interferon Wave (干擾素衝擊波)
/// Wide-area 360-degree shockwave knocking back all screen enemies and applying 50% slow (replication inhibition).
/// </summary>
public partial class InterferonWaveSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 32.0f;
    [Export] public float BaseRadius { get; set; } = 480.0f;

    public InterferonWaveSkill()
    {
        SkillId = SkillIds.InterferonWave;
        NameKey = "SKILL_INTERFERON_NAME";
        DescKey = "SKILL_INTERFERON_DESC";
        BioKey = "SKILL_INTERFERON_BIO";
        IconSymbol = "🌊";
        IsInnate = false;
        IsPassive = false;
        Cooldown = 6.0f;
        CooldownTimer = 2.0f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        float maxRadius = GetCalculatedArea(BaseRadius);
        GetDamage(BaseDamage, out float dmg, out _);

        // Spawn expanding visual wave
        var wave = new WaveVisual
        {
            GlobalPosition = Host.GlobalPosition,
            MaxRadius = maxRadius
        };
        Host.GetParent().AddChild(wave);

        // Hit pathogens
        TargetingService.ForEachInRadius(Host.GlobalPosition, maxRadius, n =>
        {
            Vector2 pushDir = (n.GlobalPosition - Host.GlobalPosition).Normalized();
            if (pushDir == Vector2.Zero)
                pushDir = Vector2.Right;

            // Knockback (pathogens are Node2D bodies, so the displacement is tweened)
            var tween = Host.CreateTween();
            tween.TweenProperty(n, "global_position", n.GlobalPosition + pushDir * 65.0f, 0.2f);

            // Replication inhibition: 50% slow for 2s
            if (n is BaseEnemy be)
                be.ApplySlow(2.0f, 0.5f);
            else if (n.HasMethod("apply_slow"))
                n.Call("apply_slow", 2.0f, 0.5f);

            CombatHelper.DealDamage(n, dmg, Host, false);
        });
    }

    public partial class WaveVisual : Node2D
    {
        public float MaxRadius { get; set; } = 480.0f;
        private float _age = 0.0f;
        private const float WaveDuration = 0.55f;

        public override void _Process(double delta)
        {
            _age += (float)delta;
            if (_age >= WaveDuration)
            {
                QueueFree();
                return;
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            float prog = _age / WaveDuration;
            float r = MaxRadius * prog;
            float alpha = 1.0f - prog;

            Color accent = SkillAssetPalette.Accent(SkillIds.InterferonWave, new Color(0.1f, 0.67f, 0.78f));
            Color core = SkillAssetPalette.Core(SkillIds.InterferonWave, Colors.White);

            // Origin dissipation pulse
            if (prog < 0.5f)
            {
                LaserGlow.DrawImpactHalo(this, Vector2.Zero, 28.0f * (1.0f - prog * 2.0f), accent, core, (0.5f - prog) * 1.2f, 1.5f);
            }

            // Leading high-pressure compression shockwave
            Color leadCoreColor = new Color(core.R, core.G, core.B, alpha * 0.95f);
            Color leadAccentColor = new Color(accent.R, accent.G, accent.B, alpha * 0.85f);
            DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 64, leadCoreColor, 2.5f * (1.0f - prog * 0.4f));
            DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 64, leadAccentColor, 5.0f * (1.0f - prog * 0.5f));

            // Outer refraction fringe
            Color outerFringe = new Color(accent.R, accent.G, accent.B, alpha * 0.35f);
            DrawArc(Vector2.Zero, r * 1.03f, 0.0f, Mathf.Tau, 48, outerFringe, 1.5f);

            // Resonant harmonic trailing ripples
            if (r > 30.0f)
            {
                Color midEcho = new Color(accent.R, accent.G, accent.B, alpha * 0.45f);
                DrawArc(Vector2.Zero, r * 0.86f, 0.0f, Mathf.Tau, 48, midEcho, 2.2f);

                Color innerEcho = new Color(core.R, core.G, core.B, alpha * 0.25f);
                DrawArc(Vector2.Zero, r * 0.72f, 0.0f, Mathf.Tau, 36, innerEcho, 1.5f);

                // Acoustic nodal radial spikes across the compression wavefront
                const int Spikes = 16;
                for (int i = 0; i < Spikes; i++)
                {
                    float angle = (i / (float)Spikes) * Mathf.Tau;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 p0 = dir * (r * 0.88f);
                    Vector2 p1 = dir * (r * 1.02f);
                    DrawLine(p0, p1, new Color(core.R, core.G, core.B, alpha * 0.4f), 1.5f);
                }
            }
        }
    }
}
