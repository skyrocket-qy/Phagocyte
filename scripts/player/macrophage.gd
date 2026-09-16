class_name Macrophage
extends CharacterBody2D

## Signals for UI and Game State
signal stats_changed(health: float, max_health: float, satiety: float, max_satiety: float, radius_ratio: float)
signal burst_state_changed(is_active: bool, time_left: float, max_time: float)
signal pathogen_digested(pathogen: Node2D, atp_gained: float)

## Base Stats & CellStats Container
@export var max_health: float = 100.0
@export var base_speed: float = 230.0

@export var base_radius: float = 48.0
var current_radius: float = 48.0

@export var max_satiety: float = 100.0
var satiety: float = 0.0

var health: float = 100.0
var current_speed: float = 230.0

## Deformation Parameters (Native Cell Morphology)
@export var vertex_count: int = 32
@export var deformation_speed: float = 3.6
@export var base_deformation_mag: float = 24.0
var current_deformation_mag: float = 24.0
var noise: FastNoiseLite
var noise_time: float = 0.0

## Respiratory Burst (呼吸爆發)
var is_respiratory_burst: bool = false
var burst_timer: float = 0.0
const BURST_DURATION: float = 6.0

## Digestion tracking
var digested_count: int = 0

## Inertial Nucleus offset
var nucleus_offset: Vector2 = Vector2.ZERO
var nucleus_target_offset: Vector2 = Vector2.ZERO

## Node references
@onready var cytoplasm: Polygon2D = $Cytoplasm
@onready var membrane: Line2D = $Membrane
@onready var nucleus: Polygon2D = $Nucleus
@onready var engulf_collider: CollisionPolygon2D = $EngulfArea/EngulfCollider
@onready var engulf_area: Area2D = $EngulfArea
@onready var acidic_aura: Area2D = $AcidicAura
@onready var aura_collider: CollisionShape2D = $AcidicAura/AuraCollider
@onready var burst_particles: CPUParticles2D = $BurstParticles
@onready var skill_manager: SkillManager = $SkillManager

const CellStats = preload("res://scripts/core/cell_stats.gd")

var stats: CellStats = null

# Cytoplasm coloring
const COLOR_NORMAL: Color = Color(0.18, 0.72, 0.65, 0.62)
const COLOR_BURST: Color = Color(0.96, 0.82, 0.16, 0.82)
const MEMBRANE_NORMAL: Color = Color(0.45, 0.95, 0.85, 0.9)
const MEMBRANE_BURST: Color = Color(1.0, 0.95, 0.4, 1.0)

func _ready() -> void:
	# Ensure CellStats node is initialized
	if has_node("CellStats"):
		stats = get_node("CellStats") as CellStats
	else:
		stats = CellStats.new()
		stats.name = "CellStats"
		add_child(stats)

	stats.set_base("max_health", max_health)
	stats.set_base("move_speed", base_speed)
	health = stats.get_stat("max_health")
	current_speed = stats.get_stat("move_speed")
	current_radius = base_radius * stats.get_stat("area")

	stats.stat_changed.connect(_on_stat_changed)

	# Initialize Noise for organic deformation
	noise = FastNoiseLite.new()
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	noise.seed = randi()
	noise.frequency = 0.65
	noise.fractal_octaves = 2

	# Connect engulfment signals
	if not engulf_area.area_entered.is_connected(_on_engulf_area_entered):
		engulf_area.area_entered.connect(_on_engulf_area_entered)
	if not acidic_aura.area_entered.is_connected(_on_acidic_aura_entered):
		acidic_aura.area_entered.connect(_on_acidic_aura_entered)

	acidic_aura.monitoring = false
	burst_particles.emitting = false

	# Setup nucleus shape
	_setup_nucleus_shape()

	# Initialize Skill System (5 Active + 5 Passive)
	skill_manager.setup(self)

	# Slot 0 Active: ROSTorrentSkill
	var ros_skill = ROSTorrentSkill.new()
	skill_manager.equip_active(ros_skill, 0)

	# Initial deformation tick to populate polygons
	_update_pseudopod_deformation(0.016)

	_emit_stats()

func _physics_process(delta: float) -> void:
	_handle_regen(delta)
	_handle_movement(delta)
	_handle_burst(delta)
	_update_pseudopod_deformation(delta)

	# Delegate active weapon skill ticks
	skill_manager.update_all_skills(delta)

func _handle_regen(delta: float) -> void:
	if stats != null:
		var regen = stats.get_stat("health_regen")
		if regen > 0.0:
			heal(regen * delta)

func _handle_movement(delta: float) -> void:
	var input_vec := Vector2.ZERO
	if Input.is_action_pressed("move_left") or Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_LEFT):
		input_vec.x -= 1.0
	if Input.is_action_pressed("move_right") or Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_RIGHT):
		input_vec.x += 1.0
	if Input.is_action_pressed("move_up") or Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_UP):
		input_vec.y -= 1.0
	if Input.is_action_pressed("move_down") or Input.is_key_pressed(KEY_S) or Input.is_key_pressed(KEY_DOWN):
		input_vec.y += 1.0

	var target_speed = stats.get_stat("move_speed")
	if is_respiratory_burst:
		target_speed *= 2.5

	current_speed = target_speed

	if input_vec != Vector2.ZERO:
		input_vec = input_vec.normalized()
		velocity = velocity.move_toward(input_vec * current_speed, current_speed * 5.0 * delta)
		nucleus_target_offset = -input_vec * (current_radius * 0.28)
	else:
		velocity = velocity.move_toward(Vector2.ZERO, current_speed * 4.0 * delta)
		nucleus_target_offset = Vector2.ZERO

	move_and_slide()

## Organic pseudopod deformation driven by FastNoiseLite & universal 'area' stat
func _update_pseudopod_deformation(delta: float) -> void:
	noise_time += delta * deformation_speed

	var expansion_ratio: float = 1.0 + (satiety / max(1.0, max_satiety)) * 1.5
	var area_scale: float = stats.get_stat("area") if stats != null else 1.0

	var cur_r: float = base_radius * expansion_ratio * area_scale
	current_deformation_mag = base_deformation_mag * expansion_ratio * area_scale

	if is_respiratory_burst:
		cur_r *= 1.15
		current_deformation_mag *= 1.35

	current_radius = cur_r

	var points := PackedVector2Array()
	var angle_step: float = TAU / float(vertex_count)
	var vel: Vector2 = velocity
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

	if cytoplasm:
		cytoplasm.polygon = points
	if membrane:
		var line_points := points.duplicate()
		if line_points.size() > 0:
			line_points.append(points[0])
		membrane.points = line_points
	if engulf_collider:
		engulf_collider.polygon = points

	_update_nucleus(delta)

func _update_nucleus(delta: float) -> void:
	if not nucleus:
		return

	nucleus_offset = nucleus_offset.lerp(nucleus_target_offset, 8.0 * delta)
	nucleus.position = nucleus_offset

	var n_scale: float = 1.0 + (satiety / max(1.0, max_satiety)) * 0.8
	nucleus.scale = Vector2(n_scale, n_scale)

func _setup_nucleus_shape() -> void:
	var n_pts := PackedVector2Array()
	var n_count: int = 16
	var n_radius: float = 18.0
	for i in range(n_count):
		var a: float = i * (TAU / float(n_count))
		var r: float = n_radius * (1.0 + 0.25 * sin(a * 2.0))
		n_pts.append(Vector2(cos(a) * r, sin(a) * r))
	nucleus.polygon = n_pts
	nucleus.color = Color(0.42, 0.22, 0.68, 0.85)

## Engulfment handler
func _on_engulf_area_entered(area: Area2D) -> void:
	var enemy = area.get_parent()
	if enemy and enemy.has_method("be_engulfed"):
		_consume_pathogen(enemy)

func _on_acidic_aura_entered(area: Area2D) -> void:
	if is_respiratory_burst:
		var enemy = area.get_parent()
		if enemy and enemy.has_method("be_engulfed"):
			_consume_pathogen(enemy)

func _consume_pathogen(enemy: Node2D) -> void:
	if not enemy or enemy.is_queued_for_deletion():
		return

	var atp: float = 12.0
	if enemy.has_method("get_atp_value"):
		atp = enemy.get_atp_value()

	enemy.be_engulfed(self)
	digested_count += 1

	if not is_respiratory_burst:
		satiety = clampf(satiety + atp, 0.0, max_satiety)
		if satiety >= max_satiety:
			trigger_respiratory_burst()

	# Macrophage Innate Trait: Heals 0.5% max HP
	var max_hp = stats.get_stat("max_health") if stats != null else 100.0
	heal(max_hp * 0.005)

	pathogen_digested.emit(enemy, atp)
	_emit_stats()

## Respiratory Burst (呼吸爆發)
func trigger_respiratory_burst() -> void:
	if is_respiratory_burst:
		return

	is_respiratory_burst = true
	burst_timer = BURST_DURATION

	current_speed = stats.get_stat("move_speed") * 2.5
	deformation_speed = 6.0

	cytoplasm.color = COLOR_BURST
	membrane.default_color = MEMBRANE_BURST

	acidic_aura.monitoring = true
	burst_particles.emitting = true

	burst_state_changed.emit(true, burst_timer, BURST_DURATION)

func _handle_burst(delta: float) -> void:
	if not is_respiratory_burst:
		return

	burst_timer -= delta
	satiety = clampf((burst_timer / BURST_DURATION) * max_satiety, 0.0, max_satiety)
	burst_state_changed.emit(true, max(0.0, burst_timer), BURST_DURATION)
	_emit_stats()

	if burst_timer <= 0.0:
		_end_respiratory_burst()

func _end_respiratory_burst() -> void:
	is_respiratory_burst = false
	burst_timer = 0.0
	satiety = 0.0
	current_speed = stats.get_stat("move_speed")
	deformation_speed = 3.6

	cytoplasm.color = COLOR_NORMAL
	membrane.default_color = MEMBRANE_NORMAL

	acidic_aura.monitoring = false
	burst_particles.emitting = false

	burst_state_changed.emit(false, 0.0, BURST_DURATION)
	_emit_stats()

func heal(amount: float) -> void:
	var max_hp = stats.get_stat("max_health") if stats != null else 100.0
	health = clampf(health + amount, 0.0, max_hp)
	_emit_stats()

func take_damage(amount: float) -> void:
	var dr = stats.get_damage_reduction_ratio() if stats != null else 0.0
	var final_dmg = amount * (1.0 - dr)
	var max_hp = stats.get_stat("max_health") if stats != null else 100.0
	health = clampf(health - final_dmg, 0.0, max_hp)
	_emit_stats()

func _on_stat_changed(stat_name: String, _val: float) -> void:
	if stat_name == "max_health":
		var max_hp = stats.get_stat("max_health")
		health = clampf(health, 0.0, max_hp)
	_emit_stats()

func _emit_stats() -> void:
	var max_hp = stats.get_stat("max_health") if stats != null else 100.0
	var ratio: float = current_radius / base_radius
	stats_changed.emit(health, max_hp, satiety, max_satiety, ratio)
