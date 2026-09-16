class_name PerforinLanceSkill
extends BaseSkill

## Active Weapon: Perforin Lance (穿孔素長矛)
## Straight piercing ray that punctures through a line of pathogens.

@export var base_damage: float = 35.0
@export var attack_range: float = 700.0

func _init() -> void:
	skill_id = "perforin_lance"
	name_key = "SKILL_PERFORIN_NAME"
	desc_key = "SKILL_PERFORIN_DESC"
	bio_key = "SKILL_PERFORIN_BIO"
	icon_symbol = "🗡️"
	is_innate = false
	is_passive = false
	cooldown = 2.8
	cooldown_timer = 0.5
	level = 1
	max_level = 5

func trigger() -> void:
	super.trigger()
	if not host or not is_instance_valid(host):
		return

	var target_dir = _find_target_direction()
	var amount = get_calculated_amount(1)
	var pierce_limit = get_calculated_pierce(3)

	for i in range(amount):
		var dir = target_dir
		if amount > 1:
			var angle_offset = (i - (amount - 1) / 2.0) * 0.15
			dir = dir.rotated(angle_offset)
		_execute_lance_strike(dir, pierce_limit)

func _find_target_direction() -> Vector2:
	var pathogens = host.get_tree().get_nodes_in_group("pathogens")
	var closest_enemy: Node2D = null
	var min_dist: float = attack_range

	for p in pathogens:
		if p is Node2D and is_instance_valid(p) and not p.get("is_being_eaten"):
			var d = host.global_position.distance_to(p.global_position)
			if d < min_dist:
				min_dist = d
				closest_enemy = p

	if closest_enemy != null:
		return (closest_enemy.global_position - host.global_position).normalized()

	if host.velocity.length() > 20.0:
		return host.velocity.normalized()
	return Vector2.RIGHT

func _execute_lance_strike(dir: Vector2, pierce_limit: int) -> void:
	var start_pos = host.global_position
	var end_pos = start_pos + dir * attack_range
	var pathogens = host.get_tree().get_nodes_in_group("pathogens")
	var hit_count: int = 0
	var beam_width: float = 24.0 * get_calculated_area(1.0)

	for p in pathogens:
		if hit_count >= pierce_limit:
			break
		if p is Node2D and is_instance_valid(p) and not p.get("is_being_eaten"):
			var p_pos = p.global_position
			var proj_point = Geometry2D.get_closest_point_to_segment(p_pos, start_pos, end_pos)
			if proj_point.distance_to(p_pos) <= beam_width:
				hit_count += 1
				if p.has_method("be_engulfed"):
					p.be_engulfed(host)
