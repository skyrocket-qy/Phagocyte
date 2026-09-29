using Godot;
using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Player;
using Game.UI;

namespace Game.Enemies;

public partial class EnemyActor : Node2D, IDamageable, ISlowable, IStunnable, IStatusHost
{
    [Signal]
    public delegate void EnemyDiedEventHandler(EnemyActor enemy);

    [Export] public string EnemyId { get; set; } = "base_enemy";
    [Export] public string DisplayNameKey { get; set; } = "ENEMY_BASE";
    [Export] public float MaxHealth { get; set; } = 20.0f;
    [Export] public float CurrentHealth { get; set; } = 20.0f;
    [Export] public float Armor { get; set; } = 0.0f;
    [Export] public int ShieldCharges { get; set; } = 0;
    [Export] public float XpValue { get; set; } = 12.0f;
    [Export] public int BaseScore { get; set; } = 15;
    [Export] public float FloatSpeed { get; set; } = 35.0f;
    [Export] public bool IsElite { get; set; } = false;
    [Export] public bool IsBoss { get; set; } = false;

    /// <summary>Tactical threat intent driving steering (see docs/enemy.md).</summary>
    [Export] public EnemyThreatMode ThreatMode { get; set; } = EnemyThreatMode.Drifter;

    /// <summary>
    /// Instance tint used by the GPU swarm batch renderer (docs/spec.md §9).
    /// Set from the enemy def; defaults to white so the baked batch texture
    /// is drawn unmodified.
    /// </summary>
    public Color SwarmBatchColor { get; set; } = Colors.White;

    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public float DriftTimer { get; set; } = 0.0f;
    public Vector2 WanderDir { get; set; } = Vector2.Zero;
    public float BreatheTimer { get; set; } = 0.0f;

    // Threat steering tuning
    public float SteeringPhase { get; set; } = 0.0f;
    public Vector2 SteeringAnchor { get; set; } = Vector2.Zero;
    public float SteeringOrbitSign { get; set; } = 1.0f;
    public bool UseGenericSteering { get; set; } = true;
    public float SteeringTurnRate { get; set; } = 2.6f;
    public float SteeringPreferredRange { get; set; } = 260.0f;
    public float SteeringLatchRange { get; set; } = 40.0f;

    // Status debuffs & Components (slow state lives in Status, not fields)
    public float StunTimer { get; set; } = 0.0f;
    public StatusController? Status { get; private set; }
    public BossPhaseComponent? BossPhase { get; private set; }

    public Area2D? HitArea { get; set; }

    private Tween? _flashTween;
    private float _lastBreathe = 0.0f;

    private bool _onScreen = true;
    private float _breathAccum;
    private const float BreathInterval = 1.0f / 30.0f;

    private static readonly HashSet<EnemyActor> _activeEnemies = new();
    public static IReadOnlyCollection<EnemyActor> ActiveEnemies => _activeEnemies;

    public override void _EnterTree()
    {
        base._EnterTree();
        _activeEnemies.Add(this);
    }

    public override void _Ready()
    {
        _activeEnemies.Add(this);
        AddToGroup("enemies");
        CurrentHealth = MaxHealth;
        DriftTimer = GD.Randf() * 5.0f;
        BreatheTimer = GD.Randf() * 10.0f;
        WanderDir = Vector2.FromAngle(GD.Randf() * Mathf.Tau);
        SteeringPhase = GD.Randf() * 10.0f;
        SteeringOrbitSign = GD.Randf() < 0.5f ? -1.0f : 1.0f;

        Status = GetNodeOrNull<StatusController>("StatusController");
        if (Status == null)
        {
            Status = new StatusController { Name = "StatusController" };
            AddChild(Status);
        }

        BossPhase = GetNodeOrNull<BossPhaseComponent>("BossPhaseComponent");

        EnsureCollisionNodes();
    }

    /// <summary>
    /// Brief modulate flash used by damage and ability feedback: sets the tint
    /// and tweens it back to white over <paramref name="duration"/> seconds.
    /// Coalesced: focus fire reuses one tween instead of stacking a new one
    /// per hit (FPS survey §4).
    /// </summary>
    protected void FlashModulate(Color flashColor, float duration)
    {
        if (_flashTween != null && _flashTween.IsValid())
            _flashTween.Kill();
        Modulate = flashColor;
        _flashTween = CreateTween();
        _flashTween.TweenProperty(this, "modulate", Colors.White, duration);
    }

    /// <summary>
    /// Cached player lookup shared by subclass AI: avoids a per-frame scene-tree
    /// group query across the 300-500 enemy concurrency budget. The cache is
    /// shared process-wide and re-resolved when the cell is freed.
    /// </summary>
    protected PlayerActor? PlayerRef => EnemySteering.GetPlayer(this);

    /// <summary>Collision radius in pixels, from the def's body size.</summary>
    public virtual float GetCollisionRadius()
    {
        return BodyDeformation.RealSizeToRadius(_bodyMicrons);
    }

    /// <summary>Public body radius for hit-presentation (envelope sizing).</summary>
    public float BodyRadius => GetCollisionRadius();

    /// <summary>
    /// Batch variant override (dormant/retracted states with distinct art).
    /// Null selects the base EnemyId species. Visual only; logic untouched.
    /// </summary>
    public string? BatchSpeciesOverride => !VariantActive && _dormantVariant != "" ? _dormantVariant : null;

    /// <summary>
    /// True when this enemy overlaps a live player cell, using the player's
    /// current radius plus <paramref name="margin"/>. Shared by contact effects.
    /// </summary>
    protected bool IsTouchingPlayer(float margin)
    {
        var player = PlayerRef;
        if (player == null || !GodotObject.IsInstanceValid(player) || player.IsDead)
            return false;

        float reach = player.CurrentRadius + margin;
        return GlobalPosition.DistanceSquaredTo(player.GlobalPosition) < reach * reach;
    }

    /// <summary>
    /// Far-enemy sleep + draw cull, driven by camera distance
    /// (see SyncCullState). Non-boss enemies outside the sleep range skip
    /// their physics tick entirely and queue no redraws. They stay in the
    /// tree and all groups, so scans and batching still see them.
    /// </summary>
    private int _cullTick;

    private void SyncCullState()
    {
        var vp = GetViewport();
        var cam = vp?.GetCamera2D();
        if (cam == null || vp == null)
        {
            _onScreen = true;
            return;
        }
        float zoom = cam.Zoom.X > 0.0f ? cam.Zoom.X : 1.0f;
        float range = vp.GetVisibleRect().Size.Length() * 0.5f / zoom
            + GetCollisionRadius() + 200.0f;
        _onScreen = GlobalPosition.DistanceSquaredTo(cam.GlobalPosition) < range * range;
    }

    /// <summary>Queue a redraw only when the body is on screen.</summary>
    protected void RedrawIfVisible()
    {
        if (_onScreen)
            QueueRedraw();
    }

    private void EnsureCollisionNodes()
    {
        HitArea = GetNodeOrNull<Area2D>("HitArea");
        if (HitArea == null)
        {
            // Passive target only: enemies are detected *by* player skills and the
            // contact sensor, so the enemy-side monitor is disabled to cut 2D physics
            // broadphase cost at 300-500 concurrent bodies (docs/spec.md §9).
            HitArea = new Area2D
            {
                Name = "HitArea",
                CollisionLayer = 2, // Layer 2: Enemies
                CollisionMask = 0,
                Monitoring = false,
                Monitorable = true
            };
            AddChild(HitArea);
        }

        CollisionShape2D? collisionShape = HitArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collisionShape == null)
        {
            collisionShape = new CollisionShape2D
            {
                Name = "CollisionShape2D",
                Shape = new CircleShape2D { Radius = GetCollisionRadius() }
            };
            HitArea.AddChild(collisionShape);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        _cullTick++;
        // Headless suites have no viewport: skip entirely so behavior there
        // is bit-identical to before (signals never fire headless either).
        if ((_cullTick % 30) == 1 && DisplayServer.GetName() != "headless")
            SyncCullState();
        if (!_onScreen && !IsBoss)
            return;

        float dt = (float)delta;

        if (StunTimer > 0.0f)
        {
            StunTimer -= dt;
            return;
        }

        PreDriftTraits();
        HandleBrownianDrift(dt);
        TickTraits(dt);

        // Organic respiration at 30 Hz like the deform rebuild: the slow
        // sine is invisible per-tick, and gating avoids dirtying the whole
        // subtree transform (plus physics/render sync) 60 times a second.
        _breathAccum += dt;
        if (_breathAccum >= BreathInterval)
        {
            BreatheTimer += _breathAccum;
            _breathAccum = 0.0f;
            float breathe = 1.0f + Mathf.Sin(BreatheTimer * 2.5f) * 0.04f;
            if (Mathf.Abs(breathe - _lastBreathe) > 0.001f)
            {
                _lastBreathe = breathe;
                Scale = new Vector2(breathe, breathe);
            }
        }
    }

    protected virtual void HandleBrownianDrift(float dt)
    {
        if (MovementLocked)
        {
            Velocity = Vector2.Zero;
            return;
        }
        DriftTimer += dt;
        SteeringPhase += dt;
        if (DriftTimer > 2.5f)
        {
            DriftTimer = 0.0f;
            WanderDir = (WanderDir + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 0.7f).Normalized();
        }

        float currentSpeed = FloatSpeed * (Status?.SpeedMultiplier ?? 1.0f);
        if (BossPhase != null)
        {
            currentSpeed *= BossPhase.CurrentSpeedMult;
        }

        Vector2 steering = UseGenericSteering ? EnemySteering.GetDirection(this, dt) : Vector2.Zero;
        if (ThreatMode == EnemyThreatMode.Invader && steering.LengthSquared() <= 0.0001f)
        {
            // Latched tissue invader: hold position while ulcerating the anchor site.
            Velocity = Vector2.Zero;
            return;
        }

        if (steering.LengthSquared() > 0.0001f)
        {
            Vector2 desired = steering.Normalized() * currentSpeed;
            Velocity = Velocity.Lerp(desired, Mathf.Clamp(SteeringTurnRate * dt, 0.0f, 1.0f));
        }
        else
        {
            Velocity = Velocity.Lerp(WanderDir * currentSpeed, Mathf.Clamp(2.0f * dt, 0.0f, 1.0f));
        }

        Position += Velocity * dt;
    }

    public virtual void TakeDamage(float damage, Node2D? source = null)
    {
        TakeDamageInternal(damage, source, false);
    }

    public virtual void TakeDamage(float damage, Node2D? source, bool isCrit)
    {
        TakeDamageInternal(damage, source, isCrit);
    }

    protected void TakeDamageInternal(float damage, Node2D? source, bool isCrit)
    {
        PreDamageTraits(damage, source);

        damage = ApplyShellAbsorb(damage, source, isCrit);

        if (ShieldCharges > 0)
        {
            ShieldCharges--;
            RedrawIfVisible();
            return;
        }

        if (BossPhase != null)
        {
            damage = BossPhase.ApplyDamageReduction(damage);
        }

        if (Status != null)
        {
            damage *= Status.DamageTakenMultiplier;
        }

        float effectiveDmg = Mathf.Max(1.0f, damage - Armor);
        CurrentHealth -= effectiveDmg;

        BossPhase?.NotifyHealthChanged(CurrentHealth, MaxHealth);

        DamageNumberSpawner.ShowDamage(GlobalPosition, effectiveDmg, isCrit);
        RunTelemetryManager.Instance?.RecordDamageDealt(source?.Name ?? "direct", effectiveDmg);

        // Life steal check on attacker
        if (source is PlayerActor playerCell && playerCell.Stats != null)
        {
            if (playerCell.Stats.RollLifeSteal())
            {
                playerCell.Heal(1.0f);
                DamageNumberSpawner.ShowHeal(playerCell.GlobalPosition, 1.0f);
                RunTelemetryManager.Instance?.RecordLifeSteal(1.0f);
            }
        }

        AudioManager.Instance?.PlayHit(isCrit);
        if (isCrit)
        {
            VfxManager.Instance?.Play(VfxType.BarbImpact, GlobalPosition);
        }

        // Flash modulate
        FlashModulate(new Color(1.8f, 0.4f, 0.4f, 1.0f), 0.15f);

        if (CurrentHealth <= 0.0f)
        {
            Die(source);
        }
        else
        {
            RedrawIfVisible();
        }

        PostDamageTraits(damage, source);
    }

    public virtual void TakeDoTDamage(float dotDamage)
    {
        if (CurrentHealth <= 0.0f)
            return;

        if (BossPhase != null)
        {
            dotDamage = BossPhase.ApplyDamageReduction(dotDamage);
        }

        CurrentHealth -= dotDamage;
        RunTelemetryManager.Instance?.RecordDamageDealt("ailment_dot", dotDamage);
        BossPhase?.NotifyHealthChanged(CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0.0f)
        {
            Die(null);
        }
    }

    /// <summary>
    /// Contact damage dealt to a cell on touch. One-directional by design:
    /// monsters hurt the cell; the cell never hurts monsters by touching.
    /// From the enemy def.
    /// </summary>
    [Export] public float ContactDamage { get; set; } = 3.0f;

    /// <summary>Seconds between contact ticks against the same cell.</summary>
    public float ContactTickInterval { get; set; } = 1.0f;

    /// <summary>Next allowed contact-tick timestamp (msec).</summary>
    public double NextContactTickMsec { get; set; }

    /// <summary>
    /// Survivor-like contact strike: at most one hit per
    /// <see cref="ContactTickInterval"/>. Routes through the cell's
    /// evasion/block/armor pipeline.
    /// </summary>
    public bool TryContactStrike(PlayerActor cell)
    {
        if (cell == null || !GodotObject.IsInstanceValid(cell) || cell.IsDead)
            return false;
        if (CurrentHealth <= 0.0f)
            return false;
        double now = Time.GetTicksMsec();
        if (now < NextContactTickMsec)
            return false;
        NextContactTickMsec = now + ContactTickInterval * 1000.0;
        cell.TakeDamage(ContactDamage);
        return true;
    }

    private bool _splitBurstConsumed;

    /// <summary>
    /// One-shot guard shared by split/burst effects (death splits and
    /// low-health bursts). Returns true the first call only.
    /// </summary>
    public bool TryConsumeSplitBurst()
    {
        if (_splitBurstConsumed)
            return false;

        _splitBurstConsumed = true;
        return true;
    }

    /// <summary>
    /// Drops a hazard pool at this enemy's position, pruning destroyed entries
    /// first and enforcing <paramref name="maxPools"/>. Returns null when the
    /// cap is reached or there is no parent to attach to.
    /// </summary>
    protected THazard? SpawnHazard<THazard>(List<THazard> tracked, int maxPools) where THazard : Node2D, new()
    {
        tracked.RemoveAll(pool => pool == null || !GodotObject.IsInstanceValid(pool));
        if (tracked.Count >= maxPools)
            return null;

        var parent = GetParent();
        if (parent == null)
            return null;

        var hazard = new THazard { GlobalPosition = GlobalPosition };
        parent.AddChild(hazard);
        tracked.Add(hazard);
        return hazard;
    }

    public override void _ExitTree()
    {
        _activeEnemies.Remove(this);
        base._ExitTree();
    }

    // Virtual for the testing dummy (Game.Debug.TargetDummy), which
    // suppresses death/exp. Production enemies never subclass.
    public virtual void Die(Node2D? killer)
    {
        RunDeathTraits();
        AudioManager.Instance?.PlayEnemyDeath();
        // All progression EXP flows through death, regardless of what
        // killed it (skills, DoT, hazards) — never grant EXP anywhere else
        // for enemies.
        var player = PlayerRef;
        if (player != null && GodotObject.IsInstanceValid(player))
            player.AddExp(XpValue * EnemySpawner.ExpGainMultiplier);
        // Equipment chamber equipment drops here too: one low-chance roll per
        // kill, spawned as a collectable pickup (TODO Phase 1 revision).
        EquipmentUnlockManager.TrySpawnDrop(GlobalPosition, GetParent(), player);
        AchievementManager.RecordEvent("enemy_killed", EnemyId);
        RunTelemetryManager.Instance?.RecordKill(BaseScore);
        EmitSignal(SignalName.EnemyDied, this);
        QueueFree();
    }

    public void ApplySlow(float duration, float factor)
    {
        // Slow state lives in data (slow-channel carrier, strongest wins);
        // this stays only as the ISlowable dispatch endpoint.
        Status?.ApplySlow(duration, 1.0f - factor);
    }

    public void ApplyStun(float duration)
    {
        StunTimer = duration;
    }

    // Compatibility methods for duck-typing
    public float GetXpValue() => XpValue;
    public float get_atp_value() => XpValue;
    public int GetBaseScore() => BaseScore;
    public int get_base_score() => BaseScore;
    public void take_damage(float damage, Node2D? source = null) => TakeDamage(damage, source, false);
    public void take_damage(float damage, Node2D? source, bool isCrit) => TakeDamage(damage, source, isCrit);
}


