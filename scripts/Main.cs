using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Skills;
using Phagocyte.UI;
using Phagocyte.Enemies;
using Phagocyte.Combat;

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
    public int ActiveScreenCap => SwarmWindowTimer > 0.0f ? ScreenCapSwarm : ScreenCapNormal;
    public float SwarmWindowTimer { get; private set; } = 0.0f;
    public int ActivePathogenCount => EnemyContainer?.GetChildCount() ?? 0;

    // Wave Director state (3-minute escalation loop)
    public bool EliteRaidTriggered { get; private set; } = false;
    public bool FirstSwarmTriggered { get; private set; } = false;
    public bool SubBossTriggered { get; private set; } = false;
    public bool ExtremeSwarmTriggered { get; private set; } = false;
    public bool BossLockdownActive { get; private set; } = false;
    public bool SubBossRewardGranted { get; private set; } = false;
    public BaseEnemy? SubBoss { get; private set; }
    public BaseEnemy? TerminalBoss { get; private set; }

    private bool _terminalPhaseStarted = false;

    public CharacterBody2D? Player { get; set; }
    public Hud? HudNode { get; set; }
    public Node2D? EnemyContainer { get; set; }
    public Camera2D? MainCamera { get; set; }

    public ColorRect? ArenaBg { get; set; }
    public Line2D? ArenaBorders { get; set; }

    public float EnvironmentTime { get; set; } = 0.0f;
    public string MapId { get; set; } = "acute_wound";

    public override void _Ready()
    {
        StaphScene ??= DefaultStaphScene;

        Player = GetNodeOrNull<CharacterBody2D>("Macrophage");
        HudNode = GetNodeOrNull<Hud>("HUD");
        EnemyContainer = GetNodeOrNull<Node2D>("EnemyContainer");
        MainCamera = GetNodeOrNull<Camera2D>("Macrophage/Camera2D");

        ArenaBg = GetNodeOrNull<ColorRect>("Background/ArenaBG");
        ArenaBorders = GetNodeOrNull<Line2D>("Background/ArenaBorders");

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
            if (HudNode != null)
            {
                HudNode.GoalSeconds = RunGoalSeconds;
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

        // Initial pathogen wave
        SpawnInitialWave(35);

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

            bc.Died += () => EndRun(false);
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
                Player.Connect("died", Callable.From(() => EndRun(false)));
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

        // Map mechanics
        ProcessMapMechanics(dt);

        if (BossLockdownActive)
        {
            CheckTerminalBossState();
            return;
        }

        ProcessDynamicBackfill();
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

        SubBoss = PathogenSpawner.SpawnSubBoss(EnemyContainer, Player, ArenaSize, MapId);
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
            EndRun(true);
            return;
        }

        TerminalBoss = PathogenSpawner.SpawnTerminalBoss(EnemyContainer, Player, ArenaSize, MapId);
        if (TerminalBoss == null)
        {
            // No boss entity available for this map: keep the run clearable.
            EndRun(true);
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

        GD.Print("[WaveDirector] Terminal boss neutralized. Specific neutralization complete.");
        EndRun(true);
    }

    private void CheckTerminalBossState()
    {
        if (TerminalBoss == null)
            return;

        if (!GodotObject.IsInstanceValid(TerminalBoss) || TerminalBoss.IsQueuedForDeletion())
        {
            TerminalBoss = null;
            EndRun(true);
        }
    }

    private void ProcessMapMechanics(float delta)
    {
        if (EnemyContainer == null)
            return;

        if (MapId == "alveolar_space")
        {
            // SPEC Section 5: Periodic respiratory breathing airflow thrust in lung alveoli
            float breathForce = Mathf.Sin(EnvironmentTime * 1.2f) * 28.0f;
            var breathVec = new Vector2(breathForce, Mathf.Sin(EnvironmentTime * 0.6f) * 12.0f);
            // Gently pushes all free pathogens with fluid current
            foreach (var child in EnemyContainer.GetChildren())
            {
                if (child is Node2D enemy)
                {
                    var eaten = enemy.Get("is_being_eaten");
                    if (eaten.VariantType == Variant.Type.Bool && (bool)eaten)
                        continue;
                    enemy.Position += breathVec * delta * 0.6f;
                }
            }
        }
        else if (MapId == "acute_wound")
        {
            // SPEC Section 5: Directional tissue fluid suction towards wound tear
            var suctionVec = new Vector2(16.0f, 10.0f);
            foreach (var child in EnemyContainer.GetChildren())
            {
                if (child is Node2D enemy)
                {
                    var eaten = enemy.Get("is_being_eaten");
                    if (eaten.VariantType == Variant.Type.Bool && (bool)eaten)
                        continue;
                    enemy.Position += suctionVec * delta * 0.4f;
                }
            }
        }
        else if (MapId == "hepatic_sinusoid")
        {
            // Hepatic sinusoid slow flow drag: gentle steady drift
            var flowVec = new Vector2(10.0f, Mathf.Sin(EnvironmentTime * 0.8f) * 6.0f);
            foreach (var child in EnemyContainer.GetChildren())
            {
                if (child is Node2D enemy)
                {
                    var eaten = enemy.Get("is_being_eaten");
                    if (eaten.VariantType == Variant.Type.Bool && (bool)eaten)
                        continue;
                    enemy.Position += flowVec * delta * 0.35f;
                }
            }
        }
        else if (MapId == "gastric_lumen")
        {
            // Gastric mucosa acid churn: periodic lateral wave
            float churnForce = Mathf.Sin(EnvironmentTime * 2.0f) * 20.0f;
            var churnVec = new Vector2(churnForce, Mathf.Cos(EnvironmentTime * 1.5f) * 10.0f);
            foreach (var child in EnemyContainer.GetChildren())
            {
                if (child is Node2D enemy)
                {
                    var eaten = enemy.Get("is_being_eaten");
                    if (eaten.VariantType == Variant.Type.Bool && (bool)eaten)
                        continue;
                    enemy.Position += churnVec * delta * 0.4f;
                }
            }
        }
        else if (MapId == "blood_brain_barrier")
        {
            // High-frequency synaptic micro-vibrations
            float pulse = Mathf.Sin(EnvironmentTime * 5.0f) * 8.0f;
            var microVec = new Vector2(pulse, Mathf.Cos(EnvironmentTime * 4.0f) * 8.0f);
            foreach (var child in EnemyContainer.GetChildren())
            {
                if (child is Node2D enemy)
                {
                    var eaten = enemy.Get("is_being_eaten");
                    if (eaten.VariantType == Variant.Type.Bool && (bool)eaten)
                        continue;
                    enemy.Position += microVec * delta * 0.25f;
                }
            }
        }
    }

    private void SpawnInitialWave(int count)
    {
        if (Player == null || EnemyContainer == null)
            return;
        PathogenSpawner.SpawnWave(EnemyContainer, Player, ArenaSize, EnvironmentTime, count);
    }

    /// <summary>
    /// Ends the run (victory when the survival goal is reached, defeat on death),
    /// persists the settlement record, and shows the medical record modal.
    /// </summary>
    public void EndRun(bool victory)
    {
        if (RunEnded)
            return;
        RunEnded = true;

        if (victory)
        {
            AudioManager.Instance?.PlayBgm("victory", 0.3f);
            AudioManager.Instance?.PlaySfx("wave_complete");
        }
        else
        {
            AudioManager.Instance?.PlayBgm("defeat", 0.3f);
            AudioManager.Instance?.PlaySfx("game_over");
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

        RunTelemetryManager.Instance?.EndRun();

        var record = RunRecordManager.RecordRun(
            victory ? RunRecordManager.ResultVictory : RunRecordManager.ResultDefeat,
            classId,
            MapId,
            EnvironmentTime,
            cell?.CurrentLevel ?? 1,
            cell?.DigestedCount ?? 0,
            PassiveTreeManager.GetSpentPoints(classId),
            skillIds.ToArray());

        if (RunTelemetryManager.Instance != null)
        {
            record["telemetry"] = RunTelemetryManager.Instance.GetTelemetryDictionary();
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
