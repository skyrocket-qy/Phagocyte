using Godot;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Endgame;
using Phagocyte.Enemies;
using Phagocyte.Player;
using Phagocyte.UI;

namespace Phagocyte.Directors;

/// <summary>
/// Endless overdrive environment ladder (docs/endgame.md §3.2) + pathological
/// overload afflictions (§4). Effects are cumulative per 3-minute cycle:
///   1) 15:00+ tissue-fluid suction intensifies + fibrin nets congeal;
///   2) 18:00+ respiratory shear storm thrust (+ cross-organ boss incursion);
///   3) 21:00+ bile-acid surge strips all armor for 3s periodically;
///   4) 24:00+ gastric acid tide shrinks the safe zone;
///   5) 27:00+ terminal composite: shear storm and acid tide coexist.
/// Extracted verbatim from Main; raid spawns delegate to
/// <see cref="BossEncounterManager"/> so all boss lifecycle stays in one place.
/// </summary>
public partial class OverdriveDirector : Node
{
    /// <summary>Run context (Main). Must be assigned before the first physics tick.</summary>
    public IRunContext? Context { get; set; }

    /// <summary>Cross-organ raid spawns are served by the boss manager.</summary>
    public BossEncounterManager? BossManager { get; set; }

    private float _fibrinNetTimer = 0.0f;
    private float _armorBreakCooldown = 0.0f;
    private float _armorBreakTimer = 0.0f;
    private float _armorBreakAmount = 0.0f;
    private float _acidTickAccumulator = 0.0f;
    private int _announcedOverdriveCycle = 0;

    /// <summary>Current 3-minute overdrive cycle (0 outside endless / before 15:00).</summary>
    public int OverdriveCycle => Context is { IsEndlessRun: true } ctx
        ? PathogenSpawner.GetOverdriveCycle(ctx.EnvironmentTime)
        : 0;

    /// <summary>Ladder HP multiplier applied to pathogens spawned right now.</summary>
    public float OverdriveHealthMultiplier => Context != null
        ? PathogenSpawner.GetOverdriveHealthMultiplier(Context.EnvironmentTime)
        : 1.0f;

    /// <summary>Ladder speed multiplier applied to pathogens spawned right now.</summary>
    public float OverdriveSpeedMultiplier => Context != null
        ? PathogenSpawner.GetOverdriveSpeedMultiplier(Context.EnvironmentTime)
        : 1.0f;

    /// <summary>Shrinking acid-tide safe radius (0 = tide not active; player must stay inside).</summary>
    public float AcidSafeRadius { get; private set; } = 0.0f;

    private Line2D? _acidRing = null;
    private float _drawnAcidRadius = -1.0f;

    // --- Pathological Overload Afflictions (docs/endgame.md §4) ---
    private float _febrileBurnTimer = AfflictionManager.FebrileBurnInterval;
    private float _antigenicDriftTimer = AfflictionManager.AntigenicDriftInterval;

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
    /// drift effects are periodic and handled by ProcessAfflictions.
    /// </summary>
    public void ApplyAfflictionLoadout()
    {
        var ctx = Context;
        if (ctx?.IsEndlessRun != true || ctx.Player is not BaseCell cell || cell.Stats == null)
            return;

        float speedPenalty = AfflictionManager.MoveSpeedPercentPenalty;
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
    public void ProcessAfflictions(float delta)
    {
        var ctx = Context;
        if (ctx?.IsEndlessRun != true || ctx.Player == null || AfflictionManager.SelectedIds.Count == 0)
            return;

        if (AfflictionManager.IsActive(AfflictionManager.FebrileConvulsion))
        {
            _febrileBurnTimer -= delta;
            if (_febrileBurnTimer <= 0.0f)
            {
                _febrileBurnTimer = AfflictionManager.FebrileBurnInterval;
                if (ctx.Player is BaseCell fevrile && fevrile.Stats != null)
                {
                    float burn = fevrile.Stats.GetStat("max_health") * AfflictionManager.FebrileBurnHealthFraction;
                    fevrile.TakeEnvironmentalDamage(burn);
                }
            }
        }

        if (AfflictionManager.IsActive(AfflictionManager.AntigenicDrift))
        {
            _antigenicDriftTimer -= delta;
            if (_antigenicDriftTimer <= 0.0f)
            {
                _antigenicDriftTimer = AfflictionManager.AntigenicDriftInterval;
                foreach (var enemy in BaseEnemy.ActiveEnemies)
                {
                    if (GodotObject.IsInstanceValid(enemy))
                        enemy.Ailments?.ClearOpsonization();
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

        // Cycle 1+: fibrin nets (slow-only webs) congeal across the battlefield.
        _fibrinNetTimer -= delta;
        if (_fibrinNetTimer <= 0.0f)
        {
            _fibrinNetTimer = 4.5f;
            SpawnFibrinNet();
        }

        // Cycle 2+: respiratory shear storm drives periodic violent thrust.
        if (cycle >= 2 && ctx.EnemyContainer != null)
        {
            float strength = 34.0f + 18.0f * Mathf.Min(cycle, 5);
            var shearVec = new Vector2(
                Mathf.Sin(ctx.EnvironmentTime * 1.6f) * strength,
                Mathf.Cos(ctx.EnvironmentTime * 1.1f) * strength * 0.5f);
            ctx.CurrentFluidVector += shearVec * 0.7f;

            foreach (var child in ctx.EnemyContainer.GetChildren())
            {
                if (child is not Node2D enemy)
                    continue;
                var eaten = enemy.Get("is_being_eaten");
                if (eaten.VariantType == Variant.Type.Bool && (bool)eaten)
                    continue;
                enemy.Position += shearVec * delta * 0.7f;
            }

            if (ctx.Player is BaseCell host)
                host.ApplyImpulse(shearVec * delta * 0.35f);
        }

        // Cycle 3+: bile-acid surge strips the whole arena's armor for 3 seconds.
        if (cycle >= 3)
        {
            if (_armorBreakTimer > 0.0f)
            {
                _armorBreakTimer -= delta;
                if (_armorBreakTimer <= 0.0f && _armorBreakAmount > 0.0f)
                {
                    if (ctx.Player is BaseCell restored)
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
                    if (ctx.Player is BaseCell cell && cell.Stats != null)
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
            float tideStart = PathogenSpawner.OverdriveStartSeconds + 3.0f * PathogenSpawner.OverdriveCycleSeconds;
            float progress = Mathf.Clamp((ctx.EnvironmentTime - tideStart) / PathogenSpawner.OverdriveCycleSeconds, 0.0f, 1.0f);
            AcidSafeRadius = Mathf.Lerp(2300.0f, 850.0f, progress);
            UpdateAcidTideRing();

            if (ctx.Player != null && ctx.Player.GlobalPosition.Length() > AcidSafeRadius)
            {
                _acidTickAccumulator += delta;
                if (_acidTickAccumulator >= 1.0f)
                {
                    _acidTickAccumulator -= 1.0f;
                    if (ctx.Player is BaseCell burned)
                    {
                        burned.TakeEnvironmentalDamage(6.0f);
                        burned.ApplySlow(1.2f, 0.6f);
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

    private void SpawnFibrinNet()
    {
        var ctx = Context;
        var container = ctx?.EnemyContainer;
        var player = ctx?.Player;
        if (container == null || player == null || ctx == null)
            return;

        float angle = GD.Randf() * Mathf.Tau;
        float dist = (float)GD.RandRange(220.0, 620.0);
        var pos = player.GlobalPosition + Vector2.FromAngle(angle) * dist;
        float halfW = (ctx.ArenaSize.X * 0.5f) - 120.0f;
        float halfH = (ctx.ArenaSize.Y * 0.5f) - 120.0f;
        pos.X = Mathf.Clamp(pos.X, -halfW, halfW);
        pos.Y = Mathf.Clamp(pos.Y, -halfH, halfH);

        var net = new BioHazardArea
        {
            Name = "FibrinNet",
            Duration = 9.0f,
            Radius = 90.0f,
            SlowFactor = 0.45f,
            SlowsTarget = true,
            DealsDamage = false,
            CoreColor = new Color(0.72f, 0.68f, 0.55f, 0.28f),
            RimColor = new Color(0.88f, 0.84f, 0.68f, 0.55f),
            GlobalPosition = pos
        };
        container.AddChild(net);
    }

    private void AnnounceOverdriveCycle(int cycle)
    {
        var ctx = Context;
        if (ctx == null)
            return;

        string title = Tr("OVERDRIVE_ALERT_TITLE");
        string desc;
        if (cycle > PathogenSpawner.OverdriveCycleCount)
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
