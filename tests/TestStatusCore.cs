using Godot;
using System;
using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;
using Game.Combat;

namespace Game.Tests;

/// <summary>
/// Verifies the portable StatusCore engine (stacking rules, aggregation, move
/// mult, amp flag, validation) and the JSON loader behind AilmentController.
/// </summary>
[TestSuite]
public partial class TestStatusCore : TestHarness
{
    private int _phase = 0;

    public override void _Initialize()
    {
        Banner("STARTING STATUS CORE VERIFICATION");
    }

    public override bool _Process(double delta)
    {
        try
        {
            switch (_phase)
            {
            case 0:
                _phase++;
                return false; // let the scene tree settle
            case 1:
                RunStackingTests();
                _phase++;
                return false;
            case 2:
                RunAggregationAndFlagTests();
                _phase++;
                return false;
            case 3:
                RunValidationTests();
                _phase++;
                return false;
            case 4:
                RunLoaderTests();
                _phase++;
                return false;
            default:
                GD.Print("==================================================================");
                GD.Print(">>> ALL STATUS CORE TESTS PASSED SUCCESSFULLY! <<<");
                GD.Print("==================================================================");
                Quit(0);
                return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestStatusCore threw: ", ex);
            Quit(1);
            return true;
        }
    }

    private static StatusController BuildCore(bool ampOwnDot = true)
    {
        var core = new StatusController();
        core.Configure(new List<StatusDef>
        {
            new() { Id = "burn", Duration = 3.0f, Stack = "refresh_max", Channels = new[] { "dot" } },
            new() { Id = "slow_a", Duration = 2.0f, Magnitude = 0.40f, Stack = "strongest_wins", Channels = new[] { "slow" } },
            new() { Id = "slow_b", Duration = 5.0f, Magnitude = 0.25f, Stack = "strongest_wins", Channels = new[] { "slow" } },
            new() { Id = "amp", Duration = 4.0f, Magnitude = 0.30f, Stack = "refresh_max", Channels = new[] { "amp" } },
            new() { Id = "leak", Duration = 3.0f, Stack = "refresh_max", Channels = new[] { "dot" }, MoveMultiplier = 3.0f },
            new() { Id = "stacks", Duration = 4.0f, Stack = "independent", Channels = new[] { "dot" }, MaxStacks = 2 }
        }, new StatusOptions { AmpAppliesToOwnDot = ampOwnDot });
        return core;
    }

    private void RunStackingTests()
    {
        var core = BuildCore();

        // RefreshMax: dps takes max, timer refreshes to max.
        AssertThat(core.Apply("burn", 10.0f, 2.0f)).IsTrue();
        AssertThat(core.Tick(1.0f, Vector2.Zero)).IsEqualApprox(10.0f, 0.01f);
        core.Apply("burn", 5.0f, 5.0f);
        AssertThat(core.GetTimer("burn")).IsEqualApprox(5.0f, 0.01f);
        AssertThat(core.Tick(1.0f, Vector2.Zero)).IsEqualApprox(10.0f, 0.01f);

        // StrongestWins: weaker application ignored entirely (timer untouched).
        AssertThat(core.Apply("slow_a", 0.40f, 2.0f)).IsTrue();
        AssertThat(core.SpeedMultiplier).IsEqualApprox(0.60f, 0.01f);
        core.Apply("slow_a", 0.20f, 9.0f);
        AssertThat(core.SpeedMultiplier).IsEqualApprox(0.60f, 0.01f);
        AssertThat(core.GetTimer("slow_a")).IsEqualApprox(2.0f, 0.01f);
        core.Apply("slow_a", 0.50f, 1.0f);
        AssertThat(core.SpeedMultiplier).IsEqualApprox(0.50f, 0.01f);

        // Independent + cap: third stack drops the oldest.
        core.Clear("burn");
        core.Apply("stacks", 1.0f, 9.0f);
        core.Apply("stacks", 2.0f, 9.0f);
        core.Apply("stacks", 3.0f, 9.0f);
        AssertThat(core.GetStackCount("stacks")).IsEqual(2);
        AssertThat(core.Tick(1.0f, Vector2.Zero)).IsEqualApprox(5.0f, 0.01f);

        // Unknown id: false, no throw, queries stay quiet.
        AssertThat(core.Apply("nope", 1.0f, 1.0f)).IsFalse();
        AssertThat(core.IsActive("nope")).IsFalse();
        AssertThat(core.GetTimer("nope")).IsEqual(0.0f);

        GD.Print("[PASS] StatusCore stacking rules (refresh_max / strongest_wins / independent+cap).");
    }

    private void RunAggregationAndFlagTests()
    {
        var core = BuildCore(ampOwnDot: true);

        // Aggregation: strongest slow governs; expiry hands over to the weaker.
        core.Apply("slow_a", 0.40f, 1.0f);
        core.Apply("slow_b", 0.25f, 9.0f);
        AssertThat(core.SpeedMultiplier).IsEqualApprox(0.60f, 0.01f);
        core.Tick(1.01f, Vector2.Zero);
        AssertThat(core.IsActive("slow_a")).IsFalse();
        AssertThat(core.SpeedMultiplier).IsEqualApprox(0.75f, 0.01f);

        // Move multiplier: stationary vs moving leak.
        core.Apply("leak", 5.0f, 2.0f);
        AssertThat(core.Tick(1.0f, Vector2.Zero)).IsEqualApprox(5.0f, 0.01f);
        AssertThat(core.Tick(1.0f, new Vector2(100.0f, 0.0f))).IsEqualApprox(15.0f, 0.01f);

        // Amp flag ON: own DoT amplified; property reads 1.30 either way.
        var amped = BuildCore(ampOwnDot: true);
        amped.Apply("amp", 0.30f, 2.0f);
        amped.Apply("burn", 10.0f, 2.0f);
        AssertThat(amped.DamageTakenMultiplier).IsEqualApprox(1.30f, 0.01f);
        AssertThat(amped.Tick(1.0f, Vector2.Zero)).IsEqualApprox(13.0f, 0.05f);

        // Amp flag OFF (Vistrace behavior): own DoT unamped.
        var raw = BuildCore(ampOwnDot: false);
        raw.Apply("amp", 0.30f, 2.0f);
        raw.Apply("burn", 10.0f, 2.0f);
        AssertThat(raw.DamageTakenMultiplier).IsEqualApprox(1.30f, 0.01f);
        AssertThat(raw.Tick(1.0f, Vector2.Zero)).IsEqualApprox(10.0f, 0.05f);

        GD.Print("[PASS] StatusCore aggregation, move multiplier and amp flag.");
    }

    private void RunValidationTests()
    {
        // Empty defs.
        AssertThat(WrapConfigure(new List<StatusDef>(), null)).IsTrue();

        // Duplicate id.
        AssertThat(WrapConfigure(new List<StatusDef>
        {
            new() { Id = "x", Duration = 1.0f, Channels = new[] { "dot" } },
            new() { Id = "X", Duration = 1.0f, Channels = new[] { "dot" } }
        }, null)).IsTrue();

        // Bad stack rule / channel / duration / clamps / move mult / aggregation.
        AssertThat(WrapConfigure(OneDef(stack: "bogus"), null)).IsTrue();
        AssertThat(WrapConfigure(OneDef(channels: new[] { "bogus" }), null)).IsTrue();
        AssertThat(WrapConfigure(OneDef(duration: 0.0f), null)).IsTrue();
        AssertThat(WrapConfigure(OneDef(minMag: 0.5f, maxMag: 0.2f), null)).IsTrue();
        AssertThat(WrapConfigure(OneDef(moveMult: 0.5f), null)).IsTrue();
        AssertThat(WrapConfigure(OneDef(), new StatusOptions { SlowAggregation = "multiplicative" })).IsTrue();

        GD.Print("[PASS] StatusCore validation fails fast on bad defs.");
    }

    private static List<StatusDef> OneDef(
        string stack = "refresh_max", string[]? channels = null, float duration = 1.0f,
        float minMag = float.NaN, float maxMag = float.NaN, float moveMult = 1.0f)
    {
        return new List<StatusDef>
        {
            new()
            {
                Id = "x", Duration = duration, Stack = stack,
                Channels = channels ?? new[] { "dot" },
                MoveMultiplier = moveMult, MinMagnitude = minMag, MaxMagnitude = maxMag
            }
        };
    }

    private static bool WrapConfigure(List<StatusDef> defs, StatusOptions? options)
    {
        try
        {
            new StatusController().Configure(defs, options);
            return false; // expected a throw
        }
        catch (StatusDataException)
        {
            return true;
        }
    }

    private void RunLoaderTests()
    {
        // The real assets/data/ailments.json loads through the generic adapter.
        var ailment = new AilmentController();
        ailment.Apply("opsonization");
        AssertThat(ailment.IsActive("opsonization")).IsTrue();
        AssertThat(ailment.GetTimer("opsonization")).IsEqualApprox(4.0f, 0.01f);
        AssertThat(ailment.DamageTakenMultiplier).IsEqualApprox(1.30f, 0.01f);

        ailment.ApplySlow();
        AssertThat(ailment.HasSlow).IsTrue();
        AssertThat(ailment.SpeedMultiplier).IsEqualApprox(0.60f, 0.01f);

        ailment.Apply("oxidative_burn", 10.0f);
        ailment.Apply("membrane_leak", 5.0f);
        ailment.Apply("endotoxin", 2.0f, 2.0f);
        AssertThat(ailment.IsActive("oxidative_burn")).IsTrue();
        AssertThat(ailment.IsActive("membrane_leak")).IsTrue();
        AssertThat(ailment.IsActive("endotoxin")).IsTrue();
        AssertThat(ailment.GetStackCount("endotoxin")).IsEqual(1);

        ailment.ClearChannel("amp");
        AssertThat(ailment.IsActive("opsonization")).IsFalse();
        ailment.ClearAll();
        AssertThat(ailment.IsActive("oxidative_burn")).IsFalse();
        AssertThat(ailment.IsActive("endotoxin")).IsFalse();
        ailment.QueueFree();

        GD.Print("[PASS] AilmentController JSON loader defaults and generic adapter surface.");
    }
}
