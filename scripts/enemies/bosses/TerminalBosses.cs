using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Combat;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Shared base for the five 15:00 map terminal bosses.
/// They cannot be digested and all lean on BossPhaseComponent for multi-phase HP
/// transitions, the hard-enrage countdown and ground danger telegraphs.
/// </summary>
public abstract partial class TerminalBossEnemy : BaseEnemy
{
    protected TerminalBossEnemy()
    {
        BaseScore = 3000;
    }

    public override bool CanBeEngulfed => false;

    protected virtual float ContactDamage => 26.0f;
    protected virtual float TelegraphScale => 1.35f;
    protected virtual float TelegraphDamage => 24.0f;

    // Engulf attempts bounce off and damage the cell (repel handled by BaseEnemy).
    protected override float EngulfContactDamage => ContactDamage;
    protected override float EngulfRepelForce => 320.0f;

    protected override void SetupEnemy()
    {
        if (BossPhase != null)
        {
            BossPhase.TelegraphScale = TelegraphScale;
            BossPhase.TelegraphDamage = TelegraphDamage;
        }
    }
}

/// <summary>
/// Map 1 Terminal Boss: MRSA Super-Colony (è€ç”²æ°§è¥¿æž—é‡‘è‘¡èŒæ¯é«”).
/// Massive drug-resistant capsule that splits into four enraged elites when its membrane breaks.
/// </summary>
public partial class MrsASuperColony : TerminalBossEnemy
{
    public const int SplitCount = 4;

    public bool HasSplit => SplitBurstConsumed;

    private float _capsulePhase;

    public MrsASuperColony()
    {
        EnemyId = "mrsa_super_colony";
        DisplayNameKey = "PATHOGEN_MRSA_NAME";
        MaxHealth = 1600.0f;
        AtpValue = 400.0f;
        FloatSpeed = 20.0f;
        Armor = 12.0f;
        IsElite = true;
        IsBoss = true;
    }

    protected override float GetCollisionRadius() => 46.0f;
    protected override float ContactDamage => 30.0f;
    protected override float TelegraphScale => 1.45f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _capsulePhase += dt * 1.8f;
        QueueRedraw();
    }

    public override void Die(Node2D? killer)
    {
        if (TryConsumeSplitBurst())
            SpawnEnragedElites();
        base.Die(killer);
    }

    /// <summary>
    /// Breaks the capsule membrane open and releases four enraged child elites.
    /// </summary>
    public void SpawnEnragedElites()
    {
        var parent = GetParent();
        if (parent == null)
            return;

        for (int i = 0; i < SplitCount; i++)
        {
            float angle = i * (Mathf.Tau / SplitCount) + 0.4f;
            var elite = new MrsaEnragedElite
            {
                GlobalPosition = GlobalPosition + Vector2.FromAngle(angle) * 54.0f
            };
            parent.AddChild(elite);
        }
    }

    public override void _Draw()
    {
        Color capsule = new Color(0.85f, 0.68f, 0.15f, 0.95f);
        Color core = new Color(0.98f, 0.85f, 0.35f, 0.95f);
        float pulse = 1.0f + 0.025f * Mathf.Sin(_capsulePhase);

        DrawCircle(Vector2.Zero, 52.0f * pulse, new Color(0.9f, 0.75f, 0.2f, 0.18f));
        DrawCircle(Vector2.Zero, 44.0f * pulse, capsule);

        // Methicillin-resistant capsule shield ring
        DrawArc(Vector2.Zero, 48.0f, 0.0f, Mathf.Tau, 40, new Color(0.4f, 0.85f, 1.0f, 0.75f), 3.0f);

        // Dense golden coccus cluster
        Vector2[] cocci =
        {
            new(-14, -10), new(0, -16), new(14, -8),
            new(-18, 6), new(-2, 2), new(16, 8),
            new(-8, 16), new(8, 18), new(0, 0)
        };
        foreach (Vector2 offset in cocci)
        {
            DrawCircle(offset, 9.0f, capsule);
            DrawCircle(offset, 6.0f, core);
        }
    }
}

/// <summary>
/// Enraged MRSA child elite released when the super-colony ruptures.
/// </summary>
public partial class MrsaEnragedElite : BaseEnemy
{
    private float _ragePhase;

    public MrsaEnragedElite()
    {
        EnemyId = "mrsa_enraged_elite";
        DisplayNameKey = "PATHOGEN_MRSA_ELITE_NAME";
        MaxHealth = 140.0f;
        AtpValue = 40.0f;
        BaseScore = 35;
        FloatSpeed = 72.0f;
        ThreatMode = EnemyThreatMode.ChemoChaser;
        Armor = 4.0f;
        IsElite = true;
    }

    protected override float GetCollisionRadius() => 18.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _ragePhase += dt * 6.0f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Color body = new Color(0.95f, 0.45f, 0.25f, 0.95f);
        Color core = new Color(1.0f, 0.75f, 0.4f, 0.95f);
        float tremble = Mathf.Sin(_ragePhase) * 1.6f;

        DrawCircle(new Vector2(tremble, 0), 18.0f, new Color(1.0f, 0.35f, 0.2f, 0.2f));
        DrawCircle(new Vector2(tremble, 0), 13.0f, body);
        DrawCircle(new Vector2(tremble, 0), 8.0f, core);
        DrawCircle(new Vector2(6 + tremble, -5), 5.0f, core);
        DrawCircle(new Vector2(-6 + tremble, 4), 4.5f, core);
        DrawCircle(new Vector2(3 + tremble, 7), 4.0f, core);
    }
}

/// <summary>
/// Map 2 Terminal Boss: Syncytial Mega-Capsid (èžåˆæ€§åˆèƒžé«”ç—…æ¯’è¤‡åˆé«”).
/// Fused syncytial cilia drag the cell inward while the traction field widens as the boss weakens.
/// </summary>
public partial class SyncytialMegaCapsid : TerminalBossEnemy
{
    public const float TractionInterval = 6.0f;
    public const float MinAuraRadius = 420.0f;
    public const float MaxAuraRadius = 760.0f;

    public int TractionPulses { get; private set; }

    private float _pulseTimer = TractionInterval;
    private float _ciliaPhase;

    public SyncytialMegaCapsid()
    {
        EnemyId = "syncytial_mega_capsid";
        DisplayNameKey = "PATHOGEN_SYNCYTIAL_NAME";
        MaxHealth = 1400.0f;
        AtpValue = 380.0f;
        FloatSpeed = 18.0f;
        Armor = 6.0f;
        IsElite = true;
        IsBoss = true;
    }

    protected override float GetCollisionRadius() => 40.0f;
    protected override float ContactDamage => 28.0f;

    /// <summary>
    /// Traction field radius. Widens as the boss loses HP, shrinking the player's safe space.
    /// </summary>
    public float CurrentAuraRadius
    {
        get
        {
            float hpRatio = MaxHealth > 0.0f ? Mathf.Clamp(CurrentHealth / MaxHealth, 0.0f, 1.0f) : 1.0f;
            return Mathf.Lerp(MinAuraRadius, MaxAuraRadius, 1.0f - hpRatio);
        }
    }

    protected override void CustomPhysicsProcess(float dt)
    {
        _ciliaPhase += dt * 3.0f;

        var player = PlayerRef;
        if (player != null && !player.IsDead)
        {
            if (GlobalPosition.DistanceTo(player.GlobalPosition) <= CurrentAuraRadius)
                player.ApplySlow(0.25f, 0.78f);
        }

        _pulseTimer -= dt;
        if (_pulseTimer <= 0.0f)
        {
            _pulseTimer = TractionInterval;
            TriggerTractionPulse();
        }

        QueueRedraw();
    }

    /// <summary>
    /// Alveolar traction pulse: drags the player toward the capsid and flashes the field boundary.
    /// </summary>
    public void TriggerTractionPulse()
    {
        TractionPulses++;

        var player = PlayerRef;
        if (player != null && !player.IsDead)
        {
            Vector2 pull = GlobalPosition - player.GlobalPosition;
            float distance = pull.Length();
            if (distance > 0.001f && distance <= CurrentAuraRadius + 140.0f)
                player.Velocity += pull.Normalized() * 260.0f;
        }

        if (GetParent() is Node parent)
        {
            var ring = new DriftWave
            {
                GlobalPosition = GlobalPosition,
                MaxRadius = CurrentAuraRadius + 140.0f
            };
            parent.AddChild(ring);
        }
    }

    public override void _Draw()
    {
        Color aura = new Color(0.45f, 0.8f, 0.95f, 0.10f);
        DrawCircle(Vector2.Zero, CurrentAuraRadius, aura);
        DrawArc(Vector2.Zero, CurrentAuraRadius, 0.0f, Mathf.Tau, 64, new Color(0.55f, 0.9f, 1.0f, 0.35f), 2.0f);

        Color capsid = new Color(0.30f, 0.55f, 0.75f, 0.95f);
        Color core = new Color(0.55f, 0.85f, 0.95f, 0.95f);
        DrawCircle(Vector2.Zero, 40.0f, capsid);
        DrawCircle(Vector2.Zero, 30.0f, core);
        DrawCircle(Vector2.Zero, 16.0f, new Color(0.15f, 0.35f, 0.55f, 0.95f));

        // Fused syncytial cilia fringe
        int cilia = 22;
        for (int i = 0; i < cilia; i++)
        {
            float angle = i * (Mathf.Tau / cilia) + Mathf.Sin(_ciliaPhase + i * 0.7f) * 0.06f;
            Vector2 from = Vector2.FromAngle(angle) * 38.0f;
            Vector2 to = Vector2.FromAngle(angle) * (54.0f + Mathf.Sin(_ciliaPhase * 1.4f + i) * 5.0f);
            DrawLine(from, to, new Color(0.7f, 0.95f, 1.0f, 0.7f), 2.0f);
            DrawCircle(to, 2.2f, new Color(0.85f, 0.98f, 1.0f, 0.9f));
        }
    }
}

/// <summary>
/// Map 3 Terminal Boss: Plasmodium Macro-Schizont (æƒ¡æ€§ç˜§åŽŸèŸ²è£‚æ®–è¤‡åˆé«”).
/// Feeds on ambient erythrocytes to heal, then bursts into a merozoite swarm upon rupture.
/// </summary>
public partial class PlasmodiumMacroSchizont : TerminalBossEnemy
{
    public const float FeedInterval = 5.0f;
    public const float FeedHealRatio = 0.06f;
    public const int MerozoiteBurstCount = 10;

    public int FeedsCount { get; private set; }
    public bool HasRuptured => SplitBurstConsumed;

    private float _feedTimer = FeedInterval;
    private float _pulsePhase;

    public PlasmodiumMacroSchizont()
    {
        EnemyId = "plasmodium_macro_schizont";
        DisplayNameKey = "PATHOGEN_MACROSCHIZONT_NAME";
        MaxHealth = 1500.0f;
        AtpValue = 380.0f;
        FloatSpeed = 20.0f;
        Armor = 5.0f;
        IsElite = true;
        IsBoss = true;
    }

    protected override float GetCollisionRadius() => 40.0f;
    protected override float ContactDamage => 28.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _pulsePhase += dt * 2.6f;

        _feedTimer -= dt;
        if (_feedTimer <= 0.0f)
        {
            _feedTimer = FeedInterval;
            FeedOnRbc();
        }

        QueueRedraw();
    }

    /// <summary>
    /// Devours surrounding red blood cells, restoring a chunk of health.
    /// </summary>
    public void FeedOnRbc()
    {
        FeedsCount++;

        float healed = Mathf.Min(MaxHealth - CurrentHealth, MaxHealth * FeedHealRatio);
        if (healed > 0.0f)
            CurrentHealth += healed;

        if (GetParent() is Node parent)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = GD.Randf() * Mathf.Tau;
                var mote = new RbcMote
                {
                    GlobalPosition = GlobalPosition + Vector2.FromAngle(angle) * 110.0f,
                    Target = this
                };
                parent.AddChild(mote);
            }
        }

        QueueRedraw();
    }

    public override void Die(Node2D? killer)
    {
        if (TryConsumeSplitBurst())
            BurstMerozoites();
        base.Die(killer);
    }

    /// <summary>
    /// Ruptures the schizont, releasing a radial merozoite swarm.
    /// </summary>
    public void BurstMerozoites()
    {
        var parent = GetParent();
        if (parent == null)
            return;

        for (int i = 0; i < MerozoiteBurstCount; i++)
        {
            float angle = i * (Mathf.Tau / MerozoiteBurstCount) + (float)GD.RandRange(-0.15, 0.15);
            var merozoite = new PlasmodiumMerozoite
            {
                GlobalPosition = GlobalPosition + Vector2.FromAngle(angle) * 44.0f,
                Velocity = Vector2.FromAngle(angle) * 140.0f
            };
            parent.AddChild(merozoite);
        }
    }

    public override void _Draw()
    {
        Color rbcRim = new Color(0.72f, 0.14f, 0.16f, 0.95f);
        Color rbcCore = new Color(0.45f, 0.08f, 0.10f, 0.95f);
        float pulse = 1.0f + 0.03f * Mathf.Sin(_pulsePhase);

        DrawCircle(Vector2.Zero, 44.0f * pulse, rbcRim);
        DrawCircle(Vector2.Zero, 34.0f * pulse, rbcCore);

        // Packed merozoite rosette inside the schizont
        for (int i = 0; i < 10; i++)
        {
            float angle = i * (Mathf.Tau / 10.0f) + _pulsePhase * 0.15f;
            Vector2 center = Vector2.FromAngle(angle) * 18.0f;
            DrawCircle(center, 5.0f, new Color(0.75f, 0.45f, 0.95f, 0.95f));
            DrawCircle(center, 2.4f, new Color(1.0f, 0.75f, 0.4f, 1.0f));
        }

        DrawCircle(Vector2.Zero, 8.0f, new Color(0.35f, 0.12f, 0.45f, 0.95f));
    }
}

/// <summary>
/// Ambient red blood cell mote consumed by the Macro-Schizont.
/// </summary>
public partial class RbcMote : Node2D
{
    public Node2D? Target { get; set; }

    private float _life = 0.7f;

    public override void _Process(double delta)
    {
        _life -= (float)delta;
        if (_life <= 0.0f || Target == null || !GodotObject.IsInstanceValid(Target))
        {
            QueueFree();
            return;
        }

        GlobalPosition = GlobalPosition.MoveToward(Target.GlobalPosition, 280.0f * (float)delta);
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 8.0f, new Color(0.78f, 0.18f, 0.18f, 0.9f));
        DrawCircle(Vector2.Zero, 4.5f, new Color(0.52f, 0.10f, 0.10f, 0.9f));
    }
}

/// <summary>
/// Map 4 Terminal Boss: H. pylori Biofilm Core (å¹½é–€èžºæ¡¿èŒç”Ÿç‰©è†œæ¯æ ¸).
/// Rotating toxin storm around the core and permanent strong acid muck.
/// </summary>
public partial class HpyloriBiofilmCore : TerminalBossEnemy
{
    public const float ToxinInterval = 0.9f;
    public const float ToxinRadius = 170.0f;
    public const float AcidInterval = 6.0f;
    public const int MaxScars = 8;

    public int ToxinTicks { get; private set; }

    private readonly List<HpyloriAcidScar> _scars = new();
    private float _toxinTimer = ToxinInterval;
    private float _acidTimer = AcidInterval;
    private float _spiralPhase;

    public HpyloriBiofilmCore()
    {
        EnemyId = "hpylori_biofilm_core";
        DisplayNameKey = "PATHOGEN_BIOFILM_CORE_NAME";
        MaxHealth = 1500.0f;
        AtpValue = 380.0f;
        FloatSpeed = 16.0f;
        Armor = 8.0f;
        IsElite = true;
        IsBoss = true;
    }

    protected override float GetCollisionRadius() => 42.0f;
    protected override float ContactDamage => 28.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _spiralPhase += dt * 4.0f;

        _toxinTimer -= dt;
        if (_toxinTimer <= 0.0f)
        {
            _toxinTimer = ToxinInterval;
            TriggerToxinTick();
        }

        _acidTimer -= dt;
        if (_acidTimer <= 0.0f)
        {
            _acidTimer = AcidInterval;
            SpawnAcidScar();
        }

        QueueRedraw();
    }

    /// <summary>
    /// Spiral toxin storm tick: damages and shoves nearby cells.
    /// </summary>
    public void TriggerToxinTick()
    {
        ToxinTicks++;

        var player = PlayerRef;
        if (player == null || player.IsDead)
            return;

        if (GlobalPosition.DistanceTo(player.GlobalPosition) <= ToxinRadius + player.CurrentRadius * 0.5f)
        {
            player.TakeDamage(10.0f);
            Vector2 away = player.GlobalPosition - GlobalPosition;
            if (away.LengthSquared() > 0.001f)
                player.Velocity += away.Normalized() * 180.0f;
        }
    }

    /// <summary>
    /// Drops a permanent strong acid muck pool (capped).
    /// </summary>
    public void SpawnAcidScar()
    {
        SpawnHazard(_scars, MaxScars);
    }

    public override void _Draw()
    {
        Color core = new Color(0.35f, 0.55f, 0.20f, 0.95f);
        Color rim = new Color(0.65f, 0.85f, 0.30f, 0.95f);

        DrawCircle(Vector2.Zero, ToxinRadius, new Color(0.55f, 0.8f, 0.2f, 0.08f));

        // Rotating spiral toxin storm
        for (int arm = 0; arm < 3; arm++)
        {
            float baseAngle = _spiralPhase + arm * (Mathf.Tau / 3.0f);
            Vector2 prev = Vector2.Zero;
            for (int i = 1; i <= 8; i++)
            {
                float t = (float)i / 8.0f;
                float radius = t * 90.0f;
                float angle = baseAngle + t * 2.2f;
                Vector2 point = Vector2.FromAngle(angle) * radius;
                DrawLine(prev, point, new Color(0.75f, 0.95f, 0.3f, 0.55f * (1.0f - t * 0.5f)), 3.0f);
                prev = point;
            }
        }

        DrawCircle(Vector2.Zero, 42.0f, core);
        DrawCircle(Vector2.Zero, 30.0f, rim);
        DrawCircle(Vector2.Zero, 14.0f, new Color(0.2f, 0.35f, 0.1f, 0.95f));

        // Helical flagella tufts
        for (int f = -2; f <= 2; f++)
        {
            Vector2 tip = new Vector2(52.0f, f * 6.0f + Mathf.Sin(_spiralPhase * 1.5f + f) * 6.0f);
            DrawLine(new Vector2(38.0f, f * 3.0f), tip, new Color(0.8f, 0.6f, 0.3f, 0.7f), 1.8f);
        }
    }
}

/// <summary>
/// Permanent strong acid muck left by the H. pylori Biofilm Core.
/// </summary>
public partial class HpyloriAcidScar : BioHazardArea
{
    public HpyloriAcidScar()
    {
        Duration = 9999.0f;
        Radius = 80.0f;
        Damage = 7.0f;
        TickInterval = 0.5f;
        SlowFactor = 0.4f;
        SlowsTarget = true;
        DealsDamage = true;
        CoreColor = new Color(0.38f, 0.5f, 0.05f, 0.4f);
        RimColor = new Color(0.78f, 0.95f, 0.2f, 0.7f);
    }
}

/// <summary>
/// Map 5 Terminal Boss: PrPsc Amyloid Aggregate (éŒ¯èª¤æŠ˜ç–ŠæœŠç—…æ¯’æ™¶é«”).
/// Extremely high armor while its crystalline shell is intact; the shell must be shattered first.
/// </summary>
public partial class PrpscAmyloidAggregate : TerminalBossEnemy
{
    public const float ShellDamageAbsorption = 0.85f;
    public const float ShellHealthRatio = 0.5f;
    public const float BrokenArmor = 6.0f;

    public float ShellMax { get; private set; }
    public float ShellHealth { get; private set; }
    public bool ShellIntact => ShellHealth > 0.0f;
    public bool ShellBroken { get; private set; }

    private float _crystalPhase;

    public PrpscAmyloidAggregate()
    {
        EnemyId = "prpsc_amyloid_aggregate";
        DisplayNameKey = "PATHOGEN_AMYLOID_NAME";
        MaxHealth = 1800.0f;
        AtpValue = 450.0f;
        FloatSpeed = 14.0f;
        Armor = 25.0f;
        IsElite = true;
        IsBoss = true;
    }

    protected override float GetCollisionRadius() => 44.0f;
    protected override float ContactDamage => 32.0f;
    protected override float TelegraphScale => 1.5f;

    protected override void SetupEnemy()
    {
        base.SetupEnemy();
        ShellMax = MaxHealth * ShellHealthRatio;
        ShellHealth = ShellMax;
    }

    public override void TakeDamage(float damage, Node2D? source = null)
    {
        TakeDamage(damage, source, false);
    }

    public override void TakeDamage(float damage, Node2D? source, bool isCrit)
    {
        if (!ShellIntact)
        {
            base.TakeDamage(damage, source, isCrit);
            return;
        }

        ShellHealth = Mathf.Max(0.0f, ShellHealth - damage * ShellDamageAbsorption);

        if (ShellHealth <= 0.0f)
        {
            ShellBroken = true;
            Armor = BrokenArmor;

            if (GetParent() is Node parent)
            {
                var shatter = new DriftWave
                {
                    GlobalPosition = GlobalPosition,
                    MaxRadius = 280.0f
                };
                parent.AddChild(shatter);
            }
        }

        // Only a small fraction leaks through until the crystalline shell is shattered.
        base.TakeDamage(Mathf.Max(1.0f, damage * (1.0f - ShellDamageAbsorption)), source, isCrit);
        QueueRedraw();
    }

    protected override void CustomPhysicsProcess(float dt)
    {
        _crystalPhase += dt * 1.4f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Color crystalCore = new Color(0.22f, 0.10f, 0.30f, 0.95f);
        Color crystalEdge = ShellIntact
            ? new Color(0.85f, 0.55f, 1.0f, 0.95f)
            : new Color(0.55f, 0.35f, 0.65f, 0.8f);

        DrawCircle(Vector2.Zero, 52.0f, new Color(0.6f, 0.2f, 0.9f, 0.16f));
        DrawCircle(Vector2.Zero, 40.0f, crystalCore);

        // Angular beta-sheet crystalline facets
        int facets = 8;
        Vector2[] poly = new Vector2[facets];
        for (int i = 0; i < facets; i++)
        {
            float angle = i * (Mathf.Tau / facets) + _crystalPhase * 0.25f;
            float radius = 40.0f + Mathf.Sin(_crystalPhase + i * 1.3f) * 4.0f;
            poly[i] = Vector2.FromAngle(angle) * radius;
        }
        DrawColoredPolygon(poly, crystalCore);
        for (int i = 0; i < facets; i++)
            DrawLine(poly[i], poly[(i + 1) % facets], crystalEdge, 3.0f);

        if (ShellIntact)
        {
            float shellRatio = ShellMax > 0.0f ? Mathf.Clamp(ShellHealth / ShellMax, 0.0f, 1.0f) : 0.0f;
            DrawArc(Vector2.Zero, 48.0f, -Mathf.Pi * 0.5f, -Mathf.Pi * 0.5f + Mathf.Tau * shellRatio,
                48, new Color(0.8f, 0.95f, 1.0f, 0.9f), 4.0f);

            for (int i = 0; i < 6; i++)
            {
                float angle = i * (Mathf.Tau / 6.0f) - _crystalPhase * 0.5f;
                Vector2 tip = Vector2.FromAngle(angle) * 56.0f;
                DrawLine(Vector2.FromAngle(angle) * 42.0f, tip, new Color(0.8f, 0.9f, 1.0f, 0.75f), 2.5f);
            }
        }
    }
}
