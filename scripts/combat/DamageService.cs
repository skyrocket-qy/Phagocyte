using Godot;

namespace Game.Combat;

public enum DamageType : byte
{
    Physical = 0,
    Fire = 1,
    Cold = 2,
    Lightning = 3,
    Chaos = 4,
}

[System.Flags]
public enum HitFlags : byte
{
    None = 0,
    BypassesArmor = 1,
    NeverCrit = 2,
    AlwaysCrit = 4,
    CannotBeEvaded = 8,
    CannotBeBlocked = 16,
}

/// <summary>Hit resolution outcome, returned to the attacker for leech/kill hooks.</summary>
public struct HitResult
{
    public float DamageDealt;
    public bool TargetKilled;
    public bool IsCrit;
    public bool IsEvaded;
    public bool IsBlocked;
}

/// <summary>Frozen at cast: raw damage (unbaked), type, force flags, source faction, attacker, knockback, effects.</summary>
public struct HitPayload
{
    public float RawDamage;
    public DamageType Type;
    public HitFlags Flags;
    public Team SourceFaction;
    public Vector2 Knockback;
    public EffectSpec Effect0;
    public EffectSpec Effect1;
    public EffectSpec Effect2;
    public int EffectCount;
    public ulong AttackerId;
}

public static class DamageService
{
    /// <summary>Pure cast-time packer: no RNG here. Crit is rolled per-impact by the pipeline.</summary>
    public static HitPayload Snapshot(
        float baseDamage,
        DamageType type = DamageType.Physical,
        Team sourceFaction = Team.Player,
        Vector2 knockback = default,
        EffectSpec effect0 = default,
        EffectSpec effect1 = default,
        EffectSpec effect2 = default,
        int effectCount = 0,
        ulong attackerId = 0,
        HitFlags flags = HitFlags.None)
    {
        return new HitPayload
        {
            RawDamage = baseDamage,
            Type = type,
            Flags = flags,
            SourceFaction = sourceFaction,
            Knockback = knockback,
            Effect0 = effect0,
            Effect1 = effect1,
            Effect2 = effect2,
            EffectCount = effectCount,
            AttackerId = attackerId,
        };
    }
}
