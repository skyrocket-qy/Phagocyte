using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Gear Trait: Mitochondrial Overclock (線粒體超頻)
/// Universal Stat Modifiers per level: Cooldown Reduction +8% (flat), Duration +10%
/// </summary>
public partial class PassiveMitochondrialOverclock : BaseSkill
{
    public const float CdrPerLevel = 0.08f;
    public const float DurationPerLevel = 0.10f;

    public PassiveMitochondrialOverclock()
    {
        SkillId = SkillIds.PassiveMitochondria;
        NameKey = "SKILL_MITOCHONDRIA_NAME";
        DescKey = "SKILL_MITOCHONDRIA_DESC";
        BioKey = "SKILL_MITOCHONDRIA_BIO";
        IconSymbol = "⚡";
        IsInnate = false;
        IsPassive = true;
        Cooldown = 0.0f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void ApplyPassiveModifiers()
    {
        float bonusCdr = CdrPerLevel * Level;
        float bonusDuration = DurationPerLevel * Level;
        ApplyStat("cooldown_reduction", bonusCdr, 0.0f);
        ApplyStat("duration", 0.0f, bonusDuration);
    }

    public override void RemovePassiveModifiers()
    {
        float bonusCdr = CdrPerLevel * Level;
        float bonusDuration = DurationPerLevel * Level;
        RemoveStat("cooldown_reduction", bonusCdr, 0.0f);
        RemoveStat("duration", 0.0f, bonusDuration);
    }
}
