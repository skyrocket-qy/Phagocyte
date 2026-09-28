using Godot;

namespace Game.Combat;

/// <summary>
/// Transient expanding shockwave ring (hit feedback for bursts, pulses and
/// shell breaks). Pure visual: spawns, animates, frees. No simulation writes.
/// </summary>
public partial class ShockRing : Node2D
{
    public float MaxRadius { get; set; } = 900.0f;

    private const float Lifetime = 1.1f;
    private float _age;

    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age >= Lifetime)
            QueueFree();
        else
            QueueRedraw();
    }

    public override void _Draw()
    {
        float t = Mathf.Clamp(_age / Lifetime, 0.0f, 1.0f);
        float radius = MaxRadius * t;
        float alpha = 1.0f - t;
        DrawCircle(Vector2.Zero, radius, new Color(0.45f, 0.85f, 1.0f, alpha * 0.10f));
        DrawArc(Vector2.Zero, radius, 0.0f, Mathf.Tau, 64, new Color(0.65f, 0.95f, 1.0f, alpha), 4.0f);
    }
}
