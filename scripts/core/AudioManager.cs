using Godot;
using System;
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

    private AudioStreamPlayer? _bgmPlayer;
    private Tween? _bgmTween;
    private string _currentBgmTrack = "";

    // Stream cache for zero runtime IO latency
    private readonly Dictionary<string, AudioStream> _audioCache = new(StringComparer.OrdinalIgnoreCase);

    public float MasterVolume { get; set; } = 1.0f;
    public float BgmVolume { get; set; } = 0.8f;
    public float SfxVolume { get; set; } = 1.0f;

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
        CacheResource("hit", "res://assets/audio/sfx/hit.mp3");
        CacheResource("hit_crit", "res://assets/audio/sfx/hit_crit.mp3");
        CacheResource("death", "res://assets/audio/sfx/death.wav");
        CacheResource("explosion", "res://assets/audio/sfx/explosion.mp3");
        CacheResource("player_hit", "res://assets/audio/sfx/combat/hit_sword_small.mp3");
        CacheResource("player_death", "res://assets/audio/sfx/combat/die_hero.mp3");
        CacheResource("level_up", "res://assets/audio/sfx/level_upgrade.mp3");
        CacheResource("ui_click", "res://assets/audio/sfx/ui/ui_click.mp3");
        CacheResource("ui_confirm", "res://assets/audio/sfx/ui/ui_confirm.mp3");
        CacheResource("pickup", "res://assets/audio/sfx/ui/item_pickup.mp3");
        CacheResource("wave_complete", "res://assets/audio/sfx/wave_complete.mp3");
        CacheResource("game_over", "res://assets/audio/sfx/game_over.mp3");
        CacheResource("shoot", "res://assets/audio/sfx/combat/shoot.mp3");

        // Common BGMs
        CacheResource("menu", "res://assets/audio/bgm/menu.mp3");
        CacheResource("battle_bgm", "res://assets/audio/bgm/battle_bgm.mp3");
        CacheResource("boss", "res://assets/audio/bgm/boss.mp3");
        CacheResource("victory", "res://assets/audio/bgm/victory.mp3");
        CacheResource("defeat", "res://assets/audio/bgm/defeat.mp3");
    }

    private void CacheResource(string key, string path)
    {
        if (ResourceLoader.Exists(path))
        {
            var stream = GD.Load<AudioStream>(path);
            if (stream != null)
            {
                _audioCache[key] = stream;
            }
        }
    }

    private AudioStream? ResolveAudioStream(string name, bool isBgm = false)
    {
        if (_audioCache.TryGetValue(name, out var cached))
            return cached;

        // Try direct file paths
        string[] candidates = isBgm ? new[]
        {
            $"res://assets/audio/bgm/{name}.mp3",
            $"res://assets/audio/bgm/{name}.ogg",
            $"res://assets/audio/bgm/{name}.wav"
        } : new[]
        {
            $"res://assets/audio/sfx/{name}.mp3",
            $"res://assets/audio/sfx/{name}.wav",
            $"res://assets/audio/sfx/combat/{name}.mp3",
            $"res://assets/audio/sfx/combat/{name}.wav",
            $"res://assets/audio/sfx/ui/{name}.mp3",
            $"res://assets/audio/sfx/ui/{name}.wav",
            $"res://assets/audio/sfx/gem/{name}.mp3",
            $"res://assets/audio/sfx/gem/{name}.wav"
        };

        foreach (var path in candidates)
        {
            if (ResourceLoader.Exists(path))
            {
                var stream = GD.Load<AudioStream>(path);
                if (stream != null)
                {
                    _audioCache[name] = stream;
                    return stream;
                }
            }
        }

        return null;
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

        var stream = ResolveAudioStream(trackName, isBgm: true);
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

    public void StopBgm(float fadeTime = 0.5f)
    {
        if (_bgmPlayer == null || !_bgmPlayer.Playing)
            return;

        if (_bgmTween != null && _bgmTween.IsValid())
            _bgmTween.Kill();

        if (fadeTime > 0.05f)
        {
            _bgmTween = CreateTween();
            _bgmTween.TweenProperty(_bgmPlayer, "volume_db", -80.0f, fadeTime);
            _bgmTween.TweenCallback(Callable.From(() =>
            {
                _bgmPlayer.Stop();
                _currentBgmTrack = "";
            }));
        }
        else
        {
            _bgmPlayer.Stop();
            _currentBgmTrack = "";
        }
    }

    // ==========================================
    // SFX Methods
    // ==========================================

    public void PlaySfx(string soundName, float pitchRandomness = 0.08f, float volumeDbOffset = 0.0f)
    {
        var stream = ResolveAudioStream(soundName, isBgm: false);
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
        PlaySfx("player_death", 0.02f, 3.0f);
    }

    public void PlayLevelUp()
    {
        PlaySfx("level_up", 0.0f, 2.0f);
    }

    public void PlayClick()
    {
        PlaySfx("ui_click", 0.03f, 0.0f);
    }

    public void PlayPickup()
    {
        PlaySfx("pickup", 0.10f, -2.0f);
    }

    public void PlayShoot()
    {
        PlaySfx("shoot", 0.08f, -1.0f);
    }
}
