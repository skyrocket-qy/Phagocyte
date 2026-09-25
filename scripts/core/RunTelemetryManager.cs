using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Phagocyte.Combat;
using Phagocyte.Enemies;
using Phagocyte.UI;

namespace Phagocyte.Core;

/// <summary>
/// One over-threshold frame: wall-clock cost plus the population census and
/// the age of the last gameplay event, so a hitch can be attributed without
/// guesswork. Plain struct, copied by value into a ring buffer — never
/// allocated on the hot path.
/// </summary>
public struct FrameSpike
{
    public float RunTimeSec;
    public float FrameMs;
    public float PhysicsMs;
    public float ProcessMs;
    public int Fps;
    public int Enemies;
    public int Projectiles;
    public int DamageNumbers;
    public float WaveAgeSec;
    public float LevelUpAgeSec;
    public float AchievementAgeSec;
    public float BossAgeSec;
}

/// <summary>
/// Zero-allocation frame-spike flight recorder. Gameplay code stamps event
/// waterlines (<c>MarkWave</c> etc.); <see cref="RunTelemetryManager"/> copies
/// one <see cref="FrameSpike"/> per over-threshold frame into a 64-entry ring
/// and dumps JSON at run end. Hot path is a float compare plus struct copy;
/// all strings and file IO happen only at dump time.
/// </summary>
public static class FrameSpikeLog
{
    public const int Capacity = 64;

    private static readonly FrameSpike[] _ring = new FrameSpike[Capacity];
    private static int _head;
    private static ulong _runStartMsec;
    private static ulong _waveMsec;
    private static ulong _levelUpMsec;
    private static ulong _achievementMsec;
    private static ulong _bossMsec;
    private static bool _hasWave;
    private static bool _hasLevelUp;
    private static bool _hasAchievement;
    private static bool _hasBoss;

    public static int Count { get; private set; }

    public static void BeginRun()
    {
        _runStartMsec = Time.GetTicksMsec();
        _head = 0;
        Count = 0;
        _hasWave = _hasLevelUp = _hasAchievement = _hasBoss = false;
    }

    public static void MarkWave() { _waveMsec = Time.GetTicksMsec(); _hasWave = true; }
    public static void MarkLevelUp() { _levelUpMsec = Time.GetTicksMsec(); _hasLevelUp = true; }
    public static void MarkAchievement() { _achievementMsec = Time.GetTicksMsec(); _hasAchievement = true; }
    public static void MarkBoss() { _bossMsec = Time.GetTicksMsec(); _hasBoss = true; }

    private static float AgeSec(ulong markMsec, bool has, ulong nowMsec)
    {
        if (!has)
            return -1.0f;
        return (nowMsec - markMsec) / 1000.0f;
    }

    internal static void Record(float frameMs, float physicsMs, float processMs, int fps, int enemies, int projectiles, int damageNumbers)
    {
        ulong now = Time.GetTicksMsec();
        _ring[_head] = new FrameSpike
        {
            RunTimeSec = (now - _runStartMsec) / 1000.0f,
            FrameMs = frameMs,
            PhysicsMs = physicsMs,
            ProcessMs = processMs,
            Fps = fps,
            Enemies = enemies,
            Projectiles = projectiles,
            DamageNumbers = damageNumbers,
            WaveAgeSec = AgeSec(_waveMsec, _hasWave, now),
            LevelUpAgeSec = AgeSec(_levelUpMsec, _hasLevelUp, now),
            AchievementAgeSec = AgeSec(_achievementMsec, _hasAchievement, now),
            BossAgeSec = AgeSec(_bossMsec, _hasBoss, now),
        };
        _head = (_head + 1) % Capacity;
        if (Count < Capacity)
            Count++;
    }

    /// <summary>Oldest-first snapshot for the JSON dump (dump time only).</summary>
    public static System.Collections.Generic.List<Godot.Collections.Dictionary> ToDictionaries()
    {
        var list = new System.Collections.Generic.List<Godot.Collections.Dictionary>(Count);
        for (int i = 0; i < Count; i++)
        {
            int idx = (Count < Capacity ? i : (_head + i) % Capacity);
            var s = _ring[idx];
            list.Add(new Godot.Collections.Dictionary
            {
                { "run_time_sec", s.RunTimeSec },
                { "frame_ms", s.FrameMs },
                { "physics_ms", s.PhysicsMs },
                { "process_ms", s.ProcessMs },
                { "fps", s.Fps },
                { "enemies", s.Enemies },
                { "projectiles", s.Projectiles },
                { "damage_numbers", s.DamageNumbers },
                { "wave_age_sec", s.WaveAgeSec },
                { "levelup_age_sec", s.LevelUpAgeSec },
                { "achievement_age_sec", s.AchievementAgeSec },
                { "boss_age_sec", s.BossAgeSec },
            });
        }
        return list;
    }
}

/// <summary>
/// Telemetry manager recording in-run combat statistics:
/// weapon damage breakdown, damage taken, and defensive procs (evasion, block, life steal).
/// </summary>
public partial class RunTelemetryManager : Node
{
    public static RunTelemetryManager? Instance { get; private set; }

    public bool IsRunActive { get; private set; } = false;
    public float TotalDamageDealt { get; private set; } = 0.0f;
    public float TotalDamageTaken { get; private set; } = 0.0f;
    public int EvadedCount { get; private set; } = 0;
    public int BlockedCount { get; private set; } = 0;
    public float LifeStealHealed { get; private set; } = 0.0f;

    /// <summary>Total pathogens killed this run (ranged kills + engulfed kills).</summary>
    public int KillCount { get; private set; } = 0;

    /// <summary>Accumulated fixed base score of every killed pathogen (docs/record.md §4.1).</summary>
    public int KillScore { get; private set; } = 0;

    public Dictionary<string, float> SkillDamageMap { get; } = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonStore.SavePathSlot _spikePath = new("spike_log.json");

    /// <summary>Spike-log file path (overridable for save-isolated tests).</summary>
    public static string SpikeLogPath
    {
        get => _spikePath.Value;
        set => _spikePath.Value = value;
    }

    /// <summary>Single-frame wall-clock threshold that records a spike. Default 50ms.</summary>
    public float SpikeThresholdMs { get; set; } = 50.0f;

    public int SpikeCount => FrameSpikeLog.Count;

    public override void _Process(double delta)
    {
        if (!IsRunActive)
            return;
        float frameMs = (float)delta * 1000.0f;
        if (frameMs < SpikeThresholdMs)
            return;
        // Monitors and counts are read ONLY on the spike branch: the steady
        // state hot path stays a float compare plus an early return.
        float physicsMs = (float)Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0f;
        float processMs = (float)Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0f;
        FrameSpikeLog.Record(
            frameMs, physicsMs, processMs, (int)Engine.GetFramesPerSecond(),
            BaseEnemy.ActiveEnemies.Count,
            ProjectileManager.Instance?.ActiveCount ?? 0,
            DamageNumberSpawner.Instance?.ActiveNumbers ?? 0);
    }

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _ExitTree()
    {
        if (IsRunActive)
        {
            IsRunActive = false;
            DumpSpikeLog();
        }
        if (Instance == this)
        {
            Instance = null;
        }
        base._ExitTree();
    }

    public void StartRun()
    {
        IsRunActive = true;
        TotalDamageDealt = 0.0f;
        TotalDamageTaken = 0.0f;
        EvadedCount = 0;
        BlockedCount = 0;
        LifeStealHealed = 0.0f;
        KillCount = 0;
        KillScore = 0;
        SkillDamageMap.Clear();
        FrameSpikeLog.BeginRun();
    }

    public void EndRun()
    {
        IsRunActive = false;
        DumpSpikeLog();
    }

    /// <summary>
    /// Writes the run's frame-spike ring to the spike log (always written,
    /// even when empty, so a missing file means the logger never ran).
    /// Dump time only — never called from the frame hot path.
    /// </summary>
    public void DumpSpikeLog()
    {
        var spikes = new Godot.Collections.Array();
        foreach (var d in FrameSpikeLog.ToDictionaries())
            spikes.Add(d);
        var payload = new Godot.Collections.Dictionary
        {
            { "spike_threshold_ms", SpikeThresholdMs },
            { "spike_count", FrameSpikeLog.Count },
            { "performance_mode", SettingsManager.PerformanceMode },
            { "spikes", spikes },
        };
        using var file = FileAccess.Open(SpikeLogPath, FileAccess.ModeFlags.Write);
        if (file != null)
            file.StoreString(Json.Stringify(payload, "  "));
    }

    public void RecordDamageDealt(string? skillId, float amount)
    {
        if (!IsRunActive || amount <= 0.0f) return;

        TotalDamageDealt += amount;
        string key = string.IsNullOrEmpty(skillId) ? "basic_attack" : skillId;

        if (!SkillDamageMap.ContainsKey(key))
        {
            SkillDamageMap[key] = 0.0f;
        }
        SkillDamageMap[key] += amount;
    }

    public void RecordDamageTaken(float amount)
    {
        if (!IsRunActive || amount <= 0.0f) return;
        TotalDamageTaken += amount;
    }

    public void RecordEvaded()
    {
        if (!IsRunActive) return;
        EvadedCount++;
    }

    public void RecordBlocked()
    {
        if (!IsRunActive) return;
        BlockedCount++;
    }

    public void RecordLifeSteal(float amount)
    {
        if (!IsRunActive || amount <= 0.0f) return;
        LifeStealHealed += amount;
    }

    /// <summary>
    /// Log a pathogen kill. Score is the enemy's fixed base score
    /// (0 for farming-neutral hazards such as senescent RBCs).
    /// </summary>
    public void RecordKill(int baseScore)
    {
        if (!IsRunActive) return;
        KillCount++;
        KillScore += Math.Max(0, baseScore);
    }

    public List<(string SkillId, float Damage, float Pct)> GetTopSkills(int count = 4)
    {
        float total = TotalDamageDealt > 0.0f ? TotalDamageDealt : 1.0f;
        return SkillDamageMap
            .OrderByDescending(kv => kv.Value)
            .Take(count)
            .Select(kv => (kv.Key, kv.Value, (kv.Value / total) * 100.0f))
            .ToList();
    }

    public Godot.Collections.Dictionary GetTelemetryDictionary()
    {
        var dict = new Godot.Collections.Dictionary
        {
            { "total_damage_dealt", TotalDamageDealt },
            { "total_damage_taken", TotalDamageTaken },
            { "evaded_count", EvadedCount },
            { "blocked_count", BlockedCount },
            { "lifesteal_healed", LifeStealHealed },
            { "kills", KillCount },
            { "kill_score", KillScore }
        };

        var skillDict = new Godot.Collections.Dictionary();
        foreach (var (k, v) in SkillDamageMap)
        {
            skillDict[k] = v;
        }
        dict["skills"] = skillDict;

        return dict;
    }
}
