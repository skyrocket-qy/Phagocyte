using System;

namespace Phagocyte.Core;

/// <summary>
/// Centralized path constants for all data files under res://assets/data/.
/// Single source of truth — update here if folder structure changes.
/// </summary>
public static class DataPaths
{
    private const string Root = "res://assets/data";

    public const string Skills = $"{Root}/skills.json";
    public const string Pathogens = $"{Root}/pathogens.json";
    public const string Maps = $"{Root}/maps.json";
    public const string Classes = $"{Root}/classes.json";
    public const string Achievements = $"{Root}/achievements.json";
    public const string PassiveTree = $"{Root}/passive_tree.json";
    public const string PassiveTraits = $"{Root}/passive_traits.json";
    public const string Ui = $"{Root}/ui.json";
    public const string StatLabels = $"{Root}/stat_labels.json";

    public const string TranslationsCsv = "res://assets/translations/translations.csv";
}
