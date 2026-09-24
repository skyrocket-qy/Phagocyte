using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Core;
using Phagocyte.Enemies;

namespace Phagocyte.Combat;

/// <summary>
/// Extreme-density bullet-hell projectile manager.
/// Uses MultiMeshInstance2D batch rendering and a 64px spatial hash grid
/// for zero-GC, O(1) collision queries capable of handling 4096+ projectiles at 60 FPS.
/// </summary>
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

    // 2D QuadTree over the active pathogens: rebuilt once per physics frame and
    // shared by every projectile, giving O(log n) neighborhood queries at the
    // 300-500 enemy concurrency budget (docs/spec.md §9 / TODO module 12).
    private const float ArenaQueryHalfExtent = 2600.0f;
    private readonly QuadTree<BaseEnemy> _enemyTree = new(
        new Rect2(-ArenaQueryHalfExtent, -ArenaQueryHalfExtent, ArenaQueryHalfExtent * 2.0f, ArenaQueryHalfExtent * 2.0f),
        capacity: 8,
        maxDepth: 6);
    private readonly List<BaseEnemy> _enemyQuery = new(64);

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
        RegisterType("antibody", null, new Vector2(16, 16));

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
        float damage,
        bool isCrit = false,
        int pierce = 0,
        float lifetime = 2.5f,
        float radius = 10.0f,
        string projType = "generic")
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
                    Damage = damage,
                    IsCrit = isCrit,
                    PierceRemaining = pierce,
                    ProjectileTypeIndex = typeIndex,
                    IsActive = true,
                    HitTarget0 = 0,
                    HitTarget1 = 0,
                    HitTarget2 = 0,
                    HitTarget3 = 0
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
            Damage = damage,
            IsCrit = isCrit,
            PierceRemaining = pierce,
            ProjectileTypeIndex = typeIndex,
            IsActive = true,
            HitTarget0 = 0,
            HitTarget1 = 0,
            HitTarget2 = 0,
            HitTarget3 = 0
        };
        MarkActive(overwriteSlot);
        _nextSpawnIndex = (_nextSpawnIndex + 1) % MaxTotalProjectiles;
    }

    private void RebuildEnemyIndex()
    {
        _enemyTree.Clear();

        foreach (var enemy in BaseEnemy.ActiveEnemies)
        {
            if (enemy == null || !GodotObject.IsInstanceValid(enemy) || enemy.IsBeingEaten) continue;
            _enemyTree.Insert(enemy.GlobalPosition, enemy);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Idle frames (no bullets in flight) skip the enemy-index rebuild,
        // the per-bullet queries and the multimesh sync entirely. The
        // transition frame already zeroed every VisibleInstanceCount.
        if (_activeCount == 0)
            return;

        float dt = (float)delta;
        Array.Clear(_typeActiveCounts, 0, _typeActiveCounts.Length);

        // Empty arena: bullets fly straight with no targets to index or query.
        bool hasTargets = BaseEnemy.ActiveEnemies.Count > 0;
        if (hasTargets)
            RebuildEnemyIndex();

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

            p.Position += p.Direction * p.Speed * dt;

            bool projectileAlive = true;
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
                    if (enemy == null || !GodotObject.IsInstanceValid(enemy) || enemy.IsBeingEaten) continue;

                    ulong enemyId = enemy.GetInstanceId();
                    if (p.HasHitTarget(enemyId)) continue;

                    if (p.Position.DistanceSquaredTo(enemy.GlobalPosition) <= hitDistSq)
                    {
                        p.AddHitTarget(enemyId);

                        // Apply direct combat damage and stats
                        enemy.TakeDamage(p.Damage, _hostNode, p.IsCrit);

                        if (p.PierceRemaining > 0)
                        {
                            p.PierceRemaining--;
                        }
                        else
                        {
                            projectileAlive = false;
                            MarkInactive(slot);
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
