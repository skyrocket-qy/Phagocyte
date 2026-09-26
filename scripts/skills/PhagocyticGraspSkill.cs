using Godot;
using System.Collections.Generic;
using System.Linq;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;

namespace Phagocyte.Skills;

/// <summary>
/// Innate Active Weapon: Phagocytic Grasp (吞噬偽足)
/// Macrophage-exclusive innate. Extends amoeboid pseudopods to grab the
/// nearest pathogens and deal contact damage on arrival.
/// Chassis deformation itself stays in BaseCell for all cells; this skill
/// is the functional grasp, not the visual wobble.
/// </summary>
public partial class PhagocyticGraspSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 28.0f;
    [Export] public float BaseReach { get; set; } = 280.0f;
    [Export] public int BaseGraspCount { get; set; } = 2;

    public PhagocyticGraspSkill()
    {
        SkillId = SkillIds.PhagocyticGrasp;
        NameKey = "SKILL_GRASP_NAME";
        DescKey = "SKILL_GRASP_DESC";
        BioKey = "SKILL_GRASP_BIO";
        IconSymbol = "🦠";
        IsInnate = true;
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

        int graspCount = GetCalculatedAmount(BaseGraspCount);
        float reach = GetCalculatedArea(BaseReach);
        GetDamage(BaseDamage, out float dmg, out bool isCrit);

        var found = new List<BaseEnemy>();
        TargetingService.CollectInRadius(Host.GlobalPosition, reach, found);

        var targets = found
            .Where(e => GodotObject.IsInstanceValid(e))
            .OrderBy(e => Host.GlobalPosition.DistanceSquaredTo(e.GlobalPosition))
            .Take(graspCount)
            .ToList();

        if (targets.Count > 0)
        {
            AudioManager.Instance?.PlayShoot();
        }

        float areaScale = GetCalculatedArea(1.0f);

        foreach (var target in targets)
        {
            if (!GodotObject.IsInstanceValid(target))
                continue;

            // Chain-strike delivery (merged from the retired PseudopodLimb
            // gear): the chain visibly travels out, and damage + drag
            // start exactly on contact instead of instantly.
            var chain = new PseudopodChainVisual
            {
                GlobalPosition = Host.GlobalPosition,
                Host = Host,
                Target = target,
                BaseHalfWidth = 16.0f * areaScale,
                ExtendSpeed = 1250.0f,
                HoldDuration = 0.30f
            };
            chain.Arrived += (Node2D arrived) =>
            {
                if (arrived is not BaseEnemy enemy || !GodotObject.IsInstanceValid(enemy))
                    return;

                CombatHelper.DealDamage(enemy, dmg, Host, isCrit);
            };
            Host.GetParent().AddChild(chain);
        }
    }
}
