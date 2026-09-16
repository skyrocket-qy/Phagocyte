class_name NeutrophilCell
extends "res://scripts/player/base_cell.gd"

## Neutrophil (嗜中性球)
## Kamikaze Demolition / Holdout.
## Features jittery granular outer membrane, distinctive multi-lobed segmented nucleus (3-4 lobes),
## Might +15%, and Degranulation Storm burst.

const ComplementCascadeClass = preload("res://scripts/skills/complement_cascade_skill.gd")

const COLOR_NORMAL: Color = Color(0.88, 0.82, 0.65, 0.70)
const COLOR_BURST: Color = Color(1.0, 0.92, 0.35, 0.85)
const MEMBRANE_NORMAL: Color = Color(0.96, 0.88, 0.55, 0.95)
const MEMBRANE_BURST: Color = Color(1.0, 0.98, 0.60, 1.0)

func _setup_cell_identity() -> void:
	max_health = 90.0
	base_speed = 240.0
	base_radius = 46.0
	base_deformation_mag = 14.0
	deformation_speed = 5.0

	noise = FastNoiseLite.new()
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	noise.seed = randi()
	noise.frequency = 1.8
	noise.fractal_octaves = 2

	if stats:
		stats.set_base("might", 1.15)
		stats.set_base("health_regen", 0.5)

func _setup_nucleus_shape() -> void:
	# Segmented 3-4 lobed nucleus (characteristic polymorphonuclear shape)
	var n_pts := PackedVector2Array()
	var n_count: int = 32
	var base_r: float = 18.0
	for i in range(n_count):
		var a: float = i * (TAU / float(n_count))
		# 3-lobed modulation
		var lobe_mod = 1.0 + 0.45 * cos(a * 3.0)
		var r = base_r * lobe_mod
		n_pts.append(Vector2(cos(a) * r, sin(a) * r))
	if nucleus:
		nucleus.polygon = n_pts
		nucleus.color = Color(0.38, 0.15, 0.55, 0.9)

func _setup_initial_skills() -> void:
	var comp = ComplementCascadeClass.new()
	skill_manager.equip_active(comp, 0)

func _get_burst_move_speed(base_sp: float) -> float:
	return base_sp * 2.3

func _apply_burst_visuals(active: bool) -> void:
	if active:
		deformation_speed = 8.0
		if cytoplasm: cytoplasm.color = COLOR_BURST
		if membrane: membrane.default_color = MEMBRANE_BURST
	else:
		deformation_speed = 5.0
		if cytoplasm: cytoplasm.color = COLOR_NORMAL
		if membrane: membrane.default_color = MEMBRANE_NORMAL
