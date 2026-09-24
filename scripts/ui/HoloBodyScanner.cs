using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Core;

namespace Phagocyte.UI;

/// <summary>
/// Holographic Host Body Scanner (全息透視人體掃描儀)
/// High-precision 2D layered medical diagnostic interface for map selection.
/// Features high-fidelity transparent cybernetic human body, additive-blended
/// anatomical organ overlays with breathing pulse tweens, laser scanline,
/// target reticles, and dynamic HUD callout lead lines to the detail card.
/// </summary>
public partial class HoloBodyScanner : Control
{
    [Signal]
    public delegate void OrganSelectedEventHandler(string mapKey);

    public string ActiveMapKey { get; set; } = "acute_wound";
    public string? HoveredMapKey { get; set; } = null;

    private float _time = 0.0f;
    private float _scanYRatio = 0.2f;
    private float _scanDirection = 1.0f;

    // Hotspot screen radius for mouse interaction
    private const float HotspotRadius = 26.0f;

    // Child display nodes
    private Control? _bodyContainer;
    private TextureRect? _baseTextureRect;
    private readonly Dictionary<string, TextureRect> _overlayRects = new();
    private readonly Dictionary<string, float> _fadeAlphas = new()
    {
        { "acute_wound", 0.0f },
        { "alveolar_space", 0.0f },
        { "hepatic_sinusoid", 0.0f },
        { "gastric_lumen", 0.0f },
        { "blood_brain_barrier", 0.0f }
    };

    // Internal HUD overlay control to render on top of textures
    private partial class HoloHudOverlay : Control
    {
        public HoloBodyScanner? Scanner { get; set; }

        public override void _Draw()
        {
            Scanner?.RenderHud(this);
        }
    }

    private HoloHudOverlay? _hudOverlay;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(430, 420);
        MouseFilter = MouseFilterEnum.Stop;

        SetupLayers();
        UpdateLayout();
    }

    private void SetupLayers()
    {
        // 1. Container for Body Base & Organ Overlays
        _bodyContainer = new Control
        {
            Name = "BodyContainer",
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_bodyContainer);

        // 2. High-precision base transparent human silhouette texture
        var baseTex = AssetLoader.Load<Texture2D>("res://assets/sprites/ui/hologram/holo_body_base.png");
        _baseTextureRect = new TextureRect
        {
            Name = "BaseBody",
            Texture = baseTex,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _bodyContainer.AddChild(_baseTextureRect);

        // 3. Additive blended anatomical organ overlays
        var overlayConfigs = new (string key, string path)[]
        {
            ("acute_wound", "res://assets/sprites/ui/hologram/skin_wound_highlight.png"),
            ("alveolar_space", "res://assets/sprites/ui/hologram/lungs_highlight.png"),
            ("hepatic_sinusoid", "res://assets/sprites/ui/hologram/liver_highlight.png"),
            ("gastric_lumen", "res://assets/sprites/ui/hologram/stomach_highlight.png"),
            ("blood_brain_barrier", "res://assets/sprites/ui/hologram/brain_highlight.png")
        };

        foreach (var (key, path) in overlayConfigs)
        {
            var tex = AssetLoader.Load<Texture2D>(path);
            var addMat = new CanvasItemMaterial
            {
                BlendMode = CanvasItemMaterial.BlendModeEnum.Add
            };

            var overlayRect = new TextureRect
            {
                Name = $"Overlay_{key}",
                Texture = tex,
                Material = addMat,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                MouseFilter = MouseFilterEnum.Ignore,
                Modulate = new Color(1, 1, 1, 0)
            };

            _bodyContainer.AddChild(overlayRect);
            _overlayRects[key] = overlayRect;
        }

        // Set initial alpha for active organ
        if (_fadeAlphas.ContainsKey(ActiveMapKey))
        {
            _fadeAlphas[ActiveMapKey] = 1.0f;
            if (_overlayRects.TryGetValue(ActiveMapKey, out var activeRect))
            {
                activeRect.Modulate = new Color(1, 1, 1, 1);
            }
        }

        // 4. HUD overlay for scanline, targeting reticle, lead lines, telemetry
        _hudOverlay = new HoloHudOverlay
        {
            Name = "HudOverlay",
            Scanner = this,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_hudOverlay);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            UpdateLayout();
        }
    }

    private void UpdateLayout()
    {
        if (_bodyContainer == null || _baseTextureRect == null || _hudOverlay == null)
            return;

        var bodyRect = GetBodyRect();
        _bodyContainer.Position = bodyRect.Position;
        _bodyContainer.Size = bodyRect.Size;

        _baseTextureRect.Position = Vector2.Zero;
        _baseTextureRect.Size = bodyRect.Size;

        foreach (var overlay in _overlayRects.Values)
        {
            overlay.Position = Vector2.Zero;
            overlay.Size = bodyRect.Size;
        }

        _hudOverlay.Position = Vector2.Zero;
        _hudOverlay.Size = Size;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _time += dt;

        // Scanline oscillation
        _scanYRatio += _scanDirection * dt * 0.35f;
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

        // Organ overlay breathing pulse (oscillates between 0.72 and 1.00)
        float pulse = 0.86f + 0.14f * Mathf.Sin(_time * 3.8f);

        foreach (var kvp in _overlayRects)
        {
            string key = kvp.Key;
            var rect = kvp.Value;
            bool isActive = (key == ActiveMapKey);
            bool isHovered = (key == HoveredMapKey);
            bool isLocked = !GameManager.IsMapUnlocked(key);

            // Locked organs render with a heavy dimmed filter instead of the healthy glow
            float activeAlpha = isLocked ? 0.16f : 1.0f;
            float hoverAlpha = isLocked ? 0.08f : 0.35f;
            float targetAlpha = isActive ? activeAlpha : (isHovered ? hoverAlpha : 0.0f);
            float currentAlpha = _fadeAlphas.TryGetValue(key, out var fa) ? fa : 0.0f;
            currentAlpha = Mathf.MoveToward(currentAlpha, targetAlpha, dt * 4.5f);
            _fadeAlphas[key] = currentAlpha;

            float finalAlpha = currentAlpha * (isActive && !isLocked ? pulse : 1.0f);
            rect.Modulate = new Color(1.0f, 1.0f, 1.0f, finalAlpha);
        }

        QueueRedraw();
        _hudOverlay?.QueueRedraw();
    }

    public void SelectOrgan(string mapKey)
    {
        if (GameManager.MapData.ContainsKey(mapKey))
        {
            ActiveMapKey = mapKey;
            QueueRedraw();
            _hudOverlay?.QueueRedraw();
        }
    }

    /// <summary>True when the given organ map is still locked by the achievement chain.</summary>
    public static bool IsOrganLocked(string mapKey)
    {
        return !GameManager.IsMapUnlocked(mapKey);
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
                QueueRedraw();
                _hudOverlay?.QueueRedraw();
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
                _hudOverlay?.QueueRedraw();
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

    public Rect2 GetBodyRect()
    {
        // 640x1000 texture aspect ratio is 0.64
        const float textureAspect = 640.0f / 1000.0f;
        float availH = Mathf.Max(100.0f, Size.Y - 76.0f);
        float availW = Mathf.Max(60.0f, Size.X - 40.0f);

        float targetH = availH;
        float targetW = targetH * textureAspect;
        if (targetW > availW)
        {
            targetW = availW;
            targetH = targetW / textureAspect;
        }

        float posX = (Size.X - targetW) * 0.5f;
        float posY = 36.0f + (availH - targetH) * 0.5f;

        return new Rect2(posX, posY, targetW, targetH);
    }

    public override void _Draw()
    {
        // Draw background glass chamber container
        DrawScannerBackground(Size);
    }

    public void RenderHud(Control canvas)
    {
        var bodyRect = GetBodyRect();

        // 1. Oscillating Laser Scanline
        DrawScanline(canvas, bodyRect);

        // 2. Interactive Organ Hotspots, Reticles & Callout Lines
        DrawOrganHotspots(canvas, bodyRect);

        // 3. Medical Diagnostic Telemetry
        DrawDiagnosticTelemetry(canvas, Size);
    }

    private void DrawScannerBackground(Vector2 size)
    {
        // Semi-transparent deep cybernetic glass chamber
        DrawRect(new Rect2(Vector2.Zero, size), new Color(0.02f, 0.04f, 0.08f, 0.72f), true);
        DrawRect(new Rect2(Vector2.Zero, size), new Color(0.15f, 0.40f, 0.70f, 0.45f), false, 1.5f);

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
        DrawLine(new Vector2(size.X, size.Y), new Vector2(size.X, bLen), bColor, 2.5f);

        // Subtle horizontal coordinate raster lines
        Color gridColor = new Color(0.12f, 0.32f, 0.55f, 0.12f);
        for (float y = 20; y < size.Y - 20; y += 28)
        {
            DrawLine(new Vector2(10, y), new Vector2(size.X - 10, y), gridColor, 1.0f);
        }
    }

    private void DrawScanline(Control canvas, Rect2 b)
    {
        float sy = b.Position.Y + b.Size.Y * _scanYRatio;
        Color scanColor = new Color(0.2f, 0.95f, 1.0f, 0.75f);
        Color glowColor = new Color(0.1f, 0.75f, 0.9f, 0.18f);

        // Glow band
        canvas.DrawRect(new Rect2(b.Position.X - 20, sy - 4, b.Size.X + 40, 8), glowColor, true);
        // Sharp scan beam
        canvas.DrawLine(new Vector2(b.Position.X - 30, sy), new Vector2(b.Position.X + b.Size.X + 30, sy), scanColor, 1.5f);
    }

    private void DrawOrganHotspots(Control canvas, Rect2 bodyRect)
    {
        Vector2 activePos = Vector2.Zero;
        Color activeColor = new Color("#2a9d8f");
        bool activeLocked = false;

        // First pass: Draw interactive hotspot indicators (subtle when inactive, invisible solid circle when active so overlay shines)
        foreach (var keyVar in GameManager.MapData.Keys)
        {
            string key = keyVar.AsString();
            var data = (Godot.Collections.Dictionary)GameManager.MapData[key];
            var normPos = data.TryGetValue("scanner_pos", out var spVal) ? spVal.AsVector2() : new Vector2(0.5f, 0.5f);
            var screenPos = bodyRect.Position + normPos * bodyRect.Size;
            var nodeColor = data.TryGetValue("color_code", out var ccVal) ? ccVal.AsColor() : new Color(0.3f, 0.7f, 1.0f);

            bool isActive = key == ActiveMapKey;
            bool isHovered = key == HoveredMapKey;
            bool isLocked = !GameManager.IsMapUnlocked(key);

            if (isActive)
            {
                activePos = screenPos;
                activeColor = isLocked ? new Color(0.95f, 0.35f, 0.35f) : nodeColor;
                activeLocked = isLocked;
            }
            else
            {
                // Inactive nodes: subtle tech ring with center micro-pip
                float r = isHovered ? 13.0f : 8.5f;
                float ringAlpha = isLocked ? (isHovered ? 0.75f : 0.26f) : (isHovered ? 0.85f : 0.35f);
                Color ringColor = isLocked
                    ? new Color(0.62f, 0.66f, 0.74f, ringAlpha)
                    : new Color(nodeColor.R, nodeColor.G, nodeColor.B, ringAlpha);

                canvas.DrawArc(screenPos, r, 0, Mathf.Tau, 20, ringColor, isHovered ? 1.8f : 1.0f);
                canvas.DrawCircle(screenPos, 2.5f, ringColor);

                if (isLocked)
                {
                    // Darkening filter + padlock overlay for locked organs
                    canvas.DrawCircle(screenPos, r + 10.0f, new Color(0.02f, 0.03f, 0.06f, isHovered ? 0.48f : 0.32f));
                    DrawPadlock(canvas, screenPos, 0.9f, new Color(0.88f, 0.90f, 0.96f, 0.9f));
                }
                else if (isHovered)
                {
                    canvas.DrawArc(screenPos, r + 4.0f, 0, Mathf.Tau, 16, new Color(1, 1, 1, 0.5f), 1.0f);
                }
            }
        }

        // Second pass: Draw active hotspot high-tech targeting reticle and callout line
        if (activePos != Vector2.Zero)
        {
            // Target Crosshair corners around the glowing organ
            float reticleSize = 26.0f;
            float cLen = 7.0f;
            Color retColor = new Color(activeColor.R, activeColor.G, activeColor.B, 0.95f);

            // [ + ] corners framing active organ
            canvas.DrawLine(activePos + new Vector2(-reticleSize, -reticleSize), activePos + new Vector2(-reticleSize + cLen, -reticleSize), retColor, 2.0f);
            canvas.DrawLine(activePos + new Vector2(-reticleSize, -reticleSize), activePos + new Vector2(-reticleSize, -reticleSize + cLen), retColor, 2.0f);

            canvas.DrawLine(activePos + new Vector2(reticleSize, -reticleSize), activePos + new Vector2(reticleSize - cLen, -reticleSize), retColor, 2.0f);
            canvas.DrawLine(activePos + new Vector2(reticleSize, -reticleSize), activePos + new Vector2(reticleSize, -reticleSize + cLen), retColor, 2.0f);

            canvas.DrawLine(activePos + new Vector2(-reticleSize, reticleSize), activePos + new Vector2(-reticleSize + cLen, reticleSize), retColor, 2.0f);
            canvas.DrawLine(activePos + new Vector2(-reticleSize, reticleSize), activePos + new Vector2(-reticleSize, reticleSize - cLen), retColor, 2.0f);

            canvas.DrawLine(activePos + new Vector2(reticleSize, reticleSize), activePos + new Vector2(reticleSize - cLen, reticleSize), retColor, 2.0f);
            canvas.DrawLine(activePos + new Vector2(reticleSize, reticleSize), activePos + new Vector2(reticleSize, -reticleSize + cLen), retColor, 2.0f);

            if (activeLocked)
            {
                // Locked target: dim disk + prominent padlock instead of a healthy pulse
                canvas.DrawCircle(activePos, 34.0f, new Color(0.02f, 0.03f, 0.06f, 0.5f));
                DrawPadlock(canvas, activePos, 1.35f, new Color(1.0f, 0.55f, 0.55f, 0.95f));
            }
            else
            {
                // Expanding sonar ring around organ
                float expandingR = (Mathf.PosMod(_time * 28.0f, 44.0f));
                float expandAlpha = 1.0f - (expandingR / 44.0f);
                canvas.DrawArc(activePos, expandingR, 0, Mathf.Tau, 36, new Color(retColor.R, retColor.G, retColor.B, expandAlpha * 0.70f), 1.5f);
            }

            // Callout Lead Line originating cleanly from left bracket of reticle
            Vector2 startPos = activePos + new Vector2(-reticleSize - 2.0f, 0.0f);
            Vector2 elbowPos = new Vector2(bodyRect.Position.X - 22.0f, activePos.Y);
            Vector2 exitPos = new Vector2(0.0f, activePos.Y);

            // Dynamic segmented lead line
            canvas.DrawLine(startPos, elbowPos, retColor, 2.0f);
            canvas.DrawLine(elbowPos, exitPos, retColor, 2.0f);

            // Lead line terminal indicator nodes
            canvas.DrawCircle(startPos, 3.0f, retColor);
            canvas.DrawCircle(elbowPos, 3.5f, retColor);
            canvas.DrawCircle(exitPos + new Vector2(4, 0), 4.5f, new Color(1, 1, 1, 0.95f));
        }
    }

    /// <summary>
    /// Vector padlock glyph (fallback fonts cannot be trusted with emoji).
    /// </summary>
    private static void DrawPadlock(Control canvas, Vector2 center, float scale, Color color)
    {
        float bodyW = 13.0f * scale;
        float bodyH = 10.0f * scale;
        var bodyRect = new Rect2(center.X - bodyW * 0.5f, center.Y - bodyH * 0.5f + 2.5f * scale, bodyW, bodyH);

        canvas.DrawRect(bodyRect, color, true);
        canvas.DrawRect(new Rect2(bodyRect.Position.X + 2.0f * scale, bodyRect.Position.Y + 3.0f * scale, 2.0f * scale, 4.0f * scale),
            new Color(0.02f, 0.03f, 0.06f, 0.9f), true);

        float shackleRadius = bodyW * 0.32f;
        canvas.DrawArc(
            new Vector2(center.X, bodyRect.Position.Y),
            shackleRadius,
            Mathf.Pi,
            Mathf.Tau,
            14,
            color,
            2.2f * scale);
    }

    private void DrawDiagnosticTelemetry(Control canvas, Vector2 size)
    {
        // Top medical telemetry bar
        canvas.DrawString(
            ThemeDB.FallbackFont,
            new Vector2(16, 26),
            Tr("SCANNER_HEADER"),
            HorizontalAlignment.Left,
            -1,
            12,
            new Color(0.4f, 0.75f, 0.95f, 0.85f)
        );

        canvas.DrawString(
            ThemeDB.FallbackFont,
            new Vector2(size.X - 120, 26),
            Tr("SCANNER_FREQ"),
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
            string organ = data.TryGetValue("organ_key", out var okVal) ? TranslationServer.Translate(okVal.AsString()) : Tr("SCANNER_ORGAN");
            string name = data.TryGetValue("name_key", out var nkVal) ? TranslationServer.Translate(nkVal.AsString()) : activeKey;
            string subtitle = data.TryGetValue("subtitle_key", out var skVal) ? TranslationServer.Translate(skVal.AsString()) : "";
            Color c = data.TryGetValue("color_code", out var ccVal) ? ccVal.AsColor() : new Color(0.3f, 0.8f, 1.0f);

            bool locked = !GameManager.IsMapUnlocked(activeKey);
            Font hudFont = canvas.GetThemeFont("font", "Label") ?? ThemeDB.FallbackFont;

            string line1 = locked
                ? $"► {Tr("SCANNER_TARGET_LOCKED")}: {organ} · {name}"
                : TextFormatter.Format(Tr("SCANNER_TARGET_FMT"), organ, name);
            Color line1Color = locked ? new Color(1.0f, 0.45f, 0.4f, 0.95f) : c;

            canvas.DrawString(
                hudFont,
                new Vector2(16, size.Y - 26),
                line1,
                HorizontalAlignment.Left,
                -1,
                12,
                line1Color
            );

            if (locked)
            {
                // Prerequisite achievement requirement readout
                canvas.DrawMultilineString(
                    hudFont,
                    new Vector2(16, size.Y - 12),
                    AchievementManager.GetMapUnlockRequirementText(activeKey),
                    HorizontalAlignment.Left,
                    size.X - 32.0f,
                    11,
                    2,
                    new Color(1.0f, 0.72f, 0.35f, 0.95f)
                );
            }
            else
            {
                canvas.DrawString(
                    hudFont,
                    new Vector2(16, size.Y - 10),
                    TextFormatter.Format(Tr("SCANNER_LOCATION_FMT"), subtitle),
                    HorizontalAlignment.Left,
                    -1,
                    11,
                    new Color(0.7f, 0.8f, 0.9f, 0.65f)
                );
            }
        }
    }
}
