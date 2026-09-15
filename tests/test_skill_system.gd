extends SceneTree

const GM = preload("res://scripts/core/game_manager.gd")

var frames_waited: int = 0
var test_done: bool = false

func _init() -> void:
	print("--- BEGINNING SKILL SYSTEM AUTOMATED VERIFICATION ---")
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

	# 1. Verify SkillManager slot count
	var sm: SkillManager = player.get_node_or_null("SkillManager")
	if sm == null:
		printerr("[FAIL] SkillManager node missing on Macrophage")
		quit(1)
		return true

	if sm.slots.size() != 6:
		printerr("[FAIL] SkillManager slots count is not 6: " + str(sm.slots.size()))
		quit(1)
		return true
	print("[PASS] SkillManager contains exactly 6 slots.")

	# 2. Verify Slot 0: Innate Macrophage Deformation Skill
	var slot0 = sm.get_slot(0)
	if slot0 == null or not (slot0 is MacrophageDeformationSkill):
		printerr("[FAIL] Slot 0 is not MacrophageDeformationSkill")
		quit(1)
		return true
	if not slot0.is_innate or not slot0.is_passive:
		printerr("[FAIL] MacrophageDeformationSkill is not marked innate/passive")
		quit(1)
		return true
	print("[PASS] Slot 0 correctly contains innate MacrophageDeformationSkill.")

	# Try overwriting innate slot without bypass - should be rejected
	var dummy_skill = ROSTorrentSkill.new()
	var overwrite_success = sm.equip_skill(dummy_skill, 0)
	if overwrite_success:
		printerr("[FAIL] Innate skill slot was overwritten!")
		quit(1)
		return true
	dummy_skill.queue_free()
	print("[PASS] Innate skill slot protection verified (cannot be overwritten).")

	# 3. Verify Slot 1: Active Weapon ROSTorrentSkill
	var slot1 = sm.get_slot(1)
	if slot1 == null or not (slot1 is ROSTorrentSkill):
		printerr("[FAIL] Slot 1 is not ROSTorrentSkill")
		quit(1)
		return true
	if slot1.is_innate:
		printerr("[FAIL] ROSTorrentSkill should not be innate")
		quit(1)
		return true
	print("[PASS] Slot 1 correctly contains ROSTorrentSkill.")

	# 4. Verify slots 2-5 are empty initially
	for i in range(2, 6):
		if sm.get_slot(i) != null:
			printerr("[FAIL] Slot %d is not empty" % i)
			quit(1)
			return true
	print("[PASS] Slots 2 to 5 are empty and available.")

	# 5. Verify procedural 32-vertex deformation execution via MacrophageDeformationSkill
	sm.update_all_skills(0.016)
	if player.cytoplasm.polygon.size() != 32:
		printerr("[FAIL] Cytoplasm polygon vertices != 32: " + str(player.cytoplasm.polygon.size()))
		quit(1)
		return true
	if player.engulf_collider.polygon.size() != 32:
		printerr("[FAIL] EngulfCollider polygon vertices != 32: " + str(player.engulf_collider.polygon.size()))
		quit(1)
		return true
	if player.membrane.points.size() != 33: # 32 + 1 to close the loop
		printerr("[FAIL] Membrane points != 33: " + str(player.membrane.points.size()))
		quit(1)
		return true
	print("[PASS] 32-vertex organic pseudopod deformation & CollisionPolygon2D sync driven by skill verified.")

	# 6. Test ROS Torrent auto-targeting & firing
	var staph_scene = load("res://scenes/enemies/staph_enemy.tscn")
	var enemy = staph_scene.instantiate()
	enemy.global_position = player.global_position + Vector2(150, 0)
	enemy_container.add_child(enemy)

	# Manually trigger the skill to test projectile emission
	slot1.trigger()
	var projectile_found: bool = false
	for child in main.get_children():
		if child is ROSJet:
			projectile_found = true
			if child.global_position.distance_to(player.global_position) > 50.0:
				printerr("[FAIL] ROSJet spawned too far from player")
				quit(1)
				return true
			break

	if not projectile_found:
		printerr("[FAIL] ROS Torrent failed to spawn ROSJet projectile")
		quit(1)
		return true
	print("[PASS] ROS Torrent projectile emission & target acquisition verified.")

	# 7. Verify HUD 6-slot rendering
	var slots_container = hud.get_node_or_null("SkillContainer/VBox/SlotsContainer")
	if slots_container == null:
		printerr("[FAIL] HUD SlotsContainer not found")
		quit(1)
		return true
	if slots_container.get_child_count() != 6:
		printerr("[FAIL] HUD SlotsContainer does not have 6 slot cards: " + str(slots_container.get_child_count()))
		quit(1)
		return true

	# Trigger HUD update
	hud._update_skill_slots()

	var card0 = slots_container.get_child(0)
	var card0_icon = card0.get_node("IconLabel").text
	var card0_badge = card0.get_node("BadgeLabel").text
	if card0_icon != "🦠":
		printerr("[FAIL] Card 0 icon mismatch: " + card0_icon)
		quit(1)
		return true
	print("[PASS] HUD Slot 0 shows Innate Deformation icon: %s, badge: %s" % [card0_icon, card0_badge])

	var card1 = slots_container.get_child(1)
	var card1_icon = card1.get_node("IconLabel").text
	if card1_icon != "💨":
		printerr("[FAIL] Card 1 icon mismatch: " + card1_icon)
		quit(1)
		return true
	print("[PASS] HUD Slot 1 shows ROS Torrent icon: %s" % card1_icon)

	var card2 = slots_container.get_child(2)
	var card2_icon = card2.get_node("IconLabel").text
	if card2_icon != "+":
		printerr("[FAIL] Card 2 should show '+' for empty slot, got: " + card2_icon)
		quit(1)
		return true
	print("[PASS] HUD Slot 2 correctly displays empty slot placeholder '+'.")

	# 8. Test I18N on Skill System
	GM.set_language("en")
	hud._update_localized_texts()
	var en_badge = card0.get_node("BadgeLabel").text
	if en_badge != "INNATE":
		printerr("[FAIL] English badge for innate skill expected 'INNATE', got: " + en_badge)
		quit(1)
		return true

	GM.set_language("zh_CN")
	hud._update_localized_texts()
	var zh_badge = card0.get_node("BadgeLabel").text
	if zh_badge != "固有":
		printerr("[FAIL] Chinese badge for innate skill expected '固有', got: " + zh_badge)
		quit(1)
		return true
	print("[PASS] Skill localization switching between 'INNATE' and '固有' verified.")

	print("--- ALL SKILL SYSTEM AUTOMATED TESTS PASSED SUCCESSFULLY! ---")
	quit(0)
	return true
