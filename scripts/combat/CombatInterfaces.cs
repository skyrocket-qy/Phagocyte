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
    /// <summary>Nominal hit size reproducing the legacy armor/(armor+50) ratio.</summary>
    public const float ReferenceDamage = 10.0f;

    public static float FromArmor(float armor, float damage)
    {
        if (armor <= 0.0f || damage <= 0.0f)
            return 0.0f;
        return Mathf.Clamp(armor / (armor + 5.0f * damage), 0.0f, 0.85f);
    }
}

public interface IDamageable
{
    /// <summary>Returns cached defense profile (zero-GC read).</summary>
    DefenseProfile Defenses { get; }

    /// <summary>Reports whether target is currently dead.</summary>
    bool IsDead { get; }

    /// <summary>
    /// Pure intake: deducts final pre-mitigated damage from HP,
    /// triggers sensory feedback, and returns actual damage taken.
    /// </summary>
    float TakeDamage(float finalDamage, Node2D? source = null, bool isCrit = false);

    /// <summary>Direct unmitigated DoT application (bypasses avoidance and armor).</summary>
    void TakeDoTDamage(float dotDamage);
}

public interface IStatusHost
{
    StatusController? Status { get; }
}
