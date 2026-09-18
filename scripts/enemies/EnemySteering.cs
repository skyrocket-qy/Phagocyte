using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Shared threat-intent steering helpers. Every pathogen uses one of four tactical
/// modes (see <see cref="EnemyThreatMode"/>) so the horde applies real positioning
/// pressure instead of aimless wandering. Pure drifting is reserved for neutral
/// environment matter and scripted bosses.
/// </summary>
public static class EnemySteering
{
    public const float ChaserJitter = 0.22f;
    public const float InterceptorJitter = 0.12f;

    /// <summary>Interceptor lead distance bounds (docs: 100-200px ahead of travel).</summary>
    public const float LeadDistanceMin = 100.0f;
    public const float LeadDistanceMax = 200.0f;
    public const float LeadVelocityFactor = 0.6f;

    private static readonly List<Vector2> TissueAnchors = new();
    private static Vector2 _arenaSize = new(4800.0f, 4800.0f);

    private static BaseCell? _cachedPlayer;

    /// <summary>
    /// Registers the current arena dimensions so invaders can pick host-tissue anchors.
    /// </summary>
    public static void ConfigureArena(Vector2 arenaSize)
    {
        _arenaSize = arenaSize;
        BuildTissueAnchors();
    }

    /// <summary>
    /// Cached player lookup (re-resolved only when the cell is freed), avoiding a
    /// scene-tree group query for every one of the 300-450 active pathogens.
    /// </summary>
    public static BaseCell? GetPlayer(Node node)
    {
        if (_cachedPlayer != null && GodotObject.IsInstanceValid(_cachedPlayer))
            return _cachedPlayer;

        _cachedPlayer = node.GetTree().GetFirstNodeInGroup("player") as BaseCell;
        return _cachedPlayer;
    }

    /// <summary>
    /// Resolves the desired steering direction for the enemy's threat mode.
    /// Returns <see cref="Vector2.Zero"/> to fall back to neutral Brownian drift.
    /// </summary>
    public static Vector2 GetDirection(BaseEnemy enemy, float dt)
    {
        return enemy.ThreatMode switch
        {
            EnemyThreatMode.ChemoChaser => SeekPlayer(enemy, ChaserJitter),
            EnemyThreatMode.Interceptor => InterceptPlayer(enemy),
            EnemyThreatMode.Standoff => Standoff(enemy),
            EnemyThreatMode.Invader => InvadeTissue(enemy),
            _ => Vector2.Zero
        };
    }

    /// <summary>Direct chemo-chase along the shortest vector to the cell.</summary>
    public static Vector2 SeekPlayer(BaseEnemy enemy, float jitterAmplitude)
    {
        var player = GetPlayer(enemy);
        if (player == null || player.IsDead)
            return Vector2.Zero;

        Vector2 toPlayer = player.GlobalPosition - enemy.GlobalPosition;
        if (toPlayer.LengthSquared() < 0.0001f)
            return Vector2.Zero;

        return ApplyJitter(enemy, toPlayer.Normalized(), jitterAmplitude);
    }

    /// <summary>
    /// Flanker interception: aims at a point 100-200px ahead of the player's current
    /// travel vector, punishing one-directional kiting / edge-circling.
    /// </summary>
    public static Vector2 InterceptPlayer(BaseEnemy enemy)
    {
        var player = GetPlayer(enemy);
        if (player == null || player.IsDead)
            return Vector2.Zero;

        return AimAtIntercept(enemy, player.GlobalPosition, player.Velocity);
    }

    /// <summary>Pure interception math, exposed for deterministic tests.</summary>
    public static Vector2 AimAtIntercept(BaseEnemy enemy, Vector2 playerPosition, Vector2 playerVelocity)
    {
        Vector2 aimPoint = playerPosition;
        if (playerVelocity.LengthSquared() > 1.0f)
        {
            float lead = Mathf.Clamp(playerVelocity.Length() * LeadVelocityFactor, LeadDistanceMin, LeadDistanceMax);
            aimPoint = playerPosition + playerVelocity.Normalized() * lead;
        }

        Vector2 toAim = aimPoint - enemy.GlobalPosition;
        if (toAim.LengthSquared() < 0.0001f)
            return Vector2.Zero;

        return ApplyJitter(enemy, toAim.Normalized(), InterceptorJitter);
    }

    /// <summary>
    /// Standoff artillery: approach when far, back off when crowded, orbit sideways in band.
    /// </summary>
    public static Vector2 Standoff(BaseEnemy enemy)
    {
        var player = GetPlayer(enemy);
        if (player == null || player.IsDead)
            return Vector2.Zero;

        Vector2 toPlayer = player.GlobalPosition - enemy.GlobalPosition;
        float distance = toPlayer.Length();
        if (distance < 0.0001f)
            return Vector2.Zero;

        Vector2 direction = toPlayer / distance;
        float preferred = Mathf.Max(60.0f, enemy.SteeringPreferredRange);

        if (distance > preferred * 1.15f)
            return direction;
        if (distance < preferred * 0.60f)
            return -direction;

        Vector2 tangent = direction.Orthogonal() * (enemy.SteeringOrbitSign >= 0.0f ? 1.0f : -1.0f);
        return ApplyJitter(enemy, tangent, 0.08f);
    }

    /// <summary>
    /// Tissue invader: ignores the player and drives at the nearest host-tissue anchor.
    /// Once host ulceration accumulates, invaders switch priority to red blood cells.
    /// Returns Zero once latched so the invader can ulcerate in place.
    /// </summary>
    public static Vector2 InvadeTissue(BaseEnemy enemy)
    {
        if (HostUlceration.Pulses >= HostUlceration.RbcPreferenceThreshold)
        {
            Node2D? rbc = FindNearestNeutral(enemy, "senescent_rbc");
            if (rbc != null)
            {
                Vector2 toRbc = rbc.GlobalPosition - enemy.GlobalPosition;
                enemy.SteeringAnchor = rbc.GlobalPosition;
                if (toRbc.Length() <= Mathf.Max(8.0f, enemy.SteeringLatchRange))
                    return Vector2.Zero;
                return toRbc.Normalized();
            }
        }

        Vector2 anchor = GetNearestTissueAnchor(enemy.GlobalPosition);
        enemy.SteeringAnchor = anchor;

        Vector2 toAnchor = anchor - enemy.GlobalPosition;
        if (toAnchor.Length() <= Mathf.Max(8.0f, enemy.SteeringLatchRange))
            return Vector2.Zero;

        return toAnchor.Normalized();
    }

    private static Node2D? FindNearestNeutral(Node owner, string group)
    {
        Node2D? best = null;
        float bestDistance = float.MaxValue;
        Vector2 from = owner is Node2D origin ? origin.GlobalPosition : Vector2.Zero;

        foreach (var node in owner.GetTree().GetNodesInGroup(group))
        {
            if (node is Node2D neutral && GodotObject.IsInstanceValid(neutral))
            {
                float distance = from.DistanceSquaredTo(neutral.GlobalPosition);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = neutral;
                }
            }
        }

        return best;
    }

    /// <summary>True when an invader reached its anchor and is ulcerating in place.</summary>
    public static bool IsInvaderLatched(BaseEnemy enemy)
    {
        Vector2 anchor = enemy.SteeringAnchor;
        return anchor != Vector2.Zero
            && enemy.GlobalPosition.DistanceTo(anchor) <= Mathf.Max(8.0f, enemy.SteeringLatchRange);
    }

    public static Vector2 GetNearestTissueAnchor(Vector2 from)
    {
        if (TissueAnchors.Count == 0)
            BuildTissueAnchors();

        Vector2 best = TissueAnchors.Count > 0 ? TissueAnchors[0] : from;
        float bestDistance = float.MaxValue;
        foreach (Vector2 anchor in TissueAnchors)
        {
            float distance = from.DistanceSquaredTo(anchor);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = anchor;
            }
        }
        return best;
    }

    public static IReadOnlyList<Vector2> GetTissueAnchors()
    {
        if (TissueAnchors.Count == 0)
            BuildTissueAnchors();
        return TissueAnchors;
    }

    private static void BuildTissueAnchors()
    {
        TissueAnchors.Clear();

        float halfW = _arenaSize.X * 0.5f;
        float halfH = _arenaSize.Y * 0.5f;
        float ringX = halfW * 0.82f;
        float ringY = halfH * 0.82f;

        // Six host-tissue sites: vascular endothelium patches around the organ slice
        for (int i = 0; i < 6; i++)
        {
            float angle = i * (Mathf.Tau / 6.0f) + 0.35f;
            TissueAnchors.Add(new Vector2(Mathf.Cos(angle) * ringX, Mathf.Sin(angle) * ringY));
        }
    }

    private static Vector2 ApplyJitter(BaseEnemy enemy, Vector2 direction, float amplitude)
    {
        if (amplitude <= 0.0f)
            return direction;

        // Deterministic per-instance phase offset keeps the tide from collapsing
        // into a single straight-line deathball without any squad bookkeeping.
        ulong id = enemy.GetInstanceId();
        float phaseOffset = (float)(id % 997) * 0.13f;
        float jitter = Mathf.Sin(enemy.SteeringPhase * 1.9f + phaseOffset) * amplitude;
        return direction.Rotated(jitter);
    }
}
