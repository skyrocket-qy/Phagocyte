class_name PseudopodLungeSkill
extends BaseSkill

## Active Weapon: Pseudopod Lunge (偽足猛擊)
## Snaps out an elongated amoebic arm to pull enemies in for phagocytosis.

@export var base_damage: float = 30.0
@export var base_reach: float = 260.0

func _init() -> void:
	skill_id = "pseudopod_lunge"
	name_key = "SKILL_LUNGE_NAME"
	desc_key = "SKILL_LUNGE_DESC"
	bio_key = "SKILL_LUNGE_BIO"
	icon_symbol = "🥊"
	is_innate = false
	is_passive = false
	cooldown = 3.0
	cooldown_timer = 2.5
	level = 1
	max_level = 5

func trigger() -> void:
	super.trigger()
	if not host or not is_instance_valid(host):
		return

	var amount = get_calculated_amount(1)
	var reach = get_calculated_area(base_reach)
	var pathogens = host.get_tree().get_nodes_in_group("pathogens")

	var pulled: int = 0
	for p in pathogens:
		if pulled >= amount:
			break
		if p is Node2D and is_instance_valid(p) and not p.get("is_being_eaten"):
			if host.global_position.distance_to(p.global_position) <= reach:
				pulled += 1
				# Pull pathogen rapidly toward player
				var tween = host.create_tween()
				tween.tween_property(p, "global_position", host.global_position, 0.15)
				tween.tween_callback(func():
					if p and is_instance_valid(p) and p.has_method("be_engulfed"):
						p.be_engulfed(host)
				)
