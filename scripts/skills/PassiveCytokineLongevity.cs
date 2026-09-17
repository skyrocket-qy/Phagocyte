using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: Cytokine Longevity (細胞因子半衰期延展)
/// Universal Stat Modifiers: Duration +15%, Knockback +10% per level
/// </summary>
public partial class PassiveCytokineLongevity : BaseSkill
{
    public const float DurationPerLevel = 0.15f;
    public const float KnockbackPerLevel = 0.10f;

    public PassiveCytokineLongevity()
    {
        SkillId = "passive_longevity";
        NameKey = "SKILL_LONGEVITY_NAME";
        DescKey = "SKILL_LONGEVITY_DESC";
        BioKey = "SKILL_LONGEVITY_BIO";
        IconSymbol = "⏳";
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
            cs.AddModifier("duration", 0.0f, DurationPerLevel * Level);
            cs.AddModifier("knockback", 0.0f, KnockbackPerLevel * Level);
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            Stats.Call("add_modifier", "duration", 0.0f, DurationPerLevel * Level);
            Stats.Call("add_modifier", "knockback", 0.0f, KnockbackPerLevel * Level);
        }
    }

    public override void RemovePassiveModifiers()
    {
        if (Stats is CellStats cs)
        {
            cs.RemoveModifier("duration", 0.0f, DurationPerLevel * Level);
            cs.RemoveModifier("knockback", 0.0f, KnockbackPerLevel * Level);
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            Stats.Call("remove_modifier", "duration", 0.0f, DurationPerLevel * Level);
            Stats.Call("remove_modifier", "knockback", 0.0f, KnockbackPerLevel * Level);
        }
    }
}
