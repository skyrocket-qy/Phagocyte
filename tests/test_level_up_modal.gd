extends SceneTree

const UpgradeManager = preload("res://scripts/core/upgrade_manager.gd")
const ROSTorrentClass = preload("res://scripts/skills/ros_torrent_skill.gd")
const CellStatsClass = preload("res://scripts/core/cell_stats.gd")
const SkillManagerClass = preload("res://scripts/skills/skill_manager.gd")
const PassiveActinClass = preload("res://scripts/skills/passive_actin_polymerization.gd")

var frame_count: int = 0
var test_done: bool = false

func _init() -> void:
	print("==================================================================")
	print(">>> STARTING LEVEL-UP 3-CHOICE MUTATION SYSTEM VERIFICATION <<<")
	print("==================================================================")

	# --- Test 1: UpgradeManager Card Generation ---
	var mock_player = CharacterBody2D.new()
	var stats = CellStatsClass.new()
	stats.name = "CellStats"
	mock_player.add_child(stats)

	var sm = SkillManagerClass.new()
	sm.name = "SkillManager"
	mock_player.add_child(sm)
	sm.setup(mock_player)

	# Equip Slot 0 with ROS Torrent Lv.1
	var ros = ROSTorrentClass.new()
	sm.equip_active(ros, 0)

	var choices = UpgradeManager.generate_choices(mock_player, 3)
	assert(choices.size() == 3, "Must generate exactly 3 choices")

	# Ensure all 3 generated choices have distinct IDs
	var ids: Array[String] = []
	for c in choices:
		assert(not ids.has(c["id"]), "Choices must not contain duplicate skills: " + c["id"])
		ids.append(c["id"])
		assert(c.has("name") and c.has("icon") and c.has("desc"), "Choice card missing display metadata")
	print("[PASS] Test 1: UpgradeManager generated 3 distinct valid cards: " + str(ids))

	# --- Test 2: Applying New Active & New Passive Choices ---
	var new_active_choice = {
		"type": "new_active",
		"id": "perforin_lance",
		"skill_class": UpgradeManager.PerforinLanceClass
	}
	var res_active = UpgradeManager.apply_choice(mock_player, new_active_choice)
	assert(res_active, "Applying new_active choice should succeed")
	assert(sm.get_active_slot(1) != null, "Perforin Lance should be equipped in active slot 1")
	assert(sm.get_active_slot(1).skill_id == "perforin_lance", "Equipped skill id mismatch")

	var new_passive_choice = {
		"type": "new_passive",
		"id": "passive_actin",
		"skill_class": UpgradeManager.PassiveActinClass
	}
	var res_passive = UpgradeManager.apply_choice(mock_player, new_passive_choice)
	assert(res_passive, "Applying new_passive choice should succeed")
	assert(sm.get_passive_slot(0) != null, "Actin should be equipped in passive slot 0")
	assert(sm.get_passive_slot(0).skill_id == "passive_actin", "Equipped passive id mismatch")
	assert(is_equal_approx(stats.get_stat("area"), 1.12), "Actin stat modifier should apply immediately")
	print("[PASS] Test 2: Applying new active and new passive choices successfully updates slots & stats.")

	# --- Test 3: Upgrading Existing Skill ---
	var upgrade_choice = {
		"type": "upgrade_passive",
		"id": "passive_actin",
		"skill_ref": sm.get_passive_slot(0)
	}
	var res_up = UpgradeManager.apply_choice(mock_player, upgrade_choice)
	assert(res_up, "Upgrading passive should succeed")
	assert(sm.get_passive_slot(0).level == 2, "Passive level should now be 2")
	assert(is_equal_approx(stats.get_stat("area"), 1.24), "Passive Lv.2 stat modifier should apply")
	print("[PASS] Test 3: Upgrading existing skill properly increments level and updates stats.")

	# --- Test 4: Slot Overflow Boundaries (Max 5 Actives / Max 5 Passives) ---
	# Fill remaining 3 active slots to reach 5/5
	sm.equip_active(UpgradeManager.ComplementCascadeClass.new())
	sm.equip_active(UpgradeManager.AntibodySalvoClass.new())
	sm.equip_active(UpgradeManager.PseudopodLungeClass.new())

	for s in sm.active_slots:
		assert(s != null, "All 5 active slots should be occupied")

	# Now generate choices: should NEVER have "new_active"
	for _iter in range(5):
		var full_active_choices = UpgradeManager.generate_choices(mock_player, 3)
		for c in full_active_choices:
			assert(c["type"] != "new_active", "Must NOT offer new_active when active slots are full (5/5)!")

	# Fill remaining 4 passive slots to reach 5/5
	sm.equip_passive(UpgradeManager.PassiveLysosomeClass.new())
	sm.equip_passive(UpgradeManager.PassiveMitochondriaClass.new())
	sm.equip_passive(UpgradeManager.PassiveOpsoninClass.new())
	sm.equip_passive(UpgradeManager.PassiveChemokineClass.new())

	for s in sm.passive_slots:
		assert(s != null, "All 5 passive slots should be occupied")

	for _iter in range(5):
		var full_all_choices = UpgradeManager.generate_choices(mock_player, 3)
		for c in full_all_choices:
			assert(c["type"] != "new_active", "Must NOT offer new_active when active full")
			assert(c["type"] != "new_passive", "Must NOT offer new_passive when passive full")
	print("[PASS] Test 4: Slot overflow boundaries (5/5 Actives, 5/5 Passives) strictly enforced.")

	mock_player.queue_free()

	# --- Test 5: In-game Runtime Integration with Main Scene ---
	var main_scene = load("res://scenes/main.tscn")
	if main_scene == null:
		printerr("[FAIL] Could not load main.tscn")
		quit(1)
		return
	var main = main_scene.instantiate()
	root.add_child(main)

func _process(_delta: float) -> bool:
	if test_done:
		return true

	frame_count += 1
	if frame_count < 4:
		return false

	test_done = true
	var main = root.get_node_or_null("Main")
	if main == null:
		printerr("[FAIL] Main scene not found")
		quit(1)
		return true

	var player = main.get_node_or_null("Macrophage")
	if player == null:
		printerr("[FAIL] Macrophage not found")
		quit(1)
		return true

	var hud = main.get_node_or_null("HUD")
	if hud == null:
		printerr("[FAIL] HUD not found")
		quit(1)
		return true

	assert(hud.upgrade_modal != null, "UpgradeModal must be initialized on HUD")
	assert(not hud.upgrade_modal.visible, "UpgradeModal must be initially hidden")
	assert(not paused, "Game must initially be running unpaused")

	# Initial level checks
	assert(player.current_level == 1, "Initial level should be 1")
	assert(player.current_exp == 0.0, "Initial exp should be 0")

	# Trigger Level-Up by awarding enough EXP
	player.add_exp(35.0)

	assert(player.current_level == 2, "Player should level up to 2")
	assert(hud.upgrade_modal.visible, "UpgradeModal must be shown upon level_up")
	assert(paused, "Game tree must be paused while UpgradeModal is shown")

	# Verify 3 cards populated
	var card_container = hud.upgrade_modal.cards_container
	assert(card_container.get_child_count() == 3, "Must have 3 card containers")
	for i in range(3):
		assert(card_container.get_child(i).visible, "Card %d must be visible" % i)

	# Simulate player clicking Card 0
	var old_active_count = 0
	for s in player.skill_manager.active_slots:
		if s != null:
			old_active_count += 1

	var chosen_item = hud.upgrade_modal.current_choices[0]
	hud.upgrade_modal._on_card_clicked(0)

	assert(not hud.upgrade_modal.visible, "UpgradeModal must close after selection")
	assert(not paused, "Game tree must unpause after selection")

	print("[PASS] Test 5: In-game Level-Up trigger, pause, modal display, card selection & resume verified.")
	print("==================================================================")
	print(">>> ALL LEVEL-UP 3-CHOICE TESTS PASSED SUCCESSFULLY! <<<")
	print("==================================================================")
	quit(0)
	return true
