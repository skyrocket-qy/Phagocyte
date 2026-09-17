using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: Kinesin Rapid-Transit (微管驅動蛋白軌道)
/// Universal Stat Modifiers: Projectile Speed +15% per level, Pierce +1
/// </summary>
public partial class PassiveKinesinTransit : BaseSkill
{
    public const float SpeedPerLevel = 0.15f;

    public PassiveKinesinTransit()
    {
        SkillId = "passive_kinesin";
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
        if (Stats is CellStats cs)
        {
            cs.AddModifier("projectile_speed", 0.0f, SpeedPerLevel * Level);
            cs.AddModifier("pierce", 1.0f, 0.0f);
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            Stats.Call("add_modifier", "projectile_speed", 0.0f, SpeedPerLevel * Level);
            Stats.Call("add_modifier", "pierce", 1.0f, 0.0f);
        }
    }

    public override void RemovePassiveModifiers()
    {
        if (Stats is CellStats cs)
        {
            cs.RemoveModifier("projectile_speed", 0.0f, SpeedPerLevel * Level);
            cs.RemoveModifier("pierce", 1.0f, 0.0f);
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            Stats.Call("remove_modifier", "projectile_speed", 0.0f, SpeedPerLevel * Level);
            Stats.Call("remove_modifier", "pierce", 1.0f, 0.0f);
        }
    }
}
