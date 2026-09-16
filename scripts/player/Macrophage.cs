using Godot;
using System;
using Phagocyte.Skills;

namespace Phagocyte.Player;

/// <summary>
/// Macrophage (巨噬細胞)
/// Heavy Melee Tank / Phagocytosis sentinel.
/// Features amoeboid organic pseudopods, kidney-shaped nucleus,
/// 0.5% max HP passive recovery per digestion, and Respiratory Burst with Acidic Aura.
/// </summary>
public partial class Macrophage : BaseCell
{
    public Area2D? AcidicAura { get; set; }
    public CollisionShape2D? AuraCollider { get; set; }

    // Cytoplasm coloring
    public static readonly Color ColorNormal = new(0.18f, 0.72f, 0.65f, 0.62f);
    public static readonly Color ColorBurst = new(0.96f, 0.82f, 0.16f, 0.82f);
    public static readonly Color MembraneNormal = new(0.45f, 0.95f, 0.85f, 0.9f);
    public static readonly Color MembraneBurst = new(1.0f, 0.95f, 0.4f, 1.0f);

    // Backward-compatibility alias for tests and HUD
    public bool IsRespiratoryBurst
    {
        get => IsBurst;
        set => IsBurst = value;
    }

    public override void SetupCellIdentity()
    {
        MaxHealth = 100.0f;
        BaseSpeed = 230.0f;
        BaseRadius = 48.0f;
        BaseDeformationMag = 24.0f;
        DeformationSpeed = 3.6f;

        Noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Seed = (int)GD.Randi(),
            Frequency = 0.65f,
            FractalOctaves = 2
        };

        AcidicAura = GetNodeOrNull<Area2D>("AcidicAura");
        AuraCollider = GetNodeOrNull<CollisionShape2D>("AcidicAura/AuraCollider");

        if (AcidicAura != null)
        {
            AcidicAura.AreaEntered += OnAcidicAuraEntered;
            AcidicAura.Monitoring = false;
        }
    }

    public override void SetupNucleusShape()
    {
        // Indented kidney / horseshoe shaped nucleus
        var nPts = new Vector2[18];
        int nCount = 18;
        float nRadius = 16.0f;
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
            Nucleus.Color = new Color(0.42f, 0.22f, 0.68f, 0.85f);
        }
    }

    public override void SetupInitialSkills()
    {
        if (CellSkillManager != null)
        {
            var ros = new RosTorrentSkill();
            CellSkillManager.EquipActive(ros, 0);
        }
    }

    public override void OnPathogenConsumed(Node2D enemy, float atp)
    {
        // Macrophage Inherent Trait: Heals 0.5% max HP per digested pathogen
        float maxHp = Stats != null ? Stats.GetStat("max_health") : 100.0f;
        Heal(maxHp * 0.005f);
    }

    public void TriggerRespiratoryBurst()
    {
        TriggerBurst();
    }

    public override void ApplyBurstVisuals(bool active)
    {
        if (active)
        {
            DeformationSpeed = 6.0f;
            if (Cytoplasm != null) Cytoplasm.Color = ColorBurst;
            if (Membrane != null) Membrane.DefaultColor = MembraneBurst;
            if (AcidicAura != null) AcidicAura.Monitoring = true;
        }
        else
        {
            DeformationSpeed = 3.6f;
            if (Cytoplasm != null) Cytoplasm.Color = ColorNormal;
            if (Membrane != null) Membrane.DefaultColor = MembraneNormal;
            if (AcidicAura != null) AcidicAura.Monitoring = false;
        }
    }

    private void OnAcidicAuraEntered(Area2D area)
    {
        if (IsBurst)
        {
            var enemy = area.GetParent();
            if (enemy != null && enemy.HasMethod("be_engulfed"))
            {
                ConsumePathogen((Node2D)enemy);
            }
        }
    }
}
