class_name Stat
extends RefCounted

## Represents a single numerical character statistic with base, flat, and percent modifiers.
## Formula: FinalValue = (base_value + flat_bonus) * (1.0 + percent_bonus)

var base_value: float = 0.0
var flat_bonus: float = 0.0
var percent_bonus: float = 0.0

func _init(p_base: float = 0.0) -> void:
	base_value = p_base
	flat_bonus = 0.0
	percent_bonus = 0.0

func get_value() -> float:
	return (base_value + flat_bonus) * (1.0 + percent_bonus)

func add_modifier(flat: float, pct: float) -> void:
	flat_bonus += flat
	percent_bonus += pct

func remove_modifier(flat: float, pct: float) -> void:
	flat_bonus -= flat
	percent_bonus -= pct

func set_base(val: float) -> void:
	base_value = val

func reset_modifiers() -> void:
	flat_bonus = 0.0
	percent_bonus = 0.0
