using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: Endotoxin Barrier (內毒素脫敏耐受)
/// Universal Stat Modifiers: Armor +3 flat, Knockback Resist +20% per level
/// </summary>
public partial class PassiveEndotoxinBarrier : BaseSkill
{
    public const float ArmorPerLevel = 3.0f;
    public const float KnockbackResistPerLevel = 0.20f;

    public PassiveEndotoxinBarrier()
    {
        SkillId = "passive_endotoxin";
        NameKey = "SKILL_ENDOTOXIN_NAME";
        DescKey = "SKILL_ENDOTOXIN_DESC";
        BioKey = "SKILL_ENDOTOXIN_BIO";
        IconSymbol = "🧱";
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
            cs.AddModifier("armor", ArmorPerLevel * Level, 0.0f);
            cs.AddModifier("knockback_resist", KnockbackResistPerLevel * Level, 0.0f);
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            Stats.Call("add_modifier", "armor", ArmorPerLevel * Level, 0.0f);
            Stats.Call("add_modifier", "knockback_resist", KnockbackResistPerLevel * Level, 0.0f);
        }
    }

    public override void RemovePassiveModifiers()
    {
        if (Stats is CellStats cs)
        {
            cs.RemoveModifier("armor", ArmorPerLevel * Level, 0.0f);
            cs.RemoveModifier("knockback_resist", KnockbackResistPerLevel * Level, 0.0f);
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            Stats.Call("remove_modifier", "armor", ArmorPerLevel * Level, 0.0f);
            Stats.Call("remove_modifier", "knockback_resist", KnockbackResistPerLevel * Level, 0.0f);
        }
    }
}
