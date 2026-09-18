using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: Aerobic Glycolysis (瓦伯格有氧糖酵解)
/// Universal Stat Modifiers: Move Speed +6%, Might +5% per level
/// </summary>
public partial class PassiveAerobicGlycolysis : BaseSkill
{
    public const float SpeedPerLevel = 0.06f;
    public const float MightPerLevel = 0.05f;

    public PassiveAerobicGlycolysis()
    {
        SkillId = "passive_glycolysis";
        NameKey = "SKILL_GLYCOLYSIS_NAME";
        DescKey = "SKILL_GLYCOLYSIS_DESC";
        BioKey = "SKILL_GLYCOLYSIS_BIO";
        IconSymbol = "🍬";
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
            cs.AddModifier("move_speed", 0.0f, SpeedPerLevel * Level);
            cs.AddModifier("might", 0.0f, MightPerLevel * Level);
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            Stats.Call("add_modifier", "move_speed", 0.0f, SpeedPerLevel * Level);
            Stats.Call("add_modifier", "might", 0.0f, MightPerLevel * Level);
        }
    }

    public override void RemovePassiveModifiers()
    {
        if (Stats is CellStats cs)
        {
            cs.RemoveModifier("move_speed", 0.0f, SpeedPerLevel * Level);
            cs.RemoveModifier("might", 0.0f, MightPerLevel * Level);
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            Stats.Call("remove_modifier", "move_speed", 0.0f, SpeedPerLevel * Level);
            Stats.Call("remove_modifier", "might", 0.0f, MightPerLevel * Level);
        }
    }
}
