extends SceneTree

const GM = preload("res://scripts/core/game_manager.gd")

var frames_waited: int = 0
var test_done: bool = false

func _init() -> void:
	print("--- BEGINNING BILINGUAL I18N AUTOMATED VERIFICATION ---")

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

	# 1. Test Simplified Chinese default
	GM.set_language("zh_CN")
	menu._update_all_texts()

	if not ("开始免疫行动" in menu.start_btn.text):
		printerr("Start button should be in Simplified Chinese: " + menu.start_btn.text)
		quit(1)
		return true
	if not ("选择你的免疫防御细胞" in menu.class_header_lbl.text):
		printerr("Class header should be in Simplified Chinese: " + menu.class_header_lbl.text)
		quit(1)
		return true
	print("[PASS] Simplified Chinese (zh_CN) text rendering verified.")

	# 2. Test English switching
	GM.set_language("en")
	menu._update_all_texts()

	if not ("Begin Immune Action" in menu.start_btn.text):
		printerr("Start button should be in English: " + menu.start_btn.text)
		quit(1)
		return true
	if not ("Select Your Immune Defense Cell" in menu.class_header_lbl.text):
		printerr("Class header should be in English: " + menu.class_header_lbl.text)
		quit(1)
		return true
	if not ("Select Pathological Stage" in menu.map_header_lbl.text):
		printerr("Map header should be in English: " + menu.map_header_lbl.text)
		quit(1)
		return true
	print("[PASS] English (en) text rendering verified.")

	# 3. Test Metadata Translation
	var macro_en = GM.get_class_info("macrophage")
	if not ("Melee Heavy Tank" in macro_en["role"]):
		printerr("Macrophage English role failed: " + macro_en["role"])
		quit(1)
		return true

	var map_en = GM.get_map_info("acute_wound")
	if not ("Acute Wound" in map_en["name"]):
		printerr("Acute Wound English name failed: " + map_en["name"])
		quit(1)
		return true
	print("[PASS] Class and Map metadata translations verified.")

	# 4. Test Toggle Functionality
	var new_lang = GM.toggle_language()
	if new_lang != "zh_CN" or GM.current_language != "zh_CN":
		printerr("Language toggle back to zh_CN failed: " + new_lang)
		quit(1)
		return true

	new_lang = GM.toggle_language()
	if new_lang != "en" or GM.current_language != "en":
		printerr("Language toggle to en failed: " + new_lang)
		quit(1)
		return true
	print("[PASS] GameManager.toggle_language() cycle verified.")

	# 5. Test In-Game HUD Localization
	menu.queue_free()

	var hud_scene = load("res://scenes/ui/hud.tscn")
	var hud = hud_scene.instantiate()
	root.add_child(hud)
	hud._ready()

	# Test in English
	GM.set_language("en")
	if not ("Status" in hud.title_label.text):
		printerr("HUD title in English failed: " + hud.title_label.text)
		quit(1)
		return true
	if not ("Resume" in hud.resume_btn.text):
		printerr("Pause Resume in English failed: " + hud.resume_btn.text)
		quit(1)
		return true

	# Test in Chinese
	GM.set_language("zh_CN")
	if not ("状态" in hud.title_label.text):
		printerr("HUD title in Chinese failed: " + hud.title_label.text)
		quit(1)
		return true
	if not ("继续战斗" in hud.resume_btn.text):
		printerr("Pause Resume in Chinese failed: " + hud.resume_btn.text)
		quit(1)
		return true
	print("[PASS] In-game HUD & Pause Menu dynamic localization verified.")

	hud.queue_free()
	print("--- ALL BILINGUAL I18N TESTS PASSED SUCCESSFULLY! ---")
	quit(0)
	return true
