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
        if (Stats is CellStats cs)
        {
            foreach (var modifier in _modifiers)
            {
                bool isFlatOrPoints = modifier.Unit is PassiveTreeManager.TreeModifierUnit.Flat or PassiveTreeManager.TreeModifierUnit.PercentagePoints;
                float flat = isFlatOrPoints ? modifier.Value * Level : 0.0f;
                float percent = isFlatOrPoints ? 0.0f : modifier.Value * Level;
                cs.AddModifier(modifier.Stat, flat, percent);
            }
        }
        else if (Stats != null && Stats.HasMethod("add_modifier"))
        {
            foreach (var modifier in _modifiers)
            {
                bool isFlatOrPoints = modifier.Unit is PassiveTreeManager.TreeModifierUnit.Flat or PassiveTreeManager.TreeModifierUnit.PercentagePoints;
                float flat = isFlatOrPoints ? modifier.Value * Level : 0.0f;
                float percent = isFlatOrPoints ? 0.0f : modifier.Value * Level;
                Stats.Call("add_modifier", modifier.Stat, flat, percent);
            }
        }
    }

    public override void RemovePassiveModifiers()
    {
        if (Stats is CellStats cs)
        {
            foreach (var modifier in _modifiers)
            {
                bool isFlatOrPoints = modifier.Unit is PassiveTreeManager.TreeModifierUnit.Flat or PassiveTreeManager.TreeModifierUnit.PercentagePoints;
                float flat = isFlatOrPoints ? modifier.Value * Level : 0.0f;
                float percent = isFlatOrPoints ? 0.0f : modifier.Value * Level;
                cs.RemoveModifier(modifier.Stat, flat, percent);
            }
        }
        else if (Stats != null && Stats.HasMethod("remove_modifier"))
        {
            foreach (var modifier in _modifiers)
            {
                bool isFlatOrPoints = modifier.Unit is PassiveTreeManager.TreeModifierUnit.Flat or PassiveTreeManager.TreeModifierUnit.PercentagePoints;
                float flat = isFlatOrPoints ? modifier.Value * Level : 0.0f;
                float percent = isFlatOrPoints ? 0.0f : modifier.Value * Level;
                Stats.Call("remove_modifier", modifier.Stat, flat, percent);
            }
        }
    }
}
