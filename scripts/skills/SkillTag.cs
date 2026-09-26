using Godot;
using System;
using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// The 7 PoE-style skill tags for Project: Phagocyte.
/// Defines the archetype classification and indicates which universal stats scale each skill.
/// </summary>
public static class SkillTag
{
    public const string AOE = "AOE";
    public const string Attack = "Attack";
    public const string Spell = "Spell";
    public const string Projectile = "Projectile";
    public const string Duration = "Duration";
    public const string Melee = "Melee";
    public const string Trap = "Trap";

    public static readonly string[] AllTags = { AOE, Attack, Spell, Projectile, Duration, Melee, Trap };

    /// <summary>
    /// Fluorescent theme colors for UI badges matching bio-microscopic aesthetic.
    /// </summary>
    public static Color GetTagColor(string tag) => tag switch
    {
        Attack => new Color(0.95f, 0.42f, 0.35f),    // Coral Red (Physical Cytoskeletal Strike)
        Spell => new Color(0.35f, 0.78f, 1.0f),      // Cyan / Arcane (Biochemical Secretion)
        AOE => new Color(0.95f, 0.85f, 0.30f),        // Amber Gold (Spatial Footprint)
        Projectile => new Color(0.38f, 0.95f, 0.65f), // Emerald Green (Traveling Entity)
        Duration => new Color(0.78f, 0.55f, 0.95f),   // Orchid Violet (Lingering / Persistent)
        Melee => new Color(0.95f, 0.60f, 0.25f),      // Warm Orange (Contact / Pseudopod Reach)
        Trap => new Color(0.32f, 0.90f, 0.90f),       // Teal Phosphor (Stationary Bio-Mine / Puddle)
        _ => Colors.White
    };

    /// <summary>
    /// Returns the universal stat names that scale skills bearing the given tag.
    /// </summary>
    public static string[] GetInfluencingStats(string tag) => tag switch
    {
        Attack => new[] { "might", "crit_chance", "crit_damage" },
        Spell => new[] { "might", "ailment_damage", "crit_chance", "crit_damage" },
        AOE => new[] { "area" },
        Projectile => new[] { "projectile_speed", "pierce", "amount" },
        Duration => new[] { "duration" },
        Melee => new[] { "area", "knockback" },
        Trap => new[] { "area", "duration" },
        _ => Array.Empty<string>()
    };
}
