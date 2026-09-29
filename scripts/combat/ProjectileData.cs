using Godot;

namespace Game.Combat;

/// <summary>
/// Projectile source faction. Selects the target set (enemy registry vs player
/// cell), never the species: minions fire as <see cref="Enemy"/> regardless of
/// summoner, hero-summoned allies would fire as <see cref="Player"/>.
/// </summary>
public enum Team
{
    Player = 0,
    Enemy = 1,
}

/// <summary>
/// Value-type data container for high-density projectile batch simulation.
/// Avoids Godot Node allocation and Garbage Collection spikes.
/// </summary>
public struct ProjectileData
{
    public Vector2 Position;
    public Vector2 Direction;
    /// <summary>Cached <see cref="Direction"/> angle (direction never changes post-spawn).</summary>
    public float Rotation;
    public float Speed;
    public float Radius;
    public float Lifetime;
    public float ElapsedTime;
    public float Damage;
    public bool IsCrit;
    public int PierceRemaining;
    public int ProjectileTypeIndex;
    public bool IsActive;

    /// <summary>Source faction: Player shots query the enemy tree, Enemy shots the player cell.</summary>
    public Team SourceTeam;
    /// <summary>Steering mode: 0 linear, 1 homing, 2 chain.</summary>
    public byte Steering;
    public float TurnRate;
    public float WobbleFreq;
    public float WobbleAmp;
    public float ReacquireRadius;
    public float Phase;
    /// <summary>Instance id of the assigned homing target (0 = steer to nearest).</summary>
    public ulong HomingTargetId;
    /// <summary>Inline on-hit effects (value types: zero heap allocation per spawn).</summary>
    public EffectSpec Effect0;
    public EffectSpec Effect1;
    public EffectSpec Effect2;
    public int EffectCount;

    public const byte SteeringLinear = 0;
    public const byte SteeringHoming = 1;
    public const byte SteeringChain = 2;

    public ulong HitTarget0;
    public ulong HitTarget1;
    public ulong HitTarget2;
    public ulong HitTarget3;

    public bool HasHitTarget(ulong id)
    {
        return HitTarget0 == id || HitTarget1 == id || HitTarget2 == id || HitTarget3 == id;
    }

    public void AddHitTarget(ulong id)
    {
        if (HitTarget0 == 0) HitTarget0 = id;
        else if (HitTarget1 == 0) HitTarget1 = id;
        else if (HitTarget2 == 0) HitTarget2 = id;
        else if (HitTarget3 == 0) HitTarget3 = id;
    }
}
