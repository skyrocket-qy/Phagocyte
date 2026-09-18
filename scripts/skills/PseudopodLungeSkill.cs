using Godot;
using System;
using Phagocyte.Enemies;

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
        SkillId = "pseudopod_lunge";
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
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;

        int amount = GetCalculatedAmount(1);
        float reach = GetCalculatedArea(BaseReach);
        var pathogens = Host.GetTree().GetNodesInGroup("pathogens");

        int pulled = 0;
        foreach (var p in pathogens)
        {
            if (pulled >= amount)
                break;

            if (p is Node2D n && GodotObject.IsInstanceValid(n))
            {
                var eaten = n.Get("is_being_eaten");
                if (eaten.VariantType == Variant.Type.Bool && (bool)eaten)
                    continue;

                if (Host.GlobalPosition.DistanceTo(n.GlobalPosition) <= reach)
                {
                    pulled++;

                    // Deal blunt impact damage
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
                }
            }
        }
    }
}
