using Godot;
using System;
using Phagocyte.Player;

namespace Phagocyte.Enemies;

/// <summary>
/// Candida albicans (白色念珠菌)
/// Dimorphic transition elite. Sprouts piercing hyphae when damaged or approached, puncturing pseudopods.
/// </summary>
public partial class CandidaEnemy : BaseEnemy
{
    private bool _hyphaeExtended = false;
    private float _hyphaeTimer = 0.0f;

    public override bool CanBeEngulfed => !_hyphaeExtended;

    public const float AmbushRange = 200.0f;
    public const float HyphaeReach = 150.0f;
    private const float HyphaeChannelTime = 1.6f;

    public bool IsHyphaeExtended => _hyphaeExtended;

    public CandidaEnemy()
    {
        EnemyId = "candida";
        DisplayNameKey = "PATHOGEN_CANDIDA_NAME";
        MaxHealth = 45.0f;
        CurrentHealth = 45.0f;
        AtpValue = 24.0f;
        BaseScore = 35;
        FloatSpeed = 32.0f;
        ThreatMode = EnemyThreatMode.ChemoChaser;
    }

    protected override float GetCollisionRadius() => 16.0f;

    /// <summary>
    /// The yeast body freezes in place while it channels its piercing pseudohyphae.
    /// </summary>
    protected override void HandleBrownianDrift(float dt)
    {
        if (_hyphaeExtended)
        {
            Velocity = Vector2.Zero;
            return;
        }

        base.HandleBrownianDrift(dt);
    }

    protected override void CustomPhysicsProcess(float dt)
    {
        var player = (BaseCell?)GetTree().GetFirstNodeInGroup("player");
        if (player != null && GodotObject.IsInstanceValid(player))
        {
            float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
            if (dist < AmbushRange && !_hyphaeExtended)
            {
                ExtendHyphae(player.GlobalPosition);
            }
        }

        if (_hyphaeExtended)
        {
            _hyphaeTimer -= dt;
            if (_hyphaeTimer <= 0.0f)
            {
                RetractHyphae();
            }
        }
    }

    public void ExtendHyphae(Vector2? targetPos = null)
    {
        _hyphaeExtended = true;
        _hyphaeTimer = HyphaeChannelTime;
        QueueRedraw();

        Vector2 aim = targetPos ?? (GlobalPosition + Vector2.Right * HyphaeReach);
        SpawnHyphaeTelegraph(aim);
    }

    private void SpawnHyphaeTelegraph(Vector2 targetPos)
    {
        var parent = GetParent();
        if (parent == null) return;

        Vector2 dir = (targetPos - GlobalPosition).Normalized();
        if (dir == Vector2.Zero) dir = Vector2.Right;

        var attack = new Phagocyte.Combat.TelegraphedAttack
        {
            Shape = Phagocyte.Combat.TelegraphAttackShape.Line,
            GlobalPosition = GlobalPosition,
            TargetDirection = dir,
            LineLength = HyphaeReach,
            LineWidth = 30.0f,
            TelegraphDuration = 0.9f,
            Damage = 18.0f,
            SourceEnemy = this
        };
        parent.AddChild(attack);
    }

    public void RetractHyphae()
    {
        _hyphaeExtended = false;
        QueueRedraw();
    }

    public override void TakeDamage(float damage, Node2D? source = null)
    {
        if (!_hyphaeExtended)
        {
            var p = (BaseCell?)GetTree().GetFirstNodeInGroup("player");
            ExtendHyphae(p != null && GodotObject.IsInstanceValid(p) ? p.GlobalPosition : null);
        }
        base.TakeDamage(damage, source);
    }

    public override void OnEngulfAttemptFailed(Node2D? predator)
    {
        if (_hyphaeExtended && predator is BaseCell cell)
        {
            // Puncture player cell membrane
            cell.TakeDamage(15.0f);
            cell.Velocity += (cell.GlobalPosition - GlobalPosition).Normalized() * 200.0f;
        }
    }

    public override void _Draw()
    {
        // 1. Oval yeast body
        Color yeastWall = new Color(0.88f, 0.85f, 0.72f, 0.95f);
        Color yeastCore = new Color(0.98f, 0.95f, 0.85f, 0.95f);

        DrawCircle(Vector2.Zero, 12.0f, yeastWall);
        DrawCircle(Vector2.Zero, 9.0f, yeastCore);

        // Budding daughter blastoconidium
        DrawCircle(new Vector2(10, -8), 6.0f, yeastWall);
        DrawCircle(new Vector2(10, -8), 4.5f, yeastCore);

        // 2. Piercing filamentous hyphae spikes when extended
        if (_hyphaeExtended)
        {
            Color hyphaeCol = new Color(0.65f, 0.2f, 0.25f, 0.95f);
            int spikeCount = 6;
            for (int i = 0; i < spikeCount; i++)
            {
                float ang = i * (Mathf.Tau / spikeCount) + 0.3f;
                Vector2 basePos = Vector2.FromAngle(ang) * 11.0f;
                Vector2 tipPos = Vector2.FromAngle(ang) * 28.0f;
                DrawLine(basePos, tipPos, hyphaeCol, 3.0f);
                DrawCircle(tipPos, 2.5f, Colors.Crimson);
            }
        }
    }
}
