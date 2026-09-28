using Godot;
using Godot.Collections;
using Game.Combat;
using Game.Core;
using Game.Directors;
using Game.Player;

namespace Game.Stages;

/// <summary>
/// Data-driven stage environment (docs/stages.md §3). One instance is created
/// by GameRoot per run; behavior comes wholly from the stage row's
/// "effects" array in assets/data/stages.json — never a per-stage subclass.
/// Effect kinds: spawner, volley, dot_scan, stat_strip, scramble, scatter.
/// </summary>
public sealed class StageEnvironment
{
    public string StageId { get; }

    /// <summary>Hard (Acute Crisis) variant: exclusive stage hazards go permanent.</summary>
    public bool HardMode { get; set; }

    /// <summary>True while a stat_strip effect holds stolen stats.</summary>
    public bool StatStripped
    {
        get
        {
            foreach (var s in _states)
            {
                if (s.Kind == "stat_strip" && s.Aux > 0.0f)
                    return true;
            }
            return false;
        }
    }

    private RandomNumberGenerator Rng { get; } = new();

    private sealed class EffectState
    {
        public string Kind = "";
        public Dictionary Def = new();
        public float Timer;
        public float Aux;
        public float Aux2;
    }

    private readonly System.Collections.Generic.List<EffectState> _states = new();

    public StageEnvironment(string stageId)
    {
        StageId = stageId;
    }

    public void Attach(IRunContext context)
    {
        Rng.Seed = (ulong)StageId.GetHashCode() ^ 0x51F15EEDUL;
        _states.Clear();

        var info = GameManager.GetStageInfo(StageId);
        if (!info.TryGetValue("effects", out Variant ev) || ev.VariantType != Variant.Type.Array)
            return;

        foreach (var item in (Array)ev)
        {
            if (item.VariantType != Variant.Type.Dictionary)
                continue;
            var def = (Dictionary)item;
            var state = new EffectState
            {
                Kind = CatalogLoader.GetString(def, "kind"),
                Def = def,
                Timer = CatalogLoader.GetFloat(def, "initial_delay", 0.0f)
            };
            _states.Add(state);
            if (state.Kind == "scatter")
                RunScatter(context, state);
        }
    }

    public void Tick(IRunContext context, float dt)
    {
        Process(context, dt);
    }

    private void Process(IRunContext context, float dt)
    {
        foreach (var state in _states)
        {
            switch (state.Kind)
            {
                case "spawner": TickSpawner(context, state, dt, 1); break;
                case "volley": TickSpawner(context, state, dt, CatalogLoader.GetInt(state.Def, "count", 1)); break;
                case "dot_scan": TickDotScan(context, state, dt); break;
                case "stat_strip": TickStatStrip(context, state, dt); break;
                case "scramble": TickScramble(context, state, dt); break;
            }
        }
    }

    private void TickSpawner(IRunContext context, EffectState state, float dt, int count)
    {
        var container = Container(context);
        var player = Cell(context);
        if (container == null || player == null)
            return;

        state.Timer -= dt;
        if (state.Timer > 0.0f)
            return;
        state.Timer = HazardInterval(CatalogLoader.GetFloat(state.Def, "interval", 6.0f));

        string prop = CatalogLoader.GetString(state.Def, "prop");
        if (CatalogLoader.GetInt(state.Def, "max_active", 0) > 0 && CountProp(container, prop) >= CatalogLoader.GetInt(state.Def, "max_active", 0))
            return;

        for (int i = 0; i < count; i++)
            container.AddChild(MakeProp(prop, state, SpawnPoint(context, state, player)));
    }

    private Vector2 SpawnPoint(IRunContext context, EffectState state, PlayerActor player)
    {
        float minDist = CatalogLoader.GetFloat(state.Def, "min_dist", 150.0f);
        float maxDist = CatalogLoader.GetFloat(state.Def, "max_dist", 700.0f);
        if (CatalogLoader.GetString(state.Def, "anchor", "player") == "arena")
        {
            float scale = CatalogLoader.GetFloat(state.Def, "arena_scale", 1.0f);
            return PointNear(Vector2.Zero, context.ArenaSize * scale, minDist, maxDist);
        }
        return PointNear(player.GlobalPosition, context.ArenaSize, minDist, maxDist);
    }

    private Node2D MakeProp(string prop, EffectState state, Vector2 pos)
    {
        var ov = state.Def.TryGetValue("overrides", out Variant ovv) && ovv.VariantType == Variant.Type.Dictionary
            ? (Dictionary)ovv : new Dictionary();
        bool randomFacing = CatalogLoader.GetString(state.Def, "orientation") == "random";
        return prop switch
        {
            "buff_zone" => new BuffZone
            {
                StatId = CatalogLoader.GetString(ov, "stat_id", "cooldown_reduction"),
                Bonus = CatalogLoader.GetFloat(ov, "bonus", 0.25f),
                Duration = CatalogLoader.GetFloat(ov, "duration", 6.0f),
                Radius = CatalogLoader.GetFloat(ov, "radius", 82.0f),
                Lifetime = CatalogLoader.GetFloat(ov, "lifetime", 22.0f),
                GlobalPosition = pos
            },
            "safe_zone" => new SafeZone
            {
                Radius = CatalogLoader.GetFloat(ov, "radius", 120.0f),
                Lifetime = CatalogLoader.GetFloat(ov, "lifetime", 16.0f),
                GlobalPosition = pos
            },
            "dot_zone" => new DotZone
            {
                Radius = CatalogLoader.GetFloat(ov, "radius", 260.0f),
                Lifetime = CatalogLoader.GetFloat(ov, "lifetime", 6.0f),
                GlobalPosition = pos
            },
            "blocker_wall" => new BlockerWall
            {
                Rotation = randomFacing && Rng.Randf() >= 0.5f ? Mathf.Pi * 0.5f : 0.0f,
                GapHalfWidth = CatalogLoader.GetFloat(ov, "gap_half_width", 55.0f),
                Length = CatalogLoader.GetFloat(ov, "length", 320.0f),
                Position = pos
            },
            _ => new BlockerPillar
            {
                Radius = CatalogLoader.GetFloat(ov, "radius", 72.0f),
                GlobalPosition = pos
            },
        };
    }

    private void TickDotScan(IRunContext context, EffectState state, float dt)
    {
        var container = Container(context);
        var player = Cell(context);
        if (container == null || player == null)
            return;

        if (CatalogLoader.GetBool(state.Def, "safe_zone", false) && SafeZone.CoversPoint(player.GlobalPosition))
            return;

        foreach (var child in container.GetChildren())
        {
            if (child is DotZone zone && GodotObject.IsInstanceValid(zone) && zone.Contains(player.GlobalPosition))
            {
                player.TakeDamage(CatalogLoader.GetFloat(state.Def, "dps", 7.0f) * dt);
                SlowService.ApplySlow(player, CatalogLoader.GetFloat(state.Def, "slow_factor", 0.3f), CatalogLoader.GetFloat(state.Def, "slow_duration", 0.7f));
                break;
            }
        }
    }

    private void TickStatStrip(IRunContext context, EffectState state, float dt)
    {
        var player = Cell(context);
        if (player?.Stats == null)
            return;

        string stat = CatalogLoader.GetString(state.Def, "stat", "armor");
        if (state.Aux > 0.0f)
        {
            state.Aux -= dt;
            if (state.Aux <= 0.0f && state.Aux2 > 0.0f)
            {
                player.Stats.AddModifier(stat, state.Aux2, 0.0f);
                state.Aux2 = 0.0f;
            }
            return;
        }

        state.Timer -= dt;
        if (state.Timer > 0.0f)
            return;
        state.Timer = HazardInterval(CatalogLoader.GetFloat(state.Def, "interval", 18.0f));
        state.Aux2 = Mathf.Max(0.0f, player.Stats.GetStat(stat));
        if (state.Aux2 > 0.0f)
        {
            player.Stats.AddModifier(stat, -state.Aux2, 0.0f);
            state.Aux = CatalogLoader.GetFloat(state.Def, "duration", 3.0f);
            GD.Print("[Stage] Stat strip: " + stat + " suppressed.");
        }
    }

    private void TickScramble(IRunContext context, EffectState state, float dt)
    {
        var player = Cell(context);
        if (player == null)
            return;

        state.Timer -= dt;
        if (state.Timer > 0.0f)
            return;
        state.Timer = HazardInterval(CatalogLoader.GetFloat(state.Def, "interval", 11.0f));
        float duration = CatalogLoader.GetFloat(state.Def, "duration", 1.6f);
        player.ApplyInvertControls(HardMode ? duration * CatalogLoader.GetFloat(state.Def, "hard_mult", 1.5f) : duration);
        GD.Print("[Stage] Control scramble pulse.");
    }

    private void RunScatter(IRunContext context, EffectState state)
    {
        var container = Container(context);
        if (container == null)
            return;

        int count = CatalogLoader.GetInt(state.Def, "count", 5);
        float ring = CatalogLoader.GetFloat(state.Def, "ring_radius", 620.0f);
        float offset = CatalogLoader.GetFloat(state.Def, "angle_offset", 0.4f);
        for (int i = 0; i < count; i++)
        {
            float angle = i * (Mathf.Tau / count) + offset;
            container.AddChild(new BlockerPillar
            {
                Radius = CatalogLoader.GetFloat(state.Def, "radius_base", 70.0f) + (i % 2) * CatalogLoader.GetFloat(state.Def, "radius_step", 14.0f),
                GlobalPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ring
            });
        }
    }

    private static int CountProp(Node2D container, string prop)
    {
        int count = 0;
        foreach (var child in container.GetChildren())
        {
            bool match = prop switch
            {
                "buff_zone" => child is BuffZone,
                "safe_zone" => child is SafeZone,
                "dot_zone" => child is DotZone,
                "blocker_wall" => child is BlockerWall,
                _ => child is BlockerPillar,
            };
            if (match && GodotObject.IsInstanceValid(child))
                count++;
        }
        return count;
    }

    private static PlayerActor? Cell(IRunContext context)
    {
        return context.Player as PlayerActor;
    }

    private static Node2D? Container(IRunContext context)
    {
        return context.EnemyContainer;
    }

    /// <summary>Hard mode shortens timers by +50% frequency (docs/achievement.md §2.1).</summary>
    private float HazardInterval(float normalSeconds)
    {
        return HardMode ? normalSeconds / 1.5f : normalSeconds;
    }

    /// <summary>Random point within <paramref name="minDist"/>-<paramref name="maxDist"/> of the anchor.</summary>
    private Vector2 PointNear(Vector2 anchor, Vector2 arenaSize, float minDist, float maxDist)
    {
        float angle = Rng.Randf() * Mathf.Tau;
        float dist = Rng.RandfRange(minDist, maxDist);
        Vector2 point = anchor + Vector2.FromAngle(angle) * dist;
        float halfW = Mathf.Max(80.0f, arenaSize.X * 0.5f - 140.0f);
        float halfH = Mathf.Max(80.0f, arenaSize.Y * 0.5f - 140.0f);
        point.X = Mathf.Clamp(point.X, -halfW, halfW);
        point.Y = Mathf.Clamp(point.Y, -halfH, halfH);
        return point;
    }

    public static StageEnvironment ForStage(string stageId)
    {
        return new StageEnvironment(stageId);
    }
}

