using Godot;
using System;
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
        SkillId = "ros_torrent";
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
        if (Host == null || !GodotObject.IsInstanceValid(Host))
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

        var pathogens = Host.GetTree().GetNodesInGroup("pathogens");
        Node2D? closestEnemy = null;
        float minDist = AttackRange;

        foreach (var p in pathogens)
        {
            if (p is Node2D n && GodotObject.IsInstanceValid(n))
            {
                var eaten = n.Get("is_being_eaten");
                if (eaten.VariantType == Variant.Type.Bool && (bool)eaten)
                    continue;

                float d = Host.GlobalPosition.DistanceTo(n.GlobalPosition);
                if (d < minDist)
                {
                    minDist = d;
                    closestEnemy = n;
                }
            }
        }

        if (closestEnemy != null)
        {
            return (closestEnemy.GlobalPosition - Host.GlobalPosition).Normalized();
        }

        // Fallback to current velocity or facing
        if (Host.Velocity.Length() > 20.0f)
        {
            return Host.Velocity.Normalized();
        }
        return Vector2.Right;
    }

    private void FireJet(Vector2 dir, float areaMult, float speedVal, float lifeVal)
    {
        if (Host == null)
            return;

        var jet = JetScene.Instantiate<Node2D>();
        Host.GetParent().AddChild(jet);

        var dmgDict = GetCalculatedDamage(BaseDamage);
        float dmg = (float)dmgDict["damage"];
        bool isCrit = (bool)dmgDict["is_crit"];

        if (jet is RosJet rj)
        {
            rj.Setup(Host, Host.GlobalPosition, dir);
            rj.Speed = speedVal;
            rj.Lifetime = lifeVal;
            rj.Damage = dmg;
            rj.IsCrit = isCrit;
            rj.Scale = new Vector2(areaMult, areaMult);
        }
        else
        {
            jet.Call("setup", Host, Host.GlobalPosition, dir);
            jet.Set("speed", speedVal);
            jet.Set("lifetime", lifeVal);
            jet.Set("damage", dmg);
            jet.Set("is_crit", isCrit);
            jet.Scale = new Vector2(areaMult, areaMult);
        }
    }
}
