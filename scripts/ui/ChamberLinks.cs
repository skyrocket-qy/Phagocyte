using Godot;

namespace Phagocyte.UI;

/// <summary>
/// Microtubule links behind the 2x2 chamber grid (workbench refactor): faint
/// cyan channels drawn through the gaps between the four bio-socket cards,
/// with a slow alpha breath so the lumen reads as living tissue. Pure view —
/// geometry is derived from the grid children every draw, so layout changes
/// can never desync it. Sits after the grid inside the well panel (drawn on
/// top) but only paints inside the inter-card gaps, never over card faces.
/// </summary>
public partial class ChamberLinks : Control
{
    /// <summary>Grid whose card gaps carry the links.</summary>
    [Export] public NodePath TargetGrid { get; set; } = new("WellMargin/ChamberGrid");

    [Export] public Color LineColor { get; set; } = new(0.35f, 0.9f, 1.0f, 0.30f);
    [Export] public Color GlowColor { get; set; } = new(0.35f, 0.9f, 1.0f, 0.10f);

    /// <summary>Breath cycles per second.</summary>
    [Export] public float PulseSpeed { get; set; } = 0.45f;

    private float _phase;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Process(double delta)
    {
        _phase += (float)delta * PulseSpeed * Mathf.Tau;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var grid = GetNodeOrNull<GridContainer>(TargetGrid);
        if (grid == null || grid.GetChildCount() != 4)
            return;

        var rects = new Rect2[4];
        for (int i = 0; i < 4; i++)
        {
            if (grid.GetChild(i) is not Control card)
                return;
            rects[i] = new Rect2(card.GetGlobalRect().Position - GetGlobalRect().Position, card.GetGlobalRect().Size);
        }

        float breath = 0.75f + 0.25f * Mathf.Sin(_phase);
        var line = LineColor;
        line.A *= breath;
        var glow = GlowColor;
        glow.A *= breath;

        // Horizontal channels: right edge of the left card -> left edge of the right card.
        DrawChannel(new Vector2(rects[0].End.X, rects[0].GetCenter().Y), new Vector2(rects[1].Position.X, rects[1].GetCenter().Y), line, glow);
        DrawChannel(new Vector2(rects[2].End.X, rects[2].GetCenter().Y), new Vector2(rects[3].Position.X, rects[3].GetCenter().Y), line, glow);
        // Vertical channels: bottom edge of the top card -> top edge of the bottom card.
        DrawChannel(new Vector2(rects[0].GetCenter().X, rects[0].End.Y), new Vector2(rects[2].GetCenter().X, rects[2].Position.Y), line, glow);
        DrawChannel(new Vector2(rects[1].GetCenter().X, rects[1].End.Y), new Vector2(rects[3].GetCenter().X, rects[3].Position.Y), line, glow);

        // Synapse node where the four channels meet.
        var center = (rects[0].GetCenter() + rects[3].GetCenter()) * 0.5f;
        DrawCircle(center, 9.0f, glow);
        DrawCircle(center, 4.0f, line);
    }

    private void DrawChannel(Vector2 from, Vector2 to, Color line, Color glow)
    {
        if ((to - from).Length() <= 0.5f)
            return;
        DrawLine(from, to, glow, 7.0f);
        DrawLine(from, to, line, 2.0f);
    }
}
