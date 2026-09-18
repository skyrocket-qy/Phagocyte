using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: Hematopoietic Reserve (造血幹細胞儲備)
/// Universal Stat Modifiers: Max HP +10% per level, Block +3% per level
/// </summary>
public partial class PassiveHematopoieticReserve : BaseSkill
{
    public const float HealthPerLevel = 0.10f;
    public const float BlockPerLevel = 0.03f;

    public PassiveHematopoieticReserve()
    {
        SkillId = "passive_hematopoietic";
        NameKey = "SKILL_HEMATOPOIETIC_NAME";
        DescKey = "SKILL_HEMATOPOIETIC_DESC";
        BioKey = "SKILL_HEMATOPOIETIC_BIO";
        IconSymbol = "🩸";
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
            cs.AddModifier("max_health", 0.0f, HealthPerLevel * Level);
            cs.AddModifier("block", BlockPerLevel * Level, 0.0f);
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            Stats.Call("add_modifier", "max_health", 0.0f, HealthPerLevel * Level);
            Stats.Call("add_modifier", "block", BlockPerLevel * Level, 0.0f);
        }
    }

    public override void RemovePassiveModifiers()
    {
        if (Stats is CellStats cs)
        {
            cs.RemoveModifier("max_health", 0.0f, HealthPerLevel * Level);
            cs.RemoveModifier("block", BlockPerLevel * Level, 0.0f);
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            Stats.Call("remove_modifier", "max_health", 0.0f, HealthPerLevel * Level);
            Stats.Call("remove_modifier", "block", BlockPerLevel * Level, 0.0f);
        }
    }
}
