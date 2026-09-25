namespace Phagocyte.Combat;

/// <summary>
/// Stack-allocated damage outcome: final damage + crit flag.
/// Replaces per-hit <c>Godot.Collections.Dictionary</c> allocation in
/// <c>BaseSkill.GetCalculatedDamage</c> (zero GC on the combat hot path).
/// </summary>
public readonly record struct DamageResult(float Damage, bool IsCrit);
