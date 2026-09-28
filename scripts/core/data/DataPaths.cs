using System;

namespace Game.Core;

/// <summary>
/// Centralized path constants for all data files under res://assets/data/.
/// Single source of truth — update here if folder structure changes.
/// </summary>
public static class DataPaths
{
    private const string Root = "res://assets/data";

    private const string SkillRoot = $"{Root}/skill";
    public const string ActiveSkills = $"{SkillRoot}/active.json";
    public const string PassiveSkills = $"{SkillRoot}/passive.json";
    public const string Equipment = $"{Root}/equipment.json";
    public const string Enemies = $"{Root}/enemy_codex.json";
    public const string EnemyDefs = $"{Root}/enemies.json";
    public const string Bosses = $"{Root}/boss_codex.json";
    public const string Stages = $"{Root}/stages.json";
    public const string Classes = $"{Root}/player_classes.json";
    public const string Achievements = $"{Root}/achievements.json";
    public const string PassiveTree = $"{Root}/passive_tree.json";
    public const string PassiveTraits = $"{Root}/passive_traits.json";
    public const string StatLabels = $"{Root}/stat_labels.json";
    public const string Ailments = $"{Root}/ailments.json";

    public const string TranslationsCsv = "res://assets/translations/translations.csv";

    public const string AudioManifest = "res://assets/audio/manifest.json";
}

