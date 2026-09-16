class_name PassiveActinPolymerization
extends BaseSkill

## Passive Organelle Trait: Actin Polymerization (肌動蛋白微絲聚合)
## Universal Stat Modifiers per level: Area +12%, Move Speed +6%

const AREA_PER_LEVEL: float = 0.12
const SPEED_PER_LEVEL: float = 0.06

func _init() -> void:
	skill_id = "passive_actin"
	name_key = "SKILL_ACTIN_NAME"
	desc_key = "SKILL_ACTIN_DESC"
	bio_key = "SKILL_ACTIN_BIO"
	icon_symbol = "🧬"
	is_innate = false
	is_passive = true
	cooldown = 0.0
	level = 1
	max_level = 5

func apply_passive_modifiers() -> void:
	if stats != null:
		var bonus_area = AREA_PER_LEVEL * float(level)
		var bonus_speed = SPEED_PER_LEVEL * float(level)
		stats.add_modifier("area", 0.0, bonus_area)
		stats.add_modifier("move_speed", 0.0, bonus_speed)

func remove_passive_modifiers() -> void:
	if stats != null:
		var bonus_area = AREA_PER_LEVEL * float(level)
		var bonus_speed = SPEED_PER_LEVEL * float(level)
		stats.remove_modifier("area", 0.0, bonus_area)
		stats.remove_modifier("move_speed", 0.0, bonus_speed)
