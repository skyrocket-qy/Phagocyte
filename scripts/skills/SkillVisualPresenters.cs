using Godot;
using System.Collections.Generic;

namespace Game.Skills;

/// <summary>
/// Stateless rendering routines for transient active skill visuals.
/// Decouples CanvasItem drawing primitives from skill gameplay logic.
/// </summary>
public static class SkillVisualPresenters
{
    public static void DrawBeam(CanvasItem canvas, Vector2 from, Vector2 to, float width, float alpha, Color accent, Color core)
    {
        canvas.DrawLine(from, to, new Color(accent, 0.35f * alpha), width + 8.0f);
        canvas.DrawLine(from, to, new Color(accent, 0.9f * alpha), width * 0.5f);
        canvas.DrawLine(from, to, new Color(core, 0.95f * alpha), width * 0.22f);
        canvas.DrawCircle(from, width * 0.6f, new Color(core, 0.8f * alpha));
        canvas.DrawCircle(to, width * 0.6f, new Color(accent, 0.8f * alpha));
    }

    public static void DrawNova(CanvasItem canvas, float r, float alpha, float coneHalfAngle, Vector2 aim, Color accent, Color core)
    {
        if (coneHalfAngle < 0.0f)
        {
            canvas.DrawCircle(Vector2.Zero, r, new Color(accent, 0.10f * alpha));
            canvas.DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 48, new Color(accent, 0.8f * alpha), 5.0f);
            canvas.DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 48, new Color(core, 0.9f * alpha), 2.0f);
        }
        else
        {
            float c = aim.Angle();
            canvas.DrawArc(Vector2.Zero, r, c - coneHalfAngle, c + coneHalfAngle, 36, new Color(accent, 0.8f * alpha), 5.0f);
            canvas.DrawArc(Vector2.Zero, r, c - coneHalfAngle, c + coneHalfAngle, 36, new Color(core, 0.9f * alpha), 2.0f);
        }
    }

    public static void DrawNovaMarker(CanvasItem canvas, float age, Color accent)
    {
        float pulse = 1.0f + 0.1f * Mathf.Sin(age * 10.0f);
        canvas.DrawArc(Vector2.Zero, 22.0f * pulse, 0.0f, Mathf.Tau, 32, new Color(accent, 0.8f), 2.0f);
        canvas.DrawCircle(Vector2.Zero, 6.0f, new Color(accent, 0.6f));
    }

    public static void DrawStrike(CanvasItem canvas, Vector2 origin, Vector2 tip, float halfWidth, string kind, Color accent, Color core)
    {
        Vector2 dir = (tip - origin).Normalized();
        if (dir == Vector2.Zero)
            return;
        Vector2 perp = dir.Orthogonal();
        canvas.DrawLine(origin, tip, new Color(accent, 0.5f), halfWidth * 0.7f);
        canvas.DrawLine(origin, tip, new Color(core, 0.8f), halfWidth * 0.25f);
        if (kind == "fist")
        {
            for (int k = -1; k <= 1; k++)
                canvas.DrawCircle(tip + perp * k * halfWidth * 0.5f + dir * 6.0f, halfWidth * 0.35f, new Color(accent, 0.8f));
        }
        else
        {
            canvas.DrawArc(tip, halfWidth * 0.8f, dir.Angle() - 1.1f, dir.Angle() + 1.1f, 16, new Color(accent, 0.9f), 3.0f);
        }
    }

    public static void DrawAuraOrbital(CanvasItem canvas, float orbit, IReadOnlyList<float> bladeAngles, Color accent, Color core)
    {
        for (int i = 0; i < bladeAngles.Count; i++)
        {
            float angle = bladeAngles[i];
            Vector2 pos = Vector2.FromAngle(angle) * orbit;
            canvas.DrawLine(pos - Vector2.FromAngle(angle + 0.45f) * 20.0f, pos, new Color(accent, 0.7f), 6.0f);
            canvas.DrawCircle(pos, 5.0f, new Color(core, 0.9f));
        }
    }

    public static void DrawAuraRadial(CanvasItem canvas, float r, Color accent, Color core)
    {
        canvas.DrawCircle(Vector2.Zero, r * 0.9f, new Color(accent, 0.10f));
        canvas.DrawArc(Vector2.Zero, r * 0.65f, 0.0f, Mathf.Tau, 40, new Color(accent, 0.5f), 2.0f);
        canvas.DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 48, new Color(core, 0.6f), 2.0f);
    }
}
