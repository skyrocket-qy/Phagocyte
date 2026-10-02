using Godot;
using Game.UI;

namespace Game.Combat;

/// <summary>Hit resolution + floating-text presentation. Keeps HitPipeline pure.</summary>
public static class HitPresenter
{
    public static HitResult ResolveAndPresent(in HitPayload payload, Node? targetNode)
    {
        HitResult result = HitPipeline.ResolveHit(payload, targetNode);
        if (result.IsEvaded)
        {
            if (targetNode is Node2D evaded)
                DamageNumberSpawner.ShowEvaded(evaded.GlobalPosition);
        }
        else if (result.IsBlocked && result.DamageDealt <= 0.0f)
        {
            if (targetNode is Node2D blocked)
                DamageNumberSpawner.ShowBlocked(blocked.GlobalPosition);
        }
        if (result.LifeStolen > 0.0f)
        {
            Node2D? attacker = HitPipeline.ResolveAttacker(payload.AttackerId);
            if (attacker != null)
                DamageNumberSpawner.ShowHeal(attacker.GlobalPosition, result.LifeStolen);
        }
        return result;
    }
}
