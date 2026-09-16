class_name BaseCell
extends CharacterBody2D

## Base class for all Immune Defense Cells in Project: Phagocyte.
## Encapsulates universal stats, physics movement, 32-vertex organic deformation,
## satiety accumulation, digestion, experience progression, and ultimate burst states.

signal stats_changed(health: float, max_health: float, satiety: float, max_satiety: float, radius_ratio: float)
signal burst_state_changed(is_active: bool, time_left: float, max_time: float)
signal pathogen_digested(pathogen: Node2D, atp_gained: float)
signal level_up(new_level: int)
signal exp_changed(current_exp: float, max_exp: float, level: int)

## Level & EXP Progression
var current_level: int = 1
var current_exp: float = 0.0
var exp_to_next_level: float = 30.0

## Base Stats
@export var max_health: float = 100.0
@export var base_speed: float = 230.0
@export var base_radius: float = 48.0
var current_radius: float = 48.0

@export var max_satiety: float = 100.0
var satiety: float = 0.0

var health: float = 100.0
var current_speed: float = 230.0

## Deformation Parameters
@export var vertex_count: int = 32
@export var deformation_speed: float = 3.6
@export var base_deformation_mag: float = 24.0
var current_deformation_mag: float = 24.0
var noise: FastNoiseLite
var noise_time: float = 0.0

## Ultimate Burst
var is_burst: bool = false
var burst_timer: float = 0.0
@export var burst_duration: float = 6.0

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
@onready var burst_particles: CPUParticles2D = $BurstParticles
@onready var skill_manager: SkillManager = $SkillManager

const CellStatsClass = preload("res://scripts/core/cell_stats.gd")
var stats: CellStatsClass = null

func _ready() -> void:
	add_to_group("player")

	# Ensure CellStats container node is initialized
	if has_node("CellStats"):
		stats = get_node("CellStats") as CellStatsClass
	else:
		stats = CellStatsClass.new()
		stats.name = "CellStats"
		add_child(stats)

	_setup_cell_identity()

	stats.set_base("max_health", max_health)
	stats.set_base("move_speed", base_speed)
	health = stats.get_stat("max_health")
	current_speed = stats.get_stat("move_speed")
	current_radius = base_radius * stats.get_stat("area")

	stats.stat_changed.connect(_on_stat_changed)

	# Connect engulfment signals
	if engulf_area and not engulf_area.area_entered.is_connected(_on_engulf_area_entered):
		engulf_area.area_entered.connect(_on_engulf_area_entered)

	if burst_particles:
		burst_particles.emitting = false

	# Setup nucleus shape
	_setup_nucleus_shape()

	# Initialize Skill System (5 Active + 5 Passive)
	if skill_manager:
		skill_manager.setup(self)
		_setup_initial_skills()

	# Initial deformation tick
	_update_pseudopod_deformation(0.016)
	_emit_stats()

func _physics_process(delta: float) -> void:
	_handle_regen(delta)
	_handle_movement(delta)
	_handle_burst(delta)
	_update_pseudopod_deformation(delta)

	if skill_manager:
		skill_manager.update_all_skills(delta)

## Virtual method: Cell subclasses override to configure noise, speed, and stat bonuses
func _setup_cell_identity() -> void:
	noise = FastNoiseLite.new()
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	noise.seed = randi()
	noise.frequency = 0.65
	noise.fractal_octaves = 2

## Virtual method: Cell subclasses override to define nucleus geometry and colors
func _setup_nucleus_shape() -> void:
	var n_pts := PackedVector2Array()
	var n_count: int = 16
	var n_radius: float = 16.0
	for i in range(n_count):
		var a: float = i * (TAU / float(n_count))
		n_pts.append(Vector2(cos(a) * n_radius, sin(a) * n_radius))
	if nucleus:
		nucleus.polygon = n_pts
		nucleus.color = Color(0.4, 0.2, 0.6, 0.85)

## Virtual method: Cell subclasses override to equip their starting weapon
func _setup_initial_skills() -> void:
	pass

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
	if is_burst:
		target_speed = _get_burst_move_speed(target_speed)

	current_speed = target_speed

	if input_vec != Vector2.ZERO:
		input_vec = input_vec.normalized()
		velocity = velocity.move_toward(input_vec * current_speed, current_speed * 5.0 * delta)
		nucleus_target_offset = -input_vec * (current_radius * 0.28)
	else:
		velocity = velocity.move_toward(Vector2.ZERO, current_speed * 4.0 * delta)
		nucleus_target_offset = Vector2.ZERO

	move_and_slide()

func _get_burst_move_speed(base_sp: float) -> float:
	return base_sp * 2.5

func _update_pseudopod_deformation(delta: float) -> void:
	noise_time += delta * deformation_speed

	var expansion_ratio: float = 1.0 + (satiety / max(1.0, max_satiety)) * 1.5
	var area_scale: float = stats.get_stat("area") if stats != null else 1.0

	var cur_r: float = base_radius * expansion_ratio * area_scale
	current_deformation_mag = base_deformation_mag * expansion_ratio * area_scale

	if is_burst:
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
		var n_val: float = noise.get_noise_3d(nx, ny, noise_time) if noise else 0.0

		var forward_bias: float = 0.0
		if move_dir != Vector2.ZERO:
			var dot: float = max(0.0, dir.dot(move_dir))
			forward_bias = dot * (current_deformation_mag * 0.6)

		var r: float = cur_r + (n_val * current_deformation_mag) + forward_bias
		points.append(dir * max(12.0, r))

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

func _on_engulf_area_entered(area: Area2D) -> void:
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

	if not is_burst:
		satiety = clampf(satiety + atp, 0.0, max_satiety)
		if satiety >= max_satiety:
			trigger_burst()

	_on_pathogen_consumed(enemy, atp)

	var growth_mult = stats.get_stat("growth") if stats != null else 1.0
	add_exp(atp * growth_mult)

	pathogen_digested.emit(enemy, atp)
	_emit_stats()

## Virtual hook: subclasses implement specific digestion passives
func _on_pathogen_consumed(_enemy: Node2D, _atp: float) -> void:
	pass

func add_exp(amount: float) -> void:
	current_exp += amount
	while current_exp >= exp_to_next_level:
		current_exp -= exp_to_next_level
		current_level += 1
		exp_to_next_level = exp_to_next_level * 1.35 + 15.0
		level_up.emit(current_level)
	exp_changed.emit(current_exp, exp_to_next_level, current_level)

func trigger_burst() -> void:
	if is_burst:
		return
	is_burst = true
	burst_timer = burst_duration
	var sp = stats.get_stat("move_speed") if stats != null else base_speed
	current_speed = _get_burst_move_speed(sp)
	if burst_particles:
		burst_particles.emitting = true
	_apply_burst_visuals(true)
	burst_state_changed.emit(true, burst_timer, burst_duration)

func _handle_burst(delta: float) -> void:
	if not is_burst:
		return
	burst_timer -= delta
	satiety = clampf((burst_timer / burst_duration) * max_satiety, 0.0, max_satiety)
	burst_state_changed.emit(true, max(0.0, burst_timer), burst_duration)
	_emit_stats()

	if burst_timer <= 0.0:
		_end_burst()

func _end_burst() -> void:
	is_burst = false
	burst_timer = 0.0
	satiety = 0.0
	current_speed = stats.get_stat("move_speed")
	if burst_particles:
		burst_particles.emitting = false
	_apply_burst_visuals(false)
	burst_state_changed.emit(false, 0.0, burst_duration)
	_emit_stats()

## Virtual hook: subclass visual color switches for burst
func _apply_burst_visuals(_active: bool) -> void:
	pass

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
