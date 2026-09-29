using Godot;

namespace Game.Combat;

public static class SlowService
{
    public static void ApplySlow(Node? target, float duration, float factor)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return;
        if (target is ISlowable slowable)
            slowable.ApplySlow(duration, factor);
    }
}
