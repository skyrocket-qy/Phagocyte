using Godot;
using Phagocyte.Player;

namespace Phagocyte.Environment;

/// <summary>
/// Per-organ fluid mechanics and physiological environment that act directly on
/// the player cell (docs/map.md §3 / TODO module 3). One instance is created by
/// Main per run; <see cref="PlayerDrift"/> is added to the player's swim velocity
/// and <see cref="Process"/> spawns/drives the organ's environmental props.
/// </summary>
public abstract class MapEnvironment
{
    public abstract string MapId { get; }

    /// <summary>World-space velocity offset applied to the player's swim velocity.</summary>
    public Vector2 PlayerDrift { get; protected set; } = Vector2.Zero;

    /// <summary>
    /// Fluid current applied to free pathogens (world-space px/s). Owned by the
    /// organ environment so Main applies one generic drift instead of repeating
    /// per-map formulas.
    /// </summary>
    public Vector2 FluidVector { get; protected set; } = Vector2.Zero;

    /// <summary>Hard (Acute Crisis) variant: exclusive organ hazards go permanent.</summary>
    public bool HardMode { get; set; }

    protected float Time { get; private set; }

    protected RandomNumberGenerator Rng { get; } = new();

    public virtual void Attach(Main main)
    {
        Rng.Seed = (ulong)MapId.GetHashCode() ^ 0x51F15EEDUL;
    }

    public void Tick(Main main, float dt)
    {
        Time += dt;
        Process(main, dt);
    }

    protected abstract void Process(Main main, float dt);

    protected static BaseCell? Cell(Main main)
    {
        return main.Player as BaseCell;
    }

    protected static Node2D? Container(Main main)
    {
        return main.EnemyContainer;
    }

    /// <summary>Hard mode shortens timers by +50% frequency (docs/achievement.md §2.1).</summary>
    protected float HazardInterval(float normalSeconds)
    {
        return HardMode ? normalSeconds / 1.5f : normalSeconds;
    }

    /// <summary>Random point within <paramref name="minDist"/>-<paramref name="maxDist"/> of the anchor.</summary>
    protected Vector2 PointNear(Vector2 anchor, Vector2 arenaSize, float minDist, float maxDist)
    {
        float angle = Rng.Randf() * Mathf.Tau;
        float dist = Rng.RandfRange(minDist, maxDist);
        Vector2 point = anchor + Vector2.FromAngle(angle) * dist;
        float halfW = Mathf.Max(80.0f, arenaSize.X * 0.5f - 140.0f);
        float halfH = Mathf.Max(80.0f, arenaSize.Y * 0.5f - 140.0f);
        point.X = Mathf.Clamp(point.X, -halfW, halfW);
        point.Y = Mathf.Clamp(point.Y, -halfH, halfH);
        return point;
    }

    protected static int CountChildren<T>(Node2D? container) where T : Node
    {
        if (container == null)
            return 0;

        int count = 0;
        foreach (var child in container.GetChildren())
        {
            if (child is T && GodotObject.IsInstanceValid(child))
                count++;
        }
        return count;
    }

    public static MapEnvironment ForMap(string mapId)
    {
        return mapId switch
        {
            "alveolar_space" => new AlveolarEnvironment(),
            "hepatic_sinusoid" => new HepaticEnvironment(),
            "gastric_lumen" => new GastricEnvironment(),
            "blood_brain_barrier" => new BloodBrainBarrierEnvironment(),
            _ => new AcuteWoundEnvironment()
        };
    }
}
