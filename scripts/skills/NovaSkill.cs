using Godot;
using Godot.Collections;
using Game.Combat;
using Game.Core;
using Game.Enemies;

namespace Game.Skills;

/// <summary>
/// Instant-area nova archetype (data: assets/data/skill/active.json).
/// Shapes: sphere (omni blast + knockback + slow), cone (directional blast
/// + knockback), delayed sphere with an optional target-following marker.
/// </summary>
public partial class NovaSkill : BaseSkill
{
    private EnemyActor? _markedTarget;
    private NovaMarker? _marker;
    private float _delayTimer;
    private bool _delayArmed;
    private float _delayDmg;
    private float _delayPen;

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;
        var p = SkillParams();
        string sfx = ParamString(p, "sfx");
        if (sfx != "")
            AudioManager.Instance?.PlaySfx(sfx);

        float delay = ParamFloat(p, "delay", 0.0f);
        if (delay > 0.0f && ParamBool(p, "follow_target", false))
        {
            _markedTarget = TargetingService.FindNearest(Host!, ParamFloat(p, "range", 550.0f));
            if (_markedTarget == null)
                return;
            _delayTimer = delay;
            _delayArmed = true;
            float baseDmg = GetBaseDamageForLevel(ParamFloat(p, "base_damage", 130.0f));
            _delayDmg = GetCalculatedDamage(baseDmg);
            _delayPen = GetCalculatedArmorPenetration();
            SpawnMarker(_markedTarget);
            return;
        }

        float baseDmgNow = GetBaseDamageForLevel(ParamFloat(p, "base_damage", 32.0f));
        Detonate(Host!.GlobalPosition, GetCalculatedDamage(baseDmgNow), GetCalculatedArmorPenetration());
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!_delayArmed || !HasValidHost())
            return;
        if (_markedTarget != null && (!GodotObject.IsInstanceValid(_markedTarget) || _markedTarget.CurrentHealth <= 0.0f))
            _markedTarget = null;
        Vector2 center = _markedTarget != null && GodotObject.IsInstanceValid(_markedTarget)
            ? _markedTarget.GlobalPosition
            : Host!.GlobalPosition;
        _delayTimer -= (float)delta;
        if (_delayTimer > 0.0f)
            return;
        _delayArmed = false;
        if (_marker != null && GodotObject.IsInstanceValid(_marker))
            _marker.QueueFree();
        _marker = null;
        Detonate(center, _delayDmg, _delayPen);
    }

    /// <summary>Direct detonation with a fresh damage roll (previews, tests).</summary>
    public void DetonateAt(Vector2 center)
    {
        Detonate(center, GetCalculatedDamage(GetBaseDamageForLevel(0.0f)), GetCalculatedArmorPenetration());
    }

    /// <summary>Immediate detonation used by triggers and zone expiries.</summary>
    public void Detonate(Vector2 center, float dmg, float pen)
    {
        var p = SkillParams();
        string shape = ParamString(p, "shape", "sphere");
        if (shape == "cone")
            DetonateCone(center, dmg, pen, p);
        else
            DetonateSphere(center, dmg, pen, p);
        SpawnNovaFx(center, p);
    }

    private void DetonateSphere(Vector2 center, float dmg, float pen, Dictionary p)
    {
        float radius = GetCalculatedArea(ParamFloat(p, "radius", ParamFloat(p, "reach", 480.0f)));
        float kbDist = ParamFloat(p, "knockback_dist", 0.0f);
        float kbTime = ParamFloat(p, "knockback_time", 0.2f);
        float slowDur = ParamFloat(p, "slow_duration", 0.0f);
        float slowFactor = ParamFloat(p, "slow_factor", 0.5f);
        ulong attackerId = Host!.GetInstanceId();
        float novaCrit = GetCalculatedCritChance();
        float novaMult = GetCalculatedCritDamage();
        TargetingService.ForEachInRadius(center, radius, enemy =>
        {
            HitPipeline.ResolveHit(new HitPayload
            {
                RawDamage = dmg,
                CritChance = novaCrit,
                CritMultiplier = novaMult,
                ArmorPenetration = pen,
                AttackerId = attackerId
            }, enemy);
            if (kbDist > 0.0f)
            {
                Vector2 push = (enemy.GlobalPosition - center).Normalized();
                if (push == Vector2.Zero)
                    push = Vector2.Up;
                var tween = enemy.CreateTween();
                tween.TweenProperty(enemy, "global_position", enemy.GlobalPosition + push * kbDist, kbTime);
            }
            if (slowDur > 0.0f && enemy.Status != null)
                enemy.Status.ApplySlow(slowDur, 1.0f - slowFactor);
        });
    }

    private void DetonateCone(Vector2 center, float dmg, float pen, Dictionary p)
    {
        Vector2 aimDir = Host!.Velocity.Length() > 20.0f ? Host.Velocity.Normalized() : Vector2.Right;
        float reach = GetCalculatedArea(ParamFloat(p, "reach", 320.0f));
        float halfCone = Mathf.DegToRad(ParamFloat(p, "angle", 100.0f) * 0.5f);
        float kbDist = ParamFloat(p, "knockback_dist", 80.0f);
        float kbTime = ParamFloat(p, "knockback_time", 0.2f);
        ulong attackerId = Host!.GetInstanceId();
        float coneCrit = GetCalculatedCritChance();
        float coneMult = GetCalculatedCritDamage();
        TargetingService.ForEachInRadius(center, reach, enemy =>
        {
            Vector2 toEnemy = enemy.GlobalPosition - center;
            if (toEnemy.Length() <= 1.0f || Mathf.Abs(aimDir.AngleTo(toEnemy)) > halfCone)
                return;
            Vector2 push = toEnemy.Normalized();
            var tween = Host!.CreateTween();
            tween.TweenProperty(enemy, "global_position", enemy.GlobalPosition + push * kbDist, kbTime);
            HitPipeline.ResolveHit(new HitPayload
            {
                RawDamage = dmg,
                CritChance = coneCrit,
                CritMultiplier = coneMult,
                ArmorPenetration = pen,
                AttackerId = attackerId
            }, enemy);
        });
    }

    private void SpawnMarker(EnemyActor target)
    {
        _marker = new NovaMarker { SkillRef = this };
        target.AddChild(_marker);
    }

    private void SpawnNovaFx(Vector2 center, Dictionary p)
    {
        var parent = Host!.GetParent();
        if (parent == null)
            return;
        string shape = ParamString(p, "shape", "sphere");
        float radius = GetCalculatedArea(ParamFloat(p, "radius", ParamFloat(p, "reach", 480.0f)));
        parent.AddChild(new NovaVisual
        {
            SkillRef = this,
            GlobalPosition = center,
            Radius = radius,
            ConeHalfAngle = shape == "cone" ? Mathf.DegToRad(ParamFloat(p, "angle", 100.0f) * 0.5f) : -1.0f,
            Aim = Host.Velocity.Length() > 20.0f ? Host.Velocity.Normalized() : Vector2.Right
        });
    }

    private bool ParamBool(Dictionary p, string key, bool fallback)
    {
        return CatalogLoader.GetBool(p, key, fallback);
    }

    /// <summary>Target-attached delay marker for follow_target novas.</summary>
    public partial class NovaMarker : Node2D
    {
        public NovaSkill? SkillRef { get; set; }
        private float _age;

        public override void _Process(double delta)
        {
            _age += (float)delta;
            QueueRedraw();
        }

        public override void _Draw()
        {
            var skill = SkillRef;
            Color accent = skill != null ? SkillAssetPalette.Accent(skill.SkillId, new Color(1.0f, 0.6f, 0.2f)) : new Color(1.0f, 0.6f, 0.2f);
            SkillVisualPresenters.DrawNovaMarker(this, _age, accent);
        }
    }

    /// <summary>Transient expanding nova ring (sphere) or cone arc.</summary>
    public partial class NovaVisual : Node2D
    {
        public NovaSkill? SkillRef { get; set; }
        public float Radius { get; set; } = 100.0f;
        public float ConeHalfAngle { get; set; } = -1.0f;
        public Vector2 Aim { get; set; } = Vector2.Right;

        private float _age;
        private const float VisualDuration = 0.5f;

        public override void _Process(double delta)
        {
            _age += (float)delta;
            if (_age >= VisualDuration)
                QueueFree();
            else
                QueueRedraw();
        }

        public override void _Draw()
        {
            var skill = SkillRef;
            Color accent = skill != null ? SkillAssetPalette.Accent(skill.SkillId, new Color(0.6f, 0.9f, 1.0f)) : new Color(0.6f, 0.9f, 1.0f);
            Color core = skill != null ? SkillAssetPalette.Core(skill.SkillId, Colors.White) : Colors.White;
            float prog = _age / VisualDuration;
            float alpha = Mathf.Clamp(1.0f - prog, 0.0f, 1.0f);
            float r = Radius * prog;
            SkillVisualPresenters.DrawNova(this, r, alpha, ConeHalfAngle, Aim, accent, core);
        }
    }
}
