class_name StaphEnemy
extends Node2D

## Signals
signal digested(staph: Node2D)

## Attributes
@export var atp_value: float = 12.0
@export var float_speed: float = 35.0
@export var drift_frequency: float = 1.2

var is_being_eaten: bool = false
var velocity: Vector2 = Vector2.ZERO
var drift_timer: float = 0.0
var wander_dir: Vector2 = Vector2.ZERO

# Cluster circle offsets for golden grape-like appearance
var cluster_spheres: Array[Dictionary] = []

@onready var hit_area: Area2D = $HitArea
@onready var collision_shape: CollisionShape2D = $HitArea/CollisionShape2D

func _ready() -> void:
	add_to_group("pathogens")
	drift_timer = randf() * 5.0
	wander_dir = Vector2.from_angle(randf() * TAU)
	
	# Generate 3-5 golden cocci spheres to form a staph cluster
	var count = randi_range(3, 5)
	var base_golden = Color(0.95, 0.78, 0.18, 0.95)
	for i in range(count):
		var offset = Vector2(randf_range(-10, 10), randf_range(-10, 10))
		var radius = randf_range(6.5, 9.5)
		var color = base_golden.lightened(randf_range(-0.1, 0.1))
		cluster_spheres.append({
			"offset": offset,
			"radius": radius,
			"color": color
		})
	queue_redraw()

func _physics_process(delta: float) -> void:
	if is_being_eaten:
		return

	drift_timer += delta
	# Brownian drifting in fluid
	if drift_timer > 2.5:
		drift_timer = 0.0
		wander_dir = (wander_dir + Vector2.from_angle(randf() * TAU) * 0.7).normalized()

	velocity = velocity.lerp(wander_dir * float_speed, 2.0 * delta)
	position += velocity * delta

func _draw() -> void:
	# Draw golden cocci cluster
	for s in cluster_spheres:
		var pos: Vector2 = s["offset"]
		var rad: float = s["radius"]
		var col: Color = s["color"]
		# Draw outer cell wall / membrane
		draw_circle(pos, rad + 1.2, Color(0.65, 0.45, 0.05, 0.9))
		# Draw golden cytoplasm
		draw_circle(pos, rad, col)
		# Draw glossy specular highlight (electron microscope vibe)
		draw_circle(pos + Vector2(-rad * 0.3, -rad * 0.3), rad * 0.32, Color(1.0, 1.0, 0.8, 0.8))

func get_atp_value() -> float:
	return atp_value

func be_engulfed(predator: Node2D) -> void:
	if is_being_eaten:
		return
	is_being_eaten = true

	# Disable collision immediately
	hit_area.set_deferred("monitoring", false)
	hit_area.set_deferred("monitorable", false)
	collision_shape.set_deferred("disabled", true)

	# Ingestion visual tween: shrink and get pulled into predator center
	var tween = create_tween().set_parallel(true)
	tween.tween_property(self, "global_position", predator.global_position, 0.25).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
	tween.tween_property(self, "scale", Vector2.ZERO, 0.25).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_IN)
	tween.tween_property(self, "modulate:a", 0.0, 0.25)
	tween.chain().tween_callback(func():
		digested.emit(self)
		queue_free()
	)
