using Godot;

namespace Game.Stages;

/// <summary>Static circular obstacle spawned in the environment.</summary>
public partial class DebrisWall : StaticBody2D
{
    public float Radius { get; set; } = 44.0f;

    public override void _Ready()
    {
        CollisionLayer = 4;
        CollisionMask = 0;
        ZIndex = 1;
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
