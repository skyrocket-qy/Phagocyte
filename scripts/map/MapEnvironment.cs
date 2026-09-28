using Godot;
using Phagocyte.Directors;
using Phagocyte.Hero;

namespace Phagocyte.Map;

/// <summary>
/// Per-organ physiological environment (docs/map.md §3 / TODO module 3).
/// One instance is created by Main per run; <see cref="Process"/>
/// spawns/drives the organ's environmental props.
/// </summary>
public abstract class MapEnvironment
{
    public abstract string MapId { get; }

    /// <summary>Hard (Acute Crisis) variant: exclusive organ hazards go permanent.</summary>
    public bool HardMode { get; set; }

    protected RandomNumberGenerator Rng { get; } = new();

    public virtual void Attach(IRunContext context)
    {
        Rng.Seed = (ulong)MapId.GetHashCode() ^ 0x51F15EEDUL;
    }

    public void Tick(IRunContext context, float dt)
    {
        Process(context, dt);
    }

    protected abstract void Process(IRunContext context, float dt);

    protected static BaseCell? Cell(IRunContext context)
    {
        return context.Player as BaseCell;
    }

    protected static Node2D? Container(IRunContext context)
    {
        return context.EnemyContainer;
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
