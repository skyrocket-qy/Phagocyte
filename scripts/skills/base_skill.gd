class_name BaseSkill
extends Node2D

const CellStats = preload("res://scripts/core/cell_stats.gd")

## Base class for all Active Cytokine Weapons and Passive Organelle Traits in Phagocyte.
## Interacts directly with the universal CellStats system.

signal cooldown_updated(time_left: float, max_time: float)
signal skill_activated()
signal skill_upgraded(new_level: int)

@export var skill_id: String = ""
@export var name_key: String = ""
@export var desc_key: String = ""
@export var bio_key: String = ""
@export var icon_symbol: String = "⚡"
@export var level: int = 1
@export var max_level: int = 5
@export var cooldown: float = 0.0
@export var is_passive: bool = false
@export var is_innate: bool = false

var cooldown_timer: float = 0.0
var slot_index: int = -1
var host: CharacterBody2D = null
var stats: Node = null

func setup(p_host: CharacterBody2D, p_slot: int) -> void:
	host = p_host
	slot_index = p_slot

	if host != null:
		if host.has_node("CellStats"):
			stats = host.get_node("CellStats")
		elif "stats" in host and host.stats != null:
			stats = host.stats

	if is_passive:
		apply_passive_modifiers()

func update_skill(delta: float) -> void:
	if is_passive:
		return
	var current_cd = get_calculated_cooldown()
	if current_cd <= 0.0:
		return

	if cooldown_timer > 0.0:
		cooldown_timer -= delta
		cooldown_updated.emit(max(0.0, cooldown_timer), current_cd)
		if cooldown_timer <= 0.0:
			trigger()

func trigger() -> void:
	skill_activated.emit()
	cooldown_timer = get_calculated_cooldown()

func upgrade() -> void:
	if level < max_level:
		if is_passive:
			remove_passive_modifiers()
		level += 1
		if is_passive:
			apply_passive_modifiers()
		skill_upgraded.emit(level)

func _exit_tree() -> void:
	if is_passive:
		remove_passive_modifiers()

## --- Stat Consumption Helpers for Active Skills ---

func get_calculated_cooldown() -> float:
	if cooldown <= 0.0:
		return 0.0
	if stats == null or not stats.has_method("get_stat"):
		return cooldown
	var cdr = stats.get_stat("cooldown_reduction")
	return cooldown * (1.0 - cdr)

func get_calculated_damage(base_dmg: float) -> Dictionary:
	var result = {
		"damage": base_dmg,
		"is_crit": false
	}
	if stats == null or not stats.has_method("get_stat"):
		return result

	var might = stats.get_stat("might")
	var dmg = base_dmg * might
	if stats.has_method("roll_critical") and stats.roll_critical():
		result["damage"] = dmg * stats.get_stat("crit_damage")
		result["is_crit"] = true
	else:
		result["damage"] = dmg
	return result

func get_calculated_area(base_area: float) -> float:
	if stats == null or not stats.has_method("get_stat"):
		return base_area
	return base_area * stats.get_stat("area")

func get_calculated_amount(base_amount: int) -> int:
	if stats == null or not stats.has_method("get_stat"):
		return base_amount
	return base_amount + int(stats.get_stat("amount"))

func get_calculated_pierce(base_pierce: int) -> int:
	if stats == null or not stats.has_method("get_stat"):
		return base_pierce
	return base_pierce + int(stats.get_stat("pierce"))

func get_calculated_speed(base_speed: float) -> float:
	if stats == null or not stats.has_method("get_stat"):
		return base_speed
	return base_speed * stats.get_stat("projectile_speed")

func get_calculated_duration(base_duration: float) -> float:
	if stats == null or not stats.has_method("get_stat"):
		return base_duration
	return base_duration * stats.get_stat("duration")

## --- Virtual Hooks for Passive Traits to provide Stat Modifiers ---

func apply_passive_modifiers() -> void:
	pass

func remove_passive_modifiers() -> void:
	pass

## --- UI Representation ---

func get_ui_data() -> Dictionary:
	var cd_pct: float = 0.0
	var eff_cd = get_calculated_cooldown()
	if eff_cd > 0.0 and not is_passive:
		cd_pct = clampf(cooldown_timer / eff_cd, 0.0, 1.0)
	return {
		"id": skill_id,
		"name": tr(name_key) if name_key != "" else skill_id,
		"description": tr(desc_key) if desc_key != "" else "",
		"biochemistry": tr(bio_key) if bio_key != "" else "",
		"icon": icon_symbol,
		"level": level,
		"max_level": max_level,
		"is_passive": is_passive,
		"is_innate": is_innate,
		"cooldown_max": eff_cd,
		"cooldown_ratio": cd_pct,
		"cooldown_time": max(0.0, cooldown_timer)
	}
