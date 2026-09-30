using Godot;
using Godot.Collections;
using Game.Combat;
using Game.Core;
using Game.Enemies;

namespace Game.Skills;

/// <summary>
/// Timed-area zone archetype (data: assets/data/skill/active.json).
/// Deploys a stationary field at self / nearest target / forward offset
/// that ticks damage, applies ailments and optionally pulls. Mine rows
/// detonate a nova sphere on expiry instead of ticking.
/// </summary>
public partial class ZoneSkill : BaseSkill
{
    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;
        var p = SkillParams();
        string sfx = ParamString(p, "sfx");
        if (sfx != "")
            AudioManager.Instance?.PlaySfx(sfx);

        Vector2 center = DeployPoint(p);
        float baseDmg = GetBaseDamageForLevel(ParamFloat(p, "base_damage", 14.0f));
        GetDamage(baseDmg, out float dmg, out float critChance, out float critMult);

        int count = GetCalculatedAmount(ParamInt(p, "count", 1));
        bool mineRow = p.ContainsKey("nova_radius");
        for (int i = 0; i < count; i++)
        {
            Vector2 at = center;
            if (ParamString(p, "deploy", "target") == "scatter")
                at = center + Vector2.FromAngle((float)GD.RandRange(0.0, Mathf.Tau))
                    * (float)GD.RandRange(ParamFloat(p, "drop_min", 50.0f), ParamFloat(p, "drop_max", 220.0f));
            var parent = Host!.GetParent();
            if (parent == null)
                return;
            var fx0 = default(EffectSpec);
            var fx1 = default(EffectSpec);
            int fxCount = BuildOnHitEffects(p, dmg, out fx0, out fx1, out _);
            parent.AddChild(new Zone
            {
                SourceTeam = Team.Player,
                Source = Host,
                GlobalPosition = at,
                Radius = GetCalculatedArea(ParamFloat(p, "radius", 65.0f)),
                Duration = GetCalculatedDuration(ParamFloat(p, "duration", ParamFloat(p, "fuse", 4.0f))),
                TickInterval = ParamFloat(p, "tick", 0.35f),
                Damage = dmg,
                CritChance = critChance,
                CritMultiplier = critMult,
                Effect0 = fx0,
                Effect1 = fx1,
                EffectCount = fxCount,
                Pull = ParamFloat(p, "pull", 0.0f),
                PullEff = ParamFloat(p, "pull_eff", 0.4f),
                CoreColor = SkillAssetPalette.Core(SkillId, Colors.White),
                RimColor = SkillAssetPalette.Accent(SkillId, new Color(0.4f, 0.9f, 0.7f)),
                NovaOnExpiry = mineRow,
                NovaRadius = GetCalculatedArea(ParamFloat(p, "nova_radius", 80.0f)),
                Expired = mineRow ? center => ExpireNova(center, p, dmg, critChance, critMult) : null
            });
        }
    }

    private Vector2 DeployPoint(Dictionary p)
    {
        string deploy = ParamString(p, "deploy", "target");
        if (deploy == "self")
            return Host!.GlobalPosition;
        var target = TargetingService.FindNearest(Host!, ParamFloat(p, "deploy_range", 450.0f));
        if (target != null)
            return target.GlobalPosition;
        Vector2 fwd = Host!.Velocity.Length() > 20.0f ? Host.Velocity.Normalized() : Vector2.Right;
        return Host.GlobalPosition + fwd * ParamFloat(p, "deploy_fallback", 100.0f);
    }

    /// <summary>Deploys the zone nova on expiry (mine rows).</summary>
    public void ExpireNova(Vector2 center, Dictionary p, float dmg, float critChance, float critMult)
    {
        var nova = new NovaSkill
        {
            SkillId = SkillId,
            Level = Level,
            MaxLevel = MaxLevel,
            Cooldown = 0.0f
        };
        nova.Setup(Host!);
        string novaSfx = ParamString(p, "nova_sfx");
        if (novaSfx != "")
            AudioManager.Instance?.PlaySfx(novaSfx);
        float radius = GetCalculatedArea(ParamFloat(p, "nova_radius", 80.0f));
        TargetingService.ForEachInRadius(center, radius, enemy =>
        {
            DamageService.DealDamage(enemy, dmg, Host, critChance, critMult);
        });
        var parent = Host!.GetParent();
        if (parent != null)
            parent.AddChild(new NovaSkill.NovaVisual
            {
                SkillRef = nova,
                GlobalPosition = center,
                Radius = radius,
                ConeHalfAngle = -1.0f,
                Aim = Vector2.Right
            });
    }
}
