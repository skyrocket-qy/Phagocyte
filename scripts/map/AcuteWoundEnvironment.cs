using Godot;
using Phagocyte.Directors;
using Phagocyte.Hero;

namespace Phagocyte.Map;

/// <summary>
/// 01. 皮下創口 (acute_wound) — fibrin clots congeal across the wound floor
/// (docs/map.md §3). Hard difficulty turns the clots into acidic biofilms.
/// </summary>
public sealed class AcuteWoundEnvironment : MapEnvironment
{
    public override string MapId => "acute_wound";

    public const float ClotInterval = 7.0f;
    public const int MaxClots = 6;

    private float _clotTimer = 2.5f;

    protected override void Process(IRunContext context, float dt)
    {
        var container = Container(context);
        var player = Cell(context);
        if (container == null || player == null)
            return;

        _clotTimer -= dt;
        if (_clotTimer > 0.0f)
            return;
        _clotTimer = HazardInterval(ClotInterval);

        if (CountChildren<FibrinClot>(container) >= MaxClots)
            return;

        var clot = new FibrinClot
        {
            HardBiofilm = HardMode,
            GlobalPosition = PointNear(player.GlobalPosition, context.ArenaSize, 180.0f, 620.0f)
        };
        container.AddChild(clot);
    }
}
