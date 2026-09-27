using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Applies one bundle of generic passive-tree stat modifiers.
/// </summary>
public partial class TreeStatBundleSkill : BaseSkill
{
    private readonly PassiveTreeManager.TreeStatModifier[] _modifiers;

    public TreeStatBundleSkill(PassiveTreeManager.TreeNode node)
    {
        SkillId = node.Id;
        IconSymbol = node.Icon;
        NameKey = node.NameKey;
        DescKey = node.DescKey;
        IsPassive = true;
        IsInnate = false;
        Cooldown = 0.0f;
        Level = 1;
        MaxLevel = 1;
        _modifiers = node.Modifiers;
    }

    public override void ApplyPassiveModifiers()
    {
        foreach (var modifier in _modifiers)
        {
            var (flat, pct, scalingStat, scalePer, hasScaling) = PassiveTreeManager.SplitScaledModifier(modifier);
            if (hasScaling && Stats is CellStats cs)
                cs.AddScaledModifier(modifier.Stat, flat, pct, scalingStat, scalePer);
            else
                ApplyStat(modifier.Stat, flat, pct);
        }
    }

    public override void RemovePassiveModifiers()
    {
        foreach (var modifier in _modifiers)
        {
            var (flat, pct, scalingStat, scalePer, hasScaling) = PassiveTreeManager.SplitScaledModifier(modifier);
            if (hasScaling && Stats is CellStats cs)
                cs.RemoveScaledModifier(modifier.Stat, flat, pct, scalingStat, scalePer);
            else
                RemoveStat(modifier.Stat, flat, pct);
        }
    }
}
