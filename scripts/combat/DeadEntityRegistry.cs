using System.Collections.Generic;

namespace Game.Combat;

/// <summary>Flat dead-owner registry: enemy ids between Die() and _ExitTree. O(1) hash lookups, zero alloc.</summary>
public static class DeadEntityRegistry
{
    private static readonly HashSet<ulong> _dead = new();

    public static int Count => _dead.Count;

    public static void Add(ulong id)
    {
        if (id != 0)
            _dead.Add(id);
    }

    public static void Remove(ulong id)
    {
        _dead.Remove(id);
    }

    public static bool IsDead(ulong id) => id != 0 && _dead.Contains(id);

    public static void Clear()
    {
        _dead.Clear();
    }
}
