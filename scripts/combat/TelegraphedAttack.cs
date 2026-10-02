using Godot;
using System;
using System.Collections.Generic;
using Game.Camera;
using Game.Core;
using Game.Player;

namespace Game.Combat;

public enum TelegraphAttackShape
{
    Circle,
    Line,
    MultiCircle
}

/// <summary>
/// Ground danger telegraph indicator for elite and boss enemies.
/// Visualizes an impending lethal strike with charging animations before resolving hits against PlayerActor.
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
        TelegraphVisualPresenter.Draw(this, new TelegraphVisualState(Shape, progress, _timer, Radius, LineLength, LineWidth, MultiCircleOffsets));
    }

    public void ExecuteImpact()
    {
        var player = (PlayerActor?)GetTree().GetFirstNodeInGroup("player");
        if (player == null || !GodotObject.IsInstanceValid(player))
            return;

        bool isHit = CheckHit(player.GlobalPosition);
        if (isHit)
        {
            HitPresenter.ResolveAndPresent(new HitPayload
            {
                RawDamage = Damage,
                SourceFaction = Team.Enemy,
                AttackerId = GetInstanceId(),
            }, player);

            // Apply push recoil away from impact
            Vector2 pushDir = (player.GlobalPosition - GlobalPosition).Normalized();
            if (pushDir == Vector2.Zero) pushDir = Vector2.Up;
            player.Velocity += pushDir * 220.0f;

            AudioManager.Instance?.PlayPlayerHit();
            CameraFollow.Instance?.AddTrauma(0.5f);
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
