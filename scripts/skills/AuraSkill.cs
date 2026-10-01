using Godot;
using Godot.Collections;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;

namespace Game.Skills;

/// <summary>
/// Persistent aura archetype (data: assets/data/skill/active.json).
/// Modes: radial (damage + ailment ticks around the host on trigger),
/// orbital (blades circling the host with periodic collision ticks).
/// </summary>
public partial class AuraSkill : BaseSkill
{
    private readonly List<float> _bladeAngles = new();
    private float _orbitTick;
    private AuraVisual? _visual;

    public override void Setup(CharacterBody2D pHost)
    {
        base.Setup(pHost);
        EnsureVisual();
    }

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;
        var p = SkillParams();
        if (ParamString(p, "mode", "radial") != "radial")
            return;

        float hostR = 48.0f;
        if (Host is Game.Player.PlayerActor actor)
            hostR = actor.CurrentRadius;
        float radius = GetCalculatedArea(ParamFloat(p, "radius", 95.0f))
            + ParamFloat(p, "radius_offset", 0.0f)
            + (ParamBool(p, "plus_host_radius", false) ? hostR : 0.0f);
        float baseDmg = GetBaseDamageForLevel(ParamFloat(p, "base_damage", 8.0f));
        float dmg = GetCalculatedDamage(baseDmg);
        ulong attackerId = Host!.GetInstanceId();
        int fxCount = BuildOnHitEffects(p, dmg, out var fx0, out var fx1, out var fx2);
        string sfx = ParamString(p, "sfx");
        bool hitAny = false;

        float critChance = GetCalculatedCritChance();
        float critMult = GetCalculatedCritDamage();
        float pen = GetCalculatedArmorPenetration();
        float ailChance = GetCalculatedAilmentChance();
        DamageType dmgType = GetDamageType();

        TargetingService.ForEachInRadius(Host!.GlobalPosition, radius, enemy =>
        {
            hitAny = true;
            HitPipeline.ResolveHit(new HitPayload
            {
                RawDamage = dmg,
                CritChance = critChance,
                CritMultiplier = critMult,
                ArmorPenetration = pen,
                AilmentChance = ailChance,
                Type = dmgType,
                AttackerId = attackerId,
                Effect0 = fx0,
                Effect1 = fx1,
                Effect2 = fx2,
                EffectCount = fxCount,
            }, enemy);
        });
        if (sfx != "" && hitAny)
            AudioManager.Instance?.PlaySfx(sfx);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!HasValidHost())
            return;
        var p = SkillParams();
        if (ParamString(p, "mode", "radial") != "orbital")
            return;

        int blades = GetCalculatedAmount(ParamInt(p, "blades", 2));
        while (_bladeAngles.Count < blades)
            _bladeAngles.Add(_bladeAngles.Count * Mathf.Tau / Mathf.Max(1, blades));
        while (_bladeAngles.Count > blades)
            _bladeAngles.RemoveAt(_bladeAngles.Count - 1);

        float orbit = GetCalculatedArea(ParamFloat(p, "orbit", 115.0f));
        float rot = GetCalculatedSpeed(ParamFloat(p, "rot_speed", 3.8f));
        for (int i = 0; i < _bladeAngles.Count; i++)
            _bladeAngles[i] += rot * (float)delta;

        _orbitTick -= (float)delta;
        if (_orbitTick > 0.0f)
            return;
        _orbitTick = ParamFloat(p, "tick", 0.22f);
        float baseDmg = GetBaseDamageForLevel(ParamFloat(p, "base_damage", 28.0f));
        float dmg = GetCalculatedDamage(baseDmg);
        ulong attackerId = Host!.GetInstanceId();
        float bladeR = ParamFloat(p, "blade_radius", 28.0f);
        bool hitAny = false;
        float orbCritChance = GetCalculatedCritChance();
        float orbCritMult = GetCalculatedCritDamage();
        float orbPen = GetCalculatedArmorPenetration();
        float orbAil = GetCalculatedAilmentChance();
        DamageType orbType = GetDamageType();
        foreach (float angle in _bladeAngles)
        {
            Vector2 bladePos = Host!.GlobalPosition + Vector2.FromAngle(angle) * orbit;
            TargetingService.ForEachInRadius(bladePos, bladeR, enemy =>
            {
                hitAny = true;
                HitPipeline.ResolveHit(new HitPayload
                {
                    RawDamage = dmg,
                    CritChance = orbCritChance,
                    CritMultiplier = orbCritMult,
                    ArmorPenetration = orbPen,
                    AilmentChance = orbAil,
                    Type = orbType,
                    AttackerId = attackerId
                }, enemy);
            });
        }
        string sfx = ParamString(p, "sfx");
        if (sfx != "" && hitAny)
            AudioManager.Instance?.PlaySfx(sfx);
    }

    /// <summary>Live blade angles for the visual (same list the logic ticks).</summary>
    public List<float> GetBladeAngles()
    {
        return _bladeAngles;
    }

    private void EnsureVisual()
    {
        if (_visual != null && GodotObject.IsInstanceValid(_visual))
            return;
        _visual = new AuraVisual { SkillRef = this };
        AddChild(_visual);
    }

    private bool ParamBool(Dictionary p, string key, bool fallback)
    {
        return CatalogLoader.GetBool(p, key, fallback);
    }

    /// <summary>Persistent aura visual: radial halo or orbiting blades.</summary>
    public partial class AuraVisual : Node2D
    {
        public AuraSkill? SkillRef { get; set; }
        private float _phase;

        public override void _Process(double delta)
        {
            var skill = SkillRef;
            if (skill == null || !skill.HasValidHost())
            {
                Visible = false;
                return;
            }
            Visible = true;
            GlobalPosition = skill.Host!.GlobalPosition;
            _phase += (float)delta * 4.0f;
            QueueRedraw();
        }

        public override void _Draw()
        {
            var skill = SkillRef;
            if (skill == null)
                return;
            var p = skill.SkillParams();
            Color accent = SkillAssetPalette.Accent(skill.SkillId, new Color(0.4f, 0.9f, 1.0f));
            Color core = SkillAssetPalette.Core(skill.SkillId, Colors.White);
            if (skill.ParamString(p, "mode", "radial") == "orbital")
            {
                float orbit = skill.GetCalculatedArea(skill.ParamFloat(p, "orbit", 115.0f));
                SkillVisualPresenters.DrawAuraOrbital(this, orbit, skill.GetBladeAngles(), accent, core);
                return;
            }
            float hostR = 48.0f;
            if (skill.Host is Game.Player.PlayerActor actor)
                hostR = actor.CurrentRadius;
            float r = skill.GetCalculatedArea(skill.ParamFloat(p, "radius", 95.0f)) + hostR;
            SkillVisualPresenters.DrawAuraRadial(this, r, accent, core);
        }
    }
}
