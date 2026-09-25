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
        RegisterSpecies("tb", 256, new Vector2(64.0f, 64.0f), BuildTbTexture());
        RegisterSpecies("e_coli", 256, new Vector2(72.0f, 72.0f), BuildEColiTexture());
        RegisterSpecies("staph", 256, new Vector2(40.0f, 40.0f), BuildStaphTexture());
        RegisterSpecies("staph_shielded", 64, new Vector2(48.0f, 48.0f), BuildStaphShieldedTexture());
        RegisterSpecies("pseudomonas", 192, new Vector2(48.0f, 48.0f), BuildPseudomonasTexture());
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

    /// <summary>Batch species for an enemy (shielded staph gets its armor variant).</summary>
    private static string ResolveSpeciesId(BaseEnemy enemy)
    {
        if (enemy.EnemyId == "staph" && enemy.FibrinShield > 0)
            return "staph_shielded";
        return enemy.EnemyId;
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
            string speciesId = ResolveSpeciesId(enemy);
            if (_indexById.ContainsKey(speciesId))
                _scratch.Add(enemy);
        }

        for (int i = 0; i < _scratch.Count; i++)
        {
            var enemy = _scratch[i];
            if (!_indexById.TryGetValue(ResolveSpeciesId(enemy), out int speciesIndex))
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

    /// <summary>TB waxy arc rod, baked at A-formula scale (r=23).</summary>
    private static ImageTexture BuildTbTexture()
    {
        const int size = 64;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        Vector2[] pts = { new(-24.6f, -6.6f), new(-9.9f, 3.3f), new(9.9f, 3.3f), new(24.6f, -6.6f) };
        var border = new Color(0.9f, 0.3f, 0.5f, 0.85f);
        var core = new Color(0.6f, 0.1f, 0.25f, 0.95f);
        for (int i = 0; i < pts.Length - 1; i++)
        {
            DrawBakedLine(image, center + pts[i], center + pts[i + 1], border, 13);
            DrawBakedLine(image, center + pts[i], center + pts[i + 1], core, 7);
        }
        FillBakedCircle(image, center + new Vector2(-13.2f, 0), 3.3f, Colors.White);
        FillBakedCircle(image, center + new Vector2(0, 3.3f), 3.3f, Colors.White);
        FillBakedCircle(image, center + new Vector2(13.2f, 0), 3.3f, Colors.White);
        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>E. coli capsule + flagella, baked at A-formula scale (r=20).</summary>
    private static ImageTexture BuildEColiTexture()
    {
        const int size = 72;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        var capsule = new Color(0.72f, 0.45f, 0.25f, 0.95f);
        var inner = new Color(0.90f, 0.62f, 0.38f, 0.90f);
        DrawBakedLine(image, center + new Vector2(-17.3f, 0), center + new Vector2(17.3f, 0), capsule, 16);
        FillBakedCircle(image, center + new Vector2(17.3f, 0), 8.0f, capsule);
        FillBakedCircle(image, center + new Vector2(-17.3f, 0), 8.0f, capsule);
        DrawBakedLine(image, center + new Vector2(-13.3f, 0), center + new Vector2(13.3f, 0), inner, 10);
        var hair = new Color(0.85f, 0.65f, 0.45f, 0.6f);
        for (int i = 0; i < 8; i++)
        {
            float ang = i * (Mathf.Tau / 8.0f);
            Vector2 root = center + new Vector2(Mathf.Cos(ang) * 18.7f, Mathf.Sin(ang) * 9.3f);
            Vector2 tip = root + Vector2.FromAngle(ang) * 16.0f;
            DrawBakedLine(image, root, tip, hair, 1);
        }
        return ImageTexture.CreateFromImage(image);
    }

    private static void BakeStaphCluster(Image image, Vector2 center, Color baseGolden)
    {
        Vector2[] offs = { new(-7, -6), new(7, -5), new(-2, 6), new(6, 7) };
        float[] rads = { 8.0f, 7.5f, 8.5f, 7.0f };
        for (int i = 0; i < offs.Length; i++)
        {
            Vector2 pos = center + offs[i];
            float rad = rads[i];
            FillBakedCircle(image, pos, rad + 1.6f, new Color(0.65f, 0.42f, 0.05f, 0.85f));
            FillBakedCircle(image, pos, rad, baseGolden);
            FillBakedCircle(image, pos + new Vector2(-rad * 0.15f, -rad * 0.15f), rad * 0.72f, baseGolden.Lightened(0.18f));
            FillBakedCircle(image, pos + new Vector2(-rad * 0.32f, -rad * 0.32f), rad * 0.28f, new Color(1, 1, 1, 0.85f));
        }
    }

    /// <summary>Staph golden cocci cluster (r=16).</summary>
    private static ImageTexture BuildStaphTexture()
    {
        const int size = 40;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        BakeStaphCluster(image, new Vector2(size * 0.5f, size * 0.5f), new Color(0.95f, 0.78f, 0.18f, 0.95f));
        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>Shielded staph: cluster plus fibrin microthrombi shimmer ring.</summary>
    private static ImageTexture BuildStaphShieldedTexture()
    {
        const int size = 48;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        BakeStaphCluster(image, center, new Color(0.95f, 0.78f, 0.18f, 0.95f));
        var ring = new Color(1.0f, 0.95f, 0.7f, 0.7f);
        for (int i = 0; i < 24; i++)
        {
            float ang = i * (Mathf.Tau / 24.0f);
            FillBakedCircle(image, center + Vector2.FromAngle(ang) * 18.0f, 1.5f, ring);
        }
        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>Pseudomonas pyocyanin rod + polar flagellum stub (r=20).</summary>
    private static ImageTexture BuildPseudomonasTexture()
    {
        const int size = 48;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        Vector2 center = new(size * 0.5f, size * 0.5f);
        var capsule = new Color(0.15f, 0.65f, 0.45f, 0.95f);
        var core = new Color(0.25f, 0.85f, 0.60f, 0.90f);
        DrawBakedLine(image, center + new Vector2(-18.7f, 0), center + new Vector2(18.7f, 0), capsule, 19);
        FillBakedCircle(image, center + new Vector2(18.7f, 0), 9.3f, capsule);
        FillBakedCircle(image, center + new Vector2(-18.7f, 0), 9.3f, capsule);
        DrawBakedLine(image, center + new Vector2(-14.7f, 0), center + new Vector2(14.7f, 0), core, 10);
        DrawBakedLine(image, center + new Vector2(-10.7f, -4), center + new Vector2(10.7f, -4), new Color(1, 1, 1, 0.7f), 2);
        Vector2 prev = center + new Vector2(-18.7f, 0);
        var flag = new Color(0.3f, 0.8f, 0.5f, 0.65f);
        for (int i = 1; i <= 6; i++)
        {
            float wave = Mathf.Sin(i * 0.8f) * 6.0f;
            Vector2 next = center + new Vector2(-18.7f - i * 6.7f, wave);
            DrawBakedLine(image, prev, next, flag, 2);
            prev = next;
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
