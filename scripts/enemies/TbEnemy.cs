using Godot;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Mycobacterium tuberculosis (結核分枝桿菌)
/// Thick waxy mycolic acid cell wall. Acid-resistant: slows digestion by 70% and burns player cytoplasm.
/// </summary>
public partial class TbEnemy : BaseEnemy
{
    public TbEnemy()
    {
        EnemyId = "tb";
        DisplayNameKey = "PATHOGEN_TB_NAME";
        MaxHealth = 35.0f;
        AtpValue = 18.0f;
        BaseScore = 100;
        FloatSpeed = 30.0f;
        ThreatMode = EnemyThreatMode.ChemoChaser;
        Armor = 2.0f; // Mycolic wax absorbs damage
    }

    protected override float GetCollisionRadius() => 14.0f;

    public override void BeEngulfed(Node2D? predator)
    {
        if (IsBeingEaten)
            return;

        if (!CanBeEngulfed)
        {
            OnEngulfAttemptFailed(predator);
            return;
        }

        IsBeingEaten = true;

        if (predator is BaseCell player)
        {
            // Apply digestion burn: deals 4 dps to player cytoplasm
            player.ApplyTBDigestionBurn(2.5f, 4.0f);
        }

        if (HitArea != null)
        {
            HitArea.SetDeferred(Area2D.PropertyName.Monitoring, false);
            HitArea.SetDeferred(Area2D.PropertyName.Monitorable, false);
        }
        if (EnemyCollisionShape != null)
        {
            EnemyCollisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        }

        Vector2 predPos = predator != null && GodotObject.IsInstanceValid(predator) ? predator.GlobalPosition : GlobalPosition;

        // Slow digestion tween (0.75s instead of 0.25s)
        var tween = CreateTween().SetParallel(true);
        tween.TweenProperty(this, "global_position", predPos, 0.75)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.75)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.75);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            EmitSignal(SignalName.Digested, this);
            QueueFree();
        }));
    }

    public override void _Draw()
    {
        // 1. Thick waxy mycolic acid outer boundary (glossy yellow-magenta)
        Color waxBorder = new Color(0.9f, 0.3f, 0.5f, 0.85f);
        Color acidCore = new Color(0.6f, 0.1f, 0.25f, 0.95f);

        // Slender curved arc body
        Vector2[] points = new Vector2[]
        {
            new Vector2(-15, -4),
            new Vector2(-6, 2),
            new Vector2(6, 2),
            new Vector2(15, -4)
        };

        // Draw thick rod
        for (int i = 0; i < points.Length - 1; i++)
        {
            DrawLine(points[i], points[i + 1], waxBorder, 8.0f);
            DrawLine(points[i], points[i + 1], acidCore, 4.5f);
        }

        // Acid-fast beaded granules
        DrawCircle(new Vector2(-8, 0), 2.0f, Colors.White);
        DrawCircle(new Vector2(0, 2), 2.0f, Colors.White);
        DrawCircle(new Vector2(8, 0), 2.0f, Colors.White);
    }
}
