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
        SkillId = SkillIds.PassiveBilayer;
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
        ApplyStat("max_health", 0.0f, HealthPerLevel * Level);
        ApplyStat("armor", ArmorPerLevel * Level, 0.0f);
    }

    public override void RemovePassiveModifiers()
    {
        RemoveStat("max_health", 0.0f, HealthPerLevel * Level);
        RemoveStat("armor", ArmorPerLevel * Level, 0.0f);
    }
}
