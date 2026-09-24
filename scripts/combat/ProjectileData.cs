using Godot;

namespace Phagocyte.Combat;

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
