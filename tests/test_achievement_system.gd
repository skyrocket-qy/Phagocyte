extends SceneTree

const GM = preload("res://scripts/core/game_manager.gd")
const AM = preload("res://scripts/core/achievement_manager.gd")
const UpgradeManagerClass = preload("res://scripts/core/upgrade_manager.gd")
const ROSTorrentClass = preload("res://scripts/skills/ros_torrent_skill.gd")
const CellStatsClass = preload("res://scripts/core/cell_stats.gd")
const SkillManagerClass = preload("res://scripts/skills/skill_manager.gd")

var phase: int = 0
var frame_count: int = 0
var codex_instance: Node = null
var menu_instance: Node = null

func _init() -> void:
	print("==================================================================")
	print(">>> STARTING ACHIEVEMENT & IMMUNE CELL PROGRESSION TEST <<<")
	print("==================================================================")

func _process(_delta: float) -> bool:
	match phase:
		0:
			# --- Step 1: Initial State (Only Macrophage unlocked, others locked) ---
			AM.reset_all()

			assert(GM.is_class_unlocked("macrophage"), "Macrophage must be unlocked by default")
			assert(not GM.is_class_unlocked("ctl"), "CTL must be locked initially")
			assert(not GM.is_class_unlocked("neutrophil"), "Neutrophil must be locked initially")
			assert(not GM.is_class_unlocked("b_cell"), "B-Cell must be locked initially")
			assert(not GM.is_class_unlocked("dendritic"), "Dendritic must be locked initially")

			print("[PASS] Step 1: Default state strictly enforces only Macrophage unlocked.")

			# --- Step 2: Skill Locking in UpgradeManager Choice Pool ---
			var mock_player = CharacterBody2D.new()
			var stats = CellStatsClass.new()
			stats.name = "CellStats"
			mock_player.add_child(stats)
			var sm = SkillManagerClass.new()
			sm.name = "SkillManager"
			mock_player.add_child(sm)
			sm.setup(mock_player)
			sm.equip_active(ROSTorrentClass.new(), 0)

			# While other 4 cells are locked, their active skills must NEVER be offered
			var locked_skill_ids = ["perforin_lance", "complement_cascade", "antibody_salvo", "pseudopod_lunge"]
			for iter in range(15):
				var choices = UpgradeManagerClass.generate_choices(mock_player, 3)
				for c in choices:
					assert(not locked_skill_ids.has(c["id"]), "Locked class skill '%s' was offered while cell is locked!" % c["id"])

			print("[PASS] Step 2: Locked cells' signature skills are completely excluded from upgrade pool.")

			# --- Step 3: Event-Driven Achievement Unlocks & Cell Rewards ---
			# Test 3a: Digestion 20 -> Unlocks CTL and Perforin Lance
			AM.record_event("pathogen_digested", 20)
			assert(AM.is_unlocked("ach_first_digestion"), "First digestion achievement should be unlocked")
			assert(AM.is_unlocked("ach_engulf_20"), "Phagocytosis Master achievement should be unlocked")
			assert(GM.is_class_unlocked("ctl"), "CTL cell should now be unlocked")

			# Now perforin_lance should be allowed in the candidate pool
			var saw_perforin = false
			for iter in range(30):
				var choices = UpgradeManagerClass.generate_choices(mock_player, 3)
				for c in choices:
					if c["id"] == "perforin_lance":
						saw_perforin = true
						break
				if saw_perforin:
					break
			assert(saw_perforin, "Perforin Lance should now be rollable after CTL unlocked!")
			print("[PASS] Step 3a: Engulf 20 unlocks CTL and adds Perforin Lance to upgrade pool.")

			# Test 3b: Burst Activated -> Unlocks Neutrophil
			AM.record_event("burst_activated")
			assert(AM.is_unlocked("ach_trigger_burst"), "Metabolic Storm achievement should be unlocked")
			assert(GM.is_class_unlocked("neutrophil"), "Neutrophil should now be unlocked")
			print("[PASS] Step 3b: Burst activation unlocks Neutrophil.")

			# Test 3c: Level 5 -> Unlocks B-Cell
			AM.record_event("level_up", 5)
			assert(AM.is_unlocked("ach_reach_level_5"), "Clonal Differentiation achievement should be unlocked")
			assert(GM.is_class_unlocked("b_cell"), "B-Cell should now be unlocked")
			print("[PASS] Step 3c: Level 5 unlocks B-Cell.")

			# Test 3d: Survival 180s -> Unlocks Dendritic Cell
			AM.record_event("survival_time", 185.0)
			assert(AM.is_unlocked("ach_survive_180s"), "Sustained Immunity achievement should be unlocked")
			assert(GM.is_class_unlocked("dendritic"), "Dendritic Cell should now be unlocked")
			print("[PASS] Step 3d: Survival 180s unlocks Dendritic Cell.")

			mock_player.queue_free()

			# --- Step 4: Disk Persistence Verification ---
			AM.save_to_disk()
			AM.unlocked_ids.clear()
			AM.progress_data.clear()
			AM.load_from_disk()

			assert(AM.is_unlocked("ach_engulf_20"), "Persisted ach_engulf_20 must load back as unlocked")
			assert(AM.is_unlocked("ach_trigger_burst"), "Persisted ach_trigger_burst must load back as unlocked")
			assert(AM.is_unlocked("ach_reach_level_5"), "Persisted ach_reach_level_5 must load back as unlocked")
			assert(AM.is_unlocked("ach_survive_180s"), "Persisted ach_survive_180s must load back as unlocked")
			assert(GM.is_class_unlocked("ctl"), "CTL should be unlocked from loaded file")
			assert(GM.is_class_unlocked("neutrophil"), "Neutrophil should be unlocked from loaded file")
			assert(GM.is_class_unlocked("b_cell"), "B-Cell should be unlocked from loaded file")
			assert(GM.is_class_unlocked("dendritic"), "Dendritic should be unlocked from loaded file")
			print("[PASS] Step 4: Full disk persistence (user://achievements.json) verified.")

			# --- Step 5: Codex Modal Tab 4 (Achievements) UI ---
			var codex_scene = load("res://scenes/ui/codex_modal.tscn")
			assert(codex_scene != null, "codex_modal.tscn should load")
			codex_instance = codex_scene.instantiate()
			root.add_child(codex_instance)
			codex_instance.open_codex(4) # Open to achievements tab

			assert(codex_instance.current_tab == 4, "Codex must switch to Tab 4 (Achievements)")
			assert(codex_instance.item_list.get_child_count() == AM.ACHIEVEMENTS.size(), "All achievements must be rendered")
			assert(codex_instance.detail_title.text != "", "Detail title must be populated")
			assert(codex_instance.detail_badge.text.contains("COMPLETED") or codex_instance.detail_badge.text.contains("达成"), "Badge should show completed")
			print("[PASS] Step 5: CodexModal Tab 4 (Achievements) UI rendering verified.")

			# --- Step 6: Main Menu Locked Cell Status & Confirm Button Test ---
			AM.reset_all() # Reset so cells are locked again
			codex_instance.queue_free()

			var menu_scene = load("res://scenes/ui/main_menu.tscn")
			assert(menu_scene != null, "main_menu.tscn should load")
			menu_instance = menu_scene.instantiate()
			root.add_child(menu_instance)
			phase = 1
			return false

		1:
			frame_count += 1
			if frame_count < 3:
				return false

			menu_instance._on_start_pressed()

			# Select locked cell CTL
			menu_instance._select_class("ctl")
			assert(menu_instance.class_confirm_btn.disabled, "Locked CTL confirm button must be disabled")
			assert(menu_instance.class_status_lbl.text.contains("🏆"), "Locked CTL must show trophy requirement")

			# Select unlocked cell Macrophage
			menu_instance._select_class("macrophage")
			assert(not menu_instance.class_confirm_btn.disabled, "Unlocked Macrophage confirm button must be enabled")

			# Unlock CTL and refresh
			AM.unlock("ach_engulf_20")
			menu_instance._setup_class_buttons()
			menu_instance._select_class("ctl")
			assert(not menu_instance.class_confirm_btn.disabled, "CTL must become confirmable after achievement unlock")

			print("[PASS] Step 6: Main Menu locked condition UI and button disabling verified.")
			print("==================================================================")
			print(">>> ACHIEVEMENT & IMMUNE CELL PROGRESSION SUITE PASSED! <<<")
			print("==================================================================")

			menu_instance.queue_free()
			AM.reset_all()
			quit(0)
			return true

	return false
