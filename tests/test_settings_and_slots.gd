extends SceneTree

const GM = preload("res://scripts/core/game_manager.gd")
const SM = preload("res://scripts/core/settings_manager.gd")
const SkillManager = preload("res://scripts/skills/skill_manager.gd")
const MitoSkill = preload("res://scripts/skills/passive_mitochondrial_overclock.gd")

var frames_waited: int = 0
var test_done: bool = false

func _init() -> void:
	print("--- BEGINNING SETTINGS & DUAL-ROW SLOTS AUTOMATED VERIFICATION ---")
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
	if frames_waited < 4:
		return false

	test_done = true
	var main = root.get_node_or_null("Main")
	if main == null:
		printerr("[FAIL] Main scene not found")
		quit(1)
		return true

	var hud = main.get_node_or_null("HUD")
	if hud == null:
		printerr("[FAIL] HUD not found")
		quit(1)
		return true

	# =========================================================================
	# TEST 1: HelpContainer is completely removed from HUD
	# =========================================================================
	var help_container = hud.get_node_or_null("HelpContainer")
	if help_container != null:
		printerr("[FAIL] HelpContainer instruction panel is still present in HUD")
		quit(1)
		return true
	print("[PASS] 1. HelpContainer instruction panel has been successfully removed from HUD.")

	# =========================================================================
	# TEST 2: Dual-row 10-Slot HUD GridContainer
	# =========================================================================
	var slots_container = hud.get_node_or_null("SkillContainer/VBox/SlotsContainer")
	if slots_container == null:
		printerr("[FAIL] SlotsContainer missing from HUD")
		quit(1)
		return true

	if not (slots_container is GridContainer):
		printerr("[FAIL] SlotsContainer is not a GridContainer, got: " + slots_container.get_class())
		quit(1)
		return true

	if slots_container.columns != 5:
		printerr("[FAIL] SlotsContainer columns expected 5, got: " + str(slots_container.columns))
		quit(1)
		return true

	var slot_count = slots_container.get_child_count()
	if slot_count != 10:
		printerr("[FAIL] SlotsContainer expected 10 slots (5 active + 5 passive), got: " + str(slot_count))
		quit(1)
		return true
	print("[PASS] 2. HUD contains 10-slot dual-row GridContainer (5 columns x 2 rows).")

	# Check active slots (0..4) and passive slots (5..9)
	for i in range(5):
		var slot = slots_container.get_child(i)
		if slot.name != "Slot" + str(i):
			printerr("[FAIL] Active slot naming mismatch: " + slot.name)
			quit(1)
			return true
	for i in range(5, 10):
		var slot = slots_container.get_child(i)
		if slot.name != "Slot" + str(i):
			printerr("[FAIL] Passive slot naming mismatch: " + slot.name)
			quit(1)
			return true
	print("[PASS] 3. Slot0..Slot4 (Active) and Slot5..Slot9 (Passive) nodes confirmed.")

	# =========================================================================
	# TEST 3: Hover Tooltip for Passive Slots (Slot 5)
	# =========================================================================
	var card5 = slots_container.get_child(5)
	hud._on_slot_mouse_entered(5, card5)
	if not hud.skill_tooltip.visible:
		printerr("[FAIL] Tooltip not visible when hovering slot 5")
		quit(1)
		return true

	if not hud.tooltip_badge.text.contains("被动") and not hud.tooltip_badge.text.contains("PASSIVE"):
		printerr("[FAIL] Slot 5 tooltip badge expected passive indication, got: " + hud.tooltip_badge.text)
		quit(1)
		return true
	hud._on_slot_mouse_exited(5)
	if hud.skill_tooltip.visible:
		printerr("[FAIL] Tooltip should be hidden after slot 5 mouse exited")
		quit(1)
		return true
	print("[PASS] 4. Passive slot 5 hover tooltip displays passive slot details and hides on exit.")

	# =========================================================================
	# TEST 4: Equip Passive Trait and Verify Slot 5 Updates
	# =========================================================================
	var player = main.player if "player" in main and main.player != null else hud.player_ref
	if player == null:
		player = root.get_tree().get_first_node_in_group("player")
	if player == null:
		printerr("[FAIL] Player node not found in main")
		quit(1)
		return true

	var skill_mgr = player.get_node_or_null("SkillManager")
	if skill_mgr == null:
		printerr("[FAIL] SkillManager not found on player")
		quit(1)
		return true

	var mito = MitoSkill.new()
	var equipped = skill_mgr.equip_passive(mito, 0) # Slot 0 in passives = Slot 5 in UI
	if not equipped:
		printerr("[FAIL] Failed to equip passive mitochondria skill")
		quit(1)
		return true

	hud._update_skill_slots()
	var slot5_icon = card5.get_node_or_null("IconLabel")
	var slot5_badge = card5.get_node_or_null("BadgeLabel")
	var slot5_cd = card5.get_node_or_null("CooldownBar")

	if slot5_icon == null or slot5_icon.text != "⚡":
		printerr("[FAIL] Slot 5 icon not updated to mitochondria icon ⚡, got: " + str(slot5_icon.text if slot5_icon else "null"))
		quit(1)
		return true

	if slot5_badge == null or not slot5_badge.text.contains("1"):
		printerr("[FAIL] Slot 5 badge expected Lv.1, got: " + str(slot5_badge.text if slot5_badge else "null"))
		quit(1)
		return true

	if slot5_cd != null and slot5_cd.visible:
		printerr("[FAIL] Passive slot should not display cooldown bar")
		quit(1)
		return true
	print("[PASS] 5. Equipped passive skill correctly populates Slot 5 with icon, Lv.1, and no cooldown bar.")

	# =========================================================================
	# TEST 5: SettingsManager Persistence and Core API
	# =========================================================================
	SM.set_master_volume(0.65)
	SM.set_sfx_volume(0.45)
	SM.set_bgm_volume(0.80)
	SM.set_fullscreen(true)
	SM.set_vsync(false)

	if abs(SM.master_volume - 0.65) > 0.01 or abs(SM.sfx_volume - 0.45) > 0.01 or abs(SM.bgm_volume - 0.80) > 0.01:
		printerr("[FAIL] SettingsManager volume set/get mismatch")
		quit(1)
		return true

	if SM.fullscreen != true or SM.vsync != false:
		printerr("[FAIL] SettingsManager graphics set/get mismatch")
		quit(1)
		return true

	# Test file existence
	if not FileAccess.file_exists("user://settings.json"):
		printerr("[FAIL] user://settings.json was not created")
		quit(1)
		return true

	# Load fresh and verify
	SM.load_from_disk()
	if abs(SM.master_volume - 0.65) > 0.01 or SM.fullscreen != true:
		printerr("[FAIL] Reloaded settings mismatch")
		quit(1)
		return true
	print("[PASS] 6. SettingsManager volume/graphics persistence to user://settings.json verified.")

	# =========================================================================
	# TEST 6: SettingsModal in HUD (Pause Menu)
	# =========================================================================
	var pause_settings_btn = hud.get_node_or_null("PauseModal/VBox/SettingsButton")
	if pause_settings_btn == null:
		printerr("[FAIL] SettingsButton not found in PauseModal")
		quit(1)
		return true

	var hud_settings_modal = hud.get_node_or_null("SettingsModal")
	if hud_settings_modal == null:
		printerr("[FAIL] SettingsModal not found in HUD")
		quit(1)
		return true

	# Trigger open settings from Pause menu
	pause_settings_btn.pressed.emit()
	if not hud_settings_modal.visible:
		printerr("[FAIL] SettingsModal did not open after pressing SettingsButton in PauseModal")
		quit(1)
		return true

	# Test close button (✕)
	var close_btn = hud_settings_modal.get_node_or_null("VBox/Header/CloseButton")
	if close_btn == null:
		printerr("[FAIL] CloseButton (✕) not found in SettingsModal")
		quit(1)
		return true
	if close_btn.text != "✕":
		printerr("[FAIL] CloseButton text expected '✕', got: " + close_btn.text)
		quit(1)
		return true

	close_btn.pressed.emit()
	if hud_settings_modal.visible:
		printerr("[FAIL] SettingsModal should be hidden after clicking CloseButton")
		quit(1)
		return true
	print("[PASS] 7. SettingsModal in in-game Pause Menu opens and closes via '✕' button.")

	# =========================================================================
	# TEST 7: SettingsModal in Main Menu
	# =========================================================================
	var menu_scene = load("res://scenes/ui/main_menu.tscn")
	if menu_scene == null:
		printerr("[FAIL] Failed to load main_menu.tscn")
		quit(1)
		return true
	var menu_node = menu_scene.instantiate()
	root.add_child(menu_node)

	var menu_settings_btn = menu_node.get_node_or_null("TitleView/VBox/SettingsButton")
	if menu_settings_btn == null:
		printerr("[FAIL] SettingsButton not found in Main Menu TitleView")
		quit(1)
		return true

	var menu_settings_modal = menu_node.get_node_or_null("SettingsModal")
	if menu_settings_modal == null:
		printerr("[FAIL] SettingsModal instance not found in Main Menu")
		quit(1)
		return true

	menu_settings_btn.pressed.emit()
	if not menu_settings_modal.visible:
		printerr("[FAIL] SettingsModal did not open in Main Menu")
		quit(1)
		return true

	# Test Tab switching: Tab 0 (Controls), Tab 1 (Audio), Tab 2 (Graphics)
	var tab_controls = menu_settings_modal.get_node_or_null("VBox/TabBar/ControlsTab")
	var tab_audio = menu_settings_modal.get_node_or_null("VBox/TabBar/AudioTab")
	var tab_graphics = menu_settings_modal.get_node_or_null("VBox/TabBar/GraphicsTab")

	if tab_controls == null or tab_audio == null or tab_graphics == null:
		printerr("[FAIL] Settings tabs missing")
		quit(1)
		return true

	tab_audio.pressed.emit()
	var audio_content = menu_settings_modal.get_node_or_null("VBox/Content/AudioPanel")
	if audio_content == null or not audio_content.visible:
		printerr("[FAIL] AudioPanel not visible after switching to Audio tab")
		quit(1)
		return true

	tab_graphics.pressed.emit()
	var graphics_content = menu_settings_modal.get_node_or_null("VBox/Content/GraphicsPanel")
	if graphics_content == null or not graphics_content.visible:
		printerr("[FAIL] GraphicsPanel not visible after switching to Graphics tab")
		quit(1)
		return true

	tab_controls.pressed.emit()
	var controls_content = menu_settings_modal.get_node_or_null("VBox/Content/ControlsPanel")
	if controls_content == null or not controls_content.visible:
		printerr("[FAIL] ControlsPanel not visible after switching to Controls tab")
		quit(1)
		return true

	var menu_close_btn = menu_settings_modal.get_node_or_null("VBox/Header/CloseButton")
	menu_close_btn.pressed.emit()
	if menu_settings_modal.visible:
		printerr("[FAIL] SettingsModal in Main Menu did not close on '✕'")
		quit(1)
		return true
	print("[PASS] 8. Main Menu Settings button, tabs (Controls/Audio/Graphics), and '✕' close button verified.")

	# Cleanup
	menu_node.queue_free()

	print("--- ALL SETTINGS & DUAL-ROW SLOTS TESTS PASSED SUCCESSFULLY! ---")
	quit(0)
	return true
