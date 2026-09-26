using Godot;
using System;
using Phagocyte.Combat;
using Phagocyte.Core;
using Phagocyte.Enemies;

namespace Phagocyte.Skills;

/// <summary>
/// Lysosomal Overload (Active Cytokine Weapon - Spell, Trap, AOE, Duration).
/// Drops an unstable hydrolytic enzymatic vesicle trap that bursts into a high-pH
/// corrosive acid pool, inflicting continuous hydrolytic erosion and membrane shred.
/// </summary>
public partial class LysosomalOverloadSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 20.0f;
    [Export] public float BaseRadius { get; set; } = 65.0f;
    [Export] public float BaseDuration { get; set; } = 4.0f;
    [Export] public float SearchRange { get; set; } = 450.0f;

    public LysosomalOverloadSkill()
    {
        SkillId = SkillIds.LysosomalOverload;
        IconSymbol = "🧪";
        NameKey = "SKILL_LYSOSOME_NAME";
        DescKey = "SKILL_LYSOSOME_DESC";
        BioKey = "SKILL_LYSOSOME_BIO";
        Cooldown = 4.5f;
        CooldownTimer = 1.0f;
        Level = 1;
        MaxLevel = 5;
        IsPassive = false;
        IsInnate = false;
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;

        float dmgVal = GetBaseDamageForLevel(BaseDamage);
        GetDamage(dmgVal, out float dmg, out bool isCrit);

        float radius = GetCalculatedArea(BaseRadius);
        float duration = GetCalculatedDuration(BaseDuration);

        // Find nearest target or target in forward velocity
        Vector2 deployPos;
        var nearest = TargetingService.FindNearest(Host, SearchRange);
        if (nearest != null)
        {
            deployPos = nearest.GlobalPosition;
        }
        else
        {
            Vector2 forward = Host.Velocity.LengthSquared() > 1.0f ? Host.Velocity.Normalized() : Vector2.Right;
            deployPos = Host.GlobalPosition + forward * 100.0f;
        }

        var pool = new LysosomeAcidPool(deployPos, radius, duration, dmg, isCrit, Host);
        Host.GetParent().AddChild(pool);

        AudioManager.Instance?.PlaySfx("desecrate");
    }

    /// <summary>
    /// Procedurally rendered hydrolytic acid hazard pool with bubbling enzymatic vesicles.
    /// </summary>
    public partial class LysosomeAcidPool : Node2D
    {
        public float Radius { get; }
        public float Duration { get; }
        public float DamagePerTick { get; }
        public bool IsCrit { get; }
        public CharacterBody2D? HostRef { get; }

        private float _lifetime = 0.0f;
        private float _tickTimer = 0.0f;
        private float _bubblePhase = 0.0f;

        private readonly Color _accentColor;
        private readonly Color _coreColor;

        public LysosomeAcidPool(Vector2 pos, float radius, float duration, float damage, bool isCrit, CharacterBody2D? host)
        {
            GlobalPosition = pos;
            Radius = radius;
            Duration = duration;
            DamagePerTick = damage;
            IsCrit = isCrit;
            HostRef = host;
            ZIndex = 8;

            _accentColor = SkillAssetPalette.Accent(SkillIds.LysosomalOverload, new Color(0.71f, 0.90f, 0.11f));
            _coreColor = SkillAssetPalette.Core(SkillIds.LysosomalOverload, Colors.White);
        }

        public override void _Ready()
        {
            _bubblePhase = GD.Randf() * 10.0f;
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;
            _lifetime += dt;
            _bubblePhase += dt * 5.0f;

            if (_lifetime >= Duration)
            {
                QueueFree();
                return;
            }

            _tickTimer += dt;
            if (_tickTimer >= 0.30f)
            {
                _tickTimer = 0.0f;
                TickDamage();
            }

            QueueRedraw();
        }

        private void TickDamage()
        {
            TargetingService.ForEachInRadius(GlobalPosition, Radius, n =>
            {
                CombatHelper.DealDamage(n, DamagePerTick, HostRef, IsCrit);

                if (n is Node node && node.GetNodeOrNull<AilmentController>("AilmentController") is AilmentController ac)
                {
                    // Hydrolytic enzyme acid burn + membrane permeability leak
                    ac.ApplyOxidativeBurn(DamagePerTick * 0.4f, 1.5f);
                    ac.ApplyMembraneLeak(DamagePerTick * 0.3f, 2.0f);
                }
            });
        }

        public override void _Draw()
        {
            float alpha = Mathf.Clamp(1.0f - (_lifetime / Duration), 0.0f, 1.0f);
            float pulse = 1.0f + 0.04f * Mathf.Sin(_bubblePhase * 2.0f);
            float curRadius = Radius * pulse;

            // 1. Amoebic lobed contour polygon
            const int Segments = 20;
            var points = new Vector2[Segments];
            for (int i = 0; i < Segments; i++)
            {
                float theta = i * (Mathf.Tau / Segments);
                float rOffset = Mathf.Sin(theta * 4.0f + _bubblePhase) * 4.5f
                              + Mathf.Cos(theta * 2.0f - _bubblePhase * 0.7f) * 3.0f;
                float r = curRadius + rOffset;
                points[i] = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * r;
            }

            Color baseFill = new Color(_accentColor.R, _accentColor.G, _accentColor.B, 0.28f * alpha);
            Color contourLine = new Color(_coreColor.R, _coreColor.G, _coreColor.B, 0.75f * alpha);
            DrawColoredPolygon(points, baseFill);
            DrawPolyline(points, contourLine, 2.0f);

            // 2. Central hydrolytic excitation halo
            LaserGlow.DrawImpactHalo(this, Vector2.Zero, curRadius * 0.45f, _accentColor, _coreColor, alpha, 2.5f);

            // 3. Bubbling digestive vesicles
            for (int i = 0; i < 5; i++)
            {
                float angle = i * (Mathf.Tau / 5.0f) + _bubblePhase * 0.3f;
                float dist = curRadius * 0.55f + Mathf.Sin(_bubblePhase + i * 2.0f) * 8.0f;
                Vector2 bPos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
                float bRad = 3.5f + Mathf.Sin(_bubblePhase * 2.5f + i) * 1.5f;

                Color bColor = new Color(_coreColor.R, _coreColor.G, _coreColor.B, 0.7f * alpha);
                DrawCircle(bPos, bRad, bColor);
                DrawArc(bPos, bRad + 1.5f, 0, Mathf.Tau, 12, new Color(_accentColor.R, _accentColor.G, _accentColor.B, 0.5f * alpha), 1.2f);
            }
        }
    }
}
