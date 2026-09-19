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
        SkillId = SkillIds.PassiveOpsonin;
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
        float bonusCrit = CritPerLevel * Level;
        float bonusCritDmg = CritDmgPerLevel * Level;
        ApplyStat("crit_chance", bonusCrit, 0.0f);
        ApplyStat("crit_damage", 0.0f, bonusCritDmg);
    }

    public override void RemovePassiveModifiers()
    {
        float bonusCrit = CritPerLevel * Level;
        float bonusCritDmg = CritDmgPerLevel * Level;
        RemoveStat("crit_chance", bonusCrit, 0.0f);
        RemoveStat("crit_damage", 0.0f, bonusCritDmg);
    }
}
