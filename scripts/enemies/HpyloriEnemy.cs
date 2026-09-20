using Godot;
using System;
using Phagocyte.Combat;

namespace Phagocyte.Enemies;

/// <summary>
/// Helicobacter pylori (幽門螺旋桿菌)
/// Helical corkscrew morphology drilling with alkaline urease shield neutralizing acid damage.
/// Tissue invader: ignores the player and latches onto host tissue, ulcerating it over time.
/// </summary>
public partial class HpyloriEnemy : BaseEnemy
{
    public const float UlcerPulseInterval = 3.0f;

    public int UlcerationPulses { get; private set; }

    private float _spinAngle = 0.0f;
    private float _ulcerTimer = 1.0f;

    public HpyloriEnemy()
    {
        EnemyId = "h_pylori";
        DisplayNameKey = "PATHOGEN_HPYLORI_NAME";
        MaxHealth = 32.0f;
        AtpValue = 16.0f;
        BaseScore = 35;
        FloatSpeed = 58.0f;
        Armor = 1.0f;
        ThreatMode = EnemyThreatMode.Invader;
    }

    protected override float GetCollisionRadius() => 14.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        _spinAngle += dt * 12.0f;
        Rotation += dt * 3.0f;

        // Latched to host tissue: ulcerate the anchor site
        if (EnemySteering.IsInvaderLatched(this))
        {
            _ulcerTimer -= dt;
            if (_ulcerTimer <= 0.0f)
            {
                _ulcerTimer = UlcerPulseInterval;
                EmitUlcerationPulse();
            }
        }
    }

    /// <summary>
    /// Secretes a short-lived VacA acid lesion at the latched tissue site.
    /// </summary>
    public void EmitUlcerationPulse()
    {
        var parent = GetParent();
        if (parent == null)
            return;

        HostUlceration.RegisterPulse();

        // Accumulated acid dissolves senescent RBCs at the ulceration site
        foreach (var node in GetTree().GetNodesInGroup("senescent_rbc"))
        {
            if (node is SenescentRBC rbc && GodotObject.IsInstanceValid(rbc)
                && GlobalPosition.DistanceTo(rbc.GlobalPosition) <= 64.0f)
            {
                rbc.Dissolve();
            }
        }

        var lesion = new BioHazardArea
        {
            GlobalPosition = GlobalPosition,
            Duration = 4.0f,
            Radius = 46.0f,
            Damage = 4.0f,
            TickInterval = 0.6f,
            SlowFactor = 0.6f,
            SlowsTarget = true,
            DealsDamage = true,
            CoreColor = new Color(0.55f, 0.75f, 0.20f, 0.30f),
            RimColor = new Color(0.75f, 0.95f, 0.35f, 0.60f)
        };
        parent.AddChild(lesion);

        UlcerationPulses++;
    }

    public override void _Draw()
    {
        // 1. Alkaline urease cloud / halo
        Color alkalineAura = new Color(0.35f, 0.75f, 0.95f, 0.25f);
        DrawCircle(Vector2.Zero, 18.0f, alkalineAura);

        // 2. Helical corkscrew body
        Color bodyColor = new Color(0.85f, 0.42f, 0.22f, 0.95f);
        Color coreColor = new Color(1.0f, 0.65f, 0.35f, 0.95f);

        Vector2 prev = new Vector2(-16, Mathf.Sin(_spinAngle) * 5.0f);
        for (int i = 1; i <= 10; i++)
        {
            float t = (float)i / 10.0f;
            float x = -16.0f + t * 32.0f;
            float y = Mathf.Sin(_spinAngle + t * Mathf.Tau * 2.0f) * 6.0f;
            Vector2 curr = new Vector2(x, y);
            DrawLine(prev, curr, bodyColor, 5.0f);
            DrawLine(prev, curr, coreColor, 2.5f);
            prev = curr;
        }

        // 3. Unipolar sheathed flagella tuft at head (16, 0)
        for (int f = -2; f <= 2; f++)
        {
            Vector2 fTip = new Vector2(24, f * 3.5f + Mathf.Sin(_spinAngle * 1.5f + f) * 4.0f);
            DrawLine(new Vector2(16, 0), fTip, new Color(0.9f, 0.6f, 0.3f, 0.7f), 1.4f);
        }
    }
}
