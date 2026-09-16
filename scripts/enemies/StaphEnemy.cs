using Godot;
using System;
using System.Collections.Generic;

namespace Phagocyte.Enemies;

public partial class StaphEnemy : Node2D
{
    [Signal]
    public delegate void DigestedEventHandler(Node2D staph);

    [Export] public float AtpValue { get; set; } = 12.0f;
    [Export] public float FloatSpeed { get; set; } = 35.0f;
    [Export] public float DriftFrequency { get; set; } = 1.2f;

    public bool IsBeingEaten { get; set; } = false;
    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public float DriftTimer { get; set; } = 0.0f;
    public Vector2 WanderDir { get; set; } = Vector2.Zero;

    public record SphereData(Vector2 Offset, float Radius, Color Color);
    private readonly List<SphereData> _clusterSpheres = new();

    public Area2D? HitArea { get; set; }
    public CollisionShape2D? EnemyCollisionShape { get; set; }

    public override void _Ready()
    {
        AddToGroup("pathogens");
        HitArea = GetNodeOrNull<Area2D>("HitArea");
        EnemyCollisionShape = GetNodeOrNull<CollisionShape2D>("HitArea/CollisionShape2D");

        DriftTimer = GD.Randf() * 5.0f;
        WanderDir = Vector2.FromAngle(GD.Randf() * Mathf.Tau);

        // Generate 3-5 golden cocci spheres to form a staph cluster
        int count = (int)GD.RandRange(3, 5);
        Color baseGolden = new Color(0.95f, 0.78f, 0.18f, 0.95f);
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = new Vector2((float)GD.RandRange(-10.0, 10.0), (float)GD.RandRange(-10.0, 10.0));
            float radius = (float)GD.RandRange(6.5, 9.5);
            Color color = baseGolden.Lightened((float)GD.RandRange(-0.1, 0.1));
            _clusterSpheres.Add(new SphereData(offset, radius, color));
        }
        QueueRedraw();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsBeingEaten)
            return;

        float dt = (float)delta;
        DriftTimer += dt;
        // Brownian drifting in fluid
        if (DriftTimer > 2.5f)
        {
            DriftTimer = 0.0f;
            WanderDir = (WanderDir + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 0.7f).Normalized();
        }

        Velocity = Velocity.Lerp(WanderDir * FloatSpeed, 2.0f * dt);
        Position += Velocity * dt;

        // Rhythmic organic breathing oscillation
        float breathe = 1.0f + Mathf.Sin((float)Time.GetTicksMsec() * 0.0035f + DriftTimer) * 0.05f;
        Scale = new Vector2(breathe, breathe);
    }

    public override void _Draw()
    {
        // Draw golden cocci cluster with 3D sphere gradient and electron specular highlights
        foreach (var s in _clusterSpheres)
        {
            Vector2 pos = s.Offset;
            float rad = s.Radius;
            Color col = s.Color;

            // 1. Peptidoglycan cell wall / capsule
            DrawCircle(pos, rad + 1.6f, new Color(0.65f, 0.42f, 0.05f, 0.85f));

            // 2. Main spherical cytoplasm
            DrawCircle(pos, rad, col);

            // 3. 3D Spherical volume light gradient
            DrawCircle(pos + new Vector2(-rad * 0.15f, -rad * 0.15f), rad * 0.72f, col.Lightened(0.18f));

            // 4. Glossy specular highlight (electron microscope vibe with HDR glow)
            DrawCircle(pos + new Vector2(-rad * 0.32f, -rad * 0.32f), rad * 0.28f, new Color(1.4f, 1.35f, 0.9f, 0.85f));
        }
    }

    public float GetAtpValue()
    {
        return AtpValue;
    }

    public float get_atp_value() => GetAtpValue();

    public void be_engulfed(Node2D? predator) => BeEngulfed(predator);

    public void BeEngulfed(Node2D? predator)
    {
        if (IsBeingEaten)
            return;
        IsBeingEaten = true;

        // Disable collision immediately
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

        // Ingestion visual tween: shrink and get pulled into predator center
        var tween = CreateTween().SetParallel(true);
        tween.TweenProperty(this, "global_position", predPos, 0.25)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.25)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.25);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            EmitSignal(SignalName.Digested, this);
            QueueFree();
        }));
    }
}
