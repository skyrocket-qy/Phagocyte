using Godot;
using Phagocyte.Core;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Antigen-Drift Influenza Virus (æŠ—åŽŸæ¼‚ç§»æµæ„Ÿç—…æ¯’)
/// Elite enemy. High-frequency HA/NA antigenic drift periodically resets resistance and mutations.
/// </summary>
public partial class FluDriftEnemy : BaseEnemy
{
    private float _driftMutationTimer = 0.0f;
    private const float DriftCooldown = 25.0f;
    private Color _currentHue = new Color(0.25f, 0.65f, 0.85f, 0.95f);
    private const float DrawScale = 9.0f / 18.0f; // A-formula visual match: 18px -> 9px

    public FluDriftEnemy()
    {
        EnemyId = "flu_drift";
        DisplayNameKey = "PATHOGEN_FLUDRIFT_NAME";
        MaxHealth = 65.0f;
        AtpValue = 26.0f;
        BaseScore = 100;
        FloatSpeed = 42.0f;
        ThreatMode = EnemyThreatMode.ChemoChaser;
        IsElite = true;
    }

    protected override float GetCollisionRadius() => Morphology.RealSizeToRadius(0.12f);

    /// <summary>Shifting antigen hue for the GPU swarm batch renderer (docs/spec.md Â§9).</summary>
    public override Color SwarmBatchColor => _currentHue;

    protected override void CustomPhysicsProcess(float dt)
    {
        _driftMutationTimer += dt;
        if (_driftMutationTimer >= DriftCooldown)
        {
            _driftMutationTimer = 0.0f;
            TriggerAntigenicDrift();
        }
    }

    private void TriggerAntigenicDrift()
    {
        // 1. Shift hue to new antigen conformation
        _currentHue = new Color(GD.Randf(), GD.Randf(), GD.Randf(), 0.95f).Lerp(Colors.Cyan, 0.3f);

        // 2. Shockwave pushing player back
        var player = PlayerRef;
        if (player != null && GodotObject.IsInstanceValid(player))
        {
            Vector2 diff = player.GlobalPosition - GlobalPosition;
            if (diff.Length() < 250.0f)
            {
                player.Velocity += diff.Normalized() * 200.0f;
            }
        }

        // 3. Flash effect
        FlashModulate(Colors.White * 2.0f, 0.3f);

        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0.0f, new Vector2(DrawScale, DrawScale));
        // 1. Shifting antigenic aura
        DrawCircle(Vector2.Zero, 22.0f, new Color(_currentHue.R, _currentHue.G, _currentHue.B, 0.25f));

        // 2. Main lipid envelope
        DrawCircle(Vector2.Zero, 14.0f, _currentHue);
        DrawCircle(Vector2.Zero, 9.0f, _currentHue.Lightened(0.2f));

        // 3. Alternating HA (rods) and NA (mushroom tetramers) surface markers
        int spikes = 14;
        for (int i = 0; i < spikes; i++)
        {
            float ang = i * (Mathf.Tau / spikes);
            Vector2 basePos = Vector2.FromAngle(ang) * 13.0f;
            Vector2 tipPos = Vector2.FromAngle(ang) * 20.0f;

            if (i % 2 == 0)
            {
                // Hemagglutinin rod
                DrawLine(basePos, tipPos, Colors.LightBlue, 2.0f);
            }
            else
            {
                // Neuraminidase mushroom
                DrawLine(basePos, tipPos, Colors.Coral, 2.0f);
                DrawCircle(tipPos, 2.2f, Colors.Coral);
            }
        }
        DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
    }
}
