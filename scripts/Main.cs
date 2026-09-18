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
    [Export] public int MaxPathogens { get; set; } = 50;
    [Export] public Vector2 ArenaSize { get; set; } = new(4800.0f, 4800.0f);
    [Export] public float RunGoalSeconds { get; set; } = 300.0f;

    public bool RunEnded { get; private set; } = false;

    public CharacterBody2D? Player { get; set; }
    public Hud? HudNode { get; set; }
    public Node2D? EnemyContainer { get; set; }
    public Camera2D? MainCamera { get; set; }

    public ColorRect? ArenaBg { get; set; }
    public Line2D? ArenaBorders { get; set; }

    public float SpawnTimer { get; set; } = 0.0f;
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

        if (!RunEnded && RunGoalSeconds > 0.0f && EnvironmentTime >= RunGoalSeconds)
        {
            EndRun(true);
            return;
        }

        // Map mechanics
        ProcessMapMechanics(dt);

        SpawnTimer += dt;
        if (SpawnTimer >= 1.5f)
        {
            SpawnTimer = 0.0f;
            MaintainPopulation();
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

    private void MaintainPopulation()
    {
        if (EnemyContainer == null || Player == null)
            return;

        int currentCount = EnemyContainer.GetChildCount();
        if (currentCount < MaxPathogens)
        {
            int spawnBatch = Mathf.Min(3, MaxPathogens - currentCount);
            PathogenSpawner.SpawnWave(EnemyContainer, Player, ArenaSize, EnvironmentTime, spawnBatch);
        }
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

        var record = RunRecordManager.RecordRun(
            victory ? RunRecordManager.ResultVictory : RunRecordManager.ResultDefeat,
            classId,
            MapId,
            EnvironmentTime,
            cell?.CurrentLevel ?? 1,
            cell?.DigestedCount ?? 0,
            PassiveTreeManager.GetSpentPoints(classId),
            skillIds.ToArray());

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
