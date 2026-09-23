using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Perforin Lance (穿孔素長矛)
/// Straight piercing ray that punctures through a line of pathogens.
/// </summary>
public partial class PerforinLanceSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 35.0f;
    [Export] public float AttackRange { get; set; } = 700.0f;

    public PerforinLanceSkill()
    {
        SkillId = SkillIds.PerforinLance;
        NameKey = "SKILL_PERFORIN_NAME";
        DescKey = "SKILL_PERFORIN_DESC";
        BioKey = "SKILL_PERFORIN_BIO";
        IconSymbol = "🗡️";
        IsInnate = true;
        IsPassive = false;
        Cooldown = 2.8f;
        CooldownTimer = 0.5f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        Vector2 targetDir = FindTargetDirection();
        int amount = GetCalculatedAmount(1);
        int pierceLimit = GetCalculatedPierce(3);

        if (amount > 0)
        {
            AudioManager.Instance?.PlayShoot();
        }

        for (int i = 0; i < amount; i++)
        {
            Vector2 dir = targetDir;
            if (amount > 1)
            {
                float angleOffset = (i - (amount - 1) / 2.0f) * 0.15f;
                dir = dir.Rotated(angleOffset);
            }
            ExecuteLanceStrike(dir, pierceLimit);
        }
    }

    private Vector2 FindTargetDirection()
    {
        if (Host == null)
            return Vector2.Right;

        Vector2 fallback = Host.Velocity.Length() > 20.0f ? Host.Velocity.Normalized() : Vector2.Right;
        return TargetingService.FindTargetDirection(Host, AttackRange, fallback);
    }

    private void ExecuteLanceStrike(Vector2 dir, int pierceLimit)
    {
        if (Host == null)
            return;

        Vector2 startPos = Host.GlobalPosition;
        Vector2 endPos = startPos + dir * AttackRange;
        var pathogens = Host.GetTree().GetNodesInGroup("pathogens");
        int hitCount = 0;
        float beamWidth = 24.0f * GetCalculatedArea(1.0f);

        // Polymerization flash along the strike axis.
        var beam = new LanceBeamVisual
        {
            GlobalPosition = startPos,
            BeamEnd = endPos - startPos,
            BeamWidth = beamWidth
        };
        Host.GetParent().AddChild(beam);

        foreach (var p in pathogens)
        {
            if (hitCount >= pierceLimit)
                break;

            if (p is Node2D n && GodotObject.IsInstanceValid(n))
            {
                // Beam geometry is a capsule (not a radius), so this scan stays
                // group-based; only pathogens not mid-engulf are pierced.
                if (n is BaseEnemy be && be.IsBeingEaten)
                    continue;

                Vector2 pPos = n.GlobalPosition;
                Vector2 projPoint = Geometry2D.GetClosestPointToSegment(pPos, startPos, endPos);
                if (projPoint.DistanceTo(pPos) <= beamWidth)
                {
                    hitCount++;
                    GetDamage(BaseDamage, out float dmg, out bool isCrit);

                    CombatHelper.DealDamage(n, dmg, Host, isCrit);

                    // Transmembrane pore decal: subunits assemble, then
                    // granzyme leaks inward through the finished ring.
                    var pore = new PoreDecal { GlobalPosition = pPos };
                    Host.GetParent().AddChild(pore);
                    VfxManager.Instance?.Play(VfxType.PerforinPore, pPos);
                }
            }
        }
    }

    /// <summary>0.18s lance flash: violet corona + white-hot core.</summary>
    public partial class LanceBeamVisual : Node2D
    {
        public Vector2 BeamEnd { get; set; } = Vector2.Right * 700.0f;
        public float BeamWidth { get; set; } = 24.0f;

        private const float Duration = 0.18f;
        private float _age = 0.0f;

        public override void _Ready()
        {
            ZIndex = 6;
        }

        public override void _Process(double delta)
        {
            _age += (float)delta;
            if (_age >= Duration)
            {
                QueueFree();
                return;
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            float a = 1.0f - _age / Duration;
            Color halo = SkillAssetPalette.Accent(SkillIds.PerforinLance, new Color(0.6f, 0.3f, 1.0f));
            Color core = SkillAssetPalette.Core(SkillIds.PerforinLance, new Color(0.85f, 0.95f, 1.0f));
            LaserGlow.DrawBeam(this, Vector2.Zero, BeamEnd, halo, core, BeamWidth, a);
            LaserGlow.DrawImpactHalo(this, BeamEnd, BeamWidth * 0.8f * a + 2.0f, halo, core, a * 0.7f);
        }
    }

    /// <summary>
    /// Perforin pore decal: arc-swept subunit assembly (0.3s), granzyme
    /// leak (lingering dots drift inward), fade by 0.8s.
    /// </summary>
    public partial class PoreDecal : Node2D
    {
        private const float AssembleTime = 0.3f;
        private const float Duration = 0.8f;
        private float _age = 0.0f;
        private readonly Vector2[] _leakSeeds = new Vector2[5];

        public PoreDecal()
        {
            for (int i = 0; i < _leakSeeds.Length; i++)
            {
                float a = (float)GD.RandRange(0.0f, Mathf.Tau);
                float r = (float)GD.RandRange(14.0f, 22.0f);
                _leakSeeds[i] = Vector2.FromAngle(a) * r;
            }
        }

        public override void _Ready()
        {
            ZIndex = 6;
        }

        public override void _Process(double delta)
        {
            _age += (float)delta;
            if (_age >= Duration)
            {
                QueueFree();
                return;
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            float fade = 1.0f - Mathf.Clamp((_age - AssembleTime) / (Duration - AssembleTime), 0.0f, 1.0f);
            float sweep = Mathf.Clamp(_age / AssembleTime, 0.0f, 1.0f);

            // Assembling ~20-mer ring pore, tinted from the skill icon.
            Color poreAccent = SkillAssetPalette.Accent(SkillIds.PerforinLance, new Color(0.55f, 0.95f, 1.0f));
            Color ringColor = new Color(poreAccent.R, poreAccent.G, poreAccent.B, fade);
            DrawArc(Vector2.Zero, 11.0f, -Mathf.Pi * 0.5f, -Mathf.Pi * 0.5f + sweep * Mathf.Tau, 32, ringColor, 2.5f);
            DrawCircle(Vector2.Zero, 5.0f, new Color(0.02f, 0.05f, 0.1f, 0.7f * fade));

            // Granzyme leak: seeds drift toward the pore after assembly.
            if (_age > AssembleTime * 0.5f)
            {
                float leakT = Mathf.Clamp((_age - AssembleTime * 0.5f) / AssembleTime, 0.0f, 1.0f);
                Color leakColor = new Color(0.9f, 0.5f, 1.0f, fade * 0.9f);
                foreach (var seed in _leakSeeds)
                {
                    Vector2 p = seed.Lerp(Vector2.Zero, leakT);
                    DrawCircle(p, 1.8f, leakColor);
                }
            }
        }
    }
}
