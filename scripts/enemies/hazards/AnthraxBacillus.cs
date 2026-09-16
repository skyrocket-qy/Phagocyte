using Godot;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Vegetative virulent form of Bacillus anthracis hatched from a broken spore.
/// Highly aggressive rod with square ends (boxcar/bamboo shape).
/// </summary>
public partial class AnthraxBacillus : BaseEnemy
{
    public AnthraxBacillus()
    {
        EnemyId = "anthrax_bacillus";
        DisplayNameKey = "PATHOGEN_ANTHRAX_NAME";
        MaxHealth = 35.0f;
        CurrentHealth = 35.0f;
        AtpValue = 22.0f;
        FloatSpeed = 70.0f;
    }

    protected override float GetCollisionRadius() => 14.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        var player = (BaseCell?)GetTree().GetFirstNodeInGroup("player");
        if (player != null && GodotObject.IsInstanceValid(player))
        {
            Vector2 toPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
            Velocity = Velocity.Lerp(toPlayer * FloatSpeed, 2.5f * dt);
            Rotation = toPlayer.Angle();
        }
    }

    public override void _Draw()
    {
        // Bamboo/boxcar rod body
        Color capsuleColor = new Color(0.42f, 0.28f, 0.48f, 0.95f);
        Color coreColor = new Color(0.68f, 0.45f, 0.75f, 1.0f);

        DrawRect(new Rect2(-14, -7, 28, 14), capsuleColor);
        DrawRect(new Rect2(-11, -4, 22, 8), coreColor);
        // Translucent poly-D-glutamic acid capsule boundary
        DrawRect(new Rect2(-16, -9, 32, 18), new Color(0.85f, 0.7f, 0.95f, 0.45f), false, 1.5f);
    }
}
