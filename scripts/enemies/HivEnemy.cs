using Godot;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Human Immunodeficiency Virus / Retrovirus (人類免疫缺乏病毒)
/// Targets CD4 core. Resource predator: steals ATP and EXP directly without hurting HP.
/// </summary>
public partial class HivEnemy : BaseEnemy
{
    private float _siphonCooldown = 0.0f;

    public HivEnemy()
    {
        EnemyId = "hiv";
        DisplayNameKey = "PATHOGEN_HIV_NAME";
        MaxHealth = 24.0f;
        CurrentHealth = 24.0f;
        AtpValue = 14.0f;
        BaseScore = 35;
        FloatSpeed = 48.0f;
    }

    protected override float GetCollisionRadius() => 14.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _siphonCooldown -= dt;
        var player = (BaseCell?)GetTree().GetFirstNodeInGroup("player");

        if (player != null && GodotObject.IsInstanceValid(player))
        {
            // Direct homing toward player core
            Vector2 toPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
            Velocity = Velocity.Lerp(toPlayer * FloatSpeed, 2.5f * dt);

            // Check contact with player core
            if (_siphonCooldown <= 0.0f && GlobalPosition.DistanceTo(player.GlobalPosition) < (player.CurrentRadius + 8.0f))
            {
                _siphonCooldown = 1.5f;
                // Siphon ATP and energy
                player.DrainAtp(15.0f);

                // Visual siphon line
                Modulate = new Color(0.2f, 1.5f, 1.5f, 1.0f);
                var tw = CreateTween();
                tw.TweenProperty(this, "modulate", Colors.White, 0.3);
            }
        }
    }

    public override void _Draw()
    {
        // 1. Envelope lipid membrane
        Color envelopeColor = new Color(0.25f, 0.15f, 0.35f, 0.95f);
        DrawCircle(Vector2.Zero, 12.0f, envelopeColor);

        // 2. Conical fullerene bullet capsid inside
        Color capsidColor = new Color(0.45f, 0.30f, 0.60f, 1.0f);
        Vector2[] capsidPoints = new Vector2[]
        {
            new Vector2(-6, -3),
            new Vector2(6, -5),
            new Vector2(7, 5),
            new Vector2(-6, 3)
        };
        DrawColoredPolygon(capsidPoints, capsidColor);

        // 3. gp120/gp41 glycoprotein spikes
        Color gp120Color = new Color(0.2f, 0.85f, 0.95f, 0.95f);
        int spikes = 10;
        for (int i = 0; i < spikes; i++)
        {
            float ang = i * (Mathf.Tau / spikes);
            Vector2 basePos = Vector2.FromAngle(ang) * 11.0f;
            Vector2 headPos = Vector2.FromAngle(ang) * 16.0f;
            DrawLine(basePos, headPos, gp120Color, 1.8f);
            DrawCircle(headPos, 2.0f, gp120Color);
        }
    }
}
