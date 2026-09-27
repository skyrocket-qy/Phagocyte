using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Hero;

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

    public void DetonateCaspase(Vector2 center)
    {
        if (!HasValidHost())
            return;

        // Spawn visual burst
        var burst = new ApoptosisBurstVisual { GlobalPosition = center };
        Host.GetParent().AddChild(burst);
        AudioManager.Instance?.PlaySfx("discharge_blast");
        CameraFollow.Instance?.AddTrauma(0.3f);

        float baseDmg = GetBaseDamageForLevel(BaseDamage);
        GetDamage(baseDmg, out float dmg, out _);
        float splashRadius = GetCalculatedArea(110.0f);

        TargetingService.ForEachInRadius(center, splashRadius, n =>
        {
            CombatHelper.DealDamage(n, dmg, Host, false);
        });
    }

    public partial class ApoptosisMarker : Node2D
    {
        private float _time = 0.0f;
        public override void _Process(double delta)
        {
            _time += (float)delta * 10.0f;
            QueueRedraw();
        }

        public override void _Draw()
        {
            float pulse = 1.0f + 0.25f * Mathf.Sin(_time);
            Color halo = SkillAssetPalette.Accent(SkillIds.GranzymeDetonation, new Color(0.91f, 0.55f, 0.25f));
            Color core = SkillAssetPalette.Core(SkillIds.GranzymeDetonation, new Color(1.0f, 0.85f, 0.65f));

            // 1. Ambient apoptotic aura
            DrawCircle(Vector2.Zero, 22.0f * pulse, halo with { A = 0.22f });

            // 2. Caspase cleavage tri-lobe glyph (3 biological execution lobes)
            for (int i = 0; i < 3; i++)
            {
                float angle = _time * 0.8f + i * (Mathf.Tau / 3.0f);
                Vector2 lobePos = Vector2.FromAngle(angle) * (14.0f * pulse);
                DrawCircle(lobePos, 5.0f, halo with { A = 0.6f });
                DrawCircle(lobePos, 3.0f, core with { A = 0.95f });
                // Cleavage bridge
                DrawLine(Vector2.Zero, lobePos, core with { A = 0.8f }, 1.5f);
            }

            // 3. Central caspase protease core
            DrawCircle(Vector2.Zero, 6.0f * pulse, core with { A = 0.95f });
            DrawArc(Vector2.Zero, 18.0f * pulse, 0.0f, Mathf.Tau, 32, halo with { A = 0.85f }, 2.0f);
        }
    }

    public partial class ApoptosisBurstVisual : Node2D
    {
        private float _age = 0.0f;
        private const float Duration = 0.45f;
        private readonly float[] _shardAngles = new float[10];
        private readonly float[] _shardLengths = new float[10];

        public override void _Ready()
        {
            for (int i = 0; i < 10; i++)
            {
                _shardAngles[i] = i * (Mathf.Tau / 10.0f) + (GD.Randf() - 0.5f) * 0.3f;
                _shardLengths[i] = 16.0f + GD.Randf() * 18.0f;
            }
        }

        public override void _Process(double delta)
        {
            _age += (float)delta;
            if (_age >= Duration)
            {
                QueueFree();
                return;
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            float progress = _age / Duration;
            float r = 125.0f * Mathf.Sqrt(progress);
            float a = 1.0f - progress;
            Color halo = SkillAssetPalette.Accent(SkillIds.GranzymeDetonation, new Color(0.91f, 0.55f, 0.25f));
            Color core = SkillAssetPalette.Core(SkillIds.GranzymeDetonation, new Color(1.0f, 0.9f, 0.75f));

            // 1. Dual-ring apoptotic shockwave
            LaserGlow.DrawImpactHalo(this, Vector2.Zero, r, halo, core, a, 3.5f);
            DrawArc(Vector2.Zero, r * 0.75f, 0.0f, Mathf.Tau, 36, halo with { A = a * 0.7f }, 2.0f);
            DrawCircle(Vector2.Zero, r * 0.45f, core with { A = a * a * 0.5f });

            // 2. Fragmented chromatin DNA strand shards radiating outward
            for (int i = 0; i < 10; i++)
            {
                Vector2 dir = Vector2.FromAngle(_shardAngles[i]);
                Vector2 start = dir * (r * 0.5f);
                Vector2 end = start + dir * (_shardLengths[i] * (1.0f - progress * 0.4f));
                // Shard glow + core line
                DrawLine(start, end, halo with { A = a * 0.6f }, 3.5f);
                DrawLine(start, end, core with { A = a * 0.95f }, 1.5f);
                // Bleb tip
                DrawCircle(end, 2.5f, core with { A = a * 0.9f });
            }
        }
    }
}
