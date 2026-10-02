using Godot;

namespace Game.Combat;

public readonly struct DefenseProfile
{
    public bool IsInvulnerable { get; init; }
    public float Evasion { get; init; }            // 0.0 to 1.0 chance
    public float BlockChance { get; init; }        // 0.0 to 1.0 chance
    public float BlockMitigation { get; init; }    // 1.0 = 100% full block
    public float Armor { get; init; }              // POE-curved in HitPipeline
    public float DamageTakenMultiplier { get; init; } // Vulnerability (default 1.0)
}

/// <summary>POE armour curve shared by every damageable: big hits penetrate.</summary>
public static class CombatMath
{
    /// <summary>DoT curve factor over per-second dps (parity with hits; raise to soften).</summary>
    public const float DotArmorFactor = 5.0f;

    public static float FromArmorPenetrated(float armor, float damage, float penetration)
    {
        if (armor <= 0.0f || damage <= 0.0f)
            return 0.0f;
        float pen = Mathf.Clamp(penetration, 0.0f, 1.0f);
        float effective = armor * (1.0f - pen);
        if (effective <= 0.0f)
            return 0.0f;
        return Mathf.Clamp(effective / (effective + 5.0f * damage), 0.0f, 0.85f);
    }

    public static float FromArmorDot(float armor, float dps, float penetration)
    {
        if (armor <= 0.0f || dps <= 0.0f)
            return 0.0f;
        float pen = Mathf.Clamp(penetration, 0.0f, 1.0f);
        float effective = armor * (1.0f - pen);
        if (effective <= 0.0f)
            return 0.0f;
        return Mathf.Clamp(effective / (effective + DotArmorFactor * dps), 0.0f, 0.85f);
    }
}

public interface IDamageable
{
    DefenseProfile Defenses { get; }
    bool IsDead { get; }
    float TakeDamage(float finalDamage, Node2D? source = null, bool isCrit = false);
    void TakeDoTDamage(float dotDamage);
}

public interface IStatusHost
{
    StatusController Status { get; }
}
