class_name Main
extends Node2D

@export var staph_scene: PackedScene = preload("res://scenes/enemies/staph_enemy.tscn")
@export var max_pathogens: int = 35
@export var arena_size: Vector2 = Vector2(2400, 2400)

@onready var player: CharacterBody2D = $Macrophage
@onready var hud: CanvasLayer = $HUD
@onready var enemy_container: Node2D = $EnemyContainer
@onready var camera: Camera2D = $Macrophage/Camera2D

var spawn_timer: float = 0.0

func _ready() -> void:
	player.add_to_group("player")
	if hud.has_method("connect_player"):
		hud.connect_player(player)

	# Initial pathogen wave
	_spawn_initial_wave(24)

func _physics_process(delta: float) -> void:
	spawn_timer += delta
	if spawn_timer >= 1.5:
		spawn_timer = 0.0
		_maintain_population()

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
