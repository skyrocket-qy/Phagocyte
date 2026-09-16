using Godot;
using System;
using Phagocyte.Skills;

namespace Phagocyte.Player;

/// <summary>
/// B-Lymphocyte (B-Cell / 漿細胞)
/// Ranged Launch / Guidance.
/// Spherical cell with clock-face nucleus and surface receptor protrusions.
/// Specializes in Cooldown Reduction (+15%) and Projectile Speed (+25%).
/// </summary>
public partial class BCell : BaseCell
{
    public static readonly Color ColorNormal = new(0.20f, 0.45f, 0.90f, 0.65f);
    public static readonly Color ColorBurst = new(0.40f, 0.65f, 1.0f, 0.90f);
    public static readonly Color MembraneNormal = new(0.35f, 0.75f, 1.0f, 0.95f);
    public static readonly Color MembraneBurst = new(0.80f, 0.95f, 1.0f, 1.0f);

    public override void SetupCellIdentity()
    {
        MaxHealth = 85.0f;
        BaseSpeed = 220.0f;
        BaseRadius = 44.0f;
        BaseDeformationMag = 7.0f;
        DeformationSpeed = 3.2f;

        Noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Seed = (int)GD.Randi(),
            Frequency = 0.9f,
            FractalOctaves = 2
        };

        if (Stats != null)
        {
            Stats.SetBase("cooldown_reduction", 0.15f);
            Stats.SetBase("projectile_speed", 1.25f);
        }
    }

    public override void SetupNucleusShape()
    {
        // Clock-face / Cartwheel spoke nucleus shape
        var nPts = new Vector2[24];
        int nCount = 24;
        float baseR = 20.0f;
        for (int i = 0; i < nCount; i++)
        {
            float a = i * (Mathf.Tau / (float)nCount);
            // 6-spoke cartwheel modulation
            float spoke = 1.0f + 0.18f * Mathf.Cos(a * 6.0f);
            float r = baseR * spoke;
            nPts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
        if (Nucleus != null)
        {
            Nucleus.Polygon = nPts;
            Nucleus.Color = new Color(0.12f, 0.15f, 0.48f, 0.92f);
        }
    }

    public override void SetupInitialSkills()
    {
        if (CellSkillManager != null)
        {
            var salvo = new AntibodySalvoSkill();
            CellSkillManager.EquipActive(salvo, 0);
        }
    }

    public override float GetBurstMoveSpeed(float baseSp)
    {
        return baseSp * 2.2f;
    }

    public override void ApplyBurstVisuals(bool active)
    {
        if (active)
        {
            DeformationSpeed = 6.0f;
            if (Cytoplasm != null) Cytoplasm.Color = ColorBurst;
            if (Membrane != null) Membrane.DefaultColor = MembraneBurst;
        }
        else
        {
            DeformationSpeed = 3.2f;
            if (Cytoplasm != null) Cytoplasm.Color = ColorNormal;
            if (Membrane != null) Membrane.DefaultColor = MembraneNormal;
        }
    }
}
