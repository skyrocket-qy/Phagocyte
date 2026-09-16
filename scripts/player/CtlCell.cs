using Godot;
using System;
using Phagocyte.Skills;

namespace Phagocyte.Player;

/// <summary>
/// Killer T-Cell (CTL / CD8+)
/// High-Speed Assassin / Perforation.
/// Compact micro-trembling spherical body with a massive circular nucleus
/// occupying ~80% of cell volume. High base speed & critical strike affinity.
/// </summary>
public partial class CtlCell : BaseCell
{
    public static readonly Color ColorNormal = new(0.85f, 0.18f, 0.32f, 0.65f);
    public static readonly Color ColorBurst = new(1.0f, 0.45f, 0.15f, 0.85f);
    public static readonly Color MembraneNormal = new(1.0f, 0.35f, 0.50f, 0.95f);
    public static readonly Color MembraneBurst = new(1.0f, 0.85f, 0.30f, 1.0f);

    public override void SetupCellIdentity()
    {
        MaxHealth = 75.0f;
        BaseSpeed = 280.0f;
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

        // Critical Strike & Agility innate bonuses
        if (Stats != null)
        {
            Stats.SetBase("crit_chance", 0.15f);
            Stats.SetBase("crit_damage", 1.75f);
            Stats.SetBase("might", 1.10f);
        }
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
            Nucleus.Color = new Color(0.48f, 0.06f, 0.16f, 0.92f);
        }
    }

    public override void SetupInitialSkills()
    {
        if (CellSkillManager != null)
        {
            var perforin = new PerforinLanceSkill();
            CellSkillManager.EquipActive(perforin, 0);
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
            DeformationSpeed = 10.0f;
            if (Cytoplasm != null) Cytoplasm.Color = ColorBurst;
            if (Membrane != null) Membrane.DefaultColor = MembraneBurst;
        }
        else
        {
            DeformationSpeed = 7.2f;
            if (Cytoplasm != null) Cytoplasm.Color = ColorNormal;
            if (Membrane != null) Membrane.DefaultColor = MembraneNormal;
        }
    }

    public override void OnPathogenConsumed(Node2D enemy, float atp)
    {
        // Perforation execution: chance to trigger instant apoptosis
        if (IsBurst && enemy != null && GodotObject.IsInstanceValid(enemy) && enemy.HasMethod("take_damage"))
        {
            enemy.Call("take_damage", 60.0f);
        }
    }
}
