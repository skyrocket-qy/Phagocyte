using Godot;
using Godot.Collections;

namespace Phagocyte.Core;

/// <summary>
/// Unified JSON catalog pipeline (Phase 3, vistrace-style): one tolerant
/// loading path for every data file under res://assets/data/.
/// JSON conventions: missing fields fall back to schema defaults; colors are
/// "#rrggbbaa" hex strings; Vector2 are [x, y] arrays; scene/skill references
/// are res:// paths / skill ids resolved at load time. Any failure throws
/// <see cref="DataLoadException"/> (fail fast, no code fallback).
/// </summary>
public static class CatalogLoader
{
    /// <summary>Load a JSON array file into its raw entry dictionaries.</summary>
    public static Array<Dictionary> LoadArray(string resPath)
    {
        using var file = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
        if (file == null)
            throw new DataLoadException(resPath, "File not found or unreadable.");

        string json = file.GetAsText();
        var parsed = Json.ParseString(json);
        if (parsed.VariantType != Variant.Type.Array)
            throw new DataLoadException(resPath, $"Expected a JSON array at top level, got {parsed.VariantType}.");

        var entries = new Array<Dictionary>();
        foreach (var item in parsed.AsGodotArray())
        {
            if (item.VariantType != Variant.Type.Dictionary)
                throw new DataLoadException(resPath, "Array entries must be JSON objects.");
            entries.Add(item.AsGodotDictionary());
        }
        return entries;
    }

    /// <summary>Load a JSON object file into its raw dictionary.</summary>
    public static Dictionary LoadObject(string resPath)
    {
        using var file = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
        if (file == null)
            throw new DataLoadException(resPath, "File not found or unreadable.");

        string json = file.GetAsText();
        var parsed = Json.ParseString(json);
        if (parsed.VariantType != Variant.Type.Dictionary)
            throw new DataLoadException(resPath, $"Expected a JSON object at top level, got {parsed.VariantType}.");
        return parsed.AsGodotDictionary();
    }

    public static string GetString(Dictionary d, string key, string def = "")
    {
        if (d.TryGetValue(key, out var v) && v.VariantType == Variant.Type.String)
            return v.AsString();
        return def;
    }

    public static bool GetBool(Dictionary d, string key, bool def = false)
    {
        if (d.TryGetValue(key, out var v) && v.VariantType == Variant.Type.Bool)
            return v.AsBool();
        return def;
    }

    public static int GetInt(Dictionary d, string key, int def = 0)
    {
        if (!d.TryGetValue(key, out var v))
            return def;
        return v.VariantType switch
        {
            Variant.Type.Int => (int)v.AsInt64(),
            Variant.Type.Float => Mathf.RoundToInt(v.AsSingle()),
            _ => def
        };
    }

    public static float GetFloat(Dictionary d, string key, float def = 0.0f)
    {
        if (!d.TryGetValue(key, out var v))
            return def;
        return v.VariantType switch
        {
            Variant.Type.Int => (float)v.AsInt64(),
            Variant.Type.Float => v.AsSingle(),
            _ => def
        };
    }

    /// <summary>Converts "#rrggbbaa" / "#rrggbb" hex strings to Color.</summary>
    public static Color GetColor(Dictionary d, string key, Color def)
    {
        if (d.TryGetValue(key, out var v) && v.VariantType == Variant.Type.String)
        {
            string hex = v.AsString().TrimStart('#');
            if (hex.Length == 6 || hex.Length == 8)
                return new Color(hex);
        }
        return def;
    }

    /// <summary>Converts [x, y] arrays to Vector2.</summary>
    public static Vector2 GetVector2(Dictionary d, string key, Vector2 def)
    {
        if (d.TryGetValue(key, out var v) && v.VariantType == Variant.Type.Array)
        {
            var a = v.AsGodotArray();
            if (a.Count >= 2)
                return new Vector2(ToFloat(a[0]), ToFloat(a[1]));
        }
        return def;
    }

    public static float ToFloat(Variant v)
    {
        return v.VariantType switch
        {
            Variant.Type.Int => (float)v.AsInt64(),
            Variant.Type.Float => v.AsSingle(),
            _ => 0.0f
        };
    }

    public static string[] GetStringArray(Dictionary d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v.VariantType != Variant.Type.Array)
            return System.Array.Empty<string>();
        var arr = v.AsGodotArray();
        var result = new string[arr.Count];
        for (int i = 0; i < arr.Count; i++)
            result[i] = arr[i].AsString();
        return result;
    }

    public static float[] GetFloatArray(Dictionary d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v.VariantType != Variant.Type.Array)
            return System.Array.Empty<float>();
        var arr = v.AsGodotArray();
        var result = new float[arr.Count];
        for (int i = 0; i < arr.Count; i++)
            result[i] = ToFloat(arr[i]);
        return result;
    }
}
