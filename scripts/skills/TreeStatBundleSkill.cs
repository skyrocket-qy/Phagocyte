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
            var (flat, pct) = PassiveTreeManager.SplitModifier(modifier);
            ApplyStat(modifier.Stat, flat, pct);
        }
    }

    public override void RemovePassiveModifiers()
    {
        foreach (var modifier in _modifiers)
        {
            var (flat, pct) = PassiveTreeManager.SplitModifier(modifier);
            RemoveStat(modifier.Stat, flat, pct);
        }
    }
}
