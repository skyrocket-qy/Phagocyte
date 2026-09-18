using Godot;
using Godot.Collections;
using System;
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

    public AntibodySalvoSkill()
    {
        SkillId = "antibody_salvo";
        NameKey = "SKILL_ANTIBODY_NAME";
        DescKey = "SKILL_ANTIBODY_DESC";
        BioKey = "SKILL_ANTIBODY_BIO";
        IconSymbol = "🏹";
        IsInnate = false;
        IsPassive = false;
        Cooldown = 3.5f;
        CooldownTimer = 2.0f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;

        int amount = GetCalculatedAmount(BaseMissileCount);
        var pathogens = GetNearbyPathogens();

        if (amount > 0)
        {
            AudioManager.Instance?.PlayShoot();
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

    private Array<Node2D> GetNearbyPathogens()
    {
        var result = new Array<Node2D>();
        if (Host == null)
            return result;

        var allPathogens = Host.GetTree().GetNodesInGroup("pathogens");
        foreach (var p in allPathogens)
        {
            if (p is Node2D n && GodotObject.IsInstanceValid(n))
            {
                var eaten = n.Get("is_being_eaten");
                if (eaten.VariantType == Variant.Type.Bool && (bool)eaten)
                    continue;

                if (Host.GlobalPosition.DistanceTo(n.GlobalPosition) <= SearchRange)
                {
                    result.Add(n);
                }
            }
        }
        return result;
    }

    private void FireAntibody(Node2D? target, int index, int total)
    {
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;

        float angle = ((float)index / (float)Mathf.Max(1, total)) * Mathf.Tau;
        Vector2 spawnDir = Vector2.FromAngle(angle);
        Vector2 spawnPos = Host.GlobalPosition + spawnDir * 30.0f;

        // If target is valid, home in on it after short delay
        if (target != null && GodotObject.IsInstanceValid(target))
        {
            var tree = Host.GetTree();
            if (tree != null)
            {
                var timer = tree.CreateTimer(0.2f + index * 0.05f);
                timer.Timeout += () =>
                {
                    if (target != null && GodotObject.IsInstanceValid(target))
                    {
                        var eaten = target.Get("is_being_eaten");
                        if (eaten.VariantType == Variant.Type.Bool && (bool)eaten)
                            return;

                        var dmgDict = GetCalculatedDamage(BaseDamage);
                        float dmg = (float)dmgDict["damage"];
                        bool isCrit = (bool)dmgDict["is_crit"];

                        if (target is BaseEnemy be)
                        {
                            be.TakeDamage(dmg, Host, isCrit);
                        }
                        else if (target.HasMethod("take_damage"))
                        {
                            target.Call("take_damage", dmg, Host, isCrit);
                        }
                    }
                };
            }
        }
    }
}
