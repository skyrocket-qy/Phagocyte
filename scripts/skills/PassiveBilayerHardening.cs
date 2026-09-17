using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: Bilayer Hardening (磷脂雙分子層緻密化)
/// Universal Stat Modifiers: Max Health +15%, Armor +2 per level
/// </summary>
public partial class PassiveBilayerHardening : BaseSkill
{
    public const float HealthPerLevel = 0.15f;
    public const float ArmorPerLevel = 2.0f;

    public PassiveBilayerHardening()
    {
        SkillId = "passive_bilayer";
        NameKey = "SKILL_BILAYER_NAME";
        DescKey = "SKILL_BILAYER_DESC";
        BioKey = "SKILL_BILAYER_BIO";
        IconSymbol = "🛡️";
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
            cs.AddModifier("armor", ArmorPerLevel * Level, 0.0f);
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            Stats.Call("add_modifier", "max_health", 0.0f, HealthPerLevel * Level);
            Stats.Call("add_modifier", "armor", ArmorPerLevel * Level, 0.0f);
        }
    }

    public override void RemovePassiveModifiers()
    {
        if (Stats is CellStats cs)
        {
            cs.RemoveModifier("max_health", 0.0f, HealthPerLevel * Level);
            cs.RemoveModifier("armor", ArmorPerLevel * Level, 0.0f);
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            Stats.Call("remove_modifier", "max_health", 0.0f, HealthPerLevel * Level);
            Stats.Call("remove_modifier", "armor", ArmorPerLevel * Level, 0.0f);
        }
    }
}
