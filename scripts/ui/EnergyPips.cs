using Godot;

namespace Phagocyte.UI;

/// <summary>
/// Circular energy pips (TODO Phase 1 revision): one disc per energy point.
/// Filled discs are consumed energy, hollow discs are still free, and the
/// trailing discs granted by generators (negative cost) render red so the
/// capacity they add is instantly recognizable. Geometry is computed per
/// draw from data, so the same node serves card badges and the chamber row.
/// </summary>
public partial class EnergyPips : Control
{
    public static readonly Color FillColor = new(0.20f, 0.78f, 0.95f);
    public static readonly Color EmptyColor = new(0.16f, 0.21f, 0.28f);
    public static readonly Color GeneratorColor = new(1.0f, 0.38f, 0.38f);
    private static readonly Color RingColor = new(0.05f, 0.08f, 0.12f, 0.95f);

    /// <summary>Total discs to draw.</summary>
    public int PipCount { get; private set; }

    /// <summary>Leading discs filled as consumed energy.</summary>
    public int FilledCount { get; private set; }

    /// <summary>Trailing discs granted by generators (drawn red).</summary>
    public int GeneratorCount { get; private set; }

    /// <summary>Disc diameter cap; discs shrink to fit narrow slots.</summary>
    [Export] public float MaxDiameter { get; set; } = 18.0f;

    /// <summary>Gap between discs.</summary>
    [Export] public float Gap { get; set; } = 4.0f;

    /// <summary>Draw the row from the left edge instead of centering it.</summary>
    [Export] public bool AlignLeft { get; set; } = true;

    public void Configure(int pipCount, int filledCount, int generatorCount)
    {
        PipCount = Mathf.Max(0, pipCount);
        FilledCount = Mathf.Clamp(filledCount, 0, PipCount);
        GeneratorCount = Mathf.Clamp(generatorCount, 0, PipCount);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (PipCount <= 0)
            return;

        float available = Size.X;
        float diameter = Mathf.Min(MaxDiameter, (available - (PipCount - 1) * Gap) / PipCount);
        if (diameter <= 1.0f)
        {
            Gap = Mathf.Max(1.0f, Gap * 0.5f);
            diameter = Mathf.Min(MaxDiameter, (available - (PipCount - 1) * Gap) / PipCount);
        }
        if (diameter <= 1.0f)
            return;

        float totalWidth = PipCount * diameter + (PipCount - 1) * Gap;
        float startX = AlignLeft ? 0.0f : Mathf.Max(0.0f, (available - totalWidth) * 0.5f);
        float centerY = Size.Y * 0.5f;
        float radius = diameter * 0.5f;

        for (int i = 0; i < PipCount; i++)
        {
            var center = new Vector2(startX + i * (diameter + Gap) + radius, centerY);
            bool isGenerator = i >= PipCount - GeneratorCount;
            bool isFilled = i < FilledCount;

            DrawCircle(center, radius, RingColor);

            Color body = isGenerator ? GeneratorColor : isFilled ? FillColor : EmptyColor;
            DrawCircle(center, Mathf.Max(1.0f, radius - 1.6f), body);

            if (isGenerator || isFilled)
            {
                DrawCircle(center - new Vector2(radius * 0.26f, radius * 0.26f),
                    Mathf.Max(0.6f, radius * 0.3f), new Color(1.0f, 1.0f, 1.0f, 0.35f));
            }
        }
    }
}
