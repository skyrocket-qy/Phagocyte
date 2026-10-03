using Godot;
using System;
using System.Collections.Generic;
using Game.Camera;
using Game.Combat;
using Game.Core;
using Game.Directors;
using Game.Skills;
using Game.Enemies;
using Game.UI;

namespace Game.Player;

/// <summary>
/// Base class for all Immune Defense Cells in Project: Game.
/// Encapsulates universal stats, physics movement, 32-vertex organic deformation,
/// and experience progression.
/// </summary>
public partial class PlayerActor : CharacterBody2D, IDamageable, IStatusHost
{
    [Signal]
    public delegate void StatsChangedEventHandler(float health, float maxHealth, float radiusRatio);

    [Signal]
    public delegate void LevelUpEventHandler(int newLevel);

    [Signal]
    public delegate void ExpChangedEventHandler(float currentExp, float maxExp, int level);

    [Signal]
    public delegate void DiedEventHandler();

    // Level & EXP Progression
    public int CurrentLevel { get; set; } = 1;
    public float CurrentExp { get; set; } = 0.0f;
    public float ExpToNextLevel { get; set; } = 30.0f;

    // Base Stats
    [Export] public float MaxHealth { get; set; } = 100.0f;
    [Export] public float BaseSpeed { get; set; } = 230.0f;
    [Export] public float BaseRadius { get; set; } = 48.0f;
    public float CurrentRadius { get; set; } = 48.0f;

    public float Health { get; set; } = 100.0f;
    public float CurrentSpeed { get; set; } = 230.0f;
    public bool IsDead { get; set; } = false;

    /// <summary>Deferred Brewmaster-stagger damage awaiting its DoT ticks (hits-only).</summary>
    public float StaggerPool { get; private set; } = 0.0f;

    /// <summary>Scheduled POE-recoup healing awaiting its ticks (hits-only).</summary>
    public float RecoupPool { get; private set; } = 0.0f;

    /// <summary>Stagger pool drains fully over this window (exponential decay).</summary>
    public const float StaggerDurationSeconds = 4.0f;

    /// <summary>Recoup pool heals fully over this window (exponential decay).</summary>
    public const float RecoupDurationSeconds = 4.0f;

    private const float PoolEpsilon = 0.05f;

    /// <summary>Grace window: dead but settlement delayed for mutual-kill trades.</summary>
    public bool IsDowned { get; private set; } = false;

    /// <summary>Remaining grace time in seconds (ticks in _PhysicsProcess).</summary>
    public float DownedTimer { get; private set; } = 0.0f;

    /// <summary>Lethal blows open a 1.0s grace window before the Died signal settles the run.</summary>
    public const float GracePeriodSeconds = 1.0f;

    /// <summary>Global slow-mo during grace; restored at settlement.</summary>
    public const float GraceTimeScale = 0.35f;

    public PlayerVisuals Visuals { get; set; } = new() { Name = "PlayerVisuals" };
    public Godot.Collections.Dictionary GetClassDef() => ClassDef();

    // Deformation Parameters (Forwarded to Visuals for backward compatibility)
    public int VertexCount { get => Visuals.VertexCount; set => Visuals.VertexCount = value; }
    public float DeformationSpeed { get => Visuals.DeformationSpeed; set => Visuals.DeformationSpeed = value; }
    public float BaseDeformationMag { get => Visuals.BaseDeformationMag; set => Visuals.BaseDeformationMag = value; }
    public float CurrentDeformationMag { get => Visuals.CurrentDeformationMag; set => Visuals.CurrentDeformationMag = value; }
    public int SmoothSubdivisions { get => Visuals.SmoothSubdivisions; set => Visuals.SmoothSubdivisions = value; }
    public FastNoiseLite? Noise { get => Visuals.Noise; set => Visuals.Noise = value; }
    public float NoiseTime { get => Visuals.NoiseTime; set => Visuals.NoiseTime = value; }

    // --- Dodge roll micro-control (docs/skill.md §6) ---
    /// <summary>Maximum dodge charges (one agile burst each).</summary>
    public const int MaxDodgeCharges = 1;

    /// <summary>Seconds to recharge one dodge charge.</summary>
    public const float DodgeRechargeSeconds = 2.5f;

    /// <summary>Dash travel time per dodge (seconds).</summary>
    public const float DodgeDuration = 0.18f;

    /// <summary>Invulnerability window per dodge (dash + grace).</summary>
    public const float DodgeInvulnSeconds = 0.22f;

    /// <summary>Dash speed multiplier over current move speed.</summary>
    public const float DodgeSpeedMultiplier = 3.2f;

    /// <summary>Remaining dodge charges (fractional while recharging).</summary>
    public float DodgeCharges { get; private set; } = MaxDodgeCharges;

    /// <summary>True while the dash burst is travelling.</summary>
    public bool IsDodging => DodgeTimer > 0.0f;

    /// <summary>True while dodge invulnerability holds (damage negated).</summary>
    public bool IsInvulnerable => InvulnTimer > 0.0f;

    public float DodgeTimer { get; private set; }
    public float InvulnTimer { get; private set; }
    public Vector2 DodgeDirection { get; private set; } = Vector2.Right;
    public float DodgeDashSpeed { get; private set; }

    private bool _dodgePressedPrev;

    /// <summary>Set on the first real HP loss — drives the dodge tutorial cue.</summary>
    public bool HasTakenDamage { get; private set; } = false;

    // Inertial Nucleus offset (delegated to Visuals)
    public Vector2 NucleusOffset { get => Visuals.NucleusOffset; set => Visuals.NucleusOffset = value; }
    public Vector2 NucleusVelocity { get => Visuals.NucleusVelocity; set => Visuals.NucleusVelocity = value; }

    // Status debuffs (stun state lives in Status, not fields)
    public float StunTimer => Status.GetTimer("stun");
    public bool IsStunned => Status.IsStunned;
    public float InvertControlsTimer { get; set; } = 0.0f;

    // Visual Node references (delegated to Visuals)
    public Polygon2D? Cytoplasm { get => Visuals.Cytoplasm; set => Visuals.Cytoplasm = value; }
    public Line2D? Membrane { get => Visuals.Membrane; set => Visuals.Membrane = value; }
    public Polygon2D? Nucleus { get => Visuals.Nucleus; set => Visuals.Nucleus = value; }
    public CollisionPolygon2D? EngulfCollider { get => Visuals.EngulfCollider; set => Visuals.EngulfCollider = value; }
    public Area2D? EngulfArea { get; set; }
    public SkillManager? CellSkillManager { get; set; }

    public EquipmentChamber? Equipment { get; set; }

    private ActorStats? _stats;
    public ActorStats? Stats
    {
        get => _stats;
        set
        {
            if (_stats != null) _stats.StatChanged -= OnStatChanged;
            _stats = value;
            if (_stats != null) _stats.StatChanged += OnStatChanged;
            InvalidateDefenses();
        }
    }

    private DefenseProfile _cachedDefenses;
    public DefenseProfile Defenses
    {
        get
        {
            var d = _cachedDefenses;
            return InvulnTimer > 0.0f ? d with { IsInvulnerable = true } : d;
        }
    }

    public void InvalidateDefenses()
    {
        _cachedDefenses = new DefenseProfile
        {
            IsInvulnerable = false,
            Evasion = Stats?.GetStat("evasion") ?? 0.0f,
            BlockChance = Stats?.GetStat("block") ?? 0.0f,
            BlockMitigation = DefenseProfile.DefaultBlockMitigation,
            Armor = Stats?.GetStat("armor") ?? 0.0f,
            DamageTakenMultiplier = RunMutatorService.IncomingDamageMultiplier * (Stats?.GetStat("damage_taken") ?? 1.0f),
        };
    }

    private StatusController? _status;

    /// <summary>Status/ailment state (slow, DoT, amp). Debuff timers and
    /// stacking live in data (<see cref="StatusController"/> over
    /// assets/data/ailments.json); the actor only reads the aggregated
    /// multipliers each frame. Lazy-created so the contract never returns null,
    /// even off-tree.</summary>
    public StatusController Status
    {
        get
        {
            if (_status == null)
            {
                _status = GetNodeOrNull<StatusController>("StatusController");
                if (_status == null)
                {
                    _status = new StatusController { Name = "StatusController" };
                    if (IsInsideTree())
                        AddChild(_status);
                }
            }
            return _status;
        }
    }

    /// <summary>Data-driven class id (assets/data/classes.json). Set before entering the tree.</summary>
    [Export] public string ClassId = "macrophage";

    private Godot.Collections.Dictionary _classDef = new();

    /// <summary>Class def, loaded lazily so off-tree identity passes work.</summary>
    private Godot.Collections.Dictionary ClassDef()
    {
        if (_classDef.Count == 0)
            _classDef = GameManager.GetPlayerClass(ClassId);
        return _classDef;
    }

    public const float DeformInterval = 1.0f / 30.0f;
    private float _deformAccum;

    public override void _Ready()
    {
        AddToGroup("player");

        if (Visuals.GetParent() == null)
        {
            AddChild(Visuals);
        }

        EngulfArea = GetNodeOrNull<Area2D>("EngulfArea");
        CellSkillManager = GetNodeOrNull<SkillManager>("SkillManager");

        if (HasNode("ActorStats"))
        {
            Stats = GetNode<ActorStats>("ActorStats");
        }
        else
        {
            Stats = new ActorStats { Name = "ActorStats" };
            AddChild(Stats);
        }

        _classDef = GameManager.GetPlayerClass(ClassId);
        SetupCellIdentity();

        Stats.SetBase("max_health", MaxHealth);
        Stats.SetBase("move_speed", BaseSpeed);
        ApplyClassBaseStats();
        Health = Stats.GetStat("max_health");
        CurrentSpeed = Stats.GetStat("move_speed");
        CurrentRadius = BaseRadius * Stats.GetStat("area");

        Stats.StatChanged += OnStatChanged;

        Visuals.Setup(this);

        // Initialize Skill System (5 Active + 5 Passive)
        if (CellSkillManager != null)
        {
            CellSkillManager.Setup(this);
            SetupInitialSkills();
        }

        // Initialize Equipment Chamber (2x2 equipment; code fallback when the
        // scene does not carry the node, mirroring the ActorStats pattern).
        Equipment = GetNodeOrNull<EquipmentChamber>("EquipmentChamber");
        if (Equipment == null)
        {
            Equipment = new EquipmentChamber { Name = "EquipmentChamber" };
            AddChild(Equipment);
        }
        Equipment.Setup(this);

        // Initial deformation tick
        UpdateBodyDeformation(0.016f);

        EmitStatsSignal();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDead)
        {
            Velocity = Vector2.Zero;
            if (IsDowned)
                TickDowned((float)delta);
            return;
        }

        float dt = (float)delta;

        // Process status debuffs (slow and stun state live in Status, not fields)
        if (InvertControlsTimer > 0.0f)
        {
            InvertControlsTimer -= dt;
        }

        HandleRegen(dt);
        TickStaggerRecoup(dt);
        UpdateDodgeState(dt);
        HandleMovement(dt);
        ProcessContactDamage();

        _deformAccum += dt;
        if (_deformAccum >= DeformInterval)
        {
            float step = _deformAccum;
            _deformAccum = 0.0f;
            UpdateBodyDeformation(step);
        }

        if (CellSkillManager != null)
        {
            CellSkillManager.UpdateAllSkills(delta);
        }
    }

    /// <summary>
    /// Identity from the class definition (assets/data/classes.json):
    /// vitals, body size, deformation rig, noise field and membrane tints.
    /// </summary>
    public void SetupCellIdentity()
    {
        MaxHealth = CatalogLoader.GetFloat(ClassDef(), "base_hp", MaxHealth);
        BaseSpeed = CatalogLoader.GetFloat(ClassDef(), "base_speed", BaseSpeed);
        BaseRadius = BodyDeformation.RealSizeToRadius(CatalogLoader.GetFloat(ClassDef(), "body_microns", 20.0f));
        if (Visuals.Host == null)
            Visuals.Setup(this);
        else
            Visuals.SetupIdentityVisuals(ClassDef());
    }

    public void SetupNucleusShape() => Visuals.SetupNucleusShape(ClassDef());
    public void SetupCytoplasmShader() => Visuals.SetupCytoplasmShader();

    /// <summary>Innate skill from the class definition, resolved by id.</summary>
    public void SetupInitialSkills()
    {
        if (CellSkillManager == null)
            return;
        string skillId = CatalogLoader.GetString(ClassDef(), "innate_skill");
        if (skillId == "")
            return;
        var skill = SkillFactory.CreateActive(skillId);
        if (skill != null)
            CellSkillManager.EquipActive(skill, CatalogLoader.GetInt(ClassDef(), "innate_slot", 0));
    }

    /// <summary>
    /// Lv.1 stat matrix from the class definition: base armor, the signature
    /// trait stat and any extra stats. Called after max_health / move_speed
    /// are seeded and before Health / CurrentRadius are derived.
    /// </summary>
    public void ApplyClassBaseStats()
    {
        if (Stats == null)
            return;
        Stats.SetBase("armor", CatalogLoader.GetFloat(ClassDef(), "base_armor", 0.0f));
        string trait = CatalogLoader.GetString(ClassDef(), "trait_stat");
        if (trait != "")
            Stats.SetBase(trait, CatalogLoader.GetFloat(ClassDef(), "trait_stat_value", 0.0f));
        if (ClassDef().TryGetValue("extra_stats", out Variant extras) && extras.VariantType == Variant.Type.Dictionary)
        {
            foreach (var kv in (Godot.Collections.Dictionary)extras)
                Stats.SetBase(kv.Key.AsString(), CatalogLoader.ToFloat(kv.Value));
        }
    }

    private void HandleRegen(float delta)
    {
        if (Stats != null && !RunMutatorService.BlocksHealthRegen)
        {
            float regen = Stats.GetStat("health_regen");
            if (regen > 0.0f)
            {
                Heal(regen * delta);
            }
        }
    }

    /// <summary>Drains the stagger pool as unmitigated DoT and pays the recoup pool as healing.</summary>
    private void TickStaggerRecoup(float dt)
    {
        if (StaggerPool > 0.0f)
        {
            float tick = StaggerPool * dt / StaggerDurationSeconds;
            if (StaggerPool < PoolEpsilon)
                tick = StaggerPool;
            StaggerPool -= tick;
            if (tick > 0.0f)
                TakeDoTDamageMitigated(tick, false);
        }
        if (!IsDead && RecoupPool > 0.0f)
        {
            float heal = RecoupPool * dt / RecoupDurationSeconds;
            if (RecoupPool < PoolEpsilon)
                heal = RecoupPool;
            RecoupPool -= heal;
            if (heal > 0.0f)
                Heal(heal);
        }
    }

    private void HandleMovement(float delta)
    {
        Vector2 inputVec = ReadMoveInput();

        float targetSpeed = Stats != null ? Stats.GetStat("move_speed") : BaseSpeed;
        targetSpeed *= Status.SpeedMultiplier;

        CurrentSpeed = targetSpeed;

        if (IsDodging)
        {
            // Dodge roll: locked-direction burst, no steering mid-dash.
            Velocity = DodgeDirection * DodgeDashSpeed;
            MoveAndSlide();
            return;
        }

        if (inputVec != Vector2.Zero)
        {
            inputVec = inputVec.Normalized();
            Vector2 targetVelocity = inputVec * CurrentSpeed;
            Velocity = Velocity.MoveToward(targetVelocity, CurrentSpeed * 5.0f * delta);
        }
        else
        {
            Velocity = Velocity.MoveToward(Vector2.Zero, CurrentSpeed * 4.0f * delta);
        }

        MoveAndSlide();
    }

    /// <summary>Normalized move vector from the InputMap (zero while stunned).</summary>
    private Vector2 ReadMoveInput()
    {
        Vector2 inputVec = Vector2.Zero;
        if (StunTimer <= 0.0f)
        {
            if (Input.IsActionPressed("move_left"))
                inputVec.X -= 1.0f;
            if (Input.IsActionPressed("move_right"))
                inputVec.X += 1.0f;
            if (Input.IsActionPressed("move_up"))
                inputVec.Y -= 1.0f;
            if (Input.IsActionPressed("move_down"))
                inputVec.Y += 1.0f;

            if (InvertControlsTimer > 0.0f)
            {
                inputVec = -inputVec;
            }
        }
        return inputVec;
    }

    /// <summary>
    /// Dodge roll state (press Space / L2 for one agile burst).
    /// Microtubule Sclerosis (docs/endgame.md §4) hard-disables it.
    /// </summary>
    private void UpdateDodgeState(float delta)
    {
        if (DodgeTimer > 0.0f)
            DodgeTimer = Mathf.Max(0.0f, DodgeTimer - delta);
        if (InvulnTimer > 0.0f)
            InvulnTimer = Mathf.Max(0.0f, InvulnTimer - delta);
        if (DodgeCharges < MaxDodgeCharges && !IsDead)
            DodgeCharges = Mathf.Min((float)MaxDodgeCharges, DodgeCharges + delta / DodgeRechargeSeconds);

        bool pressed = ReadDodgePressedRaw();
        bool justPressed = pressed && !_dodgePressedPrev;
        _dodgePressedPrev = pressed;

        if (IsDead || StunTimer > 0.0f || RunMutatorService.DodgeDisabled)
            return;

        if (justPressed && DodgeCharges >= 1.0f && !IsDodging)
        {
            DodgeCharges -= 1.0f;
            Vector2 dir = ReadMoveInput();
            if (dir == Vector2.Zero)
                dir = Velocity.Length() > 20.0f ? Velocity.Normalized() : Vector2.Right;
            DodgeDirection = dir.Normalized();
            float baseSpeed = Stats != null ? Stats.GetStat("move_speed") : BaseSpeed;
            DodgeDashSpeed = baseSpeed * DodgeSpeedMultiplier;
            DodgeTimer = DodgeDuration;
            InvulnTimer = DodgeInvulnSeconds;
            AudioManager.Instance?.PlayDodge();
        }
    }

    private static bool ReadDodgePressedRaw()
    {
        if (Input.IsActionPressed("dodge"))
            return true;

        foreach (int device in Input.GetConnectedJoypads())
        {
            if (Input.GetJoyAxis(device, JoyAxis.TriggerLeft) > 0.5f)
                return true;
        }

        return false;
    }

    public void UpdateBodyDeformation(float delta)
    {
        if (Visuals.Host == null)
            Visuals.Setup(this);
        Visuals.UpdateBodyDeformation(delta);
    }

    public static void SmoothClosedPolygonInto(Vector2[] pts, int subdivisions, Vector2[] dest)
        => PlayerVisuals.SmoothClosedPolygonInto(pts, subdivisions, dest);

    public void UpdateNucleus(float delta)
    {
        if (Visuals.Host == null)
            Visuals.Setup(this);
        Visuals.UpdateNucleus(delta);
    }

    public void FlashModulate(Color flashColor, float duration) => Visuals.FlashModulate(flashColor, duration);

    /// <summary>
    /// Survivor-like contact damage (one-directional): overlapping monsters
    /// hurt the cell on a per-enemy tick. The cell never damages monsters
    /// by touching.
    /// </summary>
    private void ProcessContactDamage()
    {
        if (EngulfArea == null || IsDead)
            return;
        foreach (var area in EngulfArea.GetOverlappingAreas())
        {
            if (area.GetParent() is not EnemyActor enemy)
                continue;
            enemy.TryContactStrike(this);
        }
    }

    public void AddExp(float amount)
    {
        CurrentExp += amount;
        while (CurrentExp >= ExpToNextLevel)
        {
            CurrentExp -= ExpToNextLevel;
            CurrentLevel += 1;
            ExpToNextLevel = ExpToNextLevel * 1.35f + 15.0f;
            AudioManager.Instance?.PlayLevelUp();
            FrameSpikeLog.MarkLevelUp();
            EmitSignal(SignalName.LevelUp, CurrentLevel);
        }
        EmitSignal(SignalName.ExpChanged, CurrentExp, ExpToNextLevel, CurrentLevel);
    }

    public void Heal(float amount)
    {
        float maxHp = Stats != null ? Stats.GetStat("max_health") : 100.0f;
        Health = Mathf.Clamp(Health + amount, 0.0f, maxHp);
        EmitStatsSignal();
    }

    /// <summary>Pure damage intake (IDamageable): staggers part of the hit into a
    /// DoT pool, schedules recoup healing, deducts the instant remainder from HP.</summary>
    public float TakeDamage(float finalDamage, bool isCrit = false)
    {
        if (IsDead || finalDamage <= 0.0f)
            return 0.0f;

        float staggerPct = Stats != null ? Stats.GetStat("stagger") : 0.0f;
        float instant = finalDamage * (1.0f - staggerPct);
        StaggerPool += finalDamage - instant;
        float recoupPct = Stats != null ? Stats.GetStat("recoup") : 0.0f;
        RecoupPool += finalDamage * recoupPct;

        DamageNumberSpawner.ShowPlayerDamage(GlobalPosition, instant);
        RunTelemetryManager.Instance?.RecordDamageTaken(instant);

        float maxHp = Stats != null ? Stats.GetStat("max_health") : 100.0f;
        Health = Mathf.Clamp(Health - instant, 0.0f, maxHp);
        HasTakenDamage = true;

        if (Health <= 0.0f)
            EnterDowned();
        else
        {
            AudioManager.Instance?.PlayPlayerHit();
            CameraFollow.Instance?.AddTrauma(finalDamage >= 15.0f ? 0.35f : 0.15f);
        }

        Visuals.FlashHit(finalDamage);
        EmitStatsSignal();
        return finalDamage;
    }

    /// <summary>Generic DoT entry (IDamageable): damage_taken mitigates at intake; armor was applied at status-application.</summary>
    public void TakeDoTDamage(float dotDamage)
    {
        TakeDoTDamageMitigated(dotDamage, true);
    }

    private void TakeDoTDamageMitigated(float dotDamage, bool mitigate)
    {
        if (IsDead || dotDamage <= 0.0f)
            return;
        if (mitigate)
        {
            float mult = RunMutatorService.IncomingDamageMultiplier * (Stats?.GetStat("damage_taken") ?? 1.0f);
            if (mult > 0.0f)
                dotDamage *= mult;
        }
        float maxHp = Stats != null ? Stats.GetStat("max_health") : 100.0f;
        Health = Mathf.Clamp(Health - dotDamage, 0.0f, maxHp);
        HasTakenDamage = true;
        RunTelemetryManager.Instance?.RecordDamageTaken(dotDamage);
        if (Health <= 0.0f)
            EnterDowned();
        EmitStatsSignal();
    }

    /// <summary>Lethal blows mark death at once but delay the Died signal through grace.</summary>
    private void EnterDowned()
    {
        Health = 0.0f;
        IsDead = true;
        IsDowned = true;
        DownedTimer = GracePeriodSeconds;
        Engine.TimeScale = GraceTimeScale;
        AudioManager.Instance?.PlayPlayerDeath();
        CameraFollow.Instance?.AddTrauma(0.65f);
    }

    /// <summary>Grace countdown; expiry emits Died so settlement runs exactly once.</summary>
    private void TickDowned(float dt)
    {
        DownedTimer -= dt;
        if (DownedTimer > 0.0f)
            return;
        IsDowned = false;
        DownedTimer = 0.0f;
        Engine.TimeScale = 1.0f;
        EmitSignal(SignalName.Died);
    }

    private void OnStatChanged(string statName, float val)
    {
        if (statName == "max_health" && Stats != null)
        {
            float maxHp = Stats.GetStat("max_health");
            Health = Mathf.Clamp(Health, 0.0f, maxHp);
        }
        InvalidateDefenses();
        EmitStatsSignal();
    }

    public void ApplyInvertControls(float duration)
    {
        InvertControlsTimer = duration;
    }

    public void DrainAtp(float amount)
    {
        CurrentExp = Mathf.Max(0.0f, CurrentExp - amount);
        EmitStatsSignal();
        EmitSignal(SignalName.ExpChanged, CurrentExp, ExpToNextLevel, CurrentLevel);
    }

    public void EmitStatsSignal()
    {
        float maxHp = Stats != null ? Stats.GetStat("max_health") : 100.0f;
        float ratio = CurrentRadius / BaseRadius;
        EmitSignal(SignalName.StatsChanged, Health, maxHp, ratio);
    }
}

