using Godot;
using System;
using Phagocyte.Combat;
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
        SkillId = SkillIds.PerforinLance;
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
        if (!HasValidHost())
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

        Vector2 fallback = Host.Velocity.Length() > 20.0f ? Host.Velocity.Normalized() : Vector2.Right;
        return TargetingService.FindTargetDirection(Host, AttackRange, fallback);
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
                // Beam geometry is a capsule (not a radius), so this scan stays
                // group-based; only pathogens not mid-engulf are pierced.
                if (n is BaseEnemy be && be.IsBeingEaten)
                    continue;

                Vector2 pPos = n.GlobalPosition;
                Vector2 projPoint = Geometry2D.GetClosestPointToSegment(pPos, startPos, endPos);
                if (projPoint.DistanceTo(pPos) <= beamWidth)
                {
                    hitCount++;
                    GetDamage(BaseDamage, out float dmg, out bool isCrit);

                    CombatHelper.DealDamage(n, dmg, Host, isCrit);
                }
            }
        }
    }
}
