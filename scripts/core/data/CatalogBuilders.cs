using Godot;
using Godot.Collections;
using System.Collections.Generic;
using static Phagocyte.Core.PassiveTreeManager;

namespace Phagocyte.Core;

/// <summary>
/// Per-catalog assembly: JSON rows → runtime dictionaries with schema defaults
/// and type restoration (int / Color / Vector2). Duplicate ids throw
/// <see cref="DataLoadException"/>. Shapes match the former C# literals exactly
/// so every consumer and test keeps working unchanged.
/// </summary>
public static class CatalogBuilders
{
    public static Dictionary BuildSkills()
    {
        var table = new Dictionary();
        var seen = new HashSet<string>();
        foreach (var row in CatalogLoader.LoadArray(DataPaths.Skills))
        {
            string id = CatalogLoader.GetString(row, "id");
            if (string.IsNullOrEmpty(id) || !seen.Add(id))
                throw new DataLoadException(DataPaths.Skills, $"Duplicate or missing skill id '{id}'.");
            table[id] = new Dictionary
            {
                { "id", id },
                { "name_key", CatalogLoader.GetString(row, "name_key") },
                { "desc_key", CatalogLoader.GetString(row, "desc_key") },
                { "bio_key", CatalogLoader.GetString(row, "bio_key") },
                { "icon", CatalogLoader.GetString(row, "icon") },
                { "type", CatalogLoader.GetString(row, "type") },
                { "class_id", CatalogLoader.GetString(row, "class_id") },
                { "cooldown", CatalogLoader.GetFloat(row, "cooldown") },
                { "max_level", CatalogLoader.GetInt(row, "max_level", 5) }
            };
        }
        GD.Print($"[Catalog] Loaded {table.Count} skills.");
        return table;
    }

    public static Dictionary BuildPathogens()
    {
        var table = new Dictionary();
        var seen = new HashSet<string>();
        foreach (var row in CatalogLoader.LoadArray(DataPaths.Pathogens))
        {
            string id = CatalogLoader.GetString(row, "id");
            if (string.IsNullOrEmpty(id) || !seen.Add(id))
                throw new DataLoadException(DataPaths.Pathogens, $"Duplicate or missing pathogen id '{id}'.");
            table[id] = new Dictionary
            {
                { "id", id },
                { "name_key", CatalogLoader.GetString(row, "name_key") },
                { "desc_key", CatalogLoader.GetString(row, "desc_key") },
                { "trait_key", CatalogLoader.GetString(row, "trait_key") },
                { "icon", CatalogLoader.GetString(row, "icon") },
                { "danger_level", CatalogLoader.GetString(row, "danger_level") }
            };
        }
        GD.Print($"[Catalog] Loaded {table.Count} pathogens.");
        return table;
    }

    public static Dictionary BuildMaps()
    {
        var table = new Dictionary();
        var seen = new HashSet<string>();
        foreach (var row in CatalogLoader.LoadArray(DataPaths.Maps))
        {
            string id = CatalogLoader.GetString(row, "id");
            if (string.IsNullOrEmpty(id) || !seen.Add(id))
                throw new DataLoadException(DataPaths.Maps, $"Duplicate or missing map id '{id}'.");
            table[id] = new Dictionary
            {
                { "id", id },
                { "organ_key", CatalogLoader.GetString(row, "organ_key") },
                { "organ_icon", CatalogLoader.GetString(row, "organ_icon") },
                { "name_key", CatalogLoader.GetString(row, "name_key") },
                { "subtitle_key", CatalogLoader.GetString(row, "subtitle_key") },
                { "env_key", CatalogLoader.GetString(row, "env_key") },
                { "mech_key", CatalogLoader.GetString(row, "mech_key") },
                { "threat_key", CatalogLoader.GetString(row, "threat_key") },
                { "difficulty", CatalogLoader.GetInt(row, "difficulty", 1) },
                { "color_code", CatalogLoader.GetColor(row, "color_code", new Color(1, 1, 1, 1)) },
                { "scanner_pos", CatalogLoader.GetVector2(row, "scanner_pos", Vector2.Zero) },
                { "bg_color", CatalogLoader.GetColor(row, "bg_color", new Color(0.05f, 0.08f, 0.12f, 1.0f)) },
                { "bg_color_deep", CatalogLoader.GetColor(row, "bg_color_deep", new Color(0.04f, 0.05f, 0.09f, 1.0f)) },
                { "bg_color_accent", CatalogLoader.GetColor(row, "bg_color_accent", new Color(0.14f, 0.04f, 0.08f, 1.0f)) },
                { "fiber_color", CatalogLoader.GetColor(row, "fiber_color", new Color(0.22f, 0.18f, 0.32f, 0.35f)) },
                { "unlocked", CatalogLoader.GetBool(row, "unlocked") },
                { "hard_unlocked", CatalogLoader.GetBool(row, "hard_unlocked") }
            };
        }
        GD.Print($"[Catalog] Loaded {table.Count} maps.");
        return table;
    }

    public static Dictionary BuildClasses()
    {
        var table = new Dictionary();
        var seen = new HashSet<string>();
        foreach (var row in CatalogLoader.LoadArray(DataPaths.Classes))
        {
            string id = CatalogLoader.GetString(row, "id");
            if (string.IsNullOrEmpty(id) || !seen.Add(id))
                throw new DataLoadException(DataPaths.Classes, $"Duplicate or missing class id '{id}'.");
            table[id] = new Dictionary
            {
                { "name_key", CatalogLoader.GetString(row, "name_key") },
                { "role_key", CatalogLoader.GetString(row, "role_key") },
                { "trait_key", CatalogLoader.GetString(row, "trait_key") },
                { "bio_key", CatalogLoader.GetString(row, "bio_key") },
                { "unlocked", CatalogLoader.GetBool(row, "unlocked") },
                { "unlock_achievement", CatalogLoader.GetString(row, "unlock_achievement") },
                { "scene_path", CatalogLoader.GetString(row, "scene_path") },
                { "base_hp", CatalogLoader.GetFloat(row, "base_hp", 100.0f) },
                { "base_speed", CatalogLoader.GetFloat(row, "base_speed", 230.0f) },
                { "base_armor", CatalogLoader.GetFloat(row, "base_armor", 0.0f) },
                { "trait_stat", CatalogLoader.GetString(row, "trait_stat") },
                { "trait_stat_value", CatalogLoader.GetFloat(row, "trait_stat_value", 0.0f) }
            };
        }
        GD.Print($"[Catalog] Loaded {table.Count} classes.");
        return table;
    }

    public static Dictionary BuildAchievements()
    {
        var table = new Dictionary();
        var seen = new HashSet<string>();
        foreach (var row in CatalogLoader.LoadArray(DataPaths.Achievements))
        {
            string id = CatalogLoader.GetString(row, "id");
            if (string.IsNullOrEmpty(id) || !seen.Add(id))
                throw new DataLoadException(DataPaths.Achievements, $"Duplicate or missing achievement id '{id}'.");
            var entry = new Dictionary
            {
                { "id", id },
                { "title_key", CatalogLoader.GetString(row, "title_key") },
                { "desc_key", CatalogLoader.GetString(row, "desc_key") },
                { "reward_key", CatalogLoader.GetString(row, "reward_key") },
                { "reward_cell", CatalogLoader.GetString(row, "reward_cell") },
                { "icon", CatalogLoader.GetString(row, "icon") },
                { "target_value", CatalogLoader.GetFloat(row, "target_value", 1.0f) },
                { "stat_key", CatalogLoader.GetString(row, "stat_key") }
            };
            // Map-clear chain extras (absent on generic achievements).
            foreach (string opt in new[] { "map_id", "difficulty", "unlock_map", "unlock_hard_map" })
            {
                string v = CatalogLoader.GetString(row, opt);
                if (!string.IsNullOrEmpty(v))
                    entry[opt] = v;
            }
            if (row.ContainsKey("talent_points"))
                entry["talent_points"] = CatalogLoader.GetInt(row, "talent_points");
            if (row.ContainsKey("unlock_endless"))
                entry["unlock_endless"] = CatalogLoader.GetBool(row, "unlock_endless");
            table[id] = entry;
        }
        GD.Print($"[Catalog] Loaded {table.Count} achievements.");
        return table;
    }

    public static void BuildTree(out TreeNode[] nodes, out (string From, string To)[] edges,
        out System.Collections.Generic.Dictionary<string, string> starts,
        out System.Collections.Generic.Dictionary<string, Vector2> regions,
        out System.Collections.Generic.Dictionary<string, TreeTrait> traits)
    {
        traits = BuildTreeTraits();

        var root = CatalogLoader.LoadObject(DataPaths.PassiveTree);
        if (!root.TryGetValue("nodes", out var nodesVar) || nodesVar.VariantType != Variant.Type.Array)
            throw new DataLoadException(DataPaths.PassiveTree, "Missing 'nodes' array.");

        regions = new System.Collections.Generic.Dictionary<string, Vector2>();
        if (root.TryGetValue("regions", out var regionsVar) && regionsVar.VariantType == Variant.Type.Array)
        {
            foreach (var item in regionsVar.AsGodotArray())
            {
                var regionRow = item.AsGodotDictionary();
                string branch = CatalogLoader.GetString(regionRow, "branch");
                if (string.IsNullOrEmpty(branch) || regions.ContainsKey(branch))
                    throw new DataLoadException(DataPaths.PassiveTree, $"Duplicate or missing region branch '{branch}'.");
                regions[branch] = GridPosition(CatalogLoader.GetInt(regionRow, "col"), CatalogLoader.GetInt(regionRow, "row"));
            }
        }
        if (regions.Count == 0)
            throw new DataLoadException(DataPaths.PassiveTree, "Missing 'regions' array (region squares are data-owned).");

        var nodeList = new System.Collections.Generic.List<TreeNode>();
        var branchCounts = new System.Collections.Generic.Dictionary<string, int>();
        var traitUseCounts = new System.Collections.Generic.Dictionary<string, int>();
        var seen = new HashSet<string>();
        foreach (var item in nodesVar.AsGodotArray())
        {
            var row = item.AsGodotDictionary();
            string id = CatalogLoader.GetString(row, "id");
            if (string.IsNullOrEmpty(id) || !seen.Add(id))
                throw new DataLoadException(DataPaths.PassiveTree, $"Duplicate or missing node id '{id}'.");
            string branch = CatalogLoader.GetString(row, "branch");
            int col = CatalogLoader.GetInt(row, "col");
            int rowIdx = CatalogLoader.GetInt(row, "row");
            if (!regions.TryGetValue(branch, out var regionCenter))
                throw new DataLoadException(DataPaths.PassiveTree, $"Node '{id}' belongs to unknown region '{branch}'.");
            Vector2 nodePosition = GridPosition(col, rowIdx);
            float regionExtent = RegionRadius * GridStep + 0.5f;
            if (Mathf.Abs(nodePosition.X - regionCenter.X) > regionExtent || Mathf.Abs(nodePosition.Y - regionCenter.Y) > regionExtent)
                throw new DataLoadException(DataPaths.PassiveTree, $"Node '{id}' lies outside its '{branch}' region square.");
            branchCounts[branch] = branchCounts.TryGetValue(branch, out int branchCount) ? branchCount + 1 : 1;

            string traitId = CatalogLoader.GetString(row, "trait");
            if (string.IsNullOrEmpty(traitId) || !traits.TryGetValue(traitId, out var trait))
                throw new DataLoadException(DataPaths.PassiveTree, $"Node '{id}' references unknown trait '{traitId}'.");
            traitUseCounts[traitId] = traitUseCounts.TryGetValue(traitId, out int traitUses) ? traitUses + 1 : 1;

            nodeList.Add(new TreeNode(
                id,
                nodePosition,
                trait.Rarity,
                branch,
                trait.Icon,
                trait.NameKey,
                trait.DescKey,
                CatalogLoader.GetInt(row, "max_stacks", 1),
                CatalogLoader.GetInt(row, "point_cost", 1),
                traitId,
                trait.Modifiers,
                LayerOf(col, rowIdx)));
        }
        nodes = nodeList.ToArray();

        if (!root.TryGetValue("edges", out var edgesVar) || edgesVar.VariantType != Variant.Type.Array)
            throw new DataLoadException(DataPaths.PassiveTree, "Missing 'edges' array.");
        var edgeList = new System.Collections.Generic.List<(string, string)>();
        foreach (var item in edgesVar.AsGodotArray())
        {
            var pair = item.AsGodotArray();
            edgeList.Add((pair[0].AsString(), pair[1].AsString()));
        }
        edges = edgeList.ToArray();

        starts = new System.Collections.Generic.Dictionary<string, string>();
        if (root.TryGetValue("start_nodes", out var startsVar) && startsVar.VariantType == Variant.Type.Dictionary)
        {
            foreach (string k in startsVar.AsGodotDictionary().Keys)
                starts[k] = startsVar.AsGodotDictionary()[k].AsString();
        }

        foreach (var regionPair in regions)
        {
            if (!branchCounts.ContainsKey(regionPair.Key))
                throw new DataLoadException(DataPaths.PassiveTree, $"Region '{regionPair.Key}' has no nodes.");
        }
        foreach (var traitPair in traits)
        {
            if (!traitUseCounts.ContainsKey(traitPair.Key))
                throw new DataLoadException(DataPaths.PassiveTraits, $"Trait '{traitPair.Key}' is never referenced by a node.");
        }
        foreach (var kv in starts)
        {
            if (!TryFindNode(nodes, kv.Value, out var startNode))
                throw new DataLoadException(DataPaths.PassiveTree, $"Start node '{kv.Value}' for '{kv.Key}' is not a tree node.");
            if (!regions.TryGetValue(startNode.Branch, out var regionCenter) || startNode.Position.DistanceTo(regionCenter) > 0.5f)
                throw new DataLoadException(DataPaths.PassiveTree, $"Start node '{kv.Value}' must sit at the centre of its '{startNode.Branch}' region.");
        }
        GD.Print($"[Catalog] Loaded {nodes.Length} tree nodes, {edges.Length} edges, {regions.Count} regions, {traits.Count} traits.");
    }

    private static System.Collections.Generic.Dictionary<string, TreeTrait> BuildTreeTraits()
    {
        var table = new System.Collections.Generic.Dictionary<string, TreeTrait>();
        var root = CatalogLoader.LoadObject(DataPaths.PassiveTraits);
        if (!root.TryGetValue("traits", out var traitsVar) || traitsVar.VariantType != Variant.Type.Array)
            throw new DataLoadException(DataPaths.PassiveTraits, "Missing 'traits' array.");

        foreach (var item in traitsVar.AsGodotArray())
        {
            var row = item.AsGodotDictionary();
            string id = CatalogLoader.GetString(row, "id");
            if (string.IsNullOrEmpty(id) || table.ContainsKey(id))
                throw new DataLoadException(DataPaths.PassiveTraits, $"Duplicate or missing trait id '{id}'.");
            if (!System.Enum.TryParse<TreeRarity>(CatalogLoader.GetString(row, "rarity", "normal"), true, out var rarity))
                throw new DataLoadException(DataPaths.PassiveTraits, $"Trait '{id}' has unknown rarity.");

            var mods = new System.Collections.Generic.List<TreeStatModifier>();
            if (row.TryGetValue("modifiers", out var modsVar) && modsVar.VariantType == Variant.Type.Array)
            {
                foreach (var m in modsVar.AsGodotArray())
                {
                    var md = m.AsGodotDictionary();
                    if (!System.Enum.TryParse<TreeModifierUnit>(CatalogLoader.GetString(md, "unit", "flat"), true, out var unit))
                        throw new DataLoadException(DataPaths.PassiveTraits, $"Trait '{id}' has unknown modifier unit.");
                    mods.Add(new TreeStatModifier(
                        CatalogLoader.GetString(md, "stat"),
                        CatalogLoader.GetFloat(md, "value"),
                        unit));
                }
            }
            if (mods.Count == 0 && rarity != TreeRarity.Start)
                throw new DataLoadException(DataPaths.PassiveTraits, $"Trait '{id}' has no modifiers.");

            table[id] = new TreeTrait(
                id,
                rarity,
                CatalogLoader.GetString(row, "icon", "🧬"),
                CatalogLoader.GetString(row, "name_key"),
                CatalogLoader.GetString(row, "desc_key"),
                mods.ToArray());
        }
        return table;
    }

    private static bool TryFindNode(TreeNode[] nodes, string id, out TreeNode node)
    {
        foreach (var candidate in nodes)
        {
            if (candidate.Id == id)
            {
                node = candidate;
                return true;
            }
        }
        node = default;
        return false;
    }

    public static System.Collections.Generic.Dictionary<string, string> BuildStatLabels()
    {
        var table = new System.Collections.Generic.Dictionary<string, string>();
        var root = CatalogLoader.LoadObject(DataPaths.StatLabels);
        foreach (string k in root.Keys)
            table[k] = root[k].AsString();
        return table;
    }
}
