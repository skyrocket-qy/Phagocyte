using Godot;
using Game.Enemies;

namespace Game.Combat;

/// <summary>
/// Shared enemy targeting queries for skills, projectiles and hazards.
/// Iterates the active-enemy registry instead of issuing a scene-tree group
/// query, so call sites do not repeat the filter boilerplate.
/// </summary>
public static class TargetingService
{
    public static bool IsValidTarget(Node? node)
    {
        return node is Node2D n && GodotObject.IsInstanceValid(n);
    }

    public static bool IsAttackable(Node? node)
    {
        if (node is not Node2D n || !GodotObject.IsInstanceValid(n))
            return false;
        if (n is EnemyActor enemy && enemy.CurrentHealth <= 0.0f)
            return false;
        return n is IDamageable;
    }

    public static EnemyActor? FindNearest(
        Node2D origin,
        float maxRange,
        System.Func<EnemyActor, bool>? predicate = null)
    {
        EnemyActor? best = null;
        float bestSq = maxRange * maxRange;
        foreach (var enemy in EnemyActor.ActiveEnemies)
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

    public static Vector2 FindTargetDirection(Node2D origin, float maxRange, Vector2 fallbackDir)
    {
        var nearest = FindNearest(origin, maxRange);
        if (nearest != null)
            return (nearest.GlobalPosition - origin.GlobalPosition).Normalized();

        return fallbackDir;
    }

    public static int CollectInRadius(
        Vector2 center,
        float radius,
        System.Collections.Generic.List<EnemyActor> results)
    {
        int added = 0;
        float radiusSq = radius * radius;
        foreach (var enemy in EnemyActor.ActiveEnemies)
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

    public static void ForEachInRadius(
        Vector2 center,
        float radius,
        System.Action<EnemyActor> action)
    {
        float radiusSq = radius * radius;
        foreach (var enemy in EnemyActor.ActiveEnemies)
        {
            if (!IsValidTarget(enemy))
                continue;

            if (center.DistanceSquaredTo(enemy.GlobalPosition) <= radiusSq)
                action(enemy);
        }
    }

    public static bool AnyInRadius(Vector2 center, float radius)
    {
        float radiusSq = radius * radius;
        foreach (var enemy in EnemyActor.ActiveEnemies)
        {
            if (!IsValidTarget(enemy))
                continue;

            if (center.DistanceSquaredTo(enemy.GlobalPosition) <= radiusSq)
                return true;
        }

        return false;
    }
}
