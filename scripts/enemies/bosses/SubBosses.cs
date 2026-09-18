using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Combat;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Shared base for the five 09:00 map sub-bosses.
/// Sub-bosses cannot be digested: engulf attempts bounce off and damage the cell.
/// </summary>
public abstract partial class SubBossEnemy : BaseEnemy
{
    protected SubBossEnemy()
    {
        BaseScore = 600;
    }

    public override bool CanBeEngulfed => false;

    protected virtual float ContactDamage => 16.0f;

    public override void OnEngulfAttemptFailed(Node2D? predator)
    {
        if (predator is BaseCell cell)
        {
            cell.TakeDamage(ContactDamage);
            Vector2 repel = cell.GlobalPosition - GlobalPosition;
            if (repel.LengthSquared() > 0.001f)
                cell.Velocity += repel.Normalized() * 240.0f;
        }
    }
}

/// <summary>
/// Map 1 Sub-Boss: Streptococcus Chain-Lord (化膿性鏈球菌巨噬長鏈).
/// Super-long serpentine swimmer with multi-segment collision beads.
/// </summary>
public partial class StreptococcusChainLord : SubBossEnemy
{
    public const int SegmentCount = 10;
    public const float SegmentSpacing = 26.0f;
    private const float SegmentRadius = 18.0f;
    private const float SegmentDamage = 14.0f;
    private const float SegmentHitInterval = 0.45f;

    private readonly Vector2[] _segments = new Vector2[SegmentCount];
    private float _swayPhase;
    private float _hitCooldown;

    public StreptococcusChainLord()
    {
        EnemyId = "streptococcus_chain_lord";
        DisplayNameKey = "PATHOGEN_CHAINLORD_NAME";
        MaxHealth = 420.0f;
        CurrentHealth = 420.0f;
        AtpValue = 120.0f;
        FloatSpeed = 54.0f;
        Armor = 3.0f;
        IsElite = true;
        IsBoss = true;
        ThreatMode = EnemyThreatMode.Interceptor;
    }

    protected override float GetCollisionRadius() => 26.0f;

    protected override float ContactDamage => 20.0f;

    protected override void SetupEnemy()
    {
        for (int i = 0; i < SegmentCount; i++)
        {
            _segments[i] = GlobalPosition - Vector2.Right * (SegmentSpacing * i);
        }
    }

    protected override void HandleBrownianDrift(float dt)
    {
        _swayPhase += dt * 3.2f;

        var player = (BaseCell?)GetTree().GetFirstNodeInGroup("player");
        Vector2 heading = WanderDir;
        if (player != null && GodotObject.IsInstanceValid(player) && !player.IsDead)
        {
            // Flanking interception: cut off the player's travel vector instead of tailing.
            Vector2 intercept = EnemySteering.AimAtIntercept(this, player.GlobalPosition, player.Velocity);
            if (intercept.LengthSquared() > 0.001f)
                heading = intercept;
        }

        float speed = FloatSpeed * SlowFactor;
        if (Ailments != null && Ailments.IsAgglutinated)
            speed *= Ailments.SpeedMultiplier;
        if (BossPhase != null)
            speed *= BossPhase.CurrentSpeedMult;

        Vector2 perpendicular = heading.Orthogonal();
        Vector2 desired = (heading + perpendicular * Mathf.Sin(_swayPhase) * 0.85f).Normalized();
        Velocity = Velocity.Lerp(desired * speed, 2.6f * dt);
        Position += Velocity * dt;
        Rotation = Velocity.Angle();

        UpdateSegments();
        CheckSegmentCollisions(dt);

        BreatheTimer += dt;
        QueueRedraw();
    }

    private void UpdateSegments()
    {
        _segments[0] = GlobalPosition;
        for (int i = 1; i < SegmentCount; i++)
        {
            Vector2 leader = _segments[i - 1];
            Vector2 dir = _segments[i] - leader;
            float dist = dir.Length();
            dir = dist > 0.001f ? dir / dist : Vector2.Right;
            _segments[i] = leader + dir * SegmentSpacing;
        }
    }

    private void CheckSegmentCollisions(float dt)
    {
        if (_hitCooldown > 0.0f)
        {
            _hitCooldown -= dt;
            return;
        }

        var player = (BaseCell?)GetTree().GetFirstNodeInGroup("player");
        if (player == null || !GodotObject.IsInstanceValid(player) || player.IsDead)
            return;

        float hitRange = 24.0f + player.CurrentRadius * 0.35f;
        for (int i = 1; i < SegmentCount; i++)
        {
            if (_segments[i].DistanceTo(player.GlobalPosition) <= hitRange)
            {
                _hitCooldown = SegmentHitInterval;
                player.TakeDamage(SegmentDamage * (1.0f + 0.05f * i));
                Vector2 knock = player.GlobalPosition - _segments[i];
                if (knock.LengthSquared() > 0.001f)
                    player.Velocity += knock.Normalized() * 180.0f;
                break;
            }
        }
    }

    public override void _Draw()
    {
        Color beadCore = new Color(0.95f, 0.85f, 0.35f, 0.95f);
        Color beadRim = new Color(0.75f, 0.45f, 0.10f, 0.95f);
        Color membrane = new Color(1.0f, 0.95f, 0.65f, 0.55f);

        for (int i = SegmentCount - 1; i >= 0; i--)
        {
            Vector2 p = ToLocal(_segments[i]);
            float radius = 22.0f - i * 1.1f;
            if (radius < 8.0f)
                radius = 8.0f;

            if (i > 0)
            {
                Vector2 prev = ToLocal(_segments[i - 1]);
                DrawLine(prev, p, membrane, radius * 1.35f);
            }

            DrawCircle(p, radius, beadRim);
            DrawCircle(p, radius * 0.72f, beadCore);
            DrawCircle(p - new Vector2(radius * 0.25f, radius * 0.25f), radius * 0.22f, Colors.White);
        }

        // Leading chain head with a polarized spear cap
        Vector2 head = ToLocal(_segments[0]);
        Vector2 forward = Vector2.FromAngle(Rotation);
        DrawCircle(head, 26.0f, new Color(0.85f, 0.55f, 0.15f, 0.95f));
        DrawCircle(head, 18.0f, new Color(1.0f, 0.9f, 0.45f, 0.95f));
        DrawLine(head, head + forward * 40.0f, new Color(1.0f, 0.8f, 0.3f, 0.85f), 5.0f);
    }
}

/// <summary>
/// Map 2 Sub-Boss: Flu-Drift Cyclone (甲型變異流感暴風核心).
/// Every 30s triggers a full-screen antigenic drift that clears targeted crit marks.
/// </summary>
public partial class FluDriftCyclone : SubBossEnemy
{
    public const float DriftCooldown = 30.0f;

    private float _driftTimer;
    private float _swirlPhase;
    private Color _hue = new(0.35f, 0.75f, 0.95f, 0.95f);

    public FluDriftCyclone()
    {
        EnemyId = "flu_drift_cyclone";
        DisplayNameKey = "PATHOGEN_FLUDUST_CYCLONE_NAME";
        MaxHealth = 380.0f;
        CurrentHealth = 380.0f;
        AtpValue = 130.0f;
        FloatSpeed = 46.0f;
        Armor = 2.0f;
        IsElite = true;
        IsBoss = true;
    }

    protected override float GetCollisionRadius() => 30.0f;

    protected override float ContactDamage => 18.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _driftTimer += dt;
        _swirlPhase += dt * 5.2f;
        if (_driftTimer >= DriftCooldown)
        {
            _driftTimer = 0.0f;
            TriggerAntigenicDrift();
        }
        QueueRedraw();
    }

    /// <summary>
    /// Full-screen antigenic drift: wipes MHC targeting marks and bursts the player back.
    /// </summary>
    public void TriggerAntigenicDrift()
    {
        _hue = new Color(GD.Randf(), GD.Randf(), GD.Randf(), 0.95f).Lerp(Colors.Cyan, 0.35f);

        foreach (var node in GetTree().GetNodesInGroup("pathogens"))
        {
            if (node is Node2D pathogen && GodotObject.IsInstanceValid(pathogen))
            {
                if (pathogen.HasMeta("mhc_marked"))
                    pathogen.RemoveMeta("mhc_marked");
            }
        }

        var player = (BaseCell?)GetTree().GetFirstNodeInGroup("player");
        if (player != null && GodotObject.IsInstanceValid(player))
        {
            Vector2 away = player.GlobalPosition - GlobalPosition;
            if (away.LengthSquared() > 0.001f)
                player.Velocity += away.Normalized() * 220.0f;
        }

        if (GetParent() is Node parent)
        {
            var wave = new DriftWave
            {
                GlobalPosition = GlobalPosition,
                MaxRadius = 900.0f
            };
            parent.AddChild(wave);
        }

        Modulate = Colors.White * 1.8f;
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", Colors.White, 0.35);
        QueueRedraw();
    }

    public override void _Draw()
    {
        Color aura = new Color(_hue.R, _hue.G, _hue.B, 0.22f);
        DrawCircle(Vector2.Zero, 38.0f, aura);

        // Spiral cyclone arms
        for (int arm = 0; arm < 3; arm++)
        {
            float baseAngle = _swirlPhase + arm * (Mathf.Tau / 3.0f);
            Vector2 prev = Vector2.Zero;
            for (int i = 1; i <= 10; i++)
            {
                float t = (float)i / 10.0f;
                float radius = 6.0f + t * 30.0f;
                float angle = baseAngle + t * 2.6f;
                Vector2 point = Vector2.FromAngle(angle) * radius;
                DrawLine(prev, point, new Color(_hue.R, _hue.G, _hue.B, 0.85f), 4.0f);
                prev = point;
            }
        }

        DrawCircle(Vector2.Zero, 12.0f, _hue.Lightened(0.25f));
        DrawCircle(Vector2.Zero, 6.5f, Colors.White);
    }
}

/// <summary>
/// Expanding antigenic drift shockwave visual.
/// </summary>
public partial class DriftWave : Node2D
{
    public float MaxRadius { get; set; } = 900.0f;

    private const float Lifetime = 1.1f;
    private float _age;

    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age >= Lifetime)
        {
            QueueFree();
            return;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        float t = Mathf.Clamp(_age / Lifetime, 0.0f, 1.0f);
        float radius = MaxRadius * t;
        float alpha = 1.0f - t;

        DrawCircle(Vector2.Zero, radius, new Color(0.45f, 0.85f, 1.0f, alpha * 0.10f));
        DrawArc(Vector2.Zero, radius, 0.0f, Mathf.Tau, 64, new Color(0.65f, 0.95f, 1.0f, alpha), 4.0f);
    }
}

/// <summary>
/// Map 3 Sub-Boss: TB Granuloma Behemoth (結核肉芽腫巨核).
/// Dense waxy wall with heavy damage reduction; leaves caseous debris on death.
/// </summary>
public partial class TbGranulomaBehemoth : SubBossEnemy
{
    private float _waxPhase;

    public TbGranulomaBehemoth()
    {
        EnemyId = "tb_granuloma_behemoth";
        DisplayNameKey = "PATHOGEN_TB_BEHEMOTH_NAME";
        MaxHealth = 650.0f;
        CurrentHealth = 650.0f;
        AtpValue = 150.0f;
        FloatSpeed = 24.0f;
        Armor = 14.0f;
        IsElite = true;
        IsBoss = true;
    }

    protected override float GetCollisionRadius() => 34.0f;

    protected override float ContactDamage => 24.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _waxPhase += dt * 2.2f;
        QueueRedraw();
    }

    public override void Die(Node2D? killer)
    {
        var parent = GetParent();
        if (parent != null)
        {
            var debris = new CaseousNecrosis
            {
                GlobalPosition = GlobalPosition,
                Radius = 48.0f
            };
            parent.AddChild(debris);
        }
        base.Die(killer);
    }

    public override void _Draw()
    {
        Color waxRim = new Color(0.92f, 0.72f, 0.25f, 0.95f);
        Color waxBody = new Color(0.72f, 0.48f, 0.16f, 0.95f);
        Color caseousCore = new Color(0.85f, 0.82f, 0.62f, 0.95f);

        DrawCircle(Vector2.Zero, 38.0f, waxRim);
        DrawCircle(Vector2.Zero, 32.0f, waxBody);

        // Waxy mycolic wall texture
        for (int i = 0; i < 12; i++)
        {
            float angle = i * (Mathf.Tau / 12.0f) + Mathf.Sin(_waxPhase + i) * 0.08f;
            Vector2 inner = Vector2.FromAngle(angle) * 30.0f;
            Vector2 outer = Vector2.FromAngle(angle) * 38.0f;
            DrawLine(inner, outer, new Color(1.0f, 0.85f, 0.4f, 0.8f), 2.4f);
        }

        // Central caseous necrosis core with irregular boundary
        int corePoints = 14;
        Vector2[] core = new Vector2[corePoints];
        for (int i = 0; i < corePoints; i++)
        {
            float angle = i * (Mathf.Tau / corePoints);
            float radius = 20.0f + Mathf.Sin(_waxPhase * 0.8f + i * 1.7f) * 4.0f;
            core[i] = Vector2.FromAngle(angle) * radius;
        }
        DrawColoredPolygon(core, caseousCore);
        DrawCircle(Vector2.Zero, 8.0f, new Color(0.45f, 0.32f, 0.12f, 0.9f));
    }
}

/// <summary>
/// Caseous necrosis debris: a permanent obstructive obstacle left by the TB behemoth.
/// </summary>
public partial class CaseousNecrosis : StaticBody2D
{
    public float Radius { get; set; } = 44.0f;

    public override void _Ready()
    {
        CollisionLayer = 4; // Arena obstacle layer (player mask)
        CollisionMask = 0;
        ZIndex = 1;
        AddToGroup("hazards");

        AddChild(new CollisionShape2D
        {
            Name = "CollisionShape2D",
            Shape = new CircleShape2D { Radius = Radius }
        });

        QueueRedraw();
    }

    public override void _Draw()
    {
        Color rim = new Color(0.78f, 0.72f, 0.45f, 0.85f);
        Color core = new Color(0.55f, 0.50f, 0.30f, 0.55f);

        DrawCircle(Vector2.Zero, Radius, core);
        DrawArc(Vector2.Zero, Radius, 0.0f, Mathf.Tau, 40, rim, 3.0f);

        for (int i = 0; i < 7; i++)
        {
            float angle = i * (Mathf.Tau / 7.0f);
            Vector2 center = Vector2.FromAngle(angle) * (Radius * 0.5f);
            DrawCircle(center, Radius * 0.18f, new Color(0.65f, 0.60f, 0.38f, 0.7f));
        }
    }
}

/// <summary>
/// Map 4 Sub-Boss: VacA Secretor (空泡毒素 VacA 分泌原體).
/// Trails large spreading acid pools across the gastric lumen.
/// </summary>
public partial class VacASecretor : SubBossEnemy
{
    public const float PoolInterval = 2.8f;
    public const int MaxPools = 6;

    private readonly List<BioHazardArea> _pools = new();
    private float _spawnTimer = 1.2f;
    private float _bubblePhase;

    public VacASecretor()
    {
        EnemyId = "vaca_secretor";
        DisplayNameKey = "PATHOGEN_VACA_NAME";
        MaxHealth = 400.0f;
        CurrentHealth = 400.0f;
        AtpValue = 120.0f;
        FloatSpeed = 34.0f;
        Armor = 4.0f;
        IsElite = true;
        IsBoss = true;
    }

    protected override float GetCollisionRadius() => 28.0f;

    protected override float ContactDamage => 18.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _bubblePhase += dt * 3.4f;
        _spawnTimer += dt;
        if (_spawnTimer >= PoolInterval)
        {
            _spawnTimer = 0.0f;
            SpawnAcidPool();
        }
        QueueRedraw();
    }

    private void SpawnAcidPool()
    {
        _pools.RemoveAll(pool => pool == null || !GodotObject.IsInstanceValid(pool));
        if (_pools.Count >= MaxPools)
            return;

        var parent = GetParent();
        if (parent == null)
            return;

        var pool = new VacAAcidPool
        {
            GlobalPosition = GlobalPosition,
            Radius = 40.0f
        };
        parent.AddChild(pool);
        _pools.Add(pool);
    }

    public override void _Draw()
    {
        Color rim = new Color(0.78f, 0.85f, 0.25f, 0.95f);
        Color body = new Color(0.45f, 0.58f, 0.12f, 0.95f);

        DrawCircle(Vector2.Zero, 30.0f, new Color(0.7f, 0.9f, 0.2f, 0.22f));
        DrawCircle(Vector2.Zero, 24.0f, rim);
        DrawCircle(Vector2.Zero, 18.0f, body);

        // Intracellular vacuoles bubbling with VacA toxin
        for (int i = 0; i < 6; i++)
        {
            float angle = i * (Mathf.Tau / 6.0f) + _bubblePhase * 0.3f;
            float dist = 8.0f + Mathf.Sin(_bubblePhase + i * 1.3f) * 5.0f;
            Vector2 pos = Vector2.FromAngle(angle) * dist;
            float radius = 3.5f + Mathf.Sin(_bubblePhase * 1.7f + i) * 1.5f;
            DrawCircle(pos, radius, new Color(0.85f, 1.0f, 0.4f, 0.85f));
        }
    }
}

/// <summary>
/// Spreading VacA acid mucus pool.
/// </summary>
public partial class VacAAcidPool : BioHazardArea
{
    public const float MaxRadius = 125.0f;
    public const float GrowthPerSecond = 17.0f;

    public VacAAcidPool()
    {
        Duration = 9.0f;
        Radius = 40.0f;
        Damage = 6.0f;
        TickInterval = 0.5f;
        SlowFactor = 0.55f;
        SlowsTarget = true;
        DealsDamage = true;
        CoreColor = new Color(0.35f, 0.45f, 0.08f, 0.35f);
        RimColor = new Color(0.75f, 0.95f, 0.25f, 0.65f);
    }

    public override void _PhysicsProcess(double delta)
    {
        Radius = Mathf.Min(MaxRadius, Radius + GrowthPerSecond * (float)delta);
        base._PhysicsProcess(delta);
    }
}

/// <summary>
/// Map 5 Sub-Boss: Toxoplasma Mega-Cyst (剛地弓形蟲巨型假包囊).
/// Upon reaching low HP, fires tachyzoites in four orthogonal directions.
/// </summary>
public partial class ToxoplasmaMegaCyst : SubBossEnemy
{
    public const float BurstHealthRatio = 0.30f;

    private bool _burstTriggered;
    private float _pulsePhase;

    public ToxoplasmaMegaCyst()
    {
        EnemyId = "toxoplasma_mega_cyst";
        DisplayNameKey = "PATHOGEN_MEGACYST_NAME";
        MaxHealth = 520.0f;
        CurrentHealth = 520.0f;
        AtpValue = 140.0f;
        FloatSpeed = 20.0f;
        Armor = 5.0f;
        IsElite = true;
        IsBoss = true;
    }

    protected override float GetCollisionRadius() => 36.0f;

    protected override float ContactDamage => 22.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _pulsePhase += dt * 2.4f;

        if (!_burstTriggered && CurrentHealth <= MaxHealth * BurstHealthRatio)
        {
            _burstTriggered = true;
            BurstTachyzoites();
        }

        QueueRedraw();
    }

    /// <summary>
    /// Fires high-velocity tachyzoites along the four orthogonal axes.
    /// </summary>
    public void BurstTachyzoites()
    {
        var parent = GetParent();
        if (parent == null)
            return;

        Vector2[] directions = { Vector2.Up, Vector2.Down, Vector2.Left, Vector2.Right };
        foreach (Vector2 direction in directions)
        {
            var tachyzoite = new Tachyzoite
            {
                GlobalPosition = GlobalPosition + direction * 34.0f
            };
            parent.AddChild(tachyzoite);
            tachyzoite.Launch(direction);
        }

        var wave = new DriftWave
        {
            GlobalPosition = GlobalPosition,
            MaxRadius = 300.0f
        };
        parent.AddChild(wave);
    }

    public override void _Draw()
    {
        Color cystRim = new Color(0.55f, 0.30f, 0.70f, 0.95f);
        Color cystBody = new Color(0.30f, 0.15f, 0.42f, 0.95f);
        float pulse = 1.0f + 0.03f * Mathf.Sin(_pulsePhase);

        DrawCircle(Vector2.Zero, 44.0f * pulse, new Color(0.6f, 0.3f, 0.8f, 0.18f));
        DrawCircle(Vector2.Zero, 36.0f * pulse, cystRim);
        DrawCircle(Vector2.Zero, 30.0f * pulse, cystBody);

        // Dormant crescent tachyzoites packed inside the pseudocyst
        for (int i = 0; i < 5; i++)
        {
            float angle = i * (Mathf.Tau / 5.0f) + _pulsePhase * 0.2f;
            Vector2 center = Vector2.FromAngle(angle) * 15.0f;
            Vector2 dir = Vector2.FromAngle(angle + 1.1f);
            DrawLine(center - dir * 7.0f, center + dir * 7.0f, new Color(0.85f, 0.55f, 1.0f, 0.9f), 3.0f);
        }
    }
}

/// <summary>
/// High-velocity tachyzoite launched by the Mega-Cyst. Dashes straight, then bursts on contact.
/// </summary>
public partial class Tachyzoite : BaseEnemy
{
    private const float DashDuration = 2.6f;
    private const float ContactDamage = 9.0f;

    private Vector2 _dashDirection = Vector2.Right;
    private float _dashTimer;
    private bool _hasHit;

    public Tachyzoite()
    {
        EnemyId = "tachyzoite";
        DisplayNameKey = "PATHOGEN_TACHYZOITE_NAME";
        MaxHealth = 8.0f;
        CurrentHealth = 8.0f;
        AtpValue = 3.0f;
        BaseScore = 5;
        FloatSpeed = 320.0f;
    }

    protected override float GetCollisionRadius() => 6.0f;

    public void Launch(Vector2 direction)
    {
        _dashDirection = direction.LengthSquared() > 0.001f ? direction.Normalized() : Vector2.Right;
        _dashTimer = DashDuration;
        Velocity = _dashDirection * FloatSpeed;
    }

    protected override void HandleBrownianDrift(float dt)
    {
        _dashTimer = Mathf.Max(0.0f, _dashTimer - dt);
        float speed = _dashTimer > 0.0f ? FloatSpeed : FloatSpeed * 0.25f;
        Velocity = Velocity.Lerp(_dashDirection * speed, 3.0f * dt);
        Position += Velocity * dt;

        if (_hasHit)
            return;

        var player = (BaseCell?)GetTree().GetFirstNodeInGroup("player");
        if (player != null && GodotObject.IsInstanceValid(player) && !player.IsDead)
        {
            if (GlobalPosition.DistanceTo(player.GlobalPosition) <= 20.0f + player.CurrentRadius * 0.4f)
            {
                _hasHit = true;
                player.TakeDamage(ContactDamage);
                Die(null);
            }
        }
    }

    public override void _Draw()
    {
        Vector2 dir = Velocity.LengthSquared() > 0.001f ? Velocity.Normalized() : _dashDirection;
        Vector2 perp = dir.Orthogonal();

        DrawLine(-dir * 9.0f, dir * 9.0f, new Color(0.9f, 0.6f, 1.0f, 0.95f), 3.5f);
        DrawCircle(Vector2.Zero, 5.0f, new Color(0.65f, 0.35f, 0.85f, 0.95f));
        DrawCircle(-dir * 3.0f + perp * 2.0f, 1.6f, new Color(1.0f, 0.75f, 0.35f, 1.0f));
    }
}
