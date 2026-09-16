extends SceneTree

const GM = preload("res://scripts/core/game_manager.gd")

var frames_waited: int = 0
var test_done: bool = false

func _init() -> void:
	print("--- BEGINNING CODEX & TOOLTIP AUTOMATED VERIFICATION ---")
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

	# 1. Verify Catalogs in GameManager
	if GM.SKILL_CATALOG.size() < 9:
		printerr("[FAIL] SKILL_CATALOG missing expected skills count: " + str(GM.SKILL_CATALOG.size()))
		quit(1)
		return true
	if not GM.SKILL_CATALOG.has("macrophage_pseudopods") or not GM.SKILL_CATALOG.has("ros_torrent"):
		printerr("[FAIL] Core skills missing from SKILL_CATALOG")
		quit(1)
		return true
	print("[PASS] GameManager.SKILL_CATALOG contains 9 complete skill definitions.")

	if GM.PATHOGEN_CATALOG.size() < 4:
		printerr("[FAIL] PATHOGEN_CATALOG missing expected pathogens count: " + str(GM.PATHOGEN_CATALOG.size()))
		quit(1)
		return true
	print("[PASS] GameManager.PATHOGEN_CATALOG contains 4 complete pathogen definitions.")

	# 2. Verify In-Game Skill Tooltip
	var skill_tooltip = hud.get_node_or_null("SkillTooltip")
	if skill_tooltip == null:
		printerr("[FAIL] SkillTooltip node missing from HUD")
		quit(1)
		return true

	var slots_container = hud.get_node_or_null("SkillContainer/VBox/SlotsContainer")
	if slots_container == null or slots_container.get_child_count() < 6:
		printerr("[FAIL] SlotsContainer missing or has insufficient slots")
		quit(1)
		return true

	# Test Hover Slot 0 (Innate Deformation Skill)
	var card0 = slots_container.get_child(0)
	hud._on_slot_mouse_entered(0, card0)
	if not skill_tooltip.visible:
		printerr("[FAIL] SkillTooltip not visible after mouse entered slot 0")
		quit(1)
		return true

	var t_icon = hud.tooltip_icon.text
	var t_title = hud.tooltip_title.text
	var t_badge = hud.tooltip_badge.text
	var t_bio = hud.tooltip_bio.text
	if t_icon != "💨":
		printerr("[FAIL] Slot 0 tooltip icon mismatch: " + t_icon)
		quit(1)
		return true
	if not hud.tooltip_badge.text.contains("主动") and not hud.tooltip_badge.text.contains("ACTIVE"):
		printerr("[FAIL] Slot 0 tooltip badge should indicate active, got: " + t_badge)
		quit(1)
		return true
	if not hud.tooltip_stats.text.contains("3.2"):
		printerr("[FAIL] Slot 0 tooltip stats should show 3.2s cooldown, got: " + hud.tooltip_stats.text)
		quit(1)
		return true
	print("[PASS] Slot 0 Active Weapon Tooltip verified: '%s %s' (%s)" % [t_icon, t_title, t_badge])

	# Test Hover Slot 1 (Empty Slot)
	var card1 = slots_container.get_child(1)
	hud._on_slot_mouse_entered(1, card1)
	if hud.tooltip_icon.text != "+":
		printerr("[FAIL] Slot 1 empty tooltip icon mismatch: " + hud.tooltip_icon.text)
		quit(1)
		return true
	if hud.tooltip_bio.visible:
		printerr("[FAIL] Empty slot tooltip should not display bio text")
		quit(1)
		return true
	print("[PASS] Slot 1 Empty Slot Tooltip verified: '%s %s'" % [hud.tooltip_icon.text, hud.tooltip_title.text])

	# Test Hover Slot 2 (Empty Slot)
	var card2 = slots_container.get_child(2)
	hud._on_slot_mouse_entered(2, card2)
	if hud.tooltip_icon.text != "+":
		printerr("[FAIL] Slot 2 empty tooltip icon mismatch: " + hud.tooltip_icon.text)
		quit(1)
		return true
	if hud.tooltip_bio.visible:
		printerr("[FAIL] Empty slot tooltip should not display bio text")
		quit(1)
		return true
	print("[PASS] Slot 2 Empty Slot Tooltip verified: '%s %s'" % [hud.tooltip_icon.text, hud.tooltip_title.text])

	# Test Mouse Exited
	hud._on_slot_mouse_exited(2)
	if skill_tooltip.visible:
		printerr("[FAIL] SkillTooltip should be hidden after mouse exited")
		quit(1)
		return true
	print("[PASS] Tooltip hide on mouse exit verified.")

	# 3. Verify Codex Modal in HUD
	var codex_modal = hud.get_node_or_null("CodexModal")
	if codex_modal == null:
		printerr("[FAIL] CodexModal missing from HUD")
		quit(1)
		return true

	# Open Codex to Tab 0 (Skills Manual)
	codex_modal.open_codex(0)
	if not codex_modal.visible:
		printerr("[FAIL] CodexModal not visible after open_codex()")
		quit(1)
		return true
	if codex_modal.current_tab != 0:
		printerr("[FAIL] CodexModal current_tab != 0")
		quit(1)
		return true

	var item_list = codex_modal.item_list
	if item_list.get_child_count() < 9:
		printerr("[FAIL] Codex Skills tab items count < 9, got: " + str(item_list.get_child_count()))
		quit(1)
		return true
	print("[PASS] Codex Skill Manual tab displays %d skills." % item_list.get_child_count())

	# Switch to Tab 1 (Cells)
	codex_modal.switch_tab(1)
	if item_list.get_child_count() < 5:
		printerr("[FAIL] Codex Cells tab items count < 5, got: " + str(item_list.get_child_count()))
		quit(1)
		return true
	print("[PASS] Codex Immune Cells tab displays %d cells." % item_list.get_child_count())

	# Switch to Tab 2 (Pathogens)
	codex_modal.switch_tab(2)
	if item_list.get_child_count() < 4:
		printerr("[FAIL] Codex Pathogens tab items count < 4, got: " + str(item_list.get_child_count()))
		quit(1)
		return true
	print("[PASS] Codex Pathogen Catalog tab displays %d pathogens." % item_list.get_child_count())

	# Switch to Tab 3 (Maps)
	codex_modal.switch_tab(3)
	if item_list.get_child_count() < 2:
		printerr("[FAIL] Codex Maps tab items count < 2, got: " + str(item_list.get_child_count()))
		quit(1)
		return true
	print("[PASS] Codex Pathological Stages tab displays %d maps." % item_list.get_child_count())

	# Close Codex
	codex_modal.close_codex()
	if codex_modal.visible:
		printerr("[FAIL] CodexModal still visible after close_codex()")
		quit(1)
		return true
	print("[PASS] CodexModal open/close and 4-tab switching verified.")

	# 4. Verify Pause Menu Manual Button
	hud.toggle_pause()
	if not hud.pause_modal.visible:
		printerr("[FAIL] Pause modal did not open")
		quit(1)
		return true

	# Click Manual Button inside Pause Menu
	hud._on_manual_pressed()
	if not codex_modal.visible:
		printerr("[FAIL] Codex did not open from Pause Menu ManualButton")
		quit(1)
		return true
	print("[PASS] Codex / Skill Manual accessible directly from in-game Pause Menu.")

	# Close codex from pause menu
	codex_modal.close_codex()
	hud.resume_game()
	if hud.pause_modal.visible or codex_modal.visible:
		printerr("[FAIL] Game did not resume cleanly")
		quit(1)
		return true
	print("[PASS] In-game pause modal and manual cleanly resumed.")

	# 5. Bilingual Localization on Tooltip and Manual
	GM.set_language("en")
	hud._on_slot_mouse_entered(0, card0)
	var en_badge = hud.tooltip_badge.text
	if not en_badge.contains("ACTIVE"):
		printerr("[FAIL] English tooltip badge mismatch: " + en_badge)
		quit(1)
		return true
	if not hud.tooltip_title.text.contains("ROS Torrent"):
		printerr("[FAIL] English tooltip title mismatch: " + hud.tooltip_title.text)
		quit(1)
		return true
	if hud.tooltip_desc.text.contains("战术机制"):
		printerr("[FAIL] English tooltip description must NOT contain Chinese headers: " + hud.tooltip_desc.text)
		quit(1)
		return true

	# Test Codex in English: ensure NO Chinese headers
	codex_modal.open_codex(1) # Cells tab
	codex_modal._select_cell("macrophage")
	if codex_modal.detail_desc.text.contains("变形特性"):
		printerr("[FAIL] English cell details must NOT contain Chinese: " + codex_modal.detail_desc.text)
		quit(1)
		return true
	if not codex_modal.detail_desc.text.contains("Deformation Trait"):
		printerr("[FAIL] English cell details must contain 'Deformation Trait': " + codex_modal.detail_desc.text)
		quit(1)
		return true

	codex_modal.switch_tab(0) # Skills tab
	codex_modal._select_skill("macrophage_pseudopods")
	if codex_modal.detail_desc.text.contains("战术机制"):
		printerr("[FAIL] English skill details must NOT contain Chinese: " + codex_modal.detail_desc.text)
		quit(1)
		return true
	if not codex_modal.detail_desc.text.contains("Tactical Effect"):
		printerr("[FAIL] English skill details must contain 'Tactical Effect': " + codex_modal.detail_desc.text)
		quit(1)
		return true

	codex_modal.close_codex()

	GM.set_language("zh_CN")
	hud._on_slot_mouse_entered(0, card0)
	var zh_badge = hud.tooltip_badge.text
	if not zh_badge.contains("主动"):
		printerr("[FAIL] Chinese tooltip badge mismatch: " + zh_badge)
		quit(1)
		return true
	if not hud.tooltip_title.text.contains("活性氧射流"):
		printerr("[FAIL] Chinese tooltip title mismatch: " + hud.tooltip_title.text)
		quit(1)
		return true
	if not hud.tooltip_desc.text.contains("战术机制"):
		printerr("[FAIL] Chinese tooltip description should contain Chinese header: " + hud.tooltip_desc.text)
		quit(1)
		return true
	hud._on_slot_mouse_exited(0)
	print("[PASS] Tooltip and Manual dynamic bilingual switching verified without residual Chinese.")

	print("--- ALL CODEX & TOOLTIP TESTS PASSED SUCCESSFULLY! ---")
	quit(0)
	return true
