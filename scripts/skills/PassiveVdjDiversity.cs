using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: V(D)J Diversity (V(D)J 基因重排多樣性)
/// Universal Stat Modifiers: Crit Damage +15%, Crit Chance +3% per level
/// </summary>
public partial class PassiveVdjDiversity : BaseSkill
{
    public const float CritDamagePerLevel = 0.15f;
    public const float CritChancePerLevel = 0.03f;

    public PassiveVdjDiversity()
    {
        SkillId = "passive_vdj";
        NameKey = "SKILL_VDJ_NAME";
        DescKey = "SKILL_VDJ_DESC";
        BioKey = "SKILL_VDJ_BIO";
        IconSymbol = "🎲";
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
            cs.AddModifier("crit_damage", 0.0f, CritDamagePerLevel * Level);
            cs.AddModifier("crit_chance", CritChancePerLevel * Level, 0.0f);
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            Stats.Call("add_modifier", "crit_damage", 0.0f, CritDamagePerLevel * Level);
            Stats.Call("add_modifier", "crit_chance", CritChancePerLevel * Level, 0.0f);
        }
    }

    public override void RemovePassiveModifiers()
    {
        if (Stats is CellStats cs)
        {
            cs.RemoveModifier("crit_damage", 0.0f, CritDamagePerLevel * Level);
            cs.RemoveModifier("crit_chance", CritChancePerLevel * Level, 0.0f);
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            Stats.Call("remove_modifier", "crit_damage", 0.0f, CritDamagePerLevel * Level);
            Stats.Call("remove_modifier", "crit_chance", CritChancePerLevel * Level, 0.0f);
        }
    }
}
