using Godot;
using Dictionary = Godot.Collections.Dictionary;
using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.Player;

/// <summary>
/// Presentation layer for PlayerActor.
/// Manages cytoplasm deformation, membrane lines, nucleus inertia, granules Brownian motion,
/// dodge burst ring, and hit flash tweens.
/// </summary>
public partial class PlayerVisuals : Node2D
{
    public partial class GranuleCanvas : Node2D
    {
        public struct Granule
        {
            public Vector2 BasePos;
            public Vector2 CurrentOffset;
            public Vector2 Velocity;
            public float Size;
            public Color GranuleColor;
            public float BrownianPhase;
            public float LagSensitivity;
        }

        public readonly List<Granule> Granules = new();

        public override void _Draw()
        {
            for (int i = 0; i < Granules.Count; i++)
            {
                var g = Granules[i];
                Vector2 drawPos = g.BasePos + g.CurrentOffset;
                DrawCircle(drawPos, g.Size, g.GranuleColor);
                if (g.Size > 2.4f)
                {
                    DrawCircle(drawPos, g.Size * 0.45f, new Color(g.GranuleColor.R * 1.5f, g.GranuleColor.G * 1.5f, g.GranuleColor.B * 1.8f, g.GranuleColor.A * 0.75f));
                }
            }
        }
    }

    public partial class DodgeRing : Node2D
    {
        public PlayerActor? Host { get; set; }

        public override void _Process(double delta)
        {
            if (Host == null || !GodotObject.IsInstanceValid(Host))
            {
                Visible = false;
                return;
            }
            Visible = Host.IsDodging;
            if (Visible)
                QueueRedraw();
        }

        public override void _Draw()
        {
            if (Host == null || !GodotObject.IsInstanceValid(Host) || !Host.IsDodging)
                return;
            float dashRadius = Host.CurrentRadius + 9.0f;
            DrawArc(Vector2.Zero, dashRadius, 0.0f, Mathf.Tau, 48,
                new Color(0.55f, 1.0f, 0.95f, 0.9f), 3.2f, true);
        }
    }

    public PlayerActor? Host { get; private set; }

    public Polygon2D? Cytoplasm { get; set; }
    public Line2D? Membrane { get; set; }
    public Polygon2D? Nucleus { get; set; }
    public CollisionPolygon2D? EngulfCollider { get; set; }

    public int VertexCount { get; set; } = 32;
    public float DeformationSpeed { get; set; } = 3.6f;
    public float BaseDeformationMag { get; set; } = 24.0f;
    public float CurrentDeformationMag { get; set; } = 24.0f;
    public int SmoothSubdivisions { get; set; } = 4;
    public FastNoiseLite? Noise { get; set; }
    public float NoiseTime { get; set; } = 0.0f;

    public Vector2 NucleusOffset { get; set; } = Vector2.Zero;
    public Vector2 NucleusVelocity { get; set; } = Vector2.Zero;

    private GranuleCanvas? _granuleCanvas;
    private DodgeRing? _dodgeRing;
    private Tween? _hitFlashTween;

    private float[] _rawRadii = Array.Empty<float>();
    private float[] _relaxedRadii = Array.Empty<float>();
    private Vector2[] _controlPoints = Array.Empty<Vector2>();
    private Vector2[] _smoothPoints = Array.Empty<Vector2>();
    private Vector2[] _uvPoints = Array.Empty<Vector2>();
    private Vector2[] _linePoints = Array.Empty<Vector2>();
    private float _lastRadiusParam = float.NaN;

    public void Setup(PlayerActor host)
    {
        Host = host;
        Cytoplasm = host.GetNodeOrNull<Polygon2D>("Cytoplasm");
        Membrane = host.GetNodeOrNull<Line2D>("Membrane");
        Nucleus = host.GetNodeOrNull<Polygon2D>("Nucleus");
        EngulfCollider = host.GetNodeOrNull<CollisionPolygon2D>("EngulfArea/EngulfCollider");

        SetupIdentityVisuals(host.GetClassDef());
        SetupGranuleCanvas();
        SetupNucleusShape(host.GetClassDef());
        SetupCytoplasmShader();

        if (_dodgeRing == null || !GodotObject.IsInstanceValid(_dodgeRing))
        {
            _dodgeRing = new DodgeRing { Name = "DodgeRing", Host = host, ZIndex = 3 };
            host.AddChild(_dodgeRing);
        }
    }

    public void SetupIdentityVisuals(Dictionary classDef)
    {
        BaseDeformationMag = CatalogLoader.GetFloat(classDef, "deform_mag", BaseDeformationMag);
        DeformationSpeed = CatalogLoader.GetFloat(classDef, "deform_speed", DeformationSpeed);
        Noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Seed = (int)GD.Randi(),
            Frequency = CatalogLoader.GetFloat(classDef, "noise_freq", 0.65f),
            FractalOctaves = CatalogLoader.GetInt(classDef, "noise_octaves", 2)
        };
        if (Cytoplasm != null)
            Cytoplasm.Color = CatalogLoader.GetColor(classDef, "cyto_color", Cytoplasm.Color);
        if (Membrane != null)
            Membrane.DefaultColor = CatalogLoader.GetColor(classDef, "membrane_color", Membrane.DefaultColor);
    }

    public void SetupGranuleCanvas()
    {
        if (_granuleCanvas != null && GodotObject.IsInstanceValid(_granuleCanvas))
            return;
        _granuleCanvas = new GranuleCanvas { Name = "Granules", ZIndex = 0 };
        if (Host != null)
            Host.AddChild(_granuleCanvas);
        else
            AddChild(_granuleCanvas);

        int count = 24;
        for (int i = 0; i < count; i++)
        {
            float angle = (float)GD.RandRange(0.0, Mathf.Tau);
            float dist = (float)GD.RandRange(0.15, 0.75) * (Host?.CurrentRadius ?? 48.0f);
            Vector2 basePos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

            float size = (float)GD.RandRange(1.8, 3.8);
            Color gColor = (i % 3) switch
            {
                0 => new Color(0.4f, 0.85f, 0.95f, 0.7f),
                1 => new Color(0.95f, 0.5f, 0.8f, 0.65f),
                _ => new Color(0.7f, 0.95f, 0.5f, 0.6f)
            };

            _granuleCanvas.Granules.Add(new GranuleCanvas.Granule
            {
                BasePos = basePos,
                CurrentOffset = Vector2.Zero,
                Velocity = Vector2.Zero,
                Size = size,
                GranuleColor = gColor,
                BrownianPhase = (float)GD.RandRange(0.0, 100.0),
                LagSensitivity = (float)GD.RandRange(0.04, 0.09)
            });
        }
    }

    public void SetupNucleusShape(Dictionary classDef)
    {
        int n = Mathf.Max(8, CatalogLoader.GetInt(classDef, "nucleus_points", 32));
        float nRadius = CatalogLoader.GetFloat(classDef, "nucleus_radius", 18.0f);
        string kind = CatalogLoader.GetString(classDef, "nucleus_kind", "circle");
        float amp = CatalogLoader.GetFloat(classDef, "nucleus_amp", 0.0f);
        float freq = CatalogLoader.GetFloat(classDef, "nucleus_freq", 1.0f);
        var nPts = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float a = i * (Mathf.Tau / (float)n);
            float r = kind switch
            {
                "notch" => nRadius * (1.0f - amp * Mathf.Max(0.0f, Mathf.Cos(a * freq))),
                "spoked" or "lobed" => nRadius * (1.0f + amp * Mathf.Cos(a * freq)),
                "oval" => nRadius * (1.0f + amp * Mathf.Sin(a * freq)),
                _ => nRadius,
            };
            nPts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
        if (Nucleus != null)
        {
            Nucleus.Color = CatalogLoader.GetColor(classDef, "nucleus_color", Nucleus.Color);
            Nucleus.Polygon = nPts;
            var uvs = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                uvs[i] = (nPts[i] / (nRadius * 2.0f)) + new Vector2(0.5f, 0.5f);
            }
            Nucleus.UV = uvs;
            Nucleus.ZIndex = 0;

            var shader = AssetLoader.Load<Shader>("res://shaders/nucleus_sphere.gdshader");
            var nMat = new ShaderMaterial { Shader = shader };
            nMat.SetShaderParameter("radius", nRadius);
            nMat.SetShaderParameter("core_color", Nucleus.Color);
            Color edgeCol = new Color(Nucleus.Color.R * 0.28f, Nucleus.Color.G * 0.15f, Nucleus.Color.B * 0.32f, 1.0f);
            nMat.SetShaderParameter("edge_color", edgeCol);
            Color highlightCol = new Color(Mathf.Min(1.0f, Nucleus.Color.R * 1.45f), Mathf.Min(1.0f, Nucleus.Color.G * 1.45f), Mathf.Min(1.0f, Nucleus.Color.B * 1.45f), 1.0f);
            nMat.SetShaderParameter("highlight_color", highlightCol);
            Nucleus.Material = nMat;
        }
        if (Cytoplasm != null)
            Cytoplasm.ZIndex = 1;
        if (Membrane != null)
            Membrane.ZIndex = 2;
    }

    public void SetupCytoplasmShader()
    {
        if (Cytoplasm == null)
            return;
        var shader = AssetLoader.Load<Shader>("res://shaders/cytoplasm_gel.gdshader");
        var mat = new ShaderMaterial { Shader = shader };
        Color baseCol = Cytoplasm.Color;
        mat.SetShaderParameter("tint_color", baseCol);
        Color rimCol = new Color(0.85f, 0.95f, 1.25f, 1.0f);
        mat.SetShaderParameter("rim_color", rimCol);
        mat.SetShaderParameter("rim_power", 3.2f);
        mat.SetShaderParameter("inner_alpha", 0.22f);
        mat.SetShaderParameter("flow_speed", 1.0f);
        float initialRadius = Host != null && Host.CurrentRadius > 0 ? Host.CurrentRadius : 48.0f;
        mat.SetShaderParameter("cell_radius", initialRadius);
        Cytoplasm.Material = mat;
    }

    public void UpdateBodyDeformation(float delta)
    {
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;

        var classDef = Host.GetClassDef();
        if (CatalogLoader.GetString(classDef, "deform_kind", "standard") == "star")
        {
            UpdateDeformStar(delta, classDef);
            return;
        }
        NoiseTime += delta * DeformationSpeed;

        float areaScale = Host.Stats != null ? Host.Stats.GetStat("area") : 1.0f;
        float curR = Host.BaseRadius * areaScale;
        CurrentDeformationMag = BaseDeformationMag * areaScale;
        Host.CurrentRadius = curR;

        EnsureDeformBuffers(VertexCount, VertexCount * SmoothSubdivisions);
        var points = _controlPoints;
        var rawRadii = _rawRadii;
        float angleStep = Mathf.Tau / (float)VertexCount;
        Vector2 vel = Host.Velocity;
        Vector2 moveDir = vel.Length() > 20.0f ? vel.Normalized() : Vector2.Zero;

        for (int i = 0; i < VertexCount; i++)
        {
            float angle = i * angleStep;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            float nx1 = Mathf.Cos(angle) * 1.5f;
            float ny1 = Mathf.Sin(angle) * 1.5f;
            float nVal1 = Noise != null ? Noise.GetNoise3D(nx1, ny1, NoiseTime) : 0.0f;

            float nx2 = Mathf.Cos(angle * 2.0f) * 2.4f;
            float ny2 = Mathf.Sin(angle * 2.0f) * 2.4f;
            float nVal2 = Noise != null ? Noise.GetNoise3D(nx2, ny2, NoiseTime * 1.35f) * 0.40f : 0.0f;

            float forwardBias = 0.0f;
            if (moveDir != Vector2.Zero)
            {
                float dot = Mathf.Max(0.0f, dir.Dot(moveDir));
                forwardBias = dot * (CurrentDeformationMag * 0.85f);
            }

            float dashStretch = 1.0f;
            if (Host.IsDodging && moveDir != Vector2.Zero)
            {
                float align = dir.Dot(moveDir);
                dashStretch = 1.0f + 0.25f * align - 0.15f * (1.0f - Mathf.Abs(align));
            }

            float r = (curR + ((nVal1 + nVal2) * CurrentDeformationMag) + forwardBias) * dashStretch;
            rawRadii[i] = Mathf.Max(curR * 0.65f, r);
        }

        var relaxedRadii = _relaxedRadii;
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < VertexCount; i++)
            {
                int prev = (i - 1 + VertexCount) % VertexCount;
                int next = (i + 1) % VertexCount;
                relaxedRadii[i] = 0.25f * rawRadii[prev] + 0.50f * rawRadii[i] + 0.25f * rawRadii[next];
            }
            Array.Copy(relaxedRadii, rawRadii, VertexCount);
        }

        for (int i = 0; i < VertexCount; i++)
        {
            float angle = i * angleStep;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            points[i] = dir * rawRadii[i];
        }

        SmoothClosedPolygonInto(points, SmoothSubdivisions, _smoothPoints);
        AssignDeformationMeshes(points, _smoothPoints, curR, updateShaderParam: true);

        UpdateNucleus(delta);
        UpdateGranules(delta);
    }

    private void UpdateDeformStar(float delta, Dictionary classDef)
    {
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;

        NoiseTime += delta * DeformationSpeed;
        float areaScale = Host.Stats != null ? Host.Stats.GetStat("area") : 1.0f;
        float curR = Host.BaseRadius * areaScale;
        CurrentDeformationMag = BaseDeformationMag * areaScale;
        Host.CurrentRadius = curR;

        int arms = Mathf.Max(1, CatalogLoader.GetInt(classDef, "deform_arms", 7));
        var points = GetControlBuffer();
        float angleStep = Mathf.Tau / (float)VertexCount;
        Vector2 vel = Host.Velocity;
        Vector2 moveDir = vel.Length() > 20.0f ? vel.Normalized() : Vector2.Zero;

        for (int i = 0; i < VertexCount; i++)
        {
            float angle = i * angleStep;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            float nx = Mathf.Cos(angle) * 1.5f;
            float ny = Mathf.Sin(angle) * 1.5f;
            float nVal = Noise != null ? Noise.GetNoise3D(nx, ny, NoiseTime) : 0.0f;
            float armVal = Mathf.Pow(Mathf.Max(0.0f, Mathf.Cos(angle * (float)arms)), 2.0f) * (CurrentDeformationMag * 0.75f);

            float forwardBias = 0.0f;
            if (moveDir != Vector2.Zero)
            {
                float dot = Mathf.Max(0.0f, dir.Dot(moveDir));
                forwardBias = dot * (CurrentDeformationMag * 0.5f);
            }

            float r = curR + (nVal * CurrentDeformationMag * 0.6f) + armVal + forwardBias;
            points[i] = dir * Mathf.Max(14.0f, r);
        }

        var smoothPoints = GetSmoothBuffer(2);
        SmoothClosedPolygonInto(points, 2, smoothPoints);
        AssignDeformationMeshes(points, smoothPoints, curR, updateShaderParam: false);

        UpdateNucleus(delta);
        UpdateGranules(delta);
    }

    public void EnsureDeformBuffers(int controlCount, int smoothCount)
    {
        if (_controlPoints.Length != controlCount)
        {
            _controlPoints = new Vector2[controlCount];
            _rawRadii = new float[controlCount];
            _relaxedRadii = new float[controlCount];
        }
        if (_smoothPoints.Length != smoothCount)
        {
            _smoothPoints = new Vector2[smoothCount];
            _uvPoints = new Vector2[smoothCount];
            _linePoints = new Vector2[smoothCount + 1];
        }
    }

    public Vector2[] GetControlBuffer()
    {
        EnsureDeformBuffers(VertexCount, VertexCount * SmoothSubdivisions);
        return _controlPoints;
    }

    public Vector2[] GetSmoothBuffer(int subdivisions)
    {
        EnsureDeformBuffers(VertexCount, VertexCount * subdivisions);
        return _smoothPoints;
    }

    public void AssignDeformationMeshes(Vector2[] controlPoints, Vector2[] smoothPoints, float curR, bool updateShaderParam)
    {
        if (Cytoplasm != null)
        {
            Cytoplasm.Polygon = smoothPoints;
            float uvDenom = Mathf.Max(24.0f, curR * 2.4f);
            for (int i = 0; i < smoothPoints.Length; i++)
            {
                _uvPoints[i] = (smoothPoints[i] / uvDenom) + new Vector2(0.5f, 0.5f);
            }
            Cytoplasm.UV = _uvPoints;
            if (updateShaderParam && Cytoplasm.Material is ShaderMaterial smat
                && (float.IsNaN(_lastRadiusParam) || Mathf.Abs(_lastRadiusParam - curR) > 0.01f))
            {
                smat.SetShaderParameter("cell_radius", curR);
                _lastRadiusParam = curR;
            }
        }

        if (Membrane != null)
        {
            Array.Copy(smoothPoints, _linePoints, smoothPoints.Length);
            _linePoints[^1] = smoothPoints[0];
            Membrane.Points = _linePoints;
        }

        if (EngulfCollider != null)
        {
            EngulfCollider.Polygon = controlPoints;
        }
    }

    public void UpdateGranules(float delta)
    {
        if (_granuleCanvas == null || _granuleCanvas.Granules.Count == 0 || Host == null)
            return;

        Vector2 vel = Host.Velocity;
        float maxOffset = Host.CurrentRadius * 0.35f;
        float expansion = Host.CurrentRadius / Host.BaseRadius;

        for (int i = 0; i < _granuleCanvas.Granules.Count; i++)
        {
            var g = _granuleCanvas.Granules[i];
            g.BrownianPhase += delta * 2.2f;

            Vector2 brownian = new Vector2(
                Mathf.Sin(g.BrownianPhase + i * 1.7f),
                Mathf.Cos(g.BrownianPhase * 1.4f + i * 2.3f)
            ) * (2.8f * expansion);

            Vector2 targetLag = -vel * g.LagSensitivity + brownian;
            if (targetLag.Length() > maxOffset)
            {
                targetLag = targetLag.Normalized() * maxOffset;
            }

            Vector2 accel = (targetLag - g.CurrentOffset) * 24.0f - g.Velocity * 7.0f;
            g.Velocity += accel * delta;
            g.CurrentOffset += g.Velocity * delta;

            _granuleCanvas.Granules[i] = g;
        }

        _granuleCanvas.QueueRedraw();
    }

    public static void SmoothClosedPolygonInto(Vector2[] pts, int subdivisions, Vector2[] dest)
    {
        int n = pts.Length;
        if (n < 4 || subdivisions <= 1)
        {
            Array.Copy(pts, dest, Math.Min(pts.Length, dest.Length));
            return;
        }

        float step = 1.0f / (float)subdivisions;
        int idx = 0;

        for (int i = 0; i < n; i++)
        {
            Vector2 p0 = pts[(i - 1 + n) % n];
            Vector2 p1 = pts[i];
            Vector2 p2 = pts[(i + 1) % n];
            Vector2 p3 = pts[(i + 2) % n];

            for (int s = 0; s < subdivisions; s++)
            {
                float t = s * step;
                float t2 = t * t;
                float t3 = t2 * t;
                dest[idx++] = 0.5f * (
                    (2.0f * p1) +
                    (-p0 + p2) * t +
                    (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * t2 +
                    (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * t3
                );
            }
        }
    }

    public void UpdateNucleus(float delta)
    {
        if (Nucleus == null || Host == null)
            return;

        Vector2 targetLag = -Host.Velocity * 0.08f;
        float maxLag = Host.CurrentRadius * 0.32f;
        if (targetLag.Length() > maxLag)
        {
            targetLag = targetLag.Normalized() * maxLag;
        }

        float springK = 48.0f;
        float damping = 9.5f;
        Vector2 accel = (targetLag - NucleusOffset) * springK - NucleusVelocity * damping;
        NucleusVelocity += accel * delta;
        NucleusOffset += NucleusVelocity * delta;
        Nucleus.Position = NucleusOffset;
    }

    public void FlashModulate(Color flashColor, float duration)
    {
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;
        if (_hitFlashTween != null && _hitFlashTween.IsValid())
            _hitFlashTween.Kill();
        Host.Modulate = flashColor;
        _hitFlashTween = Host.CreateTween();
        _hitFlashTween.TweenProperty(Host, "modulate", Colors.White, duration);
    }

    public void FlashHit(float finalDmg)
    {
        if (Cytoplasm != null && finalDmg > 0.1f && (Host == null || Host.Health > 0.0f))
        {
            if (_hitFlashTween == null || !_hitFlashTween.IsValid())
            {
                _hitFlashTween = Host?.CreateTween() ?? CreateTween();
                Cytoplasm.Modulate = new Color(2.2f, 0.6f, 0.6f, 1.0f);
                _hitFlashTween.TweenProperty(Cytoplasm, "modulate", new Color(1.0f, 1.0f, 1.0f, 1.0f), 0.1);
            }
        }
    }
}
