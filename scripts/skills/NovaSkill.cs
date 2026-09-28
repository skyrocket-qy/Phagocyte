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
    private bool _delayCrit;

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
            GetDamage(baseDmg, out _delayDmg, out _delayCrit);
            SpawnMarker(_markedTarget);
            return;
        }

        float baseDmgNow = GetBaseDamageForLevel(ParamFloat(p, "base_damage", 32.0f));
        GetDamage(baseDmgNow, out float dmg, out bool crit);
        Detonate(Host!.GlobalPosition, dmg, crit);
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
        Detonate(center, _delayDmg, _delayCrit);
    }

    /// <summary>Direct detonation with a fresh damage roll (previews, tests).</summary>
    public void DetonateAt(Vector2 center)
    {
        GetDamage(GetBaseDamageForLevel(0.0f), out float dmg, out bool crit);
        Detonate(center, dmg, crit);
    }

    /// <summary>Immediate detonation used by triggers and zone expiries.</summary>
    public void Detonate(Vector2 center, float dmg, bool crit)
    {
        var p = SkillParams();
        string shape = ParamString(p, "shape", "sphere");
        if (shape == "cone")
            DetonateCone(center, dmg, crit, p);
        else
            DetonateSphere(center, dmg, crit, p);
        SpawnNovaFx(center, p);
    }

    private void DetonateSphere(Vector2 center, float dmg, bool crit, Dictionary p)
    {
        float radius = GetCalculatedArea(ParamFloat(p, "radius", ParamFloat(p, "reach", 480.0f)));
        float kbDist = ParamFloat(p, "knockback_dist", 0.0f);
        float kbTime = ParamFloat(p, "knockback_time", 0.2f);
        float slowDur = ParamFloat(p, "slow_duration", 0.0f);
        float slowFactor = ParamFloat(p, "slow_factor", 0.5f);
        TargetingService.ForEachInRadius(center, radius, enemy =>
        {
            DamageService.DealDamage(enemy, dmg, Host, crit);
            if (kbDist > 0.0f)
            {
                Vector2 push = (enemy.GlobalPosition - center).Normalized();
                if (push == Vector2.Zero)
                    push = Vector2.Up;
                var tween = enemy.CreateTween();
                tween.TweenProperty(enemy, "global_position", enemy.GlobalPosition + push * kbDist, kbTime);
            }
            if (slowDur > 0.0f)
                enemy.ApplySlow(slowDur, slowFactor);
        });
    }

    private void DetonateCone(Vector2 center, float dmg, bool crit, Dictionary p)
    {
        Vector2 aimDir = Host!.Velocity.Length() > 20.0f ? Host.Velocity.Normalized() : Vector2.Right;
        float reach = GetCalculatedArea(ParamFloat(p, "reach", 320.0f));
        float halfCone = Mathf.DegToRad(ParamFloat(p, "angle", 100.0f) * 0.5f);
        float kbDist = ParamFloat(p, "knockback_dist", 80.0f);
        float kbTime = ParamFloat(p, "knockback_time", 0.2f);
        TargetingService.ForEachInRadius(center, reach, enemy =>
        {
            Vector2 toEnemy = enemy.GlobalPosition - center;
            if (toEnemy.Length() <= 1.0f || Mathf.Abs(aimDir.AngleTo(toEnemy)) > halfCone)
                return;
            Vector2 push = toEnemy.Normalized();
            var tween = Host!.CreateTween();
            tween.TweenProperty(enemy, "global_position", enemy.GlobalPosition + push * kbDist, kbTime);
            DamageService.DealDamage(enemy, dmg, Host, crit);
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
            float pulse = 1.0f + 0.1f * Mathf.Sin(_age * 10.0f);
            DrawArc(Vector2.Zero, 22.0f * pulse, 0.0f, Mathf.Tau, 32, new Color(accent, 0.8f), 2.0f);
            DrawCircle(Vector2.Zero, 6.0f, new Color(accent, 0.6f));
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
            if (ConeHalfAngle < 0.0f)
            {
                DrawCircle(Vector2.Zero, r, new Color(accent, 0.10f * alpha));
                DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 48, new Color(accent, 0.8f * alpha), 5.0f);
                DrawArc(Vector2.Zero, r, 0.0f, Mathf.Tau, 48, new Color(core, 0.9f * alpha), 2.0f);
            }
            else
            {
                float c = Aim.Angle();
                DrawArc(Vector2.Zero, r, c - ConeHalfAngle, c + ConeHalfAngle, 36, new Color(accent, 0.8f * alpha), 5.0f);
                DrawArc(Vector2.Zero, r, c - ConeHalfAngle, c + ConeHalfAngle, 36, new Color(core, 0.9f * alpha), 2.0f);
            }
        }
    }
}
