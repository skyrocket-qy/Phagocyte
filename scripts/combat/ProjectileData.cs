using Godot;

namespace Game.Combat;

public enum Team
{
    Player = 0,
    Enemy = 1,
    Neutral = 2,
}

/// <summary>Stack-allocated data container for batched projectile simulation.</summary>
public struct ProjectileData
{
    public Vector2 Position;
    public Vector2 Direction;
    public float Rotation;
    public float Speed;
    public float Radius;
    public float Lifetime;
    public float ElapsedTime;
    /// <summary>Cast-frozen hit snapshot; flight never mutates it.</summary>
    public HitPayload Payload;
    public int PierceRemaining;
    // Index into ProjectileManager batch layers; selects rendering MultiMesh.
    public int ProjectileTypeIndex;
    // True while pooled slot is live; false means free for reuse.
    public bool IsActive;

    public Team SourceTeam;
    public byte Steering;
    public float TurnRate;
    public float WobbleFreq;
    public float WobbleAmp;
    public float ReacquireRadius;
    public float Phase;
    public ulong HomingTargetId;

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
