using Godot;

namespace Game.Combat;

public readonly struct DefenseProfile
{
    public bool IsInvulnerable { get; init; }
    public float Evasion { get; init; }            // 0.0 to 1.0 chance
    public float BlockChance { get; init; }        // 0.0 to 1.0 chance
    public float BlockMitigation { get; init; }    // 1.0 = 100% full block
    public int ShieldCharges { get; init; }        // Discrete hit absorption count
    public float Armor { get; init; }              // Non-linear armor
    public float DamageReduction { get; init; }    // Multiplicative DR (0.0 to 1.0)
    public float DamageTakenMultiplier { get; init; } // Vulnerability (default 1.0)
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

    /// <summary>Consumes one shield charge upon hit nullification.</summary>
    void ConsumeShieldCharge();

    /// <summary>Direct unmitigated DoT application (bypasses avoidance and armor).</summary>
    void TakeDoTDamage(float dotDamage);
}

public interface IStatusHost
{
    StatusController? Status { get; }
}
