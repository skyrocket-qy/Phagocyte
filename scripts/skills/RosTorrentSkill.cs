using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;

namespace Phagocyte.Skills;

public partial class RosTorrentSkill : BaseSkill
{
    private static PackedScene? _jetScene;
    public static PackedScene JetScene => _jetScene ??= GD.Load<PackedScene>("res://scenes/skills/ros_jet.tscn");

    [Export] public float BaseDamage { get; set; } = 25.0f;
    [Export] public float AttackRange { get; set; } = 650.0f;
    [Export] public float BaseJetSpeed { get; set; } = 520.0f;
    [Export] public float BaseJetLifetime { get; set; } = 0.9f;

    public RosTorrentSkill()
    {
        SkillId = SkillIds.RosTorrent;
        NameKey = "SKILL_ROS_NAME";
        DescKey = "SKILL_ROS_DESC";
        BioKey = "SKILL_ROS_BIO";
        IconSymbol = "💨";
        IsInnate = false;
        IsPassive = false;
        Cooldown = 3.2f;
        CooldownTimer = 1.0f; // start soon after spawn
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        AudioManager.Instance?.PlayShoot();

        Vector2 targetDir = FindTargetDirection();
        int amount = GetCalculatedAmount(1);
        float areaScale = GetCalculatedArea(1.0f);
        float jetSpeed = GetCalculatedSpeed(BaseJetSpeed);
        float jetLife = GetCalculatedDuration(BaseJetLifetime);

        if (amount <= 1)
        {
            FireJet(targetDir, areaScale, jetSpeed, jetLife);
        }
        else
        {
            // Fire fan spread of jets
            float spreadAngle = 0.22f; // radians
            float startAngle = -spreadAngle * ((amount - 1) / 2.0f);
            for (int i = 0; i < amount; i++)
            {
                float angle = startAngle + i * spreadAngle;
                Vector2 dir = targetDir.Rotated(angle);
                FireJet(dir, areaScale, jetSpeed, jetLife);
            }
        }
    }

    private Vector2 FindTargetDirection()
    {
        if (Host == null)
            return Vector2.Right;

        Vector2 fallback = Host.Velocity.Length() > 20.0f ? Host.Velocity.Normalized() : Vector2.Right;
        return TargetingService.FindTargetDirection(Host, AttackRange, fallback);
    }

    private void FireJet(Vector2 dir, float areaMult, float speedVal, float lifeVal)
    {
        if (Host == null)
            return;

        var jet = JetScene.Instantiate<Node2D>();
        Host.GetParent().AddChild(jet);

        GetDamage(BaseDamage, out float dmg, out bool isCrit);

        if (jet is RosJet rj)
        {
            rj.Setup(Host, Host.GlobalPosition, dir);
            rj.Speed = speedVal;
            rj.Lifetime = lifeVal;
            rj.Damage = dmg;
            rj.IsCrit = isCrit;
            rj.Scale = new Vector2(areaMult, areaMult);
        }
    }
}
