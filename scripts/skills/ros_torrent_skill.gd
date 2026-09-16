class_name ROSTorrentSkill
extends BaseSkill

@export var jet_scene: PackedScene = preload("res://scenes/skills/ros_jet.tscn")
@export var attack_range: float = 650.0
@export var base_jet_speed: float = 520.0
@export var base_jet_lifetime: float = 0.9

func _init() -> void:
	skill_id = "ros_torrent"
	name_key = "SKILL_ROS_NAME"
	desc_key = "SKILL_ROS_DESC"
	bio_key = "SKILL_ROS_BIO"
	icon_symbol = "💨"
	is_innate = false
	is_passive = false
	cooldown = 3.2
	cooldown_timer = 1.0 # start soon after spawn
	level = 1
	max_level = 5

func trigger() -> void:
	super.trigger()
	if not host or not is_instance_valid(host):
		return

	var target_dir = _find_target_direction()
	var amount = get_calculated_amount(1)
	var area_scale = get_calculated_area(1.0)
	var jet_speed = get_calculated_speed(base_jet_speed)
	var jet_life = get_calculated_duration(base_jet_lifetime)

	if amount <= 1:
		_fire_jet(target_dir, area_scale, jet_speed, jet_life)
	else:
		# Fire fan spread of jets
		var spread_angle: float = 0.22 # radians
		var start_angle: float = -spread_angle * (float(amount - 1) / 2.0)
		for i in range(amount):
			var angle = start_angle + i * spread_angle
			var dir = target_dir.rotated(angle)
			_fire_jet(dir, area_scale, jet_speed, jet_life)

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

	# Fallback to current velocity or facing
	if host.velocity.length() > 20.0:
		return host.velocity.normalized()
	return Vector2.RIGHT

func _fire_jet(dir: Vector2, area_mult: float, speed_val: float, life_val: float) -> void:
	var jet = jet_scene.instantiate()
	host.get_parent().add_child(jet)
	jet.setup(host, host.global_position, dir)
	jet.speed = speed_val
	jet.lifetime = life_val
	jet.scale = Vector2(area_mult, area_mult)
