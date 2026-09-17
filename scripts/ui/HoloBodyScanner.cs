using Godot;
using System;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Holographic Host Body Scanner (全息透視人體掃描儀)
/// Interactive procedural 2D medical diagnostic interface for map selection.
/// Features a cybernetic vector body silhouette, oscillating scanline,
/// interactive organ hotspots, and dynamic callout lead lines to the detail card.
/// </summary>
public partial class HoloBodyScanner : Control
{
    [Signal]
    public delegate void OrganSelectedEventHandler(string mapKey);

    [Signal]
    public delegate void OrganHoveredEventHandler(string mapKey);

    public string ActiveMapKey { get; set; } = "acute_wound";
    public string? HoveredMapKey { get; set; } = null;

    private float _time = 0.0f;
    private float _scanYRatio = 0.2f;
    private float _scanDirection = 1.0f;

    // Hotspot screen radius for mouse interaction
    private const float HotspotRadius = 26.0f;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(460, 460);
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;

        // Scanline movement
        _scanYRatio += _scanDirection * (float)delta * 0.35f;
        if (_scanYRatio > 0.95f)
        {
            _scanYRatio = 0.95f;
            _scanDirection = -1.0f;
        }
        else if (_scanYRatio < 0.05f)
        {
            _scanYRatio = 0.05f;
            _scanDirection = 1.0f;
        }

        QueueRedraw();
    }

    public void SelectOrgan(string mapKey)
    {
        if (GameManager.MapData.ContainsKey(mapKey))
        {
            ActiveMapKey = mapKey;
            QueueRedraw();
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mm)
        {
            string? foundKey = HitTestOrgan(mm.Position);
            if (foundKey != HoveredMapKey)
            {
                HoveredMapKey = foundKey;
                MouseDefaultCursorShape = foundKey != null ? CursorShape.PointingHand : CursorShape.Arrow;
                if (foundKey != null)
                {
                    EmitSignal(SignalName.OrganHovered, foundKey);
                }
                QueueRedraw();
            }
        }
        else if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            string? clickedKey = HitTestOrgan(mb.Position);
            if (clickedKey != null)
            {
                ActiveMapKey = clickedKey;
                EmitSignal(SignalName.OrganSelected, clickedKey);
                QueueRedraw();
                AcceptEvent();
            }
        }
    }

    private string? HitTestOrgan(Vector2 mousePos)
    {
        var bodyRect = GetBodyRect();
        foreach (var keyVar in GameManager.MapData.Keys)
        {
            string key = keyVar.AsString();
            var data = (Godot.Collections.Dictionary)GameManager.MapData[key];
            if (data.TryGetValue("scanner_pos", out var posVal))
            {
                var normPos = posVal.AsVector2();
                var screenPos = bodyRect.Position + normPos * bodyRect.Size;
                if (mousePos.DistanceTo(screenPos) <= HotspotRadius)
                {
                    return key;
                }
            }
        }
        return null;
    }

    private Rect2 GetBodyRect()
    {
        // Centralized rectangular viewport for the anatomical body silhouette
        float marginX = 70.0f;
        float marginTop = 45.0f;
        float marginBottom = 65.0f;
        return new Rect2(
            marginX,
            marginTop,
            Size.X - marginX * 2.0f,
            Size.Y - marginTop - marginBottom
        );
    }

    public override void _Draw()
    {
        var size = Size;
        var bodyRect = GetBodyRect();

        // 1. Background Grid & Framing
        DrawScannerBackground(size);

        // 2. Holographic Anatomical Body Silhouette
        DrawHolographicBody(bodyRect);

        // 3. Oscillating Laser Scanline
        DrawScanline(bodyRect);

        // 4. Interactive Organ Hotspots & Callout Lines
        DrawOrganHotspots(bodyRect);

        // 5. Medical Diagnostic Telemetry
        DrawDiagnosticTelemetry(size);
    }

    private void DrawScannerBackground(Vector2 size)
    {
        // Semi-transparent deep cybernetic glass background
        DrawRect(new Rect2(Vector2.Zero, size), new Color(0.02f, 0.04f, 0.08f, 0.70f), true);
        DrawRect(new Rect2(Vector2.Zero, size), new Color(0.15f, 0.40f, 0.70f, 0.40f), false, 1.5f);

        // Corner framing brackets
        float bLen = 16.0f;
        Color bColor = new Color(0.30f, 0.75f, 1.0f, 0.85f);
        // Top-Left
        DrawLine(new Vector2(0, 0), new Vector2(bLen, 0), bColor, 2.5f);
        DrawLine(new Vector2(0, 0), new Vector2(0, bLen), bColor, 2.5f);
        // Top-Right
        DrawLine(new Vector2(size.X, 0), new Vector2(size.X - bLen, 0), bColor, 2.5f);
        DrawLine(new Vector2(size.X, 0), new Vector2(size.X, bLen), bColor, 2.5f);
        // Bottom-Left
        DrawLine(new Vector2(0, size.Y), new Vector2(bLen, size.Y), bColor, 2.5f);
        DrawLine(new Vector2(0, size.Y), new Vector2(0, size.Y - bLen), bColor, 2.5f);
        // Bottom-Right
        DrawLine(new Vector2(size.X, size.Y), new Vector2(size.X - bLen, size.Y), bColor, 2.5f);
        DrawLine(new Vector2(size.X, size.Y), new Vector2(size.X, size.Y - bLen), bColor, 2.5f);

        // Subtle horizontal coordinate raster lines
        Color gridColor = new Color(0.12f, 0.32f, 0.55f, 0.12f);
        for (float y = 20; y < size.Y - 20; y += 28)
        {
            DrawLine(new Vector2(10, y), new Vector2(size.X - 10, y), gridColor, 1.0f);
        }
    }

    private void DrawHolographicBody(Rect2 b)
    {
        float cx = b.Position.X + b.Size.X * 0.5f;
        float cy = b.Position.Y;
        float h = b.Size.Y;
        float w = b.Size.X;

        Color bodyFill = new Color(0.08f, 0.28f, 0.52f, 0.22f);
        Color bodyLine = new Color(0.25f, 0.65f, 0.95f, 0.65f);
        Color boneLine = new Color(0.40f, 0.85f, 1.0f, 0.35f);

        // Head Ellipse
        Vector2 headCenter = new Vector2(cx, cy + h * 0.10f);
        Vector2 headRadius = new Vector2(w * 0.11f, h * 0.085f);
        DrawCircle(headCenter, headRadius.X, bodyFill);
        DrawArc(headCenter, headRadius.X, 0, Mathf.Tau, 36, bodyLine, 1.5f);

        // Neck
        Vector2 neckTop = headCenter + new Vector2(0, headRadius.Y * 0.7f);
        Vector2 neckBot = new Vector2(cx, cy + h * 0.20f);
        DrawLine(neckTop + new Vector2(-10, 0), neckBot + new Vector2(-12, 0), bodyLine, 1.5f);
        DrawLine(neckTop + new Vector2(10, 0), neckBot + new Vector2(12, 0), bodyLine, 1.5f);

        // Torso / Ribcage
        Vector2 leftShoulder = new Vector2(cx - w * 0.32f, cy + h * 0.22f);
        Vector2 rightShoulder = new Vector2(cx + w * 0.32f, cy + h * 0.22f);
        Vector2 leftChest = new Vector2(cx - w * 0.26f, cy + h * 0.34f);
        Vector2 rightChest = new Vector2(cx + w * 0.26f, cy + h * 0.34f);
        Vector2 leftWaist = new Vector2(cx - w * 0.19f, cy + h * 0.46f);
        Vector2 rightWaist = new Vector2(cx + w * 0.19f, cy + h * 0.46f);
        Vector2 leftPelvis = new Vector2(cx - w * 0.23f, cy + h * 0.56f);
        Vector2 rightPelvis = new Vector2(cx + w * 0.23f, cy + h * 0.56f);
        Vector2 groin = new Vector2(cx, cy + h * 0.59f);

        // Torso outline
        Vector2[] torsoPts = new[]
        {
            neckBot + new Vector2(-12, 0),
            leftShoulder, leftChest, leftWaist, leftPelvis, groin,
            rightPelvis, rightWaist, rightChest, rightShoulder,
            neckBot + new Vector2(12, 0)
        };
        DrawPolygon(torsoPts, new[] { bodyFill });
        DrawPolyline(torsoPts, bodyLine, 1.5f);

        // Arms
        Vector2 leftElbow = new Vector2(cx - w * 0.36f, cy + h * 0.38f);
        Vector2 leftWrist = new Vector2(cx - w * 0.39f, cy + h * 0.52f);
        Vector2 rightElbow = new Vector2(cx + w * 0.36f, cy + h * 0.38f);
        Vector2 rightWrist = new Vector2(cx + w * 0.39f, cy + h * 0.52f);

        DrawLine(leftShoulder, leftElbow, bodyLine, 1.5f);
        DrawLine(leftElbow, leftWrist, bodyLine, 1.5f);
        DrawLine(rightShoulder, rightElbow, bodyLine, 1.5f);
        DrawLine(rightElbow, rightWrist, bodyLine, 1.5f);

        // Spine / Central Neural Conduit
        DrawLine(neckBot, groin, boneLine, 2.0f);
        for (float sy = cy + h * 0.23f; sy < cy + h * 0.54f; sy += 16.0f)
        {
            DrawLine(new Vector2(cx - 8, sy), new Vector2(cx + 8, sy), boneLine, 1.0f);
        }

        // Rib arches
        float[] ribRatios = { 0.27f, 0.31f, 0.35f };
        foreach (var r in ribRatios)
        {
            float ry = cy + h * r;
            float rw = w * (0.22f - (r - 0.27f) * 0.3f);
            DrawArc(new Vector2(cx, ry), rw, -0.3f, 3.44f, 16, boneLine, 1.0f);
        }

        // Legs
        Vector2 leftKnee = new Vector2(cx - w * 0.17f, cy + h * 0.77f);
        Vector2 rightKnee = new Vector2(cx + w * 0.17f, cy + h * 0.77f);
        Vector2 leftAnkle = new Vector2(cx - w * 0.15f, cy + h * 0.95f);
        Vector2 rightAnkle = new Vector2(cx + w * 0.15f, cy + h * 0.95f);

        DrawLine(leftPelvis, leftKnee, bodyLine, 1.5f);
        DrawLine(leftKnee, leftAnkle, bodyLine, 1.5f);
        DrawLine(rightPelvis, rightKnee, bodyLine, 1.5f);
        DrawLine(rightKnee, rightAnkle, bodyLine, 1.5f);
    }

    private void DrawScanline(Rect2 b)
    {
        float sy = b.Position.Y + b.Size.Y * _scanYRatio;
        Color scanColor = new Color(0.2f, 0.95f, 1.0f, 0.75f);
        Color glowColor = new Color(0.1f, 0.75f, 0.9f, 0.18f);

        // Glow band
        DrawRect(new Rect2(b.Position.X - 20, sy - 4, b.Size.X + 40, 8), glowColor, true);
        // Sharp scan beam
        DrawLine(new Vector2(b.Position.X - 30, sy), new Vector2(b.Position.X + b.Size.X + 30, sy), scanColor, 1.5f);
    }

    private void DrawOrganHotspots(Rect2 bodyRect)
    {
        Vector2 activePos = Vector2.Zero;
        Color activeColor = new Color("#2a9d8f");

        // First pass: Draw inactive & hovered nodes
        foreach (var keyVar in GameManager.MapData.Keys)
        {
            string key = keyVar.AsString();
            var data = (Godot.Collections.Dictionary)GameManager.MapData[key];
            var normPos = data.TryGetValue("scanner_pos", out var spVal) ? spVal.AsVector2() : new Vector2(0.5f, 0.5f);
            var screenPos = bodyRect.Position + normPos * bodyRect.Size;
            var nodeColor = data.TryGetValue("color_code", out var ccVal) ? ccVal.AsColor() : new Color(0.3f, 0.7f, 1.0f);

            bool isActive = key == ActiveMapKey;
            bool isHovered = key == HoveredMapKey;

            if (isActive)
            {
                activePos = screenPos;
                activeColor = nodeColor;
            }

            // Outer ring
            float pulse = Mathf.Sin(_time * 4.0f + normPos.Y * 10.0f) * 2.0f;
            float r = isActive ? 16.0f + pulse : (isHovered ? 14.0f : 10.0f);

            // Node core
            DrawCircle(screenPos, r, new Color(nodeColor.R, nodeColor.G, nodeColor.B, isActive ? 0.35f : 0.18f));
            DrawArc(screenPos, r, 0, Mathf.Tau, 24, nodeColor, isActive ? 2.5f : 1.2f);

            if (isHovered && !isActive)
            {
                DrawArc(screenPos, r + 4.0f, 0, Mathf.Tau, 16, new Color(1, 1, 1, 0.6f), 1.0f);
            }

            // Small center dot
            DrawCircle(screenPos, 3.5f, nodeColor);
        }

        // Second pass: Draw active hotspot high-tech targeting reticle and callout line
        if (activePos != Vector2.Zero)
        {
            // Target Crosshair corners
            float reticleSize = 24.0f;
            float cLen = 6.0f;
            Color retColor = new Color(activeColor.R, activeColor.G, activeColor.B, 0.90f);

            // [ + ] corners around active organ
            DrawLine(activePos + new Vector2(-reticleSize, -reticleSize), activePos + new Vector2(-reticleSize + cLen, -reticleSize), retColor, 2.0f);
            DrawLine(activePos + new Vector2(-reticleSize, -reticleSize), activePos + new Vector2(-reticleSize, -reticleSize + cLen), retColor, 2.0f);

            DrawLine(activePos + new Vector2(reticleSize, -reticleSize), activePos + new Vector2(reticleSize - cLen, -reticleSize), retColor, 2.0f);
            DrawLine(activePos + new Vector2(reticleSize, -reticleSize), activePos + new Vector2(reticleSize, -reticleSize + cLen), retColor, 2.0f);

            DrawLine(activePos + new Vector2(-reticleSize, reticleSize), activePos + new Vector2(-reticleSize + cLen, reticleSize), retColor, 2.0f);
            DrawLine(activePos + new Vector2(-reticleSize, reticleSize), activePos + new Vector2(-reticleSize, reticleSize - cLen), retColor, 2.0f);

            DrawLine(activePos + new Vector2(reticleSize, reticleSize), activePos + new Vector2(reticleSize - cLen, reticleSize), retColor, 2.0f);
            DrawLine(activePos + new Vector2(reticleSize, reticleSize), activePos + new Vector2(reticleSize, reticleSize - cLen), retColor, 2.0f);

            // Expanding sonar ring
            float expandingR = (Mathf.PosMod(_time * 30.0f, 40.0f));
            float expandAlpha = 1.0f - (expandingR / 40.0f);
            DrawArc(activePos, expandingR, 0, Mathf.Tau, 32, new Color(retColor.R, retColor.G, retColor.B, expandAlpha * 0.7f), 1.5f);

            // Callout Lead Line connecting Organ -> Left edge (targeting DetailPanel)
            Vector2 elbowPos = new Vector2(bodyRect.Position.X - 25.0f, activePos.Y);
            Vector2 exitPos = new Vector2(0.0f, activePos.Y);

            // Dynamic segmented lead line
            DrawLine(activePos, elbowPos, retColor, 2.0f);
            DrawLine(elbowPos, exitPos, retColor, 2.0f);

            // Lead line terminal indicator nodes
            DrawCircle(elbowPos, 3.5f, retColor);
            DrawCircle(exitPos + new Vector2(4, 0), 4.0f, new Color(1, 1, 1, 0.9f));
        }
    }

    private void DrawDiagnosticTelemetry(Vector2 size)
    {
        // Top medical telemetry bar
        DrawString(
            ThemeDB.FallbackFont,
            new Vector2(16, 26),
            "HOST BIO-SCAN // VASCULAR NETWORK",
            HorizontalAlignment.Left,
            -1,
            12,
            new Color(0.4f, 0.75f, 0.95f, 0.85f)
        );

        DrawString(
            ThemeDB.FallbackFont,
            new Vector2(size.X - 120, 26),
            "FREQ 440MHz",
            HorizontalAlignment.Left,
            -1,
            11,
            new Color(0.3f, 0.6f, 0.8f, 0.60f)
        );

        // Bottom inspection readout
        string activeKey = ActiveMapKey;
        if (GameManager.MapData.ContainsKey(activeKey))
        {
            var data = (Godot.Collections.Dictionary)GameManager.MapData[activeKey];
            string organ = data.TryGetValue("organ_key", out var okVal) ? TranslationServer.Translate(okVal.AsString()) : "ORGAN";
            string name = data.TryGetValue("name_key", out var nkVal) ? TranslationServer.Translate(nkVal.AsString()) : activeKey;
            string subtitle = data.TryGetValue("subtitle_key", out var skVal) ? TranslationServer.Translate(skVal.AsString()) : "";
            Color c = data.TryGetValue("color_code", out var ccVal) ? ccVal.AsColor() : new Color(0.3f, 0.8f, 1.0f);

            string line1 = $"► TARGET: {organ} · {name}";
            string line2 = $"LOC: {subtitle} | INFECTION SEVERITY: HIGH";

            DrawString(
                ThemeDB.FallbackFont,
                new Vector2(16, size.Y - 26),
                line1,
                HorizontalAlignment.Left,
                -1,
                12,
                c
            );

            DrawString(
                ThemeDB.FallbackFont,
                new Vector2(16, size.Y - 10),
                line2,
                HorizontalAlignment.Left,
                -1,
                11,
                new Color(0.7f, 0.8f, 0.9f, 0.65f)
            );
        }
    }
}
