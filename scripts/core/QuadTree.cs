using Godot;
using System;
using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Allocation-conscious 2D quad tree for the 300-500 pathogen concurrency budget
/// (docs/spec.md §9 / TODO module 12). Supports O(log n) circle / rect queries and
/// is designed to be rebuilt every physics frame with pooled nodes and pre-sized
/// item lists, so steady-state rebuilds allocate nothing.
/// </summary>
public sealed class QuadTree<T>
{
    private struct Item
    {
        public Vector2 Position;
        public T Value;
    }

    private sealed class Node
    {
        public Rect2 Bounds;
        public int Depth;
        public readonly List<Item> Items = new(8);
        public readonly Node?[] Children = new Node?[4];

        public bool HasChildren => Children[0] != null;
    }

    private readonly Rect2 _bounds;
    private readonly int _capacity;
    private readonly int _maxDepth;
    private readonly Stack<Node> _pool = new();
    private Node _root;

    /// <summary>Inserted item count of the last rebuild.</summary>
    public int Count { get; private set; }

    public Rect2 Bounds => _bounds;

    public QuadTree(Rect2 bounds, int capacity = 8, int maxDepth = 6)
    {
        _bounds = bounds;
        _capacity = Mathf.Max(1, capacity);
        _maxDepth = Mathf.Max(0, maxDepth);
        _root = new Node { Bounds = bounds, Depth = 0 };
    }

    /// <summary>Drops every item and returns all nodes to the pool.</summary>
    public void Clear()
    {
        ReturnToPool(_root);
        _root = Rent(_bounds, 0);
        Count = 0;
    }

    public void Insert(Vector2 position, T value)
    {
        Count++;
        InsertInto(_root, new Item { Position = position, Value = value });
    }

    /// <summary>Appends every item whose position lies inside the circle.</summary>
    public void QueryCircle(Vector2 center, float radius, List<T> results)
    {
        if (radius <= 0.0f || (!_root.HasChildren && _root.Items.Count == 0))
            return;

        float radiusSq = radius * radius;
        var area = new Rect2(
            center - new Vector2(radius, radius),
            new Vector2(radius * 2.0f, radius * 2.0f));
        QueryCircleRecursive(_root, center, radiusSq, area, results, isRoot: true);
    }

    /// <summary>Appends every item whose position lies inside the rectangle.</summary>
    public void QueryRect(Rect2 area, List<T> results)
    {
        QueryRectRecursive(_root, area, results, isRoot: true);
    }

    /// <summary>
    /// Nearest item within <paramref name="maxRadius"/>. <paramref name="scratch"/>
    /// is reused between calls to keep steady-state queries allocation-free.
    /// Items are located via <paramref name="positionOf"/>; when omitted, Node2D
    /// items use their global position.
    /// </summary>
    public bool TryFindNearest(
        Vector2 center,
        float maxRadius,
        List<T> scratch,
        out T nearest,
        Func<T, bool>? predicate = null,
        Func<T, Vector2>? positionOf = null)
    {
        scratch.Clear();
        QueryCircle(center, maxRadius, scratch);

        nearest = default!;
        float bestSq = float.MaxValue;
        bool found = false;
        float maxSq = maxRadius * maxRadius;

        foreach (var candidate in scratch)
        {
            if (predicate != null && !predicate(candidate))
                continue;
            if (!TryGetPosition(candidate, positionOf, out Vector2 candidatePos))
                continue;

            float d = center.DistanceSquaredTo(candidatePos);
            if (d <= maxSq && d < bestSq)
            {
                bestSq = d;
                nearest = candidate;
                found = true;
            }
        }

        return found;
    }

    private static bool TryGetPosition(T candidate, Func<T, Vector2>? positionOf, out Vector2 position)
    {
        if (positionOf != null)
        {
            position = positionOf(candidate);
            return true;
        }

        if (candidate is Node2D node)
        {
            position = node.GlobalPosition;
            return true;
        }

        position = default;
        return false;
    }

    private void InsertInto(Node node, Item item)
    {
        if (node.HasChildren)
        {
            int childIndex = ChildIndex(node.Bounds, item.Position);
            if (childIndex >= 0)
            {
                InsertInto(node.Children[childIndex]!, item);
                return;
            }
        }

        node.Items.Add(item);

        if (!node.HasChildren && node.Items.Count > _capacity && node.Depth < _maxDepth)
            Subdivide(node);
    }

    private void Subdivide(Node node)
    {
        Vector2 half = node.Bounds.Size * 0.5f;
        Vector2 origin = node.Bounds.Position;

        node.Children[0] = Rent(new Rect2(origin, half), node.Depth + 1);
        node.Children[1] = Rent(new Rect2(origin + new Vector2(half.X, 0.0f), half), node.Depth + 1);
        node.Children[2] = Rent(new Rect2(origin + new Vector2(0.0f, half.Y), half), node.Depth + 1);
        node.Children[3] = Rent(new Rect2(origin + half, half), node.Depth + 1);

        for (int i = node.Items.Count - 1; i >= 0; i--)
        {
            Item item = node.Items[i];
            int childIndex = ChildIndex(node.Bounds, item.Position);
            if (childIndex < 0)
                continue;

            node.Items.RemoveAt(i);
            InsertInto(node.Children[childIndex]!, item);
        }
    }

    private static int ChildIndex(Rect2 bounds, Vector2 position)
    {
        Vector2 center = bounds.Position + bounds.Size * 0.5f;
        if (position.X < bounds.Position.X || position.X > bounds.Position.X + bounds.Size.X
            || position.Y < bounds.Position.Y || position.Y > bounds.Position.Y + bounds.Size.Y)
        {
            return -1; // outside the root: keep at the containing node
        }

        bool right = position.X >= center.X;
        bool bottom = position.Y >= center.Y;
        return (bottom ? 2 : 0) + (right ? 1 : 0);
    }

    private static void QueryCircleRecursive(Node node, Vector2 center, float radiusSq, Rect2 area, List<T> results, bool isRoot)
    {
        // The root keeps items that lie outside the tree bounds; those must stay
        // queryable, so the root's item list is always scanned.
        if (!isRoot && !node.Bounds.Intersects(area, true))
            return;

        for (int i = 0; i < node.Items.Count; i++)
        {
            Item item = node.Items[i];
            if (item.Position.DistanceSquaredTo(center) <= radiusSq)
                results.Add(item.Value);
        }

        if (!node.HasChildren)
            return;

        for (int c = 0; c < 4; c++)
            QueryCircleRecursive(node.Children[c]!, center, radiusSq, area, results, isRoot: false);
    }

    private static void QueryRectRecursive(Node node, Rect2 area, List<T> results, bool isRoot)
    {
        if (!isRoot && !node.Bounds.Intersects(area, true))
            return;

        for (int i = 0; i < node.Items.Count; i++)
        {
            if (area.HasPoint(node.Items[i].Position))
                results.Add(node.Items[i].Value);
        }

        if (!node.HasChildren)
            return;

        for (int c = 0; c < 4; c++)
            QueryRectRecursive(node.Children[c]!, area, results, isRoot: false);
    }

    private Node Rent(Rect2 bounds, int depth)
    {
        Node node = _pool.Count > 0 ? _pool.Pop() : new Node();
        node.Bounds = bounds;
        node.Depth = depth;
        node.Items.Clear();
        node.Children[0] = null;
        node.Children[1] = null;
        node.Children[2] = null;
        node.Children[3] = null;
        return node;
    }

    private void ReturnToPool(Node node)
    {
        if (node.HasChildren)
        {
            for (int c = 0; c < 4; c++)
                ReturnToPool(node.Children[c]!);
        }

        node.Items.Clear();
        node.Children[0] = null;
        node.Children[1] = null;
        node.Children[2] = null;
        node.Children[3] = null;
        _pool.Push(node);
    }
}
