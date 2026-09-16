using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: Opsonin Affinity (調理素親和)
/// Universal Stat Modifiers per level: Crit Chance +5% (flat), Crit Damage +25%
/// </summary>
public partial class PassiveOpsoninAffinity : BaseSkill
{
    public const float CritPerLevel = 0.05f;
    public const float CritDmgPerLevel = 0.25f;

    public PassiveOpsoninAffinity()
    {
        SkillId = "passive_opsonin";
        NameKey = "SKILL_OPSONIN_NAME";
        DescKey = "SKILL_OPSONIN_DESC";
        BioKey = "SKILL_OPSONIN_BIO";
        IconSymbol = "🎯";
        IsInnate = false;
        IsPassive = true;
        Cooldown = 0.0f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void ApplyPassiveModifiers()
    {
        if (Stats is CellStats cs)
        {
            float bonusCrit = CritPerLevel * Level;
            float bonusCritDmg = CritDmgPerLevel * Level;
            cs.AddModifier("crit_chance", bonusCrit, 0.0f);
            cs.AddModifier("crit_damage", 0.0f, bonusCritDmg);
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            float bonusCrit = CritPerLevel * Level;
            float bonusCritDmg = CritDmgPerLevel * Level;
            Stats.Call("add_modifier", "crit_chance", bonusCrit, 0.0f);
            Stats.Call("add_modifier", "crit_damage", 0.0f, bonusCritDmg);
        }
    }

    public override void RemovePassiveModifiers()
    {
        if (Stats is CellStats cs)
        {
            float bonusCrit = CritPerLevel * Level;
            float bonusCritDmg = CritDmgPerLevel * Level;
            cs.RemoveModifier("crit_chance", bonusCrit, 0.0f);
            cs.RemoveModifier("crit_damage", 0.0f, bonusCritDmg);
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            float bonusCrit = CritPerLevel * Level;
            float bonusCritDmg = CritDmgPerLevel * Level;
            Stats.Call("remove_modifier", "crit_chance", bonusCrit, 0.0f);
            Stats.Call("remove_modifier", "crit_damage", 0.0f, bonusCritDmg);
        }
    }
}
