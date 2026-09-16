extends SceneTree

const StatClass = preload("res://scripts/core/stat.gd")
const CellStatsClass = preload("res://scripts/core/cell_stats.gd")
const BaseSkillClass = preload("res://scripts/skills/base_skill.gd")
const SkillManagerClass = preload("res://scripts/skills/skill_manager.gd")
const ROSTorrentClass = preload("res://scripts/skills/ros_torrent_skill.gd")
const PassiveActinClass = preload("res://scripts/skills/passive_actin_polymerization.gd")
const PassiveLysosomeClass = preload("res://scripts/skills/passive_lysosome_priming.gd")

var frame_count: int = 0
var test_done: bool = false

func _init() -> void:
	print("==================================================================")
	print(">>> STARTING UNIVERSAL STATS & 5+5 SKILL SUITE VERIFICATION <<<")
	print("==================================================================")

	# --- Test 1: Stat.gd Math Formulas ---
	var s = StatClass.new(10.0)
	assert(s.get_value() == 10.0, "Initial stat base value failed")
	s.add_modifier(5.0, 0.20) # (10 + 5) * (1 + 0.20) = 18.0
	assert(is_equal_approx(s.get_value(), 18.0), "Stat add_modifier formula failed")
	s.remove_modifier(5.0, 0.20)
	assert(is_equal_approx(s.get_value(), 10.0), "Stat remove_modifier formula failed")
	s.set_base(20.0)
	assert(is_equal_approx(s.get_value(), 20.0), "Stat set_base failed")
	print("[PASS] Test 1: Stat math calculation (base + flat) * (1 + pct) verified.")

	# --- Test 2: CellStats.gd 16 Universal Stats & Caps ---
	var cs = CellStatsClass.new()
	assert(cs.get_stat("might") == 1.0, "Default might should be 1.0")
	assert(cs.get_stat("area") == 1.0, "Default area should be 1.0")
	assert(cs.get_stat("cooldown_reduction") == 0.0, "Default CDR should be 0.0")
	assert(cs.get_stat("move_speed") == 230.0, "Default move_speed should be 230.0")

	# Test CDR clamp at 75%
	cs.add_modifier("cooldown_reduction", 0.90, 0.0)
	assert(cs.get_stat("cooldown_reduction") == 0.75, "CDR must be capped at 0.75")
	cs.remove_modifier("cooldown_reduction", 0.90, 0.0)
	assert(cs.get_stat("cooldown_reduction") == 0.0, "CDR remove failed")

	# Test Armor percentage formula
	cs.add_modifier("armor", 50.0, 0.0)
	assert(is_equal_approx(cs.get_damage_reduction_ratio(), 0.50), "Armor reduction should be 50%")
	print("[PASS] Test 2: CellStats 16 universal stats, CDR clamping & armor formula verified.")

	# --- Test 3: SkillManager 5 Active + 5 Passive Routing ---
	var sm = SkillManagerClass.new()
	assert(sm.active_slots.size() == 5, "Active slots must equal 5")
	assert(sm.passive_slots.size() == 5, "Passive slots must equal 5")

	var ros = ROSTorrentClass.new()
	var actin = PassiveActinClass.new()

	var eq_active = sm.equip_skill(ros)
	assert(eq_active, "Should successfully equip active skill")
	assert(sm.get_active_slot(0) == ros, "Active skill should be routed to active_slots[0]")
	assert(sm.get_passive_slot(0) == null, "Passive slot 0 should remain empty")

	var eq_passive = sm.equip_skill(actin)
	assert(eq_passive, "Should successfully equip passive skill")
	assert(sm.get_passive_slot(0) == actin, "Passive skill should be routed to passive_slots[0]")

	var ui_data = sm.get_ui_data()
	assert(ui_data.has("actives") and ui_data["actives"].size() == 5, "UI data must contain 5 actives")
	assert(ui_data.has("passives") and ui_data["passives"].size() == 5, "UI data must contain 5 passives")
	print("[PASS] Test 3: SkillManager 5 Active + 5 Passive independent capacity and auto-routing verified.")

	# --- Test 4: Passive Skill Universal Stat Injection & Upgrade ---
	var mock_host = CharacterBody2D.new()
	var mock_stats = CellStatsClass.new()
	mock_stats.name = "CellStats"
	mock_host.add_child(mock_stats)

	var mock_sm = SkillManagerClass.new()
	mock_host.add_child(mock_sm)
	mock_sm.setup(mock_host)

	assert(is_equal_approx(mock_stats.get_stat("area"), 1.0), "Initial area must be 1.0")
	assert(is_equal_approx(mock_stats.get_stat("move_speed"), 230.0), "Initial speed must be 230.0")

	var passive_item = PassiveActinClass.new()
	mock_sm.equip_passive(passive_item, 0)

	# Level 1 Actin: area +12%, speed +6%
	assert(is_equal_approx(mock_stats.get_stat("area"), 1.12), "Actin Lv1 area should be 1.12")
	assert(is_equal_approx(mock_stats.get_stat("move_speed"), 230.0 * 1.06), "Actin Lv1 move_speed should be +6%")

	# Upgrade to Level 2: area +24%, speed +12%
	passive_item.upgrade()
	assert(passive_item.level == 2, "Passive level should be 2")
	assert(is_equal_approx(mock_stats.get_stat("area"), 1.24), "Actin Lv2 area should be 1.24")
	assert(is_equal_approx(mock_stats.get_stat("move_speed"), 230.0 * 1.12), "Actin Lv2 move_speed should be +12%")

	# Equip Lysosome: might +10%, health_regen +0.6
	var lyso = PassiveLysosomeClass.new()
	mock_sm.equip_passive(lyso, 1)
	assert(is_equal_approx(mock_stats.get_stat("might"), 1.10), "Lysosome Lv1 might should be 1.10")
	assert(is_equal_approx(mock_stats.get_stat("health_regen"), 0.60), "Lysosome Lv1 health_regen should be 0.60")

	print("[PASS] Test 4: Passive traits universal stat injection, leveling and stacking verified.")

	# --- Test 5: Active Skill Stat Consumption ---
	var active_weapon = ROSTorrentClass.new()
	mock_sm.equip_active(active_weapon, 0)

	# Cooldown with 0% CDR
	assert(is_equal_approx(active_weapon.get_calculated_cooldown(), 3.2), "Weapon base cooldown should be 3.2")

	# Add 25% CDR
	mock_stats.add_modifier("cooldown_reduction", 0.25, 0.0)
	assert(is_equal_approx(active_weapon.get_calculated_cooldown(), 3.2 * 0.75), "Weapon cooldown with 25% CDR should be 2.4")

	# Amount with extra amount
	assert(active_weapon.get_calculated_amount(1) == 1, "Base amount should be 1")
	mock_stats.add_modifier("amount", 2.0, 0.0)
	assert(active_weapon.get_calculated_amount(1) == 3, "Amount with +2 should be 3")

	# Area with current 1.24 area
	assert(is_equal_approx(active_weapon.get_calculated_area(1.0), 1.24), "Weapon area should scale by 1.24")

	# Damage with might 1.10
	var dmg_calc = active_weapon.get_calculated_damage(20.0)
	assert(is_equal_approx(dmg_calc["damage"], 22.0) or dmg_calc["is_crit"], "Damage should scale with might")

	print("[PASS] Test 5: Active weapon stat consumption (CDR, Area, Amount, Damage) verified.")

	mock_host.queue_free()

	# Now load the actual game scene to test complete runtime integration
	var main_scene = load("res://scenes/main.tscn")
	if main_scene == null:
		printerr("[FAIL] Failed to load main.tscn")
		quit(1)
		return
	var main_instance = main_scene.instantiate()
	root.add_child(main_instance)

func _process(_delta: float) -> bool:
	if test_done:
		return true

	frame_count += 1
	if frame_count < 5:
		return false

	test_done = true
	var main = root.get_node_or_null("Main")
	if main == null:
		printerr("[FAIL] Main scene node not found in scene tree")
		quit(1)
		return true

	var player = main.get_node_or_null("Macrophage")
	if player == null:
		printerr("[FAIL] Macrophage player not found")
		quit(1)
		return true

	assert(player.stats != null, "Macrophage must have CellStats attached")
	assert(player.stats is CellStatsClass, "Macrophage.stats must be instance of CellStats")

	var sm = player.get_node_or_null("SkillManager")
	assert(sm != null, "SkillManager must be attached to Macrophage")
	assert(sm.active_slots.size() == 5, "SkillManager must have 5 active slots")
	assert(sm.passive_slots.size() == 5, "SkillManager must have 5 passive slots")

	var slot0_active = sm.get_active_slot(0)
	assert(slot0_active != null and (slot0_active is ROSTorrentClass), "Active slot 0 must be ROSTorrentSkill")

	# Verify Macrophage movement speed bound to CellStats
	assert(player.current_speed == player.stats.get_stat("move_speed"), "Player speed must match CellStats move_speed")

	# Verify HUD exists and handles skills_data without error
	var hud = main.get_node_or_null("HUD")
	assert(hud != null, "HUD must exist in Main scene")

	print("[PASS] Test 6: In-game Main scene integration with Macrophage, CellStats & HUD verified.")
	print("==================================================================")
	print(">>> ALL UNIVERSAL STAT & 5+5 SKILL TESTS PASSED SUCCESSFULLY! <<<")
	print("==================================================================")
	quit(0)
	return true
