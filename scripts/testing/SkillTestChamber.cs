using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Core;
using Phagocyte.Player;
using Phagocyte.Skills;

namespace Phagocyte.Testing;

public enum DummyFormation
{
    Single,
    Line,
    Radial,
    Cluster,
    GroundHazard
}

/// <summary>
/// Isolated, cleanroom environment for testing active skills.
/// Features a static laboratory canvas with calibrated range rings,
/// an unmovable subject cell, non-exp target dummies, and 0% background noise.
/// </summary>
public partial class SkillTestChamber : Node2D
{
    public static readonly Vector2 Center = new(640, 360);

    public BaseCell? Subject { get; private set; }
    public BaseSkill? CurrentSkill { get; private set; }
    public List<TargetDummy> Dummies { get; } = new();

    private ColorRect? _bgRect;
    private ChamberGridOverlay? _gridOverlay;

    public override void _Ready()
    {
        Name = "SkillTestChamber";
        BuildChamberEnvironment();
    }

    private void BuildChamberEnvironment()
    {
        // 1. Static dark bio-laboratory backdrop (ColorRect)
        _bgRect = new ColorRect
        {
            Name = "ChamberBackground",
            Color = new Color(0.04f, 0.05f, 0.08f, 1.0f),
            OffsetLeft = 0.0f,
            OffsetTop = 0.0f,
            OffsetRight = 1280.0f,
            OffsetBottom = 720.0f
        };
        AddChild(_bgRect);

        // 2. Subtle scientific millimeter range grid
        _gridOverlay = new ChamberGridOverlay { Name = "ChamberGrid", Position = Center };
        AddChild(_gridOverlay);
    }

    public void SetupSubject(PackedScene cellScene)
    {
        if (Subject != null && GodotObject.IsInstanceValid(Subject))
        {
            RemoveChild(Subject);
            Subject.QueueFree();
            Subject = null;
        }

        Subject = cellScene.Instantiate<BaseCell>();
        AddChild(Subject);

        Subject.GlobalPosition = Center;
        Subject.Velocity = Vector2.Zero;
        Subject.SetPhysicsProcess(false);

        if (Subject.Stats != null)
        {
            Subject.Stats.SetBase("crit_chance", 0.0f);
            Subject.Stats.SetBase("evasion", 0.0f);
            Subject.Stats.SetBase("block", 0.0f);
            Subject.Stats.SetBase("max_health", 999999.0f);
        }
        Subject.Health = 999999.0f;
        Subject.ExpToNextLevel = int.MaxValue;
        Subject.CurrentExp = 0;
    }

    public void ClearDummies()
    {
        foreach (var dummy in Dummies)
        {
            if (dummy != null && GodotObject.IsInstanceValid(dummy))
            {
                if (dummy.GetParent() != null)
                    dummy.GetParent().RemoveChild(dummy);
                dummy.QueueFree();
            }
        }
        Dummies.Clear();
    }

    public TargetDummy SpawnDummy(Vector2 pos)
    {
        var dummy = new TargetDummy { GlobalPosition = pos };
        AddChild(dummy);
        Dummies.Add(dummy);
        return dummy;
    }

    public void ApplyFormation(DummyFormation formation)
    {
        ClearDummies();

        switch (formation)
        {
            case DummyFormation.Single:
                SpawnDummy(Center + new Vector2(160, 0));
                break;

            case DummyFormation.Line:
                SpawnDummy(Center + new Vector2(100, 0));
                SpawnDummy(Center + new Vector2(165, 0));
                SpawnDummy(Center + new Vector2(230, 0));
                break;

            case DummyFormation.Radial:
                int count = 8;
                float radius = 135.0f;
                for (int i = 0; i < count; i++)
                {
                    float angle = i * (Mathf.Tau / count);
                    SpawnDummy(Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
                break;

            case DummyFormation.Cluster:
                SpawnDummy(Center + new Vector2(140, -35));
                SpawnDummy(Center + new Vector2(185, 0));
                SpawnDummy(Center + new Vector2(150, 35));
                break;

            case DummyFormation.GroundHazard:
                SpawnDummy(Center + new Vector2(150, 0));
                break;
        }
    }

    public void ArmSkill(BaseSkill skill, int slot = 1)
    {
        if (Subject?.CellSkillManager == null)
            return;

        // Disarm any currently equipped skills to prevent cross-talk
        for (int i = 0; i < SkillManager.MaxActiveSlots; i++)
        {
            var s = Subject.CellSkillManager.ActiveSlots[i];
            if (s != null && GodotObject.IsInstanceValid(s))
            {
                s.IsInnate = false;
                s.CooldownTimer = 999999.0f;
                Subject.CellSkillManager.RemoveChild(s);
                s.QueueFree();
                Subject.CellSkillManager.ActiveSlots[i] = null;
            }
        }

        skill.IsInnate = (slot == 0);
        skill.CooldownTimer = 999999.0f; // Disable uncontrolled auto-firing
        Subject.CellSkillManager.EquipActive(skill, slot);
        CurrentSkill = skill;
    }

    public void TriggerSkill()
    {
        CurrentSkill?.Trigger();
    }

    public void ClearTransientVfx()
    {
        var toRemove = new List<Node>();
        foreach (var child in GetChildren())
        {
            if (child == _bgRect || child == _gridOverlay || child == Subject || (child is TargetDummy td && Dummies.Contains(td)))
                continue;

            toRemove.Add(child);
        }

        foreach (var n in toRemove)
        {
            RemoveChild(n);
            n.QueueFree();
        }
    }

    /// <summary>
    /// Renders faint static range rings (100px, 200px, 300px) and Cartesian axis crosshairs.
    /// Completely static: guarantees 0% visual noise between pre-cast and post-cast frames.
    /// </summary>
    private partial class ChamberGridOverlay : Node2D
    {
        public override void _Draw()
        {
            Color axisColor = new Color(0.12f, 0.18f, 0.28f, 0.45f);
            Color ringColor1 = new Color(0.15f, 0.25f, 0.38f, 0.35f);
            Color ringColor2 = new Color(0.12f, 0.20f, 0.32f, 0.25f);
            Color ringColor3 = new Color(0.10f, 0.16f, 0.26f, 0.20f);
            Color tickColor = new Color(0.20f, 0.32f, 0.48f, 0.50f);

            // Cartesian axes
            DrawLine(new Vector2(-640, 0), new Vector2(640, 0), axisColor, 1.0f);
            DrawLine(new Vector2(0, -360), new Vector2(0, 360), axisColor, 1.0f);

            // Concentric range circles
            DrawArc(Vector2.Zero, 100.0f, 0.0f, Mathf.Tau, 64, ringColor1, 1.0f);
            DrawArc(Vector2.Zero, 200.0f, 0.0f, Mathf.Tau, 96, ringColor2, 1.0f);
            DrawArc(Vector2.Zero, 300.0f, 0.0f, Mathf.Tau, 128, ringColor3, 1.0f);

            // Distance ticks every 50px along horizontal axis
            for (int x = -500; x <= 500; x += 50)
            {
                if (x == 0) continue;
                float tickH = (x % 100 == 0) ? 6.0f : 3.0f;
                DrawLine(new Vector2(x, -tickH), new Vector2(x, tickH), tickColor, 1.0f);
            }

            // Distance ticks every 50px along vertical axis
            for (int y = -300; y <= 300; y += 50)
            {
                if (y == 0) continue;
                float tickW = (y % 100 == 0) ? 6.0f : 3.0f;
                DrawLine(new Vector2(-tickW, y), new Vector2(tickW, y), tickColor, 1.0f);
            }
        }
    }
}
