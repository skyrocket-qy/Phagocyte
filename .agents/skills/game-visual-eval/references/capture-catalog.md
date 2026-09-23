# Phagocyte Visual Surface & Skill Capture Catalog

This catalog documents all capturable visual surfaces and live skill visual effects in Project: Phagocyte, detailing the scene/skill path, the **automated test harness target**, and the **godot-ai MCP recipe** for interactive inspection.

---

## Standard Directory Structure
All captures must be saved under the project-local `tmp/` directory:
- `tmp/visual_before/`: Baseline captures before making changes
- `tmp/visual_after/`: New captures after changes are implemented
- `tmp/visual_diff/`: Generated 3-column composites (`diff_<surface>.png`)

---

## 1. UI Surfaces Catalog (Automated in `TestFullVisualPreview.cs`)

| # | Target Filename | UI Surface | Scene Path | Key Visual Elements |
|---|---|---|---|---|
| 1 | `title_view.png` | Main Menu Title | `res://scenes/ui/main_menu.tscn` | SmileySans title typography, bio-fluorescent hover styling (`menu_buttons.tres`), macrophage cell model, dark-field background bokeh |
| 2 | `class_view.png` | Class Selection | `res://scenes/ui/class_view.tscn` | Immune defense cell dossier, cell selection buttons, baseline vitals, innate skill icon |
| 3 | `loadout_view.png` | Organelle Chamber | `res://scenes/ui/loadout_view.tscn` | Sockets, ATP energy capacity, organelle inventory backpack, stats delta readout |
| 4 | `passive_view.png` | Epigenetic Talent Tree | `res://scenes/ui/passive_view.tscn` | Branching tree nodes, fluorophore allocation status, profile tabs |
| 5 | `map_view.png` | Map Selection & Scanner | `res://scenes/ui/map_view.tscn` | Holographic body scanner, organ sites, difficulty selector, hazard chips |
| 6 | `gallery_all.png` | Achievement Gallery (All) | `res://scenes/ui/achievement_gallery.tscn` | Unlocked fluorophore badges, locked cards, progress counters, scroll list |
| 7 | `gallery_locked.png` | Achievement Gallery (Locked) | `res://scenes/ui/achievement_gallery.tscn` | Filtered locked achievements, unachieved grayscale variants |
| 8 | `endgame_setup.png` | Endgame Affliction Setup | `res://scenes/ui/endgame_setup_modal.tscn` | Overdrive afflictions, modifier cards, confirm/cancel buttons |
| 9 | `codex_modal.png` | Microscopic Codex | `res://scenes/ui/codex_modal.tscn` | Encyclopaedic dossier tabs (Skills, Cells, Pathogens, Maps) |
| 10 | `settings_modal.png` | System Settings | `res://scenes/ui/settings_modal.tscn` | Audio volume sliders, display resolution toggles, keybinds |
| 11 | `run_records.png` | Run History Records | `res://scenes/ui/run_records_modal.tscn` | Medical charts, tactical review of past runs, best survival records |
| 12 | `toast_banner.png` | Toast Banner | `res://scenes/ui/toast_banner.tscn` | Component banner, icon, localized header and description |
| 13 | `achievement_toast.png` | Achievement Unlock Toast | `res://scenes/ui/achievement_toast.tscn` | Settled slide-down tween banner, golden trophy reward motif |
| 14 | `upgrade_modal.png` | Upgrade Mutation Choice | `res://scenes/ui/upgrade_modal.tscn` | 3 upgrade choice cards, rarity tier badges, uniform card bounding dimensions |
| 15 | `hud_hp.png` | In-Game Combat Arena & HUD | `res://scenes/ui/hud.tscn` & `main.tscn` | HP bar, kill count, active weapon skill slots, cytoplasm Catmull-Rom deformation |

---

## 2. Active Skill Visual Effects (Live in Combat Arena)

| # | Target Filename | Skill Name | Source Script | Visual Effect Captured |
|---|---|---|---|---|
| 16 | `skill_mac_mine.png` | Complement Cascade | `ComplementCascadeSkill.cs` | Spawns Membrane Attack Complex (MAC) assembly mine in combat arena |
| 17 | `skill_mac_blast.png` | Complement Cascade Blast | `ComplementCascadeSkill.cs` | High-energy concentric detonation shockwave destroying pathogen membranes |
| 18 | `skill_antibody_missile.png` | Antibody Salvo | `AntibodySalvoSkill.cs` | Y-shaped guided immunoglobulin missiles tracking bacterial targets |
| 19 | `skill_perforin_lance.png` | Perforin Lance | `PerforinLanceSkill.cs` | High-intensity bio-laser lance beam and glowing cylindrical membrane pore decals |
| 20 | `skill_grasp_chain.png` | Phagocytic Grasp | `PhagocyticGraspSkill.cs` | Extended amoebic pseudopod chain visual tethering and dragging target bacteria |
| 21 | `skill_ros_torrent.png` | ROS Torrent | `RosTorrentSkill.cs` | Electric cyan reactive oxygen species (ROS) jet stream spraying at pathogens |

---

## 3. Interactive Capture Recipes (godot-ai MCP)

For targeted inspection of specific scenes during active development:

```json
// Example: Open and capture Upgrade Modal in 2D viewport
scene_open(path="res://scenes/ui/upgrade_modal.tscn")
editor_screenshot(source="viewport_2d", max_resolution=0, user_prompt="Upgrade Modal Card Layout")

// Example: Run game and capture running combat arena
project_run(mode="main")
editor_screenshot(source="game", max_resolution=0, user_prompt="Live Combat Arena Framebuffer")
project_manage(op="stop")
```
