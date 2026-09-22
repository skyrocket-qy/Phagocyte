using System;

namespace Phagocyte.Core;

/// <summary>
/// Thrown when a required runtime asset is missing or fails to load.
/// Policy: always throw — a game that cannot load its assets cannot run,
/// and tests fail fast in dev. Callers for legitimately-missing content
/// (unshipped gallery art) must use <see cref="AssetLoader.TryLoad{T}"/>.
/// </summary>
public sealed class AssetLoadException : Exception
{
    public string ResPath { get; }

    public AssetLoadException(string resPath, string message, Exception? inner = null)
        : base($"[AssetLoader] {resPath}: {message}", inner)
    {
        ResPath = resPath;
    }
}
