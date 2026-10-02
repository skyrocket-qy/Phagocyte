using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Player;

namespace Game.Tests;

// Pen cast->hit cost instrument: A (frozen payload field) vs B (hybrid
// fallback) vs C (live stat read), weighted by land-rate (hits < casts).
// Prints timings only; generous bounds, never gates. Uses real APIs.
public partial class TestPenBenchmark : TestHarness
{
    private const int Casts = 100000;
    private const int Hits = 30000;
    private const int Repeats = 5;

    // C-shape probe: identical layout minus the pen float, copy-cost only.
#pragma warning disable CS0649
    private struct PenlessPayload
    {
        public float RawDamage;
        public float CritChance;
        public float CritMultiplier;
        public float AilmentChance;
        public DamageType Type;
        public HitFlags Flags;
        public Team SourceFaction;
        public EffectSpec Effect0;
        public EffectSpec Effect1;
        public EffectSpec Effect2;
        public int EffectCount;
        public ulong AttackerId;
    }
#pragma warning restore CS0649

    private int _phase = 0;
    private int _framesWaited = 0;
    private bool _testDone = false;
    private PlayerActor? _attacker;
    private ActorStats? _stats;
    private readonly List<HitPayload> _flight = new();
    private readonly List<PenlessPayload> _flightPenless = new();

    public override void _Initialize()
    {
        Banner("STARTING PEN CAST->HIT PROFILING");
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;
        _framesWaited++;
        if (_framesWaited < 3)
            return false;
        _testDone = true;
        try
        {
            SetupAttacker();
            ProfileSpawnCopy();
            ProfileHitResolve();
            GD.Print(">>> PEN CAST->HIT PROFILING DONE <<<");
            Quit(0);
        }
        catch (System.Exception ex)
        {
            GD.PrintErr("[FAIL] TestPenBenchmark threw: ", ex);
            Quit(1);
        }
        return true;
    }

    private void SetupAttacker()
    {
        _attacker = new PlayerActor { GlobalPosition = new Vector2(300, 300) };
        _stats = new ActorStats { Name = "ActorStats" };
        _attacker.AddChild(_stats);
        _attacker.Stats = _stats;
        Root.AddChild(_attacker);
        _stats.AddModifier("armor_penetration", 0.25f, 0.0f);
        ulong id = _attacker.GetInstanceId();
        for (int i = 0; i < Casts; i++)
        {
            _flight.Add(DamageService.Snapshot(20.0f, armorPenetration: 0.25f, attackerId: id));
            _flightPenless.Add(new PenlessPayload { RawDamage = 20.0f, AttackerId = id });
        }
        GD.Print($"[PenBench] staged {Casts} casts.");
    }

    private static double MinMs(System.Action fn)
    {
        double best = double.MaxValue;
        for (int r = 0; r < Repeats; r++)
        {
            var sw = Stopwatch.StartNew();
            fn();
            sw.Stop();
            best = System.Math.Min(best, sw.Elapsed.TotalMilliseconds);
        }
        return best;
    }

    private void ProfileSpawnCopy()
    {
        float sink = 0.0f;
        double full = MinMs(() =>
        {
            var buf = new List<HitPayload>(Casts);
            foreach (var p in _flight)
                buf.Add(p);
            foreach (var p in buf)
                sink += p.RawDamage;
        });
        double slim = MinMs(() =>
        {
            var buf = new List<PenlessPayload>(Casts);
            foreach (var p in _flightPenless)
                buf.Add(p);
            foreach (var p in buf)
                sink += p.RawDamage;
        });
        GD.Print($"[PenBench] spawn-copy x{Casts}: full={full:F1}ms slim={slim:F1}ms sink={sink:F0} (delta={(full - slim):F2}ms).");
    }

    private void ProfileHitResolve()
    {
        float sink = 0.0f;
        var st = _stats!;
        double frozen = MinMs(() =>
        {
            float acc = 0.0f;
            for (int i = 0; i < Hits; i++)
                acc += _flight[i].ArmorPenetration;
            sink += acc;
        });
        double hybridHit = MinMs(() =>
        {
            float acc = 0.0f;
            for (int i = 0; i < Hits; i++)
            {
                float pen = _flight[i].ArmorPenetration;
                if (pen == 0.0f)
                    pen = st.GetStat("armor_penetration");
                acc += pen;
            }
            sink += acc;
        });
        double hybridMiss = MinMs(() =>
        {
            float acc = 0.0f;
            for (int i = 0; i < Hits; i++)
            {
                float pen = 0.0f;
                if (pen == 0.0f)
                    pen = st.GetStat("armor_penetration");
                acc += pen;
            }
            sink += acc;
        });
        double live = MinMs(() =>
        {
            float acc = 0.0f;
            for (int i = 0; i < Hits; i++)
                acc += st.GetStat("armor_penetration");
            sink += acc;
        });
        GD.Print($"[PenBench] hit-resolve x{Hits}: frozen={frozen:F2}ms hybridHit={hybridHit:F2}ms hybridMiss={hybridMiss:F2}ms live={live:F2}ms sink={sink:F2}.");
        GD.Print($"[PenBench] per-hit ns: frozen={frozen * 1e6 / Hits:F1} hybridHit={hybridHit * 1e6 / Hits:F1} hybridMiss={hybridMiss * 1e6 / Hits:F1} live={live * 1e6 / Hits:F1}.");
    }
}
