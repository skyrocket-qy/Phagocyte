using Godot;
using System;
using System.Collections.Generic;

namespace Phagocyte.Combat;

/// <summary>
/// Pre-allocated GPU particle pool for combat impacts, cytoplasm bursts, and lysis reactions.
/// Completely eliminates runtime GC allocation by recycling one-shot emitters.
/// </summary>
public partial class VfxManager : Node2D
{
    public static VfxManager? Instance { get; private set; }

    private const int PoolSizePerType = 16;
    private readonly Dictionary<VfxType, GpuParticles2D[]> _particlePools = new();
    private readonly Dictionary<VfxType, int> _poolIndices = new();
    private Texture2D? _particleTexture;

    public override void _Ready()
    {
        Instance = this;
        _particleTexture = CreateParticleTexture();
        InitializePools();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base._ExitTree();
    }

    private static Texture2D CreateParticleTexture()
    {
        var grad = new Gradient();
        grad.Colors = new[] { Colors.White, new Color(1f, 1f, 1f, 0.8f), new Color(1f, 1f, 1f, 0.0f) };
        grad.Offsets = new[] { 0.0f, 0.65f, 1.0f };

        return new GradientTexture2D
        {
            Gradient = grad,
            Width = 16,
            Height = 16,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(1.0f, 0.5f)
        };
    }

    private void InitializePools()
    {
        foreach (VfxType type in Enum.GetValues<VfxType>())
        {
            var pool = new GpuParticles2D[PoolSizePerType];
            var material = CreateMaterialForType(type);

            for (int i = 0; i < PoolSizePerType; i++)
            {
                var emitter = new GpuParticles2D
                {
                    Name = $"{type}_{i}",
                    ProcessMaterial = material,
                    Texture = _particleTexture,
                    Emitting = false,
                    OneShot = true,
                    Explosiveness = 0.95f,
                    Amount = GetParticleAmount(type),
                    Lifetime = GetParticleLifetime(type),
                    ZIndex = 80
                };

                AddChild(emitter);
                pool[i] = emitter;
            }

            _particlePools[type] = pool;
            _poolIndices[type] = 0;
        }
    }

    private static int GetParticleAmount(VfxType type)
    {
        return type switch
        {
            VfxType.LysisBurst => 32,
            VfxType.CytoplasmSplatter => 24,
            VfxType.BiofilmBurst => 20,
            VfxType.AcidOxidationSparks => 16,
            VfxType.BarbImpact => 12,
            _ => 16
        };
    }

    private static float GetParticleLifetime(VfxType type)
    {
        return type switch
        {
            VfxType.LysisBurst => 0.45f,
            VfxType.CytoplasmSplatter => 0.35f,
            VfxType.BiofilmBurst => 0.40f,
            VfxType.AcidOxidationSparks => 0.25f,
            VfxType.BarbImpact => 0.20f,
            _ => 0.30f
        };
    }

    private static ParticleProcessMaterial CreateMaterialForType(VfxType type)
    {
        var mat = new ParticleProcessMaterial
        {
            Spread = 180.0f,
            InitialVelocityMin = 80.0f,
            InitialVelocityMax = 180.0f,
            Gravity = new Vector3(0, 30, 0),
            ScaleMin = 0.6f,
            ScaleMax = 1.2f
        };

        switch (type)
        {
            case VfxType.CytoplasmSplatter:
                // Bioluminescent organic cytoplasm green/cyan
                mat.Color = new Color(0.2f, 0.95f, 0.55f, 0.95f);
                mat.InitialVelocityMin = 100.0f;
                mat.InitialVelocityMax = 220.0f;
                mat.Gravity = new Vector3(0, 60, 0);
                break;

            case VfxType.AcidOxidationSparks:
                // Acidic ROS / lysozyme yellow-green
                mat.Color = new Color(0.92f, 1.0f, 0.25f, 0.95f);
                mat.InitialVelocityMin = 90.0f;
                mat.InitialVelocityMax = 200.0f;
                break;

            case VfxType.LysisBurst:
                // High-energy deep magenta/violet cellular rupture
                mat.Color = new Color(0.95f, 0.25f, 0.85f, 1.0f);
                mat.InitialVelocityMin = 120.0f;
                mat.InitialVelocityMax = 260.0f;
                mat.ScaleMin = 0.8f;
                mat.ScaleMax = 1.6f;
                break;

            case VfxType.BarbImpact:
                // Chitinous Defensin cyan-white penetration sparks
                mat.Color = new Color(0.40f, 0.88f, 1.0f, 1.0f);
                mat.InitialVelocityMin = 110.0f;
                mat.InitialVelocityMax = 230.0f;
                break;

            case VfxType.BiofilmBurst:
                // Sticky amber / microbial slime burst
                mat.Color = new Color(0.85f, 0.65f, 0.20f, 0.9f);
                mat.InitialVelocityMin = 60.0f;
                mat.InitialVelocityMax = 150.0f;
                mat.Gravity = new Vector3(0, 50, 0);
                break;
        }

        return mat;
    }

    /// <summary>
    /// Plays pre-warmed particle effect from pool at target world position.
    /// </summary>
    public void Play(VfxType type, Vector2 position, Vector2? direction = null)
    {
        if (!_particlePools.TryGetValue(type, out var pool) || pool.Length == 0)
            return;

        int index = _poolIndices[type];
        _poolIndices[type] = (index + 1) % pool.Length;

        var emitter = pool[index];
        emitter.GlobalPosition = position;

        if (direction.HasValue && direction.Value != Vector2.Zero)
        {
            emitter.Rotation = direction.Value.Angle();
        }

        emitter.Restart();
    }
}
