using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Gear Trait: Cytokine Longevity (細胞因子半衰期延展)
/// Universal Stat Modifiers: Duration +15%, Knockback +10% per level
/// </summary>
public partial class PassiveCytokineLongevity : BaseSkill
{
    public const float DurationPerLevel = 0.15f;
    public const float KnockbackPerLevel = 0.10f;

    public PassiveCytokineLongevity()
    {
        SkillId = SkillIds.PassiveLongevity;
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
        ApplyStat("duration", 0.0f, DurationPerLevel * Level);
        ApplyStat("knockback", 0.0f, KnockbackPerLevel * Level);
    }

    public override void RemovePassiveModifiers()
    {
        RemoveStat("duration", 0.0f, DurationPerLevel * Level);
        RemoveStat("knockback", 0.0f, KnockbackPerLevel * Level);
    }
}
