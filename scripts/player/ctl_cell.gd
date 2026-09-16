class_name CTLCell
extends "res://scripts/player/base_cell.gd"

## Killer T-Cell (CTL / CD8+)
## High-Speed Assassin / Perforation.
## Compact micro-trembling spherical body with a massive circular nucleus
## occupying ~80% of cell volume. High base speed & critical strike affinity.

const PerforinLanceClass = preload("res://scripts/skills/perforin_lance_skill.gd")

const COLOR_NORMAL: Color = Color(0.85, 0.18, 0.32, 0.65)
const COLOR_BURST: Color = Color(1.0, 0.45, 0.15, 0.85)
const MEMBRANE_NORMAL: Color = Color(1.0, 0.35, 0.50, 0.95)
const MEMBRANE_BURST: Color = Color(1.0, 0.85, 0.30, 1.0)

func _setup_cell_identity() -> void:
	max_health = 75.0
	base_speed = 280.0
	base_radius = 42.0
	base_deformation_mag = 6.0
	deformation_speed = 7.2

	noise = FastNoiseLite.new()
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	noise.seed = randi()
	noise.frequency = 2.4
	noise.fractal_octaves = 1

	# Critical Strike & Agility innate bonuses
	if stats:
		stats.set_base("crit_chance", 0.15)
		stats.set_base("crit_damage", 1.75)
		stats.set_base("might", 1.10)

func _setup_nucleus_shape() -> void:
	# Massive circular nucleus occupying ~80% of interior
	var n_pts := PackedVector2Array()
	var n_count: int = 24
	var n_radius: float = 32.0
	for i in range(n_count):
		var a: float = i * (TAU / float(n_count))
		n_pts.append(Vector2(cos(a) * n_radius, sin(a) * n_radius))
	if nucleus:
		nucleus.polygon = n_pts
		nucleus.color = Color(0.48, 0.06, 0.16, 0.92)

func _setup_initial_skills() -> void:
	var perforin = PerforinLanceClass.new()
	skill_manager.equip_active(perforin, 0)

func _get_burst_move_speed(base_sp: float) -> float:
	return base_sp * 2.2

func _apply_burst_visuals(active: bool) -> void:
	if active:
		deformation_speed = 10.0
		if cytoplasm: cytoplasm.color = COLOR_BURST
		if membrane: membrane.default_color = MEMBRANE_BURST
	else:
		deformation_speed = 7.2
		if cytoplasm: cytoplasm.color = COLOR_NORMAL
		if membrane: membrane.default_color = MEMBRANE_NORMAL

func _on_pathogen_consumed(enemy: Node2D, _atp: float) -> void:
	# Perforation execution: chance to trigger instant apoptosis
	if is_burst and enemy and is_instance_valid(enemy) and enemy.has_method("take_damage"):
		enemy.take_damage(60.0)
