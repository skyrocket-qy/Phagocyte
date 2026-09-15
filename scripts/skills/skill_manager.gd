class_name SkillManager
extends Node2D

signal skills_changed()
signal skill_equipped(slot_index: int, skill: BaseSkill)

const MAX_SLOTS: int = 6
var slots: Array = []

var host: CharacterBody2D = null

func _init() -> void:
	slots.resize(MAX_SLOTS)
	for i in range(MAX_SLOTS):
		slots[i] = null

func setup(p_host: CharacterBody2D) -> void:
	host = p_host

func equip_skill(skill: BaseSkill, target_slot: int = -1) -> bool:
	if target_slot >= 0 and target_slot < MAX_SLOTS:
		if slots[target_slot] != null and slots[target_slot].is_innate:
			return false
		_assign_slot(skill, target_slot)
		return true

	for i in range(MAX_SLOTS):
		if slots[i] == null:
			_assign_slot(skill, i)
			return true
	return false

func _assign_slot(skill: BaseSkill, slot_idx: int) -> void:
	if slots[slot_idx] != null and is_instance_valid(slots[slot_idx]):
		slots[slot_idx].queue_free()

	slots[slot_idx] = skill
	add_child(skill)
	skill.setup(host, slot_idx)
	skill_equipped.emit(slot_idx, skill)
	skills_changed.emit()

func get_slot(slot_idx: int) -> BaseSkill:
	if slot_idx >= 0 and slot_idx < MAX_SLOTS:
		return slots[slot_idx]
	return null

func get_all_ui_data() -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	for i in range(MAX_SLOTS):
		var s = slots[i]
		if s != null and is_instance_valid(s):
			result.append(s.get_ui_data())
		else:
			result.append({
				"id": "",
				"name": tr("SKILL_EMPTY"),
				"description": "",
				"icon": "+",
				"level": 0,
				"max_level": 0,
				"is_passive": false,
				"is_innate": false,
				"cooldown_ratio": 0.0,
				"cooldown_time": 0.0
			})
	return result

func update_all_skills(delta: float) -> void:
	for i in range(MAX_SLOTS):
		var s = slots[i]
		if s != null and is_instance_valid(s):
			s.update_skill(delta)
