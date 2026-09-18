using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Core;
using Phagocyte.Player;

namespace Phagocyte.Combat;

public enum TelegraphAttackShape
{
    Circle,
    Line,
    MultiCircle
}

/// <summary>
/// Ground danger telegraph indicator for elite and boss pathogens.
/// Visualizes an impending lethal strike with charging animations before resolving hits against BaseCell.
/// </summary>
public partial class TelegraphedAttack : Node2D
{
    public static readonly List<TelegraphedAttack> ActiveAttacks = new();

    [Export] public TelegraphAttackShape Shape { get; set; } = TelegraphAttackShape.Circle;
    [Export] public float Radius { get; set; } = 80.0f;
    [Export] public float LineLength { get; set; } = 180.0f;
    [Export] public float LineWidth { get; set; } = 40.0f;
    [Export] public Vector2 TargetDirection { get; set; } = Vector2.Right;
    [Export] public float TelegraphDuration { get; set; } = 1.2f;
    [Export] public float Damage { get; set; } = 25.0f;
    [Export] public Node2D? SourceEnemy { get; set; }

    public event Action? OnImpactExecuted;

    private float _timer = 0.0f;
    private static readonly Vector2[] MultiCircleOffsets =
    [
        Vector2.Zero,
        new Vector2(-0.85f, 0.75f),
        new Vector2(0.85f, 0.75f)
    ];

    public override void _EnterTree()
    {
        base._EnterTree();
        if (!ActiveAttacks.Contains(this))
            ActiveAttacks.Add(this);
    }

    public override void _Ready()
    {
        if (!ActiveAttacks.Contains(this))
            ActiveAttacks.Add(this);
        AddToGroup("telegraphed_attacks");

        if (TargetDirection.LengthSquared() > 0.001f)
        {
            Rotation = TargetDirection.Angle();
        }
    }

    public override void _ExitTree()
    {
        ActiveAttacks.Remove(this);
        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _timer += dt;

        QueueRedraw();

        if (_timer >= TelegraphDuration)
        {
            ExecuteImpact();
            QueueFree();
        }
    }

    public override void _Draw()
    {
        float progress = Mathf.Clamp(_timer / Mathf.Max(0.01f, TelegraphDuration), 0.0f, 1.0f);

        // Biohazard microscope colors
        Color borderCol = new Color(1.0f, 0.22f, 0.28f, 0.85f + 0.15f * Mathf.Sin(_timer * 15.0f));
        Color bgFaintCol = new Color(0.9f, 0.1f, 0.15f, 0.15f);
        Color fillChargingCol = new Color(1.0f, 0.15f + 0.25f * progress, 0.1f, 0.25f + 0.45f * progress);

        if (Shape == TelegraphAttackShape.Line)
        {
            Rect2 totalRect = new Rect2(0, -LineWidth / 2.0f, LineLength, LineWidth);
            Rect2 chargeRect = new Rect2(0, -LineWidth / 2.0f, LineLength * progress, LineWidth);

            DrawRect(totalRect, bgFaintCol);
            DrawRect(chargeRect, fillChargingCol);
            DrawRect(totalRect, borderCol, filled: false, width: 2.0f);

            // Center charging laser tracer
            DrawLine(new Vector2(0, 0), new Vector2(LineLength * progress, 0), borderCol, 1.5f);
        }
        else if (Shape == TelegraphAttackShape.MultiCircle)
        {
            float subRadius = Radius * 0.7f;
            for (int i = 0; i < MultiCircleOffsets.Length; i++)
            {
                Vector2 center = MultiCircleOffsets[i] * Radius;
                DrawCircle(center, subRadius, bgFaintCol);
                DrawCircle(center, subRadius * progress, fillChargingCol);
                DrawArc(center, subRadius, 0, Mathf.Tau, 28, borderCol, 2.0f);
            }
        }
        else // Circle
        {
            DrawCircle(Vector2.Zero, Radius, bgFaintCol);
            DrawCircle(Vector2.Zero, Radius * progress, fillChargingCol);
            DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 36, borderCol, 2.5f);

            // Expanding inner ripple
            float rippleProgress = Mathf.PosMod(progress * 2.0f, 1.0f);
            DrawArc(Vector2.Zero, Radius * rippleProgress, 0, Mathf.Tau, 32, new Color(1f, 0.8f, 0.2f, (1.0f - rippleProgress) * 0.7f), 1.5f);
        }
    }

    public void ExecuteImpact()
    {
        OnImpactExecuted?.Invoke();

        var player = (BaseCell?)GetTree().GetFirstNodeInGroup("player");
        if (player == null || !GodotObject.IsInstanceValid(player))
            return;

        bool isHit = CheckHit(player.GlobalPosition);
        if (isHit)
        {
            // Trigger player 4-stage damage resolution pipeline (Evasion -> Block -> Armor -> HP)
            player.TakeDamage(Damage);

            // Apply push recoil away from impact
            Vector2 pushDir = (player.GlobalPosition - GlobalPosition).Normalized();
            if (pushDir == Vector2.Zero) pushDir = Vector2.Up;
            player.Velocity += pushDir * 220.0f;

            AudioManager.Instance?.PlayPlayerHit();
        }
    }

    public bool CheckHit(Vector2 targetPos)
    {
        if (Shape == TelegraphAttackShape.Circle)
        {
            return targetPos.DistanceTo(GlobalPosition) <= Radius;
        }
        else if (Shape == TelegraphAttackShape.Line)
        {
            Vector2 localPos = ToLocal(targetPos);
            return Math.Abs(localPos.Y) <= LineWidth / 2.0f && localPos.X >= 0.0f && localPos.X <= LineLength;
        }
        else if (Shape == TelegraphAttackShape.MultiCircle)
        {
            float subRadius = Radius * 0.7f;
            for (int i = 0; i < MultiCircleOffsets.Length; i++)
            {
                Vector2 center = GlobalPosition + MultiCircleOffsets[i].Rotated(Rotation) * Radius;
                if (targetPos.DistanceTo(center) <= subRadius)
                {
                    return true;
                }
            }
            return false;
        }
        return false;
    }
}
