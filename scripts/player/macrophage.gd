class_name Macrophage
extends "res://scripts/player/base_cell.gd"

## Macrophage (巨噬細胞)
## Heavy Melee Tank / Phagocytosis sentinel.
## Features amoeboid organic pseudopods, kidney-shaped nucleus,
## 0.5% max HP passive recovery per digestion, and Respiratory Burst with Acidic Aura.

@onready var acidic_aura: Area2D = $AcidicAura
@onready var aura_collider: CollisionShape2D = $AcidicAura/AuraCollider

# Cytoplasm coloring
const COLOR_NORMAL: Color = Color(0.18, 0.72, 0.65, 0.62)
const COLOR_BURST: Color = Color(0.96, 0.82, 0.16, 0.82)
const MEMBRANE_NORMAL: Color = Color(0.45, 0.95, 0.85, 0.9)
const MEMBRANE_BURST: Color = Color(1.0, 0.95, 0.4, 1.0)

# Backward-compatibility alias for tests and HUD
var is_respiratory_burst: bool:
	get: return is_burst
	set(v): is_burst = v

const ROSTorrentClass = preload("res://scripts/skills/ros_torrent_skill.gd")

func _setup_cell_identity() -> void:
	max_health = 100.0
	base_speed = 230.0
	base_radius = 48.0
	base_deformation_mag = 24.0
	deformation_speed = 3.6

	noise = FastNoiseLite.new()
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	noise.seed = randi()
	noise.frequency = 0.65
	noise.fractal_octaves = 2

	if acidic_aura and not acidic_aura.area_entered.is_connected(_on_acidic_aura_entered):
		acidic_aura.area_entered.connect(_on_acidic_aura_entered)
	if acidic_aura:
		acidic_aura.monitoring = false

func _setup_nucleus_shape() -> void:
	# Indented kidney / horseshoe shaped nucleus
	var n_pts := PackedVector2Array()
	var n_count: int = 18
	var n_radius: float = 16.0
	for i in range(n_count):
		var a: float = i * (TAU / float(n_count))
		# Kidney notch at angle PI
		var notch = 1.0 - 0.4 * max(0.0, cos(a))
		var r = n_radius * notch
		n_pts.append(Vector2(cos(a) * r, sin(a) * r))
	if nucleus:
		nucleus.polygon = n_pts
		nucleus.color = Color(0.42, 0.22, 0.68, 0.85)

func _setup_initial_skills() -> void:
	var ros = ROSTorrentClass.new()
	skill_manager.equip_active(ros, 0)

func _on_pathogen_consumed(_enemy: Node2D, _atp: float) -> void:
	# Macrophage Inherent Trait: Heals 0.5% max HP per digested pathogen
	var max_hp = stats.get_stat("max_health") if stats != null else 100.0
	heal(max_hp * 0.005)

func trigger_respiratory_burst() -> void:
	trigger_burst()

func _apply_burst_visuals(active: bool) -> void:
	if active:
		deformation_speed = 6.0
		if cytoplasm: cytoplasm.color = COLOR_BURST
		if membrane: membrane.default_color = MEMBRANE_BURST
		if acidic_aura: acidic_aura.monitoring = true
	else:
		deformation_speed = 3.6
		if cytoplasm: cytoplasm.color = COLOR_NORMAL
		if membrane: membrane.default_color = MEMBRANE_NORMAL
		if acidic_aura: acidic_aura.monitoring = false

func _on_acidic_aura_entered(area: Area2D) -> void:
	if is_burst:
		var enemy = area.get_parent()
		if enemy and enemy.has_method("be_engulfed"):
			_consume_pathogen(enemy)
