using Godot;
using Phagocyte.Enemies;

namespace Phagocyte.Combat;

/// <summary>
/// Shared pathogen targeting queries for skills, projectiles and hazards.
/// Iterates the active-enemy registry instead of issuing a scene-tree group
/// query, so call sites do not repeat the filter boilerplate.
/// </summary>
public static class TargetingService
{
    /// <summary>True when the node is a live, valid Node2D.</summary>
    public static bool IsValidTarget(Node? node)
    {
        return node is Node2D n && GodotObject.IsInstanceValid(n);
    }

    /// <summary>
    /// True when the node is a valid, living pathogen that accepts damage.
    /// Used by organelles that scan for contact targets.
    /// </summary>
    public static bool IsAttackable(Node? node)
    {
        if (node is not Node2D n || !GodotObject.IsInstanceValid(n))
            return false;
        if (n is BaseEnemy enemy && enemy.CurrentHealth <= 0.0f)
            return false;
        if (n is IDamageable)
            return true;
        return n.HasMethod("take_damage");
    }

    /// <summary>
    /// Nearest pathogen within <paramref name="maxRange"/> of
    /// <paramref name="origin"/>, or null when none is in range.
    /// </summary>
    public static BaseEnemy? FindNearest(
        Node2D origin,
        float maxRange,
        System.Func<BaseEnemy, bool>? predicate = null)
    {
        BaseEnemy? best = null;
        float bestSq = maxRange * maxRange;
        foreach (var enemy in BaseEnemy.ActiveEnemies)
        {
            if (!IsValidTarget(enemy))
                continue;
            if (predicate != null && !predicate(enemy))
                continue;

            float dSq = origin.GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
            if (dSq < bestSq)
            {
                bestSq = dSq;
                best = enemy;
            }
        }

        return best;
    }

    /// <summary>Direction from origin to the nearest pathogen, or fallback when none.</summary>
    public static Vector2 FindTargetDirection(Node2D origin, float maxRange, Vector2 fallbackDir)
    {
        var nearest = FindNearest(origin, maxRange);
        if (nearest != null)
            return (nearest.GlobalPosition - origin.GlobalPosition).Normalized();

        return fallbackDir;
    }

    /// <summary>
    /// Appends every pathogen within <paramref name="radius"/> of
    /// <paramref name="center"/> into <paramref name="results"/> and returns the count.
    /// </summary>
    public static int CollectInRadius(
        Vector2 center,
        float radius,
        System.Collections.Generic.List<BaseEnemy> results)
    {
        int added = 0;
        float radiusSq = radius * radius;
        foreach (var enemy in BaseEnemy.ActiveEnemies)
        {
            if (!IsValidTarget(enemy))
                continue;

            if (center.DistanceSquaredTo(enemy.GlobalPosition) <= radiusSq)
            {
                results.Add(enemy);
                added++;
            }
        }

        return added;
    }

    /// <summary>
    /// Invokes <paramref name="action"/> for every pathogen within
    /// <paramref name="radius"/> of <paramref name="center"/>.
    /// </summary>
    public static void ForEachInRadius(
        Vector2 center,
        float radius,
        System.Action<BaseEnemy> action)
    {
        float radiusSq = radius * radius;
        foreach (var enemy in BaseEnemy.ActiveEnemies)
        {
            if (!IsValidTarget(enemy))
                continue;

            if (center.DistanceSquaredTo(enemy.GlobalPosition) <= radiusSq)
                action(enemy);
        }
    }

    /// <summary>
    /// True when at least one pathogen lies within <paramref name="radius"/> of
    /// <paramref name="center"/>.
    /// </summary>
    public static bool AnyInRadius(Vector2 center, float radius)
    {
        float radiusSq = radius * radius;
        foreach (var enemy in BaseEnemy.ActiveEnemies)
        {
            if (!IsValidTarget(enemy))
                continue;

            if (center.DistanceSquaredTo(enemy.GlobalPosition) <= radiusSq)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Counts pathogens within <paramref name="radius"/> of <paramref name="center"/>,
    /// optionally restricted by <paramref name="predicate"/>.
    /// </summary>
    public static int CountInRadius(
        Vector2 center,
        float radius,
        System.Func<BaseEnemy, bool>? predicate = null)
    {
        int count = 0;
        float radiusSq = radius * radius;
        foreach (var enemy in BaseEnemy.ActiveEnemies)
        {
            if (!IsValidTarget(enemy))
                continue;
            if (predicate != null && !predicate(enemy))
                continue;

            if (center.DistanceSquaredTo(enemy.GlobalPosition) <= radiusSq)
                count++;
        }

        return count;
    }
}
