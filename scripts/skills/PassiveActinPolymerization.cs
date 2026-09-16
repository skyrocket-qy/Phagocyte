using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: Actin Polymerization (肌動蛋白微絲聚合)
/// Universal Stat Modifiers per level: Area +12%, Move Speed +6%
/// </summary>
public partial class PassiveActinPolymerization : BaseSkill
{
    public const float AreaPerLevel = 0.12f;
    public const float SpeedPerLevel = 0.06f;

    public PassiveActinPolymerization()
    {
        SkillId = "passive_actin";
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
        if (Stats is CellStats cs)
        {
            float bonusArea = AreaPerLevel * Level;
            float bonusSpeed = SpeedPerLevel * Level;
            cs.AddModifier("area", 0.0f, bonusArea);
            cs.AddModifier("move_speed", 0.0f, bonusSpeed);
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            float bonusArea = AreaPerLevel * Level;
            float bonusSpeed = SpeedPerLevel * Level;
            Stats.Call("add_modifier", "area", 0.0f, bonusArea);
            Stats.Call("add_modifier", "move_speed", 0.0f, bonusSpeed);
        }
    }

    public override void RemovePassiveModifiers()
    {
        if (Stats is CellStats cs)
        {
            float bonusArea = AreaPerLevel * Level;
            float bonusSpeed = SpeedPerLevel * Level;
            cs.RemoveModifier("area", 0.0f, bonusArea);
            cs.RemoveModifier("move_speed", 0.0f, bonusSpeed);
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            float bonusArea = AreaPerLevel * Level;
            float bonusSpeed = SpeedPerLevel * Level;
            Stats.Call("remove_modifier", "area", 0.0f, bonusArea);
            Stats.Call("remove_modifier", "move_speed", 0.0f, bonusSpeed);
        }
    }
}
