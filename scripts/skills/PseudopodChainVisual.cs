using Godot;
using System;
using Phagocyte.Enemies;

namespace Phagocyte.Skills;

/// <summary>
/// Transient chain-strike visual for the pseudopod attacks (merged from the
/// retired PseudopodLimb organelle): a tapered flesh chain shoots from the
/// host edge in the host cell's own colors, its cup-ring tip snaps shut on
/// the prey (grab flash + cytoplasm burst), then the chain retracts.
/// Fires <see cref="Arrived"/> once the tip reaches the target so the owning
/// skill can apply damage and start the drag exactly on contact.
/// </summary>
public partial class PseudopodChainVisual : Node2D
{
    public event Action<Node2D>? Arrived;

    public Node2D? Host { get; set; }
    public Node2D? Target { get; set; }
    public float BaseHalfWidth { get; set; } = 16.0f;
    public float ExtendSpeed { get; set; } = 1250.0f;
    public float RetractSpeed { get; set; } = 900.0f;
    public float HoldDuration { get; set; } = 0.30f;
    public float GrabRadius { get; set; } = 28.0f;

    public Color? ChainFillColor { get; set; }
    public Color? ChainEdgeColor { get; set; }

    private const int RibbonSegments = 6;
    private const float FlashDuration = 0.28f;
    private const float FlashCoreDuration = 0.15f;
    private const float EnvelopDuration = 0.26f;
    private const float EnvelopSnapDuration = 0.08f;

    private enum Phase { Extend, Hold, Retract, Done }

    private Phase _phase = Phase.Extend;
    private float _tipDist;
    private float _launchDist = 120.0f;
    private Vector2 _dir = Vector2.Right;
    private float _holdTimer;
    private float _age;
    private float _flashAge = 999.0f;
    private Vector2 _flashLocal = Vector2.Zero;
    private float _envelopAge = 999.0f;
    private Vector2 _envelopLocal = Vector2.Zero;
    private float _envelopRadius = 36.0f;
    private bool _arrivedFired;
    private Color _fill;
    private Color _edge;
    private Color _core;
    private bool _colorsResolved;

    public override void _Ready()
    {
        ZIndex = 5;
        ResolveColors();
        if (Host != null && GodotObject.IsInstanceValid(Host))
            GlobalPosition = Host.GlobalPosition;
        _dir = AimDirection();
        _launchDist = LaunchDistance();
    }

    private void ResolveColors()
    {
        var (hostFill, hostEdge) = ChainFillColor.HasValue && ChainEdgeColor.HasValue
            ? (ChainFillColor.Value, ChainEdgeColor.Value)
            : PseudopodPalette.ResolveHostColors(Host);
        _fill = hostFill;
        _edge = hostEdge;
        _core = new Color(Mathf.Min(_edge.R * 1.05f, 1.0f), Mathf.Min(_edge.G * 1.05f, 1.0f), Mathf.Min(_edge.B * 1.05f, 1.0f), _edge.A);
        _colorsResolved = true;
    }

    private Vector2 AimDirection()
    {
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return Vector2.Right;
        Vector2 origin = Host.GlobalPosition;
        Vector2 aim = Target != null && GodotObject.IsInstanceValid(Target)
            ? Target.GlobalPosition
            : origin + Vector2.Right * 120.0f;
        Vector2 offset = aim - origin;
        return offset.LengthSquared() > 1.0f ? offset.Normalized() : Vector2.Right;
    }

    private float LaunchDistance()
    {
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return 120.0f;
        if (Target != null && GodotObject.IsInstanceValid(Target))
            return Mathf.Max(40.0f, Host.GlobalPosition.DistanceTo(Target.GlobalPosition));
        return 120.0f;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _age += dt;

        if (Host == null || !GodotObject.IsInstanceValid(Host))
        {
            QueueFree();
            return;
        }
        GlobalPosition = Host.GlobalPosition;

        switch (_phase)
        {
            case Phase.Extend:
                if (Target != null && GodotObject.IsInstanceValid(Target))
                    _dir = (_dir + (Target.GlobalPosition - GlobalPosition).Normalized() * dt * 8.0f).Normalized();
                _tipDist += ExtendSpeed * dt;
                if (CheckArrival())
                {
                    _phase = Phase.Hold;
                    _holdTimer = HoldDuration;
                    FireArrival();
                }
                else if (_tipDist >= _launchDist + GrabRadius)
                {
                    // Clean miss: brief hold, then reel back in.
                    _phase = Phase.Hold;
                    _holdTimer = 0.10f;
                }
                break;

            case Phase.Hold:
                _holdTimer -= dt;
                if (_holdTimer <= 0.0f)
                    _phase = Phase.Retract;
                break;

            case Phase.Retract:
                _tipDist -= RetractSpeed * dt;
                if (_tipDist <= 10.0f)
                {
                    _phase = Phase.Done;
                    QueueFree();
                    return;
                }
                break;
        }

        _flashAge += dt;
        _envelopAge += dt;
        QueueRedraw();
    }

    private bool CheckArrival()
    {
        if (Target == null || !GodotObject.IsInstanceValid(Target))
            return false;
        Vector2 tipGlobal = GlobalPosition + _dir * _tipDist;
        return tipGlobal.DistanceTo(Target.GlobalPosition) <= GrabRadius;
    }

    private void FireArrival()
    {
        Vector2 tipGlobal = GlobalPosition + _dir * _tipDist;
        _flashAge = 0.0f;
        _flashLocal = ToLocal(tipGlobal);
        // Envelope stamp: sized to the prey's body so the fleshy circle
        // visibly swallows it whole.
        float bodyR = Target is BaseEnemy foe && GodotObject.IsInstanceValid(foe)
            ? foe.BodyRadius
            : 26.0f;
        _envelopAge = 0.0f;
        _envelopLocal = ToLocal(tipGlobal);
        _envelopRadius = bodyR + 10.0f;
        if (!_arrivedFired)
        {
            _arrivedFired = true;
            if (Target != null && GodotObject.IsInstanceValid(Target))
                Arrived?.Invoke(Target);
        }
    }

    public override void _Draw()
    {
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;
        if (!_colorsResolved)
            ResolveColors();
        if (_tipDist < 4.0f)
            return;

        float fadeIn = Mathf.Clamp(_age / 0.05f, 0.0f, 1.0f);
        float ang = _dir.Angle();
        DrawSetTransform(Vector2.Zero, ang, Vector2.One);

        float wRoot = BaseHalfWidth * 0.35f;
        const float wTip = 4.0f;

        // Near-uniform flesh ribbon: halo, fill, core, edge.
        Vector2[] ribbon = BuildRibbon(_tipDist, wRoot, wTip, 1.0f);
        DrawOutline(ribbon, new Color(_edge.R, _edge.G, _edge.B, _edge.A * 0.20f * fadeIn), wRoot * 0.6f + 2.0f);
        DrawColoredPolygon(ribbon, new Color(_fill.R, _fill.G, _fill.B, _fill.A * fadeIn));
        DrawColoredPolygon(BuildRibbon(_tipDist, wRoot, wTip, 0.5f), new Color(_core.R, _core.G, _core.B, _core.A * 0.8f * fadeIn));
        DrawOutline(ribbon, new Color(_edge.R, _edge.G, _edge.B, _edge.A * fadeIn), 2.0f);

        // Cup-ring tip: open while travelling, snapped shut on arrival.
        bool closed = _phase != Phase.Extend;
        float cupR = closed ? 4.5f : 8.0f;
        Vector2 tip = new Vector2(_tipDist, 0.0f);
        Color edgeFade = new Color(_edge.R, _edge.G, _edge.B, _edge.A * fadeIn);
        DrawArc(tip, cupR, 0.0f, Mathf.Tau, 20, edgeFade, 2.5f);
        DrawCircle(tip, 2.8f, new Color(_core.R, _core.G, _core.B, _core.A * fadeIn));

        if (_flashAge < FlashDuration)
        {
            float ft = _flashAge / FlashDuration;
            DrawArc(_flashLocal.Rotated(-ang), Mathf.Lerp(8.0f, 46.0f, ft), 0.0f, Mathf.Tau, 32,
                new Color(1.0f, 1.0f, 1.0f, (1.0f - ft) * 0.8f), 4.0f * (1.0f - ft) + 1.5f);
        }
        if (_flashAge < FlashCoreDuration)
        {
            float ct = _flashAge / FlashCoreDuration;
            DrawCircle(_flashLocal.Rotated(-ang), Mathf.Lerp(12.0f, 0.0f, ct),
                new Color(1.0f, 1.0f, 1.0f, (1.0f - ct) * 0.9f));
        }

        // Envelope: a fleshy circle snaps shut over the prey, then winks
        // out — the visible "one bite". Sized to the prey's body.
        if (_envelopAge < EnvelopDuration)
        {
            Vector2 ep = _envelopLocal.Rotated(-ang);
            if (_envelopAge < EnvelopSnapDuration)
            {
                DrawCircle(ep, _envelopRadius,
                    new Color(_fill.R, _fill.G, _fill.B, _fill.A * fadeIn));
                DrawArc(ep, _envelopRadius, 0.0f, Mathf.Tau, 32,
                    new Color(_edge.R, _edge.G, _edge.B, _edge.A * fadeIn), 3.0f);
            }
            else
            {
                float et = (_envelopAge - EnvelopSnapDuration) / (EnvelopDuration - EnvelopSnapDuration);
                DrawCircle(ep, _envelopRadius * (1.0f - et),
                    new Color(_fill.R, _fill.G, _fill.B, _fill.A * (1.0f - et) * fadeIn));
                DrawArc(ep, _envelopRadius * (1.0f - et), 0.0f, Mathf.Tau, 32,
                    new Color(_edge.R, _edge.G, _edge.B, _edge.A * (1.0f - et) * fadeIn), 3.0f * (1.0f - et) + 1.0f);
            }
        }

        DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
    }

    private Vector2[] BuildRibbon(float len, float wRoot, float wTip, float widthScale)
    {
        var upper = new Vector2[RibbonSegments + 1];
        var lower = new Vector2[RibbonSegments + 1];
        for (int i = 0; i <= RibbonSegments; i++)
        {
            float t = (float)i / RibbonSegments;
            float x = Mathf.Lerp(0.0f, len, t);
            float hw = Mathf.Lerp(wRoot, wTip, t) * widthScale;
            float ripple = Mathf.Sin(t * 5.0f - _age * 10.0f) * 1.2f * Mathf.Sin(t * Mathf.Pi);
            upper[i] = new Vector2(x, -hw - ripple * widthScale);
            lower[i] = new Vector2(x, hw - ripple * widthScale);
        }

        var poly = new Vector2[(RibbonSegments + 1) * 2 + 1];
        for (int i = 0; i <= RibbonSegments; i++)
            poly[i] = upper[i];
        poly[RibbonSegments + 1] = new Vector2(len + 2.0f, 0.0f);
        for (int i = 0; i <= RibbonSegments; i++)
            poly[RibbonSegments + 2 + i] = lower[RibbonSegments - i];
        return poly;
    }

    private void DrawOutline(Vector2[] poly, Color color, float width)
    {
        var outline = new Vector2[poly.Length + 1];
        System.Array.Copy(poly, outline, poly.Length);
        outline[^1] = poly[0];
        DrawPolyline(outline, color, width);
    }
}
