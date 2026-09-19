using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: Aerobic Glycolysis (瓦伯格有氧糖酵解)
/// Universal Stat Modifiers: Move Speed +6%, Might +5% per level
/// </summary>
public partial class PassiveAerobicGlycolysis : BaseSkill
{
    public const float SpeedPerLevel = 0.06f;
    public const float MightPerLevel = 0.05f;

    public PassiveAerobicGlycolysis()
    {
        SkillId = SkillIds.PassiveGlycolysis;
        NameKey = "SKILL_GLYCOLYSIS_NAME";
        DescKey = "SKILL_GLYCOLYSIS_DESC";
        BioKey = "SKILL_GLYCOLYSIS_BIO";
        IconSymbol = "🍬";
        IsInnate = false;
        IsPassive = true;
        Cooldown = 0.0f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void ApplyPassiveModifiers()
    {
        ApplyStat("move_speed", 0.0f, SpeedPerLevel * Level);
        ApplyStat("might", 0.0f, MightPerLevel * Level);
    }

    public override void RemovePassiveModifiers()
    {
        RemoveStat("move_speed", 0.0f, SpeedPerLevel * Level);
        RemoveStat("might", 0.0f, MightPerLevel * Level);
    }
}
