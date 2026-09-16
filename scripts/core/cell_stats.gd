class_name CellStats
extends Node

const Stat = preload("res://scripts/core/stat.gd")

## Centralized 16-Universal-Stat Manager for Cells in Phagocyte.
## Excludes any skill-specific stats to maintain complete modularity.

signal stat_changed(stat_name: String, new_val: float)

# Combat Stats (10)
var might: Stat = Stat.new(1.0)
var area: Stat = Stat.new(1.0)
var cooldown_reduction: Stat = Stat.new(0.0)
var projectile_speed: Stat = Stat.new(1.0)
var duration: Stat = Stat.new(1.0)
var amount: Stat = Stat.new(0.0)
var pierce: Stat = Stat.new(0.0)
var knockback: Stat = Stat.new(1.0)
var crit_chance: Stat = Stat.new(0.05)
var crit_damage: Stat = Stat.new(2.0)

# Survival & Defense Stats (5)
var max_health: Stat = Stat.new(100.0)
var health_regen: Stat = Stat.new(0.0)
var armor: Stat = Stat.new(0.0)
var move_speed: Stat = Stat.new(230.0)
var revival: Stat = Stat.new(0.0)

# Utility & Meta Stats (4)
var magnet: Stat = Stat.new(150.0)
var growth: Stat = Stat.new(1.0)
var luck: Stat = Stat.new(1.0)
var curse: Stat = Stat.new(1.0)

var _stats_map: Dictionary = {}

func _init() -> void:
	_register_stats()

func _ready() -> void:
	if _stats_map.is_empty():
		_register_stats()

func _register_stats() -> void:
	_stats_map = {
		"might": might,
		"area": area,
		"cooldown_reduction": cooldown_reduction,
		"projectile_speed": projectile_speed,
		"duration": duration,
		"amount": amount,
		"pierce": pierce,
		"knockback": knockback,
		"crit_chance": crit_chance,
		"crit_damage": crit_damage,

		"max_health": max_health,
		"health_regen": health_regen,
		"armor": armor,
		"move_speed": move_speed,
		"revival": revival,

		"magnet": magnet,
		"growth": growth,
		"luck": luck,
		"curse": curse
	}

func get_stat_obj(stat_name: String) -> Stat:
	return _stats_map.get(stat_name, null)

func get_stat(stat_name: String) -> float:
	var s: Stat = _stats_map.get(stat_name, null)
	if s == null:
		push_warning("CellStats: Stat '%s' not found." % stat_name)
		return 1.0

	var val: float = s.get_value()

	# Universal clamps
	if stat_name == "cooldown_reduction":
		return clampf(val, 0.0, 0.75) # Cap CDR at 75%
	elif stat_name == "crit_chance":
		return clampf(val, 0.0, 1.0) # Crit chance capped at 100%
	elif stat_name == "move_speed" or stat_name == "max_health" or stat_name == "magnet":
		return max(0.0, val)

	return val

func add_modifier(stat_name: String, flat: float, pct: float) -> void:
	var s: Stat = _stats_map.get(stat_name, null)
	if s != null:
		s.add_modifier(flat, pct)
		stat_changed.emit(stat_name, get_stat(stat_name))
	else:
		push_warning("CellStats: Cannot add modifier to unknown stat '%s'." % stat_name)

func remove_modifier(stat_name: String, flat: float, pct: float) -> void:
	var s: Stat = _stats_map.get(stat_name, null)
	if s != null:
		s.remove_modifier(flat, pct)
		stat_changed.emit(stat_name, get_stat(stat_name))
	else:
		push_warning("CellStats: Cannot remove modifier from unknown stat '%s'." % stat_name)

func set_base(stat_name: String, val: float) -> void:
	var s: Stat = _stats_map.get(stat_name, null)
	if s != null:
		s.set_base(val)
		stat_changed.emit(stat_name, get_stat(stat_name))

## Returns percentage damage reduction from armor: Armor / (Armor + 50)
func get_damage_reduction_ratio() -> float:
	var a = get_stat("armor")
	if a <= 0.0:
		return 0.0
	return a / (a + 50.0)

## Rolls for critical hit
func roll_critical() -> bool:
	return randf() < get_stat("crit_chance")
