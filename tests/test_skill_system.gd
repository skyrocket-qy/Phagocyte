extends SceneTree

const GM = preload("res://scripts/core/game_manager.gd")
const PassiveActinClass = preload("res://scripts/skills/passive_actin_polymerization.gd")

var frames_waited: int = 0
var test_done: bool = false

func _init() -> void:
	print("--- BEGINNING 5+5 SKILL SYSTEM AUTOMATED VERIFICATION ---")
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
	if frames_waited < 4:
		return false

	test_done = true
	var main = root.get_node_or_null("Main")
	if main == null:
		printerr("[FAIL] Main scene not found")
		quit(1)
		return true

	var player = main.get_node_or_null("Macrophage")
	if player == null:
		printerr("[FAIL] Macrophage player not found")
		quit(1)
		return true

	var enemy_container = main.get_node_or_null("EnemyContainer")
	if enemy_container == null:
		printerr("[FAIL] EnemyContainer not found")
		quit(1)
		return true

	var hud = main.get_node_or_null("HUD")
	if hud == null:
		printerr("[FAIL] HUD not found")
		quit(1)
		return true

	# 1. Verify SkillManager 5 Active + 5 Passive slot counts
	var sm: SkillManager = player.get_node_or_null("SkillManager")
	if sm == null:
		printerr("[FAIL] SkillManager node missing on Macrophage")
		quit(1)
		return true

	if sm.active_slots.size() != 5:
		printerr("[FAIL] Active slots count is not 5: " + str(sm.active_slots.size()))
		quit(1)
		return true
	if sm.passive_slots.size() != 5:
		printerr("[FAIL] Passive slots count is not 5: " + str(sm.passive_slots.size()))
		quit(1)
		return true
	print("[PASS] SkillManager contains exactly 5 Active slots and 5 Passive slots.")

	# 2. Verify Active Slot 0: ROSTorrentSkill
	var slot0 = sm.get_active_slot(0)
	if slot0 == null or not (slot0 is ROSTorrentSkill):
		printerr("[FAIL] Active Slot 0 is not ROSTorrentSkill")
		quit(1)
		return true
	print("[PASS] Active Slot 0 correctly contains ROSTorrentSkill.")

	# 3. Verify Active Slots 1-4 are empty
	for i in range(1, 5):
		if sm.get_active_slot(i) != null:
			printerr("[FAIL] Active Slot %d is not empty" % i)
			quit(1)
			return true
	print("[PASS] Active Slots 1 to 4 are empty and available.")

	# 4. Verify Passive Slots 0-4 are initially empty
	for i in range(5):
		if sm.get_passive_slot(i) != null:
			printerr("[FAIL] Passive Slot %d is not empty" % i)
			quit(1)
			return true
	print("[PASS] Passive Slots 0 to 4 are initially empty and available.")

	# 5. Verify smooth 64-vertex deformation & 32-vertex collision sync
	player._update_pseudopod_deformation(0.016)
	if player.cytoplasm.polygon.size() != 64:
		printerr("[FAIL] Cytoplasm polygon vertices != 64: " + str(player.cytoplasm.polygon.size()))
		quit(1)
		return true
	if player.engulf_collider.polygon.size() != 32:
		printerr("[FAIL] EngulfCollider polygon vertices != 32: " + str(player.engulf_collider.polygon.size()))
		quit(1)
		return true
	if player.membrane.points.size() != 65: # 64 + 1 to close loop
		printerr("[FAIL] Membrane points != 65: " + str(player.membrane.points.size()))
		quit(1)
		return true
	print("[PASS] Smooth 64-vertex organic pseudopod deformation & CollisionPolygon2D sync verified.")

	# 6. Test ROS Torrent auto-targeting & firing
	var staph_scene = load("res://scenes/enemies/staph_enemy.tscn")
	var enemy = staph_scene.instantiate()
	enemy.global_position = player.global_position + Vector2(150, 0)
	enemy_container.add_child(enemy)

	slot0.trigger()
	var projectile_found: bool = false
	for child in main.get_children():
		if child is ROSJet:
			projectile_found = true
			if child.global_position.distance_to(player.global_position) > 60.0:
				printerr("[FAIL] ROSJet spawned too far from player")
				quit(1)
				return true
			break

	if not projectile_found:
		printerr("[FAIL] ROS Torrent failed to spawn ROSJet projectile")
		quit(1)
		return true
	print("[PASS] ROS Torrent projectile emission & target acquisition verified.")

	# 7. Test equipping a passive trait into passive slot 0
	var actin = PassiveActinClass.new()
	var initial_area = player.stats.get_stat("area")
	sm.equip_passive(actin, 0)
	assert(sm.get_passive_slot(0) == actin, "Passive slot 0 should contain Actin")
	assert(player.stats.get_stat("area") > initial_area, "Player area should increase with Actin passive")
	print("[PASS] Equipping passive trait dynamically modifies player stats.")

	# 8. Verify HUD displays skills
	hud._update_skill_slots()
	var slots_container = hud.get_node_or_null("SkillContainer/VBox/SlotsContainer")
	if slots_container != null and slots_container.get_child_count() > 0:
		var card0 = slots_container.get_child(0)
		var card0_icon = card0.get_node("IconLabel").text
		if card0_icon != "💨":
			printerr("[FAIL] HUD Slot 0 icon expected 💨, got: " + card0_icon)
			quit(1)
			return true
		print("[PASS] HUD Slot 0 correctly displays active weapon icon: " + card0_icon)

	print("--- ALL 5+5 SKILL SYSTEM AUTOMATED TESTS PASSED SUCCESSFULLY! ---")
	quit(0)
	return true
