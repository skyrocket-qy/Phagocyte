using Godot;
using System;

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
    [Export] public float FloatSpeed { get; set; } = 35.0f;
    [Export] public float DriftFrequency { get; set; } = 1.2f;
    [Export] public bool IsElite { get; set; } = false;
    [Export] public bool IsBoss { get; set; } = false;

    public bool IsBeingEaten { get; set; } = false;
    public virtual bool CanBeEngulfed => FibrinShield <= 0;

    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public float DriftTimer { get; set; } = 0.0f;
    public Vector2 WanderDir { get; set; } = Vector2.Zero;
    public float BreatheTimer { get; set; } = 0.0f;

    // Status debuffs
    public float SlowTimer { get; set; } = 0.0f;
    public float SlowFactor { get; set; } = 1.0f;
    public float StunTimer { get; set; } = 0.0f;

    public Area2D? HitArea { get; set; }
    public CollisionShape2D? EnemyCollisionShape { get; set; }

    public override void _Ready()
    {
        AddToGroup("pathogens");
        CurrentHealth = MaxHealth;
        DriftTimer = GD.Randf() * 5.0f;
        BreatheTimer = GD.Randf() * 10.0f;
        WanderDir = Vector2.FromAngle(GD.Randf() * Mathf.Tau);

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
        if (DriftTimer > 2.5f)
        {
            DriftTimer = 0.0f;
            WanderDir = (WanderDir + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 0.7f).Normalized();
        }

        float currentSpeed = FloatSpeed * SlowFactor;
        Velocity = Velocity.Lerp(WanderDir * currentSpeed, 2.0f * dt);
        Position += Velocity * dt;
    }

    protected virtual void CustomPhysicsProcess(float dt)
    {
    }

    public virtual void TakeDamage(float damage, Node2D? source = null)
    {
        if (IsBeingEaten)
            return;

        if (FibrinShield > 0)
        {
            FibrinShield--;
            QueueRedraw();
            return;
        }

        float effectiveDmg = Mathf.Max(1.0f, damage - Armor);
        CurrentHealth -= effectiveDmg;

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

    public virtual void Die(Node2D? killer)
    {
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
    public void be_engulfed(Node2D? predator) => BeEngulfed(predator);
}
