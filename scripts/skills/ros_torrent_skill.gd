class_name ROSTorrentSkill
extends BaseSkill

@export var jet_scene: PackedScene = preload("res://scenes/skills/ros_jet.tscn")
@export var attack_range: float = 650.0

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
	_fire_jet(target_dir)

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

func _fire_jet(dir: Vector2) -> void:
	var jet = jet_scene.instantiate()
	host.get_parent().add_child(jet)
	jet.setup(host, host.global_position, dir)
