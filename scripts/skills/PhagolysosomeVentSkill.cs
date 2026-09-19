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
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;

        float duration = GetCalculatedDuration(BaseDuration);
        float radius = GetCalculatedArea(BaseRadius);
        var dmgData = GetCalculatedDamage(BaseDamage);
        float dmg = (float)dmgData["damage"];

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
                if (n.HasMethod("take_damage"))
                    n.Call("take_damage", DamagePerTick);
                else if (n.HasMethod("be_engulfed"))
                    n.Call("be_engulfed", HostRef);
            }, skipEaten: false);
        }

        public override void _Draw()
        {
            float alpha = Mathf.Clamp(1.0f - (_age / Duration), 0.0f, 1.0f);
            Color acidCore = new Color(0.7f, 0.95f, 0.1f, alpha * 0.5f);
            Color acidEdge = new Color(0.9f, 0.9f, 0.2f, alpha * 0.75f);

            DrawCircle(Vector2.Zero, Radius, acidCore);
            DrawArc(Vector2.Zero, Radius, 0.0f, Mathf.Tau, 24, acidEdge, 2.0f);

            // Small bubbling dots
            DrawCircle(new Vector2(Radius * 0.3f, -Radius * 0.2f), 4.0f, acidEdge);
            DrawCircle(new Vector2(-Radius * 0.4f, Radius * 0.3f), 3.0f, acidEdge);
        }
    }
}
