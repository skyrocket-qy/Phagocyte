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

            Color bladeCore = new Color(0.3f, 0.85f, 1.0f, 0.95f);
            Color bladeGlow = new Color(0.1f, 0.5f, 0.95f, 0.4f);

            for (int i = 0; i < BladeCount; i++)
            {
                float angle = OrbitAngle + i * (Mathf.Tau / BladeCount);
                Vector2 bladePos = Vector2.FromAngle(angle) * OrbitRadius;

                // Glowing outer disc
                DrawCircle(bladePos, 14.0f, bladeGlow);
                // Dense cutting enzyme core
                DrawCircle(bladePos, 8.0f, bladeCore);

                // Spiral slicing teeth
                float spinAngle = angle * 3.0f;
                for (int t = 0; t < 3; t++)
                {
                    float toothAngle = spinAngle + t * (Mathf.Tau / 3.0f);
                    Vector2 tip = bladePos + Vector2.FromAngle(toothAngle) * 16.0f;
                    DrawLine(bladePos, tip, bladeCore, 2.0f);
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

    public override void Setup(CharacterBody2D pHost, int pSlot)
    {
        base.Setup(pHost, pSlot);
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

        GetDamage(BaseDamage, out float dmg, out _);

        for (int i = 0; i < bladeCount; i++)
        {
            float angle = _currentOrbitAngle + i * (Mathf.Tau / bladeCount);
            Vector2 bladePos = Host.GlobalPosition + Vector2.FromAngle(angle) * orbitR;

            TargetingService.ForEachInRadius(bladePos, 28.0f, n =>
            {
                if (n.HasMethod("take_damage"))
                {
                    CombatHelper.DealDamage(n, dmg);
                }
                else if (n.HasMethod("be_engulfed"))
                {
                    n.Call("be_engulfed", Host);
                }
            });
        }
    }

    public override void Trigger()
    {
        base.Trigger();
        // Cooldown reset handled by BaseSkill
    }
}
