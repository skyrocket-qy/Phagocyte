using Godot;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Varicella Zoster Virus (帶狀皰疹病毒)
/// Latent ambush virion. Drifts semi-transparently until host immune weakness triggers fierce ambush.
/// </summary>
public partial class VaricellaZosterEnemy : BaseEnemy
{
    private bool _isAwakened = false;

    public VaricellaZosterEnemy()
    {
        EnemyId = "varicella_zoster";
        DisplayNameKey = "PATHOGEN_ZOSTER_NAME";
        MaxHealth = 26.0f;
        CurrentHealth = 26.0f;
        AtpValue = 18.0f;
        BaseScore = 15;
        FloatSpeed = 35.0f;
    }

    protected override float GetCollisionRadius() => 14.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        if (!_isAwakened)
        {
            var player = (BaseCell?)GetTree().GetFirstNodeInGroup("player");
            if (player != null && GodotObject.IsInstanceValid(player))
            {
                // Awaken if player health drops below 75% or close proximity (<150px)
                float hpRatio = player.Health / Mathf.Max(1.0f, player.Stats != null ? player.Stats.GetStat("max_health") : 100.0f);
                float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
                if (hpRatio < 0.75f || dist < 150.0f)
                {
                    Awaken();
                }
            }
        }
        else
        {
            // Aggressive chase towards player
            var player = (BaseCell?)GetTree().GetFirstNodeInGroup("player");
            if (player != null && GodotObject.IsInstanceValid(player))
            {
                Vector2 toPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
                Velocity = Velocity.Lerp(toPlayer * 85.0f, 3.0f * dt);
            }
        }
    }

    public void Awaken()
    {
        if (_isAwakened)
            return;
        _isAwakened = true;
        FloatSpeed = 85.0f;

        // Flash into visible existence
        Modulate = Colors.Red * 2.0f;
        var tw = CreateTween();
        tw.TweenProperty(this, "modulate", Colors.White, 0.4);
        QueueRedraw();
    }

    public override void TakeDamage(float damage, Node2D? source = null)
    {
        if (!_isAwakened)
            Awaken();
        base.TakeDamage(damage, source);
    }

    public override void _Draw()
    {
        float alpha = _isAwakened ? 0.95f : 0.20f;
        Color envelopeCol = _isAwakened ? new Color(0.85f, 0.15f, 0.15f, alpha) : new Color(0.4f, 0.4f, 0.5f, alpha);
        Color coreCol = _isAwakened ? new Color(1.0f, 0.4f, 0.2f, alpha) : new Color(0.6f, 0.6f, 0.7f, alpha);

        // Viral envelope & icosahedral capsid
        DrawCircle(Vector2.Zero, 13.0f, envelopeCol);
        DrawCircle(Vector2.Zero, 8.0f, coreCol);

        // Tegument spikes
        int count = 8;
        for (int i = 0; i < count; i++)
        {
            float ang = i * (Mathf.Tau / count);
            Vector2 p1 = Vector2.FromAngle(ang) * 12.0f;
            Vector2 p2 = Vector2.FromAngle(ang) * 17.0f;
            DrawLine(p1, p2, envelopeCol, 1.8f);
        }
    }
}
