using Godot;
using Phagocyte.Core;
using Phagocyte.Environment;
using Phagocyte.Player;

namespace Phagocyte.Directors;

/// <summary>
/// Organ fluid-mechanics driver (docs/map.md §3). Owns the per-run
/// <see cref="MapEnvironment"/>, ticks it through <see cref="IRunContext"/>
/// (never the <c>Main</c> god object), hands the drift to the player cell and
/// applies the fluid current to the free pathogen population.
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

    /// <summary>GDScript/HUD-friendly view of the organ drift currently applied to the player.</summary>
    public Vector2 EnvironmentPlayerDrift => Current?.PlayerDrift ?? Vector2.Zero;

    /// <summary>Builds the organ environment for this run. Call once from _Ready.</summary>
    public void Initialize(bool hardRun)
    {
        var ctx = Context;
        if (ctx == null)
            return;

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
    /// Drives the organ fluid mechanics and hands their velocity offset to the
    /// player cell (docs/map.md §3), then applies the fluid current to free pathogens.
    /// </summary>
    public void PhysicsTick(float dt)
    {
        var ctx = Context;
        if (Current == null || ctx == null)
            return;

        Current.Tick(ctx, dt);

        if (ctx.Player is BaseCell cell)
            cell.EnvironmentDrift = Current.PlayerDrift;

        // The organ environment owns its fluid current (docs/map.md §3); Main only
        // applies it to the free pathogen population.
        ctx.CurrentFluidVector = Current.FluidVector;

        var container = ctx.EnemyContainer;
        if (container == null)
            return;

        foreach (var child in container.GetChildren())
        {
            if (child is not Node2D enemy)
                continue;
            if (enemy.Get("is_being_eaten").AsBool())
                continue;
            enemy.Position += ctx.CurrentFluidVector * dt;
        }
    }
}
