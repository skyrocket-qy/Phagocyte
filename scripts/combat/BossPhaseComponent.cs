using Godot;
using System;
using System.Collections.Generic;

namespace Phagocyte.Combat;

/// <summary>
/// Configuration data for an encounter phase.
/// </summary>
public class BossPhaseDef
{
    public float ThresholdPct { get; set; } = 1.0f; // HP percentage triggering this phase (e.g. 1.0, 0.6, 0.25)
    public float SpeedMult { get; set; } = 1.0f;
    public float DamageReduction { get; set; } = 0.0f; // 0.0 ~ 0.8
    public float AttackTimer { get; set; } = 3.5f;
    public List<string> Attacks { get; set; } = new();
}

/// <summary>
/// Autonomous component managing multi-phase boss transitions, damage reduction shields,
/// enrage timers, and automated ground danger telegraph rotations.
/// </summary>
public partial class BossPhaseComponent : Node
{
    [Signal]
    public delegate void BossHealthChangedEventHandler(float currentHp, float maxHp);

    [Signal]
    public delegate void BossPhaseChangedEventHandler(int newPhaseIndex);

    [Signal]
    public delegate void BossHardEnragedEventHandler();

    public string BossId { get; set; } = "pathogen_boss";
    public int CurrentPhaseIndex { get; private set; } = 1;
    public bool IsEnraged { get; private set; } = false;
    public bool IsHardEnraged { get; private set; } = false;

    public float HardEnrageSeconds { get; set; } = 120.0f;
    public float HardEnrageDamageMult { get; set; } = 1.6f;
    public float HardEnrageSpeedMult { get; set; } = 1.4f;

    // Ground danger telegraph tuning (terminal bosses scale these up)
    public float TelegraphDamage { get; set; } = 20.0f;
    public float TelegraphEnrageBonus { get; set; } = 15.0f;
    public float TelegraphScale { get; set; } = 1.0f;

    public List<BossPhaseDef> Phases { get; } = new();

    private float _fightTimer = 0.0f;
    private float _telegraphTimer = 3.5f;
    private int _attackIndex = 0;
    private Node2D? _parentEntity;

    public float CurrentDamageReduction
    {
        get
        {
            var p = GetCurrentPhaseDef();
            return p != null ? Math.Clamp(p.DamageReduction, 0.0f, 0.85f) : 0.0f;
        }
    }

    public float CurrentSpeedMult
    {
        get
        {
            var p = GetCurrentPhaseDef();
            float baseMult = p != null ? p.SpeedMult : 1.0f;
            return IsHardEnraged ? baseMult * HardEnrageSpeedMult : baseMult;
        }
    }

    public override void _Ready()
    {
        _parentEntity = GetParent() as Node2D;

        if (Phases.Count == 0)
        {
            SetupDefaultPhases();
        }

        _telegraphTimer = GetCurrentPhaseDef()?.AttackTimer ?? 3.5f;
    }

    public void SetupDefaultPhases()
    {
        Phases.Clear();
        Phases.Add(new BossPhaseDef
        {
            ThresholdPct = 1.0f,
            SpeedMult = 1.0f,
            DamageReduction = 0.0f,
            AttackTimer = 4.0f,
            Attacks = new List<string> { "circle" }
        });
        Phases.Add(new BossPhaseDef
        {
            ThresholdPct = 0.60f,
            SpeedMult = 1.25f,
            DamageReduction = 0.20f,
            AttackTimer = 3.0f,
            Attacks = new List<string> { "line", "circle" }
        });
        Phases.Add(new BossPhaseDef
        {
            ThresholdPct = 0.25f,
            SpeedMult = 1.50f,
            DamageReduction = 0.35f,
            AttackTimer = 2.0f,
            Attacks = new List<string> { "multi_circle", "line" }
        });
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_parentEntity == null || !GodotObject.IsInstanceValid(_parentEntity) || _parentEntity.IsQueuedForDeletion())
            return;

        float dt = (float)delta;
        _fightTimer += dt;
        _telegraphTimer -= dt;

        // Hard enrage check
        if (!IsHardEnraged && _fightTimer >= HardEnrageSeconds)
        {
            IsHardEnraged = true;
            EmitSignal(SignalName.BossHardEnraged);
        }

        if (_telegraphTimer <= 0.0f)
        {
            var phase = GetCurrentPhaseDef();
            _telegraphTimer = phase != null ? phase.AttackTimer : 3.0f;
            TriggerTelegraphedAttack();
        }
    }

    public float ApplyDamageReduction(float rawDamage)
    {
        float dr = CurrentDamageReduction;
        return Mathf.Max(1.0f, rawDamage * (1.0f - dr));
    }

    public void NotifyHealthChanged(float currentHp, float maxHp)
    {
        EmitSignal(SignalName.BossHealthChanged, currentHp, maxHp);
        if (maxHp <= 0.0f) return;

        float hpPct = currentHp / maxHp;
        CheckPhaseTransitions(hpPct);
    }

    private void CheckPhaseTransitions(float hpPct)
    {
        if (Phases.Count == 0) return;

        for (int i = Phases.Count - 1; i >= 0; i--)
        {
            int phaseNumber = i + 1;
            if (hpPct <= Phases[i].ThresholdPct && CurrentPhaseIndex < phaseNumber)
            {
                CurrentPhaseIndex = phaseNumber;
                IsEnraged = CurrentPhaseIndex > 1;
                EmitSignal(SignalName.BossPhaseChanged, CurrentPhaseIndex);
                break;
            }
        }
    }

    public BossPhaseDef? GetCurrentPhaseDef()
    {
        if (Phases.Count == 0 || CurrentPhaseIndex - 1 >= Phases.Count)
            return null;
        return Phases[CurrentPhaseIndex - 1];
    }

    private void TriggerTelegraphedAttack()
    {
        if (_parentEntity == null || !IsInsideTree()) return;

        var currentScene = GetTree()?.CurrentScene;
        if (currentScene == null) return;

        // Locate player
        var player = currentScene.GetNodeOrNull<Node2D>("Player") ?? currentScene.FindChild("Player", true, false) as Node2D;
        if (player == null || !GodotObject.IsInstanceValid(player)) return;

        var phase = GetCurrentPhaseDef();
        string attackType = "circle";
        if (phase != null && phase.Attacks.Count > 0)
        {
            attackType = phase.Attacks[_attackIndex % phase.Attacks.Count];
            _attackIndex++;
        }

        TelegraphAttackShape shape = attackType switch
        {
            "line" => TelegraphAttackShape.Line,
            "multi_circle" => TelegraphAttackShape.MultiCircle,
            _ => TelegraphAttackShape.Circle
        };

        Vector2 dir = (player.GlobalPosition - _parentEntity.GlobalPosition).Normalized();
        if (dir == Vector2.Zero) dir = Vector2.Right;

        float scale = Mathf.Max(0.1f, TelegraphScale);
        float damage = (TelegraphDamage + (IsHardEnraged ? TelegraphEnrageBonus : 0.0f)) * scale;

        var telegraph = new TelegraphedAttack
        {
            Shape = shape,
            LineLength = 320.0f * scale,
            LineWidth = 48.0f * scale,
            Radius = (shape == TelegraphAttackShape.MultiCircle ? 42.0f : 64.0f) * scale,
            TargetDirection = dir,
            TelegraphDuration = IsEnraged ? 1.0f : 1.4f,
            Damage = damage,
            GlobalPosition = shape == TelegraphAttackShape.Line ? _parentEntity.GlobalPosition : player.GlobalPosition
        };

        currentScene.AddChild(telegraph);
    }
}
