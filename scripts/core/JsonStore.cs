using Godot;
using Godot.Collections;

namespace Phagocyte.Core;

/// <summary>
/// Shared JSON persistence for manager singletons. Resolves the per-file save
/// path (user:// with a res://.user_data fallback when the user directory is
/// unwritable) and centralizes the load / save / delete scaffolding.
/// </summary>
public static class JsonStore
{
    /// <summary>
    /// Backing store for a manager's <c>SavePath</c> property: resolves the
    /// default path lazily and allows tests to override it.
    /// </summary>
    public sealed class SavePathSlot
    {
        private readonly string _fileName;
        private string _override = "";

        public SavePathSlot(string fileName)
        {
            _fileName = fileName;
        }

        public string Value
        {
            get => string.IsNullOrEmpty(_override) ? JsonStore.ResolvePath(_fileName) : _override;
            set => _override = value;
        }
    }

    private static bool? _userWritable;
    private static readonly System.Collections.Generic.Dictionary<string, string> _resolvedPaths = new();

    /// <summary>
    /// Resolves the save path for a file name, probing user:// writability once
    /// per process and falling back to res://.user_data when needed.
    /// </summary>
    public static string ResolvePath(string fileName)
    {
        if (_resolvedPaths.TryGetValue(fileName, out var cached))
            return cached;

        if (_userWritable == null)
        {
            using var probe = FileAccess.Open("user://.probe", FileAccess.ModeFlags.Write);
            _userWritable = probe != null;
            if (probe != null)
            {
                probe.Close();
                DirAccess.RemoveAbsolute("user://.probe");
            }
        }

        string path;
        if (_userWritable.Value)
        {
            path = $"user://{fileName}";
        }
        else
        {
            path = $"res://.user_data/{fileName}";
            DirAccess.MakeDirRecursiveAbsolute("res://.user_data");
        }

        _resolvedPaths[fileName] = path;
        return path;
    }

    /// <summary>
    /// Writes a payload as pretty-printed JSON. When the requested user:// path
    /// cannot be opened, retries under res://.user_data. Returns the path that
    /// was actually written so callers can persist a fallback.
    /// </summary>
    public static string Write(string path, Variant payload)
    {
        var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null && path.StartsWith("user://"))
        {
            path = $"res://.user_data/{path.Substring("user://".Length)}";
            DirAccess.MakeDirRecursiveAbsolute("res://.user_data");
            file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        }

        if (file != null)
        {
            using (file)
            {
                file.StoreString(Json.Stringify(payload, "\t"));
            }
        }

        return path;
    }

    /// <summary>
    /// Loads a JSON dictionary, or null when the file is missing or malformed.
    /// </summary>
    public static Dictionary? Read(string path)
    {
        if (!FileAccess.FileExists(path))
        {
            if (path.StartsWith("user://"))
            {
                string fallback = $"res://.user_data/{path.Substring("user://".Length)}";
                if (FileAccess.FileExists(fallback))
                    path = fallback;
                else
                    return null;
            }
            else
            {
                return null;
            }
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
            return null;

        var json = new Json();
        if (json.Parse(file.GetAsText()) != Error.Ok || json.Data.VariantType != Variant.Type.Dictionary)
            return null;

        return json.Data.AsGodotDictionary();
    }

    /// <summary>Deletes a save file if it exists.</summary>
    public static void Delete(string path)
    {
        if (FileAccess.FileExists(path))
            DirAccess.RemoveAbsolute(path);
    }
}
