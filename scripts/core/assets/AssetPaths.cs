using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Centralized runtime asset paths. Single source of truth — update here if
/// folder structure changes. Source filenames under gen/ carry no category
/// prefix, so ID-to-file mapping strips prefixes here (mirrors the
/// tools/asset_check convention); the Steam-mirrored achievement sprites keep
/// the full id per assets/sprites/achievements/README.md.
/// </summary>
public static class AssetPaths
{
    private const string GenRoot = "res://assets/gen";

    /// <summary>Steam-mirrored achievement artwork. Keeps the full achievement id.</summary>
    public static string AchievementSprite(string achId) => $"res://assets/sprites/achievements/{achId}.png";

    /// <summary>Skill icon. Strips the passive_ ID prefix (gen/skill/actin.png).</summary>
    public static string SkillIcon(string skillId) => $"{GenRoot}/skill/{StripPrefixes(skillId, "passive_")}.png";

    /// <summary>Passive-tree trait icon. Strips trait_/passive_/tree_ (gen/passive_tree/actin.png).</summary>
    public static string TraitIcon(string traitId) => $"{GenRoot}/passive_tree/{StripPrefixes(traitId, "trait_", "passive_", "tree_")}.png";

    /// <summary>UI icon. Strips the ui_ prefix (gen/ui/reticle_target.png).</summary>
    public static string UiIcon(string uiName) => $"{GenRoot}/ui/{StripPrefixes(uiName, "ui_")}.png";

    /// <summary>Candidate files for a BGM track, in probe order.</summary>
    public static IEnumerable<string> BgmCandidates(string trackName)
    {
        yield return $"res://assets/audio/bgm/{trackName}.mp3";
        yield return $"res://assets/audio/bgm/{trackName}.ogg";
        yield return $"res://assets/audio/bgm/{trackName}.wav";
    }

    /// <summary>Candidate files for an SFX name, in probe order.</summary>
    public static IEnumerable<string> SfxCandidates(string soundName)
    {
        yield return $"res://assets/audio/sfx/{soundName}.mp3";
        yield return $"res://assets/audio/sfx/{soundName}.wav";
        yield return $"res://assets/audio/sfx/combat/{soundName}.mp3";
        yield return $"res://assets/audio/sfx/combat/{soundName}.wav";
        yield return $"res://assets/audio/sfx/ui/{soundName}.mp3";
        yield return $"res://assets/audio/sfx/ui/{soundName}.wav";
        yield return $"res://assets/audio/sfx/gem/{soundName}.mp3";
        yield return $"res://assets/audio/sfx/gem/{soundName}.wav";
    }

    private static string StripPrefixes(string name, params string[] prefixes)
    {
        string stripped = name;
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (string prefix in prefixes)
            {
                if (!string.IsNullOrEmpty(prefix) && stripped.StartsWith(prefix))
                {
                    stripped = stripped.Substring(prefix.Length);
                    changed = true;
                    break;
                }
            }
        }
        return stripped;
    }
}
