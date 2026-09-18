using Godot;
using System;
using Phagocyte.Core;
using Phagocyte.Enemies;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Perforin Lance (穿孔素長矛)
/// Straight piercing ray that punctures through a line of pathogens.
/// </summary>
public partial class PerforinLanceSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 35.0f;
    [Export] public float AttackRange { get; set; } = 700.0f;

    public PerforinLanceSkill()
    {
        SkillId = "perforin_lance";
        NameKey = "SKILL_PERFORIN_NAME";
        DescKey = "SKILL_PERFORIN_DESC";
        BioKey = "SKILL_PERFORIN_BIO";
        IconSymbol = "🗡️";
        IsInnate = true;
        IsPassive = false;
        Cooldown = 2.8f;
        CooldownTimer = 0.5f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;

        Vector2 targetDir = FindTargetDirection();
        int amount = GetCalculatedAmount(1);
        int pierceLimit = GetCalculatedPierce(3);

        if (amount > 0)
        {
            AudioManager.Instance?.PlayShoot();
        }

        for (int i = 0; i < amount; i++)
        {
            Vector2 dir = targetDir;
            if (amount > 1)
            {
                float angleOffset = (i - (amount - 1) / 2.0f) * 0.15f;
                dir = dir.Rotated(angleOffset);
            }
            ExecuteLanceStrike(dir, pierceLimit);
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

        if (Host.Velocity.Length() > 20.0f)
        {
            return Host.Velocity.Normalized();
        }
        return Vector2.Right;
    }

    private void ExecuteLanceStrike(Vector2 dir, int pierceLimit)
    {
        if (Host == null)
            return;

        Vector2 startPos = Host.GlobalPosition;
        Vector2 endPos = startPos + dir * AttackRange;
        var pathogens = Host.GetTree().GetNodesInGroup("pathogens");
        int hitCount = 0;
        float beamWidth = 24.0f * GetCalculatedArea(1.0f);

        foreach (var p in pathogens)
        {
            if (hitCount >= pierceLimit)
                break;

            if (p is Node2D n && GodotObject.IsInstanceValid(n))
            {
                var eaten = n.Get("is_being_eaten");
                if (eaten.VariantType == Variant.Type.Bool && (bool)eaten)
                    continue;

                Vector2 pPos = n.GlobalPosition;
                Vector2 projPoint = Geometry2D.GetClosestPointToSegment(pPos, startPos, endPos);
                if (projPoint.DistanceTo(pPos) <= beamWidth)
                {
                    hitCount++;
                    var dmgDict = GetCalculatedDamage(BaseDamage);
                    float dmg = (float)dmgDict["damage"];
                    bool isCrit = (bool)dmgDict["is_crit"];

                    if (n is BaseEnemy be)
                    {
                        be.TakeDamage(dmg, Host, isCrit);
                    }
                    else if (n.HasMethod("take_damage"))
                    {
                        n.Call("take_damage", dmg, Host, isCrit);
                    }
                }
            }
        }
    }
}
