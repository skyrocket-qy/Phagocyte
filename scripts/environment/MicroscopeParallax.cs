using Godot;
using System;
using System.Collections.Generic;

namespace Phagocyte.Environment;

/// <summary>
/// Procedural Microscope Optical Depth-of-Field (DoF) System.
/// Renders out-of-focus background Erythrocytes (RBCs) and foreground lens bokeh.
/// </summary>
public partial class MicroscopeParallax : Node2D
{
    public const int RbcCount = 14;
    public const int BokehCount = 8;
    public const float ArenaExtents = 2600.0f;

    public Camera2D? CameraRef { get; set; } = null;

    public record RbcData
    {
        public Vector2 Pos;
        public float Radius;
        public float Rotation;
        public float RotSpeed;
        public Vector2 Drift;
        public Color Color;
    }

    public record BokehData
    {
        public Vector2 Pos;
        public float Radius;
        public Vector2 Drift;
        public Color Color;
    }

    private readonly List<RbcData> _rbcList = new();
    private readonly List<BokehData> _bokehList = new();

    public List<RbcData> RbcList => _rbcList;
    public List<BokehData> BokehList => _bokehList;

    public override void _Ready()
    {
        ZIndex = -5;
        SpawnErythrocytes();
        SpawnBokeh();
    }

    private void SpawnErythrocytes()
    {
        _rbcList.Clear();
        for (int i = 0; i < RbcCount; i++)
        {
            var pos = new Vector2(
                (float)GD.RandRange(-ArenaExtents, ArenaExtents),
                (float)GD.RandRange(-ArenaExtents, ArenaExtents)
            );
            float rad = (float)GD.RandRange(55.0, 100.0);
            float rot = GD.Randf() * Mathf.Tau;
            float rotSpeed = (float)GD.RandRange(-0.15, 0.15);
            var drift = new Vector2((float)GD.RandRange(15.0, 35.0), (float)GD.RandRange(-10.0, 10.0));
            var color = new Color(0.72f, 0.10f, 0.14f, (float)GD.RandRange(0.22, 0.38));
            _rbcList.Add(new RbcData
            {
                Pos = pos,
                Radius = rad,
                Rotation = rot,
                RotSpeed = rotSpeed,
                Drift = drift,
                Color = color
            });
        }
    }

    private void SpawnBokeh()
    {
        _bokehList.Clear();
        for (int i = 0; i < BokehCount; i++)
        {
            var pos = new Vector2(
                (float)GD.RandRange(-ArenaExtents, ArenaExtents),
                (float)GD.RandRange(-ArenaExtents, ArenaExtents)
            );
            float rad = (float)GD.RandRange(70.0, 140.0);
            var drift = new Vector2((float)GD.RandRange(-25.0, 25.0), (float)GD.RandRange(10.0, 40.0));
            var color = new Color(0.4f, 0.8f, 1.0f, (float)GD.RandRange(0.06, 0.15));
            _bokehList.Add(new BokehData
            {
                Pos = pos,
                Radius = rad,
                Drift = drift,
                Color = color
            });
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (CameraRef == null || !GodotObject.IsInstanceValid(CameraRef))
        {
            var vp = GetViewport();
            if (vp != null)
            {
                CameraRef = vp.GetCamera2D();
            }
        }

        foreach (var rbc in _rbcList)
        {
            rbc.Pos += rbc.Drift * dt;
            rbc.Rotation += rbc.RotSpeed * dt;
            if (rbc.Pos.X > ArenaExtents)
                rbc.Pos.X = -ArenaExtents;
            else if (rbc.Pos.X < -ArenaExtents)
                rbc.Pos.X = ArenaExtents;
            if (rbc.Pos.Y > ArenaExtents)
                rbc.Pos.Y = -ArenaExtents;
            else if (rbc.Pos.Y < -ArenaExtents)
                rbc.Pos.Y = ArenaExtents;
        }

        foreach (var b in _bokehList)
        {
            b.Pos += b.Drift * dt;
            if (b.Pos.X > ArenaExtents)
                b.Pos.X = -ArenaExtents;
            else if (b.Pos.X < -ArenaExtents)
                b.Pos.X = ArenaExtents;
            if (b.Pos.Y > ArenaExtents)
                b.Pos.Y = -ArenaExtents;
            else if (b.Pos.Y < -ArenaExtents)
                b.Pos.Y = ArenaExtents;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 camPos = CameraRef != null && GodotObject.IsInstanceValid(CameraRef) ? CameraRef.GlobalPosition : Vector2.Zero;

        // 1. Deep Parallax Erythrocytes (moves at 0.25x camera speed)
        Vector2 rbcParallaxOffset = camPos * (1.0f - 0.25f);
        foreach (var rbc in _rbcList)
        {
            Vector2 drawPos = rbc.Pos + rbcParallaxOffset;
            DrawBlurredErythrocyte(drawPos, rbc.Radius, rbc.Rotation, rbc.Color);
        }

        // 2. Foreground Bokeh (moves at 1.45x camera speed)
        Vector2 bokehParallaxOffset = camPos * (1.0f - 1.45f);
        foreach (var b in _bokehList)
        {
            Vector2 drawPos = b.Pos + bokehParallaxOffset;
            DrawBlurredBokeh(drawPos, b.Radius, b.Color);
        }
    }

    private void DrawBlurredErythrocyte(Vector2 pos, float radius, float rot, Color col)
    {
        int layers = 5;
        for (int l = layers; l > 0; l--)
        {
            float r = radius * (0.55f + 0.45f * ((float)l / (float)layers));
            float alphaMult = 0.22f * (1.0f - (float)(l - 1) / (float)layers);
            var c = new Color(col.R, col.G, col.B, col.A * alphaMult);
            float scaleY = 0.82f;
            DrawSetTransform(pos, rot, new Vector2(1.0f, scaleY));
            DrawCircle(Vector2.Zero, r, c);
            DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
        }

        DrawSetTransform(pos, rot, new Vector2(1.0f, 0.82f));
        var innerCol = new Color(col.R * 0.4f, col.G * 0.1f, col.B * 0.1f, col.A * 0.2f);
        DrawCircle(Vector2.Zero, radius * 0.32f, innerCol);
        DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
    }

    private void DrawBlurredBokeh(Vector2 pos, float radius, Color col)
    {
        int layers = 4;
        for (int l = layers; l > 0; l--)
        {
            float r = radius * ((float)l / (float)layers);
            float alpha = col.A * (0.28f * (1.0f - (float)(l - 1) / (float)layers));
            DrawCircle(pos, r, new Color(col.R, col.G, col.B, alpha));
        }
    }
}
