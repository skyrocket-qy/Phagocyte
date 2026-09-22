using Godot;

namespace Phagocyte.Core;

/// <summary>
/// Production <see cref="IAssetProvider"/> backed by the Godot resource system.
/// Sole place allowed to touch <c>ResourceLoader</c> / <c>GD.Load</c> directly.
/// </summary>
public sealed class GodotAssetProvider : IAssetProvider
{
    public bool Exists(string resPath)
    {
        if (string.IsNullOrEmpty(resPath))
            return false;
        return ResourceLoader.Exists(resPath);
    }

    public T? Load<T>(string resPath) where T : Resource
    {
        if (!Exists(resPath))
            return null;
        return ResourceLoader.Load<T>(resPath);
    }
}
