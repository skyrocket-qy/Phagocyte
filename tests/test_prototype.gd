extends SceneTree

var frames_waited: int = 0
var test_done: bool = false

func _init() -> void:
	print("--- BEGINNING PROTOTYPE AUTOMATED VERIFICATION ---")
	var main_scene = load("res://scenes/main.tscn")
	if main_scene == null:
		printerr("Failed to load main.tscn")
		quit(1)
		return
	var main = main_scene.instantiate()
	root.add_child(main)

func _process(_delta: float) -> bool:
	if test_done:
		return true

	frames_waited += 1
	# Wait 2 frames for all _ready calls to propagate through the scene tree
	if frames_waited < 3:
		return false

	test_done = true
	var main = root.get_node_or_null("Main")
	if main == null:
		printerr("Main not found")
		quit(1)
		return true

	var player = main.get_node_or_null("Macrophage")
	if player == null:
		printerr("Macrophage not found")
		quit(1)
		return true

	var enemy_container = main.get_node_or_null("EnemyContainer")
	if enemy_container == null:
		printerr("EnemyContainer not found")
		quit(1)
		return true

	# 1. Verify 32 vertices deformation & collision sync
	player._update_pseudopod_deformation(0.016)
	if player.cytoplasm.polygon.size() != 32 or player.engulf_collider.polygon.size() != 32:
		printerr("Deformation points mismatch: " + str(player.cytoplasm.polygon.size()))
		quit(1)
		return true
	print("[PASS] Initial 32-vertex pseudopod deformation & collision sync verified.")

	# 2. Check initial stats
	if player.health != 100.0 or player.satiety != 0.0 or player.current_radius != player.base_radius:
		printerr("Initial stats mismatch")
		quit(1)
		return true
	print("[PASS] Macrophage initial stats verified.")

	# 3. Test Ingestion Loop
	var initial_hp = 80.0
	player.health = initial_hp

	var staph_scene = load("res://scenes/enemies/staph_enemy.tscn")
	var staph = staph_scene.instantiate()
	staph.global_position = player.global_position
	enemy_container.add_child(staph)

	# Simulate engulfment
	player._consume_pathogen(staph)

	if player.satiety <= 0.0:
		printerr("Satiety did not increase")
		quit(1)
		return true
	if player.health <= initial_hp:
		printerr("Macrophage Instinct failed to heal")
		quit(1)
		return true
	if player.digested_count != 1:
		printerr("Digested count did not increment")
		quit(1)
		return true
	print("[PASS] Pathogen engulfment, satiety accumulation, and passive healing verified.")

	# 4. Test Satiety Growth (up to 2.5x)
	player.satiety = 100.0
	player._update_pseudopod_deformation(0.016)
	var expansion_ratio = player.current_radius / player.base_radius
	if expansion_ratio < 2.4:
		printerr("Cell did not expand to ~2.5x: " + str(expansion_ratio))
		quit(1)
		return true
	print("[PASS] Dynamic cell radius expansion (approx 2.5x) verified: %.2fx" % expansion_ratio)

	# 5. Test Respiratory Burst
	player.trigger_respiratory_burst()
	if not player.is_respiratory_burst or player.current_speed < player.base_speed * 2.4:
		printerr("Respiratory Burst failed to activate properly")
		quit(1)
		return true
	if not player.acidic_aura.monitoring:
		printerr("Acidic aura failed to activate")
		quit(1)
		return true
	print("[PASS] Respiratory Burst (+150% speed, acidic aura) verified.")

	print("--- ALL PROTOTYPE VERIFICATION TESTS PASSED SUCCESSFULLY! ---")
	quit(0)
	return true
