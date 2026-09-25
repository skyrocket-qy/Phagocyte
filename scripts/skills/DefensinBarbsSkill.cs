using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Defensin Barbs (防禦素刺棘)
/// 360-degree omni-directional radial burst piercing bacterial membranes.
/// </summary>
public partial class DefensinBarbsSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 22.0f;
    [Export] public float BaseSpeed { get; set; } = 460.0f;
    [Export] public int BaseCount { get; set; } = 8;
    [Export] public int BasePierce { get; set; } = 2;

    public DefensinBarbsSkill()
    {
        SkillId = SkillIds.DefensinBarbs;
        NameKey = "SKILL_DEFENSIN_NAME";
        DescKey = "SKILL_DEFENSIN_DESC";
        BioKey = "SKILL_DEFENSIN_BIO";
        IconSymbol = "🛡️";
        IsInnate = false;
        IsPassive = false;
        Cooldown = 3.0f;
        CooldownTimer = 1.0f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        int count = GetCalculatedAmount(BaseCount);
        int pierce = GetCalculatedPierce(BasePierce);
        float speed = GetCalculatedSpeed(BaseSpeed);
        GetDamage(BaseDamage, out float dmg, out bool isCrit);

        if (Phagocyte.Combat.ProjectileManager.Instance != null)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * (Mathf.Tau / count);
                Vector2 dir = Vector2.FromAngle(angle);
                Phagocyte.Combat.ProjectileManager.Instance.Spawn(
                    Host.GlobalPosition,
                    dir,
                    speed,
                    dmg,
                    isCrit,
                    pierce,
                    lifetime: 1.6f,
                    radius: 12.0f,
                    projType: "defensin_barb"
                );
            }
            return;
        }

        for (int i = 0; i < count; i++)
        {
            float angle = i * (Mathf.Tau / count);
            Vector2 dir = Vector2.FromAngle(angle);

            var barb = new BarbProjectile
            {
                GlobalPosition = Host.GlobalPosition,
                Direction = dir,
                Speed = speed,
                PierceLimit = pierce,
                Damage = dmg,
                HostRef = Host
            };
            Host.GetParent().AddChild(barb);
        }
    }

    public partial class BarbProjectile : Node2D
    {
        public Vector2 Direction { get; set; } = Vector2.Right;
        public float Speed { get; set; } = 460.0f;
        public int PierceLimit { get; set; } = 2;
        public float Damage { get; set; } = 22.0f;
        public CharacterBody2D? HostRef { get; set; }

        private int _hitCount = 0;
        private float _lifetime = 1.6f;
        private readonly List<BaseEnemy> _scratchHits = new();

        public override void _Process(double delta)
        {
            float dt = (float)delta;
            _lifetime -= dt;
            if (_lifetime <= 0.0f || _hitCount >= PierceLimit)
            {
                QueueFree();
                return;
            }

            GlobalPosition += Direction * Speed * dt;
            Rotation = Direction.Angle();
            QueueRedraw();
            CheckHit();
        }

        private void CheckHit()
        {
            if (HostRef == null || _hitCount >= PierceLimit)
                return;

            _scratchHits.Clear();
            TargetingService.CollectInRadius(GlobalPosition, 20.0f, _scratchHits);
            foreach (var n in _scratchHits)
            {
                if (_hitCount >= PierceLimit)
                    break;

                _hitCount++;
                CombatHelper.DealDamage(n, Damage, HostRef, false);

                if (_hitCount >= PierceLimit)
                {
                    QueueFree();
                    break;
                }
            }
        }

        public override void _Draw()
        {
            Color accent = SkillAssetPalette.Accent(SkillIds.DefensinBarbs, new Color(0.30f, 0.91f, 0.57f));
            Color core = SkillAssetPalette.Core(SkillIds.DefensinBarbs, new Color(0.85f, 1.0f, 0.92f));

            // Crystalline peptide needle shaft
            DrawLine(new Vector2(-12, 0), new Vector2(10, 0), accent, 3.5f);
            DrawLine(new Vector2(-10, 0), new Vector2(12, 0), core, 1.8f);

            // Diamond piercing needle tip
            Vector2[] tipPolygon = new Vector2[]
            {
                new Vector2(16, 0),
                new Vector2(8, -3.5f),
                new Vector2(6, 0),
                new Vector2(8, 3.5f)
            };
            DrawColoredPolygon(tipPolygon, core);

            // Backward-angled cationic barb thorns
            DrawLine(new Vector2(0, 0), new Vector2(-4, -4.5f), accent, 2.0f);
            DrawLine(new Vector2(0, 0), new Vector2(-4, 4.5f), accent, 2.0f);

            // Glowing needle tip
            LaserGlow.DrawImpactHalo(this, new Vector2(14, 0), 6.0f, accent, core, 0.85f, 1.5f);
        }
    }
}
