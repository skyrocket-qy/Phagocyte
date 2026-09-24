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

    /// <summary>Stack the discs top-to-bottom (slot bottom-row badge) instead of left-to-right.</summary>
    [Export] public bool Vertical { get; set; } = false;

    private float _time;

    public override void _Process(double delta)
    {
        if (FilledCount > 0 || GeneratorCount > 0)
        {
            _time += (float)delta;
            QueueRedraw();
        }
    }

    public void Configure(int pipCount, int filledCount, int generatorCount)
    {
        int pips = Mathf.Max(0, pipCount);
        int filled = Mathf.Clamp(filledCount, 0, pips);
        int gens = Mathf.Clamp(generatorCount, 0, pips);
        if (pips == PipCount && filled == FilledCount && gens == GeneratorCount)
            return;
        PipCount = pips;
        FilledCount = filled;
        GeneratorCount = gens;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (PipCount <= 0)
            return;

        if (Vertical)
        {
            DrawVertical();
            return;
        }

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

            DrawPipDisc(center, radius, isGenerator, isFilled, i);
        }
    }

    /// <summary>Top-to-bottom variant for the slot bottom-row badge (name left, cost right).</summary>
    private void DrawVertical()
    {
        float available = Size.Y;
        float gap = Gap;
        float diameter = Mathf.Min(MaxDiameter, (available - (PipCount - 1) * gap) / PipCount);
        if (diameter <= 1.0f)
        {
            gap = Mathf.Max(1.0f, gap * 0.5f);
            diameter = Mathf.Min(MaxDiameter, (available - (PipCount - 1) * gap) / PipCount);
        }
        if (diameter <= 1.0f)
            return;

        float totalHeight = PipCount * diameter + (PipCount - 1) * gap;
        float startY = Mathf.Max(0.0f, (available - totalHeight) * 0.5f);
        float centerX = Size.X * 0.5f;
        float radius = diameter * 0.5f;

        for (int i = 0; i < PipCount; i++)
        {
            var center = new Vector2(centerX, startY + i * (diameter + gap) + radius);
            bool isGenerator = i >= PipCount - GeneratorCount;
            bool isFilled = i < FilledCount;

            DrawPipDisc(center, radius, isGenerator, isFilled, i);
        }
    }

    private void DrawPipDisc(Vector2 center, float radius, bool isGenerator, bool isFilled, int index)
    {
        if (isGenerator || isFilled)
        {
            Color body = isGenerator ? GeneratorColor : FillColor;
            float pulse = 0.88f + 0.12f * Mathf.Sin(_time * 4.0f + index * 0.7f);

            // Glowing ATP halo
            DrawCircle(center, (radius + 2.4f) * pulse, new Color(body.R, body.G, body.B, 0.22f));

            // Structural ring
            DrawCircle(center, radius, RingColor);

            // Luminous core
            DrawCircle(center, Mathf.Max(1.0f, radius - 1.3f), body);

            // Specular inner rim
            DrawArc(center, Mathf.Max(1.0f, radius - 2.5f), 0.0f, Mathf.Tau, 14, new Color(1.0f, 1.0f, 1.0f, 0.40f), 0.9f);

            // Photon excitation spark
            DrawCircle(center - new Vector2(radius * 0.24f, radius * 0.24f),
                Mathf.Max(0.7f, radius * 0.28f), Colors.White);
        }
        else
        {
            // Empty cytoplasm well
            DrawCircle(center, radius, RingColor);
            DrawCircle(center, Mathf.Max(1.0f, radius - 1.3f), new Color(0.06f, 0.10f, 0.14f, 0.92f));
            DrawArc(center, Mathf.Max(1.0f, radius - 1.8f), 0.0f, Mathf.Tau, 12, new Color(0.22f, 0.50f, 0.65f, 0.35f), 1.0f);
        }
    }
}
