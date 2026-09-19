using Godot;
using System;
using Phagocyte.Core;

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
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;

        int count = GetCalculatedAmount(BaseCount);
        int pierce = GetCalculatedPierce(BasePierce);
        float speed = GetCalculatedSpeed(BaseSpeed);
        var dmgData = GetCalculatedDamage(BaseDamage);
        float dmg = (float)dmgData["damage"];

        bool isCrit = (bool)dmgData["is_crit"];

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
            if (HostRef == null)
                return;

            var pathogens = HostRef.GetTree().GetNodesInGroup("pathogens");
            foreach (var p in pathogens)
            {
                if (p is Node2D n && GodotObject.IsInstanceValid(n))
                {
                    if (GlobalPosition.DistanceTo(n.GlobalPosition) <= 20.0f)
                    {
                        _hitCount++;
                        if (n.HasMethod("take_damage"))
                            n.Call("take_damage", Damage);
                        else if (n.HasMethod("be_engulfed"))
                            n.Call("be_engulfed", HostRef);

                        if (_hitCount >= PierceLimit)
                        {
                            QueueFree();
                            break;
                        }
                    }
                }
            }
        }

        public override void _Draw()
        {
            Color barbColor = new Color(1.0f, 0.85f, 0.2f, 0.95f);
            DrawLine(new Vector2(-8, 0), new Vector2(10, 0), barbColor, 2.5f);
            DrawCircle(new Vector2(10, 0), 2.5f, new Color(1.0f, 1.0f, 0.6f));
        }
    }
}
