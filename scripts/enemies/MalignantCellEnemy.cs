using Godot;
using Phagocyte.Core;
using System;
using Phagocyte.Combat;

namespace Phagocyte.Enemies;

/// <summary>
/// Malignant Mutated Tumor Cell (異變腫瘤細胞)
/// Large volume, downregulates surface MHC-I. Best cleared with close-range damage or lysis.
/// </summary>
public partial class MalignantCellEnemy : BaseEnemy
{
    /// <summary>Autonomous mitosis cadence: split once every 20 seconds survived.</summary>
    public const float MitosisInterval = 20.0f;

    /// <summary>Density gate: stop replicating when this many malignant cells are nearby.</summary>
    public const int MaxNearbySiblings = 5;

    public const float SplitHealthRatio = 0.5f;
    public const float SiblingCheckRadius = 450.0f;

    public int SplitCount { get; private set; }

    private float _membranePhase = 0.0f;
    private float _mitosisTimer = 0.0f;
    private float _splitPulse = 0.0f;
    private const float DrawScale = 37.0f / 34.0f; // A-formula visual match: 34px -> 37px

    public MalignantCellEnemy()
    {
        EnemyId = "malignant_cell";
        DisplayNameKey = "PATHOGEN_MALIGNANT_NAME";
        MaxHealth = 120.0f;
        AtpValue = 48.0f;
        BaseScore = 100;
        FloatSpeed = 26.0f;
        ThreatMode = EnemyThreatMode.ChemoChaser;
        Armor = 3.0f;
        IsElite = true;
    }

    protected override float GetCollisionRadius() => Morphology.RealSizeToRadius(15.0f);

    protected override void CustomPhysicsProcess(float dt)
    {
        _membranePhase += dt * 3.5f;
        _splitPulse = Mathf.Max(0.0f, _splitPulse - dt);

        _mitosisTimer += dt;
        if (_mitosisTimer >= MitosisInterval)
        {
            _mitosisTimer -= MitosisInterval;
            TryMitosis();
        }

        RedrawIfVisible();
    }

    /// <summary>
    /// Autonomous mitosis: spawns one half-HP daughter cell in place, unless the local
    /// malignant-cell density is already saturated (contact inhibition of proliferation).
    /// </summary>
    public bool TryMitosis()
    {
        if (CountNearbySiblings() >= MaxNearbySiblings)
            return false;

        var parent = GetParent();
        if (parent == null)
            return false;

        Vector2 offset = Vector2.FromAngle(GD.Randf() * Mathf.Tau) * (float)GD.RandRange(34.0, 52.0);
        var daughter = new MalignantCellEnemy
        {
            GlobalPosition = GlobalPosition + offset
        };
        daughter.MaxHealth = MaxHealth * SplitHealthRatio;
        parent.AddChild(daughter);

        SplitCount++;
        _splitPulse = 0.45f;
        RedrawIfVisible();
        return true;
    }

    private int CountNearbySiblings()
    {
        return TargetingService.CountInRadius(
            GlobalPosition,
            SiblingCheckRadius,
            enemy => enemy is MalignantCellEnemy sibling && sibling != this);
    }

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0.0f, new Vector2(DrawScale, DrawScale));
        // 1. Irregular deformed cytoplasm with cancerous blebbing
        Color cytoplasmColor = new Color(0.32f, 0.12f, 0.18f, 0.85f);
        Color membraneColor = new Color(0.75f, 0.22f, 0.32f, 0.95f);

        int pointsCount = 24;
        Vector2[] poly = new Vector2[pointsCount];
        for (int i = 0; i < pointsCount; i++)
        {
            float ang = i * (Mathf.Tau / pointsCount);
            float deform = Mathf.Sin(_membranePhase + i * 1.5f) * 4.5f + Mathf.Cos(_membranePhase * 0.7f + i * 2.1f) * 3.0f;
            poly[i] = Vector2.FromAngle(ang) * (32.0f + deform);
        }

        DrawColoredPolygon(poly, cytoplasmColor);
        for (int i = 0; i < pointsCount; i++)
        {
            Vector2 p1 = poly[i];
            Vector2 p2 = poly[(i + 1) % pointsCount];
            DrawLine(p1, p2, membraneColor, 2.5f);
        }

        // 2. Multi-nucleated pleomorphic dark nuclei (cancerous hallmarks)
        Color nucleusCol = new Color(0.18f, 0.05f, 0.22f, 0.95f);
        Color chromatinCol = new Color(0.55f, 0.15f, 0.65f, 0.90f);

        Vector2[] nucleusPositions = new Vector2[]
        {
            new Vector2(-10, -6),
            new Vector2(8, -8),
            new Vector2(2, 9)
        };

        foreach (var np in nucleusPositions)
        {
            DrawCircle(np, 9.0f, nucleusCol);
            DrawCircle(np, 5.5f, chromatinCol);
        }

        // 3. Mitosis flash ring when a daughter cell is released
        if (_splitPulse > 0.0f)
        {
            float t = 1.0f - (_splitPulse / 0.45f);
            DrawArc(Vector2.Zero, 32.0f + t * 26.0f, 0.0f, Mathf.Tau, 32,
                new Color(0.9f, 0.3f, 0.5f, 1.0f - t), 3.5f);
        }
        DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
    }
}
