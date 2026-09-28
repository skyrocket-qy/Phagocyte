using Godot;
using Game.Combat;
using Game.Core;
using Game.Directors;
using Game.Enemies;
using Game.Player;

namespace Game.Directors;

/// <summary>
/// Endless overdrive environment ladder (docs/endgame.md §3.2) + pathological
/// overload afflictions (§4). Effects are cumulative per 3-minute cycle:
///   1) 15:00+ escalation;
///   2) 18:00+ cross-organ boss incursion;
///   3) 21:00+ bile-acid surge strips all armor for 3s periodically;
///   4) 24:00+ gastric acid tide shrinks the safe zone;
///   5) 27:00+ terminal composite: bile surge and acid tide coexist.
/// Extracted verbatim from GameRoot; raid spawns delegate to
/// <see cref="BossEncounterManager"/> so all boss lifecycle stays in one place.
/// </summary>
public partial class EndlessDirector : Node
{
    /// <summary>Run context (GameRoot). Must be assigned before the first physics tick.</summary>
    public IRunContext? Context { get; set; }

    /// <summary>Cross-organ raid spawns are served by the boss manager.</summary>
    public BossEncounterManager? BossManager { get; set; }

    private float _armorBreakCooldown = 0.0f;
    private float _armorBreakTimer = 0.0f;
    private float _armorBreakAmount = 0.0f;
    private float _acidTickAccumulator = 0.0f;
    private int _announcedOverdriveCycle = 0;

    /// <summary>Current 3-minute overdrive cycle (0 outside endless / before 15:00).</summary>
    public int OverdriveCycle => Context is { IsEndlessRun: true } ctx
        ? EnemySpawner.GetOverdriveCycle(ctx.EnvironmentTime)
        : 0;

    /// <summary>Ladder HP multiplier applied to enemies spawned right now.</summary>
    public float OverdriveHealthMultiplier => Context != null
        ? EnemySpawner.GetOverdriveHealthMultiplier(Context.EnvironmentTime)
        : 1.0f;

    /// <summary>Ladder speed multiplier applied to enemies spawned right now.</summary>
    public float OverdriveSpeedMultiplier => Context != null
        ? EnemySpawner.GetOverdriveSpeedMultiplier(Context.EnvironmentTime)
        : 1.0f;

    /// <summary>Shrinking acid-tide safe radius (0 = tide not active; player must stay inside).</summary>
    public float AcidSafeRadius { get; private set; } = 0.0f;

    private Line2D? _acidRing = null;
    private float _drawnAcidRadius = -1.0f;

    // --- Pathological Overload Afflictions (docs/endgame.md §4) ---
    private float _febrileBurnTimer = RunMutatorService.FebrileBurnInterval;
    private float _antigenicDriftTimer = RunMutatorService.AntigenicDriftInterval;

    /// <summary>Builds the acid-tide boundary ring on the arena background layer.</summary>
    public void SetupAcidTideRing(Node? ringParent)
    {
        if (ringParent == null)
            return;

        _acidRing = new Line2D
        {
            Name = "AcidTideRing",
            Closed = true,
            Width = 6.0f,
            DefaultColor = new Color(0.78f, 0.88f, 0.25f, 0.55f),
            Visible = false
        };
        ringParent.AddChild(_acidRing);
    }

    /// <summary>
    /// Applies run-start affliction effects (docs/endgame.md §4). The febrile and
    /// drift effects are periodic and handled by ProcessMutators.
    /// </summary>
    public void ApplyMutatorLoadout()
    {
        var ctx = Context;
        if (ctx?.IsEndlessRun != true || ctx.Player is not PlayerActor cell || cell.Stats == null)
            return;

        float speedPenalty = RunMutatorService.MoveSpeedPercentPenalty;
        if (speedPenalty < 0.0f)
        {
            cell.Stats.AddModifier("move_speed", 0.0f, speedPenalty);
            GD.Print($"[Affliction] Extreme viscosity: move speed {speedPenalty * 100.0f:F0}%.");
        }
    }

    /// <summary>
    /// Pathological Overload Afflictions periodic effects (docs/endgame.md §4):
    /// febrile burn every 5s, antigenic drift strip every 20s.
    /// </summary>
    public void ProcessMutators(float delta)
    {
        var ctx = Context;
        if (ctx?.IsEndlessRun != true || ctx.Player == null || RunMutatorService.SelectedIds.Count == 0)
            return;

        if (RunMutatorService.IsActive(RunMutatorService.FebrileConvulsion))
        {
            _febrileBurnTimer -= delta;
            if (_febrileBurnTimer <= 0.0f)
            {
                _febrileBurnTimer = RunMutatorService.FebrileBurnInterval;
                if (ctx.Player is PlayerActor fevrile && fevrile.Stats != null)
                {
                    float burn = fevrile.Stats.GetStat("max_health") * RunMutatorService.FebrileBurnHealthFraction;
                    fevrile.TakeDamage(burn);
                }
            }
        }

        if (RunMutatorService.IsActive(RunMutatorService.AntigenicDrift))
        {
            _antigenicDriftTimer -= delta;
            if (_antigenicDriftTimer <= 0.0f)
            {
                _antigenicDriftTimer = RunMutatorService.AntigenicDriftInterval;
                foreach (var enemy in EnemyActor.ActiveEnemies)
                {
                    if (GodotObject.IsInstanceValid(enemy))
                        enemy.Ailments?.ClearChannel("amp");
                }
                GD.Print("[Affliction] Antigenic drift: vulnerability marks reset.");
            }
        }
    }

    public void PhysicsTick(float delta)
    {
        var ctx = Context;
        if (ctx?.IsEndlessRun != true)
            return;

        int cycle = OverdriveCycle;
        if (cycle <= 0)
            return;

        if (cycle > _announcedOverdriveCycle)
        {
            _announcedOverdriveCycle = cycle;
            AnnounceOverdriveCycle(cycle);
        }

        // Every 3-minute cycle from 18:00: cross-organ boss incursion.
        if (cycle >= 2)
            BossManager?.ProcessBossRaids(cycle);

        // Cycle 3+: bile-acid surge strips the whole arena's armor for 3 seconds.
        if (cycle >= 3)
        {
            if (_armorBreakTimer > 0.0f)
            {
                _armorBreakTimer -= delta;
                if (_armorBreakTimer <= 0.0f && _armorBreakAmount > 0.0f)
                {
                    if (ctx.Player is PlayerActor restored)
                        restored.Stats?.AddModifier("armor", _armorBreakAmount, 0.0f);
                    _armorBreakAmount = 0.0f;
                }
            }
            else
            {
                _armorBreakCooldown -= delta;
                if (_armorBreakCooldown <= 0.0f)
                {
                    _armorBreakCooldown = 15.0f;
                    if (ctx.Player is PlayerActor cell && cell.Stats != null)
                    {
                        _armorBreakAmount = Mathf.Max(0.0f, cell.Stats.GetStat("armor"));
                        if (_armorBreakAmount > 0.0f)
                        {
                            cell.Stats.AddModifier("armor", -_armorBreakAmount, 0.0f);
                            _armorBreakTimer = 3.0f;
                            GD.Print("[Overdrive] Bile-acid surge: armor stripped for 3s.");
                        }
                    }
                }
            }
        }

        // Cycle 4+: gastric acid tide floods the arena; the safe zone shrinks.
        if (cycle >= 4)
        {
            float tideStart = EnemySpawner.OverdriveStartSeconds + 3.0f * EnemySpawner.OverdriveCycleSeconds;
            float progress = Mathf.Clamp((ctx.EnvironmentTime - tideStart) / EnemySpawner.OverdriveCycleSeconds, 0.0f, 1.0f);
            AcidSafeRadius = Mathf.Lerp(2300.0f, 850.0f, progress);
            UpdateAcidTideRing();

            if (ctx.Player != null && ctx.Player.GlobalPosition.Length() > AcidSafeRadius)
            {
                _acidTickAccumulator += delta;
                if (_acidTickAccumulator >= 1.0f)
                {
                    _acidTickAccumulator -= 1.0f;
                    if (ctx.Player is PlayerActor burned)
                    {
                        burned.TakeDamage(6.0f);
                        SlowService.ApplySlow(ctx.Player, 1.2f, 0.6f);
                    }
                }
            }
            else
            {
                _acidTickAccumulator = 0.0f;
            }
        }
    }

    private void UpdateAcidTideRing()
    {
        if (_acidRing == null)
            return;

        if (AcidSafeRadius <= 0.0f)
        {
            _acidRing.Visible = false;
            _drawnAcidRadius = -1.0f;
            return;
        }

        if (Mathf.Abs(_drawnAcidRadius - AcidSafeRadius) < 1.0f)
            return;

        _drawnAcidRadius = AcidSafeRadius;
        const int segments = 96;
        var points = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Tau * i / segments;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * AcidSafeRadius;
        }
        _acidRing.Points = points;
        _acidRing.Visible = true;
    }

    private void AnnounceOverdriveCycle(int cycle)
    {
        var ctx = Context;
        if (ctx == null)
            return;

        string title = Tr("OVERDRIVE_ALERT_TITLE");
        string desc;
        if (cycle > EnemySpawner.OverdriveCycleCount)
        {
            desc = Tr("OVERDRIVE_ALERT_TERMINAL");
        }
        else
        {
            int hpPct = Mathf.RoundToInt((OverdriveHealthMultiplier - 1.0f) * 100.0f);
            int spdPct = Mathf.RoundToInt((OverdriveSpeedMultiplier - 1.0f) * 100.0f);
            desc = TextFormatter.Format(Tr("OVERDRIVE_ALERT_FMT"), hpPct, spdPct, cycle);
        }

        ctx.HudNode?.ShowOverdriveAlert(title, desc);
        AudioManager.Instance?.PlayWaveStart();
        GD.Print($"[Overdrive] Cycle {cycle} engaged: HP ×{OverdriveHealthMultiplier:F2}, Speed ×{OverdriveSpeedMultiplier:F2}.");
    }
}

