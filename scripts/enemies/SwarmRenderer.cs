using Godot;
using System;
using System.Collections.Generic;

namespace Game.Enemies;

/// <summary>GPU batch renderer using MultiMeshInstance2D for microscopic swarm enemies.</summary>
public partial class SwarmRenderer : Node2D
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
    private readonly List<EnemyActor> _scratch = new(512);
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

        RegisterSpecies("norovirus", 768, new Vector2(13.0f, 13.0f), SwarmTextureFactory.CreateTexture("norovirus"));
        RegisterSpecies("flu_drift", 192, new Vector2(24.0f, 24.0f), SwarmTextureFactory.CreateTexture("flu_drift"));
        RegisterSpecies("tb", 256, new Vector2(64.0f, 64.0f), SwarmTextureFactory.CreateTexture("tb"));
        RegisterSpecies("e_coli", 256, new Vector2(72.0f, 72.0f), SwarmTextureFactory.CreateTexture("e_coli"));
        RegisterSpecies("staph", 256, new Vector2(40.0f, 40.0f), SwarmTextureFactory.CreateTexture("staph"));
        RegisterSpecies("staph_shielded", 64, new Vector2(48.0f, 48.0f), SwarmTextureFactory.CreateTexture("staph_shielded"));
        RegisterSpecies("pseudomonas", 192, new Vector2(48.0f, 48.0f), SwarmTextureFactory.CreateTexture("pseudomonas"));
        RegisterSpecies("anthrax_bacillus", 256, new Vector2(72.0f, 72.0f), SwarmTextureFactory.CreateTexture("anthrax_bacillus"));
        RegisterSpecies("plasmodium_merozoite", 192, new Vector2(48.0f, 48.0f), SwarmTextureFactory.CreateTexture("plasmodium_merozoite"));
        RegisterSpecies("prion_fragment", 256, new Vector2(24.0f, 24.0f), SwarmTextureFactory.CreateTexture("prion_fragment"));
        RegisterSpecies("tachyzoite", 128, new Vector2(64.0f, 64.0f), SwarmTextureFactory.CreateTexture("tachyzoite"));
        RegisterSpecies("candida", 256, new Vector2(96.0f, 96.0f), SwarmTextureFactory.CreateTexture("candida"));
        RegisterSpecies("toxoplasma", 256, new Vector2(64.0f, 64.0f), SwarmTextureFactory.CreateTexture("toxoplasma"));
        RegisterSpecies("hiv", 256, new Vector2(32.0f, 32.0f), SwarmTextureFactory.CreateTexture("hiv"));
        RegisterSpecies("rabies", 256, new Vector2(40.0f, 40.0f), SwarmTextureFactory.CreateTexture("rabies"));
        RegisterSpecies("varicella_zoster", 256, new Vector2(40.0f, 40.0f), SwarmTextureFactory.CreateTexture("varicella_zoster"));
        RegisterSpecies("malignant_cell", 256, new Vector2(96.0f, 96.0f), SwarmTextureFactory.CreateTexture("malignant_cell"));
        RegisterSpecies("aspergillus", 256, new Vector2(64.0f, 64.0f), SwarmTextureFactory.CreateTexture("aspergillus"));
        RegisterSpecies("tetanus", 256, new Vector2(64.0f, 64.0f), SwarmTextureFactory.CreateTexture("tetanus"));
        RegisterSpecies("s_virus", 256, new Vector2(32.0f, 32.0f), SwarmTextureFactory.CreateTexture("s_virus"));
        RegisterSpecies("plasmodium", 256, new Vector2(72.0f, 72.0f), SwarmTextureFactory.CreateTexture("plasmodium"));
        RegisterSpecies("candida_retracted", 128, new Vector2(96.0f, 96.0f), SwarmTextureFactory.CreateTexture("candida_retracted"));
        RegisterSpecies("varicella_dormant", 128, new Vector2(40.0f, 40.0f), SwarmTextureFactory.CreateTexture("varicella_dormant"));
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
    private static string ResolveSpeciesId(EnemyActor enemy)
    {
        string? variant = enemy.BatchSpeciesOverride;
        if (!string.IsNullOrEmpty(variant))
            return variant;
        if (enemy.EnemyId == "staph" && enemy.ShieldCharges > 0)
            return "staph_shielded";
        return enemy.EnemyId;
    }

    /// <summary>
    /// Rewrites every batch transform/color from the active enemy list.
    /// Called once per rendered frame by GameRoot.
    /// </summary>
    public void Sync()
    {
        LastBatchedCount = 0;
        for (int i = 0; i < _species.Count; i++)
            _species[i].VisibleCount = 0;

        _scratch.Clear();
        foreach (var enemy in EnemyActor.ActiveEnemies)
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
}
