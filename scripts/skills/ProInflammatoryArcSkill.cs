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

        private readonly List<Vector2[]> _segmentOffsets = new();

        public override void _Ready()
        {
            // Pre-seed consistent jagged path offsets so the lightning does not jitter
            for (int i = 0; i < Points.Count; i++)
            {
                Vector2 j1 = Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 16.0f;
                Vector2 j2 = Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 16.0f;
                Vector2 fork = Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 22.0f;
                _segmentOffsets.Add(new Vector2[] { j1, j2, fork });
            }
        }

        public override void _Process(double delta)
        {
            _age += (float)delta;
            if (_age >= 0.28f)
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

            float alpha = Mathf.Clamp(1.0f - (_age / 0.28f), 0.0f, 1.0f);
            Color accent = SkillAssetPalette.Accent(SkillIds.ProInflammatoryArc, new Color(0.89f, 0.65f, 0.22f));
            Color core = SkillAssetPalette.Core(SkillIds.ProInflammatoryArc, new Color(1.0f, 0.95f, 0.85f));

            Vector2 prev = StartPos;
            for (int i = 0; i < Points.Count; i++)
            {
                var pt = Points[i];
                Vector2 localPrev = ToLocal(prev);
                Vector2 localPt = ToLocal(pt);

                Vector2 j1 = (i < _segmentOffsets.Count) ? _segmentOffsets[i][0] : Vector2.Zero;
                Vector2 j2 = (i < _segmentOffsets.Count) ? _segmentOffsets[i][1] : Vector2.Zero;
                Vector2 fork = (i < _segmentOffsets.Count) ? _segmentOffsets[i][2] : Vector2.Zero;

                Vector2 mid1 = localPrev.Lerp(localPt, 0.33f) + j1;
                Vector2 mid2 = localPrev.Lerp(localPt, 0.66f) + j2;

                // 1. Ambient electrical diffusion halo (wide)
                DrawLine(localPrev, mid1, accent with { A = alpha * 0.3f }, 16.0f);
                DrawLine(mid1, mid2, accent with { A = alpha * 0.3f }, 16.0f);
                DrawLine(mid2, localPt, accent with { A = alpha * 0.3f }, 16.0f);

                // 2. High-energy cytokine lightning beam
                LaserGlow.DrawBeam(this, localPrev, mid1, accent, core, 9.0f, alpha);
                LaserGlow.DrawBeam(this, mid1, mid2, accent, core, 8.5f, alpha);
                LaserGlow.DrawBeam(this, mid2, localPt, accent, core, 9.0f, alpha);

                // 3. Side electric discharge fork
                Vector2 forkTip = mid1 + fork;
                DrawLine(mid1, forkTip, accent with { A = alpha * 0.7f }, 2.5f);
                DrawLine(mid1, forkTip, core with { A = alpha * 0.9f }, 1.0f);

                // 4. Inflammatory cytokine excitation node on target
                LaserGlow.DrawImpactHalo(this, localPt, 16.0f, accent, core, alpha * 0.95f, 2.5f);
                prev = pt;
            }
        }
    }
}
