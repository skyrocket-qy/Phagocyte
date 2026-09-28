using Godot;
using Phagocyte.Directors;

namespace Phagocyte.Map;

/// <summary>
/// 05. 血腦屏障毛細血管 (blood_brain_barrier) — astrocyte foot-process pillars
/// form a static maze, and random neural electric pulses scramble the player's
/// movement direction (docs/map.md §3).
/// </summary>
public sealed class BloodBrainBarrierEnvironment : MapEnvironment
{
    public override string MapId => "blood_brain_barrier";

    public const float PulseInterval = 11.0f;
    public const float PulseDuration = 1.6f;
    public const int PillarCount = 5;

    private float _pulseTimer = 6.0f;

    public override void Attach(IRunContext context)
    {
        base.Attach(context);

        var container = Container(context);
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

    protected override void Process(IRunContext context, float dt)
    {
        // Neural electric pulses periodically scramble the movement direction.
        // Hard mode is a permanent acute crisis: pulses last longer (docs/map.md §2).
        var player = Cell(context);
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
