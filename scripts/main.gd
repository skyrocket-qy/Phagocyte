class_name Main
extends Node2D

const GM = preload("res://scripts/core/game_manager.gd")

@export var staph_scene: PackedScene = preload("res://scenes/enemies/staph_enemy.tscn")
@export var max_pathogens: int = 35
@export var arena_size: Vector2 = Vector2(2400, 2400)

@onready var player: CharacterBody2D = $Macrophage
@onready var hud: CanvasLayer = $HUD
@onready var enemy_container: Node2D = $EnemyContainer
@onready var camera: Camera2D = $Macrophage/Camera2D

@onready var arena_bg: ColorRect = $Background/ArenaBG
@onready var arena_borders: Line2D = $Background/ArenaBorders

var spawn_timer: float = 0.0
var environment_time: float = 0.0
var map_id: String = "acute_wound"

func _ready() -> void:
	player.add_to_group("player")
	if hud.has_method("connect_player"):
		hud.connect_player(player)

	# Read map configuration from GM
	map_id = GM.selected_map
	_configure_map_environment()

	# Initial pathogen wave
	_spawn_initial_wave(24)

func _configure_map_environment() -> void:
	if map_id == "alveolar_space":
		arena_bg.color = Color(0.03, 0.09, 0.12, 1.0)
		arena_borders.default_color = Color(0.2, 0.65, 0.7, 0.7)
	else:
		# acute_wound default
		arena_bg.color = Color(0.05, 0.07, 0.11, 1.0)
		arena_borders.default_color = Color(0.35, 0.45, 0.6, 0.65)

func _physics_process(delta: float) -> void:
	environment_time += delta

	# Map mechanics
	_process_map_mechanics(delta)

	spawn_timer += delta
	if spawn_timer >= 1.5:
		spawn_timer = 0.0
		_maintain_population()

func _process_map_mechanics(delta: float) -> void:
	if map_id == "alveolar_space":
		# SPEC Section 5: Periodic breathing airflow thrust in lung alveoli
		var breath_force = sin(environment_time * 1.2) * 28.0
		var breath_vec = Vector2(breath_force, sin(environment_time * 0.6) * 12.0)
		# Gently pushes all free pathogens and player with fluid current
		for enemy in enemy_container.get_children():
			if enemy is Node2D and not enemy.get("is_being_eaten"):
				enemy.position += breath_vec * delta * 0.6
	elif map_id == "acute_wound":
		# SPEC Section 5: Directional tissue fluid suction towards wound tear
		var suction_vec = Vector2(16.0, 10.0)
		for enemy in enemy_container.get_children():
			if enemy is Node2D and not enemy.get("is_being_eaten"):
				enemy.position += suction_vec * delta * 0.4

func _spawn_initial_wave(count: int) -> void:
	for i in range(count):
		_spawn_staph_around_player(randf_range(200.0, 950.0))

func _maintain_population() -> void:
	var current_count = enemy_container.get_child_count()
	if current_count < max_pathogens:
		var spawn_batch = mini(5, max_pathogens - current_count)
		for i in range(spawn_batch):
			_spawn_staph_around_player(randf_range(450.0, 1100.0))

func _spawn_staph_around_player(dist: float) -> void:
	var angle = randf() * TAU
	var offset = Vector2(cos(angle), sin(angle)) * dist
	var spawn_pos = player.global_position + offset

	# Clamp within arena boundaries (-arena_size/2 to +arena_size/2)
	var half_w = (arena_size.x * 0.5) - 60.0
	var half_h = (arena_size.y * 0.5) - 60.0
	spawn_pos.x = clampf(spawn_pos.x, -half_w, half_w)
	spawn_pos.y = clampf(spawn_pos.y, -half_h, half_h)

	var staph = staph_scene.instantiate()
	staph.global_position = spawn_pos
	enemy_container.add_child(staph)
