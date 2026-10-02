using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Game.Core;
using Game.UI;

namespace Game.Combat;

/// <summary>How re-applications of one status effect combine (PoE rules).</summary>
public enum StatusStackRule
{
    RefreshMax,
    StrongestWins,
    Independent
}

/// <summary>
/// One status definition. Magnitude means dps for the "dot" channel and a
/// 0-1 fraction for "slow"/"amp". min/max_magnitude are optional clamps
/// (NaN = unclamped). max_stacks &lt;= 0 means unlimited stacks.
/// </summary>
public sealed class StatusDef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("duration")]
    public float Duration { get; set; } = 3.0f;

    [JsonPropertyName("magnitude")]
    public float Magnitude { get; set; } = 0.0f;

    [JsonPropertyName("stack")]
    public string Stack { get; set; } = "refresh_max";

    [JsonPropertyName("channels")]
    public string[] Channels { get; set; } = Array.Empty<string>();

    [JsonPropertyName("move_multiplier")]
    public float MoveMultiplier { get; set; } = 1.0f;

    [JsonPropertyName("max_stacks")]
    public int MaxStacks { get; set; } = 0;

    [JsonPropertyName("min_magnitude")]
    public float MinMagnitude { get; set; } = float.NaN;

    [JsonPropertyName("max_magnitude")]
    public float MaxMagnitude { get; set; } = float.NaN;

    /// <summary>
    /// Optional hit VFX id (resolves to the game's VfxType; empty = none).
    /// </summary>
    [JsonPropertyName("vfx")]
    public string Vfx { get; set; } = "";
}

/// <summary>Core-level options, supplied alongside the defs (data, not code).</summary>
public sealed class StatusOptions
{
    /// <summary>Only "strongest" is supported: strongest slow/amp wins.</summary>
    public string SlowAggregation { get; set; } = "strongest";

    /// <summary>Whether DamageTakenMultiplier scales this controller's own DoT output.</summary>
    public bool AmpAppliesToOwnDot { get; set; } = false;
}

/// <summary>Thrown when defs fail validation. Games typically wrap this in their own load exception.</summary>
public sealed class StatusDataException : Exception
{
    public StatusDataException(string message) : base($"[StatusController] {message}")
    {
    }
}

/// <summary>
/// Status and ailment controller: manages active status effects, timers, stacking,
/// channel aggregation, DoT damage, floating numbers, and hit VFX on actors.
/// </summary>
public partial class StatusController : Node
{
    private const float MoveThresholdSqr = 0.1f;

    private static List<StatusDef>? _sharedDefs;
    private static StatusOptions? _sharedOptions;
    private static readonly object _defLock = new();
    private static string _slowCarrierId = "";
    private static readonly Dictionary<string, VfxType> _vfxById = new(StringComparer.OrdinalIgnoreCase);

    private sealed class Slot
    {
        public StatusDef Def = new();
        public StatusStackRule Rule;
        public float Timer;
        public float Magnitude;
        public readonly List<(float Mag, float Timer)> Stacks = new();
    }

    private readonly Dictionary<string, Slot> _slots = new(StringComparer.OrdinalIgnoreCase);
    private StatusOptions _options = new();
    private bool _configured;

    // Visual tick accumulator (avoids spamming 60 floating numbers per sec)
    private float _visualTickTimer = 0.0f;
    private float _accumulatedVisualDot = 0.0f;

    /// <summary>Strongest active damage-taken amp wins; 1.0 when unmarked.</summary>
    public float DamageTakenMultiplier => 1.0f + StrongestActive("amp");

    /// <summary>Strongest active slow wins; 1.0 when clean.</summary>
    public float SpeedMultiplier
    {
        get
        {
            EnsureConfigured();
            float strongest = StrongestActive("slow");
            if (strongest <= 0.0f)
                return 1.0f;
            return Mathf.Clamp(1.0f - strongest, 0.1f, 1.0f);
        }
    }

    /// <summary>True while any slow-channel ailment is active.</summary>
    public bool HasSlow => SpeedMultiplier < 1.0f;

    /// <summary>Longest remaining timer across slow-channel defs (0 when clean).</summary>
    public float SlowTimer
    {
        get
        {
            EnsureConfigured();
            float longest = 0.0f;
            foreach (var slot in _slots.Values)
            {
                if (HasChannel(slot, "slow"))
                    longest = Math.Max(longest, Math.Max(0.0f, slot.Timer));
            }
            return longest;
        }
    }

    /// <summary>True while any stun-channel ailment is active.</summary>
    public bool IsStunned
    {
        get
        {
            EnsureConfigured();
            foreach (var slot in _slots.Values)
            {
                if (HasChannel(slot, "stun") && slot.Timer > 0.0f)
                    return true;
            }
            return false;
        }
    }

    /// <summary>Load defs (fail fast). Safe to call directly for custom/test setups.</summary>
    public void Configure(IEnumerable<StatusDef> defs, StatusOptions? options = null)
    {
        if (defs == null)
            throw new StatusDataException("Defs collection is null.");
        _options = options ?? new StatusOptions();
        if (!string.Equals(_options.SlowAggregation, "strongest", StringComparison.OrdinalIgnoreCase))
            throw new StatusDataException($"Unsupported slow_aggregation '{_options.SlowAggregation}' (only 'strongest').");

        _slots.Clear();
        int count = 0;
        foreach (var def in defs)
        {
            ValidateDef(def);
            if (_slots.ContainsKey(def.Id))
                throw new StatusDataException($"Duplicate def id '{def.Id}'.");
            var rule = ParseRule(def);
            _slots[def.Id] = new Slot
            {
                Def = def,
                Rule = rule,
                Timer = 0.0f,
                Magnitude = def.Magnitude
            };
            count++;
        }
        if (count == 0)
            throw new StatusDataException("Defs collection is empty.");
        _configured = true;
    }

    /// <summary>
    /// Generic apply by status id. Negative magnitude/duration fall back to def defaults.
    /// Plays the def's hit VFX on success. Unknown ids warn (combat never throws).
    /// </summary>
    public bool Apply(string id, float magnitude = -1.0f, float duration = -1.0f)
    {
        EnsureConfigured();
        if (!_slots.TryGetValue(id, out var slot))
        {
            GD.PushWarning($"[StatusController] Unknown status '{id}'.");
            return false;
        }

        float mag = magnitude < 0.0f ? slot.Def.Magnitude : magnitude;
        float dur = duration < 0.0f ? slot.Def.Duration : duration;
        if (!float.IsNaN(slot.Def.MinMagnitude))
            mag = Math.Max(mag, slot.Def.MinMagnitude);
        if (!float.IsNaN(slot.Def.MaxMagnitude))
            mag = Math.Min(mag, slot.Def.MaxMagnitude);

        switch (slot.Rule)
        {
        case StatusStackRule.RefreshMax:
            slot.Timer = Math.Max(slot.Timer, dur);
            slot.Magnitude = Math.Max(slot.Magnitude, mag);
            break;
        case StatusStackRule.StrongestWins:
            if (slot.Timer <= 0.0f || mag >= slot.Magnitude)
            {
                slot.Magnitude = mag;
                slot.Timer = Math.Max(slot.Timer, dur);
            }
            break;
        case StatusStackRule.Independent:
            if (slot.Def.MaxStacks > 0 && slot.Stacks.Count >= slot.Def.MaxStacks)
                slot.Stacks.RemoveAt(0);
            slot.Stacks.Add((mag, dur));
            break;
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
            GD.PushWarning("[StatusController] No slow-channel status defined.");
            return;
        }
        Apply(_slowCarrierId, slowPct, duration);
    }

    /// <summary>
    /// Advance all timers; returns DoT dealt this frame (post move-mult, post amp flag).
    /// Matches legacy semantics: singletons deal on their expiry frame, but the amp
    /// flag reads post-decrement state (an expiring mark does not amp its last tick).
    /// </summary>
    public float Tick(float dt, Vector2 velocity)
    {
        EnsureConfigured();
        bool moving = velocity.LengthSquared() > MoveThresholdSqr;
        float frameDot = 0.0f;

        foreach (var slot in _slots.Values)
        {
            if (slot.Rule == StatusStackRule.Independent)
            {
                if (HasChannel(slot, "dot") && slot.Stacks.Count > 0)
                {
                    float stackDps = 0.0f;
                    for (int i = slot.Stacks.Count - 1; i >= 0; i--)
                    {
                        var (mag, timer) = slot.Stacks[i];
                        timer -= dt;
                        if (timer <= 0.0f)
                            slot.Stacks.RemoveAt(i);
                        else
                        {
                            slot.Stacks[i] = (mag, timer);
                            stackDps += mag;
                        }
                    }
                    frameDot += ApplyMoveMult(slot, stackDps * dt, moving);
                }
                continue;
            }

            if (slot.Timer <= 0.0f)
                continue;
            if (HasChannel(slot, "dot"))
                frameDot += ApplyMoveMult(slot, slot.Magnitude * dt, moving);
            slot.Timer -= dt;
            if (slot.Timer <= 0.0f)
                slot.Magnitude = slot.Def.Magnitude;
        }

        float strongestAmp = StrongestActive("amp");
        if (frameDot > 0.0f && _options.AmpAppliesToOwnDot && strongestAmp > 0.0f)
            frameDot *= 1.0f + strongestAmp;
        return frameDot;
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

        float frameDot = Tick(dt, velocity);

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
    }

    public bool IsActive(string id)
    {
        EnsureConfigured();
        if (!_slots.TryGetValue(id, out var slot))
            return false;
        if (slot.Rule == StatusStackRule.Independent)
            return slot.Stacks.Count > 0;
        return slot.Timer > 0.0f;
    }

    public float GetTimer(string id)
    {
        EnsureConfigured();
        if (!_slots.TryGetValue(id, out var slot))
            return 0.0f;
        return Math.Max(0.0f, slot.Timer);
    }

    public int GetStackCount(string id)
    {
        EnsureConfigured();
        if (!_slots.TryGetValue(id, out var slot))
            return 0;
        return slot.Stacks.Count;
    }

    public void Clear(string id)
    {
        EnsureConfigured();
        if (!_slots.TryGetValue(id, out var slot))
            return;
        slot.Timer = 0.0f;
        slot.Magnitude = slot.Def.Magnitude;
        slot.Stacks.Clear();
    }

    /// <summary>Clears every def carrying a channel (e.g. antigenic drift clears "amp").</summary>
    public void ClearChannel(string channel)
    {
        EnsureConfigured();
        foreach (var slot in _slots.Values)
        {
            if (HasChannel(slot, channel))
                Clear(slot.Def.Id);
        }
    }

    public bool IsDotChannel(string id)
    {
        EnsureConfigured();
        if (!_slots.TryGetValue(id, out var slot))
            return false;
        return HasChannel(slot, "dot");
    }

    public void ClearAll()
    {
        EnsureConfigured();
        foreach (var slot in _slots.Values)
        {
            slot.Timer = 0.0f;
            slot.Magnitude = slot.Def.Magnitude;
            slot.Stacks.Clear();
        }
        _accumulatedVisualDot = 0.0f;
    }

    private void EnsureConfigured()
    {
        if (_configured)
            return;
        EnsureDefsLoaded();
        Configure(_sharedDefs!, _sharedOptions!);
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

                // Fail fast on unknown hit-VFX names; cache the slow carrier.
                _vfxById.Clear();
                _slowCarrierId = "";
                foreach (var def in defs)
                {
                    ValidateDef(def);
                    if (!string.IsNullOrEmpty(def.Vfx))
                    {
                        if (!Enum.TryParse<VfxType>(def.Vfx, true, out var vfx))
                            throw new DataLoadException(DataPaths.Ailments, $"Unknown vfx '{def.Vfx}' on status '{def.Id}'.");
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

    private static void ValidateDef(StatusDef def)
    {
        if (def == null)
            throw new StatusDataException("Null def entry.");
        if (string.IsNullOrWhiteSpace(def.Id))
            throw new StatusDataException("Def with empty id.");
        if (def.Duration <= 0.0f)
            throw new StatusDataException($"'{def.Id}': duration must be > 0.");
        if (def.Channels == null || def.Channels.Length == 0)
            throw new StatusDataException($"'{def.Id}': at least one channel required.");
        foreach (var ch in def.Channels)
        {
            if (!string.Equals(ch, "dot", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(ch, "slow", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(ch, "amp", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(ch, "stun", StringComparison.OrdinalIgnoreCase))
                throw new StatusDataException($"'{def.Id}': unknown channel '{ch}'.");
        }
        bool hasMin = !float.IsNaN(def.MinMagnitude);
        bool hasMax = !float.IsNaN(def.MaxMagnitude);
        if (hasMin && hasMax && def.MinMagnitude > def.MaxMagnitude)
            throw new StatusDataException($"'{def.Id}': min_magnitude > max_magnitude.");
        if (def.MoveMultiplier < 1.0f)
            throw new StatusDataException($"'{def.Id}': move_multiplier must be >= 1.");
    }

    private static StatusStackRule ParseRule(StatusDef def)
    {
        string s = (def.Stack ?? "").Trim().ToLowerInvariant();
        return s switch
        {
            "refresh_max" => StatusStackRule.RefreshMax,
            "strongest_wins" => StatusStackRule.StrongestWins,
            "independent" => StatusStackRule.Independent,
            _ => throw new StatusDataException($"'{def.Id}': unknown stack rule '{def.Stack}'.")
        };
    }

    private static float ApplyMoveMult(Slot slot, float dot, bool moving)
    {
        if (moving && slot.Def.MoveMultiplier > 1.0f)
            dot *= slot.Def.MoveMultiplier;
        return dot;
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

    private static bool HasChannel(Slot slot, string channel)
    {
        return HasChannel(slot.Def, channel);
    }

    private float StrongestActive(string channel)
    {
        float strongest = 0.0f;
        foreach (var slot in _slots.Values)
        {
            if (slot.Timer > 0.0f && slot.Rule != StatusStackRule.Independent && HasChannel(slot, channel))
                strongest = Math.Max(strongest, slot.Magnitude);
        }
        return strongest;
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
