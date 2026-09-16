using Godot;
using System;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Skills;
using Phagocyte.UI;

namespace Phagocyte;

public partial class Main : Node2D
{
    private static PackedScene? _defaultStaphScene;
    public static PackedScene DefaultStaphScene => _defaultStaphScene ??= GD.Load<PackedScene>("res://scenes/enemies/staph_enemy.tscn");

    [Export] public PackedScene? StaphScene { get; set; }
    [Export] public int MaxPathogens { get; set; } = 50;
    [Export] public Vector2 ArenaSize { get; set; } = new(4800.0f, 4800.0f);

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
            if (HudNode != null)
            {
                HudNode.ConnectPlayer(Player);
            }

            ConnectAchievementEvents();
        }

        // Read map configuration from GM
        MapId = GameManager.SelectedMap;
        ConfigureMapEnvironment();

        // Initial pathogen wave
        SpawnInitialWave(35);
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

            bc.BurstStateChanged += (isActive, timeLeft, maxTime) =>
            {
                if (isActive)
                    AchievementManager.RecordEvent("burst_activated");
            };

            bc.LevelUp += (lvl) =>
            {
                AchievementManager.RecordEvent("level_up", lvl);
            };

            bc.StatsChanged += (health, maxHealth, satiety, maxSatiety, radiusRatio) =>
            {
                AchievementManager.RecordEvent("radius_ratio", radiusRatio);
            };
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
            if (Player.HasSignal("burst_state_changed"))
            {
                Player.Connect("burst_state_changed", Callable.From((bool isActive, float _t, float _m) =>
                {
                    if (isActive)
                        AchievementManager.RecordEvent("burst_activated");
                }));
            }
            if (Player.HasSignal("level_up"))
            {
                Player.Connect("level_up", Callable.From((int lvl) =>
                {
                    AchievementManager.RecordEvent("level_up", lvl);
                }));
            }
            if (Player.HasSignal("stats_changed"))
            {
                Player.Connect("stats_changed", Callable.From((float _h, float _mh, float _s, float _ms, float rr) =>
                {
                    AchievementManager.RecordEvent("radius_ratio", rr);
                }));
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
        if (MapId == "alveolar_space")
        {
            if (ArenaBg != null && ArenaBg.Material is ShaderMaterial sm)
            {
                sm.SetShaderParameter("bg_color_deep", new Color(0.02f, 0.06f, 0.10f, 1.0f));
                sm.SetShaderParameter("bg_color_accent", new Color(0.04f, 0.12f, 0.16f, 1.0f));
                sm.SetShaderParameter("fiber_color", new Color(0.2f, 0.5f, 0.65f, 0.35f));
            }
            if (ArenaBorders != null)
                ArenaBorders.DefaultColor = new Color(0.2f, 0.65f, 0.7f, 0.7f);
        }
        else
        {
            // acute_wound default
            if (ArenaBg != null && ArenaBg.Material is ShaderMaterial sm)
            {
                sm.SetShaderParameter("bg_color_deep", new Color(0.04f, 0.05f, 0.09f, 1.0f));
                sm.SetShaderParameter("bg_color_accent", new Color(0.14f, 0.04f, 0.08f, 1.0f));
                sm.SetShaderParameter("fiber_color", new Color(0.22f, 0.18f, 0.32f, 0.35f));
            }
            if (ArenaBorders != null)
                ArenaBorders.DefaultColor = new Color(0.35f, 0.45f, 0.6f, 0.65f);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        EnvironmentTime += dt;
        AchievementManager.RecordEvent("survival_time", EnvironmentTime);

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
            // SPEC Section 5: Periodic breathing airflow thrust in lung alveoli
            float breathForce = Mathf.Sin(EnvironmentTime * 1.2f) * 28.0f;
            var breathVec = new Vector2(breathForce, Mathf.Sin(EnvironmentTime * 0.6f) * 12.0f);
            // Gently pushes all free pathogens and player with fluid current
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
    }

    private void SpawnInitialWave(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnStaphAroundPlayer((float)GD.RandRange(200.0, 950.0));
        }
    }

    private void MaintainPopulation()
    {
        if (EnemyContainer == null)
            return;

        int currentCount = EnemyContainer.GetChildCount();
        if (currentCount < MaxPathogens)
        {
            int spawnBatch = Mathf.Min(5, MaxPathogens - currentCount);
            for (int i = 0; i < spawnBatch; i++)
            {
                SpawnStaphAroundPlayer((float)GD.RandRange(450.0, 1100.0));
            }
        }
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
