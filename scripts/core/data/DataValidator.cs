using Godot;
using Godot.Collections;
using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Validates cross-references between all data catalogs after loading.
/// Collects ALL errors before reporting (does not fail fast on the first).
/// Missing translation keys are warnings (p3_3 owns them); broken references
/// throw <see cref="DataLoadException"/>.
/// </summary>
public static class DataValidator
{
    private static bool _validated = false;

    public static void EnsureValidated()
    {
        if (_validated)
            return;
        _validated = true;
        Validate();
    }

    private static void Validate()
    {
        var errors = new List<string>();

        // Skills → classes.
        foreach (string skillId in GameManager.SkillCatalog.Keys)
        {
            var entry = GameManager.SkillCatalog[skillId].AsGodotDictionary();
            string classId = CatalogLoader.GetString(entry, "class_id");
            if (!string.IsNullOrEmpty(classId) && !GameManager.ClassData.ContainsKey(classId))
                errors.Add($"Skill '{skillId}' references class '{classId}' missing from classes.json.");
        }

        // Classes → achievements + scenes.
        foreach (string classId in GameManager.ClassData.Keys)
        {
            var entry = GameManager.ClassData[classId].AsGodotDictionary();
            string ach = CatalogLoader.GetString(entry, "unlock_achievement");
            if (!string.IsNullOrEmpty(ach) && !AchievementManager.Achievements.ContainsKey(ach))
                errors.Add($"Class '{classId}' references achievement '{ach}' missing from achievements.json.");
            string scene = CatalogLoader.GetString(entry, "scene_path");
            if (!string.IsNullOrEmpty(scene) && !ResourceLoader.Exists(scene))
                errors.Add($"Class '{classId}' references scene '{scene}' which does not exist.");
        }

        // Achievements → classes + maps.
        foreach (string achId in AchievementManager.Achievements.Keys)
        {
            var entry = AchievementManager.Achievements[achId].AsGodotDictionary();
            string rewardCell = CatalogLoader.GetString(entry, "reward_cell");
            if (!string.IsNullOrEmpty(rewardCell) && !GameManager.ClassData.ContainsKey(rewardCell))
                errors.Add($"Achievement '{achId}' rewards class '{rewardCell}' missing from classes.json.");
            foreach (string mapKey in new[] { "map_id", "unlock_map", "unlock_hard_map" })
            {
                string mapId = CatalogLoader.GetString(entry, mapKey);
                if (!string.IsNullOrEmpty(mapId) && !GameManager.MapData.ContainsKey(mapId))
                    errors.Add($"Achievement '{achId}' references map '{mapId}' ({mapKey}) missing from maps.json.");
            }
        }

        // Tree → nodes, skills, stats.
        var nodeIds = new HashSet<string>();
        foreach (var node in PassiveTreeManager.Nodes)
            nodeIds.Add(node.Id);
        foreach (var edge in PassiveTreeManager.Edges)
        {
            if (!nodeIds.Contains(edge.From))
                errors.Add($"Tree edge references unknown node '{edge.From}'.");
            if (!nodeIds.Contains(edge.To))
                errors.Add($"Tree edge references unknown node '{edge.To}'.");
        }
        foreach (var kv in PassiveTreeManager.StartNodes)
        {
            if (!nodeIds.Contains(kv.Value))
                errors.Add($"Start node for '{kv.Key}' references unknown node '{kv.Value}'.");
        }
        foreach (var node in PassiveTreeManager.Nodes)
        {
            foreach (var mod in node.Modifiers)
            {
                if (!PassiveTreeManager.HasStatLabel(mod.Stat))
                    errors.Add($"Tree node '{node.Id}' modifies unknown stat '{mod.Stat}'.");
            }
        }

        if (errors.Count > 0)
            throw new DataLoadException("DataValidator", $"{errors.Count} error(s):\n  • {string.Join("\n  • ", errors)}");

        WarnMissingTranslationKeys();
        GD.Print("[Catalog] All cross-references validated OK.");
    }

    /// <summary>Missing Tr keys are p3_3 work — warn, don't fail.</summary>
    private static void WarnMissingTranslationKeys()
    {
        var known = LoadTranslationKeys();
        if (known.Count == 0)
            return;

        var missing = new HashSet<string>();
        CheckKeys(GameManager.SkillCatalog, missing, known, "skill");
        CheckKeys(GameManager.PathogenCatalog, missing, known, "pathogen");
        CheckKeys(GameManager.MapData, missing, known, "map");
        CheckKeys(GameManager.ClassData, missing, known, "class");
        CheckKeys(AchievementManager.Achievements, missing, known, "achievement");
        foreach (var node in PassiveTreeManager.Nodes)
        {
            CheckKey(node.NameKey, missing, known);
            CheckKey(node.DescKey, missing, known);
        }
        foreach (var kv in PassiveTreeManager.StatLabelValues)
            CheckKey(kv.Value, missing, known);

        foreach (string key in missing)
            GD.PushWarning($"[Catalog] Translation key '{key}' has no entry in translations.csv (p3_3).");
    }

    private static void CheckKeys(Dictionary table, HashSet<string> missing, HashSet<string> known, string kind)
    {
        foreach (string id in table.Keys)
        {
            var entry = table[id].AsGodotDictionary();
            foreach (string k in entry.Keys)
            {
                if (!k.EndsWith("_key") || k == "stat_key")
                    continue;
                CheckKey(entry[k].AsString(), missing, known);
            }
        }
    }

    private static void CheckKey(string key, HashSet<string> missing, HashSet<string> known)
    {
        if (!string.IsNullOrEmpty(key) && !known.Contains(key))
            missing.Add(key);
    }

    private static HashSet<string> LoadTranslationKeys()
    {
        var keys = new HashSet<string>();
        using var file = FileAccess.Open(DataPaths.TranslationsCsv, FileAccess.ModeFlags.Read);
        if (file == null)
            return keys;
        bool first = true;
        while (!file.EofReached())
        {
            string line = file.GetLine();
            if (first)
            {
                first = false;
                continue;
            }
            int comma = line.IndexOf(',');
            if (comma > 0)
                keys.Add(line.Substring(0, comma).Trim().Trim('"'));
        }
        return keys;
    }
}
