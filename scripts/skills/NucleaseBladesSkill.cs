using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.Combat;
using Phagocyte.Core;

namespace Phagocyte.Skills;

/// <summary>
/// Active Weapon: Nuclease Blades (核酸降解旋刃)
/// Orbiting DNase/RNase cutter blades slicing through pathogens around the cell.
/// </summary>
public partial class NucleaseBladesSkill : BaseSkill
{
    [Export] public float BaseDamage { get; set; } = 28.0f;
    [Export] public float BaseOrbitRadius { get; set; } = 115.0f;
    [Export] public float BaseRotationSpeed { get; set; } = 3.8f; // rad/s

    private float _currentOrbitAngle = 0.0f;
    private BladesCanvas? _canvas;

    public partial class BladesCanvas : Node2D
    {
        public NucleaseBladesSkill? Skill { get; set; }
        public float OrbitAngle { get; set; } = 0.0f;
        public float OrbitRadius { get; set; } = 115.0f;
        public int BladeCount { get; set; } = 2;

        public override void _Draw()
        {
            if (Skill == null || BladeCount <= 0)
                return;

            Color accent = SkillAssetPalette.Accent(SkillIds.NucleaseBlades, new Color(0.25f, 0.77f, 0.44f));
            Color core = SkillAssetPalette.Core(SkillIds.NucleaseBlades, new Color(0.75f, 1.0f, 0.85f));
            Color trailColor = new Color(accent.R, accent.G, accent.B, 0.45f);

            for (int i = 0; i < BladeCount; i++)
            {
                float angle = OrbitAngle + i * (Mathf.Tau / BladeCount);
                Vector2 bladePos = Vector2.FromAngle(angle) * OrbitRadius;
                Vector2 tangent = Vector2.FromAngle(angle + Mathf.Pi * 0.5f);

                // Crescent scythe enzyme cutter blade geometry
                Vector2 bladeTip = bladePos + tangent * 24.0f;
                Vector2 bladeBack = bladePos - tangent * 16.0f;
                Vector2 bladeSpine = bladePos + Vector2.FromAngle(angle) * 11.0f;
                Vector2 bladeInner = bladePos - Vector2.FromAngle(angle) * 3.0f;

                // 1. Orbital wake arc
                DrawArc(Vector2.Zero, OrbitRadius, angle - 0.45f, angle, 16, accent with { A = 0.45f }, 3.5f);
                DrawArc(Vector2.Zero, OrbitRadius, angle - 0.25f, angle, 12, core with { A = 0.8f }, 1.5f);

                // 2. Crescent cutter polygon (saturated body + white excitation cutting edge)
                Vector2[] bladePolyHalo = new Vector2[] { bladeTip, bladeSpine + Vector2.FromAngle(angle) * 3.0f, bladeBack, bladeInner };
                DrawColoredPolygon(bladePolyHalo, accent with { A = 0.4f });

                Vector2[] bladePoly = new Vector2[] { bladeTip, bladeSpine, bladeBack, bladeInner };
                DrawColoredPolygon(bladePoly, accent with { A = 0.95f });

                // Razor-sharp excitation cutting edge
                DrawLine(bladeBack, bladeTip, core with { A = 0.98f }, 2.8f);
                DrawLine(bladeSpine, bladeTip, core with { A = 0.95f }, 2.0f);

                // 3. Laser glow on cutting tip & enzyme active site hub
                LaserGlow.DrawImpactHalo(this, bladeTip, 10.0f, accent, core, 0.95f, 2.5f);
                LaserGlow.DrawImpactHalo(this, bladePos, 7.0f, accent, core, 0.85f, 2.0f);

                // 4. Orbiting nucleotide cleave sparks
                for (int s = 1; s <= 4; s++)
                {
                    float shardAngle = angle - s * 0.16f;
                    Vector2 shardPos = Vector2.FromAngle(shardAngle) * (OrbitRadius + (s % 2 == 0 ? 6.0f : -6.0f));
                    DrawCircle(shardPos, 3.0f / s + 0.8f, accent with { A = 0.7f / s });
                    DrawCircle(shardPos, 2.0f / s + 0.5f, core with { A = 0.95f / s });
                }
            }
        }
    }

    public NucleaseBladesSkill()
    {
        SkillId = SkillIds.NucleaseBlades;
        NameKey = "SKILL_NUCLEASE_NAME";
        DescKey = "SKILL_NUCLEASE_DESC";
        BioKey = "SKILL_NUCLEASE_BIO";
        IconSymbol = "⛓️";
        IsInnate = false;
        IsPassive = false;
        Cooldown = 4.5f;
        CooldownTimer = 1.0f;
        Level = 1;
        MaxLevel = 5;
    }

    public override void Setup(CharacterBody2D pHost)
    {
        base.Setup(pHost);
        if (_canvas == null)
        {
            _canvas = new BladesCanvas { Skill = this };
            AddChild(_canvas);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!HasValidHost())
            return;

        GlobalPosition = Host.GlobalPosition;

        float rotSpeed = GetCalculatedSpeed(BaseRotationSpeed);
        _currentOrbitAngle += (float)delta * rotSpeed;

        int bladeCount = GetCalculatedAmount(2);
        float orbitR = GetCalculatedArea(BaseOrbitRadius);

        if (_canvas != null)
        {
            _canvas.OrbitAngle = _currentOrbitAngle;
            _canvas.OrbitRadius = orbitR;
            _canvas.BladeCount = bladeCount;
            _canvas.QueueRedraw();
        }

        CheckBladeCollisions(bladeCount, orbitR, (float)delta);
    }

    private float _damageCooldown = 0.0f;
    private void CheckBladeCollisions(int bladeCount, float orbitR, float dt)
    {
        if (Host == null)
            return;

        _damageCooldown -= dt;
        if (_damageCooldown > 0.0f)
            return;

        _damageCooldown = 0.22f; // tick damage every ~0.2s

        float baseDmg = GetBaseDamageForLevel(BaseDamage);
        GetDamage(baseDmg, out float dmg, out _);

        int hitCount = 0;
        for (int i = 0; i < bladeCount; i++)
        {
            float angle = _currentOrbitAngle + i * (Mathf.Tau / bladeCount);
            Vector2 bladePos = Host.GlobalPosition + Vector2.FromAngle(angle) * orbitR;

            TargetingService.ForEachInRadius(bladePos, 28.0f, n =>
            {
                hitCount++;
                CombatHelper.DealDamage(n, dmg, Host, false);
            });
        }

        if (hitCount > 0)
        {
            AudioManager.Instance?.PlaySfx("ethereal_knives");
        }
    }
}
