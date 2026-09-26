using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Gear Trait: Actin Polymerization (肌動蛋白微絲聚合)
/// Universal Stat Modifiers per level: Area +12%, Move Speed +6%
/// </summary>
public partial class PassiveActinPolymerization : BaseSkill
{
    public const float AreaPerLevel = 0.12f;
    public const float SpeedPerLevel = 0.06f;

    public PassiveActinPolymerization()
    {
        SkillId = SkillIds.PassiveActin;
        NameKey = "SKILL_ACTIN_NAME";
        DescKey = "SKILL_ACTIN_DESC";
        BioKey = "SKILL_ACTIN_BIO";
        IconSymbol = "🧬";
        IsInnate = false;
        IsPassive = true;
        Cooldown = 0.0f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void ApplyPassiveModifiers()
    {
        float bonusArea = AreaPerLevel * Level;
        float bonusSpeed = SpeedPerLevel * Level;
        ApplyStat("area", 0.0f, bonusArea);
        ApplyStat("move_speed", 0.0f, bonusSpeed);
    }

    public override void RemovePassiveModifiers()
    {
        float bonusArea = AreaPerLevel * Level;
        float bonusSpeed = SpeedPerLevel * Level;
        RemoveStat("area", 0.0f, bonusArea);
        RemoveStat("move_speed", 0.0f, bonusSpeed);
    }
}
