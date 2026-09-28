using Godot;

namespace Game.Enemies;

/// <summary>
/// Permanent collision obstacle dropped by death traits (granuloma wall).
/// Static geometry on the environment layer; pure data (radius).
/// </summary>
public partial class BlockerObstacle : StaticBody2D
{
    public float Radius { get; set; } = 44.0f;

    public override void _Ready()
    {
        CollisionLayer = 4;
        CollisionMask = 0;
        ZIndex = 1;
        AddToGroup("hazards");
        AddChild(new CollisionShape2D
        {
            Name = "CollisionShape2D",
            Shape = new CircleShape2D { Radius = Radius }
        });
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, Radius, new Color(0.55f, 0.50f, 0.30f, 0.55f));
        DrawArc(Vector2.Zero, Radius, 0.0f, Mathf.Tau, 40, new Color(0.65f, 0.60f, 0.38f, 0.8f), 3.0f);
    }
}
