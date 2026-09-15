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

	# 3. Test Class Selection (Macrophage vs Locked Class)
	menu._select_class("macrophage")
	if menu.class_confirm_btn.disabled:
		printerr("Macrophage should be unlocked and confirmable")
		quit(1)
		return true
	print("[PASS] Macrophage selectable and unlocked.")

	menu._select_class("ctl")
	if not menu.class_confirm_btn.disabled:
		printerr("CTL should be locked")
		quit(1)
		return true
	print("[PASS] CTL properly locked with status info displayed.")

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

	print("--- ALL MENU & SELECTION FLOW TESTS PASSED! ---")
	quit(0)
	return true
