using Godot;
using System.Collections.Generic;

namespace Phagocyte.Core;

/// <summary>
/// Centralized Audio Manager Autoload for Project: Phagocyte.
/// Manages high-performance SFX pooling and smooth BGM transitions.
/// </summary>
public partial class AudioManager : Node
{
    public static AudioManager? Instance { get; private set; }

    private const int SfxPoolSize = 24;
    private readonly AudioStreamPlayer[] _sfxPlayers = new AudioStreamPlayer[SfxPoolSize];
    private int _sfxPoolIndex = 0;

    /// <summary>
    /// Resolved SFX streams by sound name (null = missing, warned once).
    /// PlaySfx used to rebuild candidate paths and probe the loader on every
    /// hit — at 300 contact strikes a second that dominated TakeDamage.
    /// </summary>
    private readonly Dictionary<string, AudioStream?> _sfxCache = new(System.StringComparer.Ordinal);
    /// <summary>Last play timestamp per sound: same-name retriggers inside the
    /// floor collapse to one voice (indistinguishable in a horde).</summary>
    private readonly Dictionary<string, ulong> _sfxLastPlayMsec = new(System.StringComparer.Ordinal);
    private const ulong SfxRetriggerFloorMsec = 35;

    private AudioStreamPlayer? _bgmPlayer;
    private Tween? _bgmTween;
    private string _currentBgmTrack = "";

    /// <summary>
    /// Volume state is single-sourced in <see cref="SettingsManager"/> (Phase 3):
    /// playback always reads the persisted values, so slider changes apply
    /// without any push ordering between the two autoloads.
    /// </summary>
    public float MasterVolume => SettingsManager.MasterVolume;
    public float BgmVolume => SettingsManager.BgmVolume;
    public float SfxVolume => SettingsManager.SfxVolume;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;

        // Create BGM player
        _bgmPlayer = new AudioStreamPlayer
        {
            Name = "BgmPlayer",
            Bus = "Master"
        };
        AddChild(_bgmPlayer);

        // Pre-allocate SFX pool
        for (int i = 0; i < SfxPoolSize; i++)
        {
            var p = new AudioStreamPlayer
            {
                Name = $"SfxPlayer_{i}",
                Bus = "Master"
            };
            AddChild(p);
            _sfxPlayers[i] = p;
        }

        PreloadCommonAudio();
        ValidateAudioManifest();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base._ExitTree();
    }

    private void PreloadCommonAudio()
    {
        // Core SFX
        AssetLoader.Preload<AudioStream>(
            "res://assets/audio/sfx/hit.mp3",
            "res://assets/audio/sfx/hit_crit.mp3",
            "res://assets/audio/sfx/death.wav",
            "res://assets/audio/sfx/explosion.mp3",
            "res://assets/audio/sfx/combat/hit_sword_small.mp3",
            "res://assets/audio/sfx/combat/die_hero.mp3",
            "res://assets/audio/sfx/level_upgrade.mp3",
            "res://assets/audio/sfx/ui/ui_click.mp3",
            "res://assets/audio/sfx/ui/ui_confirm.mp3",
            "res://assets/audio/sfx/ui/item_pickup.mp3",
            "res://assets/audio/sfx/wave_complete.mp3",
            "res://assets/audio/sfx/game_over.mp3",
            "res://assets/audio/sfx/combat/shoot.mp3",
            "res://assets/audio/sfx/player_hit.wav",
            // Common BGMs
            "res://assets/audio/bgm/menu.mp3",
            "res://assets/audio/bgm/battle_bgm.mp3",
            "res://assets/audio/bgm/boss.mp3",
            "res://assets/audio/bgm/boss_final.mp3",
            "res://assets/audio/bgm/battle_bgm_2.mp3",
            "res://assets/audio/bgm/echoes_of_the_aether.mp3",
            "res://assets/audio/bgm/swamp.mp3",
            "res://assets/audio/bgm/crypt.mp3",
            "res://assets/audio/bgm/dimension.mp3",
            "res://assets/audio/bgm/victory.mp3",
            "res://assets/audio/bgm/defeat.mp3");
    }

    /// <summary>
    /// Fail-fast manifest check: every BGM track and SFX name listed in
    /// <c>assets/audio/manifest.json</c> must resolve to a file on disk.
    /// Missing entries are all reported via <see cref="GD.PushError"/> (loud
    /// in editor, red in test output) instead of failing silently mid-game.
    /// </summary>
    public void ValidateAudioManifest()
    {
        Godot.Collections.Dictionary manifest;
        try
        {
            manifest = CatalogLoader.LoadObject(DataPaths.AudioManifest);
        }
        catch (DataLoadException ex)
        {
            GD.PushError($"[Audio] manifest missing or malformed: {ex.Message}");
            return;
        }

        var missing = new System.Collections.Generic.List<string>();
        foreach (string track in ReadManifestNames(manifest, "bgm"))
        {
            if (AssetLoader.TryLoadFirst<AudioStream>(AssetPaths.BgmCandidates(track)) == null)
                missing.Add($"bgm/{track}");
        }
        foreach (string name in ReadManifestNames(manifest, "sfx"))
        {
            if (AssetLoader.TryLoadFirst<AudioStream>(AssetPaths.SfxCandidates(name)) == null)
                missing.Add($"sfx/{name}");
        }
        if (missing.Count > 0)
            GD.PushError($"[Audio] {missing.Count} manifest asset(s) missing on disk: {string.Join(", ", missing)}");
    }

    private static System.Collections.Generic.List<string> ReadManifestNames(
        Godot.Collections.Dictionary manifest, string key)
    {
        var names = new System.Collections.Generic.List<string>();
        if (manifest.TryGetValue(key, out var raw) && raw.VariantType == Variant.Type.Array)
        {
            foreach (var item in raw.AsGodotArray())
            {
                if (item.VariantType == Variant.Type.String)
                    names.Add(item.AsString());
            }
        }
        return names;
    }

    // ==========================================
    // BGM Methods
    // ==========================================

    public void PlayBgm(string trackName, float fadeTime = 0.5f)
    {
        if (_bgmPlayer == null || string.IsNullOrEmpty(trackName))
            return;

        if (_currentBgmTrack == trackName && _bgmPlayer.Playing)
            return;

        var stream = AssetLoader.TryLoadFirst<AudioStream>(AssetPaths.BgmCandidates(trackName));
        if (stream == null)
        {
            GD.PushWarning($"AudioManager: BGM track '{trackName}' not found.");
            return;
        }

        _currentBgmTrack = trackName;

        if (_bgmTween != null && _bgmTween.IsValid())
            _bgmTween.Kill();

        float targetVolumeDb = Mathf.LinearToDb(Mathf.Clamp(BgmVolume * MasterVolume, 0.0001f, 1.0f));

        if (_bgmPlayer.Playing && fadeTime > 0.05f)
        {
            _bgmTween = CreateTween();
            _bgmTween.TweenProperty(_bgmPlayer, "volume_db", -80.0f, fadeTime * 0.5f);
            _bgmTween.TweenCallback(Callable.From(() =>
            {
                _bgmPlayer.Stream = stream;
                _bgmPlayer.Play();
            }));
            _bgmTween.TweenProperty(_bgmPlayer, "volume_db", targetVolumeDb, fadeTime * 0.5f);
        }
        else
        {
            _bgmPlayer.Stream = stream;
            _bgmPlayer.VolumeDb = targetVolumeDb;
            _bgmPlayer.Play();
        }
    }

    // ==========================================
    // SFX Methods
    // ==========================================

    public void PlaySfx(string soundName, float pitchRandomness = 0.08f, float volumeDbOffset = 0.0f)
    {
        ulong now = Time.GetTicksMsec();
        if (_sfxLastPlayMsec.TryGetValue(soundName, out ulong last) && now - last < SfxRetriggerFloorMsec)
            return;
        _sfxLastPlayMsec[soundName] = now;

        if (!_sfxCache.TryGetValue(soundName, out AudioStream? stream))
        {
            stream = AssetLoader.TryLoadFirst<AudioStream>(AssetPaths.SfxCandidates(soundName));
            _sfxCache[soundName] = stream;
            if (stream == null)
                GD.PushWarning($"AudioManager: SFX '{soundName}' not found.");
        }
        if (stream == null)
            return;

        var player = _sfxPlayers[_sfxPoolIndex];
        _sfxPoolIndex = (_sfxPoolIndex + 1) % SfxPoolSize;

        float baseDb = Mathf.LinearToDb(Mathf.Clamp(SfxVolume * MasterVolume, 0.0001f, 1.0f));
        player.Stream = stream;
        player.VolumeDb = baseDb + volumeDbOffset;
        player.PitchScale = pitchRandomness > 0.0f
            ? (float)GD.RandRange(1.0f - pitchRandomness, 1.0f + pitchRandomness)
            : 1.0f;
        player.Play();
    }

    // High-frequency quick helpers
    public void PlayHit(bool isCrit = false)
    {
        if (isCrit)
            PlaySfx("hit_crit", 0.05f, 1.5f);
        else
            PlaySfx("hit", 0.10f, -1.0f);
    }

    public void PlayEnemyDeath()
    {
        PlaySfx("death", 0.12f, 0.0f);
    }

    public void PlayPlayerHit()
    {
        PlaySfx("player_hit", 0.05f, 2.0f);
    }

    public void PlayPlayerDeath()
    {
        PlaySfx("die_hero", 0.02f, 3.0f);
    }

    public void PlayLevelUp()
    {
        PlaySfx("level_upgrade", 0.0f, 2.0f);
    }

    public void PlayPickup()
    {
        PlaySfx("item_pickup", 0.10f, -2.0f);
    }

    public void PlayShoot()
    {
        PlaySfx("shoot", 0.08f, -1.0f);
    }

    public void PlayDodge()
    {
        PlaySfx("flame_dash", 0.10f, -2.0f);
    }

    public void PlayAchievement()
    {
        PlaySfx("extra_coin", 0.05f, 0.0f);
    }

    public void PlaySocket()
    {
        PlaySfx("gem_socket", 0.05f, 0.0f);
    }

    public void PlayUnequip()
    {
        PlaySfx("item_drop", 0.05f, -1.0f);
    }

    public void PlayError()
    {
        PlaySfx("ui_error_02", 0.0f, 0.0f);
    }

    public void PlayGameStart()
    {
        PlaySfx("game_start", 0.0f, 1.0f);
    }

    public void PlayWaveStart()
    {
        PlaySfx("wave_start", 0.05f, -1.0f);
    }

    public void PlayClick()
    {
        PlaySfx("ui_click", 0.03f, 0.0f);
    }

    /// <summary>
    /// Hooks the click SFX to every <see cref="Button"/> under <paramref name="root"/>.
    /// Idempotent (guarded by per-button metadata) so menu lists, modal card
    /// rebuilds and view refreshes can all call it safely.
    /// </summary>
    public void WireClicks(Node root)
    {
        if (root == null)
            return;
        foreach (Node node in root.FindChildren("*", "Button", true, false))
        {
            if (node is not Button btn || btn.HasMeta("_click_wired"))
                continue;
            btn.SetMeta("_click_wired", true);
            btn.Pressed += () => Instance?.PlayClick();
        }
    }

    /// <summary>
    /// Per-organ battle BGM (TODO Phase 5): each map id resolves to its own
    /// track; unknown ids fall back to the default battle theme.
    /// </summary>
    private static readonly Dictionary<string, string> MapBgmTracks = new()
    {
        ["acute_wound"] = "battle_bgm",
        ["alveolar_space"] = "echoes_of_the_aether",
        ["hepatic_sinusoid"] = "swamp",
        ["gastric_lumen"] = "crypt",
        ["blood_brain_barrier"] = "dimension",
    };

    public void PlayMapBgm(string mapKey, float fadeTime = 0.6f)
    {
        if (!MapBgmTracks.TryGetValue(mapKey ?? "", out string? track))
            track = "battle_bgm";
        PlayBgm(track, fadeTime);
    }
}
