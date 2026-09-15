class_name ROSJet
extends Area2D

var direction: Vector2 = Vector2.RIGHT
var speed: float = 520.0
var lifetime: float = 0.9
var predator: Node2D = null

func _ready() -> void:
	collision_layer = 0
	collision_mask = 2 # Pathogens
	area_entered.connect(_on_area_entered)
	queue_redraw()

func setup(p_predator: Node2D, p_pos: Vector2, p_dir: Vector2) -> void:
	predator = p_predator
	global_position = p_pos
	direction = p_dir.normalized()
	rotation = direction.angle()

func _physics_process(delta: float) -> void:
	position += direction * speed * delta
	lifetime -= delta
	if lifetime <= 0.0:
		queue_free()

func _draw() -> void:
	# High-pressure peroxide jet stream beam
	draw_line(Vector2(-24, 0), Vector2(24, 0), Color(0.9, 1.0, 1.0, 0.95), 8.0)
	draw_line(Vector2(-30, 0), Vector2(30, 0), Color(0.3, 0.9, 1.0, 0.6), 14.0)
	draw_circle(Vector2(20, 0), 6.0, Color(1, 1, 1, 0.9))

func _on_area_entered(area: Area2D) -> void:
	var enemy = area.get_parent()
	if enemy and enemy.has_method("be_engulfed"):
		if predator and is_instance_valid(predator) and predator.has_method("_consume_pathogen"):
			predator._consume_pathogen(enemy)
		else:
			enemy.be_engulfed(predator)
