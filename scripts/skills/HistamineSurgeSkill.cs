using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Histamine Surge (肥大細胞組胺湧浪)
/// Directional frontal cone shockwave with massive knockback and tear damage.
/// </summary>
public partial class HistamineSurgeSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 48.0f;
    [Export] public float BaseReach { get; set; } = 320.0f;
    [Export] public float ConeAngleDeg { get; set; } = 100.0f; // degrees

    public HistamineSurgeSkill()
    {
        SkillId = SkillIds.HistamineSurge;
        NameKey = "SKILL_HISTAMINE_NAME";
        DescKey = "SKILL_HISTAMINE_DESC";
        BioKey = "SKILL_HISTAMINE_BIO";
        IconSymbol = "💉";
        IsInnate = false;
        IsPassive = false;
        Cooldown = 4.8f;
        CooldownTimer = 1.5f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        Vector2 aimDir = FindAimDirection();
        float reach = GetCalculatedArea(BaseReach);
        GetDamage(BaseDamage, out float dmg, out _);
        float halfConeRad = Mathf.DegToRad(ConeAngleDeg * 0.5f);

        // Spawn visual surge cone
        var surgeVisual = new SurgeVisual
        {
            GlobalPosition = Host.GlobalPosition,
            Direction = aimDir,
            Reach = reach,
            HalfAngleRad = halfConeRad
        };
        Host.GetParent().AddChild(surgeVisual);

        // Hit enemies in cone
        TargetingService.ForEachInRadius(Host.GlobalPosition, reach, n =>
        {
            Vector2 toEnemy = n.GlobalPosition - Host.GlobalPosition;
            float dist = toEnemy.Length();
            if (dist <= 1.0f)
                return;

            float angleDiff = Mathf.Abs(aimDir.AngleTo(toEnemy));
            if (angleDiff > halfConeRad)
                return;

            // Knockback (pathogens are Node2D bodies, so the displacement is tweened)
            Vector2 push = toEnemy.Normalized();
            var tween = Host.CreateTween();
            tween.TweenProperty(n, "global_position", n.GlobalPosition + push * 80.0f, 0.2f);

            CombatHelper.DamageOrEngulf(n, dmg, Host);
        }, skipEaten: false);
    }

    private Vector2 FindAimDirection()
    {
        if (Host == null)
            return Vector2.Right;

        if (Host.Velocity.Length() > 20.0f)
            return Host.Velocity.Normalized();

        return Vector2.Right;
    }

    public partial class SurgeVisual : Node2D
    {
        public Vector2 Direction { get; set; } = Vector2.Right;
        public float Reach { get; set; } = 320.0f;
        public float HalfAngleRad { get; set; } = 0.87f;

        private float _age = 0.0f;
        private const float SurgeDuration = 0.35f;

        public override void _Process(double delta)
        {
            _age += (float)delta;
            if (_age >= SurgeDuration)
            {
                QueueFree();
                return;
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            float prog = _age / SurgeDuration;
            float curR = Reach * Mathf.Sin(prog * Mathf.Pi * 0.5f);
            float alpha = 1.0f - prog;

            float centerAngle = Direction.Angle();
            float startAngle = centerAngle - HalfAngleRad;
            float endAngle = centerAngle + HalfAngleRad;

            Color coneColor = new Color(0.95f, 0.25f, 0.45f, alpha * 0.45f);
            DrawArc(Vector2.Zero, curR, startAngle, endAngle, 24, coneColor, 4.0f);
            DrawLine(Vector2.Zero, Vector2.FromAngle(startAngle) * curR, coneColor, 2.0f);
            DrawLine(Vector2.Zero, Vector2.FromAngle(endAngle) * curR, coneColor, 2.0f);
        }
    }
}
