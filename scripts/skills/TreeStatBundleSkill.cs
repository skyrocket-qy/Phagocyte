using Godot;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Applies one stack-scaled bundle of generic passive-tree stat modifiers.
/// </summary>
public partial class TreeStatBundleSkill : BaseSkill
{
    private readonly PassiveTreeManager.TreeStatModifier[] _modifiers;

    public TreeStatBundleSkill(PassiveTreeManager.TreeNode node, int stacks)
    {
        SkillId = node.Id;
        IconSymbol = node.Icon;
        NameKey = node.NameKey;
        DescKey = node.DescKey;
        IsPassive = true;
        IsInnate = false;
        Cooldown = 0.0f;
        Level = Mathf.Clamp(stacks, 1, node.MaxStacks);
        MaxLevel = node.MaxStacks;
        _modifiers = node.Modifiers;
    }

    public override void ApplyPassiveModifiers()
    {
        foreach (var modifier in _modifiers)
        {
            var (flat, pct) = PassiveTreeManager.SplitModifier(modifier, Level);
            ApplyStat(modifier.Stat, flat, pct);
        }
    }

    public override void RemovePassiveModifiers()
    {
        foreach (var modifier in _modifiers)
        {
            var (flat, pct) = PassiveTreeManager.SplitModifier(modifier, Level);
            RemoveStat(modifier.Stat, flat, pct);
        }
    }
}
