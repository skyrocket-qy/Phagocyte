using Godot;

namespace Phagocyte.Environment;

/// <summary>
/// 03. 肝血竇微循環 (hepatic_sinusoid) — slow sinusoidal flow drags the cell;
/// the bile-acid hydrolysis surge periodically strips the whole arena's armor for
/// 3 seconds; endothelial fenestrae are narrow pores that only fit a compressed
/// (Squeeze Mode) cell (docs/map.md §3).
/// </summary>
public sealed class HepaticEnvironment : MapEnvironment
{
    public override string MapId => "hepatic_sinusoid";

    public const float FlowDragX = 10.0f;
    public const float BileSurgeInterval = 18.0f;
    public const float BileArmorBreakSeconds = 3.0f;
    public const float FenestraInterval = 24.0f;
    public const int MaxFenestrae = 3;

    private float _bileTimer = 10.0f;
    private float _armorBreakTimer;
    private float _armorStolen;
    private float _fenestraTimer = 5.0f;

    /// <summary>True while the bile-acid surge has stripped the arena's armor.</summary>
    public bool ArmorBroken => _armorBreakTimer > 0.0f;

    protected override void Process(Main main, float dt)
    {
        PlayerDrift = new Vector2(FlowDragX, Mathf.Sin(Time * 0.8f) * 6.0f) * 0.35f;
        // Hepatic sinusoid slow-flow drag on the pathogen population.
        FluidVector = new Vector2(FlowDragX, Mathf.Sin(main.EnvironmentTime * 0.8f) * 6.0f) * 0.35f;

        var player = Cell(main);
        if (player?.Stats == null)
            return;

        // Bile-acid hydrolysis surge: strip all armor for 3s, then restore it.
        if (_armorBreakTimer > 0.0f)
        {
            _armorBreakTimer -= dt;
            if (_armorBreakTimer <= 0.0f && _armorStolen > 0.0f)
            {
                player.Stats.AddModifier("armor", _armorStolen, 0.0f);
                _armorStolen = 0.0f;
            }
        }
        else
        {
            _bileTimer -= dt;
            if (_bileTimer <= 0.0f)
            {
                _bileTimer = HazardInterval(BileSurgeInterval);
                _armorStolen = Mathf.Max(0.0f, player.Stats.GetStat("armor"));
                if (_armorStolen > 0.0f)
                {
                    player.Stats.AddModifier("armor", -_armorStolen, 0.0f);
                    _armorBreakTimer = BileArmorBreakSeconds;
                    GD.Print("[Hepatic] Bile-acid hydrolysis: armor stripped for 3s.");
                }
            }
        }

        var container = Container(main);
        if (container == null)
            return;

        _fenestraTimer -= dt;
        if (_fenestraTimer > 0.0f)
            return;
        _fenestraTimer = HazardInterval(FenestraInterval);

        if (CountChildren<FenestraWall>(container) >= MaxFenestrae)
            return;

        bool vertical = Rng.Randf() < 0.5f;
        Vector2 position = PointNear(Vector2.Zero, main.ArenaSize * 0.7f, 300.0f, 900.0f);
        var wall = new FenestraWall
        {
            Rotation = vertical ? 0.0f : Mathf.Pi * 0.5f,
            GapHalfWidth = 55.0f,
            Length = 320.0f,
            Position = position
        };
        container.AddChild(wall);
    }
}
