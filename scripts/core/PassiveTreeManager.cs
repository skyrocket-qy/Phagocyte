using Godot;
using Godot.Collections;
using System;
using System.Text;
using Phagocyte.Skills;

namespace Phagocyte.Core;

/// <summary>
/// Persistent pre-run passive-tree state shared by every immune cell.
/// Each cell begins from its own start node and earns one passive point per meta level.
/// </summary>
public static class PassiveTreeManager
{
    public enum TreeRarity
    {
        Normal,
        Magic,
        Rare,
        Unique
    }

    public enum TreeModifierUnit
    {
        Flat,
        Percent,
        PercentagePoints
    }

    public readonly record struct TreeStatModifier(string Stat, float Value, TreeModifierUnit Unit);

    public readonly record struct TreeNode(
        string Id,
        Vector2 Position,
        TreeRarity Rarity,
        string Branch,
        string Icon,
        string NameKey,
        string DescKey,
        int MaxStacks,
        int PointCost,
        Type? SkillType,
        TreeStatModifier[] Modifiers,
        int Ring);

    public const int BaseCellLevel = 1;

    /// <summary>
    /// The deployed cell's start hub is innately lit and never consumes points
    /// (docs/passivetree.md §2 L=0 / §5.1).
    /// </summary>
    public const int InnateStartStacks = 1;

    public const string NucleusNodeId = "tree_hsc_core";

    public static readonly Vector2 WorldCenter = new(2400.0f, 2400.0f);

    public const float GridStep = 150.0f;

    public static Vector2 GridPosition(int column, int row)
    {
        return WorldCenter + new Vector2(column * GridStep, -row * GridStep);
    }

    public static int LayerOf(int column, int row)
    {
        return Math.Abs(column) + Math.Abs(row);
    }

    public static bool IsNucleus(string nodeId)
    {
        return nodeId == NucleusNodeId;
    }

    private static readonly JsonStore.SavePathSlot _savePath = new("passive_tree.json");
    public static string SavePath
    {
        get => _savePath.Value;
        set => _savePath.Value = value;
    }


    // Tree topology is data-owned (assets/data/passive_tree.json); position math
    // (GridPosition/LayerOf) stays in code. Loaded once and cached.
    private static TreeNode[]? _nodes;
    public static TreeNode[] Nodes
    {
        get
        {
            _nodes ??= LoadTreeNodes();
            DataValidator.EnsureValidated();
            return _nodes;
        }
    }

    public static (string From, string To)[] Edges
    {
        get
        {
            EnsureTreeLoaded();
            DataValidator.EnsureValidated();
            return _loadedEdges;
        }
    }

    public static System.Collections.Generic.Dictionary<string, string> StartNodes
    {
        get
        {
            EnsureTreeLoaded();
            DataValidator.EnsureValidated();
            return _loadedStarts;
        }
    }

    private static System.Collections.Generic.Dictionary<string, string>? _statLabels;
    private static System.Collections.Generic.Dictionary<string, string> StatLabels => _statLabels ??= CatalogBuilders.BuildStatLabels();

    /// <summary>All tree data loads in one pass so Nodes/Edges/Starts stay consistent.</summary>
    private static bool _treeLoaded = false;
    private static (string From, string To)[] _loadedEdges = System.Array.Empty<(string, string)>();
    private static System.Collections.Generic.Dictionary<string, string> _loadedStarts = new();

    private static void EnsureTreeLoaded()
    {
        if (_treeLoaded)
            return;
        _treeLoaded = true;
        CatalogBuilders.BuildTree(out var nodes, out var edges, out var starts);
        _nodes = nodes;
        _loadedEdges = edges;
        _loadedStarts = starts;
    }

    private static TreeNode[] LoadTreeNodes()
    {
        EnsureTreeLoaded();
        return _nodes!;
    }

    /// <summary>skill id (passive_tree.json) → skill Type for legacy loadout nodes.</summary>
    private static readonly System.Collections.Generic.Dictionary<string, System.Type> SkillTypes = new()
    {
        { "passive_opsonin", typeof(PassiveOpsoninAffinity) },
        { "passive_vdj", typeof(PassiveVdjDiversity) },
        { "passive_chemokine", typeof(PassiveChemokineReceptors) },
        { "passive_actin", typeof(PassiveActinPolymerization) },
        { "passive_kinesin", typeof(PassiveKinesinTransit) },
        { "passive_lysosome", typeof(PassiveLysosomePriming) },
        { "passive_bilayer", typeof(PassiveBilayerHardening) },
        { "passive_endotoxin", typeof(PassiveEndotoxinBarrier) },
        { "passive_autophagy", typeof(PassiveAutophagicRecycle) },
        { "passive_mitochondria", typeof(PassiveMitochondrialOverclock) },
        { "passive_glycolysis", typeof(PassiveAerobicGlycolysis) },
        { "passive_hematopoietic", typeof(PassiveHematopoieticReserve) },
        { "passive_longevity", typeof(PassiveCytokineLongevity) }
    };

    public static System.Type? ResolveSkillType(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
            return null;
        return SkillTypes.TryGetValue(skillId, out var t) ? t : null;
    }

    public static bool HasStatLabel(string stat)
    {
        return StatLabels.ContainsKey(stat);
    }

    public static System.Collections.Generic.IReadOnlyDictionary<string, string> StatLabelValues => StatLabels;




    public static string GetRarityName(TreeRarity rarity)
    {
        return rarity switch
        {
            TreeRarity.Magic => TranslationServer.Translate("TREE_RARITY_MAGIC"),
            TreeRarity.Rare => TranslationServer.Translate("TREE_RARITY_RARE"),
            TreeRarity.Unique => TranslationServer.Translate("TREE_RARITY_UNIQUE"),
            _ => TranslationServer.Translate("TREE_RARITY_NORMAL")
        };
    }

    public static string GetStatLabel(string stat)
    {
        return StatLabels.TryGetValue(stat, out var key) ? TranslationServer.Translate(key) : stat;
    }

    public static string FormatTreeEffect(TreeStatModifier effect)
    {
        string magnitude = effect.Unit switch
        {
            TreeModifierUnit.Percent or TreeModifierUnit.PercentagePoints => FormatSignedPercent(effect.Value),
            _ => FormatSignedNumber(effect.Value)
        };
        return TextFormatter.Format(TranslationServer.Translate("TREE_EFFECT_LINE"), magnitude, GetStatLabel(effect.Stat));
    }

    public static string DescribeNodeEffects(TreeNode node)
    {
        if (IsNucleus(node.Id))
            return TranslationServer.Translate("TREE_NUCLEUS_DESC");
        if (!string.IsNullOrEmpty(node.DescKey))
            return TranslationServer.Translate(node.DescKey);

        if (node.Modifiers.Length == 1)
            return FormatTreeEffect(node.Modifiers[0]);

        var text = new StringBuilder();
        text.AppendLine(TranslationServer.Translate("TREE_EFFECTS_PER_STACK"));
        foreach (var effect in node.Modifiers)
            text.AppendLine(FormatTreeEffect(effect));
        return text.ToString().TrimEnd();
    }

    public static string GetNodeIcon(string nodeId)
    {
        return TryGetNode(nodeId, out var node) ? node.Icon : "🧬";
    }

    public static string GetNodeName(string nodeId)
    {
        return TryGetNode(nodeId, out var node) ? TranslationServer.Translate(node.NameKey) : nodeId;
    }

    public static string GetNodeDescription(string nodeId)
    {
        if (!TryGetNode(nodeId, out var node))
            return "";
        if (!string.IsNullOrEmpty(node.DescKey))
            return TranslationServer.Translate(node.DescKey);
        return DescribeNodeEffects(node);
    }

    public static string GetNodeTooltipText(string cellId, string nodeId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId) || !TryGetNode(nodeId, out var node))
            return "";

        int stacks = GetNodeStacks(cellId, nodeId);
        var text = new StringBuilder();
        text.AppendLine("[b]" + node.Icon + " " + TranslationServer.Translate(node.NameKey) + "[/b]");
        if (IsNucleus(nodeId))
            text.AppendLine(TranslationServer.Translate("TREE_NUCLEUS_INNATE"));
        else if (IsInnateStartNode(cellId, nodeId))
            text.AppendLine(TranslationServer.Translate("TREE_START_INNATE"));
        text.AppendLine(GetNodeDescription(nodeId));

        string status = "";
        if (IsInnateStartNode(cellId, nodeId))
            status = ""; // already stated in the header line above
        else if (stacks >= node.MaxStacks)
            status = TranslationServer.Translate(IsNucleus(nodeId) ? "TREE_NUCLEUS_ACTIVE" : "TREE_MAXED");
        else if (CanPurchase(cellId, nodeId))
            status = TranslationServer.Translate("TREE_PURCHASE_HINT");
        else if (GetPointsAvailable(cellId) >= node.PointCost)
            status = TranslationServer.Translate("TREE_LOCKED");

        if (!string.IsNullOrEmpty(status))
        {
            text.AppendLine();
            text.Append(status);
        }
        return text.ToString();
    }

    private static string FormatSignedNumber(float value)
    {
        string sign = value < 0.0f ? "-" : "+";
        float magnitude = Mathf.Abs(value);
        string digits = magnitude >= 100.0f ? magnitude.ToString("F0") : magnitude.ToString("F2").TrimEnd('0').TrimEnd('.');
        return sign + digits;
    }

    private static string FormatSignedPercent(float value)
    {
        return FormatSignedNumber(value * 100.0f) + "%";
    }

    public static Dictionary<string, int> CellLevels = new();
    public static Dictionary<string, Dictionary<string, int>> Allocations = new();

    /// <summary>
    /// Extra talent points awarded by meta progression (achievement map clears, etc.).
    /// Shared across every cell so organ-clear rewards always have a spendable home.
    /// </summary>
    public static int BonusPoints { get; private set; } = 0;

    public static void AddBonusPoints(int amount)
    {
        if (amount <= 0)
            return;

        EnsureLoaded();
        BonusPoints += amount;
        SaveToDisk();
    }

    private static bool _loaded;

    public static void EnsureLoaded()
    {
        if (_loaded)
            return;

        _loaded = true;
        LoadFromDisk();
    }

    public static bool IsKnownCell(string cellId)
    {
        return !string.IsNullOrEmpty(cellId) && GameManager.ClassData.ContainsKey(cellId);
    }

    public static bool IsKnownNode(string nodeId)
    {
        return TryGetNode(nodeId, out _);
    }

    public static bool TryGetNode(string nodeId, out TreeNode node)
    {
        foreach (var candidate in Nodes)
        {
            if (candidate.Id == nodeId)
            {
                node = candidate;
                return true;
            }
        }
        node = default;
        return false;
    }

    public static int GetMaxStacks(string nodeId)
    {
        return TryGetNode(nodeId, out var node) ? Math.Max(1, node.MaxStacks) : 0;
    }

    public static int GetPointCost(string nodeId)
    {
        if (!TryGetNode(nodeId, out var node))
            return int.MaxValue;
        return IsNucleus(nodeId) ? 0 : Math.Max(1, node.PointCost);
    }

    public static TreeRarity GetRarity(string nodeId)
    {
        return TryGetNode(nodeId, out var node) ? node.Rarity : TreeRarity.Normal;
    }

    public static string GetStartNode(string cellId)
    {
        EnsureLoaded();
        return StartNodes.TryGetValue(cellId, out var nodeId) ? nodeId : "";
    }

    /// <summary>True when the node is the cell's innately lit start hub (0-point).</summary>
    public static bool IsInnateStartNode(string cellId, string nodeId)
    {
        if (string.IsNullOrEmpty(cellId) || string.IsNullOrEmpty(nodeId) || IsNucleus(nodeId))
            return false;
        return nodeId == GetStartNode(cellId);
    }

    public static int GetCellLevel(string cellId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return BaseCellLevel;
        return CellLevels.TryGetValue(cellId, out var level) ? Math.Max(BaseCellLevel, level) : BaseCellLevel;
    }

    public static bool RecordRunLevel(string cellId, int runLevel)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return false;

        int normalized = Math.Max(BaseCellLevel, runLevel);
        int current = GetCellLevel(cellId);
        if (normalized <= current)
            return false;

        CellLevels[cellId] = normalized;
        SaveToDisk();
        return true;
    }

    public static Dictionary<string, int> GetAllocation(string cellId)
    {
        EnsureLoaded();
        return GetConnectedAllocation(cellId);
    }

    private static Dictionary<string, int> GetValidOwnedNodes(string cellId)
    {
        var result = new Dictionary<string, int>();
        if (!IsKnownCell(cellId) || !Allocations.TryGetValue(cellId, out var owned))
            return result;

        foreach (var pair in owned)
        {
            if (IsKnownNode(pair.Key) && pair.Value > 0)
                result[pair.Key] = Math.Min(GetMaxStacks(pair.Key), pair.Value);
        }
        return result;
    }

    private static bool HasVisitedNeighbor(string nodeId, System.Collections.Generic.HashSet<string> visited)
    {
        foreach (var neighbor in GetNeighbors(nodeId))
        {
            if (visited.Contains(neighbor))
                return true;
        }
        return false;
    }

    public static Dictionary<string, int> GetConnectedAllocation(string cellId)
    {
        var remaining = GetValidOwnedNodes(cellId);
        var connected = new Dictionary<string, int> { [NucleusNodeId] = 1 };
        string start = GetStartNode(cellId);
        var visited = new System.Collections.Generic.HashSet<string> { NucleusNodeId };
        if (!string.IsNullOrEmpty(start))
        {
            // The start hub is innately lit at 0 cost (docs/passivetree.md §5.1).
            int startStacks = remaining.TryGetValue(start, out int ownedStacks)
                ? Math.Max(InnateStartStacks, ownedStacks)
                : InnateStartStacks;
            connected[start] = startStacks;
            visited.Add(start);
        }

        var frontier = new System.Collections.Generic.Queue<string>();
        foreach (var id in visited)
            frontier.Enqueue(id);
        while (frontier.Count > 0)
        {
            frontier.Dequeue();
            foreach (var pair in remaining)
            {
                if (!connected.ContainsKey(pair.Key) && HasVisitedNeighbor(pair.Key, visited))
                {
                    connected[pair.Key] = pair.Value;
                    visited.Add(pair.Key);
                    frontier.Enqueue(pair.Key);
                }
            }
        }
        return connected;
    }

    public static bool IsFullyConnected(string cellId, string nodeId)
    {
        if (!IsKnownNode(nodeId))
            return false;
        if (nodeId == GetStartNode(cellId))
            return true;
        return GetConnectedAllocation(cellId).ContainsKey(nodeId);
    }

    private static bool WouldRemainConnected(string cellId, string removeNodeId)
    {
        var remaining = GetValidOwnedNodes(cellId);
        if (!remaining.TryGetValue(removeNodeId, out int stacks))
            return false;
        if (stacks > 1)
            remaining[removeNodeId] = stacks - 1;
        else
            remaining.Remove(removeNodeId);

        string start = GetStartNode(cellId);
        var visited = new System.Collections.Generic.HashSet<string> { NucleusNodeId };
        if (!string.IsNullOrEmpty(start))
            visited.Add(start);
        var frontier = new System.Collections.Generic.Queue<string>();
        foreach (var id in visited)
            frontier.Enqueue(id);
        while (frontier.Count > 0)
        {
            frontier.Dequeue();
            foreach (var pair in remaining)
            {
                if (!visited.Contains(pair.Key) && HasVisitedNeighbor(pair.Key, visited))
                {
                    visited.Add(pair.Key);
                    frontier.Enqueue(pair.Key);
                }
            }
        }

        foreach (var pair in remaining)
        {
            if (!visited.Contains(pair.Key))
                return false;
        }
        return true;
    }

    public static int GetNodeStacks(string cellId, string nodeId)
    {
        var allocation = GetAllocation(cellId);
        return allocation.TryGetValue(nodeId, out var stacks) ? stacks : 0;
    }

    public static int GetSpentPoints(string cellId)
    {
        int total = 0;
        foreach (var pair in GetAllocation(cellId))
        {
            if (IsNucleus(pair.Key) || IsInnateStartNode(cellId, pair.Key))
                continue;
            total += pair.Value * GetPointCost(pair.Key);
        }
        return total;
    }

    public static int GetPointsAvailable(string cellId)
    {
        return Math.Max(0, GetCellLevel(cellId) - BaseCellLevel - GetSpentPoints(cellId)) + BonusPoints;
    }

    public static System.Collections.Generic.List<string> GetNeighbors(string nodeId)
    {
        var neighbors = new System.Collections.Generic.List<string>();
        foreach (var edge in Edges)
        {
            if (edge.From == nodeId && !neighbors.Contains(edge.To))
                neighbors.Add(edge.To);
            else if (edge.To == nodeId && !neighbors.Contains(edge.From))
                neighbors.Add(edge.From);
        }
        return neighbors;
    }

    public static bool IsNodeConnected(string cellId, string nodeId)
    {
        if (!IsKnownNode(nodeId))
            return false;
        if (nodeId == GetStartNode(cellId))
            return true;

        var owned = GetAllocation(cellId);
        foreach (var neighbor in GetNeighbors(nodeId))
        {
            if (neighbor == GetStartNode(cellId))
                return true;
            if (owned.ContainsKey(neighbor))
                return true;
        }
        return false;
    }

    public static bool CanPurchase(string cellId, string nodeId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId) || !IsKnownNode(nodeId))
            return false;
        if (IsNucleus(nodeId))
            return false;
        if (GetNodeStacks(cellId, nodeId) >= GetMaxStacks(nodeId))
            return false;
        if (GetPointsAvailable(cellId) < GetPointCost(nodeId))
            return false;
        if (GetNodeStacks(cellId, nodeId) > 0)
            return IsFullyConnected(cellId, nodeId);
        return IsNodeConnected(cellId, nodeId);
    }

    public static bool Purchase(string cellId, string nodeId)
    {
        if (!CanPurchase(cellId, nodeId))
            return false;

        if (!Allocations.TryGetValue(cellId, out var owned))
        {
            owned = new Dictionary<string, int>();
            Allocations[cellId] = owned;
        }

        owned[nodeId] = GetNodeStacks(cellId, nodeId) + 1;
        SaveToDisk();
        return true;
    }

    public static bool RefundNode(string cellId, string nodeId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId) || !IsKnownNode(nodeId) || IsNucleus(nodeId))
            return false;
        // The innate start hub is permanent and never refundable.
        if (IsInnateStartNode(cellId, nodeId))
            return false;
        if (!Allocations.TryGetValue(cellId, out var owned) || !owned.TryGetValue(nodeId, out var stacks) || stacks <= 0)
            return false;
        if (!WouldRemainConnected(cellId, nodeId))
            return false;

        if (stacks == 1)
            owned.Remove(nodeId);
        else
            owned[nodeId] = stacks - 1;

        if (owned.Count == 0)
            Allocations.Remove(cellId);
        SaveToDisk();
        return true;
    }

    public static void ResetAllocation(string cellId)
    {
        EnsureLoaded();
        if (Allocations.Remove(cellId))
            SaveToDisk();
    }

    public static BaseSkill? CreateStackedSkill(string nodeId, int stacks)
    {
        if (!TryGetNode(nodeId, out var node))
            return null;

        int level = Mathf.Clamp(stacks, 1, node.MaxStacks);
        if (node.SkillType != null)
        {
            if (Activator.CreateInstance(node.SkillType) is not BaseSkill skill)
                return null;
            skill.Level = level;
            return skill;
        }

        return new TreeStatBundleSkill(node, level);
    }

    public static void SaveToDisk()
    {
        var levels = new Dictionary();
        foreach (var pair in CellLevels)
            levels[pair.Key] = pair.Value;

        var allocations = new Dictionary();
        foreach (var cellPair in Allocations)
        {
            var owned = new Dictionary();
            foreach (var nodePair in GetConnectedAllocation(cellPair.Key))
            {
                // The innate start hub is derived, never persisted.
                if (IsNucleus(nodePair.Key) || IsInnateStartNode(cellPair.Key, nodePair.Key))
                    continue;
                owned[nodePair.Key] = nodePair.Value;
            }
            if (owned.Count > 0)
                allocations[cellPair.Key] = owned;
        }

        var payload = new Dictionary
        {
            { "cell_levels", levels },
            { "allocations", allocations },
            { "bonus_points", BonusPoints }
        };

        JsonStore.Write(SavePath, payload);
    }

    public static void LoadFromDisk()
    {
        _loaded = true;

        var data = JsonStore.Read(SavePath);
        if (data == null)
            return;

        if (data.TryGetValue("bonus_points", out var bonusVal))
        {
            BonusPoints = Math.Max(0, bonusVal.AsInt32());
        }

        if (data.TryGetValue("cell_levels", out var levelsVal) && levelsVal.VariantType == Variant.Type.Dictionary)
        {
            var levels = levelsVal.AsGodotDictionary();
            foreach (var key in levels.Keys)
            {
                string cellId = key.AsString();
                if (IsKnownCell(cellId))
                    CellLevels[cellId] = Math.Max(BaseCellLevel, levels[key].AsInt32());
            }
        }

        if (data.TryGetValue("allocations", out var allocVal) && allocVal.VariantType == Variant.Type.Dictionary)
        {
            var allocations = allocVal.AsGodotDictionary();
            foreach (var key in allocations.Keys)
            {
                string cellId = key.AsString();
                if (!IsKnownCell(cellId) || allocations[key].VariantType != Variant.Type.Dictionary)
                    continue;

                var owned = new Dictionary<string, int>();
                var savedOwned = allocations[key].AsGodotDictionary();
                foreach (var nodeKey in savedOwned.Keys)
                {
                    string nodeId = nodeKey.AsString();
                    int stacks = Mathf.Clamp(savedOwned[nodeKey].AsInt32(), 1, GetMaxStacks(nodeId));
                    if (IsKnownNode(nodeId))
                        owned[nodeId] = stacks;
                }
                if (owned.Count > 0)
                    Allocations[cellId] = owned;
            }
        }

        foreach (var cellId in new System.Collections.Generic.List<string>(Allocations.Keys))
        {
            var connected = GetConnectedAllocation(cellId);
            if (connected.Count > 0)
                Allocations[cellId] = connected;
            else
                Allocations.Remove(cellId);
        }
    }

    public static void ReloadFromDisk()
    {
        CellLevels.Clear();
        Allocations.Clear();
        BonusPoints = 0;
        _loaded = false;
        EnsureLoaded();
    }

    public static void ResetAll()
    {
        CellLevels.Clear();
        Allocations.Clear();
        BonusPoints = 0;
        _loaded = true;
        JsonStore.Delete(SavePath);
    }
}
