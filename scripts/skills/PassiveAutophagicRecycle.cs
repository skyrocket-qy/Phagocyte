using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: Autophagic Recycle (自噬體修復再生)
/// Universal Stat Modifiers: Health Regen +0.4 HP/s per level
/// </summary>
public partial class PassiveAutophagicRecycle : BaseSkill
{
    public const float RegenPerLevel = 0.4f;

    public PassiveAutophagicRecycle()
    {
        SkillId = SkillIds.PassiveAutophagy;
        NameKey = "SKILL_AUTOPHAGY_NAME";
        DescKey = "SKILL_AUTOPHAGY_DESC";
        BioKey = "SKILL_AUTOPHAGY_BIO";
        IconSymbol = "🔄";
        IsInnate = false;
        IsPassive = true;
        Cooldown = 0.0f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void ApplyPassiveModifiers()
    {
        ApplyStat("health_regen", RegenPerLevel * Level, 0.0f);
    }

    public override void RemovePassiveModifiers()
    {
        RemoveStat("health_regen", RegenPerLevel * Level, 0.0f);
    }
}
