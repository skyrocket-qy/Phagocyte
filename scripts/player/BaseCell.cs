using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Endgame;
using Phagocyte.Skills;
using Phagocyte.Enemies;
using Phagocyte.UI;

namespace Phagocyte.Player;

/// <summary>
/// Base class for all Immune Defense Cells in Project: Phagocyte.
/// Encapsulates universal stats, physics movement, 32-vertex organic deformation,
/// digestion, and experience progression.
/// </summary>
public partial class BaseCell : CharacterBody2D
{
    [Signal]
    public delegate void StatsChangedEventHandler(float health, float maxHealth, float radiusRatio);

    [Signal]
    public delegate void PathogenDigestedEventHandler(Node2D pathogen, float atpGained);

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

    // Deformation Parameters
    [Export] public int VertexCount { get; set; } = 32;
    [Export] public float DeformationSpeed { get; set; } = 3.6f;
    [Export] public float BaseDeformationMag { get; set; } = 24.0f;
    public float CurrentDeformationMag { get; set; } = 24.0f;
    [Export] public int SmoothSubdivisions { get; set; } = 4; // 32 * 4 = 128 high-density smooth points
    public FastNoiseLite? Noise { get; set; }
    public float NoiseTime { get; set; } = 0.0f;

    // Digestion tracking
    public int DigestedCount { get; set; } = 0;

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

    /// <summary>
    /// Organ fluid-mechanics velocity offset (docs/map.md §3). Added straight onto
    /// the swim velocity by <see cref="HandleMovement"/>.
    /// </summary>
    public Vector2 EnvironmentDrift { get; set; } = Vector2.Zero;

    // Inertial Nucleus offset
    public Vector2 NucleusOffset { get; set; } = Vector2.Zero;
    public Vector2 NucleusTargetOffset { get; set; } = Vector2.Zero;
    public Vector2 NucleusVelocity { get; set; } = Vector2.Zero;

    // Status debuffs
    public float SlowTimer { get; set; } = 0.0f;
    public float SlowFactor { get; set; } = 1.0f;
    public float StunTimer { get; set; } = 0.0f;
    public float InvertControlsTimer { get; set; } = 0.0f;
    public float TbBurnTimer { get; set; } = 0.0f;
    public float TbBurnDps { get; set; } = 0.0f;

    // Node references
    public Polygon2D? Cytoplasm { get; set; }
    public Line2D? Membrane { get; set; }
    public Polygon2D? Nucleus { get; set; }
    public CollisionPolygon2D? EngulfCollider { get; set; }
    public Area2D? EngulfArea { get; set; }
    public SkillManager? CellSkillManager { get; set; }

    /// <summary>2x2 organelle equipment chamber (TODO Phase 0), parallel to SkillManager.</summary>
    public OrganelleChamber? CellOrganelleChamber { get; set; }

    public CellStats? Stats { get; set; }

    public partial class GranuleCanvas : Node2D
    {
        public struct Granule
        {
            public Vector2 BasePos;
            public Vector2 CurrentOffset;
            public Vector2 Velocity;
            public float Size;
            public Color GranuleColor;
            public float BrownianPhase;
            public float LagSensitivity;
        }

        public readonly List<Granule> Granules = new();

        public override void _Draw()
        {
            for (int i = 0; i < Granules.Count; i++)
            {
                var g = Granules[i];
                Vector2 drawPos = g.BasePos + g.CurrentOffset;
                DrawCircle(drawPos, g.Size, g.GranuleColor);
                if (g.Size > 2.4f)
                {
                    DrawCircle(drawPos, g.Size * 0.45f, new Color(g.GranuleColor.R * 1.5f, g.GranuleColor.G * 1.5f, g.GranuleColor.B * 1.8f, g.GranuleColor.A * 0.75f));
                }
            }
        }
    }

    /// <summary>
    /// Dodge-roll burst ring around the membrane mid-dash
    /// (docs/skill.md §6 / tutorial.md cue 3). The HP readout moved to the
    /// top-left HUD bar (VitalsView.HpBar); this node draws no health arc.
    /// </summary>
    public partial class DodgeRing : Node2D
    {
        public BaseCell? Host { get; set; }

        public override void _Process(double delta)
        {
            if (Host == null || !GodotObject.IsInstanceValid(Host))
            {
                Visible = false;
                return;
            }
            Visible = Host.IsDodging;
            if (Visible)
                QueueRedraw();
        }

        public override void _Draw()
        {
            if (Host == null || !GodotObject.IsInstanceValid(Host) || !Host.IsDodging)
                return;
            float dashRadius = Host.CurrentRadius + 9.0f;
            DrawArc(Vector2.Zero, dashRadius, 0.0f, Mathf.Tau, 48,
                new Color(0.55f, 1.0f, 0.95f, 0.9f), 3.2f, true);
        }
    }

    private GranuleCanvas? _granuleCanvas;
    private DodgeRing? _dodgeRing;
    private Tween? _hitFlashTween;

    public override void _Ready()
    {
        AddToGroup("player");

        Cytoplasm = GetNodeOrNull<Polygon2D>("Cytoplasm");
        Membrane = GetNodeOrNull<Line2D>("Membrane");
        Nucleus = GetNodeOrNull<Polygon2D>("Nucleus");
        EngulfCollider = GetNodeOrNull<CollisionPolygon2D>("EngulfArea/EngulfCollider");
        EngulfArea = GetNodeOrNull<Area2D>("EngulfArea");
        CellSkillManager = GetNodeOrNull<SkillManager>("SkillManager");

        // Ensure CellStats container node is initialized
        if (HasNode("CellStats"))
        {
            Stats = GetNode<CellStats>("CellStats");
        }
        else
        {
            Stats = new CellStats { Name = "CellStats" };
            AddChild(Stats);
        }

        SetupCellIdentity();

        Stats.SetBase("max_health", MaxHealth);
        Stats.SetBase("move_speed", BaseSpeed);
        ApplyClassBaseStats();
        Health = Stats.GetStat("max_health");
        CurrentSpeed = Stats.GetStat("move_speed");
        CurrentRadius = BaseRadius * Stats.GetStat("area");

        Stats.StatChanged += OnStatChanged;

        // EngulfArea doubles as the contact-damage sensor: overlapping
        // monsters are polled every physics tick (see ProcessContactDamage).
        // Touching no longer engulfs — eating is skills-only now.

        SetupGranuleCanvas();
        SetupNucleusShape();
        SetupCytoplasmShader();

        // Initialize Skill System (5 Active + 5 Passive)
        if (CellSkillManager != null)
        {
            CellSkillManager.Setup(this);
            SetupInitialSkills();
        }

        // Initialize Organelle Chamber (2x2 equipment; code fallback when the
        // scene does not carry the node, mirroring the CellStats pattern).
        CellOrganelleChamber = GetNodeOrNull<OrganelleChamber>("OrganelleChamber");
        if (CellOrganelleChamber == null)
        {
            CellOrganelleChamber = new OrganelleChamber { Name = "OrganelleChamber" };
            AddChild(CellOrganelleChamber);
        }
        CellOrganelleChamber.Setup(this);

        // Initial deformation tick
        UpdatePseudopodDeformation(0.016f);

        _dodgeRing = new DodgeRing { Name = "DodgeRing", Host = this, ZIndex = 3 };
        AddChild(_dodgeRing);

        EmitStatsSignal();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDead)
        {
            Velocity = Vector2.Zero;
            return;
        }

        float dt = (float)delta;

        // Process status debuffs
        if (SlowTimer > 0.0f)
        {
            SlowTimer -= dt;
            if (SlowTimer <= 0.0f)
                SlowFactor = 1.0f;
        }
        if (StunTimer > 0.0f)
        {
            StunTimer -= dt;
        }
        if (InvertControlsTimer > 0.0f)
        {
            InvertControlsTimer -= dt;
        }
        if (TbBurnTimer > 0.0f)
        {
            TbBurnTimer -= dt;
            TakeDamage(TbBurnDps * dt);
        }

        HandleRegen(dt);
        UpdateDodgeState(dt);
        HandleMovement(dt);
        ProcessContactDamage();
        UpdatePseudopodDeformation(dt);

        if (CellSkillManager != null)
        {
            CellSkillManager.UpdateAllSkills(delta);
        }
    }

    public virtual void SetupCellIdentity()
    {
        Noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Seed = (int)GD.Randi(),
            Frequency = 0.65f,
            FractalOctaves = 2
        };
    }

    private void SetupGranuleCanvas()
    {
        _granuleCanvas = new GranuleCanvas { Name = "Granules", ZIndex = 0 };
        AddChild(_granuleCanvas);

        int count = 24;
        for (int i = 0; i < count; i++)
        {
            float angle = (float)GD.RandRange(0.0, Mathf.Tau);
            float dist = (float)GD.RandRange(6.0, (BaseRadius > 0 ? BaseRadius : 48.0f) * 0.65f);
            Vector2 basePos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

            Color col;
            float size;
            if (i < 12)
            {
                col = new Color(0.24f, 0.10f, 0.38f, 0.55f);
                size = (float)GD.RandRange(2.0, 3.5);
            }
            else if (i < 19)
            {
                col = new Color(0.42f, 0.55f, 0.70f, 0.45f);
                size = (float)GD.RandRange(1.6, 2.6);
            }
            else
            {
                col = new Color(0.80f, 0.90f, 1.0f, 0.40f);
                size = (float)GD.RandRange(1.8, 3.0);
            }

            _granuleCanvas.Granules.Add(new GranuleCanvas.Granule
            {
                BasePos = basePos,
                CurrentOffset = Vector2.Zero,
                Velocity = Vector2.Zero,
                Size = size,
                GranuleColor = col,
                BrownianPhase = (float)GD.RandRange(0.0, 100.0),
                LagSensitivity = (float)GD.RandRange(0.04, 0.09)
            });
        }
    }

    public virtual void SetupNucleusShape()
    {
        var nPts = new Vector2[32];
        float nRadius = 18.0f;
        for (int i = 0; i < 32; i++)
        {
            float a = i * (Mathf.Tau / 32.0f);
            nPts[i] = new Vector2(Mathf.Cos(a) * nRadius, Mathf.Sin(a) * nRadius);
        }
        if (Nucleus != null)
        {
            Nucleus.Polygon = nPts;
            var uvs = new Vector2[32];
            for (int i = 0; i < 32; i++)
            {
                uvs[i] = (nPts[i] / (nRadius * 2.0f)) + new Vector2(0.5f, 0.5f);
            }
            Nucleus.UV = uvs;
            Nucleus.ZIndex = 0;

            var shader = AssetLoader.Load<Shader>("res://shaders/nucleus_sphere.gdshader");
            var nMat = new ShaderMaterial { Shader = shader };
            nMat.SetShaderParameter("radius", nRadius);
            nMat.SetShaderParameter("core_color", Nucleus.Color);
            Color edgeCol = new Color(Nucleus.Color.R * 0.28f, Nucleus.Color.G * 0.15f, Nucleus.Color.B * 0.32f, 1.0f);
            nMat.SetShaderParameter("edge_color", edgeCol);
            Color highlightCol = new Color(Mathf.Min(1.0f, Nucleus.Color.R * 1.45f), Mathf.Min(1.0f, Nucleus.Color.G * 1.45f), Mathf.Min(1.0f, Nucleus.Color.B * 1.45f), 1.0f);
            nMat.SetShaderParameter("highlight_color", highlightCol);
            Nucleus.Material = nMat;
        }
        if (Cytoplasm != null)
        {
            Cytoplasm.ZIndex = 1;
        }
        if (Membrane != null)
        {
            Membrane.ZIndex = 2;
        }
    }

    public void SetupCytoplasmShader()
    {
        if (Cytoplasm == null)
            return;
        var shader = AssetLoader.Load<Shader>("res://shaders/cytoplasm_gel.gdshader");
        var mat = new ShaderMaterial { Shader = shader };
        Color baseCol = Cytoplasm.Color;
        mat.SetShaderParameter("tint_color", baseCol);
        Color rimCol = new Color(0.85f, 0.95f, 1.25f, 1.0f);
        mat.SetShaderParameter("rim_color", rimCol);
        mat.SetShaderParameter("rim_power", 3.2f);
        mat.SetShaderParameter("inner_alpha", 0.22f);
        mat.SetShaderParameter("flow_speed", 1.0f);
        mat.SetShaderParameter("cell_radius", CurrentRadius > 0 ? CurrentRadius : BaseRadius);
        Cytoplasm.Material = mat;
    }

    public virtual void SetupInitialSkills()
    {
    }

    /// <summary>
    /// Applies the class-specific base stat matrix from docs/cell.md §3.
    /// Called after max_health / move_speed are seeded from the exported fields
    /// and before Health / CurrentRadius are derived, so the calibrated values
    /// (armor, area, crit, regen, ...) are the true Lv.1 baseline.
    /// </summary>
    public virtual void ApplyClassBaseStats()
    {
    }

    private void HandleRegen(float delta)
    {
        if (Stats != null && !AfflictionManager.BlocksHealthRegen)
        {
            float regen = Stats.GetStat("health_regen");
            if (regen > 0.0f)
            {
                Heal(regen * delta);
            }
        }
    }

    private void HandleMovement(float delta)
    {
        Vector2 inputVec = ReadMoveInput();

        float targetSpeed = Stats != null ? Stats.GetStat("move_speed") : BaseSpeed;
        if (SlowTimer > 0.0f)
        {
            targetSpeed *= SlowFactor;
        }

        CurrentSpeed = targetSpeed;

        if (IsDodging)
        {
            // Dodge roll: locked-direction burst, no steering mid-dash.
            Velocity = DodgeDirection * DodgeDashSpeed + EnvironmentDrift;
            NucleusTargetOffset = -DodgeDirection * (CurrentRadius * 0.28f);
            MoveAndSlide();
            return;
        }

        if (inputVec != Vector2.Zero)
        {
            inputVec = inputVec.Normalized();
            Vector2 targetVelocity = inputVec * CurrentSpeed + EnvironmentDrift;
            Velocity = Velocity.MoveToward(targetVelocity, CurrentSpeed * 5.0f * delta);
            NucleusTargetOffset = -inputVec * (CurrentRadius * 0.28f);
        }
        else if (EnvironmentDrift != Vector2.Zero)
        {
            // Fluid current keeps dragging the cell even without input (docs/map.md §3).
            Velocity = Velocity.MoveToward(EnvironmentDrift, CurrentSpeed * 4.0f * delta);
            NucleusTargetOffset = Vector2.Zero;
        }
        else
        {
            Velocity = Velocity.MoveToward(Vector2.Zero, CurrentSpeed * 4.0f * delta);
            NucleusTargetOffset = Vector2.Zero;
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

        if (IsDead || StunTimer > 0.0f || AfflictionManager.DodgeDisabled)
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

    public virtual void UpdatePseudopodDeformation(float delta)
    {
        NoiseTime += delta * DeformationSpeed;

        float areaScale = Stats != null ? Stats.GetStat("area") : 1.0f;

        float curR = BaseRadius * areaScale;
        CurrentDeformationMag = BaseDeformationMag * areaScale;

        CurrentRadius = curR;

        var points = new Vector2[VertexCount];
        float angleStep = Mathf.Tau / (float)VertexCount;
        Vector2 vel = Velocity;
        Vector2 moveDir = vel.Length() > 20.0f ? vel.Normalized() : Vector2.Zero;

        var rawRadii = new float[VertexCount];
        for (int i = 0; i < VertexCount; i++)
        {
            float angle = i * angleStep;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            float nx1 = Mathf.Cos(angle) * 1.5f;
            float ny1 = Mathf.Sin(angle) * 1.5f;
            float nVal1 = Noise != null ? Noise.GetNoise3D(nx1, ny1, NoiseTime) : 0.0f;

            float nx2 = Mathf.Cos(angle * 2.0f) * 2.4f;
            float ny2 = Mathf.Sin(angle * 2.0f) * 2.4f;
            float nVal2 = Noise != null ? Noise.GetNoise3D(nx2, ny2, NoiseTime * 1.35f) * 0.40f : 0.0f;

            float forwardBias = 0.0f;
            if (moveDir != Vector2.Zero)
            {
                float dot = Mathf.Max(0.0f, dir.Dot(moveDir));
                forwardBias = dot * (CurrentDeformationMag * 0.85f);
            }

            // Dodge stretch: elongate along travel, pinch the flanks.
            float dashStretch = 1.0f;
            if (IsDodging && moveDir != Vector2.Zero)
            {
                float align = dir.Dot(moveDir);
                dashStretch = 1.0f + 0.25f * align - 0.15f * (1.0f - Mathf.Abs(align));
            }

            float r = (curR + ((nVal1 + nVal2) * CurrentDeformationMag) + forwardBias) * dashStretch;
            // Biological lipid bilayer clamp: never collapse into negative spikes
            rawRadii[i] = Mathf.Max(curR * 0.65f, r);
        }

        // Biological Surface Tension Filter (Laplacian Relaxation)
        // Lipid bilayer surface tension prevents acute inflections / sharp V-notches
        var relaxedRadii = new float[VertexCount];
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < VertexCount; i++)
            {
                int prev = (i - 1 + VertexCount) % VertexCount;
                int next = (i + 1) % VertexCount;
                relaxedRadii[i] = 0.25f * rawRadii[prev] + 0.50f * rawRadii[i] + 0.25f * rawRadii[next];
            }
            Array.Copy(relaxedRadii, rawRadii, VertexCount);
        }

        for (int i = 0; i < VertexCount; i++)
        {
            float angle = i * angleStep;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            points[i] = dir * rawRadii[i];
        }

        var smoothPoints = SmoothClosedPolygon(points, SmoothSubdivisions);

        if (Cytoplasm != null)
        {
            Cytoplasm.Polygon = smoothPoints;
            var uvs = new Vector2[smoothPoints.Length];
            float uvDenom = Mathf.Max(24.0f, curR * 2.4f);
            for (int i = 0; i < smoothPoints.Length; i++)
            {
                uvs[i] = (smoothPoints[i] / uvDenom) + new Vector2(0.5f, 0.5f);
            }
            Cytoplasm.UV = uvs;
            if (Cytoplasm.Material is ShaderMaterial smat)
            {
                smat.SetShaderParameter("cell_radius", curR);
            }
        }

        if (Membrane != null)
        {
            var linePoints = new Vector2[smoothPoints.Length + 1];
            Array.Copy(smoothPoints, linePoints, smoothPoints.Length);
            linePoints[^1] = smoothPoints[0];
            Membrane.Points = linePoints;
        }

        if (EngulfCollider != null)
        {
            EngulfCollider.Polygon = points;
        }

        UpdateNucleus(delta);
        UpdateGranules(delta);
    }

    private void UpdateGranules(float delta)
    {
        if (_granuleCanvas == null || _granuleCanvas.Granules.Count == 0)
            return;

        Vector2 vel = Velocity;
        float maxOffset = CurrentRadius * 0.35f;
        float expansion = CurrentRadius / BaseRadius;

        for (int i = 0; i < _granuleCanvas.Granules.Count; i++)
        {
            var g = _granuleCanvas.Granules[i];
            g.BrownianPhase += delta * 2.2f;

            // Brownian wander inside the cytoplasm
            Vector2 brownian = new Vector2(
                Mathf.Sin(g.BrownianPhase + i * 1.7f),
                Mathf.Cos(g.BrownianPhase * 1.4f + i * 2.3f)
            ) * (2.8f * expansion);

            // Flow lag behind cell velocity (protoplasmic cyclosis)
            Vector2 targetLag = -vel * g.LagSensitivity + brownian;
            if (targetLag.Length() > maxOffset)
            {
                targetLag = targetLag.Normalized() * maxOffset;
            }

            // Damped spring physics
            Vector2 accel = (targetLag - g.CurrentOffset) * 24.0f - g.Velocity * 7.0f;
            g.Velocity += accel * delta;
            g.CurrentOffset += g.Velocity * delta;

            _granuleCanvas.Granules[i] = g;
        }

        _granuleCanvas.QueueRedraw();
    }

    /// <summary>
    /// Closed Catmull-Rom Spline interpolation.
    /// Converts N control points into N * subdivisions smoothly curving vertices.
    /// </summary>
    public static Vector2[] SmoothClosedPolygon(Vector2[] pts, int subdivisions = 2)
    {
        int n = pts.Length;
        if (n < 4 || subdivisions <= 1)
            return pts;

        var smoothed = new Vector2[n * subdivisions];
        float step = 1.0f / (float)subdivisions;
        int idx = 0;

        for (int i = 0; i < n; i++)
        {
            Vector2 p0 = pts[(i - 1 + n) % n];
            Vector2 p1 = pts[i];
            Vector2 p2 = pts[(i + 1) % n];
            Vector2 p3 = pts[(i + 2) % n];

            for (int s = 0; s < subdivisions; s++)
            {
                float t = s * step;
                float t2 = t * t;
                float t3 = t2 * t;
                Vector2 pt = 0.5f * (
                    (2.0f * p1) +
                    (-p0 + p2) * t +
                    (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * t2 +
                    (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * t3
                );
                smoothed[idx++] = pt;
            }
        }

        return smoothed;
    }

    public void UpdateNucleus(float delta)
    {
        if (Nucleus == null)
            return;

        // Physical inertia lag: nucleus lags behind opposite to velocity vector
        Vector2 targetLag = -Velocity * 0.08f;
        float maxLag = CurrentRadius * 0.32f;
        if (targetLag.Length() > maxLag)
        {
            targetLag = targetLag.Normalized() * maxLag;
        }

        // Damped harmonic oscillator
        float springK = 48.0f;
        float damping = 9.5f;
        Vector2 accel = (targetLag - NucleusOffset) * springK - NucleusVelocity * damping;
        NucleusVelocity += accel * delta;
        NucleusOffset += NucleusVelocity * delta;
        Nucleus.Position = NucleusOffset;
    }

    /// <summary>
    /// Survivor-like contact damage (one-directional): overlapping monsters
    /// hurt the cell on a per-enemy tick. The cell never damages monsters
    /// by touching — eating is skills-only now.
    /// </summary>
    private void ProcessContactDamage()
    {
        if (EngulfArea == null || IsDead)
            return;
        foreach (var area in EngulfArea.GetOverlappingAreas())
        {
            if (area.GetParent() is not BaseEnemy enemy)
                continue;
            enemy.TryContactStrike(this);
        }
    }

    public virtual void OnPathogenConsumed(Node2D enemy, float atp)
    {
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

    /// <summary>
    /// Pathogen damage. Endotoxemia (docs/endgame.md §4) amplifies everything
    /// that is not explicitly environmental.
    /// </summary>
    public void TakeDamage(float amount)
    {
        ApplyDamage(amount * AfflictionManager.IncomingDamageMultiplier);
    }

    /// <summary>
    /// Environmental damage (acid tide, fibrin/toxin hazards, febrile burn):
    /// bypasses the endotoxemia pathogen-damage amplification.
    /// </summary>
    public void TakeEnvironmentalDamage(float amount)
    {
        ApplyDamage(amount);
    }

    private void ApplyDamage(float amount)
    {
        if (IsDead)
            return;

        // Stage 0: Dodge invulnerability (翻滾無敵 takes precedence over
        // everything, including environmental damage).
        if (IsInvulnerable)
        {
            DamageNumberSpawner.ShowEvaded(GlobalPosition);
            return;
        }

        // Stage 1: Fluid deformation evasion (閃避判定)
        if (Stats is CellStats cs && cs.RollEvasion())
        {
            DamageNumberSpawner.ShowEvaded(GlobalPosition);
            RunTelemetryManager.Instance?.RecordEvaded();
            return;
        }

        // Stage 2: Glycocalyx barrier block (格擋判定)
        if (Stats is CellStats csBlock && csBlock.RollBlock())
        {
            DamageNumberSpawner.ShowBlocked(GlobalPosition);
            RunTelemetryManager.Instance?.RecordBlocked();
            return;
        }

        // Stage 3: Armor damage reduction (護甲減傷)
        float dr = Stats != null ? Stats.GetDamageReductionRatio() : 0.0f;
        float finalDmg = Mathf.Max(1.0f, amount * (1.0f - dr));
        DamageNumberSpawner.ShowPlayerDamage(GlobalPosition, finalDmg);
        RunTelemetryManager.Instance?.RecordDamageTaken(finalDmg);

        // Stage 4: HP Loss & Death check
        float maxHp = Stats != null ? Stats.GetStat("max_health") : 100.0f;
        Health = Mathf.Clamp(Health - finalDmg, 0.0f, maxHp);
        HasTakenDamage = true;

        if (Health <= 0.0f)
        {
            Health = 0.0f;
            IsDead = true;
            AudioManager.Instance?.PlayPlayerDeath();
            CameraFollow.Instance?.AddTrauma(0.65f);
            EmitSignal(SignalName.Died);
        }
        else
        {
            AudioManager.Instance?.PlayPlayerHit();
            CameraFollow.Instance?.AddTrauma(finalDmg >= 15.0f ? 0.35f : 0.15f);
        }


        if (Cytoplasm != null && finalDmg > 0.1f && Health > 0.0f)
        {
            if (_hitFlashTween != null && _hitFlashTween.IsValid())
                _hitFlashTween.Kill();
            _hitFlashTween = CreateTween();
            Cytoplasm.Modulate = new Color(2.2f, 0.6f, 0.6f, 1.0f);
            _hitFlashTween.TweenProperty(Cytoplasm, "modulate", new Color(1.0f, 1.0f, 1.0f, 1.0f), 0.1);
        }

        EmitStatsSignal();
    }

    public void ApplyImpulse(Vector2 impulse)
    {
        float dr = Stats != null ? Stats.GetDamageReductionRatio() : 0.0f;
        Velocity += impulse * (1.0f - Mathf.Clamp(dr, 0.0f, 0.75f));
    }

    private void OnStatChanged(string statName, float val)
    {
        if (statName == "max_health" && Stats != null)
        {
            float maxHp = Stats.GetStat("max_health");
            Health = Mathf.Clamp(Health, 0.0f, maxHp);
        }
        EmitStatsSignal();
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

    public void ApplyInvertControls(float duration)
    {
        InvertControlsTimer = duration;
    }

    public void ApplyTBDigestionBurn(float duration, float dps)
    {
        TbBurnTimer = duration;
        TbBurnDps = dps;
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
