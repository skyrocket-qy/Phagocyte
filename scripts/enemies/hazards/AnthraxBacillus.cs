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
        AtpValue = 22.0f;
        BaseScore = 100;
        FloatSpeed = 70.0f;
        ThreatMode = EnemyThreatMode.ChemoChaser;
    }

    protected override float GetCollisionRadius() => 14.0f;

    /// <summary>Damage multiplier applied to the toxin telegraph (berserk awakening = 1.5x).</summary>
    [Export] public float DamageMultiplier { get; set; } = 1.0f;

    private float _toxinTimer = 2.0f;

    protected override void CustomPhysicsProcess(float dt)
    {
        var player = PlayerRef;
        if (player != null && GodotObject.IsInstanceValid(player))
        {
            // Chasing is handled by EnemySteering; keep facing for the toxin telegraph.
            Vector2 toPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
            Rotation = toPlayer.Angle();

            _toxinTimer -= dt;
            if (_toxinTimer <= 0.0f)
            {
                _toxinTimer = 4.5f;
                SpawnToxinTelegraph(player.GlobalPosition);
            }
        }
    }

    private void SpawnToxinTelegraph(Vector2 targetPos)
    {
        var parent = GetParent();
        if (parent == null) return;

        var attack = new Phagocyte.Combat.TelegraphedAttack
        {
            Shape = Phagocyte.Combat.TelegraphAttackShape.Circle,
            GlobalPosition = targetPos,
            Radius = 70.0f,
            TelegraphDuration = 1.1f,
            Damage = 22.0f * DamageMultiplier,
            SourceEnemy = this
        };
        parent.AddChild(attack);
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
