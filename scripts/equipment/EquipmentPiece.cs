using Godot;
using System;
using Game.Player;

namespace Game.Equipment;

/// <summary>
/// Base class for Modular Equipment (外掛式細胞器).
/// Equipment are decoupled PackedScene attachments that live as child nodes of a
/// PlayerActor and extend beyond its main polygon topology (IK limbs, receptor rings).
/// Attached gear read the host's universal ActorStats pool at runtime.
/// </summary>
public abstract partial class EquipmentPiece : Node2D
{
    public PlayerActor? Host { get; protected set; }

    public bool IsAttached => Host != null && GodotObject.IsInstanceValid(Host);

    /// <summary>
    /// Binds this gear to a host cell, reparenting it under the host center.
    /// </summary>
    public virtual void AttachTo(PlayerActor host)
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
    /// Reads a universal stat from the host's ActorStats pool (0 when unattached).
    /// </summary>
    protected float GetStat(string statName)
    {
        return Host?.Stats?.GetStat(statName) ?? 0.0f;
    }
}
