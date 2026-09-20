using Godot;

namespace Phagocyte.Environment;

/// <summary>
/// 05. 血腦屏障毛細血管 (blood_brain_barrier) — extremely narrow high-shear flow
/// drags the cell, astrocyte foot-process pillars form a static maze, and random
/// neural electric pulses scramble the player's movement direction
/// (docs/map.md §3).
/// </summary>
public sealed class BloodBrainBarrierEnvironment : MapEnvironment
{
    public override string MapId => "blood_brain_barrier";

    public const float ShearAmplitudeX = 26.0f;
    public const float ShearAmplitudeY = 12.0f;
    public const float PulseInterval = 11.0f;
    public const float PulseDuration = 1.6f;
    public const int PillarCount = 5;

    private float _pulseTimer = 6.0f;

    public override void Attach(Main main)
    {
        base.Attach(main);

        var container = Container(main);
        if (container == null)
            return;

        // Astrocyte foot-process maze: static obstacles placed once per run.
        for (int i = 0; i < PillarCount; i++)
        {
            float angle = i * (Mathf.Tau / PillarCount) + 0.4f;
            var pillar = new AstrocytePillar
            {
                Radius = 70.0f + (i % 2) * 14.0f,
                GlobalPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 620.0f
            };
            container.AddChild(pillar);
        }
    }

    protected override void Process(Main main, float dt)
    {
        // High-shear capillary flow: strong oscillating lateral drag. Hard mode is
        // a permanent acute crisis: the shear is stronger and the neural pulses
        // scramble the controls for longer (docs/map.md §2).
        float shearScale = HardMode ? 1.3f : 1.0f;
        PlayerDrift = new Vector2(
            Mathf.Sin(Time * 1.4f) * ShearAmplitudeX * shearScale,
            Mathf.Cos(Time * 0.9f) * ShearAmplitudeY * shearScale);

        // High-frequency synaptic micro-vibrations on the pathogen population.
        FluidVector = new Vector2(
            Mathf.Sin(main.EnvironmentTime * 5.0f) * 8.0f,
            Mathf.Cos(main.EnvironmentTime * 4.0f) * 8.0f) * 0.25f;

        var player = Cell(main);
        if (player == null)
            return;

        _pulseTimer -= dt;
        if (_pulseTimer <= 0.0f)
        {
            _pulseTimer = HazardInterval(PulseInterval);
            player.ApplyInvertControls(HardMode ? PulseDuration * 1.5f : PulseDuration);
            GD.Print("[BBB] Neural electric pulse: movement direction scrambled.");
        }
    }
}
