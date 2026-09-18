namespace Phagocyte.Core;

using System;
using Godot;

/// <summary>
/// Extension methods for Godot.Node providing assertive fail-fast node resolution.
/// Ensures missing or incorrectly typed nodes in scenes crash early with rich diagnostics.
/// </summary>
public static class NodeExtensions
{
    /// <summary>
    /// Retrieves a required child node of type <typeparamref name="T"/> at the specified <paramref name="path"/>.
    /// Throws an explicit <see cref="InvalidOperationException"/> with rich scene diagnostics if the node is missing or of incorrect type.
    /// </summary>
    public static T GetRequiredNode<T>(this Node parent, NodePath path) where T : class
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(path);

        var node = parent.GetNodeOrNull<T>(path);
        if (node == null)
        {
            string scenePath = parent.Owner?.SceneFilePath
                ?? parent.GetTree()?.CurrentScene?.SceneFilePath
                ?? "UnknownScene";

            throw new InvalidOperationException(
                $"[{parent.GetType().Name}] Required node '{path}' of type '{typeof(T).Name}' was not found or invalid in scene '{scenePath}'.");
        }

        return node;
    }

    /// <summary>
    /// Retrieves a required child node of type <typeparamref name="T"/> at the specified string path.
    /// </summary>
    public static T GetRequiredNode<T>(this Node parent, string path) where T : class
        => parent.GetRequiredNode<T>(new NodePath(path));
}
