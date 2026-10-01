using Godot;
using Godot.Collections;
using Game.Combat;
using Game.Core;
using Game.Player;
using Game.Stages;

namespace Game.Enemies;

/// <summary>
/// Data-driven enemy behavior (assets/data/enemies.json). One
/// <see cref="EnemyActor"/> plus this trait engine replaces every former
/// per-species subclass: stats, steering, tints and the traits dict come
/// from the def, and new enemies are new JSON rows. A genuinely new
/// behavior extends a trait's schema here — never a new enemy class.
/// </summary>
public partial class EnemyActor
{
    private Dictionary _enemyDef = new();
    private float _bodyMicrons = 1.0f;
    private string _dormantVariant = "";

    /// <summary>Dormant-variant flag (ambush / rooted states clear it visually).</summary>
    public bool VariantActive { get; private set; }

    /// <summary>Damage multiplier for trait attacks (awakened berserk).</summary>
    public float DamageMult { get; set; } = 1.0f;

    /// <summary>Latched-lesion pulse count (diagnostics / tests).</summary>
    public int LatchPulses { get; private set; }

    /// <summary>Current slow-aura radius, or -1 when the def has no slow_aura trait.</summary>
    public float CurrentAuraRadius
    {
        get
        {
            var aura = Trait("slow_aura");
            if (aura == null)
                return -1.0f;
            float hpRatio = Mathf.Clamp(CurrentHealth / Mathf.Max(1.0f, MaxHealth), 0.0f, 1.0f);
            return Mathf.Lerp(
                CatalogLoader.GetFloat(aura, "radius_base", 420.0f),
                CatalogLoader.GetFloat(aura, "radius_max", 760.0f),
                1.0f - hpRatio);
        }
    }

    /// <summary>Shell diagnostics (defs with a shell trait; -1/true when absent).</summary>
    public float ShellMax => Trait("shell") != null ? MaxHealth * CatalogLoader.GetFloat(Trait("shell")!, "pool_pct", 0.5f) : -1.0f;
    public float ShellHealth => _shellPool;
    public bool ShellIntact => Trait("shell") == null || (!_shellBroken && _shellPool > 0.0f);
    public bool ShellBroken => _shellBroken;

    /// <summary>Set by traits that fully own movement for this tick.</summary>
    public bool MovementLocked { get; private set; }

    private float _shellPool;
    private bool _shellBroken;

    private sealed class TraitState
    {
        public float Timer;
        public float Timer2;
        public int Phase;
        public bool Flag;
        public Vector2 Dir = Vector2.Right;
        public Vector2[] Segs = System.Array.Empty<Vector2>();
        public System.Collections.Generic.List<Zone> Zones = new();
    }

    private readonly System.Collections.Generic.Dictionary<string, TraitState> _traitStates = new();

    /// <summary>Applies a simulation def (GameManager.GetEnemyDef). Call before AddChild.</summary>
    public void ApplyDef(Dictionary def)
    {
        _enemyDef = def;
        EnemyId = CatalogLoader.GetString(def, "id", EnemyId);
        MaxHealth = CatalogLoader.GetFloat(def, "max_health", MaxHealth);
        CurrentHealth = MaxHealth;
        Armor = CatalogLoader.GetFloat(def, "armor", Armor);
        AilmentThresholdMult = CatalogLoader.GetFloat(def, "ailment_threshold_mult", AilmentThresholdMult);
        XpValue = CatalogLoader.GetFloat(def, "xp", XpValue);
        BaseScore = CatalogLoader.GetInt(def, "score", BaseScore);
        FloatSpeed = CatalogLoader.GetFloat(def, "float_speed", FloatSpeed);
        ContactDamage = CatalogLoader.GetFloat(def, "contact_damage", ContactDamage);
        DisplayNameKey = CatalogLoader.GetString(def, "display_name_key", DisplayNameKey);
        if (System.Enum.TryParse(CatalogLoader.GetString(def, "threat_mode", "Drifter"), true, out EnemyThreatMode tm))
            ThreatMode = tm;
        IsElite = CatalogLoader.GetBool(def, "elite", IsElite);
        IsBoss = CatalogLoader.GetBool(def, "boss", IsBoss);
        _bodyMicrons = CatalogLoader.GetFloat(def, "body_microns", 1.0f);
        DamageMult = CatalogLoader.GetFloat(def, "damage_mult", 1.0f);
        _dormantVariant = CatalogLoader.GetString(def, "batch_variant");
        string tintHex = CatalogLoader.GetString(def, "tint");
        SwarmBatchColor = tintHex != "" ? new Color(tintHex) : Colors.White;

        Dictionary steering = def.TryGetValue("steering", out Variant stv) && stv.VariantType == Variant.Type.Dictionary
            ? (Dictionary)stv : new Dictionary();
        UseGenericSteering = CatalogLoader.GetBool(steering, "use_generic", true);
        SteeringTurnRate = CatalogLoader.GetFloat(steering, "turn_rate", 2.6f);
        SteeringPreferredRange = CatalogLoader.GetFloat(steering, "preferred_range", 260.0f);
        SteeringLatchRange = CatalogLoader.GetFloat(steering, "latch_range", 40.0f);

        _shellPool = 0.0f;
        _shellBroken = false;
        var shell = Trait("shell");
        if (shell != null)
            _shellPool = MaxHealth * CatalogLoader.GetFloat(shell, "pool_pct", 0.5f);

        _traitStates.Clear();
    }

    private Dictionary? Trait(string key)
    {
        if (_enemyDef.TryGetValue("traits", out Variant tv) && tv.VariantType == Variant.Type.Dictionary)
        {
            var traits = (Dictionary)tv;
            if (traits.TryGetValue(key, out Variant v) && v.VariantType == Variant.Type.Dictionary)
                return (Dictionary)v;
        }
        return null;
    }

    private TraitState StateOf(string key, Dictionary def, float defaultTimer)
    {
        if (!_traitStates.TryGetValue(key, out var state))
        {
            state = new TraitState
            {
                Timer = CatalogLoader.GetFloat(def, "initial_delay", defaultTimer),
                Dir = Vector2.Right
            };
            _traitStates[key] = state;
        }
        return state;
    }

    /// <summary>
    /// Pre-drift lock pass: engages <see cref="MovementLocked"/> before the
    /// generic drift integrates, so rooting/owned-movement traits never leak
    /// one frame of drift on the transition tick.
    /// </summary>
    private void PreDriftTraits()
    {
        MovementLocked = false;
        if (_enemyDef.Count == 0)
            return;

        if (_traitStates.TryGetValue("telegraph_line", out var line) && line.Flag)
        {
            MovementLocked = true;
            return;
        }
        if (_traitStates.TryGetValue("charge", out var charge) && charge.Phase > 0)
        {
            MovementLocked = true;
            return;
        }
        if (_traitStates.ContainsKey("zigzag") || _traitStates.ContainsKey("segmented"))
        {
            MovementLocked = true;
            return;
        }
        if (_traitStates.TryGetValue("dash_suicide", out var dash) && dash.Timer > 0.0f && !dash.Flag)
            MovementLocked = true;
    }

    private void TickTraits(float dt)
    {
        MovementLocked = false;
        if (_enemyDef.Count == 0)
            return;
        var charge = Trait("charge");
        if (charge != null) TickCharge(charge, dt);
        var line = Trait("telegraph_line");
        if (line != null) TickTelegraphLine(line, dt);
        var circle = Trait("telegraph_circle");
        if (circle != null) TickTelegraphCircle(circle, dt);
        var ranged = Trait("ranged");
        if (ranged != null) TickRanged(ranged, dt);
        var replicate = Trait("replicate");
        if (replicate != null) TickReplicate(replicate, dt);
        var mitosis = Trait("mitosis");
        if (mitosis != null) TickMitosis(mitosis, dt);
        var contact = Trait("contact");
        if (contact != null) TickContact(contact, dt);
        var zigzag = Trait("zigzag");
        if (zigzag != null) TickZigzag(zigzag, dt);
        var sway = Trait("sway");
        var segmented = Trait("segmented");
        if (segmented != null) TickSegmented(segmented, sway, dt);
        var band = Trait("keep_band");
        if (band != null) TickKeepBand(band, dt);
        var ambush = Trait("ambush");
        if (ambush != null) TickAmbush(ambush, dt);
        var pull = Trait("aura_pull");
        if (pull != null) TickAuraPull(pull, dt);
        var dot = Trait("aura_dot");
        if (dot != null) TickAuraDot(dot, dt);
        var slowAura = Trait("slow_aura");
        if (slowAura != null) TickSlowAura(slowAura, dt);
        var traction = Trait("traction_pulse");
        if (traction != null) TickTraction(traction, dt);
        var feed = Trait("feed");
        if (feed != null) TickFeed(feed, dt);
        var cleanse = Trait("cleanse_pulse");
        if (cleanse != null) TickCleanse(cleanse, dt);
        var lesion = Trait("latch_lesion");
        if (lesion != null) TickLatchLesion(lesion, dt);
        var timed = Trait("timed_zone");
        if (timed != null) TickTimedZone(timed, dt);
        var dash = Trait("dash_suicide");
        if (dash != null) TickDashSuicide(dash, dt);
        var splitHp = Trait("split_hp");
        if (splitHp != null && CatalogLoader.GetString(splitHp, "trigger", "damage") == "poll"
            && CurrentHealth <= MaxHealth * CatalogLoader.GetFloat(splitHp, "frac", 0.5f))
            SplitBurst(splitHp);
    }

    private void PreDamageTraits(float damage, Node2D? source)
    {
        if (_enemyDef.Count == 0)
            return;

        var split = Trait("split_death");
        if (split != null && CatalogLoader.GetBool(split, "also_on_damage", false))
            Rupture(split);

        var ambush = Trait("ambush");
        if (ambush != null && CatalogLoader.GetBool(ambush, "wake_on_damage", false))
            Awaken(ambush);

        var line = Trait("telegraph_line");
        if (line != null && CatalogLoader.GetBool(line, "trigger_on_damage", false))
            ExtendLine(line);
    }

    private void PostDamageTraits(float damage, Node2D? source)
    {
        if (_enemyDef.Count == 0)
            return;

        var transform = Trait("transform_hp");
        if (transform != null && CurrentHealth > 0.0f && CurrentHealth <= MaxHealth * CatalogLoader.GetFloat(transform, "frac", 0.5f))
            TransformSpawn(transform);

        var splitHp = Trait("split_hp");
        if (splitHp != null && CurrentHealth <= MaxHealth * CatalogLoader.GetFloat(splitHp, "frac", 0.5f))
            SplitBurst(splitHp);
    }

    private void RunDeathTraits()
    {
        if (_enemyDef.Count == 0)
            return;

        var split = Trait("split_death");
        if (split != null && !CatalogLoader.GetBool(split, "also_on_damage", false))
            Rupture(split);

        var hatch = Trait("death_hatch");
        if (hatch != null && TryConsumeSplitBurst())
            SpawnMinion(CatalogLoader.GetString(hatch, "spawn"), GlobalPosition, Vector2.Zero);

        var drop = Trait("death_drop");
        if (drop != null)
            SpawnZone(drop, GlobalPosition);

        var obstacle = Trait("death_obstacle");
        if (obstacle != null)
        {
            var parent = GetParent();
            if (parent != null)
                parent.AddChild(new DebrisWall
                {
                    Radius = CatalogLoader.GetFloat(obstacle, "radius", 46.0f),
                    GlobalPosition = GlobalPosition
                });
        }
    }

    private float ApplyShellAbsorb(float damage, Node2D? source, bool isCrit)
    {
        var shell = Trait("shell");
        if (shell == null || _shellBroken)
            return damage;

        float absorb = CatalogLoader.GetFloat(shell, "absorb", 0.85f);
        _shellPool = Mathf.Max(0.0f, _shellPool - damage * absorb);
        if (_shellPool <= 0.0f)
        {
            _shellBroken = true;
            Armor = CatalogLoader.GetFloat(shell, "break_armor_to", 6.0f);
            var parent = GetParent();
            if (parent != null)
                parent.AddChild(new ShockRing
                {
                    MaxRadius = CatalogLoader.GetFloat(shell, "wave_radius", 280.0f),
                    GlobalPosition = GlobalPosition
                });
        }
        return Mathf.Max(1.0f, damage * (1.0f - absorb));
    }

    // --- trait handlers ---

    private void TickCharge(Dictionary def, float dt)
    {
        var player = PlayerRef;
        var state = StateOf("charge", def, 0.0f);
        float triggerTime = CatalogLoader.GetFloat(def, "trigger_time", 3.0f);
        float triggerRange = CatalogLoader.GetFloat(def, "trigger_range", 500.0f);

        if (state.Phase == 0)
        {
            state.Timer += dt;
            if (player != null && GodotObject.IsInstanceValid(player) && state.Timer >= triggerTime
                && GlobalPosition.DistanceTo(player.GlobalPosition) < triggerRange)
            {
                state.Phase = 1;
                state.Timer = 0.0f;
                state.Dir = (player.GlobalPosition - GlobalPosition).Normalized();
                if (state.Dir == Vector2.Zero)
                    state.Dir = Vector2.Right;
                Rotation = state.Dir.Angle();
                Velocity = Vector2.Zero;
            }
            return;
        }

        if (state.Phase == 1)
        {
            MovementLocked = true;
            Velocity = Velocity.MoveToward(Vector2.Zero, 100.0f * dt);
            Position += Velocity * dt;
            state.Timer += dt;
            if (state.Timer >= CatalogLoader.GetFloat(def, "windup", 0.6f))
            {
                state.Phase = 2;
                state.Timer = 0.0f;
                Velocity = state.Dir * CatalogLoader.GetFloat(def, "dash_speed", 320.0f);
            }
            return;
        }

        if (state.Phase == 2)
        {
            MovementLocked = true;
            Position += Velocity * dt;
            state.Timer += dt;
            if (player != null && GodotObject.IsInstanceValid(player) && !player.IsDead
                && GlobalPosition.DistanceTo(player.GlobalPosition) < player.CurrentRadius + BodyRadius)
            {
                player.Velocity += state.Dir * CatalogLoader.GetFloat(def, "knockback", 350.0f);
                HitPipeline.ResolveHit(new HitPayload
                {
                    RawDamage = CatalogLoader.GetFloat(def, "damage", 12.0f),
                    SourceFaction = Team.Enemy,
                    AttackerId = GetInstanceId(),
                }, player);
                state.Phase = 3;
                state.Timer = 0.0f;
                Velocity = -state.Dir * CatalogLoader.GetFloat(def, "rebound", 50.0f);
                return;
            }
            if (state.Timer >= CatalogLoader.GetFloat(def, "charge_time", 1.0f))
            {
                state.Phase = 3;
                state.Timer = 0.0f;
            }
            return;
        }

        MovementLocked = true;
        Velocity = Velocity.Lerp(Vector2.Zero, Mathf.Clamp(4.0f * dt, 0.0f, 1.0f));
        Position += Velocity * dt;
        state.Timer += dt;
        if (state.Timer >= CatalogLoader.GetFloat(def, "cooldown", 1.5f))
        {
            state.Phase = 0;
            state.Timer = 0.0f;
        }
    }

    private void ExtendLine(Dictionary def)
    {
        var state = StateOf("telegraph_line", def, 0.0f);
        if (state.Flag)
            return;
        state.Flag = true;
        VariantActive = true;
        state.Timer = CatalogLoader.GetFloat(def, "root_duration", 1.6f);

        var player = PlayerRef;
        Vector2 aim = player != null && GodotObject.IsInstanceValid(player)
            ? player.GlobalPosition
            : GlobalPosition + Vector2.Right * CatalogLoader.GetFloat(def, "length", 150.0f);
        Vector2 dir = (aim - GlobalPosition).Normalized();
        if (dir == Vector2.Zero)
            dir = Vector2.Right;

        var parent = GetParent();
        if (parent != null)
            parent.AddChild(new TelegraphedAttack
            {
                Shape = TelegraphAttackShape.Line,
                GlobalPosition = GlobalPosition,
                TargetDirection = dir,
                LineLength = CatalogLoader.GetFloat(def, "length", 150.0f),
                LineWidth = CatalogLoader.GetFloat(def, "line_width", 30.0f),
                TelegraphDuration = CatalogLoader.GetFloat(def, "telegraph", 0.9f),
                Damage = CatalogLoader.GetFloat(def, "damage", 18.0f)
            });
        RedrawIfVisible();
    }

    private void TickTelegraphLine(Dictionary def, float dt)
    {
        var state = StateOf("telegraph_line", def, 0.0f);
        if (state.Flag)
        {
            MovementLocked = true;
            Velocity = Vector2.Zero;
            state.Timer -= dt;
            if (state.Timer <= 0.0f)
            {
                state.Flag = false;
                VariantActive = false;
                RedrawIfVisible();
            }
            return;
        }

        var player = PlayerRef;
        if (player != null && GodotObject.IsInstanceValid(player)
            && GlobalPosition.DistanceTo(player.GlobalPosition) < CatalogLoader.GetFloat(def, "trigger_range", 200.0f))
            ExtendLine(def);
    }

    private void TickTelegraphCircle(Dictionary def, float dt)
    {
        float interval = CatalogLoader.GetFloat(def, "interval", 4.5f);
        var state = StateOf("telegraph_circle", def, interval);
        state.Timer -= dt;
        if (state.Timer > 0.0f)
            return;
        state.Timer = interval;

        var player = PlayerRef;
        if (player == null || !GodotObject.IsInstanceValid(player))
            return;
        var parent = GetParent();
        if (parent == null)
            return;
        parent.AddChild(new TelegraphedAttack
        {
            Shape = TelegraphAttackShape.Circle,
            GlobalPosition = player.GlobalPosition,
            Radius = CatalogLoader.GetFloat(def, "radius", 70.0f),
            TelegraphDuration = CatalogLoader.GetFloat(def, "telegraph", 1.1f),
            Damage = CatalogLoader.GetFloat(def, "damage", 22.0f) * DamageMult
        });
    }

    private void TickRanged(Dictionary def, float dt)
    {
        float interval = CatalogLoader.GetFloat(def, "interval", 2.5f);
        var state = StateOf("ranged", def, interval);
        state.Timer -= dt;
        if (state.Timer > 0.0f)
            return;

        var player = PlayerRef;
        if (player == null || !GodotObject.IsInstanceValid(player))
            return;
        if (GlobalPosition.DistanceTo(player.GlobalPosition) > CatalogLoader.GetFloat(def, "range", 640.0f))
            return;
        state.Timer = interval;

        var mgr = ProjectileManager.Instance;
        if (mgr == null)
            return;
        Vector2 dir = (player.GlobalPosition - GlobalPosition).Normalized();
        if (dir.LengthSquared() < 0.0001f)
            dir = Vector2.Right;
        var fx = default(EffectSpec);
        int fxCount = 0;
        float stun = CatalogLoader.GetFloat(def, "stun", 0.0f);
        if (stun > 0.0f)
        {
            fx = new EffectSpec { EffectId = "stun", Duration = stun };
            fxCount = 1;
        }
        mgr.Spawn(
            GlobalPosition + dir * CatalogLoader.GetFloat(def, "spawn_offset", 16.0f),
            dir,
            CatalogLoader.GetFloat(def, "speed", 300.0f),
            new HitPayload
            {
                RawDamage = CatalogLoader.GetFloat(def, "damage", 9.0f),
                SourceFaction = Team.Enemy,
                AttackerId = GetInstanceId(),
                Effect0 = fx,
                EffectCount = fxCount,
            },
            0,
            CatalogLoader.GetFloat(def, "lifetime", 5.0f),
            8.0f, "enemy_pellet");
    }

    private void TickReplicate(Dictionary def, float dt)
    {
        float interval = CatalogLoader.GetFloat(def, "interval", 18.0f);
        var state = StateOf("replicate", def, interval);
        state.Timer += dt;
        if (state.Timer < interval)
            return;
        state.Timer = 0.0f;

        var parent = GetParent();
        if (parent == null)
            return;
        float spread = CatalogLoader.GetFloat(def, "spread", 30.0f);
        var clone = EnemySpawner.CreateEnemy(EnemyId);
        if (clone == null)
            return;
        clone.GlobalPosition = GlobalPosition + new Vector2((float)GD.RandRange(-spread, spread), (float)GD.RandRange(-spread, spread));
        parent.AddChild(clone);
    }

    private void TickMitosis(Dictionary def, float dt)
    {
        float interval = CatalogLoader.GetFloat(def, "interval", 20.0f);
        var state = StateOf("mitosis", def, interval);
        state.Timer += dt;
        if (state.Timer < interval)
            return;
        state.Timer -= interval;

        float radius = CatalogLoader.GetFloat(def, "radius", 450.0f);
        int siblings = 0;
        foreach (var other in ActiveEnemies)
        {
            if (other != this && GodotObject.IsInstanceValid(other) && other.EnemyId == EnemyId
                && GlobalPosition.DistanceSquaredTo(other.GlobalPosition) <= radius * radius)
            {
                siblings++;
                if (siblings >= CatalogLoader.GetInt(def, "cap", 5))
                    return;
            }
        }

        var parent = GetParent();
        if (parent == null)
            return;
        var daughter = EnemySpawner.CreateEnemy(EnemyId);
        if (daughter == null)
            return;
        daughter.MaxHealth = MaxHealth * 0.5f;
        daughter.GlobalPosition = GlobalPosition + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * (float)GD.RandRange(34.0f, 52.0f);
        parent.AddChild(daughter);
    }

    private void TickContact(Dictionary def, float dt)
    {
        var player = PlayerRef;
        if (player == null || !GodotObject.IsInstanceValid(player) || player.IsDead)
            return;

        float interval = CatalogLoader.GetFloat(def, "interval", 1.5f);
        var state = StateOf("contact", def, 0.0f);
        state.Timer -= dt;
        if (state.Timer > 0.0f)
            return;

        float mult = CatalogLoader.GetFloat(def, "radius_mult", 1.0f);
        float reach = player.CurrentRadius * mult + BodyRadius + CatalogLoader.GetFloat(def, "margin", 0.0f);
        if (GlobalPosition.DistanceSquaredTo(player.GlobalPosition) > reach * reach)
            return;
        state.Timer = interval;

        string kind = CatalogLoader.GetString(def, "kind");
        switch (kind)
        {
            case "slow":
                player.Status.ApplySlow(CatalogLoader.GetFloat(def, "duration", 2.5f), 1.0f - CatalogLoader.GetFloat(def, "factor", 0.65f));
                break;
            case "invert":
                HitPipeline.ResolveHit(new HitPayload
                {
                    RawDamage = ContactDamage,
                    SourceFaction = Team.Enemy,
                    AttackerId = GetInstanceId(),
                }, player);
                player.ApplyInvertControls(CatalogLoader.GetFloat(def, "duration", 2.0f));
                break;
            case "drain":
                player.DrainAtp(CatalogLoader.GetFloat(def, "amount", 15.0f));
                FlashModulate(new Color(0.2f, 1.5f, 1.5f, 1.0f), 0.3f);
                if (!CatalogLoader.GetBool(def, "no_damage", false))
                {
                    HitPipeline.ResolveHit(new HitPayload
                    {
                        RawDamage = ContactDamage,
                        SourceFaction = Team.Enemy,
                        AttackerId = GetInstanceId(),
                    }, player);
                }
                break;
            default:
                HitPipeline.ResolveHit(new HitPayload
                {
                    RawDamage = CatalogLoader.GetFloat(def, "amount", ContactDamage),
                    SourceFaction = Team.Enemy,
                    AttackerId = GetInstanceId(),
                }, player);
                break;
        }
    }

    private void TickZigzag(Dictionary def, float dt)
    {
        MovementLocked = true;
        var state = StateOf("zigzag", def, 0.0f);
        state.Timer += dt;
        if (state.Timer >= CatalogLoader.GetFloat(def, "turn_interval", 0.4f))
        {
            state.Timer = 0.0f;
            float sign = GD.Randf() > 0.5f ? 1.0f : -1.0f;
            state.Dir = state.Dir.Rotated(Mathf.Pi * 0.5f * sign);
            var player = PlayerRef;
            if (player != null && GodotObject.IsInstanceValid(player))
            {
                Vector2 toPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
                state.Dir = (state.Dir + toPlayer * CatalogLoader.GetFloat(def, "bias", 0.6f)).Normalized();
            }
            Rotation = state.Dir.Angle();
        }
        Velocity = state.Dir * FloatSpeed * (Status.SpeedMultiplier);
        Position += Velocity * dt;
    }

    private void TickSegmented(Dictionary def, Dictionary? sway, float dt)
    {
        MovementLocked = true;
        var state = StateOf("segmented", def, 0.0f);
        int count = CatalogLoader.GetInt(def, "count", 10);
        float spacing = CatalogLoader.GetFloat(def, "spacing", 26.0f);
        if (state.Segs.Length != count)
        {
            state.Segs = new Vector2[count];
            for (int i = 0; i < count; i++)
                state.Segs[i] = GlobalPosition - Vector2.Right * spacing * i;
        }

        var player = PlayerRef;
        Vector2 heading = WanderDir;
        if (player != null && GodotObject.IsInstanceValid(player))
            heading = EnemySteering.AimAtIntercept(this, player.GlobalPosition, player.Velocity);
        if (heading == Vector2.Zero)
            heading = Vector2.Right;

        float speed = FloatSpeed * (Status.SpeedMultiplier);
        if (BossPhase != null)
            speed *= BossPhase.CurrentSpeedMult;

        if (sway != null)
        {
            state.Timer2 += dt * CatalogLoader.GetFloat(sway, "frequency", 3.2f);
            Vector2 perp = heading.Orthogonal();
            heading = (heading + perp * Mathf.Sin(state.Timer2) * CatalogLoader.GetFloat(sway, "amplitude", 0.85f)).Normalized();
        }

        Velocity = Velocity.Lerp(heading * speed, Mathf.Clamp(SteeringTurnRate * dt, 0.0f, 1.0f));
        Position += Velocity * dt;
        Rotation = Velocity.Angle();

        state.Segs[0] = GlobalPosition;
        for (int i = 1; i < count; i++)
        {
            Vector2 toPrev = state.Segs[i - 1] - state.Segs[i];
            if (toPrev.Length() > spacing)
                state.Segs[i] = state.Segs[i - 1] - toPrev.Normalized() * spacing;
        }

        state.Timer -= dt;
        if (state.Timer > 0.0f || player == null || !GodotObject.IsInstanceValid(player) || player.IsDead)
            return;
        float hitRange = CatalogLoader.GetFloat(def, "hit_range", 24.0f)
            + player.CurrentRadius * CatalogLoader.GetFloat(def, "hit_radius_factor", 0.35f);
        for (int i = 1; i < count; i++)
        {
            if (state.Segs[i].DistanceTo(player.GlobalPosition) <= hitRange)
            {
                state.Timer = CatalogLoader.GetFloat(def, "hit_cooldown", 0.45f);
                HitPipeline.ResolveHit(new HitPayload
                {
                    RawDamage = CatalogLoader.GetFloat(def, "damage", 14.0f) * (1.0f + CatalogLoader.GetFloat(def, "damage_per_segment", 0.05f) * i),
                    SourceFaction = Team.Enemy,
                    AttackerId = GetInstanceId(),
                }, player);
                Vector2 knock = player.GlobalPosition - state.Segs[i];
                if (knock == Vector2.Zero)
                    knock = Vector2.Up;
                player.Velocity += knock.Normalized() * CatalogLoader.GetFloat(def, "knockback", 180.0f);
                break;
            }
        }
    }

    private void TickKeepBand(Dictionary def, float dt)
    {
        var player = PlayerRef;
        if (player == null || !GodotObject.IsInstanceValid(player))
            return;
        Vector2 diff = player.GlobalPosition - GlobalPosition;
        float dist = diff.Length();
        if (dist < 0.001f)
            return;
        Vector2 dir = diff / dist;
        Rotation = dir.Angle();

        float min = CatalogLoader.GetFloat(def, "min", 350.0f);
        float max = CatalogLoader.GetFloat(def, "max", 500.0f);
        if (dist < min)
            Velocity = Velocity.Lerp(-dir * FloatSpeed * CatalogLoader.GetFloat(def, "back_mult", 1.2f), Mathf.Clamp(2.0f * dt, 0.0f, 1.0f));
        else if (dist > max)
            Velocity = Velocity.Lerp(dir * FloatSpeed, Mathf.Clamp(2.0f * dt, 0.0f, 1.0f));
    }

    private void Awaken(Dictionary def)
    {
        var state = StateOf("ambush", def, 0.0f);
        if (state.Flag)
            return;
        state.Flag = true;
        VariantActive = true;
        FloatSpeed = CatalogLoader.GetFloat(def, "speed_to", 85.0f);
        if (System.Enum.TryParse(CatalogLoader.GetString(def, "threat_to", "ChemoChaser"), true, out EnemyThreatMode tm))
            ThreatMode = tm;
        FlashModulate(Colors.Red * 2.0f, 0.4f);
        RedrawIfVisible();
    }

    private void TickAmbush(Dictionary def, float dt)
    {
        var state = StateOf("ambush", def, 0.0f);
        if (state.Flag)
            return;
        var player = PlayerRef;
        if (player == null || !GodotObject.IsInstanceValid(player) || player.Stats == null)
            return;
        float hpRatio = player.Health / Mathf.Max(1.0f, player.Stats.GetStat("max_health"));
        if (hpRatio < CatalogLoader.GetFloat(def, "wake_player_hp", 0.75f)
            || GlobalPosition.DistanceTo(player.GlobalPosition) < CatalogLoader.GetFloat(def, "wake_dist", 150.0f))
            Awaken(def);
    }

    private void TickAuraPull(Dictionary def, float dt)
    {
        float interval = CatalogLoader.GetFloat(def, "interval", 5.5f);
        var state = StateOf("aura_pull", def, interval);
        if (state.Timer2 <= 0.0f)
        {
            state.Timer -= dt;
            if (state.Timer > 0.0f)
                return;
            state.Timer = interval;
            state.Timer2 = CatalogLoader.GetFloat(def, "duration", 1.6f);
            RedrawIfVisible();
        }
        state.Timer2 -= dt;
        var player = PlayerRef;
        if (player != null && GodotObject.IsInstanceValid(player))
        {
            Vector2 pullDir = (GlobalPosition - player.GlobalPosition).Normalized();
            player.Velocity += pullDir * CatalogLoader.GetFloat(def, "speed", 180.0f) * dt;
        }
    }

    private void TickAuraDot(Dictionary def, float dt)
    {
        float interval = CatalogLoader.GetFloat(def, "interval", 0.9f);
        var state = StateOf("aura_dot", def, interval);
        state.Timer -= dt;
        if (state.Timer > 0.0f)
            return;
        state.Timer = interval;

        var player = PlayerRef;
        if (player == null || !GodotObject.IsInstanceValid(player) || player.IsDead)
            return;
        float reach = CatalogLoader.GetFloat(def, "radius", 170.0f)
            + player.CurrentRadius * CatalogLoader.GetFloat(def, "radius_player_mult", 0.5f);
        if (GlobalPosition.DistanceTo(player.GlobalPosition) > reach)
            return;
        HitPipeline.ResolveHit(new HitPayload
        {
            RawDamage = CatalogLoader.GetFloat(def, "damage", 10.0f),
            SourceFaction = Team.Enemy,
            AttackerId = GetInstanceId(),
        }, player);
        Vector2 away = player.GlobalPosition - GlobalPosition;
        if (away == Vector2.Zero)
            away = Vector2.Up;
        player.Velocity += away.Normalized() * CatalogLoader.GetFloat(def, "shove", 180.0f);
    }

    private void TickSlowAura(Dictionary def, float dt)
    {
        var player = PlayerRef;
        if (player == null || !GodotObject.IsInstanceValid(player) || player.IsDead)
            return;
        float hpRatio = Mathf.Clamp(CurrentHealth / Mathf.Max(1.0f, MaxHealth), 0.0f, 1.0f);
        float radius = Mathf.Lerp(
            CatalogLoader.GetFloat(def, "radius_base", 420.0f),
            CatalogLoader.GetFloat(def, "radius_max", 760.0f),
            1.0f - hpRatio);
        if (GlobalPosition.DistanceTo(player.GlobalPosition) <= radius)
            player.Status.ApplySlow(CatalogLoader.GetFloat(def, "duration", 0.25f), 1.0f - CatalogLoader.GetFloat(def, "factor", 0.78f));
    }

    private void TickTraction(Dictionary def, float dt)
    {
        float interval = CatalogLoader.GetFloat(def, "interval", 6.0f);
        var state = StateOf("traction_pulse", def, interval);
        state.Timer -= dt;
        if (state.Timer > 0.0f)
            return;
        state.Timer = interval;

        var player = PlayerRef;
        if (player == null || !GodotObject.IsInstanceValid(player) || player.IsDead)
            return;
        var aura = Trait("slow_aura");
        float radius = 760.0f;
        if (aura != null)
        {
            float hpRatio = Mathf.Clamp(CurrentHealth / Mathf.Max(1.0f, MaxHealth), 0.0f, 1.0f);
            radius = Mathf.Lerp(
                CatalogLoader.GetFloat(aura, "radius_base", 420.0f),
                CatalogLoader.GetFloat(aura, "radius_max", 760.0f),
                1.0f - hpRatio);
        }
        radius += CatalogLoader.GetFloat(def, "radius_bonus", 140.0f);
        Vector2 pull = GlobalPosition - player.GlobalPosition;
        if (pull.Length() > 0.001f && pull.Length() <= radius)
        {
            player.Velocity += pull.Normalized() * CatalogLoader.GetFloat(def, "pull", 260.0f);
            var waveParent = GetParent();
            if (waveParent != null)
                waveParent.AddChild(new ShockRing { MaxRadius = radius, GlobalPosition = GlobalPosition });
        }
    }

    private void TickFeed(Dictionary def, float dt)
    {
        float interval = CatalogLoader.GetFloat(def, "interval", 5.0f);
        var state = StateOf("feed", def, interval);
        state.Timer -= dt;
        if (state.Timer > 0.0f)
            return;
        state.Timer = interval;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + MaxHealth * CatalogLoader.GetFloat(def, "pct", 0.06f));
    }

    private void TickCleanse(Dictionary def, float dt)
    {
        float interval = CatalogLoader.GetFloat(def, "interval", 25.0f);
        var state = StateOf("cleanse_pulse", def, interval);
        state.Timer += dt;
        if (state.Timer < interval)
            return;
        state.Timer = 0.0f;

        var player = PlayerRef;
        if (player != null && GodotObject.IsInstanceValid(player))
        {
            Vector2 diff = player.GlobalPosition - GlobalPosition;
            if (diff.Length() < CatalogLoader.GetFloat(def, "radius", 250.0f) && diff.Length() > 0.001f)
                player.Velocity += diff.Normalized() * CatalogLoader.GetFloat(def, "knockback", 200.0f);
        }
        if (CatalogLoader.GetBool(def, "clear_mark", false))
        {
            foreach (var other in ActiveEnemies)
            {
                if (other != null && GodotObject.IsInstanceValid(other))
                {
                    if (other.HasMeta("mhc_marked"))
                        other.RemoveMeta("mhc_marked");
                    other.Status.ClearChannel("amp");
                }
            }
        }
        var waveParent = GetParent();
        if (waveParent != null)
            waveParent.AddChild(new ShockRing
            {
                MaxRadius = CatalogLoader.GetFloat(def, "radius", 250.0f),
                GlobalPosition = GlobalPosition
            });
        FlashModulate(Colors.White * 2.0f, 0.3f);
    }

    private void TickLatchLesion(Dictionary def, float dt)
    {
        if (!EnemySteering.IsInvaderLatched(this))
            return;
        float interval = CatalogLoader.GetFloat(def, "interval", 3.0f);
        var state = StateOf("latch_lesion", def, interval);
        state.Timer -= dt;
        if (state.Timer > 0.0f)
            return;
        state.Timer = interval;

        Game.HostUlceration.RegisterPulse();
        var parent = GetParent();
        if (parent == null)
            return;
        float lesionTick = CatalogLoader.GetFloat(def, "tick", 0.6f);
        var zone = new Zone
        {
            SourceTeam = Team.Enemy,
            GlobalPosition = GlobalPosition,
            Duration = CatalogLoader.GetFloat(def, "lifetime", 4.0f),
            Radius = CatalogLoader.GetFloat(def, "radius", 46.0f),
            Damage = CatalogLoader.GetFloat(def, "damage", 4.0f),
            TickInterval = lesionTick,
            SlowFactor = CatalogLoader.GetFloat(def, "slow_factor", 0.6f),
            SlowDuration = lesionTick * 1.5f
        };
        ApplyZoneColors(zone, def);
        parent.AddChild(zone);
        LatchPulses++;
    }

    private void TickTimedZone(Dictionary def, float dt)
    {
        float interval = CatalogLoader.GetFloat(def, "interval", 2.8f);
        var state = StateOf("timed_zone", def, interval);
        state.Timer -= dt;
        if (state.Timer > 0.0f)
            return;
        state.Timer = interval;

        state.Zones.RemoveAll(z => z == null || !GodotObject.IsInstanceValid(z));
        if (state.Zones.Count >= CatalogLoader.GetInt(def, "cap", 6))
            return;
        var parent = GetParent();
        if (parent == null)
            return;
        float zoneTick = CatalogLoader.GetFloat(def, "tick", 0.5f);
        float zoneDps = CatalogLoader.GetFloat(def, "dps", 6.0f);
        var zone = new Zone
        {
            SourceTeam = Team.Enemy,
            GlobalPosition = GlobalPosition,
            Duration = CatalogLoader.GetFloat(def, "lifetime", 9.0f),
            Radius = CatalogLoader.GetFloat(def, "radius", 40.0f),
            Damage = zoneDps * zoneTick,
            TickInterval = zoneTick,
            SlowFactor = CatalogLoader.GetFloat(def, "slow_factor", 0.55f),
            SlowDuration = zoneTick * 1.5f,
            GrowFrom = CatalogLoader.GetFloat(def, "radius", 40.0f),
            GrowRate = CatalogLoader.GetFloat(def, "grow_rate", 0.0f)
        };
        zone.Radius = CatalogLoader.GetFloat(def, "grow_max", zone.Radius);
        ApplyZoneColors(zone, def);
        parent.AddChild(zone);
        state.Zones.Add(zone);
    }

    private void TickDashSuicide(Dictionary def, float dt)
    {
        var state = StateOf("dash_suicide", def, 0.0f);
        if (state.Timer <= 0.0f || state.Flag)
            return;
        MovementLocked = true;
        state.Timer = Mathf.Max(0.0f, state.Timer - dt);
        float speed = state.Timer > 0.0f ? FloatSpeed : FloatSpeed * 0.25f;
        Velocity = Velocity.Lerp(state.Dir * speed, Mathf.Clamp(3.0f * dt, 0.0f, 1.0f));
        Position += Velocity * dt;

        var player = PlayerRef;
        if (player == null || !GodotObject.IsInstanceValid(player) || player.IsDead)
            return;
        float reach = CatalogLoader.GetFloat(def, "hit_margin", 20.0f)
            + player.CurrentRadius * CatalogLoader.GetFloat(def, "hit_radius_mult", 0.4f);
        if (GlobalPosition.DistanceTo(player.GlobalPosition) <= reach)
        {
            state.Flag = true;
            HitPipeline.ResolveHit(new HitPayload
            {
                RawDamage = CatalogLoader.GetFloat(def, "hit_damage", 9.0f),
                SourceFaction = Team.Enemy,
                AttackerId = GetInstanceId(),
            }, player);
            Die(null);
        }
    }

    /// <summary>Launches a dash-suicide mote along a direction.</summary>
    public void Launch(Vector2 direction)
    {
        var state = StateOf("dash_suicide", new Dictionary(), 0.0f);
        state.Dir = direction.LengthSquared() > 0.0001f ? direction.Normalized() : Vector2.Right;
        state.Timer = CatalogLoader.GetFloat(Trait("dash_suicide") ?? new Dictionary(), "dash_time", 2.6f);
        Velocity = state.Dir * FloatSpeed;
    }

    private void SplitBurst(Dictionary def)
    {
        if (!TryConsumeSplitBurst())
            return;
        string pattern = CatalogLoader.GetString(def, "pattern");
        string spawn = CatalogLoader.GetString(def, "spawn");
        int count = CatalogLoader.GetInt(def, "count", 2);
        if (pattern == "orthogonal")
        {
            Vector2[] dirs = [Vector2.Up, Vector2.Down, Vector2.Left, Vector2.Right];
            float offset = CatalogLoader.GetFloat(def, "offset", 34.0f);
            for (int i = 0; i < count && i < dirs.Length; i++)
            {
                var mote = SpawnMinion(spawn, GlobalPosition + dirs[i] * offset, Vector2.Zero);
                mote?.Launch(dirs[i]);
            }
            var waveParent = GetParent();
            if (waveParent != null)
                waveParent.AddChild(new ShockRing
                {
                    MaxRadius = CatalogLoader.GetFloat(def, "wave_radius", 300.0f),
                    GlobalPosition = GlobalPosition
                });
            return;
        }
        if (pattern == "split_pair")
        {
            SpawnMinion(spawn, GlobalPosition + new Vector2(-25.0f, (float)GD.RandRange(-15.0f, 15.0f)), new Vector2(-60.0f, 0.0f));
            SpawnMinion(spawn, GlobalPosition + new Vector2(25.0f, (float)GD.RandRange(-15.0f, 15.0f)), new Vector2(60.0f, 0.0f));
            return;
        }
        RuptureAt(spawn, count, CatalogLoader.GetFloat(def, "speed", 0.0f),
            CatalogLoader.GetFloat(def, "ring_offset", 0.0f), CatalogLoader.GetFloat(def, "jitter", 0.0f));
    }

    private void Rupture(Dictionary def)
    {
        if (!TryConsumeSplitBurst())
            return;
        int count = CatalogLoader.GetInt(def, "count", 6);
        int countMax = CatalogLoader.GetInt(def, "count_max", count);
        if (countMax > count)
            count = (int)GD.RandRange(count, countMax);
        RuptureAt(
            CatalogLoader.GetString(def, "spawn"),
            count,
            CatalogLoader.GetFloat(def, "speed", 0.0f),
            CatalogLoader.GetFloat(def, "ring_offset", 0.0f),
            CatalogLoader.GetFloat(def, "jitter", 0.3f),
            CatalogLoader.GetFloat(def, "offset_min", 0.0f),
            CatalogLoader.GetFloat(def, "offset_max", 0.0f));
    }

    private void RuptureAt(string spawnId, int count, float speed, float ringOffset, float jitter, float offsetMin = 0.0f, float offsetMax = 0.0f)
    {
        var parent = GetParent();
        if (parent == null || spawnId == "")
            return;
        for (int i = 0; i < count; i++)
        {
            float angle = count > 1 ? i * (Mathf.Tau / count) : 0.0f;
            if (jitter > 0.0f)
                angle += (float)GD.RandRange(-jitter, jitter);
            float off = ringOffset;
            if (offsetMax > offsetMin)
                off = (float)GD.RandRange(offsetMin, offsetMax);
            Vector2 dir = Vector2.FromAngle(angle);
            SpawnMinion(spawnId, GlobalPosition + dir * off, dir * speed);
        }
    }

    private void TransformSpawn(Dictionary def)
    {
        if (!TryConsumeSplitBurst())
            return;
        var child = SpawnMinion(CatalogLoader.GetString(def, "spawn"), GlobalPosition, Vector2.Zero);
        if (child != null)
        {
            child.FloatSpeed *= CatalogLoader.GetFloat(def, "speed_mult", 1.8f);
            child.DamageMult = CatalogLoader.GetFloat(def, "damage_mult", 1.5f);
        }
        var parent = GetParent();
        if (parent != null)
            parent.AddChild(new ShockRing { MaxRadius = 120.0f, GlobalPosition = GlobalPosition });
        QueueFree();
    }

    private EnemyActor? SpawnMinion(string spawnId, Vector2 at, Vector2 velocity)
    {
        if (spawnId == "")
            return null;
        var parent = GetParent();
        if (parent == null)
            return null;
        var child = EnemySpawner.CreateEnemy(spawnId);
        if (child == null)
            return null;
        child.GlobalPosition = at;
        if (velocity != Vector2.Zero)
            child.Velocity = velocity;
        parent.AddChild(child);
        return child;
    }

    private void SpawnZone(Dictionary def, Vector2 at)
    {
        var parent = GetParent();
        if (parent == null)
            return;
        float tick = CatalogLoader.GetFloat(def, "tick", 0.5f);
        float dps = CatalogLoader.GetFloat(def, "dps", 8.0f);
        var zone = new Zone
        {
            SourceTeam = Team.Enemy,
            GlobalPosition = at,
            Duration = CatalogLoader.GetFloat(def, "lifetime", 6.0f),
            Radius = CatalogLoader.GetFloat(def, "radius", 100.0f),
            Damage = dps * tick,
            TickInterval = tick,
            SlowFactor = CatalogLoader.GetBool(def, "slow", true)
                ? CatalogLoader.GetFloat(def, "slow_factor", 0.5f)
                : -1.0f,
            SlowDuration = tick * 1.5f,
            GrowFrom = CatalogLoader.GetFloat(def, "grow_from", -1.0f),
            GrowTime = CatalogLoader.GetFloat(def, "grow_time", 1.5f)
        };
        ApplyZoneColors(zone, def);
        parent.AddChild(zone);
    }

    private static void ApplyZoneColors(Zone zone, Dictionary def)
    {
        string coreHex = CatalogLoader.GetString(def, "core_color");
        string rimHex = CatalogLoader.GetString(def, "rim_color");
        if (coreHex != "")
            zone.CoreColor = new Color(coreHex);
        if (rimHex != "")
            zone.RimColor = new Color(rimHex);
    }

    public override void _Draw()
    {
        float r = BodyRadius;
        DrawCircle(Vector2.Zero, r, SwarmBatchColor);
        DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 24, new Color(1.0f, 1.0f, 1.0f, 0.5f), 1.5f);
    }
}
