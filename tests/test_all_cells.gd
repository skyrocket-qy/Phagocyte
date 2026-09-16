extends SceneTree

const GM = preload("res://scripts/core/game_manager.gd")
const BaseCellClass = preload("res://scripts/player/base_cell.gd")

var phase: int = 0
var frame_count: int = 0
var main_instance: Node = null

func _init() -> void:
	print("==================================================================")
	print(">>> STARTING FULL IMMUNE CELL ROSTER VERIFICATION (5 CELLS) <<<")
	print("==================================================================")

func _process(_delta: float) -> bool:
	match phase:
		0:
			# --- Phase 0: Test All 5 Cell Archetypes in Isolation ---
			var cell_ids = ["macrophage", "ctl", "neutrophil", "b_cell", "dendritic"]
			for cid in cell_ids:
				var scene = GM.get_cell_scene(cid)
				assert(scene != null, "Scene for '%s' must exist" % cid)
				var cell = scene.instantiate()
				assert(cell is BaseCellClass, "'%s' must inherit BaseCell" % cid)
				root.add_child(cell)

				# Because tree is active in _process, _ready has fired
				assert(cell.stats != null, "'%s' must have CellStats" % cid)
				assert(cell.skill_manager != null, "'%s' must have SkillManager" % cid)

				# Check smooth 64-vertex organic deformation
				cell._update_pseudopod_deformation(0.016)
				assert(cell.cytoplasm.polygon.size() == 64, "'%s' cytoplasm must have 64 vertices" % cid)
				assert(cell.engulf_collider.polygon.size() == 32, "'%s' engulf collider must have 32 vertices" % cid)
				assert(cell.nucleus.polygon.size() >= 16, "'%s' nucleus must have geometry" % cid)

				# Check Slot 0 active weapon
				var slot0 = cell.skill_manager.get_active_slot(0)
				assert(slot0 != null, "'%s' must have starting weapon in slot 0" % cid)

				# Verify cell-specific starting weapon and stats
				match cid:
					"macrophage":
						assert(slot0.skill_id == "ros_torrent", "Macrophage must start with ROS Torrent")
						assert(cell.max_health == 100.0, "Macrophage max HP mismatch")
						assert(cell.base_speed == 230.0, "Macrophage speed mismatch")
					"ctl":
						assert(slot0.skill_id == "perforin_lance", "CTL must start with Perforin Lance")
						assert(cell.max_health == 75.0, "CTL max HP mismatch")
						assert(cell.base_speed == 280.0, "CTL speed mismatch")
						assert(is_equal_approx(cell.stats.get_stat("crit_chance"), 0.15), "CTL crit chance mismatch")
					"neutrophil":
						assert(slot0.skill_id == "complement_cascade", "Neutrophil must start with Complement Cascade")
						assert(cell.max_health == 90.0, "Neutrophil max HP mismatch")
						assert(is_equal_approx(cell.stats.get_stat("might"), 1.15), "Neutrophil might mismatch")
					"b_cell":
						assert(slot0.skill_id == "antibody_salvo", "B-Cell must start with Antibody Salvo")
						assert(cell.max_health == 85.0, "B-Cell max HP mismatch")
						assert(is_equal_approx(cell.stats.get_stat("cooldown_reduction"), 0.15), "B-Cell CDR mismatch")
					"dendritic":
						assert(slot0.skill_id == "pseudopod_lunge", "Dendritic must start with Pseudopod Lunge")
						assert(cell.max_health == 95.0, "Dendritic max HP mismatch")
						assert(is_equal_approx(cell.stats.get_stat("magnet"), 1.50), "Dendritic magnet mismatch")

				# Test Burst trigger
				assert(not cell.is_burst, "'%s' should not be in burst initially" % cid)
				cell.trigger_burst()
				assert(cell.is_burst, "'%s' burst trigger failed" % cid)
				assert(cell.current_speed > cell.base_speed * 2.0, "'%s' burst speed boost failed" % cid)

				print("[PASS] Verified '%s': Stats, 32-Vertex Morphology, Nucleus, Slot 0 Weapon, Burst." % cid)
				cell.queue_free()

			print("[PASS] All 5 cell archetypes verified successfully in isolation.")

			# --- Phase 1: In-Game Runtime Instantiation with CTL selected ---
			GM.selected_class = "ctl"
			var main_scene = load("res://scenes/main.tscn")
			assert(main_scene != null, "Could not load main.tscn")
			main_instance = main_scene.instantiate()
			root.add_child(main_instance)
			phase = 1
			return false

		1:
			# Wait a few frames for main._ready() and deferred calls
			frame_count += 1
			if frame_count < 4:
				return false

			var main = root.get_node_or_null("Main")
			assert(main != null, "Main scene must be active")

			# Verify active player is CTLCell
			assert(main.player != null, "Main.player must be instantiated")
			assert(main.player.get_script().resource_path.contains("ctl_cell"), "Active player should be CTLCell")
			assert(main.player.is_in_group("player"), "Player must be in 'player' group")
			assert(main.camera.get_parent() == main.player, "Camera2D must be attached to active player")
			assert(main.hud.player_ref == main.player, "HUD must be connected to active player")

			print("[PASS] In-game runtime dynamic spawning and integration with non-default cell (CTL) verified.")
			print("==================================================================")
			print(">>> ALL 5 IMMUNE CELL ARCHETYPES FULLY VERIFIED! <<<")
			print("==================================================================")

			# Reset selected class to default
			GM.selected_class = "macrophage"
			quit(0)
			return true

	return false
