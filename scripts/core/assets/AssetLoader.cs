using Godot;
using System;
using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Unified runtime asset pipeline (vistrace DataLoader-style): every Resource
/// load in game and test code goes through here. Central case-insensitive
/// cache, fail-fast <see cref="Load{T}"/>, nullable <see cref="TryLoad{T}"/>
/// for legitimately-missing content, injectable provider for tests.
/// </summary>
public static class AssetLoader
{
    private static IAssetProvider _provider = new GodotAssetProvider();
    private static readonly Dictionary<string, Resource> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Replace the backend (tests). Clears the cache.</summary>
    public static void SetProvider(IAssetProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _cache.Clear();
    }

    /// <summary>Restore the production Godot backend. Clears the cache.</summary>
    public static void ResetProvider()
    {
        _provider = new GodotAssetProvider();
        _cache.Clear();
    }

    /// <summary>Drop all cached resources (test isolation).</summary>
    public static void Clear() => _cache.Clear();

    public static bool Exists(string resPath)
    {
        if (string.IsNullOrEmpty(resPath))
            return false;
        if (_cache.ContainsKey(resPath))
            return true;
        return _provider.Exists(resPath);
    }

    /// <summary>Load a required asset. Throws <see cref="AssetLoadException"/> when missing.</summary>
    public static T Load<T>(string resPath) where T : Resource
    {
        if (_cache.TryGetValue(resPath, out var cached))
        {
            if (cached is T typed)
                return typed;
            throw new AssetLoadException(resPath, $"Cached resource is {cached.GetType().Name}, not {typeof(T).Name}.");
        }

        T? loaded;
        try
        {
            loaded = _provider.Load<T>(resPath);
        }
        catch (AssetLoadException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AssetLoadException(resPath, $"Provider failed: {ex.Message}", ex);
        }

        if (loaded == null)
            throw new AssetLoadException(resPath, "File not found or failed to load.");
        _cache[resPath] = loaded;
        return loaded;
    }

    /// <summary>Load an optional asset. Returns null when missing; never throws.</summary>
    public static T? TryLoad<T>(string resPath) where T : Resource
    {
        if (string.IsNullOrEmpty(resPath))
            return null;
        if (_cache.TryGetValue(resPath, out var cached))
            return cached as T;
        T? loaded;
        try
        {
            loaded = _provider.Load<T>(resPath);
        }
        catch
        {
            return null;
        }
        if (loaded != null)
            _cache[resPath] = loaded;
        return loaded;
    }

    /// <summary>Load the first existing candidate. Throws listing every candidate when all miss.</summary>
    public static T LoadFirst<T>(IEnumerable<string> candidates) where T : Resource
    {
        var tried = new List<string>();
        foreach (string path in candidates)
        {
            tried.Add(path);
            T? hit = TryLoad<T>(path);
            if (hit != null)
                return hit;
        }
        throw new AssetLoadException(string.Join(", ", tried), "None of the candidate paths exist.");
    }

    /// <summary>Load the first existing candidate, or null when all miss. Never throws.</summary>
    public static T? TryLoadFirst<T>(IEnumerable<string> candidates) where T : Resource
    {
        foreach (string path in candidates)
        {
            T? hit = TryLoad<T>(path);
            if (hit != null)
                return hit;
        }
        return null;
    }

    /// <summary>Best-effort warmup: loads what exists, silently skips what does not.</summary>
    public static void Preload<T>(IEnumerable<string> resPaths) where T : Resource
    {
        foreach (string path in resPaths)
            TryLoad<T>(path);
    }

    /// <inheritdoc cref="Preload{T}(IEnumerable{string})"/>
    public static void Preload<T>(params string[] resPaths) where T : Resource => Preload<T>((IEnumerable<string>)resPaths);
}
