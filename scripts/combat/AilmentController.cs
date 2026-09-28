using Godot;
using System;
using System.Collections.Generic;
using Game.Core;
using Game.UI;
using Game.Combat;

namespace Game.Combat;

/// <summary>
/// Game adapter over the portable <see cref="StatusController"/> core:
/// owns the Node lifecycle, loads ailment defs from assets/data/ailments.json,
/// routes DoT into TakeDoTDamage and batches floating numbers. Game-specific
/// hooks (antigenic drift clears marked) stay here, not in the core.
/// </summary>
public partial class AilmentController : Node
{
    private static List<StatusDef>? _sharedDefs;
    private static StatusOptions? _sharedOptions;
    private static readonly object _defLock = new();
    /// <summary>First def carrying the slow channel (JSON owns which ailment "the slow" is).</summary>
    private static string _slowCarrierId = "";
    /// <summary>Parsed hit-VFX per def id (fail fast on unknown names at load).</summary>
    private static readonly Dictionary<string, VfxType> _vfxById = new(StringComparer.OrdinalIgnoreCase);

    private readonly StatusController _core = new();
    private bool _configured;

    // Visual tick accumulator (avoids spamming 60 floating numbers per sec)
    private float _visualTickTimer = 0.0f;
    private float _accumulatedVisualDot = 0.0f;

    /// <summary>Strongest active damage-taken amp wins; 1.0 when clean.</summary>
    public float DamageTakenMultiplier => _core.DamageTakenMultiplier;
    public float SpeedMultiplier => _core.SpeedMultiplier;
    /// <summary>True while any slow-channel ailment is active.</summary>
    public bool HasSlow => _core.SpeedMultiplier < 1.0f;
    /// <summary>Longest remaining timer across slow-channel defs (0 when clean).</summary>
    public float SlowTimer
    {
        get
        {
            EnsureConfigured();
            float longest = 0.0f;
            foreach (var def in _sharedDefs!)
            {
                if (HasChannel(def, "slow"))
                    longest = Mathf.Max(longest, _core.GetTimer(def.Id));
            }
            return longest;
        }
    }

    /// <summary>
    /// Generic apply by ailment id (ids live in assets/data/ailments.json, never
    /// in code). Negative args fall back to def defaults. Plays the def's hit
    /// VFX on success. Unknown ids warn (combat never throws).
    /// </summary>
    public bool Apply(string id, float magnitude = -1.0f, float duration = -1.0f)
    {
        EnsureConfigured();
        if (!_core.Apply(id, magnitude, duration))
        {
            GD.PushWarning($"[AilmentController] Unknown ailment '{id}'.");
            return false;
        }
        if (_vfxById.TryGetValue(id, out var vfx) && GetParent() is Node2D parent)
            VfxManager.Instance?.Play(vfx, parent.GlobalPosition);
        return true;
    }

    /// <summary>Generic slow by channel (routes to the slow-carrier def from JSON).</summary>
    public void ApplySlow(float duration = -1.0f, float slowPct = -1.0f)
    {
        EnsureConfigured();
        if (string.IsNullOrEmpty(_slowCarrierId))
        {
            GD.PushWarning("[AilmentController] No slow-channel ailment defined.");
            return;
        }
        Apply(_slowCarrierId, slowPct, duration);
    }

    public bool IsActive(string id)
    {
        EnsureConfigured();
        return _core.IsActive(id);
    }

    public float GetTimer(string id)
    {
        EnsureConfigured();
        return _core.GetTimer(id);
    }

    public int GetStackCount(string id)
    {
        EnsureConfigured();
        return _core.GetStackCount(id);
    }

    public void Clear(string id)
    {
        EnsureConfigured();
        _core.Clear(id);
    }

    /// <summary>Clears every def carrying a channel (e.g. antigenic drift clears "amp").</summary>
    public void ClearChannel(string channel)
    {
        EnsureConfigured();
        foreach (var def in _sharedDefs!)
        {
            if (HasChannel(def, channel))
                _core.Clear(def.Id);
        }
    }

    public void ClearAll()
    {
        EnsureConfigured();
        _core.ClearAll();
        _accumulatedVisualDot = 0f;
    }

    public override void _PhysicsProcess(double delta)
    {
        EnsureConfigured();
        float dt = (float)delta;

        Vector2 velocity = Vector2.Zero;
        if (GetParent() is Node2D parent2D)
        {
            var velProp = parent2D.Get("Velocity");
            if (velProp.VariantType == Variant.Type.Vector2)
                velocity = velProp.AsVector2();
        }

        float frameDot = _core.Tick(dt, velocity);

        // frameDot already carries move-mult and (per JSON flag) marked amp.
        if (frameDot > 0.0f)
        {
            ApplyDoTToParent(frameDot);

            // Accumulate for periodic floating text
            _accumulatedVisualDot += frameDot;
            _visualTickTimer += dt;
            if (_visualTickTimer >= 0.35f)
            {
                _visualTickTimer = 0.0f;
                if (_accumulatedVisualDot >= 1.0f && GetParent() is Node2D p)
                {
                    DamageNumberSpawner.ShowDamage(p.GlobalPosition, _accumulatedVisualDot, false);
                    _accumulatedVisualDot = 0.0f;
                }
            }
        }
    }

    private void ApplyDoTToParent(float damage)
    {
        var parent = GetParent();
        if (parent == null || !GodotObject.IsInstanceValid(parent) || parent.IsQueuedForDeletion())
            return;

        if (parent is IDamageable damageable)
        {
            damageable.TakeDoTDamage(damage);
        }
        else if (parent.HasMethod("TakeDoTDamage"))
        {
            parent.Call("TakeDoTDamage", damage);
        }
        else if (parent.HasMethod("take_dot_damage"))
        {
            parent.Call("take_dot_damage", damage);
        }
    }

    private void EnsureConfigured()
    {
        if (_configured)
            return;
        EnsureDefsLoaded();
        _core.Configure(_sharedDefs!, _sharedOptions!);
        _configured = true;
    }

    private static void EnsureDefsLoaded()
    {
        if (_sharedDefs != null)
            return;
        lock (_defLock)
        {
            if (_sharedDefs != null)
                return;
            try
            {
                var root = CatalogLoader.LoadObject(DataPaths.Ailments);
                int schema = CatalogLoader.GetInt(root, "schema", 0);
                if (schema != 1)
                    throw new DataLoadException(DataPaths.Ailments, $"Unsupported schema {schema} (expected 1).");

                var options = new StatusOptions
                {
                    SlowAggregation = CatalogLoader.GetString(root, "slow_aggregation", "strongest"),
                    AmpAppliesToOwnDot = CatalogLoader.GetBool(root, "amp_applies_to_own_dot", false)
                };

                var defs = new List<StatusDef>();
                if (!root.TryGetValue("ailments", out var listVar) || listVar.VariantType != Variant.Type.Array)
                    throw new DataLoadException(DataPaths.Ailments, "Missing 'ailments' array.");
                foreach (var item in listVar.AsGodotArray())
                {
                    if (item.VariantType != Variant.Type.Dictionary)
                        throw new DataLoadException(DataPaths.Ailments, "Ailment entries must be JSON objects.");
                    var d = item.AsGodotDictionary();
                    defs.Add(new StatusDef
                    {
                        Id = CatalogLoader.GetString(d, "id"),
                        Name = CatalogLoader.GetString(d, "name"),
                        Duration = CatalogLoader.GetFloat(d, "duration", 3.0f),
                        Magnitude = CatalogLoader.GetFloat(d, "magnitude", 0.0f),
                        Stack = CatalogLoader.GetString(d, "stack", "refresh_max"),
                        Channels = CatalogLoader.GetStringArray(d, "channels"),
                        MoveMultiplier = CatalogLoader.GetFloat(d, "move_multiplier", 1.0f),
                        MaxStacks = CatalogLoader.GetInt(d, "max_stacks", 0),
                        MinMagnitude = GetOptionalFloat(d, "min_magnitude"),
                        MaxMagnitude = GetOptionalFloat(d, "max_magnitude"),
                        Vfx = CatalogLoader.GetString(d, "vfx")
                    });
                }

                // Fail fast on invalid defs before any controller consumes them.
                new StatusController().Configure(defs, options);
                // Fail fast on unknown hit-VFX names; cache the slow carrier.
                _vfxById.Clear();
                _slowCarrierId = "";
                foreach (var def in defs)
                {
                    if (!string.IsNullOrEmpty(def.Vfx))
                    {
                        if (!Enum.TryParse<VfxType>(def.Vfx, true, out var vfx))
                            throw new DataLoadException(DataPaths.Ailments, $"Unknown vfx '{def.Vfx}' on ailment '{def.Id}'.");
                        _vfxById[def.Id] = vfx;
                    }
                    if (_slowCarrierId == "" && HasChannel(def, "slow"))
                        _slowCarrierId = def.Id;
                }
                _sharedDefs = defs;
                _sharedOptions = options;
            }
            catch (StatusDataException ex)
            {
                throw new DataLoadException(DataPaths.Ailments, ex.Message, ex);
            }
        }
    }

    private static bool HasChannel(StatusDef def, string channel)
    {
        foreach (var ch in def.Channels)
        {
            if (string.Equals(ch, channel, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static float GetOptionalFloat(Godot.Collections.Dictionary d, string key)
    {
        if (!d.TryGetValue(key, out var v))
            return float.NaN;
        return v.VariantType switch
        {
            Variant.Type.Int => (float)v.AsInt64(),
            Variant.Type.Float => v.AsSingle(),
            _ => float.NaN
        };
    }
}

