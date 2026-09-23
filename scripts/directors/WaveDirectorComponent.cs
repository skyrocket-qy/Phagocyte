using Godot;
using Phagocyte.Core;
using Phagocyte.Enemies;

namespace Phagocyte.Directors;

/// <summary>
/// 15:00 wave timeline + kill-driven dynamic backfill (docs/map.md §4.2).
/// Extracted from <c>Main.ProcessWaveDirector / ProcessDynamicBackfill</c>:
/// 03:00 elite raid, 06:00 swarm + elite pincer, 09:00 sub-boss (delegated to
/// <see cref="BossEncounterManager"/>), 12:00 extreme swarm, 15:00 terminal
/// lockdown (raised as <see cref="TerminalPhaseReached"/> so this component
/// never touches boss state directly).
/// </summary>
public partial class WaveDirectorComponent : Node
{
    /// <summary>Raised once when <see cref="IRunContext.RunGoalSeconds"/> is crossed.</summary>
    public event System.Action? TerminalPhaseReached;

    /// <summary>Run context (Main). Must be assigned before the first physics tick.</summary>
    public IRunContext? Context { get; set; }

    public bool EliteRaidTriggered { get; private set; } = false;
    public bool FirstSwarmTriggered { get; private set; } = false;
    public bool ExtremeSwarmTriggered { get; private set; } = false;

    public float SwarmWindowTimer { get; private set; } = 0.0f;

    private bool _terminalPhaseStarted = false;

    /// <summary>
    /// Kill-Driven Dynamic Backfill cap: endless overdrive raises it to 500,
    /// otherwise the swarm window raises it to the swarm cap (docs/endgame.md §3.4).
    /// </summary>
    public int ActiveScreenCap
    {
        get
        {
            var ctx = Context;
            if (ctx == null)
                return 0;
            if (ctx.IsEndlessRun && ctx.EnvironmentTime >= PathogenSpawner.OverdriveStartSeconds)
                return PathogenSpawner.MaxActiveEndless;
            return SwarmWindowTimer > 0.0f ? ctx.ScreenCapSwarm : ctx.ScreenCapNormal;
        }
    }

    /// <summary>
    /// Live pathogen count only (hazards, telegraphs and FX nodes never consume screen-cap slots).
    /// </summary>
    public int ActivePathogenCount
    {
        get
        {
            var container = Context?.EnemyContainer;
            if (container == null)
                return 0;

            int count = 0;
            foreach (var child in container.GetChildren())
            {
                if (child is BaseEnemy)
                    count++;
            }
            return count;
        }
    }

    /// <summary>3-minute escalation dispatcher + swarm-window decay. Same-frame order as Main had.</summary>
    public void PhysicsTick(float dt)
    {
        var ctx = Context;
        if (ctx == null)
            return;

        if (!EliteRaidTriggered && ctx.EnvironmentTime >= PathogenSpawner.EscalationInterval)
        {
            EliteRaidTriggered = true;
            TriggerEliteRaid();
        }

        if (!FirstSwarmTriggered && ctx.EnvironmentTime >= PathogenSpawner.EscalationInterval * 2.0f)
        {
            FirstSwarmTriggered = true;
            TriggerFirstSwarm();
        }

        if (!ExtremeSwarmTriggered && ctx.EnvironmentTime >= PathogenSpawner.EscalationInterval * 4.0f)
        {
            ExtremeSwarmTriggered = true;
            TriggerExtremeSwarm();
        }

        if (!_terminalPhaseStarted && ctx.EnvironmentTime >= ctx.RunGoalSeconds)
        {
            _terminalPhaseStarted = true;
            TerminalPhaseReached?.Invoke();
        }

        if (SwarmWindowTimer > 0.0f)
            SwarmWindowTimer = Mathf.Max(0.0f, SwarmWindowTimer - dt);
    }

    /// <summary>
    /// Kill-Driven Dynamic Backfill (docs/map.md §4.2): refill the deficit
    /// between the screen cap and the active pathogen count just outside the
    /// camera view. Skipped during boss lockdown (caller returns early there).
    /// </summary>
    public void ProcessBackfill()
    {
        var ctx = Context;
        var container = ctx?.EnemyContainer;
        var player = ctx?.Player;
        if (container == null || player == null || ctx == null)
            return;

        int deficit = ActiveScreenCap - ActivePathogenCount;
        if (deficit <= 0)
            return;

        int batch = Mathf.Min(deficit, PathogenSpawner.MaxBackfillPerTick);
        PathogenSpawner.Backfill(container, player, ctx.ArenaSize, GetVisibleWorldSize(), ctx.EnvironmentTime, batch);
    }

    public Vector2 GetVisibleWorldSize()
    {
        var ctx = Context;
        if (ctx is not Node node)
            return Vector2.Zero;

        var viewport = node.GetViewport();
        if (viewport == null)
            return Vector2.Zero;

        Vector2 size = viewport.GetVisibleRect().Size;
        var camera = ctx?.MainCamera;
        if (camera != null && camera.Zoom.X > 0.0f && camera.Zoom.Y > 0.0f)
        {
            size.X /= camera.Zoom.X;
            size.Y /= camera.Zoom.Y;
        }
        return size;
    }

    private void TriggerEliteRaid()
    {
        var ctx = Context;
        var container = ctx?.EnemyContainer;
        var player = ctx?.Player;
        if (container == null || player == null || ctx == null)
            return;

        // Single mechanic elite: pure positioning check.
        PathogenSpawner.SpawnElite(container, player, ctx.ArenaSize, ctx.EnvironmentTime, 1);
        AudioManager.Instance?.PlayWaveStart();
        GD.Print("[WaveDirector] 03:00 Elite raid incoming.");
    }

    private void TriggerFirstSwarm()
    {
        var ctx = Context;
        var container = ctx?.EnemyContainer;
        var player = ctx?.Player;
        if (container == null || player == null || ctx == null)
            return;

        // Elite pincer from both flanks; backfill raises the screen cap to 450 for the swarm tide.
        PathogenSpawner.SpawnElite(container, player, ctx.ArenaSize, ctx.EnvironmentTime, 1, 0.0f);
        PathogenSpawner.SpawnElite(container, player, ctx.ArenaSize, ctx.EnvironmentTime, 1, Mathf.Pi);
        SwarmWindowTimer = PathogenSpawner.SwarmWindowSeconds;
        AudioManager.Instance?.PlayWaveStart();
        GD.Print("[WaveDirector] 06:00 First swarm tide + double elite pincer.");
    }

    private void TriggerExtremeSwarm()
    {
        var ctx = Context;
        var container = ctx?.EnemyContainer;
        var player = ctx?.Player;
        if (container == null || player == null || ctx == null)
            return;

        // Backfill floods the arena to the 450 swarm cap over the next frames.
        SwarmWindowTimer = PathogenSpawner.SwarmWindowSeconds;
        PathogenSpawner.SpawnElite(container, player, ctx.ArenaSize, ctx.EnvironmentTime, 2);
        AudioManager.Instance?.PlayWaveStart();
        GD.Print("[WaveDirector] 12:00 Extreme swarm + mixed forces.");
    }
}
