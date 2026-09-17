using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Core;

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
        SkillId = "lysozyme_ricochet";
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
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;

        int amount = GetCalculatedAmount(1);
        int bounces = GetCalculatedPierce(BaseBounces);
        float speed = GetCalculatedSpeed(BaseSpeed);
        var dmgData = GetCalculatedDamage(BaseDamage);
        float dmg = (float)dmgData["damage"];

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
            if (HostRef == null)
                return;

            var pathogens = HostRef.GetTree().GetNodesInGroup("pathogens");
            foreach (var p in pathogens)
            {
                if (p is Node2D n && GodotObject.IsInstanceValid(n))
                {
                    if (_recentlyHit.Contains(n.GetInstanceId()))
                        continue;

                    if (GlobalPosition.DistanceTo(n.GlobalPosition) <= 22.0f)
                    {
                        _recentlyHit.Add(n.GetInstanceId());
                        _currentBounces++;

                        if (n.HasMethod("take_damage"))
                            n.Call("take_damage", Damage);
                        else if (n.HasMethod("be_engulfed"))
                            n.Call("be_engulfed", HostRef);

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
            }
        }

        private Node2D? FindNextTarget(Node2D current)
        {
            if (HostRef == null)
                return null;

            var pathogens = HostRef.GetTree().GetNodesInGroup("pathogens");
            Node2D? best = null;
            float minDist = 350.0f;

            foreach (var p in pathogens)
            {
                if (p is Node2D n && GodotObject.IsInstanceValid(n) && n != current)
                {
                    if (_recentlyHit.Contains(n.GetInstanceId()))
                        continue;

                    float d = GlobalPosition.DistanceTo(n.GlobalPosition);
                    if (d < minDist)
                    {
                        minDist = d;
                        best = n;
                    }
                }
            }
            return best;
        }

        public override void _Draw()
        {
            DrawCircle(Vector2.Zero, 9.0f, new Color(0.2f, 1.0f, 0.4f, 0.35f));
            DrawCircle(Vector2.Zero, 5.5f, new Color(0.6f, 1.0f, 0.8f, 0.95f));
        }
    }
}
