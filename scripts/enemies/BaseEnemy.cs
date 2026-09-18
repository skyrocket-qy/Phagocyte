using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.UI;

namespace Phagocyte.Enemies;

public abstract partial class BaseEnemy : Node2D
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

    public static readonly List<BaseEnemy> ActiveEnemies = new();

    public override void _EnterTree()
    {
        base._EnterTree();
        if (!ActiveEnemies.Contains(this))
            ActiveEnemies.Add(this);
    }

    public override void _Ready()
    {
        if (!ActiveEnemies.Contains(this))
            ActiveEnemies.Add(this);
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

    protected virtual float GetCollisionRadius()
    {
        return 16.0f;
    }

    private void EnsureCollisionNodes()
    {
        HitArea = GetNodeOrNull<Area2D>("HitArea");
        if (HitArea == null)
        {
            HitArea = new Area2D
            {
                Name = "HitArea",
                CollisionLayer = 2, // Layer 2: Enemies
                CollisionMask = 1   // Mask 1: Player
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
        Modulate = new Color(1.8f, 0.4f, 0.4f, 1.0f);
        var tw = CreateTween();
        tw.TweenProperty(this, "modulate", Colors.White, 0.15);

        if (CurrentHealth <= 0.0f)
        {
            Die(source);
        }
        else
        {
            QueueRedraw();
        }
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

        var tween = CreateTween().SetParallel(true);
        tween.TweenProperty(this, "global_position", predPos, 0.25)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.25)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.25);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            VfxManager.Instance?.Play(VfxType.LysisBurst, predPos);
            EmitSignal(SignalName.Digested, this);
            QueueFree();
        }));
    }

    public virtual void OnEngulfAttemptFailed(Node2D? predator)
    {
        if (FibrinShield > 0)
        {
            FibrinShield--;
            QueueRedraw();
        }
    }

    public override void _ExitTree()
    {
        ActiveEnemies.Remove(this);
        base._ExitTree();
    }

    public virtual void Die(Node2D? killer)
    {
        AudioManager.Instance?.PlayEnemyDeath();
        VfxManager.Instance?.Play(VfxType.CytoplasmSplatter, GlobalPosition);
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
