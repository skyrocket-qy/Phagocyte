using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Gear Trait: Lysosome Priming (溶酶體酵素活化)
/// Universal Stat Modifiers per level: Might +10%, Health Regen +0.6 HP/s
/// </summary>
public partial class PassiveLysosomePriming : BaseSkill
{
    public const float MightPerLevel = 0.10f;
    public const float RegenPerLevel = 0.6f;

    public PassiveLysosomePriming()
    {
        SkillId = SkillIds.PassiveLysosome;
        NameKey = "TREE_NODE_LYSOSOME_NAME";
        DescKey = "TREE_NODE_LYSOSOME_DESC";
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
        float bonusMight = MightPerLevel * Level;
        float bonusRegen = RegenPerLevel * Level;
        ApplyStat("might", 0.0f, bonusMight);
        ApplyStat("health_regen", bonusRegen, 0.0f);
    }

    public override void RemovePassiveModifiers()
    {
        float bonusMight = MightPerLevel * Level;
        float bonusRegen = RegenPerLevel * Level;
        RemoveStat("might", 0.0f, bonusMight);
        RemoveStat("health_regen", bonusRegen, 0.0f);
    }
}
