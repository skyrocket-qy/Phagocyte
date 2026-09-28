using Godot;
using Phagocyte.Directors;

namespace Phagocyte.Map;

/// <summary>
/// 02. 肺泡氣體微腔 (alveolar_space) — hyperoxic pockets float through the
/// lumen and grant a temporary cooldown-reduction surge (docs/map.md §3).
/// </summary>
public sealed class AlveolarEnvironment : MapEnvironment
{
    public override string MapId => "alveolar_space";

    public const float PocketInterval = 6.0f;
    public const int MaxPockets = 3;

    private float _pocketTimer = 3.0f;

    protected override void Process(IRunContext context, float dt)
    {
        var container = Container(context);
        var player = Cell(context);
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
            GlobalPosition = PointNear(player.GlobalPosition, context.ArenaSize, 200.0f, 700.0f)
        });
    }
}
