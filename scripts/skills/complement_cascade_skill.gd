class_name ComplementCascadeSkill
extends BaseSkill

## Active Weapon: Complement Cascade (補體瀑布)
## Spawns chemical resonance rings that detonate after a short delay.

@export var base_damage: float = 45.0
@export var base_area_radius: float = 80.0

func _init() -> void:
	skill_id = "complement_cascade"
	name_key = "SKILL_COMPLEMENT_NAME"
	desc_key = "SKILL_COMPLEMENT_DESC"
	bio_key = "SKILL_COMPLEMENT_BIO"
	icon_symbol = "💥"
	is_innate = false
	is_passive = false
	cooldown = 4.0
	cooldown_timer = 1.5
	level = 1
	max_level = 5

func trigger() -> void:
	super.trigger()
	if not host or not is_instance_valid(host):
		return

	var amount = get_calculated_amount(1)
	var eff_radius = get_calculated_area(base_area_radius)
	var eff_duration = get_calculated_duration(1.2)

	for i in range(amount):
		var offset = Vector2.from_angle(randf() * TAU) * randf_range(50.0, 220.0)
		var spawn_pos = host.global_position + offset
		_spawn_resonance_ring(spawn_pos, eff_radius, eff_duration)

func _spawn_resonance_ring(pos: Vector2, radius: float, delay: float) -> void:
	var tree = host.get_tree()
	if tree == null:
		return

	# Create a simple timer to trigger MAC detonation
	var timer = tree.create_timer(delay)
	timer.timeout.connect(func():
		if not host or not is_instance_valid(host):
			return
		_detonate_mac_ring(pos, radius)
	)

func _detonate_mac_ring(center: Vector2, radius: float) -> void:
	var pathogens = host.get_tree().get_nodes_in_group("pathogens")
	for p in pathogens:
		if p is Node2D and is_instance_valid(p) and not p.get("is_being_eaten"):
			if center.distance_to(p.global_position) <= radius:
				if p.has_method("be_engulfed"):
					p.be_engulfed(host)
