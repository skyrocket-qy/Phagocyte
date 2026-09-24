using Godot;
using System;
using Godot.Collections;

namespace Phagocyte.Core;

/// <summary>
/// Manages audio volume, graphics display modes, and persistent configuration.
/// </summary>
public partial class SettingsManager : Node
{
    [Signal]
    public delegate void SettingsChangedEventHandler();

    private static readonly JsonStore.SavePathSlot _savePath = new("settings.json");
    public static string SavePath
    {
        get => _savePath.Value;
        set => _savePath.Value = value;
    }

    public static SettingsManager? Instance = null;

    public static float MasterVolume = 1.0f;
    public static float SfxVolume = 1.0f;
    public static float BgmVolume = 0.8f;
    public static bool Fullscreen = false;
    public static bool Vsync = true;
    public static bool ScreenShake = false;

    /// <summary>
    /// Fill-rate saver (FPS survey §1): disables 2D glow/bloom, the
    /// fullscreen microscope postprocess overlay and the animated tissue
    /// backdrop shader (flat color instead). Default off — full visuals.
    /// </summary>
    public static bool PerformanceMode = false;

    /// <summary>FPS counter overlay in the HUD (top-right). Default off.</summary>
    public static bool ShowFps = false;

    /// <summary>Frame-rate cap (0 = unlimited, the current behavior).</summary>
    public static int MaxFps = 0;

    /// <summary>
    /// Organ map mechanics master switch (docs/map.md §3): fluid drift,
    /// hazards and the fluid-arrow cues. Visual tints stay always-on.
    /// Default off so runs are mechanically neutral unless opted in.
    /// </summary>
    public static bool MapEffectsEnabled = false;

    public SettingsManager()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        Instance = this;
        LoadFromDisk();
        ApplySettings();
    }

    /// <summary>
    /// Apply runtime audio and display server settings.
    /// Single source of truth for volumes (Phase 3): the Master bus is pushed
    /// here; Sfx/Bgm channel levels are read from these statics by
    /// AudioManager at play time (all players sit on the Master bus).
    /// </summary>
    public static void ApplySettings()
    {
        // Audio Bus volumes if buses exist
        if (AudioServer.GetBusCount() > 0)
        {
            int masterIdx = AudioServer.GetBusIndex("Master");
            if (masterIdx >= 0)
            {
                float db = MasterVolume > 0.0f ? Mathf.LinearToDb(Mathf.Max(0.0001f, MasterVolume)) : -80.0f;
                AudioServer.SetBusVolumeDb(masterIdx, db);
            }
        }

        // Window display mode
        if (DisplayServer.HasFeature(DisplayServer.Feature.Subwindows))
        {
            var mode = Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed;
            DisplayServer.WindowSetMode(mode);
        }

        // VSync mode
        var vsyncMode = Vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled;
        DisplayServer.WindowSetVsyncMode(vsyncMode);

        // Frame-rate cap (0 = unlimited)
        Engine.MaxFps = MaxFps;

        KeyBindings.ApplyAll();

        if (Instance != null && IsInstanceValid(Instance))
        {
            Instance.EmitSignal(SignalName.SettingsChanged);
        }
    }

    public static void SetMasterVolume(float val)
    {
        MasterVolume = Mathf.Clamp(val, 0.0f, 1.0f);
        ApplySettings();
        SaveToDisk();
    }

    public static void SetSfxVolume(float val)
    {
        SfxVolume = Mathf.Clamp(val, 0.0f, 1.0f);
        ApplySettings();
        SaveToDisk();
    }

    public static void SetBgmVolume(float val)
    {
        BgmVolume = Mathf.Clamp(val, 0.0f, 1.0f);
        ApplySettings();
        SaveToDisk();
    }

    public static void SetFullscreen(bool enabled)
    {
        Fullscreen = enabled;
        ApplySettings();
        SaveToDisk();
    }

    public static void SetVsync(bool enabled)
    {
        Vsync = enabled;
        ApplySettings();
        SaveToDisk();
    }

    public static void SetScreenShake(bool enabled)
    {
        ScreenShake = enabled;
        ApplySettings();
        SaveToDisk();
    }

    public static void SetPerformanceMode(bool enabled)
    {
        PerformanceMode = enabled;
        ApplySettings();
        SaveToDisk();
    }

    public static void SetShowFps(bool enabled)
    {
        ShowFps = enabled;
        ApplySettings();
        SaveToDisk();
    }

    public static void SetMaxFps(int fps)
    {
        MaxFps = fps < 0 ? 0 : fps;
        ApplySettings();
        SaveToDisk();
    }

    public static void SaveToDisk()
    {
        var payload = new Godot.Collections.Dictionary<string, Variant>
        {
            { "master_volume", MasterVolume },
            { "sfx_volume", SfxVolume },
            { "bgm_volume", BgmVolume },
            { "fullscreen", Fullscreen },
            { "vsync", Vsync },
            { "screen_shake", ScreenShake },
            { "performance_mode", PerformanceMode },
            { "show_fps", ShowFps },
            { "max_fps", MaxFps },
            { "map_effects_enabled", MapEffectsEnabled },
            { "keybindings", KeyBindings.ToDict() }
        };
        JsonStore.Write(SavePath, payload);
    }

    public static void LoadFromDisk()
    {
        var d = JsonStore.Read(SavePath);
        if (d == null)
            return;

        MasterVolume = d.ContainsKey("master_volume") ? (float)d["master_volume"] : 1.0f;
        SfxVolume = d.ContainsKey("sfx_volume") ? (float)d["sfx_volume"] : 1.0f;
        BgmVolume = d.ContainsKey("bgm_volume") ? (float)d["bgm_volume"] : 0.8f;
        Fullscreen = d.ContainsKey("fullscreen") ? (bool)d["fullscreen"] : false;
        Vsync = d.ContainsKey("vsync") ? (bool)d["vsync"] : true;
        ScreenShake = d.ContainsKey("screen_shake") ? (bool)d["screen_shake"] : false;
        PerformanceMode = d.ContainsKey("performance_mode") ? (bool)d["performance_mode"] : false;
        ShowFps = d.ContainsKey("show_fps") ? (bool)d["show_fps"] : false;
        MaxFps = d.ContainsKey("max_fps") ? (int)d["max_fps"] : 0;
        MapEffectsEnabled = d.ContainsKey("map_effects_enabled") ? (bool)d["map_effects_enabled"] : false;
        if (d.TryGetValue("keybindings", out var kbVal) && kbVal.VariantType == Variant.Type.Dictionary)
            KeyBindings.FromDict(kbVal.AsGodotDictionary());
        else
            KeyBindings.ApplyAll();
    }
}
