extends SceneTree

const LOCALES = ["en", "zh_CN", "zh_TW", "ja", "de", "fr", "ru", "es"]

func _init():
	print("=== STARTING COMPREHENSIVE MULTILINGUAL DIAGNOSTIC ===")
	
	var scenes_to_check = [
		"res://scenes/ui/settings_modal.tscn",
		"res://scenes/ui/codex_modal.tscn",
		"res://scenes/ui/endgame_setup_modal.tscn",
		"res://scenes/ui/title_view.tscn",
		"res://scenes/ui/upgrade_modal.tscn",
		"res://scenes/ui/run_records_modal.tscn",
		"res://scenes/ui/achievement_gallery.tscn"
	]
	
	for scene_path in scenes_to_check:
		check_scene(scene_path)
		
	# Also check MainMenu as a whole
	check_main_menu()
	
	print("=== COMPREHENSIVE MULTILINGUAL DIAGNOSTIC COMPLETE ===")
	quit(0)

func check_scene(path: String):
	print("\n--------------------------------------------------")
	print("Checking scene: ", path)
	var packed = load(path)
	if not packed:
		print("  [ERROR] Failed to load: ", path)
		return
	
	var instance = packed.instantiate()
	root.add_child(instance)
	
	# Simulate 1280x720 window
	if instance is Control:
		instance.size = Vector2(1280, 720)
		instance.position = Vector2(0, 0)
	
	for loc in LOCALES:
		# Set language
		var gm = root.get_node_or_null("/root/GameManager")
		if gm:
			gm.call("SetLanguage", loc)
		else:
			TranslationServer.set_locale(loc)
			
		if instance.has_method("UpdateLocalizedTexts"):
			instance.call("UpdateLocalizedTexts")
			
		if instance.has_method("OpenSettings"):
			instance.call("OpenSettings", 0)
		elif instance.has_method("OpenSetup"):
			instance.call("OpenSetup")
		elif instance.has_method("OpenCodex"):
			instance.call("OpenCodex", 0)
			
		# Force layout update
		if instance is Control:
			instance.propagate_notification(CanvasItem.NOTIFICATION_DRAW)
			
		# Inspect all child controls
		check_controls(instance, loc, path)
		
	root.remove_child(instance)
	instance.queue_free()

func check_controls(node: Node, loc: String, scene_name: String):
	var stack = [node]
	while not stack.is_empty():
		var curr = stack.pop_back()
		for child in curr.get_children():
			stack.append(child)
			
		if not curr is Control:
			continue
			
		var ctrl = curr as Control
		if not ctrl.visible:
			continue
			
		# Check Labels
		if ctrl is Label:
			var lbl = ctrl as Label
			var text = lbl.text
			
			# Check CJK in Western languages
			if loc in ["en", "de", "fr", "ru", "es"]:
				if has_cjk(text):
					# Exclude known titles/names if any
					print("  [LEAKED CJK][", loc, "] ", ctrl.get_path(), " text: '", text.replace("\n", " "), "'")
					
			# Check autowrap and expansion
			if lbl.autowrap_mode == TextServer.AUTOWRAP_OFF:
				var min_w = lbl.get_minimum_size().x
				if min_w > 500:
					print("  [OVERSIZED UNWRAPPED LABEL][", loc, "] ", ctrl.get_path(), " min_width: ", min_w, " text: '", text.substr(0, 40), "...'")
					
		# Check Buttons
		elif ctrl is Button:
			var btn = ctrl as Button
			var text = btn.text
			if loc in ["en", "de", "fr", "ru", "es"]:
				if has_cjk(text):
					print("  [LEAKED CJK BUTTON][", loc, "] ", ctrl.get_path(), " text: '", text, "'")
			var min_w = btn.get_minimum_size().x
			var cust_min = btn.custom_minimum_size.x
			if cust_min > 0 and min_w > cust_min + 20:
				print("  [BUTTON TEXT EXCEEDS MIN SIZE][", loc, "] ", ctrl.get_path(), " min_w: ", min_w, " > custom_min: ", cust_min, " text: '", text, "'")
				
		# Check SettingsModal specific bounds
		if scene_name.ends_with("settings_modal.tscn") and ctrl.name == "VBox":
			var panel = ctrl.get_parent().get_node_or_null("Panel") as Control
			if panel:
				var vbox_rect = ctrl.get_global_rect()
				var panel_rect = panel.get_global_rect()
				if vbox_rect.position.x < panel_rect.position.x:
					print("  [SETTINGS MODAL OVERFLOW LEFT][", loc, "] VBox pos.x=", vbox_rect.position.x, " < Panel pos.x=", panel_rect.position.x, " (Overflow by ", panel_rect.position.x - vbox_rect.position.x, "px)")
				if vbox_rect.end.x > panel_rect.end.x:
					print("  [SETTINGS MODAL OVERFLOW RIGHT][", loc, "] VBox end.x=", vbox_rect.end.x, " > Panel end.x=", panel_rect.end.x, " (Overflow by ", vbox_rect.end.x - panel_rect.end.x, "px)")
				if vbox_rect.end.y > panel_rect.end.y:
					print("  [SETTINGS MODAL OVERFLOW BOTTOM][", loc, "] VBox end.y=", vbox_rect.end.y, " > Panel end.y=", panel_rect.end.y, " (Overflow by ", vbox_rect.end.y - panel_rect.end.y, "px)")

func check_main_menu():
	print("\n--------------------------------------------------")
	print("Checking MainMenu...")
	var packed = load("res://scenes/ui/main_menu.tscn")
	if not packed:
		return
	var menu = packed.instantiate()
	root.add_child(menu)
	menu.size = Vector2(1280, 720)
	
	for loc in LOCALES:
		var gm = root.get_node_or_null("/root/GameManager")
		if gm:
			gm.call("SetLanguage", loc)
		check_controls(menu, loc, "main_menu.tscn")
		
	root.remove_child(menu)
	menu.queue_free()

func has_cjk(s: String) -> bool:
	for i in range(s.length()):
		var c = s.unicode_at(i)
		if (c >= 0x4E00 and c <= 0x9FFF) or (c >= 0x3400 and c <= 0x4DBF):
			return true
	return false
