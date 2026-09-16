class_name MicroscopeParallax
extends Node2D

## Procedural Microscope Optical Depth-of-Field (DoF) System.
## Renders out-of-focus background Erythrocytes (RBCs) and foreground lens bokeh.

const RBC_COUNT: int = 14
const BOKEH_COUNT: int = 8
const ARENA_EXTENTS: float = 2600.0

var camera_ref: Camera2D = null

var rbc_list: Array[Dictionary] = []
var bokeh_list: Array[Dictionary] = []

func _ready() -> void:
	z_index = -5
	_spawn_erythrocytes()
	_spawn_bokeh()

func _spawn_erythrocytes() -> void:
	rbc_list.clear()
	for i in range(RBC_COUNT):
		var pos = Vector2(
			randf_range(-ARENA_EXTENTS, ARENA_EXTENTS),
			randf_range(-ARENA_EXTENTS, ARENA_EXTENTS)
		)
		var rad = randf_range(55.0, 100.0)
		var rot = randf() * TAU
		var rot_speed = randf_range(-0.15, 0.15)
		var drift = Vector2(randf_range(15.0, 35.0), randf_range(-10.0, 10.0))
		var color = Color(0.72, 0.10, 0.14, randf_range(0.22, 0.38))
		rbc_list.append({
			"pos": pos,
			"radius": rad,
			"rotation": rot,
			"rot_speed": rot_speed,
			"drift": drift,
			"color": color
		})

func _spawn_bokeh() -> void:
	bokeh_list.clear()
	for i in range(BOKEH_COUNT):
		var pos = Vector2(
			randf_range(-ARENA_EXTENTS, ARENA_EXTENTS),
			randf_range(-ARENA_EXTENTS, ARENA_EXTENTS)
		)
		var rad = randf_range(70.0, 140.0)
		var drift = Vector2(randf_range(-25.0, 25.0), randf_range(10.0, 40.0))
		var color = Color(0.4, 0.8, 1.0, randf_range(0.06, 0.15))
		bokeh_list.append({
			"pos": pos,
			"radius": rad,
			"drift": drift,
			"color": color
		})

func _process(delta: float) -> void:
	if not camera_ref or not is_instance_valid(camera_ref):
		var vp = get_viewport()
		if vp:
			camera_ref = vp.get_camera_2d()

	for rbc in rbc_list:
		rbc["pos"] += rbc["drift"] * delta
		rbc["rotation"] += rbc["rot_speed"] * delta
		if rbc["pos"].x > ARENA_EXTENTS:
			rbc["pos"].x = -ARENA_EXTENTS
		elif rbc["pos"].x < -ARENA_EXTENTS:
			rbc["pos"].x = ARENA_EXTENTS
		if rbc["pos"].y > ARENA_EXTENTS:
			rbc["pos"].y = -ARENA_EXTENTS
		elif rbc["pos"].y < -ARENA_EXTENTS:
			rbc["pos"].y = ARENA_EXTENTS

	for b in bokeh_list:
		b["pos"] += b["drift"] * delta
		if b["pos"].x > ARENA_EXTENTS:
			b["pos"].x = -ARENA_EXTENTS
		elif b["pos"].x < -ARENA_EXTENTS:
			b["pos"].x = ARENA_EXTENTS
		if b["pos"].y > ARENA_EXTENTS:
			b["pos"].y = -ARENA_EXTENTS
		elif b["pos"].y < -ARENA_EXTENTS:
			b["pos"].y = ARENA_EXTENTS

	queue_redraw()

func _draw() -> void:
	var cam_pos = camera_ref.global_position if camera_ref else Vector2.ZERO

	# 1. Deep Parallax Erythrocytes (moves at 0.25x camera speed)
	var rbc_parallax_offset = cam_pos * (1.0 - 0.25)
	for rbc in rbc_list:
		var draw_pos = rbc["pos"] + rbc_parallax_offset
		_draw_blurred_erythrocyte(draw_pos, rbc["radius"], rbc["rotation"], rbc["color"])

	# 2. Foreground Bokeh (moves at 1.45x camera speed)
	var bokeh_parallax_offset = cam_pos * (1.0 - 1.45)
	for b in bokeh_list:
		var draw_pos = b["pos"] + bokeh_parallax_offset
		_draw_blurred_bokeh(draw_pos, b["radius"], b["color"])

func _draw_blurred_erythrocyte(pos: Vector2, radius: float, rot: float, col: Color) -> void:
	var layers = 5
	for l in range(layers, 0, -1):
		var r = radius * (0.55 + 0.45 * (float(l) / float(layers)))
		var alpha_mult = 0.22 * (1.0 - float(l - 1) / float(layers))
		var c = Color(col.r, col.g, col.b, col.a * alpha_mult)
		var scale_y = 0.82
		draw_set_transform(pos, rot, Vector2(1.0, scale_y))
		draw_circle(Vector2.ZERO, r, c)
		draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)

	draw_set_transform(pos, rot, Vector2(1.0, 0.82))
	var inner_col = Color(col.r * 0.4, col.g * 0.1, col.b * 0.1, col.a * 0.2)
	draw_circle(Vector2.ZERO, radius * 0.32, inner_col)
	draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)

func _draw_blurred_bokeh(pos: Vector2, radius: float, col: Color) -> void:
	var layers = 4
	for l in range(layers, 0, -1):
		var r = radius * (float(l) / float(layers))
		var alpha = col.a * (0.28 * (1.0 - float(l - 1) / float(layers)))
		draw_circle(pos, r, Color(col.r, col.g, col.b, alpha))
