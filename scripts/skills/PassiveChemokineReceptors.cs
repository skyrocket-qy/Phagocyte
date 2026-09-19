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
        SkillId = SkillIds.PassiveChemokine;
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
        float bonusMag = MagnetPerLevel * Level;
        float bonusSpeed = SpeedPerLevel * Level;
        ApplyStat("magnet", 0.0f, bonusMag);
        ApplyStat("move_speed", 0.0f, bonusSpeed);
    }

    public override void RemovePassiveModifiers()
    {
        float bonusMag = MagnetPerLevel * Level;
        float bonusSpeed = SpeedPerLevel * Level;
        RemoveStat("magnet", 0.0f, bonusMag);
        RemoveStat("move_speed", 0.0f, bonusSpeed);
    }
}
