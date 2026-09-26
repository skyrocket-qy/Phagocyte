using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Antibody Salvo (Y型抗體齊射)
/// Fires auto-homing Y-antibodies that track and neutralize nearby pathogens.
/// </summary>
public partial class AntibodySalvoSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 18.0f;
    [Export] public int BaseMissileCount { get; set; } = 3;
    [Export] public float SearchRange { get; set; } = 600.0f;
    [Export] public float BaseMissileSpeed { get; set; } = 420.0f;

    public AntibodySalvoSkill()
    {
        SkillId = SkillIds.AntibodySalvo;
        NameKey = "SKILL_ANTIBODY_NAME";
        DescKey = "SKILL_ANTIBODY_DESC";
        BioKey = "SKILL_ANTIBODY_BIO";
        IconSymbol = "🏹";
        IsInnate = true;
        IsPassive = false;
        Cooldown = 3.5f;
        CooldownTimer = 2.0f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        int amount = GetCalculatedAmount(BaseMissileCount);
        var pathogens = GetNearbyPathogens();

        if (amount > 0)
        {
            AudioManager.Instance?.PlaySfx("split_arrow_fire");
        }

        for (int i = 0; i < amount; i++)
        {
            Node2D? target = null;
            if (pathogens.Count > 0)
            {
                target = pathogens[i % pathogens.Count];
            }
            FireAntibody(target, i, amount);
        }
    }

    private List<BaseEnemy> GetNearbyPathogens()
    {
        var result = new List<BaseEnemy>();
        if (Host == null)
            return result;

        TargetingService.CollectInRadius(Host.GlobalPosition, SearchRange, result);
        return result;
    }

    private void FireAntibody(Node2D? target, int index, int total)
    {
        if (!HasValidHost())
            return;

        // Staggered launch cadence preserved; the missile itself now
        // carries the visual flight + opsonization bind.
        if (target != null && GodotObject.IsInstanceValid(target))
        {
            var tree = Host.GetTree();
            if (tree != null)
            {
                var timer = tree.CreateTimer(0.2f + index * 0.05f);
                timer.Timeout += () =>
                {
                    if (!HasValidHost())
                        return;
                    if (target != null && GodotObject.IsInstanceValid(target))
                    {
                        float baseDmg = GetBaseDamageForLevel(BaseDamage);
                        GetDamage(baseDmg, out float dmg, out bool isCrit);
                        float speed = GetCalculatedSpeed(BaseMissileSpeed);

                        var missile = new AntibodyMissile
                        {
                            Damage = dmg,
                            IsCrit = isCrit,
                            Speed = speed,
                            HostRef = Host
                        };
                        Host.GetParent().AddChild(missile);
                        float spread = (index - (total - 1) / 2.0f) * 0.35f;
                        Vector2 dir = (target.GlobalPosition - Host.GlobalPosition).Normalized().Rotated(spread);
                        missile.Launch(target, Host.GlobalPosition, dir);
                    }
                };
            }
        }
    }
}
