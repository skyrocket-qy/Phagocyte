class_name BCell
extends "res://scripts/player/base_cell.gd"

## B-Lymphocyte (B-Cell / 漿細胞)
## Ranged Launch / Guidance.
## Spherical cell with clock-face nucleus and surface receptor protrusions.
## Specializes in Cooldown Reduction (+15%) and Projectile Speed (+25%).

const AntibodySalvoClass = preload("res://scripts/skills/antibody_salvo_skill.gd")

const COLOR_NORMAL: Color = Color(0.20, 0.45, 0.90, 0.65)
const COLOR_BURST: Color = Color(0.40, 0.65, 1.0, 0.90)
const MEMBRANE_NORMAL: Color = Color(0.35, 0.75, 1.0, 0.95)
const MEMBRANE_BURST: Color = Color(0.80, 0.95, 1.0, 1.0)

var receptor_nodes: Array[Vector2] = []

func _setup_cell_identity() -> void:
	max_health = 85.0
	base_speed = 220.0
	base_radius = 44.0
	base_deformation_mag = 7.0
	deformation_speed = 3.2

	noise = FastNoiseLite.new()
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	noise.seed = randi()
	noise.frequency = 0.9
	noise.fractal_octaves = 2

	if stats:
		stats.set_base("cooldown_reduction", 0.15)
		stats.set_base("projectile_speed", 1.25)

func _setup_nucleus_shape() -> void:
	# Clock-face / Cartwheel spoke nucleus shape
	var n_pts := PackedVector2Array()
	var n_count: int = 24
	var base_r: float = 20.0
	for i in range(n_count):
		var a: float = i * (TAU / float(n_count))
		# 6-spoke cartwheel modulation
		var spoke = 1.0 + 0.18 * cos(a * 6.0)
		var r = base_r * spoke
		n_pts.append(Vector2(cos(a) * r, sin(a) * r))
	if nucleus:
		nucleus.polygon = n_pts
		nucleus.color = Color(0.12, 0.15, 0.48, 0.92)

func _setup_initial_skills() -> void:
	var salvo = AntibodySalvoClass.new()
	skill_manager.equip_active(salvo, 0)

func _get_burst_move_speed(base_sp: float) -> float:
	return base_sp * 2.2

func _apply_burst_visuals(active: bool) -> void:
	if active:
		deformation_speed = 6.0
		if cytoplasm: cytoplasm.color = COLOR_BURST
		if membrane: membrane.default_color = MEMBRANE_BURST
	else:
		deformation_speed = 3.2
		if cytoplasm: cytoplasm.color = COLOR_NORMAL
		if membrane: membrane.default_color = MEMBRANE_NORMAL
