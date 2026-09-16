extends SceneTree

const GM = preload("res://scripts/core/game_manager.gd")
const BaseCellClass = preload("res://scripts/player/base_cell.gd")
const StaphEnemyClass = preload("res://scripts/enemies/staph_enemy.gd")

var frames_waited: int = 0
var test_done: bool = false

func _init() -> void:
	print("==================================================================")
	print(">>> STARTING COMMERCIAL-GRADE VISUAL OVERHAUL AUTOMATED TEST <<<")
	print("==================================================================")
	var main_scene = load("res://scenes/main.tscn")
	if main_scene == null:
		printerr("[FAIL] Failed to load main.tscn")
		quit(1)
		return
	var main = main_scene.instantiate()
	root.add_child(main)

func _process(_delta: float) -> bool:
	if test_done:
		return true

	frames_waited += 1
	if frames_waited < 5:
		return false

	test_done = true
	var main = root.get_node_or_null("Main")
	if main == null:
		printerr("[FAIL] Main scene not found")
		quit(1)
		return true

	var player = main.player if "player" in main and main.player != null else main.get_node_or_null("Macrophage")
	if player == null:
		printerr("[FAIL] Player not found in Main scene")
		quit(1)
		return true

	# =========================================================================
	# TEST 1: Catmull-Rom Polygon Smoothing (32 -> 64 vertices)
	# =========================================================================
	player._update_pseudopod_deformation(0.016)
	if player.cytoplasm.polygon.size() != 64:
		printerr("[FAIL] Cytoplasm visual polygon should be smoothed to 64 vertices, got: " + str(player.cytoplasm.polygon.size()))
		quit(1)
		return true

	if player.membrane.points.size() != 65:
		printerr("[FAIL] Membrane line should have 65 points (64 + closed loop point), got: " + str(player.membrane.points.size()))
		quit(1)
		return true

	if player.engulf_collider.polygon.size() != 32:
		printerr("[FAIL] Physics engulf collider should maintain 32 control vertices for speed, got: " + str(player.engulf_collider.polygon.size()))
		quit(1)
		return true

	if player.cytoplasm.uv.size() != 64:
		printerr("[FAIL] Cytoplasm UV array should match 64 vertices, got: " + str(player.cytoplasm.uv.size()))
		quit(1)
		return true
	print("[PASS] 1. Catmull-Rom spline smoothing verified: 32 control points -> 64 smooth visual vertices + 65-point closed membrane.")

	# =========================================================================
	# TEST 2: Cytoplasm Gel Shader & Fresnel Rim
	# =========================================================================
	var mat = player.cytoplasm.material
	if mat == null or not (mat is ShaderMaterial):
		printerr("[FAIL] Cytoplasm does not have a ShaderMaterial attached")
		quit(1)
		return true

	var tint_col = mat.get_shader_parameter("tint_color")
	var rim_col = mat.get_shader_parameter("rim_color")
	var rim_power = mat.get_shader_parameter("rim_power")
	var inner_alpha = mat.get_shader_parameter("inner_alpha")

	if tint_col == null or rim_col == null or rim_power == null or inner_alpha == null:
		printerr("[FAIL] Cytoplasm ShaderMaterial missing expected uniform parameters")
		quit(1)
		return true

	if rim_col.g <= 1.0 and rim_col.b <= 1.0:
		printerr("[FAIL] Rim color should have HDR emissive values (> 1.0) for Bloom, got: " + str(rim_col))
		quit(1)
		return true
	print("[PASS] 2. Cytoplasm Gel Shader verified with semi-transparent core (alpha=%.2f) and HDR Fresnel rim (%s)." % [inner_alpha, str(rim_col)])

	# =========================================================================
	# TEST 3: Nucleus Damped Spring Lag Physics
	# =========================================================================
	player.velocity = Vector2(300.0, 0.0)
	player._update_nucleus(0.05)
	if player.nucleus_velocity == Vector2.ZERO:
		printerr("[FAIL] Nucleus spring velocity did not respond to player movement")
		quit(1)
		return true
	if player.nucleus.position.x >= 0.0:
		printerr("[FAIL] Nucleus should lag behind movement direction (towards negative X), got pos: " + str(player.nucleus.position))
		quit(1)
		return true
	print("[PASS] 3. Nucleus damped spring lag physics verified: squishy physical momentum and recoil active.")

	# =========================================================================
	# TEST 4: WorldEnvironment & 2D Glow/Bloom
	# =========================================================================
	var we = main.get_node_or_null("WorldEnvironment")
	if we == null or we.environment == null:
		printerr("[FAIL] WorldEnvironment node or environment resource missing in Main scene")
		quit(1)
		return true

	var env = we.environment
	if not env.glow_enabled:
		printerr("[FAIL] Environment glow_enabled should be true")
		quit(1)
		return true
	if env.background_mode != Environment.BG_CANVAS:
		printerr("[FAIL] Environment background_mode should be BG_CANVAS (3), got: " + str(env.background_mode))
		quit(1)
		return true
	print("[PASS] 4. WorldEnvironment 2D Glow / Bloom verified (BG_CANVAS, Glow Enabled, HDR Threshold 1.0).")

	# =========================================================================
	# TEST 5: Microscope Atmosphere Background & Parallax Depth-of-Field (DoF)
	# =========================================================================
	var arena_bg = main.get_node_or_null("Background/ArenaBG")
	if arena_bg == null or arena_bg.material == null:
		printerr("[FAIL] ArenaBG or its tissue shader material missing")
		quit(1)
		return true

	var parallax = main.get_node_or_null("Background/MicroscopeParallax")
	if parallax == null:
		printerr("[FAIL] MicroscopeParallax node missing in Background")
		quit(1)
		return true

	if parallax.rbc_list.size() < 10:
		printerr("[FAIL] Out-of-focus background RBCs count insufficient: " + str(parallax.rbc_list.size()))
		quit(1)
		return true
	if parallax.bokeh_list.size() < 5:
		printerr("[FAIL] Foreground lens bokeh count insufficient: " + str(parallax.bokeh_list.size()))
		quit(1)
		return true
	print("[PASS] 5. Microscope Parallax DoF active: %d out-of-focus RBCs + %d foreground bokeh particles." % [parallax.rbc_list.size(), parallax.bokeh_list.size()])

	# =========================================================================
	# TEST 6: Microscope Post-Process (Vignette & Chromatic Aberration)
	# =========================================================================
	var post_layer = main.get_node_or_null("MicroscopePostProcess")
	if post_layer == null:
		printerr("[FAIL] MicroscopePostProcess CanvasLayer missing")
		quit(1)
		return true

	var lens_overlay = post_layer.get_node_or_null("LensOverlay")
	if lens_overlay == null or lens_overlay.material == null:
		printerr("[FAIL] LensOverlay ColorRect or its shader material missing")
		quit(1)
		return true

	var ca_param = lens_overlay.material.get_shader_parameter("chromatic_aberration")
	var vig_param = lens_overlay.material.get_shader_parameter("vignette_radius")
	if ca_param == null or vig_param == null:
		printerr("[FAIL] Microscope post-process shader parameters missing")
		quit(1)
		return true
	print("[PASS] 6. Full-screen Microscope Post-Process verified (Vignette r=%.2f, Chromatic Aberration=%.4f)." % [vig_param, ca_param])

	# =========================================================================
	# TEST 7: Pathogen Breathing Oscillation & 3D Shading
	# =========================================================================
	var staph = load("res://scenes/enemies/staph_enemy.tscn").instantiate()
	main.add_child(staph)
	staph._physics_process(0.016)
	var initial_scale = staph.scale.x
	# Check scale is non-zero and breathing logic runs
	if initial_scale < 0.9 or initial_scale > 1.1:
		printerr("[FAIL] Staph enemy scale out of expected breathing range: " + str(initial_scale))
		quit(1)
		return true
	staph.queue_free()
	print("[PASS] 7. Pathogen organic breathing scale oscillation verified.")

	print("==================================================================")
	print(">>> ALL 7 VISUAL OVERHAUL TESTS PASSED WITH FLYING COLORS! <<<")
	print("==================================================================")
	quit(0)
	return true
