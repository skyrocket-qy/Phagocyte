class_name BaseSkill
extends Node2D

signal cooldown_updated(time_left: float, max_time: float)
signal skill_activated()
signal skill_upgraded(new_level: int)

@export var skill_id: String = ""
@export var name_key: String = ""
@export var desc_key: String = ""
@export var icon_symbol: String = "⚡"
@export var level: int = 1
@export var max_level: int = 5
@export var cooldown: float = 0.0
@export var is_passive: bool = false
@export var is_innate: bool = false

var cooldown_timer: float = 0.0
var slot_index: int = -1
var host: CharacterBody2D = null

func setup(p_host: CharacterBody2D, p_slot: int) -> void:
	host = p_host
	slot_index = p_slot

func update_skill(delta: float) -> void:
	if is_passive:
		return
	if cooldown <= 0.0:
		return

	if cooldown_timer > 0.0:
		cooldown_timer -= delta
		cooldown_updated.emit(max(0.0, cooldown_timer), cooldown)
		if cooldown_timer <= 0.0:
			trigger()

func trigger() -> void:
	skill_activated.emit()
	cooldown_timer = cooldown

func upgrade() -> void:
	if level < max_level:
		level += 1
		skill_upgraded.emit(level)

func get_ui_data() -> Dictionary:
	var cd_pct: float = 0.0
	if cooldown > 0.0 and not is_passive:
		cd_pct = clampf(cooldown_timer / cooldown, 0.0, 1.0)
	return {
		"id": skill_id,
		"name": tr(name_key),
		"description": tr(desc_key),
		"icon": icon_symbol,
		"level": level,
		"max_level": max_level,
		"is_passive": is_passive,
		"is_innate": is_innate,
		"cooldown_ratio": cd_pct,
		"cooldown_time": max(0.0, cooldown_timer)
	}
