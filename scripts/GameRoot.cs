using Godot;
using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Directors;
using Game.Enemies;
using Game.Stages;
using Game.Stages.Vfx;
using Game.Player;
using Game.Testing;
using Game.UI;

namespace Game;

/// <summary>
/// Run orchestrator (assembler). Owns scene wiring, run-scoped configuration
/// and the per-frame dispatch; every gameplay system lives in a director
/// component below (<see cref="WaveDirectorComponent"/>,
/// <see cref="BossEncounterManager"/>, <see cref="EndlessDirector"/>,
/// <see cref="NeutralPropManager"/>, <see cref="StageSystem"/>,
/// <see cref="RunSettlementService"/>).
/// Public members of the former god object are preserved as thin facades so
/// the 45-suite test harness and HUD coupling keep working unchanged.
/// </summary>
public partial class GameRoot : Node2D, IRunContext
{
    [Export] public int ScreenCapNormal { get; set; } = EnemySpawner.MaxActiveNormal;
    [Export] public int ScreenCapSwarm { get; set; } = EnemySpawner.MaxActiveSwarm;
    [Export] public Vector2 ArenaSize { get; set; } = new(4800.0f, 4800.0f);
    [Export] public float RunGoalSeconds { get; set; } = EnemySpawner.StandardRunDuration;

    // Director components (wired from main.tscn, created on demand as fallback).
    [Export] public WaveDirectorComponent? WaveDirector { get; set; }
    [Export] public BossEncounterManager? BossManager { get; set; }
    [Export] public EndlessDirector? Overdrive { get; set; }
    [Export] public NeutralPropManager? NeutralMatter { get; set; }
    [Export] public StageSystem? StageSystem { get; set; }
    [Export] public RunSettlementService? Settlement { get; set; }

    public bool RunEnded { get; private set; } = false;

    // ---- Wave director facades (kill-driven backfill + escalation state) ----
    // Pre-_Ready reads match the original defaults (EnvironmentTime 0 → normal cap).
    public int ActiveScreenCap => WaveDirector?.ActiveScreenCap ?? ScreenCapNormal;

    public float SwarmWindowTimer => WaveDirector?.SwarmWindowTimer ?? 0.0f;

    public int ActiveEnemyCount => WaveDirector?.ActiveEnemyCount ?? 0;

    public bool EliteRaidTriggered => WaveDirector?.EliteRaidTriggered ?? false;
    public bool FirstSwarmTriggered => WaveDirector?.FirstSwarmTriggered ?? false;
    public bool ExtremeSwarmTriggered => WaveDirector?.ExtremeSwarmTriggered ?? false;

    // ---- Boss encounter facades ----
    public bool SubBossTriggered => BossManager?.SubBossTriggered ?? false;
    public bool BossLockdownActive => BossManager?.BossLockdownActive ?? false;
    public bool SubBossRewardGranted => BossManager?.SubBossRewardGranted ?? false;
    public EnemyActor? SubBoss => BossManager?.SubBoss;
    public EnemyActor? TerminalBoss => BossManager?.TerminalBoss;
    public bool TerminalBossNeutralized => BossManager?.TerminalBossNeutralized ?? false;
    public IReadOnlyList<EnemyActor> RaidBosses => BossManager?.RaidBosses ?? Array.Empty<EnemyActor>();

    // ---- Overdrive facades ----
    public int OverdriveCycle => Overdrive?.OverdriveCycle
        ?? (IsEndlessRun ? EnemySpawner.GetOverdriveCycle(EnvironmentTime) : 0);
    public float OverdriveHealthMultiplier => Overdrive?.OverdriveHealthMultiplier
        ?? EnemySpawner.GetOverdriveHealthMultiplier(EnvironmentTime);
    public float OverdriveSpeedMultiplier => Overdrive?.OverdriveSpeedMultiplier
        ?? EnemySpawner.GetOverdriveSpeedMultiplier(EnvironmentTime);
    public float AcidSafeRadius => Overdrive?.AcidSafeRadius ?? 0.0f;

    // ---- Neutral matter facades ----
    public const int MaxToxinVesicles = NeutralPropManager.MaxToxinVesicles;
    public const float NeutralSpawnInterval = NeutralPropManager.NeutralSpawnInterval;
    public int UlcerationPulses => HostUlceration.Pulses;

    // ---- Organ environment facades ----
    public StageEnvironment? Stage => StageSystem?.Current;
    public string EnvironmentId => StageSystem?.EnvironmentId ?? "";

    public CharacterBody2D? Player { get; set; }
    public Hud? HudNode { get; set; }
    IDirectorHud? IRunContext.HudNode => HudNode;
    public Node2D? EnemyContainer { get; set; }
    public Camera2D? MainCamera { get; set; }

    public ColorRect? ArenaBg { get; set; }
    public Line2D? ArenaBorders { get; set; }

    /// <summary>Whether the current organ map is unlocked in the achievement chain.</summary>
    public bool CurrentMapUnlocked => GameManager.IsMapUnlocked(StageId);

    /// <summary>Whether the current organ map's Hard (Acute Crisis) mode is unlocked.</summary>
    public bool CurrentMapHardUnlocked => GameManager.IsMapHardUnlocked(StageId);

    public float EnvironmentTime { get; set; } = 0.0f;
    public string StageId { get; set; } = "acute_wound";

    /// <summary>Run difficulty tier ("normal" / "hard"); Hard is the Acute Crisis mode.</summary>
    public string RunDifficulty { get; set; } = GameManager.SelectedDifficulty;

    /// <summary>Whether this run is fought on the Hard (Acute Crisis) tier.</summary>
    public bool IsHardRun => RunDifficulty == RunRecordManager.DifficultyHard;

    /// <summary>
    /// Endless Cytokine Storm run (docs/endgame.md §3.1): the 15:00 goal does
    /// not settle the run — the uncapped timeline keeps escalating until the
    /// membrane ruptures.
    /// </summary>
    public bool IsEndlessRun { get; private set; } = GameManager.EndlessMode;

    /// <summary>
    /// GPU swarm batching for the microscopic species (docs/spec.md §9 / TODO module 12).
    /// Enabled by default; disabling restores per-node drawing for every enemy.
    /// </summary>
    public bool SwarmBatchingEnabled { get; set; } = true;

    /// <summary>MultiMesh batch renderer for norovirus / influenza micro swarms.</summary>
    public SwarmRenderer? SwarmRenderer { get; private set; }

    public override void _Ready()
    {
        EnemySteering.ConfigureArena(ArenaSize);
        HostUlceration.Reset();
        // Run-scoped spawn modifiers: set once per run, never leaks across scenes.
        EnemySpawner.ConfigureRun(new EnemySpawner.RunConfig
        {
            HardMode = IsHardRun,
            Overdrive = IsEndlessRun
        });

        Player = GetNodeOrNull<CharacterBody2D>("Player");
        HudNode = GetNodeOrNull<Hud>("HUD");
        EnemyContainer = GetNodeOrNull<Node2D>("EnemyContainer");
        MainCamera = GetNodeOrNull<Camera2D>("Player/Camera2D");

        ArenaBg = GetNodeOrNull<ColorRect>("Background/ArenaBG");
        ArenaBorders = GetNodeOrNull<Line2D>("Background/ArenaBorders");

        ResolveDirectors();
        Overdrive?.SetupAcidTideRing(ArenaBorders?.GetParent());

        string classId = string.IsNullOrEmpty(GameManager.SelectedClass) ? "macrophage" : GameManager.SelectedClass;
        if (classId != "macrophage")
        {
            var cellScene = GameManager.GetPlayerScene(classId);
            if (cellScene != null && Player != null && MainCamera != null)
            {
                var cam = MainCamera;
                Player.RemoveChild(cam);
                Player.QueueFree();

                Player = cellScene.Instantiate<CharacterBody2D>();
                Player.Name = "Player";
                if (Player is PlayerActor classed)
                    classed.ClassId = classId;
                AddChild(Player);
                Player.AddChild(cam);
                MainCamera = cam;
            }
        }

        if (Player != null)
        {
            Player.AddToGroup("player");
            ApplyTreeLoadout();
            ApplyChamberLoadout();
            Overdrive?.ApplyMutatorLoadout();
            if (HudNode != null)
            {
                HudNode.GoalSeconds = RunGoalSeconds;
                HudNode.EndlessMode = IsEndlessRun;
                HudNode.ConnectPlayer(Player);
            }

            Settlement?.ConnectRunEvents(Player);
        }

        if (ProjectileManager.Instance == null)
        {
            var projMgr = new ProjectileManager { Name = "ProjectileManager" };
            AddChild(projMgr);
            if (Player != null)
            {
                projMgr.SetHost(Player);
            }
        }

        if (RunTelemetryManager.Instance == null)
        {
            var teleMgr = new RunTelemetryManager { Name = "RunTelemetryManager" };
            AddChild(teleMgr);
        }
        RunTelemetryManager.Instance?.StartRun();

        if (VfxManager.Instance == null)
        {
            var vfxMgr = new VfxManager { Name = "VfxManager" };
            AddChild(vfxMgr);
        }

        // Read map configuration from GM
        StageId = GameManager.SelectedMap;
        StageSystem?.ConfigureArenaVisuals(ArenaBg, ArenaBorders, StageId);

        // Organ-specific fluid mechanics & physiology acting on the player (docs/map.md §3)
        StageSystem?.Initialize(IsHardRun);

        // Fill-rate switch (FPS survey §1): applied after the map tint so the
        // stashed "full" material already carries the per-map shader params.
        BackdropQuality.ApplyTo(this, SettingsManager.PerformanceMode);

        // Initial enemy wave (balance tuning: throttled to ~30% pacing, was 35).
        if (Player != null && EnemyContainer != null)
            EnemySpawner.SpawnWave(EnemyContainer, Player, ArenaSize, EnvironmentTime, 10);

        // GPU swarm batch renderer for the microscopic species (docs/spec.md §9).
        // Parented to GameRoot (not EnemyContainer) so the map fluid mechanics never
        // drift the batch layer, and inserted before EnemyContainer so it draws
        // above the arena backdrop but under the individually drawn enemies.
        if (EnemyContainer != null)
        {
            SwarmRenderer = new SwarmRenderer { Name = "SwarmRenderer" };
            AddChild(SwarmRenderer);
            MoveChild(SwarmRenderer, EnemyContainer.GetIndex());
        }

        // Tutorial cue 1 (docs/tutorial.md §2): dormant micro-staph targets ahead
        SpawnTutorialGuides();

        // Neutral environment matter (dormant toxin vesicles)
        NeutralMatter?.SeedInitialPopulation();

        AudioManager.Instance?.PlayMapBgm(GameManager.SelectedMap, 0.6f);

        // Test-demand full-build cheat (debug builds only, --cheats=all [--godmode]).
        CheatTools.ApplyHeadedRunCheats(this);
    }

    /// <summary>
    /// Debug-only test hotkey (plain editor F5 runs): F9 maxes the deployed
    /// player, Shift+F9 adds invulnerability. No-op in release builds.
    /// </summary>
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!OS.IsDebugBuild() || @event is not InputEventKey key || !key.Pressed || key.Echo)
            return;
        if (key.Keycode == Key.F9)
        {
            if (CheatTools.MaxOutPlayer(this, godmode: key.ShiftPressed))
                GD.Print("[Cheats] Run player maxed out (F9).");
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Resolves director components from main.tscn ([Export] wiring), creating
    /// code-only fallbacks so scene-independent harnesses keep working, then
    /// injects this run context and cross-director links.
    /// </summary>
    private void ResolveDirectors()
    {
        WaveDirector ??= GetNodeOrNull<WaveDirectorComponent>("WaveDirector");
        if (WaveDirector == null)
        {
            WaveDirector = new WaveDirectorComponent { Name = "WaveDirector" };
            AddChild(WaveDirector);
        }

        BossManager ??= GetNodeOrNull<BossEncounterManager>("BossController");
        if (BossManager == null)
        {
            BossManager = new BossEncounterManager { Name = "BossController" };
            AddChild(BossManager);
        }

        Overdrive ??= GetNodeOrNull<EndlessDirector>("OverdriveSystem");
        if (Overdrive == null)
        {
            Overdrive = new EndlessDirector { Name = "OverdriveSystem" };
            AddChild(Overdrive);
        }

        NeutralMatter ??= GetNodeOrNull<NeutralPropManager>("NeutralMatterSystem");
        if (NeutralMatter == null)
        {
            NeutralMatter = new NeutralPropManager { Name = "NeutralMatterSystem" };
            AddChild(NeutralMatter);
        }

        StageSystem ??= GetNodeOrNull<StageSystem>("StageSystem");
        if (StageSystem == null)
        {
            StageSystem = new StageSystem { Name = "StageSystem" };
            AddChild(StageSystem);
        }

        Settlement ??= GetNodeOrNull<RunSettlementService>("SettlementController");
        if (Settlement == null)
        {
            Settlement = new RunSettlementService { Name = "SettlementController" };
            AddChild(Settlement);
        }

        WaveDirector.Context = this;
        BossManager.Context = this;
        Overdrive.Context = this;
        Overdrive.BossManager = BossManager;
        NeutralMatter.Context = this;
        StageSystem.Context = this;
        Settlement.Context = this;

        // 15:00 lockdown arrives as an event so the wave director never touches boss state.
        WaveDirector.TerminalPhaseReached += BossManager.EnterBossLockdown;
    }

    private void ApplyTreeLoadout()
    {
        if (Player is not PlayerActor bc)
            return;

        string classId = GameManager.SelectedClass;
        foreach (var node in PassiveTreeManager.Nodes)
        {
            if (!PassiveTreeManager.IsPlaced(classId, node.Id))
                continue;

            var skill = PassiveTreeManager.CreateSkill(node.Id);
            if (skill == null)
                continue;

            skill.Name = "TreeLoadout_" + node.Id;
            bc.AddChild(skill);
            skill.Setup(bc);
        }
    }

    /// <summary>
    /// Deploys the cell's active gear loadout (TODO Phase 1). A fresh cell
    /// has no equipment: an empty profile simply equips nothing. Entries that
    /// are stale, still locked or energy-illegal are skipped, never fatal.
    /// </summary>
    private void ApplyChamberLoadout()
    {
        if (Player is not PlayerActor bc || bc.Equipment == null)
            return;

        string classId = GameManager.SelectedClass;
        string[] slots = LoadoutManager.GetActiveSlots(classId);
        var chamber = bc.Equipment;
        for (int i = 0; i < slots.Length && i < EquipmentChamber.MaxSlots; i++)
        {
            string id = slots[i];
            if (string.IsNullOrEmpty(id) || !EquipmentUnlockManager.IsUnlocked(id))
                continue;
            if (!chamber.AddToBackpack(id))
                continue;
            chamber.Equip(id, i);
        }
    }

    /// <summary>
    /// Idle-frame swarm batch sync: enemy physics has already advanced this frame,
    /// so the MultiMesh transforms match the final positions.
    /// </summary>
    public override void _Process(double delta)
    {
        if (SwarmRenderer == null || !GodotObject.IsInstanceValid(SwarmRenderer))
            return;

        SwarmRenderer.Enabled = SwarmBatchingEnabled;
        SwarmRenderer.Sync();
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        EnvironmentTime += dt;
        AchievementManager.RecordEvent("survival_time", EnvironmentTime);

        if (RunEnded)
            return;

        // Snapshot escalation flags for the facade while components may be absent (pre-_Ready harnesses).
        WaveDirector?.PhysicsTick(dt);

        BossManager?.PhysicsTick(dt);

        // Map mechanics (environment ticks first so its fluid current is same-frame)
        StageSystem?.PhysicsTick(dt);
        Overdrive?.PhysicsTick(dt);
        Overdrive?.ProcessMutators(dt);
        NeutralMatter?.PhysicsTick(dt);

        if (BossLockdownActive)
        {
            BossManager?.CheckTerminalBossState();
            return;
        }

        WaveDirector?.ProcessBackfill();
    }

    /// <summary>
    /// Tutorial cue 1 (docs/tutorial.md §2): two dormant micro-staphylococci
    /// spawn 150px directly ahead so the opening seconds teach direct damage.
    /// </summary>
    private void SpawnTutorialGuides()
    {
        if (EnemyContainer == null || Player == null)
            return;

        for (int i = 0; i < 2; i++)
        {
            var guide = EnemySpawner.CreateEnemy("staph");
            if (guide == null)
                return;
            guide.GlobalPosition = Player.GlobalPosition + new Vector2(150.0f, i == 0 ? -28.0f : 28.0f);
            guide.ShieldCharges = 0;
            guide.MaxHealth = Mathf.Min(guide.MaxHealth, 8.0f);
            guide.Scale = new Vector2(0.8f, 0.8f);
            EnemyContainer.AddChild(guide);
            guide.ApplyStun(9999.0f); // dormant teaching target
        }
    }

    /// <summary>
    /// Ends the run and persists the settlement record (served by
    /// <see cref="RunSettlementService"/>; victory needs 15:00 survival AND
    /// terminal-boss neutralization per docs/record.md §3.1).
    /// </summary>
    public void EndRun(bool victory, string cause = "")
    {
        if (RunEnded)
            return;

        if (Settlement != null)
        {
            if (Settlement.TryEndRun(victory, cause))
                RunEnded = true;
            return;
        }

        RunEnded = true;
    }
}

/// <summary>
/// Run-scoped host ulceration meter. Tissue invaders (H. pylori) accumulate acid
/// damage; crossing thresholds degrades the whole arena.
/// </summary>
public static class HostUlceration
{
    public const int EnvironmentThreshold = 4;
    public const int SevereThreshold = 8;

    public static int Pulses { get; private set; }

    public static void RegisterPulse()
    {
        Pulses++;
    }

    public static void Reset()
    {
        Pulses = 0;
    }
}


