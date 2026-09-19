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
        SkillId = SkillIds.PassiveHematopoietic;
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
        ApplyStat("max_health", 0.0f, HealthPerLevel * Level);
        ApplyStat("block", BlockPerLevel * Level, 0.0f);
    }

    public override void RemovePassiveModifiers()
    {
        RemoveStat("max_health", 0.0f, HealthPerLevel * Level);
        RemoveStat("block", BlockPerLevel * Level, 0.0f);
    }
}
