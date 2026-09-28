// StatusCore v1 — portable status/ailment engine.
// Copy notice: this file is intentionally dependency-free apart from the Godot
// SDK (Vector2/Mathf). It references NO game namespaces, so it can be copied
// verbatim into any Godot 4 Mono project. Origin: engine core.
// Games supply their own ailment set as data (see Configure); per-game wiring
// (damage routing, VFX, special interactions) lives in a thin Node adapter.
//
// Semantics (Path of Exile rules):
// - RefreshMax:      timer refreshes to max, magnitude takes max (ignite/bleed style).
// - StrongestWins:   weaker applications are ignored entirely, not even refreshed.
// - Independent:     every application is its own stack; max_stacks > 0 drops oldest.
// - Aggregation:     strongest slow wins, strongest amp wins (never multiplicative).
// - AmpAppliesToOwnDot (option): whether DamageTakenMultiplier scales core DoT output.
namespace Game.Combat;

using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>How re-applications of one ailment combine (PoE rules).</summary>
public enum StatusStackRule
{
    RefreshMax,
    StrongestWins,
    Independent
}

/// <summary>
/// One ailment definition. Magnitude means dps for the "dot" channel and a
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
    /// Optional hit VFX id (resolves to the game's VfxType in the adapter;
    /// empty = none). Plain string so the core stays dependency-free.
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
    public StatusDataException(string message) : base($"[StatusCore] {message}")
    {
    }
}

/// <summary>
/// Game-agnostic ailment engine: timers, stacking, channel aggregation, DoT.
/// Pure state + math; all host I/O (damage routing, movement query, VFX) is the adapter's job.
/// </summary>
public sealed class StatusController
{
    private const float MoveThresholdSqr = 0.1f;

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

    /// <summary>Load defs (fail fast). Safe to call once per controller lifetime.</summary>
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
                && !string.Equals(ch, "amp", StringComparison.OrdinalIgnoreCase))
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

    /// <summary>
    /// Apply an ailment. Negative magnitude/duration fall back to def defaults.
    /// Returns false for unknown ids (game adapter warns; never throws in combat).
    /// </summary>
    public bool Apply(string id, float magnitude = -1.0f, float duration = -1.0f)
    {
        if (!_slots.TryGetValue(id, out var slot))
            return false;
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
        return true;
    }

    /// <summary>
    /// Advance all timers; returns DoT dealt this frame (post move-mult, post amp flag).
    /// Matches legacy semantics: singletons deal on their expiry frame, but the amp
    /// flag reads post-decrement state (an expiring mark does not amp its last tick).
    /// </summary>
    public float Tick(float dt, Vector2 velocity)
    {
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

    private static float ApplyMoveMult(Slot slot, float dot, bool moving)
    {
        if (moving && slot.Def.MoveMultiplier > 1.0f)
            dot *= slot.Def.MoveMultiplier;
        return dot;
    }

    private static bool HasChannel(Slot slot, string channel)
    {
        foreach (var ch in slot.Def.Channels)
        {
            if (string.Equals(ch, channel, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
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

    /// <summary>Strongest active slow wins; 1.0 when nothing slows.</summary>
    public float SpeedMultiplier
    {
        get
        {
            float strongest = StrongestActive("slow");
            if (strongest <= 0.0f)
                return 1.0f;
            return Mathf.Clamp(1.0f - strongest, 0.1f, 1.0f);
        }
    }

    /// <summary>Strongest active amp wins; 1.0 when unmarked.</summary>
    public float DamageTakenMultiplier => 1.0f + StrongestActive("amp");

    public bool IsActive(string id)
    {
        if (!_slots.TryGetValue(id, out var slot))
            return false;
        if (slot.Rule == StatusStackRule.Independent)
            return slot.Stacks.Count > 0;
        return slot.Timer > 0.0f;
    }

    public float GetTimer(string id)
    {
        if (!_slots.TryGetValue(id, out var slot))
            return 0.0f;
        return Math.Max(0.0f, slot.Timer);
    }

    public int GetStackCount(string id)
    {
        if (!_slots.TryGetValue(id, out var slot))
            return 0;
        return slot.Stacks.Count;
    }

    public void Clear(string id)
    {
        if (!_slots.TryGetValue(id, out var slot))
            return;
        slot.Timer = 0.0f;
        slot.Magnitude = slot.Def.Magnitude;
        slot.Stacks.Clear();
    }

    public void ClearAll()
    {
        foreach (var slot in _slots.Values)
        {
            slot.Timer = 0.0f;
            slot.Magnitude = slot.Def.Magnitude;
            slot.Stacks.Clear();
        }
    }
}
