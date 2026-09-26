using Godot;
using System;
using Phagocyte.Player;

namespace Phagocyte.Gear;

/// <summary>
/// Base class for Modular Gear (外掛式細胞器).
/// Gear are decoupled PackedScene attachments that live as child nodes of a
/// BaseCell and extend beyond its main polygon topology (IK limbs, receptor rings).
/// Attached gear read the host's universal CellStats pool at runtime.
/// </summary>
public abstract partial class ModularGear : Node2D
{
    public BaseCell? Host { get; protected set; }

    public bool IsAttached => Host != null && GodotObject.IsInstanceValid(Host);

    /// <summary>
    /// Binds this gear to a host cell, reparenting it under the host center.
    /// </summary>
    public virtual void AttachTo(BaseCell host)
    {
        ArgumentNullException.ThrowIfNull(host);

        Host = host;
        if (GetParent() != host)
        {
            GetParent()?.RemoveChild(this);
            host.AddChild(this);
        }

        Position = Vector2.Zero;
        Rotation = 0.0f;
        ZIndex = 3;
    }

    /// <summary>
    /// Unbinds the gear from its host and removes it.
    /// </summary>
    public virtual void Detach()
    {
        Host = null;
        QueueFree();
    }

    /// <summary>
    /// Reads a universal stat from the host's CellStats pool (0 when unattached).
    /// </summary>
    protected float GetStat(string statName)
    {
        return Host?.Stats?.GetStat(statName) ?? 0.0f;
    }
}
