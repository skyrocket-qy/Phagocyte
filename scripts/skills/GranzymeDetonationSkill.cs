using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Granzyme Detonation (顆粒酶延遲爆發)
/// Injects apoptotic granzyme into a high-threat enemy, triggering Caspase explosion after 1.5s.
/// </summary>
public partial class GranzymeDetonationSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 130.0f;
    [Export] public float SearchRange { get; set; } = 550.0f;
    [Export] public float DetonationDelay { get; set; } = 1.5f;

    public GranzymeDetonationSkill()
    {
        SkillId = SkillIds.GranzymeDetonation;
        NameKey = "SKILL_GRANZYME_NAME";
        DescKey = "SKILL_GRANZYME_DESC";
        BioKey = "SKILL_GRANZYME_BIO";
        IconSymbol = "🧬";
        IsInnate = true;
        IsPassive = false;
        Cooldown = 3.5f;
        CooldownTimer = 1.5f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        Node2D? target = FindPriorityTarget();
        if (target != null && GodotObject.IsInstanceValid(target))
        {
            InjectGranzyme(target);
        }
    }

    private Node2D? FindPriorityTarget()
    {
        if (Host == null)
            return null;

        return TargetingService.FindNearest(Host, SearchRange);
    }

    private void InjectGranzyme(Node2D target)
    {
        if (Host == null)
            return;

        // Visual injection particle / marker attached to target
        var marker = new ApoptosisMarker();
        target.AddChild(marker);

        var tree = Host.GetTree();
        if (tree == null)
            return;

        var timer = tree.CreateTimer(DetonationDelay);
        timer.Timeout += () =>
        {
            if (target != null && GodotObject.IsInstanceValid(target))
            {
                DetonateCaspase(target.GlobalPosition);
            }
        };
    }

    private void DetonateCaspase(Vector2 center)
    {
        if (!HasValidHost())
            return;

        // Spawn visual burst
        var burst = new ApoptosisBurstVisual { GlobalPosition = center };
        Host.GetParent().AddChild(burst);

        GetDamage(BaseDamage, out float dmg, out _);
        float splashRadius = GetCalculatedArea(110.0f);

        TargetingService.ForEachInRadius(center, splashRadius, n =>
        {
            CombatHelper.DamageOrEngulf(n, dmg, Host);
        });
    }

    public partial class ApoptosisMarker : Node2D
    {
        private float _time = 0.0f;
        public override void _Process(double delta)
        {
            _time += (float)delta * 8.0f;
            QueueRedraw();
        }

        public override void _Draw()
        {
            float pulse = 1.0f + 0.3f * Mathf.Sin(_time);
            DrawCircle(Vector2.Zero, 12.0f * pulse, new Color(0.85f, 0.2f, 1.0f, 0.4f));
            DrawArc(Vector2.Zero, 18.0f * pulse, 0.0f, Mathf.Tau, 24, new Color(0.9f, 0.4f, 1.0f, 0.8f), 2.0f);
        }
    }

    public partial class ApoptosisBurstVisual : Node2D
    {
        private float _age = 0.0f;
        public override void _Process(double delta)
        {
            _age += (float)delta;
            if (_age >= 0.4f)
            {
                QueueFree();
                return;
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            float progress = _age / 0.4f;
            float r = 110.0f * progress;
            float a = 1.0f - progress;
            DrawCircle(Vector2.Zero, r * 0.7f, new Color(0.7f, 0.1f, 0.9f, a * 0.4f));
            DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 36, new Color(1.0f, 0.3f, 0.95f, a), 3.0f);
        }
    }
}
