using Godot;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Spike Coronaviral Virion (刺突冠狀病毒)
/// Anchors to receptors via S spike glycoprotein crown; applies adhesive slow down to player.
/// </summary>
public partial class SVirusEnemy : BaseEnemy
{
    private float _replicationTimer = 0.0f;
    private const float ReplicationThreshold = 18.0f;

    public SVirusEnemy()
    {
        EnemyId = "s_virus";
        DisplayNameKey = "PATHOGEN_SVIRUS_NAME";
        MaxHealth = 22.0f;
        CurrentHealth = 22.0f;
        AtpValue = 10.0f;
        BaseScore = 15;
        FloatSpeed = 38.0f;
    }

    protected override float GetCollisionRadius() => 15.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _replicationTimer += dt;
        var player = (BaseCell?)GetTree().GetFirstNodeInGroup("player");

        if (player != null && GodotObject.IsInstanceValid(player))
        {
            // If overlapping player, apply receptor adhesive slow
            if (GlobalPosition.DistanceTo(player.GlobalPosition) < (player.CurrentRadius + 12.0f))
            {
                player.ApplySlow(2.5f, 0.65f); // 35% speed reduction
            }
        }

        // Replication attempt if survived long enough
        if (_replicationTimer >= ReplicationThreshold)
        {
            _replicationTimer = 0.0f;
            ReplicateClone();
        }
    }

    private void ReplicateClone()
    {
        var parent = GetParent();
        if (parent == null)
            return;

        var clone = new SVirusEnemy
        {
            GlobalPosition = GlobalPosition + new Vector2((float)GD.RandRange(-30, 30), (float)GD.RandRange(-30, 30))
        };
        parent.AddChild(clone);
    }

    public override void _Draw()
    {
        // 1. Core viral lipid envelope
        Color envelopeColor = new Color(0.68f, 0.18f, 0.28f, 0.95f);
        Color coreRnaColor = new Color(0.88f, 0.35f, 0.45f, 0.95f);

        DrawCircle(Vector2.Zero, 12.0f, envelopeColor);
        DrawCircle(Vector2.Zero, 8.0f, coreRnaColor);

        // 2. Trimeric S-Spike glycoprotein crowns radiating outward
        Color spikeStem = new Color(0.95f, 0.4f, 0.4f, 0.95f);
        Color spikeCrown = new Color(1.0f, 0.65f, 0.2f, 1.0f);

        int spikeCount = 12;
        for (int i = 0; i < spikeCount; i++)
        {
            float ang = i * (Mathf.Tau / spikeCount) + (float)Time.GetTicksMsec() * 0.001f;
            Vector2 basePos = Vector2.FromAngle(ang) * 11.0f;
            Vector2 headPos = Vector2.FromAngle(ang) * 18.0f;

            // Spike stem
            DrawLine(basePos, headPos, spikeStem, 2.0f);
            // Clover/club spike head
            DrawCircle(headPos, 2.8f, spikeCrown);
        }
    }
}
