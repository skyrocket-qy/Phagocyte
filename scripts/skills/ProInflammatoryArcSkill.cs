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
        if (!HasValidHost())
            return;

        int chainCount = GetCalculatedAmount(BaseChainCount);
        float baseDmg = GetBaseDamageForLevel(BaseDamage);
        GetDamage(baseDmg, out float dmg, out _);

        var targets = FindChainTargets(chainCount);
        if (targets.Count > 0)
        {
            AudioManager.Instance?.PlaySfx("spark");
            var arcVisual = new ArcLightningVisual { StartPos = Host.GlobalPosition };
            foreach (var t in targets)
            {
                arcVisual.Points.Add(t.GlobalPosition);
                CombatHelper.DealDamage(t, dmg, Host, false);
            }
            Host.GetParent().AddChild(arcVisual);
        }
    }

    private List<Node2D> FindChainTargets(int maxChains)
    {
        var result = new List<Node2D>();
        if (Host == null)
            return result;

        var first = TargetingService.FindNearest(Host, SearchRange);
        if (first == null)
            return result;

        result.Add(first);
        var current = first;

        for (int i = 1; i < maxChains; i++)
        {
            var next = TargetingService.FindNearest(
                current,
                ChainRange,
                enemy => !result.Contains(enemy));
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

            float alpha = Mathf.Clamp(1.0f - (_age / 0.22f), 0.0f, 1.0f);
            Color accent = SkillAssetPalette.Accent(SkillIds.ProInflammatoryArc, new Color(0.89f, 0.65f, 0.22f));
            Color core = SkillAssetPalette.Core(SkillIds.ProInflammatoryArc, new Color(1.0f, 0.95f, 0.85f));

            Vector2 prev = StartPos;
            foreach (var pt in Points)
            {
                Vector2 localPrev = ToLocal(prev);
                Vector2 localPt = ToLocal(pt);

                // Multi-segment jagged lightning step
                Vector2 mid1 = localPrev.Lerp(localPt, 0.33f) + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 14.0f;
                Vector2 mid2 = localPrev.Lerp(localPt, 0.66f) + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 14.0f;

                LaserGlow.DrawBeam(this, localPrev, mid1, accent, core, 6.0f, alpha);
                LaserGlow.DrawBeam(this, mid1, mid2, accent, core, 5.5f, alpha);
                LaserGlow.DrawBeam(this, mid2, localPt, accent, core, 6.0f, alpha);

                // Side electric discharge fork
                Vector2 forkTip = mid1 + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 18.0f;
                DrawLine(mid1, forkTip, new Color(accent.R, accent.G, accent.B, alpha * 0.5f), 1.5f);

                // Inflammatory cytokine excitation node on target
                LaserGlow.DrawImpactHalo(this, localPt, 12.0f, accent, core, alpha, 2.0f);
                prev = pt;
            }
        }
    }
}
