using Godot;
using Phagocyte.Core;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Spike Coronaviral Virion (åˆºçªå† ç‹€ç—…æ¯’)
/// Anchors to receptors via S spike glycoprotein crown; applies adhesive slow down to player.
/// </summary>
public partial class SVirusEnemy : BaseEnemy
{
    private float _replicationTimer = 0.0f;
    private float _spikeTimer = 1.6f;
    private const float ReplicationThreshold = 18.0f;
    private const float SpikeInterval = 2.5f;
    private const float SpikeRange = 640.0f;
    private const float DrawScale = 8.0f / 15.0f; // A-formula visual match: 15px -> 8px

    public SVirusEnemy()
    {
        EnemyId = "s_virus";
        DisplayNameKey = "PATHOGEN_SVIRUS_NAME";
        MaxHealth = 22.0f;
        AtpValue = 10.0f;
        BaseScore = 15;
        FloatSpeed = 38.0f;
        ThreatMode = EnemyThreatMode.Standoff;
    }

    protected override float GetCollisionRadius() => Morphology.RealSizeToRadius(0.1f);

    public override float SteeringPreferredRange => 280.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _replicationTimer += dt;
        _spikeTimer -= dt;
        var player = PlayerRef;

        if (player != null && GodotObject.IsInstanceValid(player) && !player.IsDead)
        {
            // If overlapping player, apply receptor adhesive slow
            if (IsTouchingPlayer(12.0f))
            {
                player.ApplySlow(2.5f, 0.65f); // 35% speed reduction
            }

            // Standoff artillery: fire spike virions at range
            if (_spikeTimer <= 0.0f && GlobalPosition.DistanceTo(player.GlobalPosition) <= SpikeRange)
            {
                _spikeTimer = SpikeInterval;
                FireSpikePellet(player.GlobalPosition);
            }
        }

        // Replication attempt if survived long enough
        if (_replicationTimer >= ReplicationThreshold)
        {
            _replicationTimer = 0.0f;
            ReplicateClone();
        }
    }

    /// <summary>Fires a spike virion pellet at the given world position.</summary>
    public void FireSpikePellet(Vector2 targetPosition)
    {
        if (!EnemyPellet.CanSpawn())
            return;

        var parent = GetParent();
        if (parent == null)
            return;

        Vector2 direction = targetPosition - GlobalPosition;
        if (direction.LengthSquared() < 0.0001f)
            direction = Vector2.Right;
        direction = direction.Normalized();

        var pellet = new EnemyPellet
        {
            GlobalPosition = GlobalPosition + direction * 16.0f,
            Direction = direction,
            Speed = 300.0f,
            Damage = 9.0f,
            Lifetime = 5.0f,
            CoreColor = new Color(1.0f, 0.65f, 0.2f, 0.95f),
            AuraColor = new Color(0.95f, 0.4f, 0.4f, 0.35f)
        };
        parent.AddChild(pellet);
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
        DrawSetTransform(Vector2.Zero, 0.0f, new Vector2(DrawScale, DrawScale));
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
        DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
    }
}
