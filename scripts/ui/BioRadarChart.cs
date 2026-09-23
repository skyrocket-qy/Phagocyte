using Godot;
using System;

namespace Phagocyte.UI;

/// <summary>
/// Bio-Radar Chart: renders an interactive, organic polygonal bio-radar web
/// displaying cellular vitals (Health, Armor, Motility, Special Trait).
/// Features animated vertex morphing, concentric cyber-fluorescent grid webs,
/// glowing polygon fills, and microscopy specimen containment brackets.
/// </summary>
public partial class BioRadarChart : Control
{
    private static readonly Color GridColor = new(0.15f, 0.45f, 0.65f, 0.35f);
    private static readonly Color GridStrongColor = new(0.25f, 0.75f, 0.95f, 0.55f);
    private static readonly Color PolyFillColor = new(0.0f, 0.92f, 0.75f, 0.26f);
    private static readonly Color PolyLineColor = new(0.22f, 1.0f, 0.85f, 0.95f);
    private static readonly Color PolyGlowColor = new(0.0f, 0.85f, 1.0f, 0.30f);
    private static readonly Color BracketColor = new(0.35f, 0.80f, 1.0f, 0.45f);
    private static readonly Color LabelColor = new(0.70f, 0.88f, 0.98f, 0.85f);
    private static readonly Color ValueColor = new(0.40f, 1.0f, 0.75f, 0.95f);

    private readonly float[] _target = new float[4] { 0.5f, 0.5f, 0.5f, 0.5f };
    private readonly float[] _current = new float[4] { 0.1f, 0.1f, 0.1f, 0.1f };
    private readonly string[] _labels = new string[4] { "VITALS", "ARMOR", "MOTILITY", "TRAIT" };
    private readonly string[] _values = new string[4] { "", "", "", "" };

    private float _time;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(240, 125);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        for (int i = 0; i < 4; i++)
        {
            float diff = _target[i] - _current[i];
            if (Mathf.Abs(diff) > 0.001f)
            {
                _current[i] = Mathf.Lerp(_current[i], _target[i], (float)delta * 14.0f);
            }
            else
            {
                _current[i] = _target[i];
            }
        }

        // Always redraw to keep breathing bioluminescence alive
        QueueRedraw();
    }

    /// <summary>
    /// Configures the 4-axis biological metrics.
    /// Values should be normalized between 0.1 and 1.0.
    /// </summary>
    public void SetStats(float vitals, float armor, float motility, float trait,
        string vitalsVal = "", string armorVal = "", string motilityVal = "", string traitVal = "",
        string? vitalsName = null, string? armorName = null, string? motilityName = null, string? traitName = null)
    {
        _target[0] = Mathf.Clamp(vitals, 0.12f, 1.0f);
        _target[1] = Mathf.Clamp(armor, 0.12f, 1.0f);
        _target[2] = Mathf.Clamp(motility, 0.12f, 1.0f);
        _target[3] = Mathf.Clamp(trait, 0.12f, 1.0f);

        _values[0] = vitalsVal;
        _values[1] = armorVal;
        _values[2] = motilityVal;
        _values[3] = traitVal;

        if (!string.IsNullOrEmpty(vitalsName)) _labels[0] = vitalsName;
        if (!string.IsNullOrEmpty(armorName)) _labels[1] = armorName;
        if (!string.IsNullOrEmpty(motilityName)) _labels[2] = motilityName;
        if (!string.IsNullOrEmpty(traitName)) _labels[3] = traitName;

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Size.X < 20.0f || Size.Y < 20.0f)
            return;

        Vector2 center = Size * 0.5f;
        float radius = Mathf.Min(Size.X * 0.22f, Size.Y * 0.30f);

        DrawSpecimenBrackets();
        DrawWebGrid(center, radius);
        DrawDataPolygon(center, radius);
        DrawAxisLabels(center, radius);
    }

    private void DrawSpecimenBrackets()
    {
        // 4 corner containment brackets (microscopy specimen slide)
        float bSize = 12.0f;
        float pad = 4.0f;
        Vector2 tl = new(pad, pad);
        Vector2 tr = new(Size.X - pad, pad);
        Vector2 bl = new(pad, Size.Y - pad);
        Vector2 br = new(Size.X - pad, Size.Y - pad);

        // Top-Left
        DrawLine(tl, tl + new Vector2(bSize, 0), BracketColor, 1.5f);
        DrawLine(tl, tl + new Vector2(0, bSize), BracketColor, 1.5f);

        // Top-Right
        DrawLine(tr, tr - new Vector2(bSize, 0), BracketColor, 1.5f);
        DrawLine(tr, tr + new Vector2(0, bSize), BracketColor, 1.5f);

        // Bottom-Left
        DrawLine(bl, bl + new Vector2(bSize, 0), BracketColor, 1.5f);
        DrawLine(bl, bl - new Vector2(0, bSize), BracketColor, 1.5f);

        // Bottom-Right
        DrawLine(br, br - new Vector2(bSize, 0), BracketColor, 1.5f);
        DrawLine(br, br - new Vector2(0, bSize), BracketColor, 1.5f);
    }

    private void DrawWebGrid(Vector2 center, float radius)
    {
        // Concentric webs at 0.25, 0.5, 0.75, 1.0
        float[] tiers = { 0.25f, 0.5f, 0.75f, 1.0f };
        for (int t = 0; t < tiers.Length; t++)
        {
            float r = radius * tiers[t];
            Color c = t == tiers.Length - 1 ? GridStrongColor : GridColor;
            Vector2[] ring = GetAxisPoints(center, r, r, r, r);
            Vector2[] closed = CloseLoop(ring);
            DrawPolyline(closed, c, t == tiers.Length - 1 ? 1.5f : 1.0f);
        }

        // 4 Cardinal Axis Spokes
        Vector2[] outer = GetAxisPoints(center, radius, radius, radius, radius);
        for (int i = 0; i < 4; i++)
        {
            DrawLine(center, outer[i], GridColor, 1.0f);
        }

        // Center hub dot
        DrawCircle(center, 3.0f, GridStrongColor);
    }

    private void DrawDataPolygon(Vector2 center, float radius)
    {
        // Compute 4 vertices
        float rTop = radius * _current[0];
        float rRight = radius * _current[1];
        float rBottom = radius * _current[2];
        float rLeft = radius * _current[3];

        Vector2[] points = GetAxisPoints(center, rTop, rRight, rBottom, rLeft);
        Vector2[] closed = CloseLoop(points);

        // Translucent fill
        DrawColoredPolygon(points, PolyFillColor);

        // Soft outer glow stroke
        DrawPolyline(closed, PolyGlowColor, 5.0f, true);

        // Sharp neon perimeter
        DrawPolyline(closed, PolyLineColor, 2.2f, true);

        // Pulsing vertex pips
        float pulse = 1.0f + 0.18f * Mathf.Sin(_time * 4.0f);
        for (int i = 0; i < 4; i++)
        {
            DrawCircle(points[i], 4.5f * pulse, new Color(PolyLineColor.R, PolyLineColor.G, PolyLineColor.B, 0.65f));
            DrawCircle(points[i], 2.4f, Colors.White);
        }
    }

    private void DrawAxisLabels(Vector2 center, float radius)
    {
        Font font = ThemeDB.FallbackFont;
        int fontSize = 11;
        float offset = 14.0f;

        // Top: Axis 0 (Vitals)
        DrawCenteredLabel(font, _labels[0], _values[0], center + new Vector2(0, -radius - offset), fontSize, true);

        // Right: Axis 1 (Armor)
        DrawRightLabel(font, _labels[1], _values[1], center + new Vector2(radius + offset - 4.0f, 0), fontSize);

        // Bottom: Axis 2 (Motility)
        DrawCenteredLabel(font, _labels[2], _values[2], center + new Vector2(0, radius + offset + 2.0f), fontSize, false);

        // Left: Axis 3 (Trait)
        DrawLeftLabel(font, _labels[3], _values[3], center + new Vector2(-radius - offset + 4.0f, 0), fontSize);
    }

    private void DrawCenteredLabel(Font font, string name, string val, Vector2 pos, int fontSize, bool isTop)
    {
        string text = string.IsNullOrEmpty(val) ? name : $"{name} {val}";
        Vector2 sz = font.GetStringSize(text, HorizontalAlignment.Center, -1, fontSize);
        Vector2 origin = new(pos.X - sz.X * 0.5f, isTop ? pos.Y - sz.Y * 0.2f : pos.Y + sz.Y * 0.8f);
        DrawString(font, origin + new Vector2(1, 1), text, HorizontalAlignment.Left, -1, fontSize, Colors.Black);
        DrawString(font, origin, text, HorizontalAlignment.Left, -1, fontSize, isTop ? ValueColor : LabelColor);
    }

    private void DrawRightLabel(Font font, string name, string val, Vector2 pos, int fontSize)
    {
        string text = string.IsNullOrEmpty(val) ? name : $"{name}\n{val}";
        Vector2 sz = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
        Vector2 origin = new(pos.X + 2.0f, pos.Y + sz.Y * 0.35f);
        DrawString(font, origin + new Vector2(1, 1), text, HorizontalAlignment.Left, -1, fontSize, Colors.Black);
        DrawString(font, origin, text, HorizontalAlignment.Left, -1, fontSize, LabelColor);
    }

    private void DrawLeftLabel(Font font, string name, string val, Vector2 pos, int fontSize)
    {
        string text = string.IsNullOrEmpty(val) ? name : $"{name}\n{val}";
        Vector2 sz = font.GetStringSize(text, HorizontalAlignment.Right, -1, fontSize);
        Vector2 origin = new(pos.X - sz.X - 2.0f, pos.Y + sz.Y * 0.35f);
        DrawString(font, origin + new Vector2(1, 1), text, HorizontalAlignment.Left, -1, fontSize, Colors.Black);
        DrawString(font, origin, text, HorizontalAlignment.Left, -1, fontSize, LabelColor);
    }

    private static Vector2[] GetAxisPoints(Vector2 center, float top, float right, float bottom, float left)
    {
        return new Vector2[4]
        {
            center + new Vector2(0, -top),
            center + new Vector2(right, 0),
            center + new Vector2(0, bottom),
            center + new Vector2(-left, 0)
        };
    }

    private static Vector2[] CloseLoop(Vector2[] pts)
    {
        var res = new Vector2[pts.Length + 1];
        Array.Copy(pts, res, pts.Length);
        res[pts.Length] = pts[0];
        return res;
    }
}
