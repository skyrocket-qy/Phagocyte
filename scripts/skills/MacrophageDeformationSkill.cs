using Godot;
using System;

namespace Phagocyte.Skills;

public partial class MacrophageDeformationSkill : BaseSkill
{
    [Export] public int VertexCount { get; set; } = 32;
    [Export] public float DeformationSpeed { get; set; } = 3.6f;
    [Export] public float BaseDeformationMag { get; set; } = 30.0f;
    public float CurrentDeformationMag { get; set; } = 30.0f;

    private FastNoiseLite? _noise;
    private float _noiseTime = 0.0f;

    public MacrophageDeformationSkill()
    {
        SkillId = "macrophage_pseudopods";
        NameKey = "SKILL_DEFORM_NAME";
        DescKey = "SKILL_DEFORM_DESC";
        BioKey = "SKILL_DEFORM_BIO";
        IconSymbol = "🦠";
        IsInnate = true;
        IsPassive = true;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Setup(CharacterBody2D pHost, int pSlot)
    {
        base.Setup(pHost, pSlot);
        _noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Seed = (int)GD.Randi(),
            Frequency = 0.65f,
            FractalOctaves = 2
        };
    }

    public override void UpdateSkill(double delta)
    {
        base.UpdateSkill(delta);
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;
        UpdatePseudopodDeformation((float)delta);
        UpdateNucleus((float)delta);
    }

    private void UpdatePseudopodDeformation(float delta)
    {
        if (Host == null || _noise == null)
            return;

        float defSpeed = DeformationSpeed;
        var hostDefSpeed = Host.Get("deformation_speed");
        if (hostDefSpeed.VariantType == Variant.Type.Float)
            defSpeed = (float)hostDefSpeed;

        _noiseTime += delta * defSpeed;

        float satiety = 0.0f;
        var hostSatiety = Host.Get("satiety");
        if (hostSatiety.VariantType == Variant.Type.Float)
            satiety = (float)hostSatiety;

        float maxSatiety = 100.0f;
        var hostMaxSatiety = Host.Get("max_satiety");
        if (hostMaxSatiety.VariantType == Variant.Type.Float)
            maxSatiety = (float)hostMaxSatiety;

        float baseR = 63.0f;
        var hostBaseR = Host.Get("base_radius");
        if (hostBaseR.VariantType == Variant.Type.Float)
            baseR = (float)hostBaseR;

        bool isBurst = false;
        var hostIsBurst = Host.Get("is_respiratory_burst");
        if (hostIsBurst.VariantType == Variant.Type.Bool)
            isBurst = (bool)hostIsBurst;

        // Satiety radius expansion (1.0x to 2.5x)
        float expansionRatio = 1.0f + (satiety / Mathf.Max(1.0f, maxSatiety)) * 1.5f;
        float curR = baseR * expansionRatio;
        CurrentDeformationMag = BaseDeformationMag * expansionRatio;

        if (isBurst)
        {
            curR *= 1.15f;
            CurrentDeformationMag *= 1.35f;
        }

        Host.Set("current_radius", curR);

        var points = new Vector2[VertexCount];
        float angleStep = Mathf.Tau / (float)VertexCount;
        Vector2 vel = Host.Velocity;
        Vector2 moveDir = vel.Length() > 20.0f ? vel.Normalized() : Vector2.Zero;

        for (int i = 0; i < VertexCount; i++)
        {
            float angle = i * angleStep;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            float nx = Mathf.Cos(angle) * 1.8f;
            float ny = Mathf.Sin(angle) * 1.8f;
            float nVal = _noise.GetNoise3D(nx, ny, _noiseTime);

            float forwardBias = 0.0f;
            if (moveDir != Vector2.Zero)
            {
                float dot = Mathf.Max(0.0f, dir.Dot(moveDir));
                forwardBias = dot * (CurrentDeformationMag * 0.6f);
            }

            float r = curR + (nVal * CurrentDeformationMag) + forwardBias;
            points[i] = dir * Mathf.Max(15.0f, r);
        }

        // Update visual Cytoplasm Polygon2D
        if (Host.HasNode("Cytoplasm"))
        {
            Host.GetNode<Polygon2D>("Cytoplasm").Polygon = points;
        }

        // Update Membrane Line2D
        if (Host.HasNode("Membrane"))
        {
            var linePoints = new Vector2[points.Length + 1];
            Array.Copy(points, linePoints, points.Length);
            linePoints[^1] = points[0];
            Host.GetNode<Line2D>("Membrane").Points = linePoints;
        }

        // "What you see is what you touch": Deep copy to CollisionPolygon2D
        if (Host.HasNode("EngulfArea/EngulfCollider"))
        {
            Host.GetNode<CollisionPolygon2D>("EngulfArea/EngulfCollider").Polygon = points;
        }
    }

    private void UpdateNucleus(float delta)
    {
        if (Host == null || !Host.HasNode("Nucleus"))
            return;

        Vector2 nucleusOffset = Vector2.Zero;
        var hostNucleusOffset = Host.Get("nucleus_offset");
        if (hostNucleusOffset.VariantType == Variant.Type.Vector2)
            nucleusOffset = (Vector2)hostNucleusOffset;

        Vector2 targetOffset = Vector2.Zero;
        var hostTargetOffset = Host.Get("nucleus_target_offset");
        if (hostTargetOffset.VariantType == Variant.Type.Vector2)
            targetOffset = (Vector2)hostTargetOffset;

        float satiety = 0.0f;
        var hostSatiety = Host.Get("satiety");
        if (hostSatiety.VariantType == Variant.Type.Float)
            satiety = (float)hostSatiety;

        float maxSatiety = 100.0f;
        var hostMaxSatiety = Host.Get("max_satiety");
        if (hostMaxSatiety.VariantType == Variant.Type.Float)
            maxSatiety = (float)hostMaxSatiety;

        nucleusOffset = nucleusOffset.Lerp(targetOffset, 8.0f * delta);
        Host.Set("nucleus_offset", nucleusOffset);

        var nucleusNode = Host.GetNode<Node2D>("Nucleus");
        nucleusNode.Position = nucleusOffset;

        float nScale = 1.0f + (satiety / Mathf.Max(1.0f, maxSatiety)) * 0.8f;
        nucleusNode.Scale = new Vector2(nScale, nScale);
    }
}
