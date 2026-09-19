using Godot;
using Phagocyte.Player;

namespace Phagocyte.Environment;

/// <summary>
/// 04. 胃腔極酸黏膜 (gastric_lumen) — periodic acid surges flood the floor with
/// corrosion DoT; H. pylori urea hydrolysis opens alkaline neutralization zones
/// that act as safe harbors (docs/map.md §3).
/// </summary>
public sealed class GastricEnvironment : MapEnvironment
{
    public override string MapId => "gastric_lumen";

    public const float SurgeInterval = 14.0f;
    public const float SurgeDamagePerSecond = 7.0f;
    public const int SurgesPerWave = 2;
    public const float ZoneInterval = 9.0f;
    public const int MaxZones = 3;

    private float _surgeTimer = 6.0f;
    private float _zoneTimer = 3.0f;

    protected override void Process(Main main, float dt)
    {
        // Gastric churn: periodic lateral wave on the player's swim velocity.
        PlayerDrift = new Vector2(Mathf.Sin(Time * 0.9f) * 8.0f, Mathf.Cos(Time * 1.3f) * 6.0f);

        var container = Container(main);
        var player = Cell(main);
        if (container == null || player == null)
            return;

        _zoneTimer -= dt;
        if (_zoneTimer <= 0.0f)
        {
            _zoneTimer = HazardInterval(ZoneInterval);
            if (CountChildren<NeutralizationZone>(container) < MaxZones)
            {
                container.AddChild(new NeutralizationZone
                {
                    GlobalPosition = PointNear(Vector2.Zero, main.ArenaSize * 0.8f, 200.0f, 900.0f)
                });
            }
        }

        _surgeTimer -= dt;
        if (_surgeTimer <= 0.0f)
        {
            _surgeTimer = HazardInterval(SurgeInterval);
            for (int i = 0; i < SurgesPerWave; i++)
            {
                container.AddChild(new AcidSurge
                {
                    GlobalPosition = PointNear(player.GlobalPosition, main.ArenaSize, 120.0f, 760.0f)
                });
            }
            GD.Print("[Gastric] Acid surge inbound.");
        }

        // Corrosion only applies outside the alkaline neutralization zones.
        if (NeutralizationZone.CoversPoint(player.GlobalPosition))
            return;

        foreach (var child in container.GetChildren())
        {
            if (child is AcidSurge surge && GodotObject.IsInstanceValid(surge)
                && surge.Contains(player.GlobalPosition))
            {
                player.TakeEnvironmentalDamage(SurgeDamagePerSecond * dt);
                player.ApplySlow(0.3f, 0.7f);
                break;
            }
        }
    }
}
