namespace Phagocyte.UI;

using Godot;
using Phagocyte.Core;
using System;

public enum DamageNumberType
{
    EnemyDamage,
    PlayerDamage,
    Heal
}

/// <summary>
/// High-performance floating combat text spawner.
/// Pre-allocates a fixed struct pool (256 entries) and direct-draws via CanvasItem,
/// ensuring zero Godot node instantiation overhead during intense combat swarms.
/// </summary>
public partial class DamageNumberSpawner : CanvasLayer
{
    public static DamageNumberSpawner? Instance { get; private set; }

    public const int MaxActiveNumbers = 256;

    public struct DamageNumberEntry
    {
        public bool IsActive;
        public string Text;
        public Color Color;
        public int FontSize;
        public Vector2 WorldPosition;
        public Vector2 ScreenJitter;
        public Vector2 Velocity;
        public float Lifetime;
        public float MaxLifetime;
    }

    private readonly DamageNumberEntry[] _pool = new DamageNumberEntry[MaxActiveNumbers];
    private DamageNumberCanvas? _canvas;
    private int _activeCount;
    private int _spawnHint;

    /// <summary>Live floating-text census for the frame-spike flight recorder.</summary>
    public int ActiveNumbers => _activeCount;

    public override void _Ready()
    {
        Instance = this;
        Layer = 15;
        ProcessMode = ProcessModeEnum.Always;

        _canvas = new DamageNumberCanvas(this);
        AddChild(_canvas);
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base._ExitTree();
    }

    public static void ShowDamage(Vector2 worldPos, float amount, bool isCrit = false)
    {
        Instance?.Spawn2D(worldPos, amount, DamageNumberType.EnemyDamage, isCrit);
    }

    public static void ShowPlayerDamage(Vector2 worldPos, float amount)
    {
        Instance?.Spawn2D(worldPos, amount, DamageNumberType.PlayerDamage, false);
    }

    public static void ShowHeal(Vector2 worldPos, float amount)
    {
        Instance?.Spawn2D(worldPos, amount, DamageNumberType.Heal, false);
    }

    public static void ShowEvaded(Vector2 worldPos)
    {
        Instance?.SpawnTextInternal(worldPos, TranslationServer.Translate("COMBAT_EVADED"), new Color(0.25f, 0.95f, 0.55f), 14);
    }

    public static void ShowBlocked(Vector2 worldPos)
    {
        Instance?.SpawnTextInternal(worldPos, TranslationServer.Translate("COMBAT_BLOCKED"), new Color(0.35f, 0.75f, 1.0f), 14);
    }

    public void Spawn2D(Vector2 worldPos, float amount, DamageNumberType type, bool isCrit = false)
    {
        string text;
        Color color;
        int fontSize;

        switch (type)
        {
            case DamageNumberType.EnemyDamage:
                if (isCrit)
                {
                    text = $"{TranslationServer.Translate("COMBAT_CRIT")} {Mathf.RoundToInt(amount)}";
                    color = new Color(1.0f, 0.84f, 0.0f); // Gold
                    fontSize = 18;
                }
                else
                {
                    text = Mathf.RoundToInt(amount).ToString();
                    color = new Color(1.0f, 0.92f, 0.35f); // Bright yellow
                    fontSize = 13;
                }
                break;

            case DamageNumberType.PlayerDamage:
                text = $"-{Mathf.RoundToInt(amount)}";
                color = new Color(1.0f, 0.28f, 0.28f); // Red
                fontSize = 15;
                break;

            case DamageNumberType.Heal:
                text = $"+{Mathf.RoundToInt(amount)} HP";
                color = new Color(0.35f, 0.95f, 0.55f); // Bio-green
                fontSize = 13;
                break;

            default:
                text = Mathf.RoundToInt(amount).ToString();
                color = Colors.White;
                fontSize = 13;
                break;
        }

        SpawnTextInternal(worldPos, text, color, fontSize);
    }

    private void SpawnTextInternal(Vector2 worldPos, string text, Color color, int fontSize)
    {
        Vector2 screenJitter = new Vector2(
            (float)(Random.Shared.NextDouble() * 24.0 - 12.0),
            (float)(Random.Shared.NextDouble() * 16.0 - 8.0)
        );

        // O(1) slot claim: hint slot when free, else next free slot, else
        // round-robin eviction of the hint slot. Never scans for "oldest" —
        // under saturation that turned every spawn into a full pool walk.
        int targetSlot = _spawnHint;
        if (_pool[targetSlot].IsActive)
        {
            targetSlot = -1;
            for (int pass = 1; pass < MaxActiveNumbers; pass++)
            {
                int i = (_spawnHint + pass) % MaxActiveNumbers;
                if (!_pool[i].IsActive)
                {
                    targetSlot = i;
                    break;
                }
            }
            if (targetSlot < 0)
                targetSlot = _spawnHint;
        }

        bool wasActive = _pool[targetSlot].IsActive;
        _pool[targetSlot] = new DamageNumberEntry
        {
            IsActive = true,
            Text = text,
            Color = color,
            FontSize = fontSize,
            WorldPosition = worldPos,
            ScreenJitter = screenJitter,
            Velocity = new Vector2(0, -50.0f),
            Lifetime = 0f,
            MaxLifetime = 0.75f
        };
        if (!wasActive)
            _activeCount++;
        _spawnHint = (targetSlot + 1) % MaxActiveNumbers;
    }

    private partial class DamageNumberCanvas : Control
    {
        private readonly DamageNumberSpawner _spawner;
        private Font? _cachedFont;

        public DamageNumberCanvas(DamageNumberSpawner spawner)
        {
            _spawner = spawner;
            MouseFilter = MouseFilterEnum.Ignore;
            SetAnchorsPreset(LayoutPreset.FullRect);
        }

        public override void _Ready()
        {
            _cachedFont = ThemeDB.FallbackFont;
        }

        public override void _Process(double delta)
        {
            if (_spawner._activeCount <= 0)
                return;
            float dt = (float)delta;
            bool anyActive = false;

            for (int i = 0; i < MaxActiveNumbers; i++)
            {
                if (!_spawner._pool[i].IsActive) continue;

                anyActive = true;
                _spawner._pool[i].Lifetime += dt;
                if (_spawner._pool[i].Lifetime >= _spawner._pool[i].MaxLifetime)
                {
                    _spawner._pool[i].IsActive = false;
                    _spawner._activeCount--;
                    continue;
                }

                _spawner._pool[i].WorldPosition += _spawner._pool[i].Velocity * dt;
                _spawner._pool[i].Velocity = _spawner._pool[i].Velocity.MoveToward(Vector2.Zero, 35.0f * dt);
            }

            if (anyActive)
            {
                QueueRedraw();
            }
        }

        public override void _Draw()
        {
            if (_cachedFont == null)
            {
                _cachedFont = ThemeDB.FallbackFont;
                if (_cachedFont == null) return;
            }

            Camera2D? camera = GetViewport()?.GetCamera2D();
            Vector2 camPos = camera?.GlobalPosition ?? Vector2.Zero;
            Vector2 halfScreen = GetViewportRect().Size * 0.5f;
            Vector2 vpSize = GetViewportRect().Size;
            var bounds = new Rect2(new Vector2(-64.0f, -64.0f), vpSize + new Vector2(128.0f, 128.0f));
            // Performance mode halves font cost by dropping the outline pass.
            bool outlines = !SettingsManager.PerformanceMode;

            for (int i = 0; i < MaxActiveNumbers; i++)
            {
                if (!_spawner._pool[i].IsActive) continue;

                var entry = _spawner._pool[i];
                float progress = entry.Lifetime / entry.MaxLifetime;
                float alpha = progress > 0.55f ? (1.0f - progress) / 0.45f : 1.0f;

                Vector2 screenPos = (camera != null ? (entry.WorldPosition - camPos + halfScreen) : entry.WorldPosition) + entry.ScreenJitter;
                if (!bounds.HasPoint(screenPos))
                    continue;

                Color drawColor = new Color(entry.Color.R, entry.Color.G, entry.Color.B, alpha);

                if (outlines)
                {
                    Color outlineColor = new Color(0, 0, 0, alpha * 0.85f);
                    DrawStringOutline(_cachedFont, screenPos, entry.Text, HorizontalAlignment.Center, -1, entry.FontSize, 3, outlineColor);
                }
                DrawString(_cachedFont, screenPos, entry.Text, HorizontalAlignment.Center, -1, entry.FontSize, drawColor);
            }
        }
    }
}
