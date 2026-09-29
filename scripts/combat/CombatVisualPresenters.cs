using Godot;

namespace Game.Combat;

public readonly record struct ZoneVisualState(
    float LifeTimer,
    float Duration,
    float CurrentRadius,
    float Phase,
    Color CoreColor,
    Color RimColor);

public static class ZoneVisualPresenter
{
    public static void Draw(CanvasItem canvas, in ZoneVisualState state)
    {
        float alphaRatio = Mathf.Clamp(1.0f - (state.LifeTimer / Mathf.Max(0.01f, state.Duration)), 0.0f, 1.0f);
        Color core = new(state.CoreColor.R, state.CoreColor.G, state.CoreColor.B, state.CoreColor.A * alphaRatio);
        Color rim = new(state.RimColor.R, state.RimColor.G, state.RimColor.B, state.RimColor.A * alphaRatio);

        float pulse = 1.0f + 0.04f * Mathf.Sin(state.Phase);
        float drawRadius = state.CurrentRadius;
        canvas.DrawCircle(Vector2.Zero, drawRadius * pulse, core);
        canvas.DrawArc(Vector2.Zero, drawRadius * pulse, 0, Mathf.Tau, 32, rim, 2.0f);

        for (int i = 0; i < 4; i++)
        {
            float angle = (i * (Mathf.Tau / 4.0f)) + state.Phase * 0.2f;
            float dist = drawRadius * 0.45f + 8.0f * Mathf.Sin(state.Phase + i);
            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            float bubbleRadius = 5.0f + 2.0f * Mathf.Sin(state.Phase * 1.5f + i);

            canvas.DrawCircle(pos, bubbleRadius, new Color(rim.R, rim.G, rim.B, 0.4f * alphaRatio));
            canvas.DrawArc(pos, bubbleRadius, 0, Mathf.Tau, 12, rim, 1.0f);
        }
    }
}

public readonly record struct TelegraphVisualState(
    TelegraphAttackShape Shape,
    float Progress,
    float Timer,
    float Radius,
    float LineLength,
    float LineWidth,
    Vector2[] MultiCircleOffsets);

public static class TelegraphVisualPresenter
{
    public static void Draw(CanvasItem canvas, in TelegraphVisualState state)
    {
        float progress = state.Progress;
        Color borderCol = new(1.0f, 0.22f, 0.28f, 0.85f + 0.15f * Mathf.Sin(state.Timer * 15.0f));
        Color bgFaintCol = new(0.9f, 0.1f, 0.15f, 0.15f);
        Color fillChargingCol = new(1.0f, 0.15f + 0.25f * progress, 0.1f, 0.25f + 0.45f * progress);

        if (state.Shape == TelegraphAttackShape.Line)
        {
            Rect2 totalRect = new(0, -state.LineWidth / 2.0f, state.LineLength, state.LineWidth);
            Rect2 chargeRect = new(0, -state.LineWidth / 2.0f, state.LineLength * progress, state.LineWidth);

            canvas.DrawRect(totalRect, bgFaintCol);
            canvas.DrawRect(chargeRect, fillChargingCol);
            canvas.DrawRect(totalRect, borderCol, filled: false, width: 2.0f);
            canvas.DrawLine(new Vector2(0, 0), new Vector2(state.LineLength * progress, 0), borderCol, 1.5f);
        }
        else if (state.Shape == TelegraphAttackShape.MultiCircle)
        {
            float subRadius = state.Radius * 0.7f;
            for (int i = 0; i < state.MultiCircleOffsets.Length; i++)
            {
                Vector2 center = state.MultiCircleOffsets[i] * state.Radius;
                canvas.DrawCircle(center, subRadius, bgFaintCol);
                canvas.DrawCircle(center, subRadius * progress, fillChargingCol);
                canvas.DrawArc(center, subRadius, 0, Mathf.Tau, 28, borderCol, 2.0f);
            }
        }
        else
        {
            canvas.DrawCircle(Vector2.Zero, state.Radius, bgFaintCol);
            canvas.DrawCircle(Vector2.Zero, state.Radius * progress, fillChargingCol);
            canvas.DrawArc(Vector2.Zero, state.Radius, 0, Mathf.Tau, 36, borderCol, 2.5f);

            float rippleProgress = Mathf.PosMod(progress * 2.0f, 1.0f);
            canvas.DrawArc(Vector2.Zero, state.Radius * rippleProgress, 0, Mathf.Tau, 32,
                new Color(1f, 0.8f, 0.2f, (1.0f - rippleProgress) * 0.7f), 1.5f);
        }
    }
}
