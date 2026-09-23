using Godot;
using Phagocyte.Core;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Phagocyte.Tests;

/// <summary>
/// Audio manifest integrity: every BGM track and SFX name listed in
/// assets/audio/manifest.json must resolve to a file on disk through the
/// same candidate probing the runtime uses. Fails the suite instead of
/// failing silently mid-game (PlaySfx used to return without a sound).
/// </summary>
public partial class TestAudioAssets : TestHarness
{
    private int _framesWaited = 0;
    private bool _testDone = false;

    public override bool _Process(double delta)
    {
        if (_testDone)
            return true;

        _framesWaited++;
        if (_framesWaited < 3)
            return false;

        _testDone = true;

        var manifest = CatalogLoader.LoadObject(DataPaths.AudioManifest);
        AssertThat(manifest.ContainsKey("bgm")).IsTrue();
        AssertThat(manifest.ContainsKey("sfx")).IsTrue();

        var bgm = manifest["bgm"].AsGodotArray();
        var sfx = manifest["sfx"].AsGodotArray();
        AssertThat(bgm.Count).IsGreater(0);
        AssertThat(sfx.Count).IsGreater(0);

        foreach (var trackVar in bgm)
        {
            string track = trackVar.AsString();
            AssertThat(AssetLoader.TryLoadFirst<AudioStream>(AssetPaths.BgmCandidates(track))).IsNotNull();
        }
        GD.Print($"[PASS] All {bgm.Count} manifest BGM tracks resolve to audio files.");

        foreach (var sfxVar in sfx)
        {
            string name = sfxVar.AsString();
            AssertThat(AssetLoader.TryLoadFirst<AudioStream>(AssetPaths.SfxCandidates(name))).IsNotNull();
        }
        GD.Print($"[PASS] All {sfx.Count} manifest SFX names resolve to audio files.");

        // Previously silent gaps (wrong names): these helpers must hit real files.
        AssertThat(AssetLoader.TryLoadFirst<AudioStream>(AssetPaths.SfxCandidates("die_hero"))).IsNotNull();
        AssertThat(AssetLoader.TryLoadFirst<AudioStream>(AssetPaths.SfxCandidates("level_upgrade"))).IsNotNull();
        AssertThat(AssetLoader.TryLoadFirst<AudioStream>(AssetPaths.SfxCandidates("item_pickup"))).IsNotNull();
        GD.Print("[PASS] Remapped helpers (death/level-up/pickup) resolve to real files.");

        AudioManager.Instance?.ValidateAudioManifest();
        GD.Print("[PASS] AudioManager manifest validation runs without missing assets.");

        GD.Print("--- ALL AUDIO ASSET TESTS PASSED SUCCESSFULLY! ---");
        Quit(0);
        return true;
    }
}
