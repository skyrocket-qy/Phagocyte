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
        Unique,
        Start
    }

    public enum TreeModifierUnit
    {
        Flat,
        Percent,
        PercentagePoints
    }

    public readonly record struct TreeStatModifier(string Stat, float Value, TreeModifierUnit Unit);

    /// <summary>
    /// Single source of truth for splitting one tree modifier
    /// into flat/percent channels (mirrors the gear modifier
    /// convention: flat/percentage-points go flat, everything else percent).
    /// Used by both the run-time skill
    /// (<see cref="Phagocyte.Skills.TreeStatBundleSkill"/>) and the menu
    /// build preview so the two can never diverge.
    /// </summary>
    public static (float Flat, float Pct) SplitModifier(TreeStatModifier modifier)
    {
        bool isFlatOrPoints = modifier.Unit is TreeModifierUnit.Flat
            or TreeModifierUnit.PercentagePoints;
        return isFlatOrPoints
            ? (modifier.Value, 0.0f)
            : (0.0f, modifier.Value);
    }

    /// <summary>
    /// Reusable talent trait (docs/passivetree.md §1.2): nodes reference a trait
    /// by id, so identical traits always share one name, icon, rarity and
    /// modifier bundle.
    /// </summary>
    public readonly record struct TreeTrait(
        string Id,
        TreeRarity Rarity,
        string Icon,
        string NameKey,
        string DescKey,
        string BioKey,
        TreeStatModifier[] Modifiers);

    public readonly record struct TreeNode(
        string Id,
        Vector2 Position,
        TreeRarity Rarity,
        string Branch,
        string Icon,
        string NameKey,
        string DescKey,
        string TraitId,
        TreeStatModifier[] Modifiers,
        int Ring);

    public const int BaseCellLevel = 1;

    /// <summary>
    /// Meta level cap per cell: 14 level points plus shared achievement bonus
    /// stays well under the 54-point full-tree cost, so builds must choose.
    /// Enforced in <see cref="RecordRunLevel"/> and <see cref="GetCellLevel"/>.
    /// </summary>
    public const int MaxCellLevel = 15;

    /// <summary>
    /// The deployed cell's start hub is innately lit and never consumes points
    /// (docs/passivetree.md §2 L=0 / §5.1).
    /// </summary>
    public static readonly Vector2 WorldCenter = new(2400.0f, 2400.0f);

    public const float GridStep = 150.0f;

    /// <summary>
    /// Every branch owns a 3x3 square of grid slots centred on one hub
    /// (docs/passivetree.md §1.1). The cell start hubs sit at the square centre;
    /// the core square is centred on the world origin.
    /// </summary>
    public const int RegionRadius = 1;

    /// <summary>
    /// Region squares tile edge-to-edge: each is 3x3 grid cells wide
    /// (half extent 1.5 steps, side 450px), so neighbouring centres 3 steps
    /// apart share a border and the six squares form one 6x9 board.
    /// </summary>
    public const float RegionHalfExtent = 1.5f;

    public static Vector2 GridPosition(int column, int row)
    {
        return WorldCenter + new Vector2(column * GridStep, -row * GridStep);
    }

    public static int LayerOf(int column, int row)
    {
        return Math.Abs(column) + Math.Abs(row);
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

    /// <summary>trait id → reusable trait definition (name/icon/rarity/modifiers).</summary>
    public static System.Collections.Generic.IReadOnlyDictionary<string, TreeTrait> Traits
    {
        get
        {
            EnsureTreeLoaded();
            DataValidator.EnsureValidated();
            return _loadedTraits;
        }
    }

    public static bool TryGetTrait(string traitId, out TreeTrait trait)
    {
        EnsureTreeLoaded();
        return _loadedTraits.TryGetValue(traitId, out trait);
    }

    /// <summary>branch id → region square centre in world coordinates.</summary>
    public static System.Collections.Generic.IReadOnlyDictionary<string, Vector2> RegionCenters
    {
        get
        {
            EnsureTreeLoaded();
            DataValidator.EnsureValidated();
            return _loadedRegions;
        }
    }

    public static bool TryGetRegionCenter(string branch, out Vector2 center)
    {
        EnsureTreeLoaded();
        return _loadedRegions.TryGetValue(branch, out center);
    }

    /// <summary>Region square in world coordinates (3x3 cells, side 3 grid steps).</summary>
    public static Rect2 GetRegionBounds(string branch)
    {
        if (!TryGetRegionCenter(branch, out var center))
            return new Rect2(Vector2.Zero, Vector2.Zero);
        float half = RegionHalfExtent * GridStep;
        return new Rect2(center - new Vector2(half, half), new Vector2(half * 2.0f, half * 2.0f));
    }

    private static System.Collections.Generic.Dictionary<string, string>? _statLabels;
    private static System.Collections.Generic.Dictionary<string, string> StatLabels => _statLabels ??= CatalogBuilders.BuildStatLabels();

    /// <summary>All tree data loads in one pass so Nodes/Edges/Starts/Traits stay consistent.</summary>
    private static bool _treeLoaded = false;
    private static (string From, string To)[] _loadedEdges = System.Array.Empty<(string, string)>();
    private static System.Collections.Generic.Dictionary<string, string> _loadedStarts = new();
    private static System.Collections.Generic.Dictionary<string, Vector2> _loadedRegions = new();
    private static System.Collections.Generic.Dictionary<string, TreeTrait> _loadedTraits = new();

    private static void EnsureTreeLoaded()
    {
        if (_treeLoaded)
            return;
        _treeLoaded = true;
        CatalogBuilders.BuildTree(out var nodes, out var edges, out var starts, out var regions, out var traits);
        _nodes = nodes;
        _loadedEdges = edges;
        _loadedStarts = starts;
        _loadedRegions = regions;
        _loadedTraits = traits;
    }

    private static TreeNode[] LoadTreeNodes()
    {
        EnsureTreeLoaded();
        return _nodes!;
    }

    public static bool HasStatLabel(string stat)
    {
        return StatLabels.ContainsKey(stat);
    }

    public static System.Collections.Generic.IReadOnlyDictionary<string, string> StatLabelValues => StatLabels;

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

        bool placed = IsPlaced(cellId, nodeId);
        var text = new StringBuilder();
        text.AppendLine("[b][img=40x40]" + AssetPaths.TraitIcon(node.TraitId) + "[/img] " + TranslationServer.Translate(node.NameKey) + "[/b]");
        if (IsInnateStartNode(cellId, nodeId))
            text.AppendLine(TranslationServer.Translate("TREE_START_INNATE"));
        text.AppendLine(GetNodeDescription(nodeId));

        // Deep lore layer (Dim 4): real-world mechanism below the tactical text.
        if (TryGetTrait(node.TraitId, out var trait) && !string.IsNullOrEmpty(trait.BioKey))
        {
            string bio = TranslationServer.Translate(trait.BioKey);
            if (bio != "" && bio != trait.BioKey)
            {
                text.AppendLine();
                text.AppendLine(bio);
            }
        }

        // Innate and owned nodes state their effects with no further hint.
        string status = "";
        if (!IsInnateStartNode(cellId, nodeId) && !placed)
        {
            if (CanPurchase(cellId, nodeId))
                status = TranslationServer.Translate("TREE_PURCHASE_HINT");
            else if (GetPointsAvailable(cellId) >= 1)
                status = TranslationServer.Translate("TREE_LOCKED");
        }

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

    /// <summary>Maximum build profiles (talent loadouts) per cell.</summary>
    public const int MaxProfiles = 3;

    /// <summary>
    /// cellId → ordered build profiles; each profile holds the placed node ids.
    /// Lists always hold between 1 and <see cref="MaxProfiles"/> slots.
    /// </summary>
    public static Dictionary<string, Array<Array<string>>> Allocations = new();

    /// <summary>cellId → index of the profile currently edited and deployed.</summary>
    public static Dictionary<string, int> ActiveProfiles = new();

    /// <summary>
    /// Extra talent points from meta progression (achievement map clears).
    /// Derived live from the catalog + unlock state on every read — never
    /// stored, so no path (cheats included) can inflate the total beyond what
    /// the unlocked achievements justify. Shared across every cell so
    /// organ-clear rewards always have a spendable home.
    /// </summary>
    public static int GetEarnedBonusPoints()
    {
        EnsureLoaded();
        int total = 0;
        foreach (var achIdVar in AchievementManager.Achievements.Keys)
        {
            string achId = achIdVar.AsString();
            if (!AchievementManager.IsUnlocked(achId))
                continue;
            var data = AchievementManager.Achievements[achId].AsGodotDictionary();
            if (data.TryGetValue("talent_points", out var tpVar))
                total += tpVar.AsInt32();
        }
        return Math.Max(0, total);
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

    public static string GetStartNode(string cellId)
    {
        EnsureLoaded();
        return StartNodes.TryGetValue(cellId, out var nodeId) ? nodeId : "";
    }

    /// <summary>True when the node is the cell's innately lit start hub (0-point).</summary>
    public static bool IsInnateStartNode(string cellId, string nodeId)
    {
        if (string.IsNullOrEmpty(cellId) || string.IsNullOrEmpty(nodeId))
            return false;
        return nodeId == GetStartNode(cellId);
    }

    public static int GetCellLevel(string cellId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return BaseCellLevel;
        return CellLevels.TryGetValue(cellId, out var level) ? Math.Min(MaxCellLevel, Math.Max(BaseCellLevel, level)) : BaseCellLevel;
    }

    public static bool RecordRunLevel(string cellId, int runLevel)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return false;

        int normalized = Math.Min(MaxCellLevel, Math.Max(BaseCellLevel, runLevel));
        int current = GetCellLevel(cellId);
        if (normalized <= current)
            return false;

        CellLevels[cellId] = normalized;
        SaveToDisk();
        return true;
    }

    /// <summary>Placed node ids for the active profile (start hub included).</summary>
    public static System.Collections.Generic.HashSet<string> GetAllocation(string cellId)
    {
        EnsureLoaded();
        return GetConnectedAllocation(cellId);
    }

    /// <summary>All profile slots for a cell, guaranteeing at least one slot.</summary>
    private static Array<Array<string>> GetSlots(string cellId)
    {
        if (!Allocations.TryGetValue(cellId, out var slots) || slots.Count == 0)
        {
            slots = new Array<Array<string>> { new Array<string>() };
            Allocations[cellId] = slots;
        }
        return slots;
    }

    /// <summary>The profile slot currently edited and deployed.</summary>
    private static Array<string> GetActiveOwned(string cellId)
    {
        return GetSlots(cellId)[GetActiveProfile(cellId)];
    }

    /// <summary>Number of build profiles stored for a cell (0 when the cell is unknown).</summary>
    public static int GetProfileCount(string cellId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return 0;
        return GetSlots(cellId).Count;
    }

    /// <summary>Index of the active build profile (0 when the cell is unknown).</summary>
    public static int GetActiveProfile(string cellId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return 0;
        var slots = GetSlots(cellId);
        if (!ActiveProfiles.TryGetValue(cellId, out int active) || active < 0 || active >= slots.Count)
        {
            active = 0;
            ActiveProfiles[cellId] = 0;
        }
        return active;
    }

    /// <summary>Switch the active build profile. Returns false for unknown cells or out-of-range indices.</summary>
    public static bool SetActiveProfile(string cellId, int index)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return false;
        var slots = GetSlots(cellId);
        if (index < 0 || index >= slots.Count)
            return false;
        if (!ActiveProfiles.TryGetValue(cellId, out int current) || current != index)
        {
            ActiveProfiles[cellId] = index;
            SaveToDisk();
        }
        return true;
    }

    /// <summary>Append an empty build profile and switch to it. Refused at <see cref="MaxProfiles"/>.</summary>
    public static bool AddProfile(string cellId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return false;
        var slots = GetSlots(cellId);
        if (slots.Count >= MaxProfiles)
            return false;
        slots.Add(new Array<string>());
        ActiveProfiles[cellId] = slots.Count - 1;
        SaveToDisk();
        return true;
    }

    /// <summary>
    /// Delete one build profile. The last remaining profile is kept.
    /// Deleting the active (or an earlier) slot moves the active index along.
    /// </summary>
    public static bool DeleteProfile(string cellId, int index)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return false;
        var slots = GetSlots(cellId);
        if (slots.Count <= 1 || index < 0 || index >= slots.Count)
            return false;
        int active = GetActiveProfile(cellId);
        slots.RemoveAt(index);
        if (active == index)
            active = Math.Max(0, index - 1);
        else if (active > index)
            active -= 1;
        ActiveProfiles[cellId] = active;
        SaveToDisk();
        return true;
    }

    /// <summary>Display name for a profile slot (配置一/二/三).</summary>
    public static string GetProfileName(int index)
    {
        return TranslationServer.Translate("TREE_PROFILE_" + (index + 1));
    }

    private static System.Collections.Generic.HashSet<string> FilterValidNodes(Array<string> owned)
    {
        var result = new System.Collections.Generic.HashSet<string>();
        foreach (string id in owned)
        {
            if (IsKnownNode(id))
                result.Add(id);
        }
        return result;
    }

    private static System.Collections.Generic.HashSet<string> GetValidOwnedNodes(string cellId)
    {
        if (!IsKnownCell(cellId))
            return new System.Collections.Generic.HashSet<string>();
        return FilterValidNodes(GetActiveOwned(cellId));
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

    public static System.Collections.Generic.HashSet<string> GetConnectedAllocation(string cellId)
    {
        return GetConnectedFrom(GetValidOwnedNodes(cellId), cellId);
    }

    private static System.Collections.Generic.HashSet<string> GetConnectedFrom(System.Collections.Generic.HashSet<string> owned, string cellId)
    {
        var connected = new System.Collections.Generic.HashSet<string>();
        string start = GetStartNode(cellId);
        var visited = new System.Collections.Generic.HashSet<string>();
        if (!string.IsNullOrEmpty(start))
        {
            // The start hub is innately lit at 0 cost (docs/passivetree.md §5.1).
            connected.Add(start);
            visited.Add(start);
        }

        var frontier = new System.Collections.Generic.Queue<string>();
        foreach (var id in visited)
            frontier.Enqueue(id);
        while (frontier.Count > 0)
        {
            frontier.Dequeue();
            foreach (string id in owned)
            {
                if (!connected.Contains(id) && HasVisitedNeighbor(id, visited))
                {
                    connected.Add(id);
                    visited.Add(id);
                    frontier.Enqueue(id);
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
        return GetConnectedAllocation(cellId).Contains(nodeId);
    }

    private static bool WouldRemainConnected(string cellId, string removeNodeId)
    {
        var remaining = new System.Collections.Generic.HashSet<string>(GetValidOwnedNodes(cellId));
        if (!remaining.Remove(removeNodeId))
            return false;

        string start = GetStartNode(cellId);
        var visited = new System.Collections.Generic.HashSet<string>();
        if (!string.IsNullOrEmpty(start))
            visited.Add(start);
        var frontier = new System.Collections.Generic.Queue<string>();
        foreach (var id in visited)
            frontier.Enqueue(id);
        while (frontier.Count > 0)
        {
            frontier.Dequeue();
            foreach (string id in remaining)
            {
                if (!visited.Contains(id) && HasVisitedNeighbor(id, visited))
                {
                    visited.Add(id);
                    frontier.Enqueue(id);
                }
            }
        }

        foreach (string id in remaining)
        {
            if (!visited.Contains(id))
                return false;
        }
        return true;
    }

    /// <summary>True when the node is placed (the start hub is always placed).</summary>
    public static bool IsPlaced(string cellId, string nodeId)
    {
        return GetAllocation(cellId).Contains(nodeId);
    }

    public static int GetSpentPoints(string cellId)
    {
        int total = 0;
        foreach (string id in GetAllocation(cellId))
        {
            if (IsInnateStartNode(cellId, id))
                continue;
            total += 1;
        }
        return total;
    }

    public static int GetPointsAvailable(string cellId)
    {
        return Math.Max(0, GetCellLevel(cellId) - BaseCellLevel + GetEarnedBonusPoints() - GetSpentPoints(cellId));
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
            if (owned.Contains(neighbor))
                return true;
        }
        return false;
    }

    public static bool CanPurchase(string cellId, string nodeId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId) || !IsKnownNode(nodeId))
            return false;
        if (IsPlaced(cellId, nodeId))
            return false;
        if (GetPointsAvailable(cellId) < 1)
            return false;
        return IsNodeConnected(cellId, nodeId);
    }

    public static bool Purchase(string cellId, string nodeId)
    {
        if (!CanPurchase(cellId, nodeId))
            return false;

        GetActiveOwned(cellId).Add(nodeId);
        SaveToDisk();
        return true;
    }

    public static bool RefundNode(string cellId, string nodeId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId) || !IsKnownNode(nodeId))
            return false;
        // The innate start hub is permanent and never refundable.
        if (IsInnateStartNode(cellId, nodeId))
            return false;
        var owned = GetActiveOwned(cellId);
        if (!owned.Contains(nodeId))
            return false;
        if (!WouldRemainConnected(cellId, nodeId))
            return false;

        owned.Remove(nodeId);
        SaveToDisk();
        return true;
    }

    public static void ResetAllocation(string cellId)
    {
        EnsureLoaded();
        if (!IsKnownCell(cellId))
            return;
        var owned = GetActiveOwned(cellId);
        if (owned.Count > 0)
        {
            owned.Clear();
            SaveToDisk();
        }
    }

    public static BaseSkill? CreateSkill(string nodeId)
    {
        if (!TryGetNode(nodeId, out var node))
            return null;

        return new TreeStatBundleSkill(node);
    }

    public static void SaveToDisk()
    {
        var levels = new Dictionary();
        foreach (var pair in CellLevels)
            levels[pair.Key] = pair.Value;

        var allocations = new Dictionary();
        var actives = new Dictionary();
        foreach (var cellPair in Allocations)
        {
            if (!IsKnownCell(cellPair.Key))
                continue;
            var slotsOut = new Godot.Collections.Array();
            bool anyNonEmpty = false;
            foreach (var owned in cellPair.Value)
            {
                var slotOut = new Godot.Collections.Array();
                foreach (string nodeId in GetConnectedFrom(FilterValidNodes(owned), cellPair.Key))
                {
                    // The innate start hub is derived, never persisted.
                    if (IsInnateStartNode(cellPair.Key, nodeId))
                        continue;
                    slotOut.Add(nodeId);
                }
                if (slotOut.Count > 0)
                    anyNonEmpty = true;
                slotsOut.Add(slotOut);
            }
            if (!anyNonEmpty)
                continue;
            allocations[cellPair.Key] = slotsOut;
            actives[cellPair.Key] = GetActiveProfile(cellPair.Key);
        }

        var payload = new Dictionary
        {
            { "cell_levels", levels },
            { "allocations", allocations },
            { "active_profiles", actives }
        };

        JsonStore.Write(SavePath, payload);
    }

    public static void LoadFromDisk()
    {
        _loaded = true;

        var data = JsonStore.Read(SavePath);
        if (data == null)
            return;

        // Legacy "bonus_points" key (pre-dynamic era) is intentionally not
        // read: the earned total is recomputed from unlocks on every access,
        // so stale inflated values evaporate on load.

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
                if (!IsKnownCell(cellId))
                    continue;

                var slots = new Array<Array<string>>();
                var raw = allocations[key];
                if (raw.VariantType == Variant.Type.Array)
                {
                    foreach (var slotVar in raw.AsGodotArray())
                    {
                        if (slots.Count >= MaxProfiles)
                            break;
                        // Anything but a string-id array is a pre-binary save:
                        // those saves are wiped, never migrated.
                        if (slotVar.VariantType != Variant.Type.Array)
                        {
                            WipeLegacySave();
                            return;
                        }
                        var placed = new Array<string>();
                        foreach (var idVar in slotVar.AsGodotArray())
                        {
                            string nodeId = idVar.AsString();
                            if (IsKnownNode(nodeId))
                                placed.Add(nodeId);
                        }
                        slots.Add(placed);
                    }
                }
                else
                {
                    WipeLegacySave();
                    return;
                }
                if (slots.Count == 0)
                    slots.Add(new Array<string>());
                Allocations[cellId] = slots;
            }
        }

        if (data.TryGetValue("active_profiles", out var activeVal) && activeVal.VariantType == Variant.Type.Dictionary)
        {
            var actives = activeVal.AsGodotDictionary();
            foreach (var key in actives.Keys)
            {
                string cellId = key.AsString();
                if (!IsKnownCell(cellId) || !Allocations.TryGetValue(cellId, out var slots))
                    continue;
                ActiveProfiles[cellId] = Mathf.Clamp(actives[key].AsInt32(), 0, slots.Count - 1);
            }
        }

        foreach (var cellId in new System.Collections.Generic.List<string>(Allocations.Keys))
        {
            var slots = Allocations[cellId];
            bool anyKept = false;
            for (int i = 0; i < slots.Count; i++)
            {
                var connected = GetConnectedFrom(FilterValidNodes(slots[i]), cellId);
                var pruned = new Array<string>();
                foreach (string id in connected)
                    pruned.Add(id);
                slots[i] = pruned;
                if (pruned.Count > 0)
                    anyKept = true;
            }
            if (!anyKept)
            {
                Allocations.Remove(cellId);
                ActiveProfiles.Remove(cellId);
            }
        }
    }

    /// <summary>Deletes a pre-binary tree save and resets to a fresh tree.</summary>
    private static void WipeLegacySave()
    {
        JsonStore.Delete(SavePath);
        CellLevels.Clear();
        Allocations.Clear();
        ActiveProfiles.Clear();
    }

    public static void ReloadFromDisk()
    {
        CellLevels.Clear();
        Allocations.Clear();
        ActiveProfiles.Clear();
        _loaded = false;
        EnsureLoaded();
    }

    public static void ResetAll()
    {
        CellLevels.Clear();
        Allocations.Clear();
        ActiveProfiles.Clear();
        _loaded = true;
        JsonStore.Delete(SavePath);
    }
}
