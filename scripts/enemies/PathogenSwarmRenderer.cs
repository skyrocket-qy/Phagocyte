using Godot;
using System;
using System.Collections.Generic;

namespace Phagocyte.Enemies;

/// <summary>
/// GPU batch renderer for the microscopic swarm species (docs/spec.md §9 /
/// TODO module 12). Every matching pathogen is written into a per-species
/// <see cref="MultiMeshInstance2D"/> and its own per-node drawing is suppressed,
/// so 500 micro viruses cost one draw call per species instead of 500.
/// </summary>
public partial class PathogenSwarmRenderer : Node2D
{
    private sealed class Species
    {
        public string Id = "";
        public int Capacity = 256;
        public MultiMesh Multimesh = null!;
        public int VisibleCount;
    }

    private readonly List<Species> _species = new();
    private readonly Dictionary<string, int> _indexById = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<BaseEnemy> _scratch = new(512);
    private MultiMeshInstance2D? _batchRoot;

    /// <summary>Master switch; when disabled every enemy reverts to per-node drawing.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Instances written during the last <see cref="Sync"/>.</summary>
    public int LastBatchedCount { get; private set; }

    public int SpeciesCount => _species.Count;

    public override void _Ready()
    {
        // Tree order already places EnemyContainer after Background; keep z at the
        // default bucket so the batch draws above the arena backdrop.
        ZIndex = 0;
        _batchRoot = new MultiMeshInstance2D { Name = "SwarmBatchRoot" };
        AddChild(_batchRoot);

        RegisterSpecies("norovirus", 768, new Vector2(13.0f, 13.0f), BuildNorovirusTexture());
        RegisterSpecies("flu_drift", 192, new Vector2(24.0f, 24.0f), BuildFluDriftTexture());
    }

    public void RegisterSpecies(string speciesId, int capacity, Vector2 quadSize, Texture2D texture)
    {
        if (_indexById.ContainsKey(speciesId))
            return;

        var multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseColors = true,
            InstanceCount = capacity,
            VisibleInstanceCount = 0,
            Mesh = new QuadMesh { Size = quadSize }
        };

        var layer = new MultiMeshInstance2D
        {
            Name = $"SwarmBatch_{speciesId}",
            Multimesh = multimesh,
            Texture = texture
        };
        Node parent = _batchRoot != null ? _batchRoot : (Node)this;
        parent.AddChild(layer);

        _indexById[speciesId] = _species.Count;
        _species.Add(new Species { Id = speciesId, Capacity = capacity, Multimesh = multimesh });
    }

    /// <summary>Instances currently drawn for a species (test / diagnostics hook).</summary>
    public int GetVisibleCount(string speciesId)
    {
        return _indexById.TryGetValue(speciesId, out int idx) ? _species[idx].VisibleCount : 0;
    }

    /// <summary>True when a species participates in GPU batching.</summary>
    public bool IsSpeciesBatched(string speciesId)
    {
        return _indexById.ContainsKey(speciesId);
    }

    /// <summary>
    /// Rewrites every batch transform/color from the active pathogen list.
    /// Called once per rendered frame by Main.
    /// </summary>
    public void Sync()
    {
        LastBatchedCount = 0;
        for (int i = 0; i < _species.Count; i++)
            _species[i].VisibleCount = 0;

        _scratch.Clear();
        foreach (var enemy in BaseEnemy.ActiveEnemies)
        {
            if (enemy == null || !GodotObject.IsInstanceValid(enemy))
                continue;
            if (_indexById.ContainsKey(enemy.EnemyId))
                _scratch.Add(enemy);
        }

        for (int i = 0; i < _scratch.Count; i++)
        {
            var enemy = _scratch[i];
            if (!_indexById.TryGetValue(enemy.EnemyId, out int speciesIndex))
                continue;

            var species = _species[speciesIndex];

            if (!Enabled || enemy.IsQueuedForDeletion() || species.VisibleCount >= species.Capacity)
            {
                enemy.Visible = true; // fall back to the node's own drawing
                continue;
            }

            enemy.Visible = false;
            species.Multimesh.SetInstanceTransform2D(species.VisibleCount, enemy.GlobalTransform);
            species.Multimesh.SetInstanceColor(species.VisibleCount, enemy.SwarmBatchColor * enemy.Modulate);
            species.VisibleCount++;
            LastBatchedCount++;
        }

        for (int i = 0; i < _species.Count; i++)
        {
            _species[i].Multimesh.VisibleInstanceCount = Enabled ? _species[i].VisibleCount : 0;
        }
    }

    private static ImageTexture BuildNorovirusTexture()
    {
        const int size = 16;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        const float apothem = 5.6f; // circumradius ≈ 6.5px like the per-node drawing

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float hexDistance = -1e9f;
                for (int edge = 0; edge < 6; edge++)
                {
                    float angle = Mathf.DegToRad(30.0f + edge * 60.0f);
                    Vector2 normal = Vector2.FromAngle(angle);
                    hexDistance = Mathf.Max(hexDistance, p.Dot(normal));
                }

                Color color = new Color(0, 0, 0, 0);
                if (hexDistance <= apothem)
                    color = new Color(0.8f, 0.8f, 0.8f, 0.95f); // capsid shell (tinted)
                if (p.Length() <= 3.0f)
                    color = new Color(1.0f, 1.0f, 1.0f, 0.98f);  // bright core

                image.SetPixel(x, y, color);
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    private static ImageTexture BuildFluDriftTexture()
    {
        const int size = 48;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float d = p.Length();

                Color color = new Color(0, 0, 0, 0);
                if (d <= 22.0f)
                    color = new Color(1.0f, 1.0f, 1.0f, 0.22f); // antigenic aura
                if (d <= 14.0f)
                    color = new Color(0.82f, 0.82f, 0.82f, 0.95f); // lipid envelope
                if (d <= 9.0f)
                    color = new Color(1.0f, 1.0f, 1.0f, 0.95f); // lightened core

                image.SetPixel(x, y, color);
            }
        }

        // Alternating HA rods / NA mushroom tetramers along the envelope rim.
        for (int i = 0; i < 14; i++)
        {
            float angle = i * (Mathf.Tau / 14.0f);
            Vector2 basePos = Vector2.FromAngle(angle) * 13.0f;
            Vector2 tipPos = Vector2.FromAngle(angle) * 20.0f;
            bool even = i % 2 == 0;
            Color spike = even
                ? new Color(0.85f, 1.0f, 1.0f, 0.95f)
                : new Color(0.95f, 0.72f, 0.62f, 0.95f);

            DrawBakedLine(image, center + basePos, center + tipPos, spike, 2);
            if (!even)
                FillBakedCircle(image, center + tipPos, 2.2f, spike);
        }

        return ImageTexture.CreateFromImage(image);
    }

    private static void DrawBakedLine(Image image, Vector2 from, Vector2 to, Color color, int thickness)
    {
        float length = from.DistanceTo(to);
        int steps = Mathf.Max(1, (int)Mathf.Ceil(length));
        for (int s = 0; s <= steps; s++)
        {
            Vector2 point = from.Lerp(to, s / (float)steps);
            FillBakedCircle(image, point, thickness * 0.5f, color);
        }
    }

    private static void FillBakedCircle(Image image, Vector2 center, float radius, Color color)
    {
        int minX = Mathf.Max(0, (int)Mathf.Floor(center.X - radius));
        int maxX = Mathf.Min(image.GetWidth() - 1, (int)Mathf.Ceil(center.X + radius));
        int minY = Mathf.Max(0, (int)Mathf.Floor(center.Y - radius));
        int maxY = Mathf.Min(image.GetHeight() - 1, (int)Mathf.Ceil(center.Y + radius));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (new Vector2(x + 0.5f, y + 0.5f).DistanceTo(center) <= radius)
                    image.SetPixel(x, y, color);
            }
        }
    }
}
