using Godot;
using System;
using System.Collections.Generic;
using Game.Core;
using Game.Enemies;
using Game.Player;

namespace Game.Combat;

/// <summary>Batch projectile manager using MultiMesh rendering and QuadTree queries.</summary>
public partial class ProjectileManager : Node2D
{
    public static ProjectileManager? Instance { get; private set; }

    public const int MaxTotalProjectiles = 4096;
    public const int MaxPerTypeCapacity = 2048;

    private readonly ProjectileData[] _projectiles = new ProjectileData[MaxTotalProjectiles];
    private readonly int[] _activeSlots = new int[MaxTotalProjectiles];
    private readonly int[] _slotToActiveIdx = new int[MaxTotalProjectiles];
    private int _activeCount = 0;
    private int _nextSpawnIndex = 0;

    private readonly List<string> _typeKeys = new();
    private readonly Dictionary<string, int> _typeKeyToIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<MultiMeshInstance2D> _typeMultiMeshes = new();
    private int[] _typeActiveCounts = Array.Empty<int>();

    // QuadTree over active enemies: rebuilt once per physics frame for O(log n) queries.
    private const float ArenaQueryHalfExtent = 2600.0f;
    private readonly QuadTree<EnemyActor> _enemyTree = new(
        new Rect2(-ArenaQueryHalfExtent, -ArenaQueryHalfExtent, ArenaQueryHalfExtent * 2.0f, ArenaQueryHalfExtent * 2.0f),
        capacity: 8,
        maxDepth: 6);
    private readonly List<EnemyActor> _enemyQuery = new(64);

    private Node2D? _hostNode;
    private static Texture2D? _cachedBulletTexture;

    public int ActiveCount => _activeCount;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Pausable;
        ZIndex = 40;
        Array.Fill(_slotToActiveIdx, -1);

        RegisterType("generic", null, new Vector2(16, 16));
        RegisterType("defensin_barb", null, new Vector2(22, 10));
        RegisterType("seeker", null, new Vector2(16, 16));
        RegisterType("enemy_pellet", null, new Vector2(16, 16));

        _typeActiveCounts = new int[_typeKeys.Count];
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base._ExitTree();
    }

    public void SetHost(Node2D host)
    {
        _hostNode = host;
    }

    private static Texture2D GetOrCreateBulletTexture()
    {
        if (_cachedBulletTexture != null) return _cachedBulletTexture;

        const int size = 24;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = center.DistanceTo(new Vector2(x + 0.5f, y + 0.5f));
                if (d <= radius)
                {
                    float factor = 1.0f - (d / radius);
                    float a = factor * factor;
                    img.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                else
                {
                    img.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        }
        _cachedBulletTexture = ImageTexture.CreateFromImage(img);
        return _cachedBulletTexture;
    }

    public int RegisterType(string key, string? texturePath, Vector2 quadSize)
    {
        if (_typeKeyToIndex.TryGetValue(key, out int existingIndex))
        {
            return existingIndex;
        }

        int newIndex = _typeKeys.Count;
        _typeKeys.Add(key);
        _typeKeyToIndex[key] = newIndex;

        Texture2D? tex = null;
        if (!string.IsNullOrEmpty(texturePath))
        {
            tex = AssetLoader.TryLoad<Texture2D>(texturePath);
        }
        tex ??= GetOrCreateBulletTexture();

        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            InstanceCount = MaxPerTypeCapacity,
            VisibleInstanceCount = 0,
            Mesh = new QuadMesh { Size = quadSize }
        };

        var instance = new MultiMeshInstance2D
        {
            Name = $"BatchLayer_{key}",
            Multimesh = multiMesh,
            Texture = tex
        };

        AddChild(instance);
        _typeMultiMeshes.Add(instance);
        _typeActiveCounts = new int[_typeKeys.Count];

        return newIndex;
    }

    public int GetTypeIndex(string typeKey)
    {
        if (_typeKeyToIndex.TryGetValue(typeKey, out int idx))
        {
            return idx;
        }
        return RegisterType(typeKey, null, new Vector2(18, 18));
    }

    private void MarkActive(int slotIndex)
    {
        if (_slotToActiveIdx[slotIndex] != -1) return;

        int activePos = _activeCount++;
        _activeSlots[activePos] = slotIndex;
        _slotToActiveIdx[slotIndex] = activePos;
    }

    private void MarkInactive(int slotIndex)
    {
        int activePos = _slotToActiveIdx[slotIndex];
        if (activePos == -1) return;

        _projectiles[slotIndex].IsActive = false;

        int lastPos = --_activeCount;
        int lastSlot = _activeSlots[lastPos];

        _activeSlots[activePos] = lastSlot;
        _slotToActiveIdx[lastSlot] = activePos;

        _slotToActiveIdx[slotIndex] = -1;
    }

    public void Spawn(
        Vector2 pos,
        Vector2 dir,
        float speed,
        float baseDamage,
        float critChance = 0.0f,
        float critMultiplier = 1.0f,
        int pierce = 0,
        float lifetime = 2.5f,
        float radius = 10.0f,
        string projType = "generic",
        Team team = Team.Player,
        EffectSpec effect0 = default,
        EffectSpec effect1 = default,
        EffectSpec effect2 = default,
        int effectCount = 0,
        int steering = 0,
        float turnRate = 6.0f,
        float wobbleFreq = 0.0f,
        float wobbleAmp = 0.0f,
        float reacquireRadius = 350.0f,
        ulong homingTargetId = 0)
    {
        if (dir == Vector2.Zero) dir = Vector2.Right;
        else dir = dir.Normalized();

        int typeIndex = GetTypeIndex(projType);

        // Circular buffer slot finding
        for (int i = 0; i < MaxTotalProjectiles; i++)
        {
            int slot = (_nextSpawnIndex + i) % MaxTotalProjectiles;
            if (!_projectiles[slot].IsActive)
            {
                _projectiles[slot] = new ProjectileData
                {
                    Position = pos,
                    Direction = dir,
                    Rotation = dir.Angle(),
                    Speed = speed,
                    Radius = radius,
                    Lifetime = lifetime,
                    ElapsedTime = 0.0f,
                    BaseDamage = baseDamage,
                    CritChance = critChance,
                    CritMultiplier = critMultiplier,
                    PierceRemaining = pierce,
                    ProjectileTypeIndex = typeIndex,
                    IsActive = true,
                    HitTarget0 = 0,
                    HitTarget1 = 0,
                    HitTarget2 = 0,
                    HitTarget3 = 0,
                    SourceTeam = team,
                    Steering = (byte)steering,
                    TurnRate = turnRate,
                    WobbleFreq = wobbleFreq,
                    WobbleAmp = wobbleAmp,
                    ReacquireRadius = reacquireRadius,
                    Phase = 0.0f,
                    HomingTargetId = homingTargetId,
                    Effect0 = effect0,
                    Effect1 = effect1,
                    Effect2 = effect2,
                    EffectCount = effectCount
                };
                MarkActive(slot);
                _nextSpawnIndex = (slot + 1) % MaxTotalProjectiles;
                return;
            }
        }

        // Overwrite oldest if full
        int overwriteSlot = _nextSpawnIndex;
        _projectiles[overwriteSlot] = new ProjectileData
        {
            Position = pos,
            Direction = dir,
            Rotation = dir.Angle(),
            Speed = speed,
            Radius = radius,
            Lifetime = lifetime,
            ElapsedTime = 0.0f,
            BaseDamage = baseDamage,
            CritChance = critChance,
            CritMultiplier = critMultiplier,
            PierceRemaining = pierce,
            ProjectileTypeIndex = typeIndex,
            IsActive = true,
            HitTarget0 = 0,
            HitTarget1 = 0,
            HitTarget2 = 0,
            HitTarget3 = 0,
            SourceTeam = team,
            Steering = (byte)steering,
            TurnRate = turnRate,
            WobbleFreq = wobbleFreq,
            WobbleAmp = wobbleAmp,
            ReacquireRadius = reacquireRadius,
            Phase = 0.0f,
            HomingTargetId = homingTargetId,
            Effect0 = effect0,
            Effect1 = effect1,
            Effect2 = effect2,
            EffectCount = effectCount
        };
        MarkActive(overwriteSlot);
        _nextSpawnIndex = (_nextSpawnIndex + 1) % MaxTotalProjectiles;
    }

    private void RebuildEnemyIndex()
    {
        _enemyTree.Clear();

        foreach (var enemy in EnemyActor.ActiveEnemies)
        {
            if (enemy == null || !GodotObject.IsInstanceValid(enemy)) continue;
            _enemyTree.Insert(enemy.GlobalPosition, enemy);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Idle frames (no bullets in flight) skip the enemy-index rebuild, the
        // per-bullet queries and the multimesh sync entirely. The
        // transition frame already zeroed every VisibleInstanceCount.
        if (_activeCount == 0)
            return;

        float dt = (float)delta;
        Array.Clear(_typeActiveCounts, 0, _typeActiveCounts.Length);

        // Empty arena: bullets fly straight with no targets to index or query.
        bool hasTargets = EnemyActor.ActiveEnemies.Count > 0;
        if (hasTargets)
            RebuildEnemyIndex();

        // Enemy-team shots fly at the player cell (the host doubles as the
        // damage source for player-team shots, preserving life-steal).
        PlayerActor? player = _hostNode as PlayerActor ?? EnemySteering.GetPlayer(this);
        bool playerValid = player != null && GodotObject.IsInstanceValid(player) && !player.IsDead;
        Vector2 playerPos = playerValid ? player!.GlobalPosition : Vector2.Zero;
        float playerRadius = playerValid ? player!.CurrentRadius : 0.0f;
        ulong playerId = playerValid ? player!.GetInstanceId() : 0;

        for (int a = _activeCount - 1; a >= 0; a--)
        {
            int slot = _activeSlots[a];
            ref var p = ref _projectiles[slot];
            if (!p.IsActive)
            {
                MarkInactive(slot);
                continue;
            }

            p.ElapsedTime += dt;
            if (p.ElapsedTime >= p.Lifetime)
            {
                MarkInactive(slot);
                continue;
            }

            bool projectileAlive;
            if (p.SourceTeam == Team.Enemy)
            {
                p.Position += p.Direction * p.Speed * dt;
                p.Rotation = p.Direction.Angle();
                projectileAlive = true;
                if (playerValid && !p.HasHitTarget(playerId))
                {
                    float reach = p.Radius + playerRadius;
                    if (p.Position.DistanceSquaredTo(playerPos) <= reach * reach)
                    {
                        p.AddHitTarget(playerId);
                        DamageService.DealDamage(player, p.BaseDamage, null, p.CritChance, p.CritMultiplier);
                        EffectSpec.ApplyAll(player, in p.Effect0, in p.Effect1, in p.Effect2, p.EffectCount);
                        if (p.PierceRemaining > 0)
                            p.PierceRemaining--;
                        else
                        {
                            projectileAlive = false;
                            MarkInactive(slot);
                        }
                    }
                }
            }
            else
            {
                if (hasTargets && p.Steering == ProjectileData.SteeringHoming)
                    SteerHoming(ref p, dt);
                p.Position += p.Direction * p.Speed * dt;
                if (p.WobbleAmp > 0.0f && p.WobbleFreq > 0.0f)
                {
                    p.Phase += dt * p.WobbleFreq;
                    p.Position += p.Direction.Orthogonal() * (Mathf.Sin(p.Phase) * p.WobbleAmp * p.Speed * dt);
                }
                p.Rotation = p.Direction.Angle();

                projectileAlive = true;
                if (hasTargets)
                {
                    // QuadTree neighborhood query: O(log n + k) candidate lookup
                    float queryRadius = p.Radius + 18.0f;
                    float hitDistSq = queryRadius * queryRadius;
                    _enemyQuery.Clear();
                    _enemyTree.QueryCircle(p.Position, queryRadius, _enemyQuery);

                    for (int b = 0; b < _enemyQuery.Count && projectileAlive; b++)
                    {
                        var enemy = _enemyQuery[b];
                        if (enemy == null || !GodotObject.IsInstanceValid(enemy)) continue;

                        ulong enemyId = enemy.GetInstanceId();
                        if (p.HasHitTarget(enemyId)) continue;

                        if (p.Position.DistanceSquaredTo(enemy.GlobalPosition) <= hitDistSq)
                        {
                            p.AddHitTarget(enemyId);
                            DamageService.DealDamage(enemy, p.BaseDamage, _hostNode, p.CritChance, p.CritMultiplier);
                            EffectSpec.ApplyAll(enemy, in p.Effect0, in p.Effect1, in p.Effect2, p.EffectCount);

                            if (p.PierceRemaining > 0)
                            {
                                p.PierceRemaining--;
                                if (p.Steering == ProjectileData.SteeringChain)
                                    RedirectChain(ref p);
                            }
                            else
                            {
                                projectileAlive = false;
                                MarkInactive(slot);
                            }
                        }
                    }
                }
            }

            if (!projectileAlive) continue;

            // Update MultiMesh batch instance
            int type = p.ProjectileTypeIndex;
            if (type >= 0 && type < _typeMultiMeshes.Count)
            {
                int currentTypeIndex = _typeActiveCounts[type];
                if (currentTypeIndex < MaxPerTypeCapacity)
                {
                    Transform2D xform = new Transform2D(p.Rotation, p.Position);
                    _typeMultiMeshes[type].Multimesh.SetInstanceTransform2D(currentTypeIndex, xform);
                    _typeActiveCounts[type]++;
                }
            }
        }

        // Sync visible instance counts
        for (int t = 0; t < _typeMultiMeshes.Count; t++)
        {
            _typeMultiMeshes[t].Multimesh.VisibleInstanceCount = _typeActiveCounts[t];
        }
    }

    /// <summary>
    /// Homing steer toward the locked target while it is still indexed, else
    /// the nearest indexed enemy. Lock ids come from the spawner; 0 steers to
    /// nearest (matches the old unassigned-target fallback).
    /// </summary>
    private void SteerHoming(ref ProjectileData p, float dt)
    {
        _enemyQuery.Clear();
        _enemyTree.QueryCircle(p.Position, Mathf.Max(p.ReacquireRadius, 64.0f), _enemyQuery);

        Vector2? aim = null;
        float bestSq = float.MaxValue;
        Vector2? locked = null;
        foreach (var enemy in _enemyQuery)
        {
            if (enemy == null || !GodotObject.IsInstanceValid(enemy)) continue;
            float dSq = p.Position.DistanceSquaredTo(enemy.GlobalPosition);
            if (p.HomingTargetId != 0 && enemy.GetInstanceId() == p.HomingTargetId)
            {
                locked = enemy.GlobalPosition;
                break;
            }
            if (dSq < bestSq)
            {
                bestSq = dSq;
                aim = enemy.GlobalPosition;
            }
        }
        Vector2? target = locked ?? aim;
        if (target == null)
            return;
        Vector2 desired = (target.Value - p.Position).Normalized();
        if (desired == Vector2.Zero)
            return;
        p.Direction = p.Direction.Lerp(desired, Mathf.Clamp(p.TurnRate * dt, 0.0f, 1.0f));
        if (p.Direction.LengthSquared() < 0.000001f)
            p.Direction = desired;
        else
            p.Direction = p.Direction.Normalized();
    }

    /// <summary>Chain redirect: nearest unhit enemy, else a random deflection.</summary>
    private void RedirectChain(ref ProjectileData p)
    {
        _enemyQuery.Clear();
        _enemyTree.QueryCircle(p.Position, Mathf.Max(p.ReacquireRadius, 64.0f), _enemyQuery);

        float bestSq = float.MaxValue;
        Vector2? best = null;
        foreach (var enemy in _enemyQuery)
        {
            if (enemy == null || !GodotObject.IsInstanceValid(enemy)) continue;
            if (p.HasHitTarget(enemy.GetInstanceId())) continue;
            float dSq = p.Position.DistanceSquaredTo(enemy.GlobalPosition);
            if (dSq < bestSq)
            {
                bestSq = dSq;
                best = enemy.GlobalPosition;
            }
        }
        if (best != null)
        {
            Vector2 dir = (best.Value - p.Position).Normalized();
            if (dir != Vector2.Zero)
                p.Direction = dir;
        }
        else
        {
            p.Direction = p.Direction.Rotated((float)GD.RandRange(1.8f, 2.5f));
        }
    }

    /// <summary>Test hook: active shots for one faction.</summary>
    public int CountForTeam(Team team)
    {
        int n = 0;
        for (int i = 0; i < _activeCount; i++)
        {
            ref var p = ref _projectiles[_activeSlots[i]];
            if (p.IsActive && p.SourceTeam == team)
                n++;
        }
        return n;
    }

    /// <summary>Test hook: any active shot of one faction carrying an effect.</summary>
    public bool TeamHasEffect(Team team, string effectId)
    {
        for (int i = 0; i < _activeCount; i++)
        {
            ref var p = ref _projectiles[_activeSlots[i]];
            if (!p.IsActive || p.SourceTeam != team || p.EffectCount <= 0)
                continue;
            if (p.Effect0.EffectId == effectId) return true;
            if (p.EffectCount > 1 && p.Effect1.EffectId == effectId) return true;
            if (p.EffectCount > 2 && p.Effect2.EffectId == effectId) return true;
        }
        return false;
    }

    public void ClearAll()
    {
        for (int i = 0; i < MaxTotalProjectiles; i++)
        {
            _projectiles[i].IsActive = false;
            _slotToActiveIdx[i] = -1;
        }
        _activeCount = 0;

        for (int t = 0; t < _typeMultiMeshes.Count; t++)
        {
            _typeMultiMeshes[t].Multimesh.VisibleInstanceCount = 0;
            _typeActiveCounts[t] = 0;
        }
    }
}
