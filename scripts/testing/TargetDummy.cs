using Godot;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;

namespace Phagocyte.Testing;

/// <summary>
/// Static, non-aggressive biological test dummy for isolated skill testing.
/// Never drops EXP (AtpValue = 0), never moves (physics disabled),
/// maintains high/resetting health, and presents a standard HitArea on Layer 2.
/// </summary>
public partial class TargetDummy : BaseEnemy
{
    public float DamageAccumulated { get; private set; }
    public int HitsReceived { get; private set; }

    public TargetDummy()
    {
        EnemyId = "target_dummy";
        DisplayNameKey = "TARGET_DUMMY";
        MaxHealth = 999999.0f;
        CurrentHealth = 999999.0f;
        AtpValue = 0.0f;
        BaseScore = 0;
        FloatSpeed = 0.0f;
        ThreatMode = EnemyThreatMode.Drifter;
        Armor = 0.0f;
    }

    public override void _Ready()
    {
        base._Ready();
        SetPhysicsProcess(false);
        SetProcess(false);
    }

    protected override float GetCollisionRadius() => 20.0f;

    public override void TakeDamage(float damage, Node2D? source, bool isCrit)
    {
        HitsReceived++;
        DamageAccumulated += damage;
        CurrentHealth = MaxHealth; // Instant health reset so dummy never dies in tests
        VfxManager.Instance?.Play(VfxType.CytoplasmSplatter, GlobalPosition);
    }

    public override void TakeDoTDamage(float dotDamage)
    {
        HitsReceived++;
        DamageAccumulated += dotDamage;
        CurrentHealth = MaxHealth;
    }

    public override void Die(Node2D? killer)
    {
        // Suppress exp granting and queue-freeing
        CurrentHealth = MaxHealth;
    }

    public override void _Draw()
    {
        // Clean biological target dummy visual:
        // Outer concentric reticle rings with bio-cyan core and faint crosshairs
        Color ringColor = new Color(0.2f, 0.7f, 0.9f, 0.75f);
        Color coreColor = new Color(0.1f, 0.4f, 0.6f, 0.4f);
        Color crossColor = new Color(0.3f, 0.85f, 1.0f, 0.5f);

        DrawCircle(Vector2.Zero, 18.0f, coreColor);
        DrawArc(Vector2.Zero, 18.0f, 0.0f, Mathf.Tau, 32, ringColor, 2.0f);
        DrawArc(Vector2.Zero, 10.0f, 0.0f, Mathf.Tau, 24, ringColor * 0.8f, 1.5f);
        DrawCircle(Vector2.Zero, 3.5f, new Color(0.9f, 0.3f, 0.4f, 0.9f));

        // Crosshairs
        DrawLine(new Vector2(-24, 0), new Vector2(-12, 0), crossColor, 1.5f);
        DrawLine(new Vector2(12, 0), new Vector2(24, 0), crossColor, 1.5f);
        DrawLine(new Vector2(0, -24), new Vector2(0, -12), crossColor, 1.5f);
        DrawLine(new Vector2(0, 12), new Vector2(0, 24), crossColor, 1.5f);
    }
}
