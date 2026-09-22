using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System;
using System.Collections.Generic;
using Phagocyte.Core;

namespace Phagocyte.Tests;

/// <summary>
/// Verifies the unified AssetLoader pipeline: fail-fast Load, nullable
/// TryLoad, candidate probing, central cache, best-effort Preload, path
/// mapping and provider injection. Restores the production provider on exit.
/// </summary>
[TestSuite]
public partial class TestAssetLoader : TestHarness
{
    private int _frame = 0;
    private bool _done = false;

    public override void _Initialize()
    {
        Banner("STARTING ASSET LOADER VERIFICATION");
    }

    public override bool _Process(double delta)
    {
        if (_done)
            return true;

        if (!Gate(ref _frame, 2))
            return false;

        _done = true;
        try
        {
            RunLoaderTests();
            RunPathTests();
            GD.Print("==================================================================");
            GD.Print(">>> ALL ASSET LOADER TESTS PASSED SUCCESSFULLY! <<<");
            GD.Print("==================================================================");
            Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr("[FAIL] TestAssetLoader threw: ", ex);
            Quit(1);
        }
        finally
        {
            AssetLoader.ResetProvider();
        }
        return true;
    }

    private sealed class FakeAssetProvider : IAssetProvider
    {
        public readonly Dictionary<string, Resource> Resources = new(StringComparer.OrdinalIgnoreCase);
        public int LoadCalls;

        public bool Exists(string resPath) => !string.IsNullOrEmpty(resPath) && Resources.ContainsKey(resPath);

        public T? Load<T>(string resPath) where T : Resource
        {
            LoadCalls++;
            return Resources.TryGetValue(resPath, out var r) ? r as T : null;
        }
    }

    private static Texture2D MakeTexture()
    {
        var img = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
        img.Fill(new Color(1, 0, 0, 1));
        return ImageTexture.CreateFromImage(img);
    }

    private static void RunLoaderTests()
    {
        var fake = new FakeAssetProvider();
        fake.Resources["res://fake/icon.png"] = MakeTexture();
        fake.Resources["res://fake/hit.mp3"] = new AudioStreamWav();
        AssetLoader.SetProvider(fake);

        // Exists honors empty / known / unknown paths.
        AssertThat(AssetLoader.Exists("")).IsFalse();
        AssertThat(AssetLoader.Exists("res://fake/icon.png")).IsTrue();
        AssertThat(AssetLoader.Exists("res://fake/nope.png")).IsFalse();
        GD.Print("[PASS] AssetLoader.Exists verified.");

        // Load returns the resource and caches it (one provider call for two loads).
        var tex1 = AssetLoader.Load<Texture2D>("res://fake/icon.png");
        var tex2 = AssetLoader.Load<Texture2D>("res://fake/icon.png");
        AssertThat(tex1).IsNotNull();
        AssertThat(ReferenceEquals(tex1, tex2)).IsTrue();
        AssertThat(fake.LoadCalls).IsEqual(1);
        GD.Print("[PASS] AssetLoader.Load cache hit verified.");

        // Load on a missing file throws AssetLoadException carrying the path.
        try
        {
            AssetLoader.Load<Texture2D>("res://fake/nope.png");
            AssertThat(false).IsTrue();
        }
        catch (AssetLoadException ex)
        {
            AssertThat(ex.ResPath).IsEqual("res://fake/nope.png");
        }
        GD.Print("[PASS] AssetLoader.Load missing-file throw verified.");

        // Load on a type mismatch throws AssetLoadException.
        try
        {
            AssetLoader.Load<Texture2D>("res://fake/hit.mp3");
            AssertThat(false).IsTrue();
        }
        catch (AssetLoadException)
        {
        }
        GD.Print("[PASS] AssetLoader.Load type-mismatch throw verified.");

        // TryLoad never throws: missing and empty paths yield null.
        AssertThat(AssetLoader.TryLoad<Texture2D>("")).IsNull();
        AssertThat(AssetLoader.TryLoad<Texture2D>("res://fake/nope.png")).IsNull();
        GD.Print("[PASS] AssetLoader.TryLoad null contract verified.");

        // Candidate probing picks the first hit; all-miss throws / nulls.
        var first = AssetLoader.LoadFirst<Texture2D>(new[] { "res://fake/nope.png", "res://fake/icon.png" });
        AssertThat(ReferenceEquals(first, tex1)).IsTrue();
        AssertThat(AssetLoader.TryLoadFirst<Texture2D>(new[] { "res://fake/nope.png" })).IsNull();
        try
        {
            AssetLoader.LoadFirst<Texture2D>(new[] { "res://fake/a.png", "res://fake/b.png" });
            AssertThat(false).IsTrue();
        }
        catch (AssetLoadException)
        {
        }
        GD.Print("[PASS] AssetLoader candidate probing verified.");

        // Preload warms the cache and silently skips missing files.
        AssetLoader.Clear();
        AssetLoader.Preload<Texture2D>("res://fake/icon.png", "res://fake/nope.png");
        int before = fake.LoadCalls;
        AssetLoader.Load<Texture2D>("res://fake/icon.png");
        AssertThat(fake.LoadCalls - before).IsEqual(0);
        GD.Print("[PASS] AssetLoader.Preload verified.");

        // Clear + ResetProvider restore production behavior hooks.
        AssetLoader.Clear();
        AssetLoader.ResetProvider();
        AssertThat(AssetLoader.Exists("res://fake/icon.png")).IsFalse();
        GD.Print("[PASS] AssetLoader.Clear/ResetProvider verified.");
    }

    private static void RunPathTests()
    {
        // Ids equal gen/ file stems; paths derived per category.
        AssertThat(AssetPaths.AchievementSprite("engulf_20"))
            .IsEqual("res://assets/gen/achievement/engulf_20.png");

        // Ids equal file stems (no prefix stripping).
        AssertThat(AssetPaths.SkillIcon("actin")).IsEqual("res://assets/gen/skill/actin.png");
        AssertThat(AssetPaths.SkillIcon("ros_torrent")).IsEqual("res://assets/gen/skill/ros_torrent.png");
        AssertThat(AssetPaths.TraitIcon("actin")).IsEqual("res://assets/gen/passive_tree/actin.png");
        AssertThat(AssetPaths.TraitIcon("adaptive_overdrive")).IsEqual("res://assets/gen/passive_tree/adaptive_overdrive.png");
        AssertThat(AssetPaths.TraitIcon("small_might")).IsEqual("res://assets/gen/passive_tree/small_might.png");
        AssertThat(AssetPaths.UiIcon("reticle_target")).IsEqual("res://assets/gen/ui/reticle_target.png");
        GD.Print("[PASS] AssetPaths mapping verified.");
    }
}
