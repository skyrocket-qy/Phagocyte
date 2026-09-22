using Godot;

namespace Phagocyte.Core;

/// <summary>
/// Represents a single numerical character statistic with base, flat, and percent modifiers.
/// Formula: FinalValue = (base_value + flat_bonus) * (1.0 + percent_bonus)
/// </summary>
public class Stat
{
    public float BaseValue { get; set; }
    public float FlatBonus { get; set; }
    public float PercentBonus { get; set; }

    public Stat(float baseValue = 0.0f)
    {
        BaseValue = baseValue;
        FlatBonus = 0.0f;
        PercentBonus = 0.0f;
    }

    public float GetValue()
    {
        return (BaseValue + FlatBonus) * (1.0f + PercentBonus);
    }

    public void AddModifier(float flat, float pct)
    {
        FlatBonus += flat;
        PercentBonus += pct;
    }

    public void RemoveModifier(float flat, float pct)
    {
        FlatBonus -= flat;
        PercentBonus -= pct;
    }

    public void SetBase(float val)
    {
        BaseValue = val;
    }
}
