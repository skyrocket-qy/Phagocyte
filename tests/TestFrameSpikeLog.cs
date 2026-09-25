using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using Phagocyte.Core;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the frame-spike flight recorder: over-threshold frames are
/// captured with a population census plus event waterlines, quiet frames
/// record nothing, and EndRun dumps parseable JSON to the isolated path.
/// </summary>
[TestSuite]
public partial class TestFrameSpikeLog : TestHarness
{
    private int _phase = 0;
    private int _framesWaited = 0;
    private bool _testDone = false;
    private RunTelemetryManager? _tele;
    private int _quietCount = 0;
    private string _spikePath = "";

    public override void _Initialize()
    {
        Banner("STARTING FRAME SPIKE LOG VERIFICATION");
    }

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        switch (_phase)
        {
            case 0:
                _framesWaited++;
                if (_framesWaited < 3)
                    return false;
                _framesWaited = 0;

                _tele = new RunTelemetryManager();
                Root.AddChild(_tele);
                _tele.StartRun();
                _spikePath = JsonStore.ResolvePath("test_framespikelog_spikes.json");
                DeleteIfExists(_spikePath);
                RunTelemetryManager.SpikeLogPath = _spikePath;
                // Zero threshold: every frame must record (proves the hot path fires).
                _tele.SpikeThresholdMs = 0.0f;
                FrameSpikeLog.MarkWave();
                _phase = 1;
                return false;

            case 1:
                _framesWaited++;
                if (_framesWaited < 3)
                    return false;
                _framesWaited = 0;

                AssertThat(_tele!.SpikeCount >= 2).IsTrue();
                var dicts = FrameSpikeLog.ToDictionaries();
                AssertThat(dicts.Count == _tele.SpikeCount).IsTrue();
                // Wave was marked before the frames: age present; nothing else marked.
                AssertThat(dicts[0]["wave_age_sec"].AsSingle() >= 0.0f).IsTrue();
                AssertThat(dicts[0]["achievement_age_sec"].AsSingle()).IsEqual(-1.0f);
                AssertThat(dicts[0]["boss_age_sec"].AsSingle()).IsEqual(-1.0f);
                GD.Print($"[PASS] 1. Spike capture works ({_tele.SpikeCount} spikes, wave waterline present, unmarked events -1).");

                // Sky-high threshold: quiet frames must record nothing.
                _tele.SpikeThresholdMs = 1.0e9f;
                _quietCount = _tele.SpikeCount;
                _phase = 2;
                return false;

            case 2:
                _framesWaited++;
                if (_framesWaited < 2)
                    return false;
                _framesWaited = 0;

                AssertThat(_tele!.SpikeCount).IsEqual(_quietCount);
                GD.Print("[PASS] 2. Quiet frames record nothing (no false positives).");

                _tele.EndRun();
                AssertThat(FileAccess.FileExists(_spikePath)).IsTrue();
                using (var file = FileAccess.Open(_spikePath, FileAccess.ModeFlags.Read))
                {
                    var parsed = Json.ParseString(file!.GetAsText()).AsGodotDictionary();
                    AssertThat(parsed.ContainsKey("spikes")).IsTrue();
                    AssertThat(parsed["spike_count"].AsInt32()).IsEqual(_quietCount);
                    AssertThat(parsed["spikes"].AsGodotArray().Count).IsEqual(_quietCount);
                }
                GD.Print("[PASS] 3. EndRun dumps parseable JSON with the full spike ring.");

                DeleteIfExists(_spikePath);
                RunTelemetryManager.SpikeLogPath = "";
                _tele.QueueFree();
                _testDone = true;
                GD.Print(">>> ALL FRAME SPIKE LOG TESTS PASSED SUCCESSFULLY! <<<");
                Quit(0);
                return true;

            default:
                return true;
        }
    }
}
