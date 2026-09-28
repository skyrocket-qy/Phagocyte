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
    public const string BurnId = "oxidative_burn";
    public const string AgglutinationId = "agglutination";
    public const string MarkedId = "opsonization";
    public const string LeakId = "membrane_leak";
    public const string EndotoxinId = "endotoxin";

    private static List<StatusDef>? _sharedDefs;
    private static StatusOptions? _sharedOptions;
    private static readonly object _defLock = new();

    private readonly StatusController _core = new();
    private bool _configured;

    // Visual tick accumulator (avoids spamming 60 floating numbers per sec)
    private float _visualTickTimer = 0.0f;
    private float _accumulatedVisualDot = 0.0f;

    public bool IsOxidized => _core.IsActive(BurnId);
    public bool IsAgglutinated => _core.IsActive(AgglutinationId);
    public bool IsMarked => _core.IsActive(MarkedId);
    public bool IsLeaking => _core.IsActive(LeakId);
    public bool IsToxic => _core.IsActive(EndotoxinId);

    public float MarkationMultiplier => _core.DamageTakenMultiplier;
    public float SpeedMultiplier => _core.SpeedMultiplier;

    public float BurnTimer => _core.GetTimer(BurnId);
    public float AgglutinationTimer => _core.GetTimer(AgglutinationId);
    public float MarkationTimer => _core.GetTimer(MarkedId);
    public float LeakTimer => _core.GetTimer(LeakId);
    public int EndotoxinStackCount => _core.GetStackCount(EndotoxinId);

    /// <summary>
    /// Applies or refreshes ROS Oxidative Burn DoT. Negative args fall back to JSON defaults.
    /// </summary>
    public void ApplyOxidativeBurn(float dps, float duration = -1.0f)
    {
        EnsureConfigured();
        if (!_core.Apply(BurnId, dps, duration))
            GD.PushWarning($"[AilmentController] Unknown ailment '{BurnId}'.");
    }

    /// <summary>
    /// Applies or refreshes Agglutination slow. Stronger slow overrides weaker (PoE strongest-wins).
    /// </summary>
    public void ApplyAgglutination(float duration = -1.0f, float slowPct = -1.0f)
    {
        EnsureConfigured();
        if (!_core.Apply(AgglutinationId, slowPct, duration))
            GD.PushWarning($"[AilmentController] Unknown ailment '{AgglutinationId}'.");
    }

    /// <summary>
    /// Marks the target as vulnerable, amplifying all damage taken.
    /// </summary>
    public void ApplyMarkation(float duration = -1.0f, float ampPct = -1.0f)
    {
        EnsureConfigured();
        if (!_core.Apply(MarkedId, ampPct, duration))
            GD.PushWarning($"[AilmentController] Unknown ailment '{MarkedId}'.");
    }

    /// <summary>
    /// Punctures target membrane, causing leakage that intensifies 3x when moving.
    /// </summary>
    public void ApplyMembraneLeak(float dps, float duration = -1.0f)
    {
        EnsureConfigured();
        if (!_core.Apply(LeakId, dps, duration))
            GD.PushWarning($"[AilmentController] Unknown ailment '{LeakId}'.");
    }

    /// <summary>
    /// Adds an independent Endotoxin poison stack.
    /// </summary>
    public void ApplyEndotoxin(float dps, float duration = -1.0f)
    {
        EnsureConfigured();
        if (!_core.Apply(EndotoxinId, dps, duration))
            GD.PushWarning($"[AilmentController] Unknown ailment '{EndotoxinId}'.");
    }

    public void ClearAll()
    {
        EnsureConfigured();
        _core.ClearAll();
        _accumulatedVisualDot = 0f;
    }

    /// <summary>
    /// Antigenic drift (docs/endgame.md §4): strips the specific-vulnerability
    /// mark (marked) while leaving all other ailments untouched.
    /// </summary>
    public void ClearMarkation()
    {
        EnsureConfigured();
        _core.Clear(MarkedId);
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
                        MaxMagnitude = GetOptionalFloat(d, "max_magnitude")
                    });
                }

                // Fail fast on invalid defs before any controller consumes them.
                new StatusController().Configure(defs, options);
                _sharedDefs = defs;
                _sharedOptions = options;
            }
            catch (StatusDataException ex)
            {
                throw new DataLoadException(DataPaths.Ailments, ex.Message, ex);
            }
        }
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

