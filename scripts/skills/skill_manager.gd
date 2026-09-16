class_name SkillManager
extends Node2D

## Manages 5 Active Cytokine Weapon slots and 5 Passive Organelle Trait slots.
## Separates weapon ticks and stat modifier lifecycles cleanly.

signal skills_changed()
signal skill_equipped(is_passive: bool, slot_index: int, skill: BaseSkill)

const MAX_ACTIVE_SLOTS: int = 5
const MAX_PASSIVE_SLOTS: int = 5

var active_slots: Array[BaseSkill] = []
var passive_slots: Array[BaseSkill] = []

var host: CharacterBody2D = null

func _init() -> void:
	active_slots.resize(MAX_ACTIVE_SLOTS)
	passive_slots.resize(MAX_PASSIVE_SLOTS)
	for i in range(MAX_ACTIVE_SLOTS):
		active_slots[i] = null
	for i in range(MAX_PASSIVE_SLOTS):
		passive_slots[i] = null

func setup(p_host: CharacterBody2D) -> void:
	host = p_host

## General equip method that automatically routes based on skill.is_passive
func equip_skill(skill: BaseSkill, target_slot: int = -1) -> bool:
	if skill == null:
		return false
	if skill.is_passive:
		return equip_passive(skill, target_slot)
	else:
		return equip_active(skill, target_slot)

## Equips an Active weapon skill into active_slots (0..4)
func equip_active(skill: BaseSkill, target_slot: int = -1) -> bool:
	if skill == null or skill.is_passive:
		return false

	if target_slot >= 0 and target_slot < MAX_ACTIVE_SLOTS:
		if active_slots[target_slot] != null and active_slots[target_slot].is_innate:
			return false
		_assign_active_slot(skill, target_slot)
		return true

	for i in range(MAX_ACTIVE_SLOTS):
		if active_slots[i] == null:
			_assign_active_slot(skill, i)
			return true
	return false

## Equips a Passive organelle trait into passive_slots (0..4)
func equip_passive(skill: BaseSkill, target_slot: int = -1) -> bool:
	if skill == null or not skill.is_passive:
		return false

	if target_slot >= 0 and target_slot < MAX_PASSIVE_SLOTS:
		if passive_slots[target_slot] != null and passive_slots[target_slot].is_innate:
			return false
		_assign_passive_slot(skill, target_slot)
		return true

	for i in range(MAX_PASSIVE_SLOTS):
		if passive_slots[i] == null:
			_assign_passive_slot(skill, i)
			return true
	return false

func _assign_active_slot(skill: BaseSkill, slot_idx: int) -> void:
	if active_slots[slot_idx] != null and is_instance_valid(active_slots[slot_idx]):
		active_slots[slot_idx].queue_free()

	active_slots[slot_idx] = skill
	add_child(skill)
	skill.setup(host, slot_idx)
	skill_equipped.emit(false, slot_idx, skill)
	skills_changed.emit()

func _assign_passive_slot(skill: BaseSkill, slot_idx: int) -> void:
	if passive_slots[slot_idx] != null and is_instance_valid(passive_slots[slot_idx]):
		passive_slots[slot_idx].queue_free()

	passive_slots[slot_idx] = skill
	add_child(skill)
	skill.setup(host, slot_idx)
	skill_equipped.emit(true, slot_idx, skill)
	skills_changed.emit()

func get_active_slot(slot_idx: int) -> BaseSkill:
	if slot_idx >= 0 and slot_idx < MAX_ACTIVE_SLOTS:
		return active_slots[slot_idx]
	return null

func get_passive_slot(slot_idx: int) -> BaseSkill:
	if slot_idx >= 0 and slot_idx < MAX_PASSIVE_SLOTS:
		return passive_slots[slot_idx]
	return null

## Update loop: Only active weapons tick cooldowns and trigger
func update_all_skills(delta: float) -> void:
	for skill in active_slots:
		if skill != null and is_instance_valid(skill):
			skill.update_skill(delta)

## Categorized UI Data for Dual-Row HUD (5 Active + 5 Passive)
func get_ui_data() -> Dictionary:
	var active_list: Array[Dictionary] = []
	for i in range(MAX_ACTIVE_SLOTS):
		var s = active_slots[i]
		if s != null and is_instance_valid(s):
			active_list.append(s.get_ui_data())
		else:
			active_list.append(_get_empty_slot_data(false))

	var passive_list: Array[Dictionary] = []
	for i in range(MAX_PASSIVE_SLOTS):
		var s = passive_slots[i]
		if s != null and is_instance_valid(s):
			passive_list.append(s.get_ui_data())
		else:
			passive_list.append(_get_empty_slot_data(true))

	return {
		"actives": active_list,
		"passives": passive_list
	}

## Flat UI Data array (10 items total: 0..4 actives, 5..9 passives)
func get_all_ui_data() -> Array[Dictionary]:
	var res: Array[Dictionary] = []
	var ui_dict = get_ui_data()
	res.append_array(ui_dict["actives"])
	res.append_array(ui_dict["passives"])
	return res

func _get_empty_slot_data(is_pass: bool) -> Dictionary:
	return {
		"id": "",
		"name": tr("SKILL_EMPTY"),
		"description": "",
		"biochemistry": "",
		"icon": "+",
		"level": 0,
		"max_level": 0,
		"is_passive": is_pass,
		"is_innate": false,
		"cooldown_max": 0.0,
		"cooldown_ratio": 0.0,
		"cooldown_time": 0.0
	}
