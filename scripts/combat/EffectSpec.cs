using Godot;

namespace Game.Combat;

/// <summary>
/// Generic on-hit effect. The engine sees only effect_id + scalars, never
/// domain types: stun dispatches via <see cref="IStunnable"/>, status via
/// <see cref="IAilmentHost"/>. Magnitude semantics per effect_id: stun = unused
/// (Duration holds seconds); opsonization = amp fraction (-1 = JSON default);
/// agglutination = slow fraction; oxidative_burn / membrane_leak / endotoxin =
/// DoT dps. Radius reserves on-hit AoE; Tint reserves per-instance batch color.
/// </summary>
public struct EffectSpec
{
    public string EffectId;
    public float Magnitude;
    public float Duration;
    public float Radius;
    public Color Tint;

    public readonly void ApplyTo(Node? target, float baseDamage = 0.0f)
    {
        if (target == null || !GodotObject.IsInstanceValid(target) || string.IsNullOrEmpty(EffectId))
            return;

        if (EffectId == "stun")
        {
            if (target is IStunnable stunnable)
                stunnable.ApplyStun(Duration);
            return;
        }

        if (target is not IAilmentHost host || host.Ailments == null)
            return;
        var ailments = host.Ailments;
        switch (EffectId)
        {
            case AilmentController.MarkedId:
                ailments.ApplyMarkation(Duration, Magnitude);
                if (target is Node2D marked)
                    VfxManager.Instance?.Play(VfxType.MarkBind, marked.GlobalPosition);
                break;
            case AilmentController.AgglutinationId:
                ailments.ApplyAgglutination(Duration, Magnitude);
                break;
            case AilmentController.BurnId:
                ailments.ApplyOxidativeBurn(Magnitude, Duration);
                break;
            case AilmentController.LeakId:
                ailments.ApplyMembraneLeak(Magnitude, Duration);
                break;
            case AilmentController.EndotoxinId:
                ailments.ApplyEndotoxin(Magnitude, Duration);
                break;
            default:
                GD.PushWarning($"[EffectSpec] Unknown effect '{EffectId}'.");
                break;
        }
    }

    public static void ApplyAll(Node? target, float baseDamage, in EffectSpec e0, in EffectSpec e1, in EffectSpec e2, int count)
    {
        if (count <= 0)
            return;
        e0.ApplyTo(target, baseDamage);
        if (count <= 1)
            return;
        e1.ApplyTo(target, baseDamage);
        if (count <= 2)
            return;
        e2.ApplyTo(target, baseDamage);
    }
}
