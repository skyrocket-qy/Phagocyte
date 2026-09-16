class_name AntibodySalvoSkill
extends BaseSkill

## Active Weapon: Antibody Salvo (Y型抗體齊射)
## Fires auto-homing Y-antibodies that track and neutralize nearby pathogens.

@export var base_damage: float = 18.0
@export var base_missile_count: int = 3
@export var search_range: float = 600.0

func _init() -> void:
	skill_id = "antibody_salvo"
	name_key = "SKILL_ANTIBODY_NAME"
	desc_key = "SKILL_ANTIBODY_DESC"
	bio_key = "SKILL_ANTIBODY_BIO"
	icon_symbol = "🏹"
	is_innate = false
	is_passive = false
	cooldown = 3.5
	cooldown_timer = 2.0
	level = 1
	max_level = 5

func trigger() -> void:
	super.trigger()
	if not host or not is_instance_valid(host):
		return

	var amount = get_calculated_amount(base_missile_count)
	var pathogens = _get_nearby_pathogens()

	for i in range(amount):
		var target: Node2D = null
		if pathogens.size() > 0:
			target = pathogens[i % pathogens.size()]
		_fire_antibody(target, i, amount)

func _get_nearby_pathogens() -> Array[Node2D]:
	var result: Array[Node2D] = []
	var all_pathogens = host.get_tree().get_nodes_in_group("pathogens")
	for p in all_pathogens:
		if p is Node2D and is_instance_valid(p) and not p.get("is_being_eaten"):
			if host.global_position.distance_to(p.global_position) <= search_range:
				result.append(p)
	return result

func _fire_antibody(target: Node2D, index: int, total: int) -> void:
	if not host or not is_instance_valid(host):
		return

	var angle = (float(index) / float(max(1, total))) * TAU
	var spawn_dir = Vector2.from_angle(angle)
	var spawn_pos = host.global_position + spawn_dir * 30.0

	# If target is valid, home in on it after short delay
	if target and is_instance_valid(target):
		var tree = host.get_tree()
		if tree != null:
			var timer = tree.create_timer(0.2 + index * 0.05)
			timer.timeout.connect(func():
				if target and is_instance_valid(target) and not target.get("is_being_eaten"):
					if target.has_method("be_engulfed"):
						target.be_engulfed(host)
			)
