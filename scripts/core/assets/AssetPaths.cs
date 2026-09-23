using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Centralized runtime asset paths. Single source of truth — update here if
/// folder structure changes. Ids equal gen/ file stems (no prefixes);
/// paths are derived as res://assets/gen/&lt;category&gt;/{id}.png (Vistrace-style).
/// </summary>
public static class AssetPaths
{
    private const string GenRoot = "res://assets/gen";

    /// <summary>Achievement artwork: res://assets/gen/achievement/{id}.png.</summary>
    public static string AchievementSprite(string achId) => $"{GenRoot}/achievement/{achId}.png";

    /// <summary>Achievement locked variant: res://assets/gen/achievement/{id}_unachieved.png.</summary>
    public static string AchievementSpriteUnachieved(string achId) => $"{GenRoot}/achievement/{achId}_unachieved.png";

    /// <summary>Skill icon: res://assets/gen/skill/{id}.png.</summary>
    public static string SkillIcon(string skillId) => $"{GenRoot}/skill/{skillId}.png";

    /// <summary>Passive-tree trait icon: res://assets/gen/passive_tree/{id}.png.</summary>
    public static string TraitIcon(string traitId) => $"{GenRoot}/passive_tree/{traitId}.png";

    /// <summary>Organelle chamber equipment icon: res://assets/gen/organelle/{id}.png.</summary>
    public static string OrganelleIcon(string organelleId) => $"{GenRoot}/organelle/{organelleId}.png";

    /// <summary>UI icon: res://assets/gen/ui/{id}.png.</summary>
    public static string UiIcon(string uiName) => $"{GenRoot}/ui/{uiName}.png";

    /// <summary>Placeholder texture shown when art is missing (no emoji fallback).</summary>
    public static string PlaceholderIcon => $"{GenRoot}/ui/frame_organelle.png";

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
        yield return $"res://assets/audio/sfx/event/{soundName}.mp3";
        yield return $"res://assets/audio/sfx/event/{soundName}.wav";
        yield return $"res://assets/audio/sfx/gem/{soundName}.mp3";
        yield return $"res://assets/audio/sfx/gem/{soundName}.wav";
    }
}
