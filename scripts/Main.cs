using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Skills;
using Phagocyte.UI;
using Phagocyte.Enemies;
using Phagocyte.Combat;
using Phagocyte.Endgame;
using Phagocyte.Environment;

namespace Phagocyte;

public partial class Main : Node2D
{
    private static PackedScene? _defaultStaphScene;
    public static PackedScene DefaultStaphScene => _defaultStaphScene ??= GD.Load<PackedScene>("res://scenes/enemies/staph_enemy.tscn");

    [Export] public PackedScene? StaphScene { get; set; }
    [Export] public int ScreenCapNormal { get; set; } = PathogenSpawner.MaxActiveNormal;
    [Export] public int ScreenCapSwarm { get; set; } = PathogenSpawner.MaxActiveSwarm;
    [Export] public Vector2 ArenaSize { get; set; } = new(4800.0f, 4800.0f);
    [Export] public float RunGoalSeconds { get; set; } = PathogenSpawner.StandardRunDuration;

    public bool RunEnded { get; private set; } = false;

    // Kill-Driven Dynamic Backfill (Section 4.2)
    public int ActiveScreenCap
    {
        get
        {
            // Endless overdrive raises the on-screen cap to 500 (docs/endgame.md §3.4).
            if (IsEndlessRun && EnvironmentTime >= PathogenSpawner.OverdriveStartSeconds)
                return PathogenSpawner.MaxActiveEndless;

            return SwarmWindowTimer > 0.0f ? ScreenCapSwarm : ScreenCapNormal;
        }
    }
    public float SwarmWindowTimer { get; private set; } = 0.0f;

    /// <summary>
    /// Live pathogen count only (hazards, telegraphs and FX nodes never consume screen-cap slots).
    /// </summary>
    public int ActivePathogenCount
    {
        get
        {
            if (EnemyContainer == null)
                return 0;

            int count = 0;
            foreach (var child in EnemyContainer.GetChildren())
            {
                if (child is BaseEnemy)
                    count++;
            }
            return count;
        }
    }

    // Wave Director state (3-minute escalation loop)
    public bool EliteRaidTriggered { get; private set; } = false;
    public bool FirstSwarmTriggered { get; private set; } = false;
    public bool SubBossTriggered { get; private set; } = false;
    public bool ExtremeSwarmTriggered { get; private set; } = false;
    public bool BossLockdownActive { get; private set; } = false;
    public bool SubBossRewardGranted { get; private set; } = false;
    public BaseEnemy? SubBoss { get; private set; }
    public BaseEnemy? TerminalBoss { get; private set; }

    /// <summary>Set only when the terminal primary boss is actually killed (victory criterion).</summary>
    public bool TerminalBossNeutralized { get; private set; } = false;

    private bool _terminalPhaseStarted = false;

    // Neutral environment matter & host ulceration (Section 4.3)
    public const int MaxSenescentRbc = 12;
    public const int MaxToxinVesicles = 10;
    public const float NeutralSpawnInterval = 6.0f;

    public int UlcerationPulses => HostUlceration.Pulses;

    private float _neutralSpawnTimer = NeutralSpawnInterval;
    private float _ulcerHazardTimer = 12.0f;

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

    // --- Endless overdrive ladder & environment anomalies (docs/endgame.md §3.2) ---
    private float _fibrinNetTimer = 0.0f;
    private float _armorBreakCooldown = 0.0f;
    private float _armorBreakTimer = 0.0f;
    private float _armorBreakAmount = 0.0f;
    private float _acidTickAccumulator = 0.0f;
    private int _announcedOverdriveCycle = 0;

    /// <summary>Current 3-minute overdrive cycle (0 outside endless / before 15:00).</summary>
    public int OverdriveCycle => IsEndlessRun ? PathogenSpawner.GetOverdriveCycle(EnvironmentTime) : 0;

    /// <summary>Ladder HP multiplier applied to pathogens spawned right now.</summary>
    public float OverdriveHealthMultiplier => PathogenSpawner.GetOverdriveHealthMultiplier(EnvironmentTime);

    /// <summary>Ladder speed multiplier applied to pathogens spawned right now.</summary>
    public float OverdriveSpeedMultiplier => PathogenSpawner.GetOverdriveSpeedMultiplier(EnvironmentTime);

    /// <summary>Shrinking acid-tide safe radius (0 = tide not active; player must stay inside).</summary>
    public float AcidSafeRadius { get; private set; } = 0.0f;

    /// <summary>
    /// World-space fluid field vector currently acting on the arena (map current
    /// + overdrive shear). Drives the tutorial fluid-ripple cues (docs/tutorial.md §2 cue 5).
    /// </summary>
    public Vector2 CurrentFluidVector { get; private set; } = Vector2.Zero;

    private Line2D? _acidRing = null;
    private float _drawnAcidRadius = -1.0f;

    // --- Endless multi-boss incursions (docs/endgame.md §3.3) ---
    private readonly List<BaseEnemy> _raidBosses = new();
    private int _nextRaidCycle = 2; // first incursion at 18:00 (cycle 2)

    /// <summary>Alive multi-boss incursion bosses drawn from other organs.</summary>
    public IReadOnlyList<BaseEnemy> RaidBosses => _raidBosses;

    /// <summary>Alive boss-incursion count (GDScript-friendly scalar view).</summary>
    public int RaidBossCount => _raidBosses.Count;

    /// <summary>
    /// GPU swarm batching for the microscopic species (docs/spec.md §9 / TODO module 12).
    /// Enabled by default; disabling restores per-node drawing for every pathogen.
    /// </summary>
    public bool SwarmBatchingEnabled { get; set; } = true;

    /// <summary>MultiMesh batch renderer for norovirus / influenza micro swarms.</summary>
    public PathogenSwarmRenderer? SwarmRenderer { get; private set; }

    /// <summary>Active organ fluid mechanics acting directly on the player cell (docs/map.md §3).</summary>
    public MapEnvironment? OrganEnvironment { get; private set; }

    /// <summary>GDScript/HUD-friendly view of the active organ environment id.</summary>
    public string EnvironmentId => OrganEnvironment?.MapId ?? "";

    /// <summary>GDScript/HUD-friendly view of the organ drift currently applied to the player.</summary>
    public Vector2 EnvironmentPlayerDrift => OrganEnvironment?.PlayerDrift ?? Vector2.Zero;

    // --- Pathological Overload Afflictions (docs/endgame.md §4) ---
    private float _febrileBurnTimer = AfflictionManager.FebrileBurnInterval;
    private float _antigenicDriftTimer = AfflictionManager.AntigenicDriftInterval;

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
        SetupAcidTideRing();

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
            ApplyAfflictionLoadout();
            if (HudNode != null)
            {
                HudNode.GoalSeconds = RunGoalSeconds;
                HudNode.EndlessMode = IsEndlessRun;
                HudNode.ConnectPlayer(Player);
            }

            ConnectAchievementEvents();
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
        ConfigureMapEnvironment();

        // Organ-specific fluid mechanics & physiology acting on the player (docs/map.md §3)
        OrganEnvironment = MapEnvironment.ForMap(MapId);
        OrganEnvironment.HardMode = IsHardRun;
        OrganEnvironment.Attach(this);

        // Initial pathogen wave
        SpawnInitialWave(35);

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

        // Neutral environment matter (senescent RBCs + dormant toxin vesicles)
        SeedNeutralMatter();

        AudioManager.Instance?.PlayBgm("battle_bgm", 0.6f);
    }

    private void ApplyTreeLoadout()
    {
        if (Player is not BaseCell bc)
            return;

        string classId = GameManager.SelectedClass;
        foreach (var node in PassiveTreeManager.Nodes)
        {
            int stacks = PassiveTreeManager.GetNodeStacks(classId, node.Id);
            if (stacks <= 0)
                continue;

            var skill = PassiveTreeManager.CreateStackedSkill(node.Id, stacks);
            if (skill == null)
                continue;

            skill.Name = "TreeLoadout_" + node.Id;
            bc.AddChild(skill);
            skill.Setup(bc, -1);
        }
    }

    private void RecordTreeLevel(int level)
    {
        string classId = GameManager.SelectedClass;
        if (GameManager.ClassData.ContainsKey(classId))
            PassiveTreeManager.RecordRunLevel(classId, level);
    }

    private void ConnectAchievementEvents()
    {
        if (Player == null)
            return;

        if (Player is BaseCell bc)
        {
            bc.PathogenDigested += (pathogen, atp) =>
            {
                AchievementManager.RecordEvent("pathogen_digested", bc.DigestedCount);
            };

            bc.LevelUp += (lvl) =>
            {
                AchievementManager.RecordEvent("level_up", lvl);
                RecordTreeLevel(lvl);
            };

            bc.StatsChanged += (health, maxHealth, radiusRatio) =>
            {
                AchievementManager.RecordEvent("radius_ratio", radiusRatio);
            };

            bc.Died += () => EndRun(false, RunRecordManager.CauseMembraneRupture);
        }
        else
        {
            if (Player.HasSignal("pathogen_digested"))
            {
                Player.Connect("pathogen_digested", Callable.From((Node2D _p, float _atp) =>
                {
                    var digVal = Player.Get("digested_count");
                    AchievementManager.RecordEvent("pathogen_digested", digVal.VariantType == Variant.Type.Int ? digVal.AsInt32() : 0);
                }));
            }
            if (Player.HasSignal("level_up"))
            {
                Player.Connect("level_up", Callable.From((int lvl) =>
                {
                    AchievementManager.RecordEvent("level_up", lvl);
                    RecordTreeLevel(lvl);
                }));
            }
            if (Player.HasSignal("stats_changed"))
            {
                Player.Connect("stats_changed", Callable.From((float _h, float _mh, float rr) =>
                {
                    AchievementManager.RecordEvent("radius_ratio", rr);
                }));
            }
            if (Player.HasSignal("died"))
            {
                Player.Connect("died", Callable.From(() => EndRun(false, RunRecordManager.CauseMembraneRupture)));
            }
        }

        var sm = Player.GetNodeOrNull<SkillManager>("SkillManager");
        if (sm != null)
        {
            sm.SkillsChanged += () =>
            {
                int count = 0;
                foreach (var s in sm.ActiveSlots)
                {
                    if (s != null) count++;
                }
                AchievementManager.RecordEvent("active_skills_count", count);
            };
        }
    }

    private void ConfigureMapEnvironment()
    {
        var mapInfo = GameManager.GetMapInfo(MapId);
        Color deep = mapInfo.TryGetValue("bg_color_deep", out var dVal) ? dVal.AsColor() : new Color(0.04f, 0.05f, 0.09f, 1.0f);
        Color accent = mapInfo.TryGetValue("bg_color_accent", out var aVal) ? aVal.AsColor() : new Color(0.14f, 0.04f, 0.08f, 1.0f);
        Color fiber = mapInfo.TryGetValue("fiber_color", out var fVal) ? fVal.AsColor() : new Color(0.22f, 0.18f, 0.32f, 0.35f);
        Color border = mapInfo.TryGetValue("color_code", out var cVal) ? cVal.AsColor() : new Color(0.35f, 0.45f, 0.6f, 0.65f);

        if (ArenaBg != null && ArenaBg.Material is ShaderMaterial sm)
        {
            sm.SetShaderParameter("bg_color_deep", deep);
            sm.SetShaderParameter("bg_color_accent", accent);
            sm.SetShaderParameter("fiber_color", fiber);
        }

        if (ArenaBorders != null)
        {
            ArenaBorders.DefaultColor = border;
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

        ProcessWaveDirector();

        if (SwarmWindowTimer > 0.0f)
            SwarmWindowTimer = Mathf.Max(0.0f, SwarmWindowTimer - dt);

        // Map mechanics (environment ticks first so its fluid current is same-frame)
        ProcessOrganEnvironment(dt);
        ProcessMapMechanics(dt);
        ProcessOverdriveEnvironment(dt);
        ProcessAfflictions(dt);
        ProcessNeutralMatter(dt);
        ProcessHostUlceration(dt);

        if (BossLockdownActive)
        {
            CheckTerminalBossState();
            return;
        }

        ProcessDynamicBackfill();
    }

    /// <summary>
    /// Maintains the drifting neutral matter population (cover RBCs + toxin mines).
    /// Neutrals are not BaseEnemy nodes, so they never consume screen-cap slots.
    /// </summary>
    private void ProcessNeutralMatter(float dt)
    {
        if (EnemyContainer == null || Player == null)
            return;

        _neutralSpawnTimer -= dt;
        if (_neutralSpawnTimer > 0.0f)
            return;

        _neutralSpawnTimer = NeutralSpawnInterval;

        if (CountGroup("senescent_rbc") < MaxSenescentRbc)
            SpawnSenescentRbc();
        if (CountGroup("toxin_vesicle") < MaxToxinVesicles)
            SpawnToxinVesicle();
    }

    private void SeedNeutralMatter()
    {
        for (int i = 0; i < 4; i++)
            SpawnSenescentRbc();
        for (int i = 0; i < 3; i++)
            SpawnToxinVesicle();
    }

    private void SpawnSenescentRbc()
    {
        if (EnemyContainer == null || Player == null)
            return;

        var rbc = new SenescentRBC
        {
            GlobalPosition = GetNeutralSpawnPoint()
        };
        EnemyContainer.AddChild(rbc);
    }

    private void SpawnToxinVesicle()
    {
        if (EnemyContainer == null || Player == null)
            return;

        var vesicle = new DormantToxinVesicle
        {
            GlobalPosition = GetNeutralSpawnPoint()
        };
        EnemyContainer.AddChild(vesicle);
    }

    private Vector2 GetNeutralSpawnPoint()
    {
        Vector2 viewSize = GetVisibleWorldSize();
        float margin = (float)GD.RandRange(100.0, 220.0);
        return PathogenSpawner.GetOffscreenSpawnPoint(Player!.GlobalPosition, ArenaSize, viewSize, margin);
    }

    /// <summary>
    /// Host ulceration (docs/map.md): accumulated invader acid degrades the arena by
    /// spawning ambient acid mist near the battle as the ulceration level rises.
    /// </summary>
    private void ProcessHostUlceration(float dt)
    {
        if (EnemyContainer == null || Player == null)
            return;

        if (HostUlceration.Pulses < HostUlceration.EnvironmentThreshold)
            return;

        _ulcerHazardTimer -= dt;
        if (_ulcerHazardTimer > 0.0f)
            return;

        _ulcerHazardTimer = HostUlceration.Pulses >= HostUlceration.SevereThreshold ? 6.0f : 12.0f;

        Vector2 offset = Vector2.FromAngle(GD.Randf() * Mathf.Tau) * (float)GD.RandRange(160.0, 360.0);
        var mist = new BioHazardArea
        {
            GlobalPosition = Player.GlobalPosition + offset,
            Duration = 6.0f,
            Radius = 70.0f,
            Damage = 5.0f,
            TickInterval = 0.6f,
            SlowFactor = 0.7f,
            SlowsTarget = true,
            DealsDamage = true,
            CoreColor = new Color(0.45f, 0.22f, 0.12f, 0.30f),
            RimColor = new Color(0.85f, 0.45f, 0.20f, 0.60f)
        };
        EnemyContainer.AddChild(mist);
    }

    private int CountGroup(string group)
    {
        return GetTree().GetNodesInGroup(group).Count;
    }

    /// <summary>
    /// Kill-Driven Dynamic Backfill (docs/map.md §4.2): every frame, refill the
    /// deficit between the current screen cap and the active pathogen count,
    /// spawning just outside the camera view. Kills instantly free slots, so the
    /// faster the player clears, the faster reinforcements arrive.
    /// </summary>
    private void ProcessDynamicBackfill()
    {
        if (EnemyContainer == null || Player == null)
            return;

        int deficit = ActiveScreenCap - ActivePathogenCount;
        if (deficit <= 0)
            return;

        int batch = Mathf.Min(deficit, PathogenSpawner.MaxBackfillPerTick);
        PathogenSpawner.Backfill(EnemyContainer, Player, ArenaSize, GetVisibleWorldSize(), EnvironmentTime, batch);
    }

    private Vector2 GetVisibleWorldSize()
    {
        var viewport = GetViewport();
        if (viewport == null)
            return Vector2.Zero;

        Vector2 size = viewport.GetVisibleRect().Size;
        if (MainCamera != null && MainCamera.Zoom.X > 0.0f && MainCamera.Zoom.Y > 0.0f)
        {
            size.X /= MainCamera.Zoom.X;
            size.Y /= MainCamera.Zoom.Y;
        }
        return size;
    }

    /// <summary>
    /// 3-minute escalation loop: 03:00 elite raid, 06:00 swarm + elite pincer,
    /// 09:00 sub-boss showdown, 12:00 extreme swarm, 15:00 terminal boss lockdown.
    /// </summary>
    private void ProcessWaveDirector()
    {
        if (!EliteRaidTriggered && EnvironmentTime >= PathogenSpawner.EscalationInterval)
        {
            EliteRaidTriggered = true;
            TriggerEliteRaid();
        }

        if (!FirstSwarmTriggered && EnvironmentTime >= PathogenSpawner.EscalationInterval * 2.0f)
        {
            FirstSwarmTriggered = true;
            TriggerFirstSwarm();
        }

        if (!SubBossTriggered && EnvironmentTime >= PathogenSpawner.EscalationInterval * 3.0f)
        {
            SubBossTriggered = true;
            TriggerSubBossEncounter();
        }

        if (!ExtremeSwarmTriggered && EnvironmentTime >= PathogenSpawner.EscalationInterval * 4.0f)
        {
            ExtremeSwarmTriggered = true;
            TriggerExtremeSwarm();
        }

        if (!_terminalPhaseStarted && EnvironmentTime >= RunGoalSeconds)
        {
            _terminalPhaseStarted = true;
            EnterBossLockdown();
        }
    }

    private void TriggerEliteRaid()
    {
        if (EnemyContainer == null || Player == null)
            return;

        // Single mechanic elite: pure positioning check.
        PathogenSpawner.SpawnElite(EnemyContainer, Player, ArenaSize, EnvironmentTime, 1);
        AudioManager.Instance?.PlaySfx("wave_complete", -2.0f);
        GD.Print("[WaveDirector] 03:00 Elite raid incoming.");
    }

    private void TriggerFirstSwarm()
    {
        if (EnemyContainer == null || Player == null)
            return;

        // Elite pincer from both flanks; backfill raises the screen cap to 450 for the swarm tide.
        PathogenSpawner.SpawnElite(EnemyContainer, Player, ArenaSize, EnvironmentTime, 1, 0.0f);
        PathogenSpawner.SpawnElite(EnemyContainer, Player, ArenaSize, EnvironmentTime, 1, Mathf.Pi);
        SwarmWindowTimer = PathogenSpawner.SwarmWindowSeconds;
        AudioManager.Instance?.PlaySfx("wave_complete", -2.0f);
        GD.Print("[WaveDirector] 06:00 First swarm tide + double elite pincer.");
    }

    private void TriggerSubBossEncounter()
    {
        if (EnemyContainer == null || Player == null)
            return;

        SubBoss = PathogenSpawner.SpawnSubBoss(EnemyContainer, Player, ArenaSize, MapId, EnvironmentTime);
        if (SubBoss != null)
        {
            SubBoss.EnemyDied += OnSubBossDefeated;
            SubBoss.Digested += OnSubBossDefeated;
        }

        AudioManager.Instance?.PlaySfx("wave_complete", -2.0f);
        GD.Print("[WaveDirector] 09:00 Sub-boss showdown started.");
    }

    private void TriggerExtremeSwarm()
    {
        if (EnemyContainer == null || Player == null)
            return;

        // Backfill floods the arena to the 450 swarm cap over the next frames.
        SwarmWindowTimer = PathogenSpawner.SwarmWindowSeconds;
        PathogenSpawner.SpawnElite(EnemyContainer, Player, ArenaSize, EnvironmentTime, 2);
        AudioManager.Instance?.PlaySfx("wave_complete", -2.0f);
        GD.Print("[WaveDirector] 12:00 Extreme swarm + mixed forces.");
    }

    private void EnterBossLockdown()
    {
        BossLockdownActive = true;

        if (EnemyContainer == null || Player == null)
        {
            EndRun(false, RunRecordManager.CauseSystemFailure);
            return;
        }

        TerminalBoss = PathogenSpawner.SpawnTerminalBoss(EnemyContainer, Player, ArenaSize, MapId, EnvironmentTime);
        if (TerminalBoss == null)
        {
            // No boss entity available for this map: the clear condition cannot be met.
            EndRun(false, RunRecordManager.CauseSystemFailure);
            return;
        }

        TerminalBoss.EnemyDied += OnTerminalBossDefeated;
        TerminalBoss.Digested += OnTerminalBossDefeated;
        AudioManager.Instance?.PlaySfx("wave_complete", -2.0f);
        GD.Print("[WaveDirector] 15:00 Terminal boss lockdown! Specific neutralization required.");
    }

    private void OnSubBossDefeated(BaseEnemy boss)
    {
        if (SubBossRewardGranted)
            return;
        SubBossRewardGranted = true;

        AudioManager.Instance?.PlaySfx("wave_complete");

        // Guaranteed superweapon chest: the epigenetic evolution system is not
        // online yet, so the reward is currently a guaranteed level-up draft.
        if (Player is BaseCell cell)
        {
            float missing = Mathf.Max(0.0f, cell.ExpToNextLevel - cell.CurrentExp);
            if (missing > 0.0f)
                cell.AddExp(missing);
        }

        GD.Print("[WaveDirector] Sub-boss neutralized. Guaranteed evolution reward granted.");
    }

    private void OnTerminalBossDefeated(BaseEnemy boss)
    {
        if (RunEnded)
            return;

        TerminalBossNeutralized = true;

        if (IsEndlessRun)
        {
            // Endless overdrive (docs/endgame.md §3.1): the 15:00 clear does not
            // settle the run — the uncapped timeline keeps escalating.
            TerminalBoss = null;
            BossLockdownActive = false;
            AudioManager.Instance?.PlaySfx("wave_complete");
            GD.Print("[WaveDirector] Terminal boss neutralized. Endless overdrive continues past 15:00.");
            return;
        }

        GD.Print("[WaveDirector] Terminal boss neutralized. Specific neutralization complete.");
        EndRun(true, RunRecordManager.CauseSpecificNeutralization);
    }

    private void CheckTerminalBossState()
    {
        if (TerminalBoss == null)
            return;

        if (!GodotObject.IsInstanceValid(TerminalBoss) || TerminalBoss.IsQueuedForDeletion())
        {
            TerminalBoss = null;

            if (IsEndlessRun)
            {
                // No clear criterion to protect in endless mode: release the lockdown.
                BossLockdownActive = false;
                GD.PushWarning("[WaveDirector] Terminal boss vanished in endless mode; lockdown released.");
                return;
            }

            // The boss vanished without a confirmed kill: the clear criterion is not met.
            GD.PushWarning("[WaveDirector] Terminal boss vanished without a kill; settling as defeat.");
            EndRun(false, RunRecordManager.CauseSystemFailure);
        }
    }

    /// <summary>
    /// Drives the organ fluid mechanics and hands their velocity offset to the
    /// player cell (docs/map.md §3).
    /// </summary>
    private void ProcessOrganEnvironment(float dt)
    {
        if (OrganEnvironment == null)
            return;

        OrganEnvironment.Tick(this, dt);

        if (Player is BaseCell cell)
            cell.EnvironmentDrift = OrganEnvironment.PlayerDrift;
    }

    private void ProcessMapMechanics(float delta)
    {
        if (EnemyContainer == null)
            return;

        // The organ environment owns its fluid current (docs/map.md §3); Main only
        // applies it to the free pathogen population.
        CurrentFluidVector = OrganEnvironment?.FluidVector ?? Vector2.Zero;

        foreach (var child in EnemyContainer.GetChildren())
        {
            if (child is not Node2D enemy)
                continue;
            if (enemy.Get("is_being_eaten").AsBool())
                continue;
            enemy.Position += CurrentFluidVector * delta;
        }
    }

    /// <summary>
    /// Endless overdrive environment ladder (docs/endgame.md §3.2). Effects are
    /// cumulative per 3-minute cycle:
    ///   1) 15:00+ tissue-fluid suction intensifies + fibrin nets congeal;
    ///   2) 18:00+ respiratory shear storm thrust;
    ///   3) 21:00+ bile-acid surge strips all armor for 3s periodically;
    ///   4) 24:00+ gastric acid tide shrinks the safe zone;
    ///   5) 27:00+ terminal composite: shear storm and acid tide coexist.
    /// </summary>
    /// <summary>
    /// Applies run-start affliction effects (docs/endgame.md §4). The febrile and
    /// drift effects are periodic and handled by ProcessAfflictions.
    /// </summary>
    private void ApplyAfflictionLoadout()
    {
        if (!IsEndlessRun || Player is not BaseCell cell || cell.Stats == null)
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
    private void ProcessAfflictions(float delta)
    {
        if (!IsEndlessRun || Player == null || AfflictionManager.SelectedIds.Count == 0)
            return;

        if (AfflictionManager.IsActive(AfflictionManager.FebrileConvulsion))
        {
            _febrileBurnTimer -= delta;
            if (_febrileBurnTimer <= 0.0f)
            {
                _febrileBurnTimer = AfflictionManager.FebrileBurnInterval;
                if (Player is BaseCell fevrile && fevrile.Stats != null)
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

    private void ProcessOverdriveEnvironment(float delta)
    {
        if (!IsEndlessRun)
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
            ProcessBossRaids(cycle);

        // Cycle 1+: fibrin nets (slow-only webs) congeal across the battlefield.
        _fibrinNetTimer -= delta;
        if (_fibrinNetTimer <= 0.0f)
        {
            _fibrinNetTimer = 4.5f;
            SpawnFibrinNet();
        }

        // Cycle 2+: respiratory shear storm drives periodic violent thrust.
        if (cycle >= 2 && EnemyContainer != null)
        {
            float strength = 34.0f + 18.0f * Mathf.Min(cycle, 5);
            var shearVec = new Vector2(
                Mathf.Sin(EnvironmentTime * 1.6f) * strength,
                Mathf.Cos(EnvironmentTime * 1.1f) * strength * 0.5f);
            CurrentFluidVector += shearVec * 0.7f;

            foreach (var child in EnemyContainer.GetChildren())
            {
                if (child is not Node2D enemy)
                    continue;
                var eaten = enemy.Get("is_being_eaten");
                if (eaten.VariantType == Variant.Type.Bool && (bool)eaten)
                    continue;
                enemy.Position += shearVec * delta * 0.7f;
            }

            if (Player is BaseCell host)
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
                    if (Player is BaseCell restored)
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
                    if (Player is BaseCell cell && cell.Stats != null)
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
            float progress = Mathf.Clamp((EnvironmentTime - tideStart) / PathogenSpawner.OverdriveCycleSeconds, 0.0f, 1.0f);
            AcidSafeRadius = Mathf.Lerp(2300.0f, 850.0f, progress);
            UpdateAcidTideRing();

            if (Player != null && Player.GlobalPosition.Length() > AcidSafeRadius)
            {
                _acidTickAccumulator += delta;
                if (_acidTickAccumulator >= 1.0f)
                {
                    _acidTickAccumulator -= 1.0f;
                    if (Player is BaseCell burned)
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

    /// <summary>Builds the acid-tide boundary ring on the arena background layer.</summary>
    private void SetupAcidTideRing()
    {
        var ringParent = ArenaBorders?.GetParent();
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

    /// <summary>
    /// Multi-boss incursion (docs/endgame.md §3.3): every 3-minute overdrive
    /// cycle from 18:00 draws two terminal bosses from other organs onto the
    /// field; from 30:00 the siege escalates to a triple-boss assault.
    /// </summary>
    private void ProcessBossRaids(int cycle)
    {
        _raidBosses.RemoveAll(b => !GodotObject.IsInstanceValid(b) || b.IsQueuedForDeletion());

        if (cycle < _nextRaidCycle || EnemyContainer == null || Player == null)
            return;

        _nextRaidCycle = cycle + 1;

        bool tripleSiege = EnvironmentTime >= 1800.0f; // 30:00+ terminal siege
        int count = tripleSiege ? 3 : 2;

        var candidates = new List<string>();
        foreach (var keyVar in GameManager.MapData.Keys)
        {
            string candidateMap = keyVar.AsString();
            if (candidateMap != MapId)
                candidates.Add(candidateMap);
        }

        // Fisher-Yates shuffle: random cross-organ draw without duplicates.
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = (int)GD.RandRange(0, i);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        var bossNames = new List<string>();
        for (int i = 0; i < count && i < candidates.Count; i++)
        {
            float angle = Mathf.Tau * i / count + (float)GD.RandRange(-0.4, 0.4);
            var boss = PathogenSpawner.SpawnRaidBoss(EnemyContainer, Player, ArenaSize, candidates[i], EnvironmentTime, angle);
            if (boss == null)
                continue;

            boss.EnemyDied += OnRaidBossDefeated;
            _raidBosses.Add(boss);
            bossNames.Add(Tr(boss.DisplayNameKey));
        }

        if (bossNames.Count > 0)
        {
            string title = Tr("OVERDRIVE_RAID_TITLE");
            string desc = TextFormatter.Format(
                Tr(tripleSiege ? "OVERDRIVE_RAID_TRIPLE_FMT" : "OVERDRIVE_RAID_TWIN_FMT"),
                string.Join(" · ", bossNames));
            HudNode?.ShowOverdriveAlert(title, desc);
            GD.Print($"[Overdrive] Boss incursion ×{bossNames.Count}: {string.Join(", ", bossNames)}");
        }
    }

    private void OnRaidBossDefeated(BaseEnemy boss)
    {
        _raidBosses.Remove(boss);
        AudioManager.Instance?.PlaySfx("wave_complete", -2.0f);
        GD.Print($"[Overdrive] Raid boss neutralized: {boss.EnemyId}.");
    }

    private void SpawnFibrinNet()
    {
        if (EnemyContainer == null || Player == null)
            return;

        float angle = GD.Randf() * Mathf.Tau;
        float dist = (float)GD.RandRange(220.0, 620.0);
        var pos = Player.GlobalPosition + Vector2.FromAngle(angle) * dist;
        float halfW = (ArenaSize.X * 0.5f) - 120.0f;
        float halfH = (ArenaSize.Y * 0.5f) - 120.0f;
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
        EnemyContainer.AddChild(net);
    }

    private void AnnounceOverdriveCycle(int cycle)
    {
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

        HudNode?.ShowOverdriveAlert(title, desc);
        AudioManager.Instance?.PlaySfx("wave_complete", -3.0f);
        GD.Print($"[Overdrive] Cycle {cycle} engaged: HP ×{OverdriveHealthMultiplier:F2}, Speed ×{OverdriveSpeedMultiplier:F2}.");
    }

    private void SpawnInitialWave(int count)
    {
        if (Player == null || EnemyContainer == null)
            return;
        PathogenSpawner.SpawnWave(EnemyContainer, Player, ArenaSize, EnvironmentTime, count);
    }

    /// <summary>
    /// Tutorial cue 1 (docs/tutorial.md §2): two dormant micro-staphylococci
    /// spawn 150px directly ahead so the opening seconds teach direct engulfment.
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
    /// Ends the run and persists the settlement record.
    /// Victory (docs/record.md §3.1): survive to 15:00 AND neutralize the terminal boss.
    /// Defeat (docs/record.md §3.2): cell membrane integrity reaches zero (SIRS).
    /// </summary>
    public void EndRun(bool victory, string cause = "")
    {
        if (RunEnded)
            return;

        if (victory && !RunRecordManager.CanSettleVictory(EnvironmentTime, TerminalBossNeutralized, IsEndlessRun))
        {
            if (IsEndlessRun)
            {
                GD.PushWarning("[Main] Endless overdrive runs can only settle as defeat (membrane rupture).");
            }
            else
            {
                GD.PushWarning($"[Main] Victory rejected: 15:00 survival + terminal boss neutralization required " +
                               $"(survival={EnvironmentTime:F1}s, boss_neutralized={TerminalBossNeutralized}).");
            }
            return;
        }

        RunEnded = true;

        if (victory)
        {
            AudioManager.Instance?.PlayBgm("victory", 0.3f);
            AudioManager.Instance?.PlaySfx("wave_complete");
            AchievementManager.RecordMapClear(MapId, IsHardRun);
        }
        else
        {
            AudioManager.Instance?.PlayBgm("defeat", 0.3f);
            AudioManager.Instance?.PlaySfx("game_over");
        }

        if (string.IsNullOrEmpty(cause))
        {
            cause = victory ? RunRecordManager.CauseSpecificNeutralization : RunRecordManager.CauseMembraneRupture;
        }

        var cell = Player as BaseCell;
        string classId = GameManager.SelectedClass;

        var skillIds = new List<string>();
        var sm = Player?.GetNodeOrNull<SkillManager>("SkillManager");
        if (sm != null)
        {
            foreach (var skill in sm.ActiveSlots)
            {
                if (skill != null && !string.IsNullOrEmpty(skill.SkillId))
                    skillIds.Add(skill.SkillId);
            }
        }

        int kills = RunTelemetryManager.Instance?.KillCount ?? 0;
        int killScore = RunTelemetryManager.Instance?.KillScore ?? 0;

        RunTelemetryManager.Instance?.EndRun();

        float afflictionMultiplier = IsEndlessRun ? AfflictionManager.ScoreMultiplier : 1.0f;
        string[] afflictionIds = IsEndlessRun
            ? AfflictionManager.SelectedIds.ToArray()
            : Array.Empty<string>();

        var record = RunRecordManager.RecordRun(
            victory ? RunRecordManager.ResultVictory : RunRecordManager.ResultDefeat,
            classId,
            MapId,
            EnvironmentTime,
            cell?.CurrentLevel ?? 1,
            cell?.DigestedCount ?? 0,
            PassiveTreeManager.GetSpentPoints(classId),
            skillIds.ToArray(),
            TerminalBossNeutralized,
            cause,
            kills,
            killScore,
            RunDifficulty,
            IsEndlessRun,
            afflictionMultiplier,
            afflictionIds);

        if (RunTelemetryManager.Instance != null)
        {
            record["telemetry"] = RunTelemetryManager.Instance.GetTelemetryDictionary();
        }

        // Steam global leaderboards: reserved upload for endless overdrive runs
        // (docs/endgame.md §5.3). No-op without the SDK; the local record above
        // remains the offline fallback.
        if (IsEndlessRun)
        {
            SteamBridge.SubmitEndlessLeaderboard(
                Mathf.RoundToInt(EnvironmentTime),
                record.GetValueOrDefault("score", 0).AsInt32());
        }

        if (HudNode != null)
        {
            HudNode.PauseInputSuppressed = true;
            HudNode.ResumeGame();
        }

        var modal = GetNodeOrNull<RunRecordsModal>("UIOverlay/RunRecordsModal");
        if (modal != null)
            modal.OpenSettlement(record);

        GetTree().Paused = true;
    }

    private void SpawnStaphAroundPlayer(float dist)
    {
        if (Player == null || EnemyContainer == null || StaphScene == null)
            return;

        float angle = GD.Randf() * Mathf.Tau;
        var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
        var spawnPos = Player.GlobalPosition + offset;

        // Clamp within arena boundaries (-arena_size/2 to +arena_size/2)
        float halfW = (ArenaSize.X * 0.5f) - 60.0f;
        float halfH = (ArenaSize.Y * 0.5f) - 60.0f;
        spawnPos.X = Mathf.Clamp(spawnPos.X, -halfW, halfW);
        spawnPos.Y = Mathf.Clamp(spawnPos.Y, -halfH, halfH);

        var staph = StaphScene.Instantiate<Node2D>();
        staph.GlobalPosition = spawnPos;
        EnemyContainer.AddChild(staph);
    }
}

/// <summary>
/// Run-scoped host ulceration meter. Tissue invaders (H. pylori) accumulate acid
/// damage; crossing thresholds degrades the whole arena and retargets invaders
/// onto red blood cells.
/// </summary>
public static class HostUlceration
{
    public const int RbcPreferenceThreshold = 3;
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
