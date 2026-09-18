namespace Phagocyte.Enemies;

/// <summary>
/// Tactical threat intent driving enemy steering (docs/pathogen.md steering model).
/// Every pathogen should exert pressure; pure idle wandering is reserved for
/// environment-neutral matter only.
/// </summary>
public enum EnemyThreatMode
{
    /// <summary>Neutral drift / bosses with scripted pressure (Brownian wander).</summary>
    Drifter = 0,

    /// <summary>Direct chemo-chaser: seeks the cell along the shortest vector (~80% tide).</summary>
    ChemoChaser = 1,

    /// <summary>Flanker: aims 100-200px ahead of the player's travel vector to punish kiting.</summary>
    Interceptor = 2,

    /// <summary>Standoff artillery: keeps distance, orbits and fires projectiles.</summary>
    Standoff = 3,

    /// <summary>Tissue invader: ignores the player, latches onto host tissue and ulcerates it.</summary>
    Invader = 4
}
