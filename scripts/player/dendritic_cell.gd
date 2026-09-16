class_name DendriticCell
extends "res://scripts/player/base_cell.gd"

## Dendritic Cell (樹突狀細胞)
## Tactical Commander / Antigen Summoner.
## Star-shaped sea-anemone body with long branching dendritic tree extensions.
## Specializes in Magnet pickup radius (+50%) and Growth (+25%).

const PseudopodLungeClass = preload("res://scripts/skills/pseudopod_lunge_skill.gd")

const COLOR_NORMAL: Color = Color(0.18, 0.72, 0.45, 0.65)
const COLOR_BURST: Color = Color(0.60, 1.0, 0.40, 0.90)
const MEMBRANE_NORMAL: Color = Color(0.40, 0.95, 0.65, 0.95)
const MEMBRANE_BURST: Color = Color(0.85, 1.0, 0.60, 1.0)

func _setup_cell_identity() -> void:
	max_health = 95.0
	base_speed = 215.0
	base_radius = 45.0
	base_deformation_mag = 28.0
	deformation_speed = 2.8

	noise = FastNoiseLite.new()
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	noise.seed = randi()
	noise.frequency = 0.5
	noise.fractal_octaves = 2

	if stats:
		stats.set_base("magnet", 1.50)
		stats.set_base("growth", 1.25)

func _setup_nucleus_shape() -> void:
	# Central irregular oval nucleus
	var n_pts := PackedVector2Array()
	var n_count: int = 20
	var base_r: float = 16.0
	for i in range(n_count):
		var a: float = i * (TAU / float(n_count))
		var r = base_r * (1.0 + 0.22 * sin(a * 2.0))
		n_pts.append(Vector2(cos(a) * r, sin(a) * r))
	if nucleus:
		nucleus.polygon = n_pts
		nucleus.color = Color(0.10, 0.35, 0.22, 0.90)

func _setup_initial_skills() -> void:
	var lunge = PseudopodLungeClass.new()
	skill_manager.equip_active(lunge, 0)

## Dendritic tree projection morphology override: 7 prominent radial branches
func _update_pseudopod_deformation(delta: float) -> void:
	noise_time += delta * deformation_speed

	var expansion_ratio: float = 1.0 + (satiety / max(1.0, max_satiety)) * 1.5
	var area_scale: float = stats.get_stat("area") if stats != null else 1.0

	var cur_r: float = base_radius * expansion_ratio * area_scale
	current_deformation_mag = base_deformation_mag * expansion_ratio * area_scale

	if is_burst:
		cur_r *= 1.20
		current_deformation_mag *= 1.45

	current_radius = cur_r

	var points := PackedVector2Array()
	var angle_step: float = TAU / float(vertex_count)
	var vel: Vector2 = velocity
	var move_dir: Vector2 = vel.normalized() if vel.length() > 20.0 else Vector2.ZERO

	for i in range(vertex_count):
		var angle: float = i * angle_step
		var dir := Vector2(cos(angle), sin(angle))

		var nx: float = cos(angle) * 1.5
		var ny: float = sin(angle) * 1.5
		var n_val: float = noise.get_noise_3d(nx, ny, noise_time) if noise else 0.0

		# 7-branch dendritic projection arms
		var arm_val = pow(max(0.0, cos(angle * 7.0)), 2.0) * (current_deformation_mag * 0.75)

		var forward_bias: float = 0.0
		if move_dir != Vector2.ZERO:
			var dot: float = max(0.0, dir.dot(move_dir))
			forward_bias = dot * (current_deformation_mag * 0.5)

		var r: float = cur_r + (n_val * current_deformation_mag * 0.6) + arm_val + forward_bias
		points.append(dir * max(14.0, r))

	var smooth_points := _smooth_closed_polygon(points, 2)

	if cytoplasm:
		cytoplasm.polygon = smooth_points
		var uvs := PackedVector2Array()
		var uv_denom: float = max(24.0, cur_r * 2.4)
		for pt in smooth_points:
			uvs.append((pt / uv_denom) + Vector2(0.5, 0.5))
		cytoplasm.uv = uvs
	if membrane:
		var line_points := smooth_points.duplicate()
		if line_points.size() > 0:
			line_points.append(smooth_points[0])
		membrane.points = line_points
	if engulf_collider:
		engulf_collider.polygon = points

	_update_nucleus(delta)

func _get_burst_move_speed(base_sp: float) -> float:
	return base_sp * 2.2

func _apply_burst_visuals(active: bool) -> void:
	if active:
		deformation_speed = 5.0
		if cytoplasm: cytoplasm.color = COLOR_BURST
		if membrane: membrane.default_color = MEMBRANE_BURST
	else:
		deformation_speed = 2.8
		if cytoplasm: cytoplasm.color = COLOR_NORMAL
		if membrane: membrane.default_color = MEMBRANE_NORMAL
