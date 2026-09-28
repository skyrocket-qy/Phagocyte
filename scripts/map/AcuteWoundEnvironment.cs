using Phagocyte.Directors;

namespace Phagocyte.Map;

/// <summary>
/// 01. 皮下創口 (acute_wound) — cleared arena: no environmental hazards
/// (docs/map.md §3). Registration and the map fallback in
/// <see cref="MapEnvironment.ForMap"/> are preserved; the wound remains the
/// default boot map with waves, bosses and progression intact.
/// </summary>
public sealed class AcuteWoundEnvironment : MapEnvironment
{
    public override string MapId => "acute_wound";

    protected override void Process(IRunContext context, float dt)
    {
    }
}
