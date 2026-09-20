using Godot;
using Phagocyte.Player;

namespace Phagocyte.Environment;

/// <summary>
/// 01. 皮下創口 (acute_wound) — wound exudate suction drags the cell up-right and
/// fibrin clots congeal across the wound floor (docs/map.md §3). Hard difficulty
/// turns the clots into acidic biofilms.
/// </summary>
public sealed class AcuteWoundEnvironment : MapEnvironment
{
    public override string MapId => "acute_wound";

    public const float ExudateSuctionX = 16.0f;
    public const float ExudateSuctionY = 10.0f;
    public const float ClotInterval = 7.0f;
    public const int MaxClots = 6;

    private float _clotTimer = 2.5f;

    protected override void Process(Main main, float dt)
    {
        // Exudate suction is added straight onto the player's swim velocity.
        PlayerDrift = new Vector2(ExudateSuctionX, ExudateSuctionY);
        // Directional tissue-fluid suction on the pathogen population.
        FluidVector = new Vector2(ExudateSuctionX, ExudateSuctionY) * 0.4f;

        var container = Container(main);
        var player = Cell(main);
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
            GlobalPosition = PointNear(player.GlobalPosition, main.ArenaSize, 180.0f, 620.0f)
        };
        container.AddChild(clot);
    }
}
