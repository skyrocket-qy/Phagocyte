using Godot;
using Game.Core;
using Game.Stages;

namespace Game.Directors;

/// <summary>
/// Organ environment driver (docs/stages.md §3). Owns the per-run
/// <see cref="StageEnvironment"/>, ticks it through <see cref="IRunContext"/>
/// (never the <c>GameRoot</c> god object).
/// Also owns the arena backdrop/border tint (former <c>GameRoot.ConfigureStageEnvironment</c>).
/// </summary>
public partial class StageSystem : Node
{
    /// <summary>Run context (GameRoot). Must be assigned before use.</summary>
    public IRunContext? Context { get; set; }

    /// <summary>Active organ fluid mechanics acting directly on the player cell.</summary>
    public StageEnvironment? Current { get; private set; }

    /// <summary>GDScript/HUD-friendly view of the active organ environment id.</summary>
    public string EnvironmentId => Current?.StageId ?? "";

    /// <summary>
    /// Builds the organ environment for this run. Call once from _Ready.
    /// Gated by <see cref="SettingsManager.StageEffectsEnabled"/>: while off,
    /// no environment is built and only the arena tints apply. Read at deploy;
    /// toggling the flag mid-run is unsupported and requires a re-deploy.
    /// </summary>
    public void Initialize(bool hardRun)
    {
        var ctx = Context;
        if (ctx == null)
            return;

        if (!SettingsManager.StageEffectsEnabled)
        {
            Current = null;
            return;
        }

        Current = StageEnvironment.ForStage(ctx.StageId);
        Current.HardMode = hardRun;
        Current.Attach(ctx);
    }

    /// <summary>Injects the stage's shader/border tint into the arena backdrop.</summary>
    public void ConfigureArenaVisuals(ColorRect? arenaBg, Line2D? arenaBorders, string stageId)
    {
        var stageInfo = GameManager.GetStageInfo(stageId);
        Color deep = stageInfo.TryGetValue("bg_color_deep", out var dVal) ? dVal.AsColor() : new Color(0.04f, 0.05f, 0.09f, 1.0f);
        Color accent = stageInfo.TryGetValue("bg_color_accent", out var aVal) ? aVal.AsColor() : new Color(0.14f, 0.04f, 0.08f, 1.0f);
        Color fiber = stageInfo.TryGetValue("fiber_color", out var fVal) ? fVal.AsColor() : new Color(0.22f, 0.18f, 0.32f, 0.35f);
        Color border = stageInfo.TryGetValue("color_code", out var cVal) ? cVal.AsColor() : new Color(0.35f, 0.45f, 0.6f, 0.65f);

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
    /// Drives the organ environment props and hazards (docs/stages.md §3).
    /// </summary>
    public void PhysicsTick(float dt)
    {
        var ctx = Context;
        if (Current == null || ctx == null)
            return;
        if (!SettingsManager.StageEffectsEnabled)
            return;

        Current.Tick(ctx, dt);
    }
}
