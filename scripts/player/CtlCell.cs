using Godot;
using System;
using Phagocyte.Skills;

namespace Phagocyte.Player;

/// <summary>
/// Killer T-Cell (CTL / CD8+)
/// High-Speed Assassin / Perforation.
/// Compact micro-trembling spherical body with a massive circular nucleus
/// occupying ~80% of cell volume.
/// </summary>
public partial class CtlCell : BaseCell
{
    public static readonly Color ColorNormal = new(0.85f, 0.32f, 0.45f, 0.45f);
    public static readonly Color MembraneNormal = new(0.95f, 0.55f, 0.68f, 0.80f);

    public override void SetupCellIdentity()
    {
        MaxHealth = 90.0f;
        BaseSpeed = 260.0f;
        BaseRadius = 42.0f;
        BaseDeformationMag = 6.0f;
        DeformationSpeed = 7.2f;

        Noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Seed = (int)GD.Randi(),
            Frequency = 2.4f,
            FractalOctaves = 1
        };
    }

    public override void SetupNucleusShape()
    {
        // Massive circular nucleus occupying ~80% of interior
        var nPts = new Vector2[24];
        int nCount = 24;
        float nRadius = 32.0f;
        for (int i = 0; i < nCount; i++)
        {
            float a = i * (Mathf.Tau / (float)nCount);
            nPts[i] = new Vector2(Mathf.Cos(a) * nRadius, Mathf.Sin(a) * nRadius);
        }
        if (Nucleus != null)
        {
            Nucleus.Polygon = nPts;
            Nucleus.Color = new Color(0.48f, 0.12f, 0.32f, 0.92f);
        }
    }

    public override void ApplyClassBaseStats()
    {
        if (Stats == null)
            return;

        Stats.SetBase("armor", 0.0f);
        Stats.SetBase("crit_chance", 0.15f);
        Stats.SetBase("evasion", 0.10f);
        Stats.SetBase("pierce", 1.0f);
    }

    public override void SetupInitialSkills()
    {
        if (CellSkillManager != null)
        {
            var perforin = new PerforinLanceSkill();
            CellSkillManager.EquipActive(perforin, 0);
        }
    }
}
