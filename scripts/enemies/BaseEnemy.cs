using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.UI;

namespace Phagocyte.Enemies;

public abstract partial class BaseEnemy : Node2D, IDamageable, IEngulfable
{
    [Signal]
    public delegate void DigestedEventHandler(BaseEnemy enemy);

    [Signal]
    public delegate void EnemyDiedEventHandler(BaseEnemy enemy);

    [Export] public string EnemyId { get; set; } = "base_enemy";
    [Export] public string DisplayNameKey { get; set; } = "PATHOGEN_BASE";
    [Export] public float MaxHealth { get; set; } = 20.0f;
    [Export] public float CurrentHealth { get; set; } = 20.0f;
    [Export] public float Armor { get; set; } = 0.0f;
    [Export] public int FibrinShield { get; set; } = 0;
    [Export] public float AtpValue { get; set; } = 12.0f;
    [Export] public int BaseScore { get; set; } = 15;
    [Export] public float FloatSpeed { get; set; } = 35.0f;
    [Export] public float DriftFrequency { get; set; } = 1.2f;
    [Export] public bool IsElite { get; set; } = false;
    [Export] public bool IsBoss { get; set; } = false;

    /// <summary>Tactical threat intent driving steering (see docs/pathogen.md).</summary>
    [Export] public EnemyThreatMode ThreatMode { get; set; } = EnemyThreatMode.Drifter;

    /// <summary>
    /// Instance tint used by the GPU swarm batch renderer (docs/spec.md §9).
    /// Defaults to white so the baked batch texture is drawn unmodified.
    /// </summary>
    public virtual Color SwarmBatchColor => Colors.White;

    public bool IsBeingEaten { get; set; } = false;
    public virtual bool CanBeEngulfed => FibrinShield <= 0;

    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public float DriftTimer { get; set; } = 0.0f;
    public Vector2 WanderDir { get; set; } = Vector2.Zero;
    public float BreatheTimer { get; set; } = 0.0f;

    // Threat steering tuning
    public float SteeringPhase { get; set; } = 0.0f;
    public Vector2 SteeringAnchor { get; set; } = Vector2.Zero;
    public float SteeringOrbitSign { get; set; } = 1.0f;
    protected virtual bool UseGenericSteering => true;
    protected virtual float SteeringTurnRate => 2.6f;
    public virtual float SteeringPreferredRange => 260.0f;
    public virtual float SteeringLatchRange => 40.0f;

    // Status debuffs & Components
    public float SlowTimer { get; set; } = 0.0f;
    public float SlowFactor { get; set; } = 1.0f;
    public float StunTimer { get; set; } = 0.0f;
    public AilmentController? Ailments { get; private set; }
    public BossPhaseComponent? BossPhase { get; private set; }

    public Area2D? HitArea { get; set; }
    public CollisionShape2D? EnemyCollisionShape { get; set; }

    private static readonly HashSet<BaseEnemy> _activeEnemies = new();
    public static IReadOnlyCollection<BaseEnemy> ActiveEnemies => _activeEnemies;

    public override void _EnterTree()
    {
        base._EnterTree();
        _activeEnemies.Add(this);
    }

    public override void _Ready()
    {
        _activeEnemies.Add(this);
        AddToGroup("pathogens");
        CurrentHealth = MaxHealth;
        DriftTimer = GD.Randf() * 5.0f;
        BreatheTimer = GD.Randf() * 10.0f;
        WanderDir = Vector2.FromAngle(GD.Randf() * Mathf.Tau);
        SteeringPhase = GD.Randf() * 10.0f;
        SteeringOrbitSign = GD.Randf() < 0.5f ? -1.0f : 1.0f;

        Ailments = GetNodeOrNull<AilmentController>("AilmentController");
        if (Ailments == null)
        {
            Ailments = new AilmentController { Name = "AilmentController" };
            AddChild(Ailments);
        }

        BossPhase = GetNodeOrNull<BossPhaseComponent>("BossPhaseComponent");

        EnsureCollisionNodes();
        SetupEnemy();
    }

    protected virtual void SetupEnemy()
    {
    }

    /// <summary>
    /// Brief modulate flash used by damage and ability feedback: sets the tint
    /// and tweens it back to white over <paramref name="duration"/> seconds.
    /// </summary>
    protected void FlashModulate(Color flashColor, float duration)
    {
        Modulate = flashColor;
        var tw = CreateTween();
        tw.TweenProperty(this, "modulate", Colors.White, duration);
    }

    /// <summary>
    /// Cached player lookup shared by subclass AI: avoids a per-frame scene-tree
    /// group query across the 300-500 pathogen concurrency budget. The cache is
    /// shared process-wide and re-resolved when the cell is freed.
    /// </summary>
    protected BaseCell? PlayerRef => EnemySteering.GetPlayer(this);

    protected virtual float GetCollisionRadius()
    {
        return 16.0f;
    }

    /// <summary>Public body radius for hit-presentation (envelope sizing).</summary>
    public float BodyRadius => GetCollisionRadius();

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

    private void EnsureCollisionNodes()
    {
        HitArea = GetNodeOrNull<Area2D>("HitArea");
        if (HitArea == null)
        {
            // Passive target only: pathogens are detected *by* player skills and the
            // engulf area, so the enemy-side monitor is disabled to cut 2D physics
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

        EnemyCollisionShape = HitArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (EnemyCollisionShape == null)
        {
            EnemyCollisionShape = new CollisionShape2D
            {
                Name = "CollisionShape2D",
                Shape = new CircleShape2D { Radius = GetCollisionRadius() }
            };
            HitArea.AddChild(EnemyCollisionShape);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (StunTimer > 0.0f)
        {
            StunTimer -= dt;
            return;
        }

        if (SlowTimer > 0.0f)
        {
            SlowTimer -= dt;
            if (SlowTimer <= 0.0f)
                SlowFactor = 1.0f;
        }

        if (IsBeingEaten)
            return;

        HandleBrownianDrift(dt);
        CustomPhysicsProcess(dt);

        // Organic respiration
        BreatheTimer += dt;
        float breathe = 1.0f + Mathf.Sin(BreatheTimer * 2.5f) * 0.04f;
        Scale = new Vector2(breathe, breathe);
    }

    protected virtual void HandleBrownianDrift(float dt)
    {
        DriftTimer += dt;
        SteeringPhase += dt;
        if (DriftTimer > 2.5f)
        {
            DriftTimer = 0.0f;
            WanderDir = (WanderDir + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 0.7f).Normalized();
        }

        float currentSpeed = FloatSpeed * SlowFactor;
        if (Ailments != null && Ailments.IsAgglutinated)
        {
            currentSpeed *= Ailments.SpeedMultiplier;
        }
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
            Velocity = Velocity.Lerp(WanderDir * currentSpeed, 2.0f * dt);
        }

        Position += Velocity * dt;
    }

    protected virtual void CustomPhysicsProcess(float dt)
    {
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
        OnPreDamage(damage, source, isCrit);

        if (IsBeingEaten)
            return;

        if (FibrinShield > 0)
        {
            FibrinShield--;
            QueueRedraw();
            return;
        }

        if (BossPhase != null)
        {
            damage = BossPhase.ApplyDamageReduction(damage);
        }

        if (Ailments != null && Ailments.IsOpsonized)
        {
            damage *= Ailments.OpsonizationMultiplier;
        }

        float effectiveDmg = Mathf.Max(1.0f, damage - Armor);
        CurrentHealth -= effectiveDmg;

        BossPhase?.NotifyHealthChanged(CurrentHealth, MaxHealth);

        DamageNumberSpawner.ShowDamage(GlobalPosition, effectiveDmg, isCrit);
        RunTelemetryManager.Instance?.RecordDamageDealt(source?.Name ?? "direct", effectiveDmg);

        // Life steal check on attacker
        if (source is BaseCell playerCell && playerCell.Stats != null)
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
            QueueRedraw();
        }

        OnPostDamage(damage, source, isCrit);
    }

    /// <summary>
    /// Runs at the top of every damage intake (before the eaten/shield guards).
    /// Subclasses use it for hit-triggered reactions that must also fire on the
    /// crit-routed overload.
    /// </summary>
    protected virtual void OnPreDamage(float damage, Node2D? source, bool isCrit)
    {
    }

    /// <summary>
    /// Runs at the bottom of every damage intake (after death resolution).
    /// Subclasses use it for post-hit thresholds such as low-health splits.
    /// </summary>
    protected virtual void OnPostDamage(float damage, Node2D? source, bool isCrit)
    {
    }

    public virtual void TakeDoTDamage(float dotDamage)
    {
        if (IsBeingEaten || CurrentHealth <= 0.0f)
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

    public virtual void BeEngulfed(Node2D? predator)
    {
        if (IsBeingEaten)
            return;

        if (!CanBeEngulfed)
        {
            OnEngulfAttemptFailed(predator);
            return;
        }

        IsBeingEaten = true;
        OnEngulfedBy(predator);

        // Centralized engulf bookkeeping: every successful engulf counts
        // here, exactly once (IsBeingEaten guards re-entry).
        if (predator is BaseCell cell && GodotObject.IsInstanceValid(cell))
        {
            cell.DigestedCount += 1;
            cell.OnPathogenConsumed(this, GetAtpValue());
            cell.EmitSignal(BaseCell.SignalName.PathogenDigested, this, GetAtpValue());
        }

        if (HitArea != null)
        {
            HitArea.SetDeferred(Area2D.PropertyName.Monitoring, false);
            HitArea.SetDeferred(Area2D.PropertyName.Monitorable, false);
        }
        if (EnemyCollisionShape != null)
        {
            EnemyCollisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        }

        Vector2 predPos = predator != null && GodotObject.IsInstanceValid(predator) ? predator.GlobalPosition : GlobalPosition;
        float digestDuration = EngulfDigestDuration;

        var tween = CreateTween().SetParallel(true);
        tween.TweenProperty(this, "global_position", predPos, digestDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "scale", Vector2.Zero, digestDuration)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "modulate:a", 0.0f, digestDuration);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            PlayDigestionVfx(predPos);
            EmitSignal(SignalName.Digested, this);
            // Route through death: kill credit + EXP, same as any other kill.
            Die(predator);
        }));
    }

    /// <summary>Seconds the engulf-shrink tween takes before the pathogen is digested.</summary>
    protected virtual float EngulfDigestDuration => 0.25f;

    /// <summary>Runs once the cell has committed to digesting this pathogen.</summary>
    protected virtual void OnEngulfedBy(Node2D? predator)
    {
    }

    /// <summary>VFX played at the digestion point when the tween completes.</summary>
    protected virtual void PlayDigestionVfx(Vector2 pos)
    {
        VfxManager.Instance?.Play(VfxType.LysisBurst, pos);
    }

    /// <summary>Contact damage dealt to a cell whose engulf attempt bounces off.</summary>
    protected virtual float EngulfContactDamage => 0.0f;

    /// <summary>Per-enemy contact damage table (survivor-like touch damage).</summary>
    private static readonly Dictionary<string, float> ContactDamageByEnemyId = new()
    {
        ["staph"] = 3.0f,
        ["tb"] = 5.0f,
        ["e_coli"] = 4.0f,
        ["candida"] = 5.0f,
        ["pseudomonas"] = 4.0f,
        ["ebola"] = 6.0f,
        ["h_pylori"] = 4.0f,
        ["flu_drift"] = 5.0f,
        ["tetanus"] = 4.0f,
        ["plasmodium"] = 5.0f,
        ["plasmodium_merozoite"] = 3.0f,
        ["malignant_cell"] = 6.0f,
        ["toxoplasma"] = 4.0f,
        ["hiv"] = 4.0f,
        ["s_virus"] = 4.0f,
        ["norovirus"] = 3.0f,
        ["varicella_zoster"] = 4.0f,
        ["rabies"] = 5.0f,
        ["anthrax_spore"] = 4.0f,
        ["anthrax_bacillus"] = 5.0f,
        ["aspergillus"] = 5.0f,
        ["prion"] = 12.0f,
        ["prion_fragment"] = 8.0f,
    };

    /// <summary>
    /// Contact damage dealt to a cell on touch. One-directional by design:
    /// monsters hurt the cell; the cell never hurts monsters by touching.
    /// Boss families override with their own values.
    /// </summary>
    protected virtual float ContactDamage => ContactDamageByEnemyId.TryGetValue(EnemyId, out float dmg) ? dmg : 3.0f;

    /// <summary>Seconds between contact ticks against the same cell.</summary>
    protected virtual float ContactTickInterval => 1.0f;

    /// <summary>Next allowed contact-tick timestamp (msec).</summary>
    public double NextContactTickMsec { get; set; }

    /// <summary>
    /// Survivor-like contact strike: at most one hit per
    /// <see cref="ContactTickInterval"/>. Routes through the cell's
    /// evasion/block/armor pipeline.
    /// </summary>
    public bool TryContactStrike(BaseCell cell)
    {
        if (cell == null || !GodotObject.IsInstanceValid(cell) || cell.IsDead)
            return false;
        if (IsBeingEaten || CurrentHealth <= 0.0f)
            return false;
        double now = Time.GetTicksMsec();
        if (now < NextContactTickMsec)
            return false;
        NextContactTickMsec = now + ContactTickInterval * 1000.0;
        cell.TakeDamage(ContactDamage);
        return true;
    }

    /// <summary>Knockback impulse applied to a cell whose engulf attempt bounces off.</summary>
    protected virtual float EngulfRepelForce => 0.0f;

    private bool _splitBurstConsumed;

    /// <summary>True once the one-shot split/burst effect has fired.</summary>
    protected bool SplitBurstConsumed => _splitBurstConsumed;

    /// <summary>
    /// One-shot guard shared by boss split/burst effects (death splits and
    /// low-health bursts). Returns true the first call only.
    /// </summary>
    protected bool TryConsumeSplitBurst()
    {
        if (_splitBurstConsumed)
            return false;

        _splitBurstConsumed = true;
        return true;
    }

    public virtual void OnEngulfAttemptFailed(Node2D? predator)
    {
        if (EngulfContactDamage > 0.0f || EngulfRepelForce > 0.0f)
        {
            if (predator is BaseCell cell)
            {
                if (EngulfContactDamage > 0.0f)
                    cell.TakeDamage(EngulfContactDamage);

                Vector2 repel = cell.GlobalPosition - GlobalPosition;
                if (EngulfRepelForce > 0.0f && repel.LengthSquared() > 0.001f)
                    cell.Velocity += repel.Normalized() * EngulfRepelForce;
            }
            return;
        }

        if (FibrinShield > 0)
        {
            FibrinShield--;
            QueueRedraw();
        }
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

    public virtual void Die(Node2D? killer)
    {
        AudioManager.Instance?.PlayEnemyDeath();
        // All progression EXP flows through death, regardless of what
        // killed it (skills, DoT, hazards). Engulfed prey routes here too
        // at digestion end — never grant EXP anywhere else for enemies.
        var player = PlayerRef;
        if (player != null && GodotObject.IsInstanceValid(player))
            player.AddExp(AtpValue);
        AchievementManager.RecordEvent("pathogen_killed", EnemyId);
        RunTelemetryManager.Instance?.RecordKill(BaseScore);
        EmitSignal(SignalName.EnemyDied, this);
        QueueFree();
    }

    public void ApplySlow(float duration, float factor)
    {
        SlowTimer = duration;
        SlowFactor = Mathf.Min(SlowFactor, factor);
    }

    public void ApplyStun(float duration)
    {
        StunTimer = duration;
    }

    // Compatibility methods for duck-typing
    public float GetAtpValue() => AtpValue;
    public float get_atp_value() => AtpValue;
    public int GetBaseScore() => BaseScore;
    public int get_base_score() => BaseScore;
    public void be_engulfed(Node2D? predator) => BeEngulfed(predator);
    public void take_damage(float damage, Node2D? source = null) => TakeDamage(damage, source, false);
    public void take_damage(float damage, Node2D? source, bool isCrit) => TakeDamage(damage, source, isCrit);
}
