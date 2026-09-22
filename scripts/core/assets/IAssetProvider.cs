using Godot;

namespace Phagocyte.Core;

/// <summary>
/// Abstraction over the engine resource backend. The default
/// <see cref="GodotAssetProvider"/> delegates to <c>ResourceLoader</c>;
/// tests inject fakes via <see cref="AssetLoader.SetProvider"/>.
/// </summary>
public interface IAssetProvider
{
    bool Exists(string resPath);
    T? Load<T>(string resPath) where T : Resource;
}
