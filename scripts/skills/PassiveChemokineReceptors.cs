using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: Chemokine Receptors (趨化因子受體)
/// Universal Stat Modifiers per level: Magnet +15%, Luck +10%
/// </summary>
public partial class PassiveChemokineReceptors : BaseSkill
{
    public const float MagnetPerLevel = 0.15f;
    public const float LuckPerLevel = 0.10f;

    public PassiveChemokineReceptors()
    {
        SkillId = "passive_chemokine";
        NameKey = "SKILL_CHEMOKINE_NAME";
        DescKey = "SKILL_CHEMOKINE_DESC";
        BioKey = "SKILL_CHEMOKINE_BIO";
        IconSymbol = "🧲";
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
            float bonusMag = MagnetPerLevel * Level;
            float bonusLuck = LuckPerLevel * Level;
            cs.AddModifier("magnet", 0.0f, bonusMag);
            cs.AddModifier("luck", 0.0f, bonusLuck);
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            float bonusMag = MagnetPerLevel * Level;
            float bonusLuck = LuckPerLevel * Level;
            Stats.Call("add_modifier", "magnet", 0.0f, bonusMag);
            Stats.Call("add_modifier", "luck", 0.0f, bonusLuck);
        }
    }

    public override void RemovePassiveModifiers()
    {
        if (Stats is CellStats cs)
        {
            float bonusMag = MagnetPerLevel * Level;
            float bonusLuck = LuckPerLevel * Level;
            cs.RemoveModifier("magnet", 0.0f, bonusMag);
            cs.RemoveModifier("luck", 0.0f, bonusLuck);
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            float bonusMag = MagnetPerLevel * Level;
            float bonusLuck = LuckPerLevel * Level;
            Stats.Call("remove_modifier", "magnet", 0.0f, bonusMag);
            Stats.Call("remove_modifier", "luck", 0.0f, bonusLuck);
        }
    }
}
