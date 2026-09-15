class_name Macrophage
extends CharacterBody2D

## Signals for UI and Game State
signal stats_changed(health: float, max_health: float, satiety: float, max_satiety: float, radius_ratio: float)
signal burst_state_changed(is_active: bool, time_left: float, max_time: float)
signal pathogen_digested(pathogen: Node2D, atp_gained: float)

## Base Stats
@export var max_health: float = 100.0
var health: float = 100.0

@export var base_speed: float = 230.0
var current_speed: float = 230.0

# Base radius with Macrophage passive (+40% engulfment range)
@export var base_radius: float = 63.0
var current_radius: float = 63.0

@export var max_satiety: float = 100.0
var satiety: float = 0.0

## Deformation Parameters (Delegated to MacrophageDeformationSkill)
@export var deformation_speed: float = 3.6
var current_deformation_mag: float = 30.0

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

# Cytoplasm coloring
const COLOR_NORMAL: Color = Color(0.18, 0.72, 0.65, 0.62)
const COLOR_BURST: Color = Color(0.96, 0.82, 0.16, 0.82)
const MEMBRANE_NORMAL: Color = Color(0.45, 0.95, 0.85, 0.9)
const MEMBRANE_BURST: Color = Color(1.0, 0.95, 0.4, 1.0)

func _ready() -> void:
	health = max_health
	current_speed = base_speed
	current_radius = base_radius

	# Connect engulfment signals
	if not engulf_area.area_entered.is_connected(_on_engulf_area_entered):
		engulf_area.area_entered.connect(_on_engulf_area_entered)
	if not acidic_aura.area_entered.is_connected(_on_acidic_aura_entered):
		acidic_aura.area_entered.connect(_on_acidic_aura_entered)

	acidic_aura.monitoring = false
	burst_particles.emitting = false

	# Setup nucleus shape
	_setup_nucleus_shape()

	# Initialize 6-Slot Skill System
	skill_manager.setup(self)

	# Slot 1: Macrophage Exclusive Innate Deformation Skill
	var deform_skill = MacrophageDeformationSkill.new()
	skill_manager.equip_skill(deform_skill, 0)

	# Slot 2: Active Cytokine Skill - ROS Torrent (活性氧射流)
	var ros_skill = ROSTorrentSkill.new()
	skill_manager.equip_skill(ros_skill, 1)

	# Run initial deformation tick so polygons are immediately populated
	_update_pseudopod_deformation(0.016)

	_emit_stats()

func _physics_process(delta: float) -> void:
	_handle_movement(delta)
	_handle_burst(delta)

	# Delegate skill execution (drives deformation, weapons, passive ticks)
	skill_manager.update_all_skills(delta)

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

	if input_vec != Vector2.ZERO:
		input_vec = input_vec.normalized()
		velocity = velocity.move_toward(input_vec * current_speed, current_speed * 5.0 * delta)
		nucleus_target_offset = -input_vec * (current_radius * 0.28)
	else:
		velocity = velocity.move_toward(Vector2.ZERO, current_speed * 4.0 * delta)
		nucleus_target_offset = Vector2.ZERO

	move_and_slide()

## Delegator for deformation updates
func _update_pseudopod_deformation(delta: float) -> void:
	var deform_skill = skill_manager.get_slot(0)
	if deform_skill and deform_skill.has_method("_update_pseudopod_deformation"):
		deform_skill._update_pseudopod_deformation(delta)
		deform_skill._update_nucleus(delta)

func _setup_nucleus_shape() -> void:
	var n_pts := PackedVector2Array()
	var n_count: int = 16
	var n_radius: float = 20.0
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

	# Macrophage Passive: Heals 0.5% max HP
	heal(max_health * 0.005)

	pathogen_digested.emit(enemy, atp)
	_emit_stats()

## Respiratory Burst (呼吸爆發)
func trigger_respiratory_burst() -> void:
	if is_respiratory_burst:
		return

	is_respiratory_burst = true
	burst_timer = BURST_DURATION

	current_speed = base_speed * 2.5
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
	current_speed = base_speed
	deformation_speed = 3.6

	cytoplasm.color = COLOR_NORMAL
	membrane.default_color = MEMBRANE_NORMAL

	acidic_aura.monitoring = false
	burst_particles.emitting = false

	burst_state_changed.emit(false, 0.0, BURST_DURATION)
	_emit_stats()

func heal(amount: float) -> void:
	health = clampf(health + amount, 0.0, max_health)
	_emit_stats()

func take_damage(amount: float) -> void:
	health = clampf(health - amount, 0.0, max_health)
	_emit_stats()

func _emit_stats() -> void:
	var ratio: float = current_radius / base_radius
	stats_changed.emit(health, max_health, satiety, max_satiety, ratio)
