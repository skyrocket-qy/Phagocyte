using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Lysozyme Ricochet (溶菌酶彈射胞)
/// Fires bouncing hydrolytic enzyme vesicles ricocheting between enemies with armor shred.
/// </summary>
public partial class LysozymeRicochetSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 24.0f;
    [Export] public float BaseSpeed { get; set; } = 520.0f;
    [Export] public int BaseBounces { get; set; } = 4;

    public LysozymeRicochetSkill()
    {
        SkillId = SkillIds.LysozymeRicochet;
        NameKey = "SKILL_LYSOZYME_NAME";
        DescKey = "SKILL_LYSOZYME_DESC";
        BioKey = "SKILL_LYSOZYME_BIO";
        IconSymbol = "🧪";
        IsInnate = false;
        IsPassive = false;
        Cooldown = 3.8f;
        CooldownTimer = 1.2f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        int amount = GetCalculatedAmount(1);
        int bounces = GetCalculatedPierce(BaseBounces);
        float speed = GetCalculatedSpeed(BaseSpeed);
        float baseDmg = GetBaseDamageForLevel(BaseDamage);
        GetDamage(baseDmg, out float dmg, out _);

        AudioManager.Instance?.PlaySfx("cold_snap");

        for (int i = 0; i < amount; i++)
        {
            float angle = (i - (amount - 1) / 2.0f) * 0.35f;
            Vector2 dir = Vector2.Right.Rotated(GD.Randf() * Mathf.Tau + angle);

            var vesicle = new RicochetVesicle
            {
                GlobalPosition = Host.GlobalPosition,
                Direction = dir,
                Speed = speed,
                MaxBounces = bounces,
                Damage = dmg,
                HostRef = Host
            };
            Host.GetParent().AddChild(vesicle);
        }
    }

    public partial class RicochetVesicle : Node2D
    {
        public Vector2 Direction { get; set; } = Vector2.Right;
        public float Speed { get; set; } = 520.0f;
        public int MaxBounces { get; set; } = 4;
        public float Damage { get; set; } = 24.0f;
        public CharacterBody2D? HostRef { get; set; }

        private int _currentBounces = 0;
        private float _lifetime = 3.0f;
        private readonly HashSet<ulong> _recentlyHit = new();
        private readonly List<BaseEnemy> _scratchHits = new();

        public override void _Process(double delta)
        {
            float dt = (float)delta;
            _lifetime -= dt;
            if (_lifetime <= 0.0f || _currentBounces >= MaxBounces)
            {
                QueueFree();
                return;
            }

            GlobalPosition += Direction * Speed * dt;
            QueueRedraw();
            CheckHit();
        }

        private void CheckHit()
        {
            if (HostRef == null || _currentBounces >= MaxBounces)
                return;

            _scratchHits.Clear();
            TargetingService.CollectInRadius(GlobalPosition, 22.0f, _scratchHits);
            foreach (var n in _scratchHits)
            {
                if (_recentlyHit.Contains(n.GetInstanceId()))
                    continue;

                _recentlyHit.Add(n.GetInstanceId());
                _currentBounces++;

                CombatHelper.DealDamage(n, Damage, HostRef, false);

                // Find next bounce target
                Node2D? nextTarget = FindNextTarget(n);
                if (nextTarget != null)
                {
                    Direction = (nextTarget.GlobalPosition - GlobalPosition).Normalized();
                }
                else
                {
                    Direction = Direction.Rotated((float)GD.RandRange(1.8, 2.5));
                }
                break;
            }
        }

        private Node2D? FindNextTarget(Node2D current)
        {
            if (HostRef == null)
                return null;

            return TargetingService.FindNearest(
                this,
                350.0f,
                enemy => enemy != current && !_recentlyHit.Contains(enemy.GetInstanceId()));
        }

        public override void _Draw()
        {
            Color accent = SkillAssetPalette.Accent(SkillIds.LysozymeRicochet, new Color(0.11f, 0.44f, 0.86f));
            Color core = SkillAssetPalette.Core(SkillIds.LysozymeRicochet, Colors.White);

            Vector2 fwd = Direction.LengthSquared() > 0.001f ? Direction.Normalized() : Vector2.Right;
            Vector2 side = new Vector2(-fwd.Y, fwd.X);

            // Trailing enzymatic velocity wake
            Color trailAccent = new Color(accent.R, accent.G, accent.B, 0.45f);
            Color trailFade = new Color(accent.R, accent.G, accent.B, 0.0f);
            DrawLine(Vector2.Zero, -fwd * 22.0f, trailAccent, 3.0f);
            DrawLine(-fwd * 8.0f, -fwd * 34.0f, trailFade, 1.5f);

            // Globular enzymatic core and halo
            LaserGlow.DrawImpactHalo(this, Vector2.Zero, 14.0f, accent, core, 0.85f, 1.8f);
            DrawCircle(Vector2.Zero, 6.0f, core);

            // Catalytic cleft / lobes (bi-lobed globular structure)
            Vector2 lobeA = fwd * 3.0f + side * 4.5f;
            Vector2 lobeB = fwd * 3.0f - side * 4.5f;
            DrawCircle(lobeA, 3.5f, accent);
            DrawCircle(lobeB, 3.5f, accent);

            // Forward catalytic cleft focal pin
            DrawLine(Vector2.Zero, fwd * 8.0f, new Color(core.R, core.G, core.B, 0.95f), 2.0f);
        }
    }
}
