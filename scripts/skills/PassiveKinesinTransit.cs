using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Gear Trait: Kinesin Rapid-Transit (微管驅動蛋白軌道)
/// Universal Stat Modifiers: Projectile Speed +15% per level, Pierce +1
/// </summary>
public partial class PassiveKinesinTransit : BaseSkill
{
    public const float SpeedPerLevel = 0.15f;

    public PassiveKinesinTransit()
    {
        SkillId = SkillIds.PassiveKinesin;
        NameKey = "SKILL_KINESIN_NAME";
        DescKey = "SKILL_KINESIN_DESC";
        BioKey = "SKILL_KINESIN_BIO";
        IconSymbol = "🛤️";
        IsInnate = false;
        IsPassive = true;
        Cooldown = 0.0f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void ApplyPassiveModifiers()
    {
        ApplyStat("projectile_speed", 0.0f, SpeedPerLevel * Level);
        ApplyStat("pierce", 1.0f, 0.0f);
    }

    public override void RemovePassiveModifiers()
    {
        RemoveStat("projectile_speed", 0.0f, SpeedPerLevel * Level);
        RemoveStat("pierce", 1.0f, 0.0f);
    }
}
