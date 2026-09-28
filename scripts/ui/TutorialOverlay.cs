using Godot;

namespace Game.UI;

/// <summary>
/// Screen-space overlay for the first-run micro-cues (docs/tutorial.md §2):
/// the dodge roll hint above the cell. Drawn behind the
/// HUD panels (ZIndex = -1) so it never blocks interaction.
/// </summary>
public partial class TutorialOverlay : Control
{
    public Node2D? PlayerRef { get; set; }

    /// <summary>Cue 3: floating dodge hint above the cell.</summary>
    public bool ShowDodgeHint { get; set; }
    public string DodgeHintText { get; set; } = "";
    public float DodgeHintAlpha { get; set; } = 1.0f;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ZIndex = -1;
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (PlayerRef == null || !GodotObject.IsInstanceValid(PlayerRef))
            return;

        Vector2 cellScreen = PlayerRef.GetGlobalTransformWithCanvas().Origin;

        if (ShowDodgeHint)
            DrawDodgeHint(cellScreen);
    }

    private void DrawDodgeHint(Vector2 center)
    {
        var font = GetThemeDefaultFont();
        int fontSize = 17;
        float alpha = Mathf.Clamp(DodgeHintAlpha, 0.0f, 1.0f);
        var color = new Color(0.75f, 1.0f, 0.96f, alpha);
        Vector2 textSize = font.GetStringSize(DodgeHintText, HorizontalAlignment.Left, -1, fontSize);
        Vector2 pos = center + new Vector2(-textSize.X * 0.5f, -152.0f);

        var box = new Rect2(pos - new Vector2(14.0f, 8.0f), textSize + new Vector2(28.0f, 18.0f));
        DrawRect(box, new Color(0.02f, 0.09f, 0.11f, 0.88f * alpha));
        DrawRect(box, new Color(0.45f, 1.0f, 0.92f, 0.9f * alpha), false, 1.8f);
        DrawString(font, pos, DodgeHintText, HorizontalAlignment.Left, -1, fontSize, color);
    }

}
