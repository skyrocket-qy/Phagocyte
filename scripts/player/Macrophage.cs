using Godot;
using System;
using Phagocyte.Core;
using Phagocyte.Skills;

namespace Phagocyte.Player;

/// <summary>
/// Macrophage (巨噬細胞)
/// Heavy Melee Tank / Phagocytosis sentinel.
/// Features amoeboid organic pseudopods and a kidney-shaped nucleus.
/// </summary>
public partial class Macrophage : BaseCell
{
    // Cytoplasm coloring (Deep Transparent Aqueous Glass + Deep Violet Chromatin Nucleus)
    public static readonly Color ColorNormal = new(0.18f, 0.32f, 0.52f, 0.22f);
    public static readonly Color MembraneNormal = new(0.80f, 0.92f, 1.0f, 0.60f);

    public override void SetupCellIdentity()
    {
        MaxHealth = 140.0f;
        BaseSpeed = 210.0f;
        BaseRadius = Morphology.RealSizeToRadius(20.0f);
        BaseDeformationMag = 23.3f;
        DeformationSpeed = 3.6f;

        Noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Seed = (int)GD.Randi(),
            Frequency = 0.65f,
            FractalOctaves = 2
        };
    }

    public override void SetupNucleusShape()
    {
        // Indented kidney / horseshoe shaped nucleus
        var nPts = new Vector2[18];
        int nCount = 18;
        float nRadius = 13.3f;
        for (int i = 0; i < nCount; i++)
        {
            float a = i * (Mathf.Tau / (float)nCount);
            // Kidney notch at angle PI
            float notch = 1.0f - 0.4f * Mathf.Max(0.0f, Mathf.Cos(a));
            float r = nRadius * notch;
            nPts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
        if (Nucleus != null)
        {
            Nucleus.Polygon = nPts;
            Nucleus.Color = new Color(0.48f, 0.18f, 0.68f, 0.88f);
        }
    }

    public override void ApplyClassBaseStats()
    {
        if (Stats == null)
            return;

        Stats.SetBase("armor", 10.0f);
        Stats.SetBase("area", 1.25f);
        Stats.SetBase("might", 1.0f);
        Stats.SetBase("block", 0.08f);
    }

    public override void _Ready()
    {
        base._Ready();
    }

    public override void SetupInitialSkills()
    {
        if (CellSkillManager != null)
        {
            // Innate active: Phagocytic Grasp (吞噬偽足).
            // Amoeboid deformation itself is chassis-visual in BaseCell;
            // the innate is the functional grasp.
            var grasp = new PhagocyticGraspSkill();
            CellSkillManager.EquipActive(grasp, 0);
        }
    }
}
