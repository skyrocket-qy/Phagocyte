class_name PassiveLysosomePriming
extends BaseSkill

## Passive Organelle Trait: Lysosome Priming (溶酶體酵素活化)
## Universal Stat Modifiers per level: Might +10%, Health Regen +0.6 HP/s

const MIGHT_PER_LEVEL: float = 0.10
const REGEN_PER_LEVEL: float = 0.6

func _init() -> void:
	skill_id = "passive_lysosome"
	name_key = "SKILL_LYSOSOME_NAME"
	desc_key = "SKILL_LYSOSOME_DESC"
	bio_key = "SKILL_LYSOSOME_BIO"
	icon_symbol = "🧪"
	is_innate = false
	is_passive = true
	cooldown = 0.0
	level = 1
	max_level = 5

func apply_passive_modifiers() -> void:
	if stats != null:
		var bonus_might = MIGHT_PER_LEVEL * float(level)
		var bonus_regen = REGEN_PER_LEVEL * float(level)
		stats.add_modifier("might", 0.0, bonus_might)
		stats.add_modifier("health_regen", bonus_regen, 0.0)

func remove_passive_modifiers() -> void:
	if stats != null:
		var bonus_might = MIGHT_PER_LEVEL * float(level)
		var bonus_regen = REGEN_PER_LEVEL * float(level)
		stats.remove_modifier("might", 0.0, bonus_might)
		stats.remove_modifier("health_regen", bonus_regen, 0.0)
