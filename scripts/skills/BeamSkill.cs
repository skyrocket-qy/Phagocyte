using Godot;
using Godot.Collections;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;

namespace Game.Skills;

/// <summary>
/// Beam archetype (data: assets/data/skill/active.json).
/// Modes: pierce (instant fanned piercing rays with on-hit marks),
/// channel (damage-over-time ray glued to host with retarget + metadata),
/// chain (arcing jumps across nearby targets, no falloff).
/// </summary>
public partial class BeamSkill : BaseSkill
{
    private EnemyActor? _channelTarget;
    private float _channelTimer;
    private BeamVisual? _beamFx;

    public override void Trigger()
    {
        base.Trigger();
        if (!HasValidHost())
            return;
        var p = SkillParams();
        string mode = ParamString(p, "mode", "pierce");
        string sfx = ParamString(p, "sfx");
        if (sfx != "")
            AudioManager.Instance?.PlaySfx(sfx);

        if (mode == "channel")
            StartChannel(p);
        else if (mode == "chain")
            FireChain(p);
        else
            FirePierce(p);
    }

    private void FirePierce(Dictionary p)
    {
        Vector2 baseDir = TargetingService.FindTargetDirection(Host!, ParamFloat(p, "range", 700.0f),
            Host!.Velocity.Length() > 20.0f ? Host.Velocity.Normalized() : Vector2.Right);
        int count = GetCalculatedAmount(ParamInt(p, "count", 1));
        float fan = ParamFloat(p, "fan", 0.15f);
        float width = 24.0f * GetCalculatedArea(ParamFloat(p, "width_mult", 1.0f));
        float range = ParamFloat(p, "range", 700.0f);
        int pierce = GetCalculatedPierce(ParamInt(p, "pierce", 3));
        float baseDmg = GetBaseDamageForLevel(ParamFloat(p, "base_damage", 35.0f));

        for (int i = 0; i < count; i++)
        {
            Vector2 dir = count > 1 ? baseDir.Rotated((i - (count - 1) * 0.5f) * fan) : baseDir;
            Vector2 start = Host!.GlobalPosition;
            Vector2 end = start + dir * range;

            var hits = new List<(EnemyActor enemy, float along)>();
            foreach (var enemy in EnemyActor.ActiveEnemies)
            {
                if (!TargetingService.IsValidTarget(enemy))
                    continue;
                Vector2 proj = Geometry2D.GetClosestPointToSegment(enemy.GlobalPosition, start, end);
                if (proj.DistanceTo(enemy.GlobalPosition) > width)
                    continue;
                if ((enemy.GlobalPosition - start).Length() > range + width)
                    continue;
                hits.Add((enemy, (proj - start).Length()));
            }
            hits.Sort((a, b) => a.along.CompareTo(b.along));

            GetDamage(baseDmg, out float dmg, out float critChance, out float critMult);
            int struck = 0;
            foreach (var (enemy, _) in hits)
            {
                if (struck >= pierce)
                    break;
                struck++;
                DamageService.DealDamage(enemy, dmg, Host, critChance, critMult);
                ApplyOnHitEffects(enemy, p, dmg);
                VfxManager.Instance?.Play(VfxType.PerforinPore, enemy.GlobalPosition);
            }
            SpawnBeamFx(start, end, width);
        }
    }

    private void StartChannel(Dictionary p)
    {
        var target = TargetingService.FindNearest(Host!, ParamFloat(p, "range", 520.0f));
        if (target == null)
            return;
        _channelTarget = target;
        _channelTimer = GetCalculatedDuration(ParamFloat(p, "duration", 2.0f));
        EnsureBeamFx();
    }

    private void FireChain(Dictionary p)
    {
        int count = GetCalculatedAmount(ParamInt(p, "count", 3));
        float baseDmg = GetBaseDamageForLevel(ParamFloat(p, "base_damage", 35.0f));
        GetDamage(baseDmg, out float dmg, out float critChance, out float critMult);

        var visited = new HashSet<EnemyActor>();
        EnemyActor? current = TargetingService.FindNearest(Host!, ParamFloat(p, "range", 480.0f));
        Vector2 from = Host!.GlobalPosition;
        int jumps = 0;
        while (current != null && jumps < count)
        {
            visited.Add(current);
            DamageService.DealDamage(current, dmg, Host, critChance, critMult);
            SpawnBeamFx(from, current.GlobalPosition, 9.0f);
            from = current.GlobalPosition;
            jumps++;
            current = TargetingService.FindNearest(current, ParamFloat(p, "chain_range", 240.0f),
                e => e != null && !visited.Contains(e));
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_channelTarget == null || !HasValidHost())
        {
            HideBeamFx();
            return;
        }
        var p = SkillParams();
        if (ParamString(p, "mode") != "channel")
        {
            _channelTarget = null;
            return;
        }
        if (!GodotObject.IsInstanceValid(_channelTarget) || _channelTarget.CurrentHealth <= 0.0f)
        {
            _channelTarget = TargetingService.FindNearest(Host!, ParamFloat(p, "range", 520.0f));
            if (_channelTarget == null)
            {
                HideBeamFx();
                return;
            }
        }

        _channelTimer -= (float)delta;
        if (_channelTimer <= 0.0f)
        {
            _channelTarget = null;
            HideBeamFx();
            return;
        }

        float dps = GetBaseDamageForLevel(ParamFloat(p, "base_damage", 32.0f));
        DamageService.DealDamage(_channelTarget, dps * (float)delta, Host);
        string meta = ParamString(p, "meta");
        if (meta != "" && !_channelTarget.HasMeta(meta))
            _channelTarget.SetMeta(meta, true);
        ShowBeamFx(Host!.GlobalPosition, _channelTarget.GlobalPosition);
    }

    public override void Setup(CharacterBody2D pHost)
    {
        base.Setup(pHost);
        if (ParamString(SkillParams(), "mode", "pierce") == "channel")
        {
            EnsureBeamFx();
            HideBeamFx();
        }
    }

    private void EnsureBeamFx()
    {
        if (_beamFx == null || !GodotObject.IsInstanceValid(_beamFx))
        {
            _beamFx = new BeamVisual { SkillRef = this };
            AddChild(_beamFx);
        }
        _beamFx.Visible = true;
    }

    private void ShowBeamFx(Vector2 from, Vector2 to)
    {
        EnsureBeamFx();
        _beamFx!.From = ToLocal(from);
        _beamFx.To = ToLocal(to);
        _beamFx.Visible = true;
        _beamFx.QueueRedraw();
    }

    private void HideBeamFx()
    {
        if (_beamFx != null && GodotObject.IsInstanceValid(_beamFx))
            _beamFx.Visible = false;
    }

    private void SpawnBeamFx(Vector2 from, Vector2 to, float width)
    {
        var parent = Host!.GetParent();
        if (parent == null)
            return;
        parent.AddChild(new BeamVisual
        {
            SkillRef = this,
            GlobalPosition = from,
            From = Vector2.Zero,
            To = to - from,
            Width = width,
            Lifetime = 0.28f
        });
    }

    /// <summary>Transient or sticky beam ribbon, palette-flavored per skill id.</summary>
    public partial class BeamVisual : Node2D
    {
        public BeamSkill? SkillRef { get; set; }
        public Vector2 From { get; set; }
        public Vector2 To { get; set; }
        public float Width { get; set; } = 10.0f;
        public float Lifetime { get; set; }

        private float _age;

        public override void _Process(double delta)
        {
            if (Lifetime <= 0.0f)
                return;
            _age += (float)delta;
            if (_age >= Lifetime)
                QueueFree();
            else
                QueueRedraw();
        }

        public override void _Draw()
        {
            var skill = SkillRef;
            Color accent = skill != null ? SkillAssetPalette.Accent(skill.SkillId, new Color(0.4f, 1.0f, 0.6f)) : new Color(0.4f, 1.0f, 0.6f);
            Color core = skill != null ? SkillAssetPalette.Core(skill.SkillId, Colors.White) : Colors.White;
            float alpha = Lifetime > 0.0f ? Mathf.Clamp(1.0f - _age / Lifetime, 0.0f, 1.0f) : 1.0f;
            SkillVisualPresenters.DrawBeam(this, From, To, Width, alpha, accent, core);
        }
    }
}
