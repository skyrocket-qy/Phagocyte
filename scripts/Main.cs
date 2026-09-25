using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Directors;
using Phagocyte.Enemies;
using Phagocyte.Environment;
using Phagocyte.Player;
using Phagocyte.Tests;
using Phagocyte.UI;

namespace Phagocyte;

/// <summary>
/// Run orchestrator (assembler). Owns scene wiring, run-scoped configuration
/// and the per-frame dispatch; every gameplay system lives in a director
/// component below (<see cref="WaveDirectorComponent"/>,
/// <see cref="BossEncounterManager"/>, <see cref="OverdriveDirector"/>,
/// <see cref="NeutralMatterManager"/>, <see cref="OrganEnvironmentSystem"/>,
/// <see cref="RunSettlementService"/>).
/// Public members of the former god object are preserved as thin facades so
/// the 45-suite test harness and HUD coupling keep working unchanged.
/// </summary>
public partial class Main : Node2D, IRunContext
{
    public static PackedScene DefaultStaphScene => AssetLoader.Load<PackedScene>("res://scenes/enemies/staph_enemy.tscn");

    [Export] public PackedScene? StaphScene { get; set; }
    [Export] public int ScreenCapNormal { get; set; } = PathogenSpawner.MaxActiveNormal;
    [Export] public int ScreenCapSwarm { get; set; } = PathogenSpawner.MaxActiveSwarm;
    [Export] public Vector2 ArenaSize { get; set; } = new(4800.0f, 4800.0f);
    [Export] public float RunGoalSeconds { get; set; } = PathogenSpawner.StandardRunDuration;

    // Director components (wired from main.tscn, created on demand as fallback).
    [Export] public WaveDirectorComponent? WaveDirector { get; set; }
    [Export] public BossEncounterManager? BossManager { get; set; }
    [Export] public OverdriveDirector? Overdrive { get; set; }
    [Export] public NeutralMatterManager? NeutralMatter { get; set; }
    [Export] public OrganEnvironmentSystem? OrganSystem { get; set; }
    [Export] public RunSettlementService? Settlement { get; set; }

    public bool RunEnded { get; private set; } = false;

    // ---- Wave director facades (kill-driven backfill + escalation state) ----
    // Pre-_Ready reads match the original defaults (EnvironmentTime 0 → normal cap).
    public int ActiveScreenCap => WaveDirector?.ActiveScreenCap ?? ScreenCapNormal;

    public float SwarmWindowTimer => WaveDirector?.SwarmWindowTimer ?? 0.0f;

    public int ActivePathogenCount => WaveDirector?.ActivePathogenCount ?? 0;

    public bool EliteRaidTriggered => WaveDirector?.EliteRaidTriggered ?? false;
    public bool FirstSwarmTriggered => WaveDirector?.FirstSwarmTriggered ?? false;
    public bool ExtremeSwarmTriggered => WaveDirector?.ExtremeSwarmTriggered ?? false;

    // ---- Boss encounter facades ----
    public bool SubBossTriggered => BossManager?.SubBossTriggered ?? false;
    public bool BossLockdownActive => BossManager?.BossLockdownActive ?? false;
    public bool SubBossRewardGranted => BossManager?.SubBossRewardGranted ?? false;
    public BaseEnemy? SubBoss => BossManager?.SubBoss;
    public BaseEnemy? TerminalBoss => BossManager?.TerminalBoss;
    public bool TerminalBossNeutralized => BossManager?.TerminalBossNeutralized ?? false;
    public IReadOnlyList<BaseEnemy> RaidBosses => BossManager?.RaidBosses ?? Array.Empty<BaseEnemy>();

    // ---- Overdrive facades ----
    public int OverdriveCycle => Overdrive?.OverdriveCycle
        ?? (IsEndlessRun ? PathogenSpawner.GetOverdriveCycle(EnvironmentTime) : 0);
    public float OverdriveHealthMultiplier => Overdrive?.OverdriveHealthMultiplier
        ?? PathogenSpawner.GetOverdriveHealthMultiplier(EnvironmentTime);
    public float OverdriveSpeedMultiplier => Overdrive?.OverdriveSpeedMultiplier
        ?? PathogenSpawner.GetOverdriveSpeedMultiplier(EnvironmentTime);
    public float AcidSafeRadius => Overdrive?.AcidSafeRadius ?? 0.0f;

    /// <summary>
    /// World-space fluid field vector currently acting on the arena (map current
    /// + overdrive shear). Drives the tutorial fluid-ripple cues (docs/tutorial.md §2 cue 5).
    /// </summary>
    public Vector2 CurrentFluidVector { get; set; } = Vector2.Zero;

    // ---- Neutral matter facades ----
    public const int MaxToxinVesicles = NeutralMatterManager.MaxToxinVesicles;
    public const float NeutralSpawnInterval = NeutralMatterManager.NeutralSpawnInterval;
    public int UlcerationPulses => HostUlceration.Pulses;

    // ---- Organ environment facades ----
    public MapEnvironment? OrganEnvironment => OrganSystem?.Current;
    public string EnvironmentId => OrganSystem?.EnvironmentId ?? "";
    public Vector2 EnvironmentPlayerDrift => OrganSystem?.EnvironmentPlayerDrift ?? Vector2.Zero;

    public CharacterBody2D? Player { get; set; }
    public Hud? HudNode { get; set; }
    public Node2D? EnemyContainer { get; set; }
    public Camera2D? MainCamera { get; set; }

    public ColorRect? ArenaBg { get; set; }
    public Line2D? ArenaBorders { get; set; }

    /// <summary>Whether the current organ map is unlocked in the achievement chain.</summary>
    public bool CurrentMapUnlocked => GameManager.IsMapUnlocked(MapId);

    /// <summary>Whether the current organ map's Hard (Acute Crisis) mode is unlocked.</summary>
    public bool CurrentMapHardUnlocked => GameManager.IsMapHardUnlocked(MapId);

    public float EnvironmentTime { get; set; } = 0.0f;
    public string MapId { get; set; } = "acute_wound";

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
    /// Enabled by default; disabling restores per-node drawing for every pathogen.
    /// </summary>
    public bool SwarmBatchingEnabled { get; set; } = true;

    /// <summary>MultiMesh batch renderer for norovirus / influenza micro swarms.</summary>
    public PathogenSwarmRenderer? SwarmRenderer { get; private set; }

    public override void _Ready()
    {
        StaphScene ??= DefaultStaphScene;
        EnemySteering.ConfigureArena(ArenaSize);
        HostUlceration.Reset();
        // Run-scoped spawn modifiers: set once per run, never leaks across scenes.
        PathogenSpawner.ConfigureRun(new PathogenSpawner.RunConfig
        {
            HardMode = IsHardRun,
            Overdrive = IsEndlessRun
        });

        Player = GetNodeOrNull<CharacterBody2D>("Macrophage");
        HudNode = GetNodeOrNull<Hud>("HUD");
        EnemyContainer = GetNodeOrNull<Node2D>("EnemyContainer");
        MainCamera = GetNodeOrNull<Camera2D>("Macrophage/Camera2D");

        ArenaBg = GetNodeOrNull<ColorRect>("Background/ArenaBG");
        ArenaBorders = GetNodeOrNull<Line2D>("Background/ArenaBorders");

        ResolveDirectors();
        Overdrive?.SetupAcidTideRing(ArenaBorders?.GetParent());

        if (!string.IsNullOrEmpty(GameManager.SelectedClass) && GameManager.SelectedClass != "macrophage")
        {
            var cellScene = GameManager.GetCellScene(GameManager.SelectedClass);
            if (cellScene != null && Player != null && MainCamera != null)
            {
                var cam = MainCamera;
                Player.RemoveChild(cam);
                Player.QueueFree();

                Player = cellScene.Instantiate<CharacterBody2D>();
                Player.Name = "PlayerCell";
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
            Overdrive?.ApplyAfflictionLoadout();
            if (HudNode != null)
            {
                HudNode.GoalSeconds = RunGoalSeconds;
                HudNode.EndlessMode = IsEndlessRun;
                HudNode.FluidVectorProvider = () => CurrentFluidVector;
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
        MapId = GameManager.SelectedMap;
        OrganSystem?.ConfigureArenaVisuals(ArenaBg, ArenaBorders, MapId);

        // Organ-specific fluid mechanics & physiology acting on the player (docs/map.md §3)
        OrganSystem?.Initialize(IsHardRun);

        // Fill-rate switch (FPS survey §1): applied after the map tint so the
        // stashed "full" material already carries the per-map shader params.
        BackdropQuality.ApplyTo(this, SettingsManager.PerformanceMode);

        // Initial pathogen wave (balance tuning: throttled to ~30% pacing, was 35).
        if (Player != null && EnemyContainer != null)
            PathogenSpawner.SpawnWave(EnemyContainer, Player, ArenaSize, EnvironmentTime, 10);

        // GPU swarm batch renderer for the microscopic species (docs/spec.md §9).
        // Parented to Main (not EnemyContainer) so the map fluid mechanics never
        // drift the batch layer, and inserted before EnemyContainer so it draws
        // above the arena backdrop but under the individually drawn pathogens.
        if (EnemyContainer != null)
        {
            SwarmRenderer = new PathogenSwarmRenderer { Name = "PathogenSwarmRenderer" };
            AddChild(SwarmRenderer);
            MoveChild(SwarmRenderer, EnemyContainer.GetIndex());
        }

        // Tutorial cue 1 (docs/tutorial.md §2): dormant micro-staph targets ahead
        SpawnTutorialGuides();

        // Neutral environment matter (dormant toxin vesicles)
        NeutralMatter?.SeedInitialPopulation();

        AudioManager.Instance?.PlayMapBgm(GameManager.SelectedMap, 0.6f);

        // Test-demand full-build cheat (debug builds only, --cheats=all [--godmode]).
        TestCheats.ApplyHeadedRunCheats(this);
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
            if (TestCheats.MaxOutPlayer(this, godmode: key.ShiftPressed))
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

        Overdrive ??= GetNodeOrNull<OverdriveDirector>("OverdriveSystem");
        if (Overdrive == null)
        {
            Overdrive = new OverdriveDirector { Name = "OverdriveSystem" };
            AddChild(Overdrive);
        }

        NeutralMatter ??= GetNodeOrNull<NeutralMatterManager>("NeutralMatterSystem");
        if (NeutralMatter == null)
        {
            NeutralMatter = new NeutralMatterManager { Name = "NeutralMatterSystem" };
            AddChild(NeutralMatter);
        }

        OrganSystem ??= GetNodeOrNull<OrganEnvironmentSystem>("OrganEnvironmentSystem");
        if (OrganSystem == null)
        {
            OrganSystem = new OrganEnvironmentSystem { Name = "OrganEnvironmentSystem" };
            AddChild(OrganSystem);
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
        OrganSystem.Context = this;
        Settlement.Context = this;

        // 15:00 lockdown arrives as an event so the wave director never touches boss state.
        WaveDirector.TerminalPhaseReached += BossManager.EnterBossLockdown;
    }

    private void ApplyTreeLoadout()
    {
        if (Player is not BaseCell bc)
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
    /// Deploys the cell's active organelle loadout (TODO Phase 1). A fresh cell
    /// has no equipment: an empty profile simply equips nothing. Entries that
    /// are stale, still locked or energy-illegal are skipped, never fatal.
    /// </summary>
    private void ApplyChamberLoadout()
    {
        if (Player is not BaseCell bc || bc.CellOrganelleChamber == null)
            return;

        string classId = GameManager.SelectedClass;
        string[] slots = LoadoutManager.GetActiveSlots(classId);
        var chamber = bc.CellOrganelleChamber;
        for (int i = 0; i < slots.Length && i < OrganelleChamber.MaxSlots; i++)
        {
            string id = slots[i];
            if (string.IsNullOrEmpty(id) || !OrganelleUnlockManager.IsUnlocked(id))
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
        OrganSystem?.PhysicsTick(dt);
        Overdrive?.PhysicsTick(dt);
        Overdrive?.ProcessAfflictions(dt);
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
        if (EnemyContainer == null || Player == null || StaphScene == null)
            return;

        for (int i = 0; i < 2; i++)
        {
            var guide = StaphScene.Instantiate<BaseEnemy>();
            guide.GlobalPosition = Player.GlobalPosition + new Vector2(150.0f, i == 0 ? -28.0f : 28.0f);
            guide.FibrinShield = 0;
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
