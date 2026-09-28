using Godot;
using Godot.Collections;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;

namespace Game.Skills;

/// <summary>
/// Melee strike archetype (data: assets/data/skill/active.json).
/// Grabs the nearest N (or radial) targets in reach with extending chain
/// visuals; on arrival deals damage plus splash or a pull tween.
/// </summary>
public partial class StrikeSkill : BaseSkill
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

        int count = GetCalculatedAmount(ParamInt(p, "count", 1));
        float reach = GetCalculatedArea(ParamFloat(p, "reach", 280.0f));
        float baseDmg = GetBaseDamageForLevel(ParamFloat(p, "base_damage", 28.0f));
        GetDamage(baseDmg, out float dmg, out bool crit);

        var found = new List<EnemyActor>();
        TargetingService.CollectInRadius(Host!.GlobalPosition, reach, found);
        if (ParamBool(p, "sort_by_distance", true))
            found.Sort((a, b) => a.GlobalPosition.DistanceSquaredTo(Host.GlobalPosition)
                .CompareTo(b.GlobalPosition.DistanceSquaredTo(Host.GlobalPosition)));

        int struck = 0;
        foreach (var enemy in found)
        {
            if (struck >= count)
                break;
            struck++;
            var parent = Host.GetParent();
            if (parent == null)
                continue;
            parent.AddChild(new ChainVisual
            {
                SkillRef = this,
                GlobalPosition = Host.GlobalPosition,
                Target = enemy,
                Damage = dmg,
                IsCrit = crit
            });
        }
    }

    private bool ParamBool(Dictionary p, string key, bool fallback)
    {
        return CatalogLoader.GetBool(p, key, fallback);
    }

    /// <summary>Target arrival hook for chain visuals.</summary>
    public void OnChainArrived(EnemyActor target, Vector2 tipPos, float dmg, bool crit)
    {
        if (!HasValidHost() || !GodotObject.IsInstanceValid(target))
            return;
        var p = SkillParams();
        DamageService.DealDamage(target, dmg, Host, crit);
        VfxManager.Instance?.Play(VfxType.CytoplasmSplatter, tipPos);

        if (p.ContainsKey("splash_radius"))
        {
            float splashR = GetCalculatedArea(ParamFloat(p, "splash_radius", 40.0f));
            float splashDmg = dmg * ParamFloat(p, "splash_mult", 0.4f);
            TargetingService.ForEachInRadius(tipPos, splashR, enemy =>
            {
                if (enemy != target)
                    DamageService.DealDamage(enemy, splashDmg, Host, crit);
            });
        }

        if (p.ContainsKey("pull_tween"))
        {
            var tween = target.CreateTween();
            tween.TweenProperty(target, "global_position", Host!.GlobalPosition, ParamFloat(p, "pull_tween", 0.15f));
        }
    }

    /// <summary>Extending tapered chain ribbon (cup-pincer or blunt-fist tip).</summary>
    public partial class ChainVisual : Node2D
    {
        public StrikeSkill? SkillRef { get; set; }
        public EnemyActor? Target { get; set; }
        public float Damage { get; set; }
        public bool IsCrit { get; set; }

        /// <summary>Chain tip kind (cup / fist) for tests and previews.</summary>
        public string VisualKind { get; private set; } = "";

        private enum Phase { Extend, Hold, Retract }
        private Phase _phase = Phase.Extend;
        private float _holdTimer;
        private bool _arrivedFired;
        private Vector2 _tip;

        public override void _Ready()
        {
            var skill = SkillRef;
            if (skill != null)
                VisualKind = skill.ParamString(skill.SkillParams(), "chain_kind", "cup");
        }

        public override void _PhysicsProcess(double delta)
        {
            float dt = (float)delta;
            var skill = SkillRef;
            if (skill == null || !skill.HasValidHost())
            {
                QueueFree();
                return;
            }
            var p = skill.SkillParams();
            Vector2 origin = skill.Host!.GlobalPosition;
            Vector2 goal = Target != null && GodotObject.IsInstanceValid(Target)
                ? Target.GlobalPosition
                : origin + Vector2.Right * 100.0f;

            if (_tip == Vector2.Zero)
                _tip = origin;
            VisualKind = skill.ParamString(p, "chain_kind", "cup");

            if (_phase == Phase.Extend)
            {
                float extend = skill.ParamFloat(p, "chain_extend", 1250.0f);
                Vector2 want = (goal - _tip).Normalized() * extend * dt;
                _tip += want.LimitLength((goal - _tip).Length());
                if (_tip.DistanceTo(goal) <= 28.0f)
                {
                    _phase = Phase.Hold;
                    _holdTimer = skill.ParamFloat(p, "chain_hold", 0.3f);
                    if (!_arrivedFired)
                    {
                        _arrivedFired = true;
                        if (Target != null && GodotObject.IsInstanceValid(Target))
                            skill.OnChainArrived(Target, _tip, Damage, IsCrit);
                    }
                }
            }
            else if (_phase == Phase.Hold)
            {
                _holdTimer -= dt;
                if (Target != null && GodotObject.IsInstanceValid(Target))
                    _tip = Target.GlobalPosition;
                if (_holdTimer <= 0.0f)
                    _phase = Phase.Retract;
            }
            else
            {
                _tip = _tip.MoveToward(origin, 900.0f * dt);
                if (_tip.DistanceTo(origin) <= 10.0f)
                {
                    QueueFree();
                    return;
                }
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            var skill = SkillRef;
            if (skill == null || !skill.HasValidHost())
                return;
            var p = skill.SkillParams();
            Color accent = SkillAssetPalette.Accent(skill.SkillId, new Color(0.5f, 0.9f, 0.6f));
            Color core = SkillAssetPalette.Core(skill.SkillId, Colors.White);
            Vector2 origin = ToLocal(skill.Host!.GlobalPosition);
            Vector2 tip = ToLocal(_tip == Vector2.Zero ? skill.Host.GlobalPosition : _tip);
            float halfWidth = skill.ParamFloat(p, "chain_half_width", 16.0f) * skill.GetCalculatedArea(1.0f);
            Vector2 dir = (tip - origin).Normalized();
            if (dir == Vector2.Zero)
                return;
            Vector2 perp = dir.Orthogonal();
            DrawLine(origin, tip, new Color(accent, 0.5f), halfWidth * 0.7f);
            DrawLine(origin, tip, new Color(core, 0.8f), halfWidth * 0.25f);
            string kind = skill.ParamString(p, "chain_kind", "cup");
            if (kind == "fist")
            {
                for (int k = -1; k <= 1; k++)
                    DrawCircle(tip + perp * k * halfWidth * 0.5f + dir * 6.0f, halfWidth * 0.35f, new Color(accent, 0.8f));
            }
            else
            {
                DrawArc(tip, halfWidth * 0.8f, dir.Angle() - 1.1f, dir.Angle() + 1.1f, 16, new Color(accent, 0.9f), 3.0f);
            }
        }
    }
}
