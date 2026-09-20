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
/// nearest pathogens, deal contact damage and drag them back for
/// phagocytosis (wiki step a: phagosome formation).
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

            CombatHelper.DealDamage(target, dmg, Host, isCrit);

            // Lamellipodial sheet + phagocytic cup over the drag.
            var arm = new PseudopodArmVisual
            {
                GlobalPosition = Host.GlobalPosition,
                Host = Host,
                Target = target,
                BaseHalfWidth = 24.0f * areaScale
            };
            Host.GetParent().AddChild(arm);

            if (!GodotObject.IsInstanceValid(target))
                continue;
            if (target.IsBeingEaten)
                continue;

            // Drag back into the cell body, then attempt engulfment.
            // Non-engulfable foes (TB wax, anthrax shell, prions, bosses)
            // keep the damage and the pull, but BeEngulfed refuses them.
            var tween = Host.CreateTween();
            tween.TweenProperty(target, "global_position", Host.GlobalPosition, 0.15f);
            tween.TweenCallback(Callable.From(() =>
            {
                if (target != null && GodotObject.IsInstanceValid(target) && target.HasMethod("be_engulfed"))
                {
                    target.Call("be_engulfed", Host);
                }
            }));
        }
    }
}
