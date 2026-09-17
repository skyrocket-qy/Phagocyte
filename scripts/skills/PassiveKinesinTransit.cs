using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Passive Organelle Trait: Kinesin Rapid-Transit (微管驅動蛋白軌道)
/// Universal Stat Modifiers: Projectile Speed +15% per level, Pierce +1 at Lv.3 and Lv.5
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
        float extraPierce = (Level >= 5 ? 2.0f : (Level >= 3 ? 1.0f : 0.0f));
        if (Stats is CellStats cs)
        {
            cs.AddModifier("projectile_speed", 0.0f, SpeedPerLevel * Level);
            if (extraPierce > 0.0f)
                cs.AddModifier("pierce", extraPierce, 0.0f);
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            Stats.Call("add_modifier", "projectile_speed", 0.0f, SpeedPerLevel * Level);
            if (extraPierce > 0.0f)
                Stats.Call("add_modifier", "pierce", extraPierce, 0.0f);
        }
    }

    public override void RemovePassiveModifiers()
    {
        float extraPierce = (Level >= 5 ? 2.0f : (Level >= 3 ? 1.0f : 0.0f));
        if (Stats is CellStats cs)
        {
            cs.RemoveModifier("projectile_speed", 0.0f, SpeedPerLevel * Level);
            if (extraPierce > 0.0f)
                cs.RemoveModifier("pierce", extraPierce, 0.0f);
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            Stats.Call("remove_modifier", "projectile_speed", 0.0f, SpeedPerLevel * Level);
            if (extraPierce > 0.0f)
                Stats.Call("remove_modifier", "pierce", extraPierce, 0.0f);
        }
    }
}
