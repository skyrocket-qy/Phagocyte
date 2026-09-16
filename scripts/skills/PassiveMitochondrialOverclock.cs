using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: Mitochondrial Overclock (線粒體超頻)
/// Universal Stat Modifiers per level: Cooldown Reduction +8% (flat), Duration +10%
/// </summary>
public partial class PassiveMitochondrialOverclock : BaseSkill
{
    public const float CdrPerLevel = 0.08f;
    public const float DurationPerLevel = 0.10f;

    public PassiveMitochondrialOverclock()
    {
        SkillId = "passive_mitochondria";
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
        if (Stats is CellStats cs)
        {
            float bonusCdr = CdrPerLevel * Level;
            float bonusDuration = DurationPerLevel * Level;
            cs.AddModifier("cooldown_reduction", bonusCdr, 0.0f);
            cs.AddModifier("duration", 0.0f, bonusDuration);
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            float bonusCdr = CdrPerLevel * Level;
            float bonusDuration = DurationPerLevel * Level;
            Stats.Call("add_modifier", "cooldown_reduction", bonusCdr, 0.0f);
            Stats.Call("add_modifier", "duration", 0.0f, bonusDuration);
        }
    }

    public override void RemovePassiveModifiers()
    {
        if (Stats is CellStats cs)
        {
            float bonusCdr = CdrPerLevel * Level;
            float bonusDuration = DurationPerLevel * Level;
            cs.RemoveModifier("cooldown_reduction", bonusCdr, 0.0f);
            cs.RemoveModifier("duration", 0.0f, bonusDuration);
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            float bonusCdr = CdrPerLevel * Level;
            float bonusDuration = DurationPerLevel * Level;
            Stats.Call("remove_modifier", "cooldown_reduction", bonusCdr, 0.0f);
            Stats.Call("remove_modifier", "duration", 0.0f, bonusDuration);
        }
    }
}
