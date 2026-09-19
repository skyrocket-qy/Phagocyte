using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Combat;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Pro-Inflammatory Arc (促炎因子電弧)
/// Interleukin-1 bio-electric chain lightning arcing between 3-7 adjacent pathogens.
/// </summary>
public partial class ProInflammatoryArcSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 35.0f;
    [Export] public float SearchRange { get; set; } = 480.0f;
    [Export] public float ChainRange { get; set; } = 240.0f;
    [Export] public int BaseChainCount { get; set; } = 3;

    public ProInflammatoryArcSkill()
    {
        SkillId = SkillIds.ProInflammatoryArc;
        NameKey = "SKILL_PRO_INFLAM_NAME";
        DescKey = "SKILL_PRO_INFLAM_DESC";
        BioKey = "SKILL_PRO_INFLAM_BIO";
        IconSymbol = "⚡";
        IsInnate = false;
        IsPassive = false;
        Cooldown = 3.0f;
        CooldownTimer = 1.0f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (Host == null || !GodotObject.IsInstanceValid(Host))
            return;

        int chainCount = GetCalculatedAmount(BaseChainCount);
        var dmgData = GetCalculatedDamage(BaseDamage);
        float dmg = (float)dmgData["damage"];

        var targets = FindChainTargets(chainCount);
        if (targets.Count > 0)
        {
            var arcVisual = new ArcLightningVisual { StartPos = Host.GlobalPosition };
            foreach (var t in targets)
            {
                arcVisual.Points.Add(t.GlobalPosition);
                if (t.HasMethod("take_damage"))
                    t.Call("take_damage", dmg);
                else if (t.HasMethod("be_engulfed"))
                    t.Call("be_engulfed", Host);
            }
            Host.GetParent().AddChild(arcVisual);
        }
    }

    private List<Node2D> FindChainTargets(int maxChains)
    {
        var result = new List<Node2D>();
        if (Host == null)
            return result;

        var first = TargetingService.FindNearest(Host, SearchRange, skipEaten: false);
        if (first == null)
            return result;

        result.Add(first);
        var current = first;

        for (int i = 1; i < maxChains; i++)
        {
            var next = TargetingService.FindNearest(
                current,
                ChainRange,
                enemy => !result.Contains(enemy),
                skipEaten: false);
            if (next == null)
                break;

            result.Add(next);
            current = next;
        }

        return result;
    }

    public partial class ArcLightningVisual : Node2D
    {
        public Vector2 StartPos { get; set; }
        public List<Vector2> Points { get; set; } = new();
        private float _age = 0.0f;

        public override void _Process(double delta)
        {
            _age += (float)delta;
            if (_age >= 0.22f)
            {
                QueueFree();
                return;
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (Points.Count == 0)
                return;

            Vector2 prev = StartPos;
            Color arcColor = new Color(0.4f, 0.8f, 1.0f, 1.0f - (_age / 0.22f));

            foreach (var pt in Points)
            {
                // Jittered lightning line
                Vector2 mid = (prev + pt) * 0.5f + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 12.0f;
                DrawLine(ToLocal(prev), ToLocal(mid), arcColor, 2.5f);
                DrawLine(ToLocal(mid), ToLocal(pt), arcColor, 2.5f);
                DrawCircle(ToLocal(pt), 6.0f, new Color(0.8f, 0.95f, 1.0f, arcColor.A));
                prev = pt;
            }
        }
    }
}
