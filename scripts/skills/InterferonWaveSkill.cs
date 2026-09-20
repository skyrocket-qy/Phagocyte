using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Interferon Wave (干擾素衝擊波)
/// Wide-area 360-degree shockwave knocking back all screen enemies and applying 50% slow (replication inhibition).
/// </summary>
public partial class InterferonWaveSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 32.0f;
    [Export] public float BaseRadius { get; set; } = 480.0f;
    [Export] public float BaseKnockback { get; set; } = 360.0f;

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
            if (n.HasMethod("apply_slow"))
                n.Call("apply_slow", 2.0f, 0.5f);

            if (n.HasMethod("take_damage"))
                CombatHelper.DealDamage(n, dmg);
            else if (n.HasMethod("be_engulfed"))
                n.Call("be_engulfed", Host);
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

            // Outer shockwave arc
            Color outerColor = new Color(0.2f, 0.75f, 1.0f, alpha * 0.85f);
            DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 48, outerColor, 4.0f * (1.0f - prog * 0.5f));

            // Secondary trailing echo
            if (r > 30.0f)
            {
                Color innerColor = new Color(0.4f, 0.9f, 1.0f, alpha * 0.4f);
                DrawArc(Vector2.Zero, r * 0.85f, 0.0f, Mathf.Tau, 36, innerColor, 2.0f);
            }
        }
    }
}
