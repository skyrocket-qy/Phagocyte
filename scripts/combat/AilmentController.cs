using Godot;
using System;
using System.Collections.Generic;
using Phagocyte.UI;

namespace Phagocyte.Combat;

/// <summary>
/// Autonomous component managing biological status effects (Ailments) on combatants.
/// Supports ROS Oxidative Burn (DoT), Agglutination (Slow), Opsonization (Damage Amp),
/// Membrane Leakage (Movement-scaled Bleed), and Endotoxin (Stacking Poison).
/// </summary>
public partial class AilmentController : Node
{
    // 1. ROS Oxidative Burn (Ignite DoT)
    private float _burnDps = 0.0f;
    private float _burnTimer = 0.0f;

    // 2. Agglutination / Cross-linking (Slow)
    private float _agglutinationTimer = 0.0f;
    private float _agglutinationSlowPct = 0.40f;

    // 3. Opsonization / Marked for Lysis (Damage Amplification)
    private float _opsonizationTimer = 0.0f;
    private float _opsonizationAmpPct = 0.30f; // +30% damage taken

    // 4. Membrane Leakage (Bleed: 3x DoT while moving)
    private float _leakDps = 0.0f;
    private float _leakTimer = 0.0f;
    private const float LeakMovingMultiplier = 3.0f;

    // 5. Endotoxin / Sepsis (Poison: independent stacking DoT)
    private readonly List<(float Dps, float Timer)> _endotoxinStacks = new();

    // Visual tick accumulator (avoids spamming 60 floating numbers per sec)
    private float _visualTickTimer = 0.0f;
    private float _accumulatedVisualDot = 0.0f;

    public bool IsOxidized => _burnTimer > 0.0f;
    public bool IsAgglutinated => _agglutinationTimer > 0.0f;
    public bool IsOpsonized => _opsonizationTimer > 0.0f;
    public bool IsLeaking => _leakTimer > 0.0f;
    public bool IsToxic => _endotoxinStacks.Count > 0;

    public float OpsonizationMultiplier => IsOpsonized ? (1.0f + _opsonizationAmpPct) : 1.0f;
    public float SpeedMultiplier => IsAgglutinated ? Mathf.Clamp(1.0f - _agglutinationSlowPct, 0.1f, 1.0f) : 1.0f;

    public float BurnTimer => _burnTimer;
    public float AgglutinationTimer => _agglutinationTimer;
    public float OpsonizationTimer => _opsonizationTimer;
    public float LeakTimer => _leakTimer;
    public int EndotoxinStackCount => _endotoxinStacks.Count;

    /// <summary>
    /// Applies or refreshes ROS Oxidative Burn DoT.
    /// </summary>
    public void ApplyOxidativeBurn(float dps, float duration = 3.0f)
    {
        _burnDps = Math.Max(_burnDps, dps);
        _burnTimer = Math.Max(_burnTimer, duration);
    }

    /// <summary>
    /// Applies or refreshes Agglutination slow. Stronger slow overrides weaker.
    /// </summary>
    public void ApplyAgglutination(float duration = 2.5f, float slowPct = 0.40f)
    {
        slowPct = Math.Clamp(slowPct, 0.05f, 0.75f);
        if (slowPct >= _agglutinationSlowPct)
        {
            _agglutinationSlowPct = slowPct;
            _agglutinationTimer = Math.Max(_agglutinationTimer, duration);
        }
        else
        {
            _agglutinationTimer = Math.Max(_agglutinationTimer, duration);
        }
    }

    /// <summary>
    /// Marks target with Opsonin (C3b / antibody Fc), amplifying all damage taken.
    /// </summary>
    public void ApplyOpsonization(float duration = 4.0f, float ampPct = 0.30f)
    {
        _opsonizationAmpPct = Math.Clamp(ampPct, 0.10f, 0.60f);
        _opsonizationTimer = Math.Max(_opsonizationTimer, duration);
    }

    /// <summary>
    /// Punctures target membrane, causing leakage that intensifies 3x when moving.
    /// </summary>
    public void ApplyMembraneLeak(float dps, float duration = 3.0f)
    {
        _leakDps = Math.Max(_leakDps, dps);
        _leakTimer = Math.Max(_leakTimer, duration);
    }

    /// <summary>
    /// Adds an independent Endotoxin poison stack.
    /// </summary>
    public void ApplyEndotoxin(float dps, float duration = 4.0f)
    {
        _endotoxinStacks.Add((dps, duration));
    }

    public void ClearAll()
    {
        _burnDps = 0f;
        _burnTimer = 0f;
        _agglutinationTimer = 0f;
        _opsonizationTimer = 0f;
        _leakDps = 0f;
        _leakTimer = 0f;
        _endotoxinStacks.Clear();
        _accumulatedVisualDot = 0f;
    }

    /// <summary>
    /// Antigenic drift (docs/endgame.md §4): strips the specific-vulnerability
    /// mark (opsonization) while leaving all other ailments untouched.
    /// </summary>
    public void ClearOpsonization()
    {
        _opsonizationTimer = 0.0f;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        float frameDot = 0.0f;

        // 1. Burn timer
        if (_burnTimer > 0.0f)
        {
            _burnTimer -= dt;
            frameDot += _burnDps * dt;
            if (_burnTimer <= 0.0f) _burnDps = 0.0f;
        }

        // 2. Agglutination timer
        if (_agglutinationTimer > 0.0f)
        {
            _agglutinationTimer -= dt;
            if (_agglutinationTimer <= 0.0f) _agglutinationSlowPct = 0.40f;
        }

        // 3. Opsonization timer
        if (_opsonizationTimer > 0.0f)
        {
            _opsonizationTimer -= dt;
        }

        // 4. Leak timer
        if (_leakTimer > 0.0f)
        {
            _leakTimer -= dt;
            float leak = _leakDps * dt;

            // Check if parent entity is moving
            if (GetParent() is Node2D parent2D)
            {
                bool isMoving = false;
                if (parent2D is CharacterBody2D cb)
                    isMoving = cb.Velocity.LengthSquared() > 0.1f;
                else
                {
                    // Check BaseEnemy's Velocity property
                    var velProp = parent2D.Get("Velocity");
                    if (velProp.VariantType == Variant.Type.Vector2)
                        isMoving = velProp.AsVector2().LengthSquared() > 0.1f;
                }

                if (isMoving)
                {
                    leak *= LeakMovingMultiplier;
                }
            }

            frameDot += leak;
            if (_leakTimer <= 0.0f) _leakDps = 0.0f;
        }

        // 5. Endotoxin stacks
        if (_endotoxinStacks.Count > 0)
        {
            float toxinDps = 0.0f;
            for (int i = _endotoxinStacks.Count - 1; i >= 0; i--)
            {
                var (dps, timer) = _endotoxinStacks[i];
                timer -= dt;
                if (timer <= 0.0f)
                {
                    _endotoxinStacks.RemoveAt(i);
                }
                else
                {
                    _endotoxinStacks[i] = (dps, timer);
                    toxinDps += dps;
                }
            }
            frameDot += toxinDps * dt;
        }

        // Apply Opsonization amplification to DoT as well
        if (frameDot > 0.0f)
        {
            if (IsOpsonized)
            {
                frameDot *= OpsonizationMultiplier;
            }

            ApplyDoTToParent(frameDot);

            // Accumulate for periodic floating text
            _accumulatedVisualDot += frameDot;
            _visualTickTimer += dt;
            if (_visualTickTimer >= 0.35f)
            {
                _visualTickTimer = 0.0f;
                if (_accumulatedVisualDot >= 1.0f && GetParent() is Node2D p)
                {
                    DamageNumberSpawner.ShowDamage(p.GlobalPosition, _accumulatedVisualDot, false);
                    _accumulatedVisualDot = 0.0f;
                }
            }
        }
    }

    private void ApplyDoTToParent(float damage)
    {
        var parent = GetParent();
        if (parent == null || !GodotObject.IsInstanceValid(parent) || parent.IsQueuedForDeletion())
            return;

        if (parent is IDamageable damageable)
        {
            damageable.TakeDoTDamage(damage);
        }
        else if (parent.HasMethod("TakeDoTDamage"))
        {
            parent.Call("TakeDoTDamage", damage);
        }
        else if (parent.HasMethod("take_dot_damage"))
        {
            parent.Call("take_dot_damage", damage);
        }
    }
}
