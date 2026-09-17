using Godot;
using System;
using Phagocyte.Skills;

namespace Phagocyte.Player;

/// <summary>
/// Neutrophil (嗜中性球)
/// Kamikaze Demolition / Holdout.
/// Features jittery granular outer membrane and a distinctive multi-lobed segmented nucleus (3-4 lobes).
/// </summary>
public partial class NeutrophilCell : BaseCell
{
    public static readonly Color ColorNormal = new(0.86f, 0.88f, 0.94f, 0.40f);
    public static readonly Color MembraneNormal = new(0.92f, 0.95f, 1.0f, 0.80f);

    public override void SetupCellIdentity()
    {
        MaxHealth = 90.0f;
        BaseSpeed = 240.0f;
        BaseRadius = 46.0f;
        BaseDeformationMag = 14.0f;
        DeformationSpeed = 5.0f;

        Noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Seed = (int)GD.Randi(),
            Frequency = 1.8f,
            FractalOctaves = 2
        };
    }

    public override void SetupNucleusShape()
    {
        // Segmented 3-4 lobed nucleus (characteristic polymorphonuclear shape)
        var nPts = new Vector2[32];
        int nCount = 32;
        float baseR = 18.0f;
        for (int i = 0; i < nCount; i++)
        {
            float a = i * (Mathf.Tau / (float)nCount);
            // 3-lobed modulation
            float lobeMod = 1.0f + 0.45f * Mathf.Cos(a * 3.0f);
            float r = baseR * lobeMod;
            nPts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
        if (Nucleus != null)
        {
            Nucleus.Polygon = nPts;
            Nucleus.Color = new Color(0.48f, 0.22f, 0.62f, 0.92f);
        }
    }

    public override void SetupInitialSkills()
    {
        if (CellSkillManager != null)
        {
            var comp = new ComplementCascadeSkill();
            CellSkillManager.EquipActive(comp, 0);
        }
    }
}
