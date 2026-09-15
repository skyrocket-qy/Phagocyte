class_name MacrophageDeformationSkill
extends BaseSkill

@export var vertex_count: int = 32
@export var deformation_speed: float = 3.6
@export var base_deformation_mag: float = 30.0
var current_deformation_mag: float = 30.0

var noise: FastNoiseLite
var noise_time: float = 0.0

func _init() -> void:
	skill_id = "macrophage_pseudopods"
	name_key = "SKILL_DEFORM_NAME"
	desc_key = "SKILL_DEFORM_DESC"
	bio_key = "SKILL_DEFORM_BIO"
	icon_symbol = "🦠"
	is_innate = true
	is_passive = true
	level = 1
	max_level = 5

func setup(p_host: CharacterBody2D, p_slot: int) -> void:
	super.setup(p_host, p_slot)
	noise = FastNoiseLite.new()
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	noise.seed = randi()
	noise.frequency = 0.65
	noise.fractal_octaves = 2

func update_skill(delta: float) -> void:
	super.update_skill(delta)
	if not host or not is_instance_valid(host):
		return
	_update_pseudopod_deformation(delta)
	_update_nucleus(delta)

func _update_pseudopod_deformation(delta: float) -> void:
	var def_speed = host.get("deformation_speed") if "deformation_speed" in host else deformation_speed
	noise_time += delta * def_speed

	var satiety = host.get("satiety") if "satiety" in host else 0.0
	var max_satiety = host.get("max_satiety") if "max_satiety" in host else 100.0
	var base_r = host.get("base_radius") if "base_radius" in host else 63.0
	var is_burst = host.get("is_respiratory_burst") if "is_respiratory_burst" in host else false

	# Satiety radius expansion (1.0x to 2.5x)
	var expansion_ratio: float = 1.0 + (satiety / max(1.0, max_satiety)) * 1.5
	var cur_r: float = base_r * expansion_ratio
	current_deformation_mag = base_deformation_mag * expansion_ratio

	if is_burst:
		cur_r *= 1.15
		current_deformation_mag *= 1.35

	host.set("current_radius", cur_r)

	var points := PackedVector2Array()
	var angle_step: float = TAU / float(vertex_count)
	var vel: Vector2 = host.velocity
	var move_dir: Vector2 = vel.normalized() if vel.length() > 20.0 else Vector2.ZERO

	for i in range(vertex_count):
		var angle: float = i * angle_step
		var dir := Vector2(cos(angle), sin(angle))

		var nx: float = cos(angle) * 1.8
		var ny: float = sin(angle) * 1.8
		var n_val: float = noise.get_noise_3d(nx, ny, noise_time)

		var forward_bias: float = 0.0
		if move_dir != Vector2.ZERO:
			var dot: float = max(0.0, dir.dot(move_dir))
			forward_bias = dot * (current_deformation_mag * 0.6)

		var r: float = cur_r + (n_val * current_deformation_mag) + forward_bias
		points.append(dir * max(15.0, r))

	# Update visual Cytoplasm Polygon2D
	if host.has_node("Cytoplasm"):
		host.get_node("Cytoplasm").polygon = points

	# Update Membrane Line2D
	if host.has_node("Membrane"):
		var line_points := points.duplicate()
		if line_points.size() > 0:
			line_points.append(points[0])
		host.get_node("Membrane").points = line_points

	# "What you see is what you touch": Deep copy to CollisionPolygon2D
	if host.has_node("EngulfArea/EngulfCollider"):
		host.get_node("EngulfArea/EngulfCollider").polygon = points

func _update_nucleus(delta: float) -> void:
	if not host.has_node("Nucleus"):
		return

	var nucleus_offset: Vector2 = host.get("nucleus_offset") if "nucleus_offset" in host else Vector2.ZERO
	var target_offset: Vector2 = host.get("nucleus_target_offset") if "nucleus_target_offset" in host else Vector2.ZERO
	var satiety = host.get("satiety") if "satiety" in host else 0.0
	var max_satiety = host.get("max_satiety") if "max_satiety" in host else 100.0

	nucleus_offset = nucleus_offset.lerp(target_offset, 8.0 * delta)
	host.set("nucleus_offset", nucleus_offset)

	var nucleus_node = host.get_node("Nucleus")
	nucleus_node.position = nucleus_offset

	var n_scale: float = 1.0 + (satiety / max(1.0, max_satiety)) * 0.8
	nucleus_node.scale = Vector2(n_scale, n_scale)
