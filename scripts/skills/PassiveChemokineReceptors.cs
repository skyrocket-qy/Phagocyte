using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: Chemokine Receptors (趨化因子受體)
/// Universal Stat Modifiers per level: Magnet +25%, Move Speed +6%
/// </summary>
public partial class PassiveChemokineReceptors : BaseSkill
{
    public const float MagnetPerLevel = 0.25f;
    public const float SpeedPerLevel = 0.06f;

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
            float bonusSpeed = SpeedPerLevel * Level;
            cs.AddModifier("magnet", 0.0f, bonusMag);
            cs.AddModifier("move_speed", 0.0f, bonusSpeed);
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            float bonusMag = MagnetPerLevel * Level;
            float bonusSpeed = SpeedPerLevel * Level;
            Stats.Call("add_modifier", "magnet", 0.0f, bonusMag);
            Stats.Call("add_modifier", "move_speed", 0.0f, bonusSpeed);
        }
    }

    public override void RemovePassiveModifiers()
    {
        if (Stats is CellStats cs)
        {
            float bonusMag = MagnetPerLevel * Level;
            float bonusSpeed = SpeedPerLevel * Level;
            cs.RemoveModifier("magnet", 0.0f, bonusMag);
            cs.RemoveModifier("move_speed", 0.0f, bonusSpeed);
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            float bonusMag = MagnetPerLevel * Level;
            float bonusSpeed = SpeedPerLevel * Level;
            Stats.Call("remove_modifier", "magnet", 0.0f, bonusMag);
            Stats.Call("remove_modifier", "move_speed", 0.0f, bonusSpeed);
        }
    }
}
