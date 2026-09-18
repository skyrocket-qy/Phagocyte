using Godot;
using Phagocyte.Player;

namespace Phagocyte.UI;

/// <summary>
/// Screen-space overlay for the first-run micro-cues (docs/tutorial.md §2):
/// the WASD breathing ring, the Squeeze Mode hint above the cell, and the
/// microscopic fluid-shear arrow trails at the screen edges. Drawn behind the
/// HUD panels (ZIndex = -1) so it never blocks interaction.
/// </summary>
public partial class TutorialOverlay : Control
{
    public Node2D? PlayerRef { get; set; }

    /// <summary>Cue 1: WASD breathing ring around the cell (opening 5 seconds).</summary>
    public bool ShowMoveCue { get; set; }
    public float MoveCueProgress { get; set; } = 0.0f; // 1 -> 0 over the cue window

    /// <summary>Cue 3: floating squeeze hint above the cell.</summary>
    public bool ShowSqueezeHint { get; set; }
    public string SqueezeHintText { get; set; } = "";
    public float SqueezeHintAlpha { get; set; } = 1.0f;

    /// <summary>Cue 5: fluid field arrows at the screen edges.</summary>
    public bool FluidFieldActive { get; set; }
    public Vector2 FluidVector { get; set; }

    private float _phase = 0.0f;

    /// <summary>Diagnostics: number of draw passes (used by tests).</summary>
    public int DrawCount { get; private set; } = 0;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ZIndex = -1;
    }

    public override void _Process(double delta)
    {
        _phase += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCount++;
        if (PlayerRef == null || !GodotObject.IsInstanceValid(PlayerRef))
            return;

        Vector2 cellScreen = PlayerRef.GetGlobalTransformWithCanvas().Origin;

        if (ShowMoveCue)
            DrawMoveCue(cellScreen);

        if (ShowSqueezeHint)
            DrawSqueezeHint(cellScreen);

        if (FluidFieldActive)
            DrawFluidTrails();
    }

    private void DrawMoveCue(Vector2 center)
    {
        float breathe = 0.5f + 0.5f * Mathf.Sin(_phase * 2.4f);
        float fade = Mathf.Clamp(MoveCueProgress * 3.0f, 0.0f, 1.0f);
        float alpha = (0.42f + 0.28f * breathe) * fade;

        var ringColor = new Color(0.55f, 0.92f, 1.0f, alpha);
        float ringRadius = 92.0f + 5.0f * breathe;
        DrawArc(center, ringRadius, 0.0f, Mathf.Tau, 72, ringColor, 2.4f, true);

        var font = GetThemeDefaultFont();
        int fontSize = 18;
        var keyColor = new Color(0.72f, 0.97f, 1.0f, Mathf.Min(1.0f, alpha * 1.8f));

        DrawKeyCap(center + new Vector2(0.0f, -ringRadius - 6.0f), "W", font, fontSize, keyColor);
        DrawKeyCap(center + new Vector2(-ringRadius - 6.0f, 0.0f), "A", font, fontSize, keyColor);
        DrawKeyCap(center + new Vector2(0.0f, ringRadius + 6.0f), "S", font, fontSize, keyColor);
        DrawKeyCap(center + new Vector2(ringRadius + 6.0f, 0.0f), "D", font, fontSize, keyColor);
    }

    private void DrawKeyCap(Vector2 center, string label, Font font, int fontSize, Color color)
    {
        var cap = new Rect2(center - new Vector2(16.0f, 16.0f), new Vector2(32.0f, 32.0f));
        DrawRect(cap, new Color(0.03f, 0.10f, 0.14f, color.A * 0.55f));
        DrawRect(cap, color, false, 1.8f);
        DrawString(font, center + new Vector2(-8.0f, 6.5f), label,
            HorizontalAlignment.Left, -1, fontSize, color);
    }

    private void DrawSqueezeHint(Vector2 center)
    {
        var font = GetThemeDefaultFont();
        int fontSize = 17;
        float alpha = Mathf.Clamp(SqueezeHintAlpha, 0.0f, 1.0f);
        var color = new Color(0.75f, 1.0f, 0.96f, alpha);
        Vector2 textSize = font.GetStringSize(SqueezeHintText, HorizontalAlignment.Left, -1, fontSize);
        Vector2 pos = center + new Vector2(-textSize.X * 0.5f, -152.0f);

        var box = new Rect2(pos - new Vector2(14.0f, 8.0f), textSize + new Vector2(28.0f, 18.0f));
        DrawRect(box, new Color(0.02f, 0.09f, 0.11f, 0.88f * alpha));
        DrawRect(box, new Color(0.45f, 1.0f, 0.92f, 0.9f * alpha), false, 1.8f);
        DrawString(font, pos, SqueezeHintText, HorizontalAlignment.Left, -1, fontSize, color);
    }

    private void DrawFluidTrails()
    {
        Vector2 dir = FluidVector;
        if (dir.LengthSquared() <= 0.0001f)
            return;

        dir = dir.Normalized();
        Vector2 perp = new Vector2(-dir.Y, dir.X);
        Vector2 screen = GetViewportRect().Size;
        if (screen.X <= 1.0f || screen.Y <= 1.0f)
            return;

        var origin = screen * 0.5f;
        float travel = 140.0f; // hysteresis length toward the field direction
        float pulse = 0.5f + 0.5f * Mathf.Sin(_phase * 1.6f);

        for (int i = -2; i <= 2; i++)
        {
            Vector2 anchor = origin
                + perp * (i * Mathf.Min(screen.X, screen.Y) * 0.18f)
                - dir * Mathf.Min(screen.X, screen.Y) * 0.28f;

            float scroll = ((_phase * 90.0f) + i * 47.0f) % travel;
            Vector2 tail = anchor + dir * scroll;
            Vector2 head = tail + dir * 34.0f;

            float alpha = (0.14f + 0.14f * pulse) * (1.0f - scroll / travel);
            var trailColor = new Color(0.5f, 0.85f, 1.0f, alpha);

            DrawLine(tail, head, trailColor, 1.6f, true);
            DrawLine(head, head - dir * 12.0f + perp * 6.0f, trailColor, 1.4f, true);
            DrawLine(head, head - dir * 12.0f - perp * 6.0f, trailColor, 1.4f, true);
        }
    }
}
