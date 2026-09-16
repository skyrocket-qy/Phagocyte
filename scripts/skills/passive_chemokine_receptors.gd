class_name PassiveChemokineReceptors
extends BaseSkill

## Passive Organelle Trait: Chemokine Receptors (趨化因子受體)
## Universal Stat Modifiers per level: Magnet +15%, Luck +10%

const MAGNET_PER_LEVEL: float = 0.15
const LUCK_PER_LEVEL: float = 0.10

func _init() -> void:
	skill_id = "passive_chemokine"
	name_key = "SKILL_CHEMOKINE_NAME"
	desc_key = "SKILL_CHEMOKINE_DESC"
	bio_key = "SKILL_CHEMOKINE_BIO"
	icon_symbol = "🧲"
	is_innate = false
	is_passive = true
	cooldown = 0.0
	level = 1
	max_level = 5

func apply_passive_modifiers() -> void:
	if stats != null:
		var bonus_mag = MAGNET_PER_LEVEL * float(level)
		var bonus_luck = LUCK_PER_LEVEL * float(level)
		stats.add_modifier("magnet", 0.0, bonus_mag)
		stats.add_modifier("luck", 0.0, bonus_luck)

func remove_passive_modifiers() -> void:
	if stats != null:
		var bonus_mag = MAGNET_PER_LEVEL * float(level)
		var bonus_luck = LUCK_PER_LEVEL * float(level)
		stats.remove_modifier("magnet", 0.0, bonus_mag)
		stats.remove_modifier("luck", 0.0, bonus_luck)
