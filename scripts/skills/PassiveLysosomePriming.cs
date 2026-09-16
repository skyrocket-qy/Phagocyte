using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: Lysosome Priming (溶酶體酵素活化)
/// Universal Stat Modifiers per level: Might +10%, Health Regen +0.6 HP/s
/// </summary>
public partial class PassiveLysosomePriming : BaseSkill
{
    public const float MightPerLevel = 0.10f;
    public const float RegenPerLevel = 0.6f;

    public PassiveLysosomePriming()
    {
        SkillId = "passive_lysosome";
        NameKey = "SKILL_LYSOSOME_NAME";
        DescKey = "SKILL_LYSOSOME_DESC";
        BioKey = "SKILL_LYSOSOME_BIO";
        IconSymbol = "🧪";
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
            float bonusMight = MightPerLevel * Level;
            float bonusRegen = RegenPerLevel * Level;
            cs.AddModifier("might", 0.0f, bonusMight);
            cs.AddModifier("health_regen", bonusRegen, 0.0f);
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            float bonusMight = MightPerLevel * Level;
            float bonusRegen = RegenPerLevel * Level;
            Stats.Call("add_modifier", "might", 0.0f, bonusMight);
            Stats.Call("add_modifier", "health_regen", bonusRegen, 0.0f);
        }
    }

    public override void RemovePassiveModifiers()
    {
        if (Stats is CellStats cs)
        {
            float bonusMight = MightPerLevel * Level;
            float bonusRegen = RegenPerLevel * Level;
            cs.RemoveModifier("might", 0.0f, bonusMight);
            cs.RemoveModifier("health_regen", bonusRegen, 0.0f);
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            float bonusMight = MightPerLevel * Level;
            float bonusRegen = RegenPerLevel * Level;
            Stats.Call("remove_modifier", "might", 0.0f, bonusMight);
            Stats.Call("remove_modifier", "health_regen", bonusRegen, 0.0f);
        }
    }
}
