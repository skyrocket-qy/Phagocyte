extends SceneTree

const GameManagerScript = preload("res://scripts/core/game_manager.gd")
var game_manager: Node = null

var frames_waited: int = 0
var test_done: bool = false

func _init() -> void:
	print("--- BEGINNING MENU & SELECTION FLOW AUTOMATED TEST ---")
	
	# Instantiate GameManager as autoload mock in SceneTree
	game_manager = GameManagerScript.new()
	game_manager.name = "GameManager"
	root.add_child(game_manager)

	var menu_scene = load("res://scenes/ui/main_menu.tscn")
	if menu_scene == null:
		printerr("Failed to load main_menu.tscn")
		quit(1)
		return
	var menu = menu_scene.instantiate()
	root.add_child(menu)

func _process(_delta: float) -> bool:
	if test_done:
		return true

	frames_waited += 1
	if frames_waited < 3:
		return false

	test_done = true
	var menu = root.get_node_or_null("MainMenu")
	if menu == null:
		printerr("MainMenu node not found")
		quit(1)
		return true

	# 1. Test Title View initial state
	if not menu.title_view.visible or menu.class_view.visible or menu.map_view.visible:
		printerr("Initial view visibility incorrect")
		quit(1)
		return true
	print("[PASS] TitleView initially visible, class and map views hidden.")

	# 2. Test Transition to Class Selection
	menu._on_start_pressed()
	if menu.title_view.visible or not menu.class_view.visible:
		printerr("Class view transition failed")
		quit(1)
		return true
	print("[PASS] Transition to ClassView verified.")

	const AM = preload("res://scripts/core/achievement_manager.gd")
	AM.reset_all()
	menu._setup_class_buttons()

	# 3. Test Initial Lock States: Macrophage unlocked, others locked
	menu._select_class("macrophage")
	if menu.class_confirm_btn.disabled:
		printerr("Macrophage should be unlocked by default")
		quit(1)
		return true

	for locked_id in ["ctl", "neutrophil", "b_cell", "dendritic"]:
		menu._select_class(locked_id)
		if not menu.class_confirm_btn.disabled:
			printerr("Class '%s' should be locked initially" % locked_id)
			quit(1)
			return true
	print("[PASS] Initial lock state enforced: Macrophage unlocked, other 4 cells locked.")

	# Unlock remaining cells via achievements and verify they become confirmable
	for ach in ["ach_engulf_20", "ach_trigger_burst", "ach_reach_level_5", "ach_survive_180s"]:
		AM.unlock(ach)
	menu._setup_class_buttons()

	for cell_id in ["macrophage", "ctl", "neutrophil", "b_cell", "dendritic"]:
		menu._select_class(cell_id)
		if menu.class_confirm_btn.disabled:
			printerr("Class '%s' should be unlocked and confirmable after achievements" % cell_id)
			quit(1)
			return true
	print("[PASS] All 5 immune defense cells selectable and confirmed unlocked via achievements.")

	# Re-select Macrophage and proceed
	menu._select_class("macrophage")
	menu._on_class_confirm_pressed()
	if menu.class_view.visible or not menu.map_view.visible:
		printerr("Map view transition failed")
		quit(1)
		return true
	if game_manager.selected_class != "macrophage":
		printerr("game_manager.selected_class not saved correctly: " + str(game_manager.selected_class))
		quit(1)
		return true
	print("[PASS] Transition to MapView with GameManager.selected_class = 'macrophage' verified.")

	# 4. Test Map Selection
	menu._select_map("alveolar_space")
	if menu.active_map_key != "alveolar_space":
		printerr("Active map key not updated")
		quit(1)
		return true
	print("[PASS] Map selection (Alveolar Space) verified.")

	# 5. Test Main Scene loading with selected map
	menu.queue_free()

	game_manager.selected_map = "alveolar_space"
	var main_scene = load("res://scenes/main.tscn")
	var main = main_scene.instantiate()
	root.add_child(main)
	main._ready()

	if main.map_id != "alveolar_space":
		printerr("Main arena did not apply alveolar_space map_id")
		quit(1)
		return true
	print("[PASS] Main arena successfully configured with alveolar_space environment.")

	main.queue_free()
	game_manager.queue_free()
	AM.reset_all()

	print("--- ALL MENU & SELECTION FLOW TESTS PASSED! ---")
	quit(0)
	return true
