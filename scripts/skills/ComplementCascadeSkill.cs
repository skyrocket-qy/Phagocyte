using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;
using Phagocyte.Player;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Complement Cascade (補體瀑布)
/// Deposits MAC assembly mines that polymerize C9 subunits into a
/// transmembrane ring pore, then rupture targets via osmotic swelling.
/// </summary>
public partial class ComplementCascadeSkill : BaseSkill
{
    [Export] public float BaseAreaRadius { get; set; } = 80.0f;

    public ComplementCascadeSkill()
    {
        SkillId = SkillIds.ComplementCascade;
        NameKey = "SKILL_COMPLEMENT_NAME";
        DescKey = "SKILL_COMPLEMENT_DESC";
        BioKey = "SKILL_COMPLEMENT_BIO";
        IconSymbol = "💥";
        IsInnate = false;
        IsPassive = false;
        Cooldown = 4.0f;
        CooldownTimer = 1.5f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        int amount = GetCalculatedAmount(1);
        float effRadius = GetCalculatedArea(BaseAreaRadius);
        float effDuration = GetCalculatedDuration(1.2f);

        for (int i = 0; i < amount; i++)
        {
            Vector2 offset = Vector2.FromAngle(GD.Randf() * Mathf.Tau) * (float)GD.RandRange(50.0f, 220.0f);
            Vector2 spawnPos = Host.GlobalPosition + offset;
            var mine = new MacAssemblyMine
            {
                GlobalPosition = spawnPos,
                BlastRadius = effRadius,
                AssemblyTime = effDuration,
                HostRef = Host
            };
            Host.GetParent().AddChild(mine);
        }
    }

    private void DetonateMacRing(Vector2 center, float radius)
    {
        if (!HasValidHost())
            return;

        // Rupture flash: expanding shockwave ring + MAC particle burst + thump.
        var blast = new MacDetonationBlast
        {
            GlobalPosition = center,
            BlastRadius = radius
        };
        Host.GetParent().AddChild(blast);
        VfxManager.Instance?.Play(VfxType.MacRingBurst, center);
        AudioManager.Instance?.PlayEnemyDeath();
        CameraFollow.Instance?.AddTrauma(0.25f);

        // Osmotic swelling first (Cryo-EM lysis sequence), then engulfment.
        TargetingService.ForEachInRadius(center, radius, n =>
        {
            if (n is not BaseEnemy be || !GodotObject.IsInstanceValid(be))
                return;
            var swell = be.CreateTween();
            swell.TweenProperty(be, "scale", be.Scale * 1.3f, 0.12f);
            swell.TweenCallback(Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(be) && !be.IsBeingEaten)
                    be.BeEngulfed(Host);
            }));
        });
    }

    /// <summary>
    /// Ground-deposited MAC assembly site. C9 subunits visibly join the
    /// ring pore during the fuse, telegraphing the blast radius.
    /// </summary>
    public partial class MacAssemblyMine : Node2D
    {
        public float BlastRadius { get; set; } = 80.0f;
        public float AssemblyTime { get; set; } = 1.2f;
        public Node2D? HostRef { get; set; }

        private const int Subunits = 12;
        private float _age = 0.0f;
        private bool _detonated = false;

        public override void _Ready()
        {
            ZIndex = 6;
        }

        public override void _Process(double delta)
        {
            _age += (float)delta;
            QueueRedraw();

            if (_age >= AssemblyTime && !_detonated)
            {
                _detonated = true;
                if (HostRef is not null && GodotObject.IsInstanceValid(HostRef) &&
                    HostRef.GetParent() != null && HasValidSkill())
                {
                    _skill!.DetonateMacRing(GlobalPosition, BlastRadius);
                }
                QueueFree();
            }
        }

        private ComplementCascadeSkill? _skill;
        private bool HasValidSkill()
        {
            // The mine detonates through its owning skill so damage routing
            // stays in one place.
            _skill = FindSkill();
            return _skill != null;
        }

        private ComplementCascadeSkill? FindSkill()
        {
            if (HostRef == null || !GodotObject.IsInstanceValid(HostRef))
                return null;
            var sm = HostRef.GetNodeOrNull<SkillManager>("SkillManager");
            if (sm == null)
                return null;
            foreach (var s in sm.ActiveSlots)
            {
                if (s is ComplementCascadeSkill c && GodotObject.IsInstanceValid(c))
                    return c;
            }
            return null;
        }

        public override void _Draw()
        {
            float progress = Mathf.Clamp(_age / Mathf.Max(0.05f, AssemblyTime), 0.0f, 1.0f);
            float pulse = 1.0f + 0.08f * Mathf.Sin(_age * 10.0f);
            float ringR = 22.0f * pulse;

            // Blast-radius telegraph.
            DrawCircle(Vector2.Zero, BlastRadius, new Color(1.0f, 0.75f, 0.3f, 0.07f));
            DrawArc(Vector2.Zero, BlastRadius, 0.0f, Mathf.Tau, 48, new Color(1.0f, 0.8f, 0.4f, 0.22f), 1.5f);

            // Hollow transmembrane pore.
            DrawCircle(Vector2.Zero, 7.0f, new Color(0.05f, 0.02f, 0.08f, 0.85f));
            DrawArc(Vector2.Zero, 7.0f, 0.0f, Mathf.Tau, 24, new Color(1.0f, 0.9f, 0.6f, 0.5f), 1.5f);

            // Polymerizing C9 ring: swept arc + joined subunit dots.
            Color ringColor = new Color(1.0f, 0.82f, 0.35f, 0.95f);
            DrawArc(Vector2.Zero, ringR, -Mathf.Pi * 0.5f, -Mathf.Pi * 0.5f + progress * Mathf.Tau, 48, ringColor, 3.5f);
            int joined = Mathf.FloorToInt(progress * Subunits);
            for (int i = 0; i < Subunits; i++)
            {
                float a = i * Mathf.Tau / Subunits - Mathf.Pi * 0.5f;
                Vector2 p = Vector2.FromAngle(a) * ringR;
                if (i < joined)
                    DrawCircle(p, 3.2f, ringColor);
                else
                    DrawArc(p, 3.2f, 0.0f, Mathf.Tau, 12, new Color(1.0f, 0.82f, 0.35f, 0.3f), 1.0f);
            }
        }
    }

    /// <summary>Expanding rupture shockwave ring, fading over 0.35s.</summary>
    public partial class MacDetonationBlast : Node2D
    {
        public float BlastRadius { get; set; } = 80.0f;

        private const float Duration = 0.35f;
        private float _age = 0.0f;

        public override void _Ready()
        {
            ZIndex = 7;
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
            float t = Mathf.Clamp(_age / Duration, 0.0f, 1.0f);
            float r = BlastRadius * (0.2f + 0.8f * t);
            float a = 1.0f - t;
            DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 48, new Color(1.0f, 0.9f, 0.55f, a * 0.9f), 4.0f * (1.0f - t * 0.5f));
            DrawCircle(Vector2.Zero, r * 0.55f, new Color(1.0f, 0.85f, 0.4f, a * 0.35f));
        }
    }
}
