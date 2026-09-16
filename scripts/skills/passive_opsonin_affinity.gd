class_name PassiveOpsoninAffinity
extends BaseSkill

## Passive Organelle Trait: Opsonin Affinity (調理素親和)
## Universal Stat Modifiers per level: Crit Chance +5% (flat), Crit Damage +25%

const CRIT_PER_LEVEL: float = 0.05
const CRIT_DMG_PER_LEVEL: float = 0.25

func _init() -> void:
	skill_id = "passive_opsonin"
	name_key = "SKILL_OPSONIN_NAME"
	desc_key = "SKILL_OPSONIN_DESC"
	bio_key = "SKILL_OPSONIN_BIO"
	icon_symbol = "🎯"
	is_innate = false
	is_passive = true
	cooldown = 0.0
	level = 1
	max_level = 5

func apply_passive_modifiers() -> void:
	if stats != null:
		var bonus_crit = CRIT_PER_LEVEL * float(level)
		var bonus_crit_dmg = CRIT_DMG_PER_LEVEL * float(level)
		stats.add_modifier("crit_chance", bonus_crit, 0.0)
		stats.add_modifier("crit_damage", 0.0, bonus_crit_dmg)

func remove_passive_modifiers() -> void:
	if stats != null:
		var bonus_crit = CRIT_PER_LEVEL * float(level)
		var bonus_crit_dmg = CRIT_DMG_PER_LEVEL * float(level)
		stats.remove_modifier("crit_chance", bonus_crit, 0.0)
		stats.remove_modifier("crit_damage", 0.0, bonus_crit_dmg)
