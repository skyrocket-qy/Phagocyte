using Godot;

namespace Phagocyte.UI;

/// <summary>
/// Organic cellular-membrane vitals gauge (Phase 4): a <see cref="ProgressBar"/>
/// subclass so <c>VitalsView.HpBar</c> bindings, paths and headless asserts
/// keep working unchanged. The flat green fill becomes a pulsating membrane:
/// idle breathing pulse, white damage flash on HP drops, and an adrenaline
/// surge (faster, redder pulse) below 30% HP. The shared
/// <c>StyleBoxFlat_hp_fill</c> is duplicated per instance on ready (shared vs
/// per-instance stylebox rule).
/// </summary>
public partial class MembraneGauge : ProgressBar
{
    private StyleBoxFlat? _fill;
    private float _phase;
    private float _flash;
    private double _lastValue = -1.0;

    private static readonly Color HealthyMembrane = new(0.22f, 0.88f, 0.52f, 0.95f);
    private static readonly Color DangerMembrane = new(0.95f, 0.28f, 0.32f, 0.95f);

    public override void _Ready()
    {
        var sb = GetThemeStylebox("fill")?.Duplicate() as StyleBoxFlat;
        if (sb != null)
        {
            _fill = sb;
            AddThemeStyleboxOverride("fill", sb);
        }
        _lastValue = Value;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (_lastValue >= 0.0 && Value < _lastValue - 0.001f)
            _flash = 1.0f;
        _lastValue = Value;
        _flash = Mathf.Max(0.0f, _flash - dt * 2.5f);

        float ratio = MaxValue > 0.0f ? (float)(Value / MaxValue) : 1.0f;
        float danger = 1.0f - Mathf.Clamp(ratio / 0.30f, 0.0f, 1.0f);
        float speed = ratio > 0.0f ? Mathf.Lerp(2.2f, 7.5f, danger) : 0.0f;
        _phase += dt * speed;

        if (_fill != null)
        {
            Color baseCol = HealthyMembrane.Lerp(DangerMembrane, danger);
            float breathe = 0.06f * Mathf.Sin(_phase);
            baseCol = baseCol.Lightened(breathe + danger * 0.08f * Mathf.Sin(_phase * 1.7f));
            _fill.BgColor = baseCol.Lerp(Colors.White, _flash * 0.65f);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Membrane speckle shimmer riding on top of the theme fill.
        float ratio = MaxValue > 0.0f ? (float)(Value / MaxValue) : 1.0f;
        float w = Size.X * Mathf.Clamp(ratio, 0.0f, 1.0f);
        if (w < 4.0f)
            return;
        float danger = 1.0f - Mathf.Clamp(ratio / 0.30f, 0.0f, 1.0f);
        float shimmer = 0.35f + 0.25f * Mathf.Sin(_phase * 1.3f) + _flash * 0.4f;
        for (int i = 0; i < 14; i++)
        {
            float t = (i + 0.5f) / 14.0f;
            float x = t * w;
            float y = Size.Y * 0.5f + Mathf.Sin(_phase * 2.0f + i * 1.7f) * (Size.Y * 0.22f);
            float r = 1.2f + 0.8f * Mathf.Sin(_phase + i);
            var c = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp(shimmer, 0.0f, 1.0f) * (0.5f - danger * 0.2f));
            DrawCircle(new Vector2(x, y), Mathf.Max(0.6f, r), c);
        }
        // Adrenaline rim: hot edge glow when critical.
        if (danger > 0.01f && ratio > 0.0f)
        {
            DrawRect(new Rect2(0.0f, 0.0f, w, 1.6f),
                new Color(1.0f, 0.45f, 0.45f, danger * (0.5f + 0.3f * Mathf.Sin(_phase * 2.0f))));
        }
    }
}
