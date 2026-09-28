using Godot;
using Phagocyte.Core;
using Phagocyte.Map;

namespace Phagocyte.Directors;

/// <summary>
/// Organ environment driver (docs/map.md §3). Owns the per-run
/// <see cref="MapEnvironment"/>, ticks it through <see cref="IRunContext"/>
/// (never the <c>Main</c> god object).
/// Also owns the arena backdrop/border tint (former <c>Main.ConfigureMapEnvironment</c>).
/// </summary>
public partial class OrganEnvironmentSystem : Node
{
    /// <summary>Run context (Main). Must be assigned before use.</summary>
    public IRunContext? Context { get; set; }

    /// <summary>Active organ fluid mechanics acting directly on the player cell.</summary>
    public MapEnvironment? Current { get; private set; }

    /// <summary>GDScript/HUD-friendly view of the active organ environment id.</summary>
    public string EnvironmentId => Current?.MapId ?? "";

    /// <summary>
    /// Builds the organ environment for this run. Call once from _Ready.
    /// Gated by <see cref="SettingsManager.MapEffectsEnabled"/>: while off,
    /// no environment is built and only the arena tints apply. Read at deploy;
    /// toggling the flag mid-run is unsupported and requires a re-deploy.
    /// </summary>
    public void Initialize(bool hardRun)
    {
        var ctx = Context;
        if (ctx == null)
            return;

        if (!SettingsManager.MapEffectsEnabled)
        {
            Current = null;
            return;
        }

        Current = MapEnvironment.ForMap(ctx.MapId);
        Current.HardMode = hardRun;
        Current.Attach(ctx);
    }

    /// <summary>Injects the map's shader/border tint into the arena backdrop.</summary>
    public void ConfigureArenaVisuals(ColorRect? arenaBg, Line2D? arenaBorders, string mapId)
    {
        var mapInfo = GameManager.GetMapInfo(mapId);
        Color deep = mapInfo.TryGetValue("bg_color_deep", out var dVal) ? dVal.AsColor() : new Color(0.04f, 0.05f, 0.09f, 1.0f);
        Color accent = mapInfo.TryGetValue("bg_color_accent", out var aVal) ? aVal.AsColor() : new Color(0.14f, 0.04f, 0.08f, 1.0f);
        Color fiber = mapInfo.TryGetValue("fiber_color", out var fVal) ? fVal.AsColor() : new Color(0.22f, 0.18f, 0.32f, 0.35f);
        Color border = mapInfo.TryGetValue("color_code", out var cVal) ? cVal.AsColor() : new Color(0.35f, 0.45f, 0.6f, 0.65f);

        if (arenaBg != null && arenaBg.Material is ShaderMaterial sm)
        {
            sm.SetShaderParameter("bg_color_deep", deep);
            sm.SetShaderParameter("bg_color_accent", accent);
            sm.SetShaderParameter("fiber_color", fiber);
        }

        if (arenaBorders != null)
        {
            arenaBorders.DefaultColor = border;
        }
    }

    /// <summary>
    /// Drives the organ environment props and hazards (docs/map.md §3).
    /// </summary>
    public void PhysicsTick(float dt)
    {
        var ctx = Context;
        if (Current == null || ctx == null)
            return;
        if (!SettingsManager.MapEffectsEnabled)
            return;

        Current.Tick(ctx, dt);
    }
}
