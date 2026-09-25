using Godot;
using System;
using System.Collections.Generic;

namespace Phagocyte.Environment;

/// <summary>
/// Mid-depth microvascular tissue layer (Phase 4): seeded branching capillary
/// vessels rendered between the deep tissue shader (<c>ArenaBG</c>) and the
/// drifting RBC/bokeh plane (<see cref="MicroscopeParallax"/>). Each vessel is
/// a meandering polyline with a dark lumen, endothelial wall rim and a slow
/// fluid-current dash train so the arena reads as living tissue, not a flat
/// backdrop. Seeded <see cref="System.Random"/> keeps the layout stable
/// across runs (screenshots stay comparable); parallax follows the same
/// camera-relative pattern as <see cref="MicroscopeParallax"/>.
/// </summary>
public partial class CapillaryTissueLayer : Node2D
{
    public const int VesselCount = 9;
    public const int PointsPerVessel = 22;
    public const float ArenaExtents = 2600.0f;
    public const float ParallaxFactor = 0.55f;

    [Export] public float FlowSpeed { get; set; } = 60.0f;

    public Camera2D? CameraRef { get; set; }

    public record VesselData
    {
        public Vector2[] Points = Array.Empty<Vector2>();
        public float Width;
        public float Depth;
        public float FlowOffset;
    }

    private readonly List<VesselData> _vessels = new();
    private float _age;

    /// <summary>Slow tissue drift; 30 Hz redraw halves polyline cost with no visible step.</summary>
    public const float RedrawInterval = 1.0f / 30.0f;
    private float _redrawAccum;

    // Scratch buffers reused every _Draw (avoids 2 allocs per vessel per frame).
    private Vector2[] _scratchShifted = Array.Empty<Vector2>();
    private Vector2[] _scratchSheen = Array.Empty<Vector2>();

    public override void _Ready()
    {
        ZIndex = -4;
        GenerateVessels();
    }

    private void GenerateVessels()
    {
        _vessels.Clear();
        var rng = new Random(1337);
        for (int v = 0; v < VesselCount; v++)
        {
            float depth = (float)rng.NextDouble();
            float yBase = Mathf.Lerp(-ArenaExtents, ArenaExtents, (float)rng.NextDouble());
            float amplitude = Mathf.Lerp(120.0f, 420.0f, (float)rng.NextDouble());
            float frequency = Mathf.Lerp(1.0f, 3.0f, (float)rng.NextDouble());
            float phase = (float)rng.NextDouble() * Mathf.Tau;
            var points = new Vector2[PointsPerVessel];
            for (int i = 0; i < PointsPerVessel; i++)
            {
                float t = (float)i / (PointsPerVessel - 1);
                float x = Mathf.Lerp(-ArenaExtents, ArenaExtents, t);
                float y = yBase + Mathf.Sin(t * frequency * Mathf.Tau + phase) * amplitude * 0.5f;
                points[i] = new Vector2(x, y);
            }
            _vessels.Add(new VesselData
            {
                Points = points,
                Width = Mathf.Lerp(26.0f, 64.0f, depth),
                Depth = depth,
                FlowOffset = (float)rng.NextDouble() * 1000.0f,
            });
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _age += dt;
        if (CameraRef == null || !GodotObject.IsInstanceValid(CameraRef))
        {
            var vp = GetViewport();
            if (vp != null)
                CameraRef = vp.GetCamera2D();
        }
        _redrawAccum += dt;
        if (_redrawAccum >= RedrawInterval)
        {
            _redrawAccum = 0.0f;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        Vector2 camPos = CameraRef != null && GodotObject.IsInstanceValid(CameraRef)
            ? CameraRef.GlobalPosition
            : Vector2.Zero;
        Vector2 parallaxOffset = camPos * (1.0f - ParallaxFactor);

        Rect2? cull = null;
        if (CameraRef != null && GodotObject.IsInstanceValid(CameraRef))
        {
            float zoom = Mathf.Max(0.01f, CameraRef.Zoom.X);
            Vector2 viewSize = GetViewportRect().Size / zoom;
            const float margin = 120.0f;
            cull = new Rect2(camPos - viewSize * 0.5f - new Vector2(margin, margin), viewSize + new Vector2(margin * 2.0f, margin * 2.0f));
        }

        foreach (var vessel in _vessels)
        {
            int n = vessel.Points.Length;
            if (n == 0)
                continue;
            if (_scratchShifted.Length < n)
            {
                _scratchShifted = new Vector2[n];
                _scratchSheen = new Vector2[n];
            }

            for (int i = 0; i < n; i++)
                _scratchShifted[i] = vessel.Points[i] + parallaxOffset;

            // Whole-vessel reject: skip 3 wide polylines when fully off-screen.
            if (cull.HasValue)
            {
                bool anyVisible = false;
                float pad = vessel.Width * 0.5f + 6.0f;
                var bounds = cull.Value.Grow(pad);
                for (int i = 0; i < n; i++)
                {
                    if (bounds.HasPoint(_scratchShifted[i]))
                    {
                        anyVisible = true;
                        break;
                    }
                }
                if (!anyVisible)
                    continue;
            }

            float depthAlpha = Mathf.Lerp(0.10f, 0.22f, vessel.Depth);
            var wall = new Color(0.45f, 0.16f, 0.20f, depthAlpha);
            var lumen = new Color(0.10f, 0.03f, 0.05f, depthAlpha + 0.08f);
            var sheen = new Color(0.85f, 0.35f, 0.40f, depthAlpha * 0.5f);

            DrawPolyline(_scratchShifted, wall, vessel.Width + 6.0f, true);
            DrawPolyline(_scratchShifted, lumen, vessel.Width, true);

            // Endothelial sheen: thin offset highlight along the vessel.
            for (int i = 0; i < n; i++)
                _scratchSheen[i] = _scratchShifted[i] + new Vector2(0.0f, -vessel.Width * 0.28f);
            DrawPolyline(_scratchSheen, sheen, 2.0f, true);

            // Fluid-current dashes drifting along the vessel direction.
            float travel = (_age * FlowSpeed + vessel.FlowOffset) % 1000.0f;
            for (int d = 0; d < 4; d++)
            {
                float s = (travel + d * 250.0f) / 1000.0f;
                Vector2 p = SamplePolyline(_scratchShifted, n, s);
                if (cull.HasValue && !cull.Value.HasPoint(p))
                    continue;
                DrawCircle(p, 3.0f, new Color(0.9f, 0.55f, 0.55f, depthAlpha * 0.9f));
            }
        }
    }

    private static Vector2 SamplePolyline(Vector2[] pts, int count, float t)
    {
        if (count == 0)
            return Vector2.Zero;
        if (count == 1)
            return pts[0];
        float f = Mathf.Clamp(t, 0.0f, 1.0f) * (count - 1);
        int i = Mathf.Min(Mathf.FloorToInt(f), count - 2);
        return pts[i].Lerp(pts[i + 1], f - i);
    }
}
