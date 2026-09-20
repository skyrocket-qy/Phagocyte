using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Pseudopod Lunge (偽足猛擊)
/// Snaps out an elongated amoebic arm to pull enemies in for phagocytosis.
/// </summary>
public partial class PseudopodLungeSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 30.0f;
    [Export] public float BaseReach { get; set; } = 260.0f;

    public PseudopodLungeSkill()
    {
        SkillId = SkillIds.PseudopodLunge;
        NameKey = "SKILL_LUNGE_NAME";
        DescKey = "SKILL_LUNGE_DESC";
        BioKey = "SKILL_LUNGE_BIO";
        IconSymbol = "🥊";
        IsInnate = false;
        IsPassive = false;
        Cooldown = 3.0f;
        CooldownTimer = 2.5f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        int amount = GetCalculatedAmount(1);
        float reach = GetCalculatedArea(BaseReach);
        GetDamage(BaseDamage, out float dmg, out bool isCrit);

        int pulled = 0;
        TargetingService.ForEachInRadius(Host.GlobalPosition, reach, n =>
        {
            if (pulled >= amount)
                return;

            pulled++;

            CombatHelper.DealDamage(n, dmg, Host, isCrit);

            // Lamellipodial sheet + phagocytic cup over the drag.
            var arm = new PseudopodArmVisual
            {
                GlobalPosition = Host.GlobalPosition,
                Host = Host,
                Target = n,
                BaseHalfWidth = 24.0f * GetCalculatedArea(1.0f)
            };
            Host.GetParent().AddChild(arm);

            // Pull pathogen rapidly toward player
            var tween = Host.CreateTween();
            tween.TweenProperty(n, "global_position", Host.GlobalPosition, 0.15f);
            tween.TweenCallback(Callable.From(() =>
            {
                if (n != null && GodotObject.IsInstanceValid(n) && n.HasMethod("be_engulfed"))
                {
                    n.Call("be_engulfed", Host);
                }
            }));
        });
    }
}
