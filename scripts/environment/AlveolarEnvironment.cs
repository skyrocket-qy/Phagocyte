using Godot;

namespace Phagocyte.Environment;

/// <summary>
/// 02. 肺泡氣體微腔 (alveolar_space) — a 12s respiratory cycle: 3s inhale pushes
/// the cell down, the matching exhale pushes it back up. Hyperoxic pockets float
/// through the lumen and grant a temporary cooldown-reduction surge
/// (docs/map.md §3).
/// </summary>
public sealed class AlveolarEnvironment : MapEnvironment
{
    public override string MapId => "alveolar_space";

    public const float BreathPeriod = 12.0f;
    public const float BreathPhaseSeconds = 3.0f;
    public const float ExhaleStartSeconds = 6.0f;
    public const float BreathForce = 35.0f;
    public const float PocketInterval = 6.0f;
    public const int MaxPockets = 3;

    private float _pocketTimer = 3.0f;

    protected override void Process(Main main, float dt)
    {
        float phase = Mathf.PosMod(Time, BreathPeriod);
        if (phase < BreathPhaseSeconds)
            PlayerDrift = new Vector2(0.0f, BreathForce);          // inhale: downward shear
        else if (phase >= ExhaleStartSeconds && phase < ExhaleStartSeconds + BreathPhaseSeconds)
            PlayerDrift = new Vector2(0.0f, -BreathForce);         // exhale: reverse thrust
        else if (HardMode)
            PlayerDrift = new Vector2(0.0f, BreathForce * 0.35f);  // Hard: the shear never rests
        else
            PlayerDrift = Vector2.Zero;

        // Respiratory airflow on the pathogen population (was Main.ProcessMapMechanics).
        float breathForce = Mathf.Sin(main.EnvironmentTime * 1.2f) * 28.0f;
        FluidVector = new Vector2(breathForce, Mathf.Sin(main.EnvironmentTime * 0.6f) * 12.0f) * 0.6f;

        var container = Container(main);
        var player = Cell(main);
        if (container == null || player == null)
            return;

        _pocketTimer -= dt;
        if (_pocketTimer > 0.0f)
            return;
        _pocketTimer = HazardInterval(PocketInterval);

        if (CountChildren<HyperoxicPocket>(container) >= MaxPockets)
            return;

        container.AddChild(new HyperoxicPocket
        {
            GlobalPosition = PointNear(player.GlobalPosition, main.ArenaSize, 200.0f, 700.0f)
        });
    }
}
