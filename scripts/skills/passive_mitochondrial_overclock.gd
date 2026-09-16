class_name PassiveMitochondrialOverclock
extends BaseSkill

## Passive Organelle Trait: Mitochondrial Overclock (線粒體超頻)
## Universal Stat Modifiers per level: Cooldown Reduction +8% (flat), Duration +10%

const CDR_PER_LEVEL: float = 0.08
const DURATION_PER_LEVEL: float = 0.10

func _init() -> void:
	skill_id = "passive_mitochondria"
	name_key = "SKILL_MITOCHONDRIA_NAME"
	desc_key = "SKILL_MITOCHONDRIA_DESC"
	bio_key = "SKILL_MITOCHONDRIA_BIO"
	icon_symbol = "⚡"
	is_innate = false
	is_passive = true
	cooldown = 0.0
	level = 1
	max_level = 5

func apply_passive_modifiers() -> void:
	if stats != null:
		var bonus_cdr = CDR_PER_LEVEL * float(level)
		var bonus_duration = DURATION_PER_LEVEL * float(level)
		stats.add_modifier("cooldown_reduction", bonus_cdr, 0.0)
		stats.add_modifier("duration", 0.0, bonus_duration)

func remove_passive_modifiers() -> void:
	if stats != null:
		var bonus_cdr = CDR_PER_LEVEL * float(level)
		var bonus_duration = DURATION_PER_LEVEL * float(level)
		stats.remove_modifier("cooldown_reduction", bonus_cdr, 0.0)
		stats.remove_modifier("duration", 0.0, bonus_duration)
