using Godot;
using System;
using Phagocyte.Skills;

namespace Phagocyte.Player;

/// <summary>
/// Dendritic Cell (樹突狀細胞)
/// Tactical Commander / Antigen Summoner.
/// Star-shaped sea-anemone body with long branching dendritic tree extensions.
/// </summary>
public partial class DendriticCell : BaseCell
{
    public static readonly Color ColorNormal = new(0.40f, 0.78f, 0.60f, 0.38f);
    public static readonly Color MembraneNormal = new(0.60f, 0.90f, 0.75f, 0.75f);

    public override void SetupCellIdentity()
    {
        MaxHealth = 110.0f;
        BaseSpeed = 225.0f;
        BaseRadius = 45.0f;
        BaseDeformationMag = 28.0f;
        DeformationSpeed = 2.8f;

        Noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Seed = (int)GD.Randi(),
            Frequency = 0.5f,
            FractalOctaves = 2
        };
    }

    public override void SetupNucleusShape()
    {
        // Central irregular oval nucleus
        var nPts = new Vector2[20];
        int nCount = 20;
        float baseR = 16.0f;
        for (int i = 0; i < nCount; i++)
        {
            float a = i * (Mathf.Tau / (float)nCount);
            float r = baseR * (1.0f + 0.22f * Mathf.Sin(a * 2.0f));
            nPts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
        if (Nucleus != null)
        {
            Nucleus.Polygon = nPts;
            Nucleus.Color = new Color(0.18f, 0.45f, 0.30f, 0.88f);
        }
    }

    public override void ApplyClassBaseStats()
    {
        if (Stats == null)
            return;

        Stats.SetBase("armor", 2.0f);
        Stats.SetBase("magnet", 260.0f);
        Stats.SetBase("duration", 1.2f);
        Stats.SetBase("cooldown_reduction", 0.10f);
    }

    public override void SetupInitialSkills()
    {
        if (CellSkillManager != null)
        {
            var tracer = new MhcTracerBeamSkill();
            CellSkillManager.EquipActive(tracer, 0);
        }
    }

    /// <summary>
    /// Dendritic tree projection morphology override: 7 prominent radial branches
    /// </summary>
    public override void UpdatePseudopodDeformation(float delta)
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

        for (int i = 0; i < VertexCount; i++)
        {
            float angle = i * angleStep;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            float nx = Mathf.Cos(angle) * 1.5f;
            float ny = Mathf.Sin(angle) * 1.5f;
            float nVal = Noise != null ? Noise.GetNoise3D(nx, ny, NoiseTime) : 0.0f;

            // 7-branch dendritic projection arms
            float armVal = Mathf.Pow(Mathf.Max(0.0f, Mathf.Cos(angle * 7.0f)), 2.0f) * (CurrentDeformationMag * 0.75f);

            float forwardBias = 0.0f;
            if (moveDir != Vector2.Zero)
            {
                float dot = Mathf.Max(0.0f, dir.Dot(moveDir));
                forwardBias = dot * (CurrentDeformationMag * 0.5f);
            }

            float r = curR + (nVal * CurrentDeformationMag * 0.6f) + armVal + forwardBias;
            points[i] = dir * Mathf.Max(14.0f, r);
        }

        var smoothPoints = SmoothClosedPolygon(points, 2);

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

        // Call base UpdateNucleus
        UpdateNucleusReflection(delta);
    }

    private void UpdateNucleusReflection(float delta)
    {
        if (Nucleus == null)
            return;

        Vector2 targetLag = -Velocity * 0.08f;
        float maxLag = CurrentRadius * 0.32f;
        if (targetLag.Length() > maxLag)
        {
            targetLag = targetLag.Normalized() * maxLag;
        }

        float springK = 48.0f;
        float damping = 9.5f;
        Vector2 accel = (targetLag - NucleusOffset) * springK - NucleusVelocity * damping;
        NucleusVelocity += accel * delta;
        NucleusOffset += NucleusVelocity * delta;
        Nucleus.Position = NucleusOffset;
    }
}
