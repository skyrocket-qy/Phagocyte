# Phagocyte Visual Surface Capture Catalog

This catalog documents all capturable visual surfaces in Project: Phagocyte, detailing the scene path, the **godot-ai MCP recipe** (interactive or runtime capture), the **test harness stage** (for automated headless/headed batch captures), and key evaluation targets.

---

## Standard Directory Structure
All captures must be saved under the project-local `tmp/` directory:
- `tmp/visual_before/`: Baseline captures before making changes
- `tmp/visual_after/`: New captures after changes are implemented
- `tmp/visual_diff/`: Generated 3-column composites (`diff_<surface>.png`)

---

## Surface Index & Capture Recipes

### 1. Main Menu / Title View
- **Scene**: `res://scenes/ui/main_menu.tscn` (includes `title_view.tscn`)
- **Output Filename**: `title_view.png`
- **MCP Capture Recipe**:
  ```json
  // 1. Run the main menu scene
  project_run(mode="custom", scene="res://scenes/ui/main_menu.tscn")
  // 2. Capture when live
  editor_screenshot(source="game", max_resolution=0, user_prompt="Main Menu Title Screen")
  // 3. Stop
  project_manage(op="stop")
  ```
- **Harness Equivalent**: `TestAchievementPreview.cs` (Stage 1)
- **Visual Targets**: Bio-hazard title typography (`SmileySans`), glow on start buttons, menu button styling (`assets/theme/menu_buttons.tres`), background microscope ambiance.

---

### 2. Endless Affliction Setup Modal
- **Scene**: `res://scenes/ui/endgame_setup_modal.tscn` (instantiated in `main_menu.tscn`)
- **Output Filename**: `endgame_setup.png`
- **MCP Capture Recipe**:
  ```json
  project_run(mode="custom", scene="res://scenes/ui/main_menu.tscn")
  // Open modal via button input or direct script evaluation
  game_manage(op="input_mouse", params={"event": "button", "position": {"x": 640, "y": 420}})
  editor_screenshot(source="game", max_resolution=0, user_prompt="Endless Affliction Setup Modal")
  project_manage(op="stop")
  ```
- **Harness Equivalent**: `TestAchievementPreview.cs` (Stage 2)
- **Visual Targets**: Modal center alignment, darkened backdrop shield, affliction option toggle buttons, confirm/cancel buttons.

---

### 3. Achievement Gallery (All & Locked Filters)
- **Scene**: `res://scenes/ui/achievement_gallery.tscn` (instantiated in `main_menu.tscn`)
- **Output Filenames**: `gallery_all.png`, `gallery_locked.png`
- **MCP Capture Recipe**:
  ```json
  project_run(mode="custom", scene="res://scenes/ui/main_menu.tscn")
  // Click Achievements button on menu
  game_manage(op="input_mouse", params={"event": "button", "position": {"x": 640, "y": 480}})
  editor_screenshot(source="game", max_resolution=0, user_prompt="Achievement Gallery All")
  // Toggle filter to Locked
  game_manage(op="input_mouse", params={"event": "button", "position": {"x": 800, "y": 120}})
  editor_screenshot(source="game", max_resolution=0, user_prompt="Achievement Gallery Locked")
  project_manage(op="stop")
  ```
- **Harness Equivalent**: `TestAchievementPreview.cs` (Stages 3 & 4)
- **Visual Targets**: Grid layout of achievement cards (`achievement_card.tscn`), badge icon rendering ($256 \times 256$ scaled), unachieved darkened variants, filter tab highlights, scrollbar appearance.

---

### 4. Toast Banner & Achievement Toast
- **Scene**: `res://scenes/ui/toast_banner.tscn`, `res://scenes/ui/achievement_toast.tscn`
- **Output Filenames**: `toast_banner.png`, `achievement_toast.png`
- **MCP Capture Recipe**:
  Can be inspected by opening the component scenes in editor or triggering an achievement event in a running game session.
- **Harness Equivalent**: `TestAchievementPreview.cs` (Stages 5 & 6)
  - Waits 30 frames for the Godot tween slide-down animation to settle.
- **Visual Targets**: Slide-in banner border glow, reward icon scaling, title/desc typography contrast against dark-field background.

---

### 5. In-Game Combat & Survivor HUD
- **Scene**: `res://scenes/main.tscn` (game arena + `res://scenes/ui/hud.tscn`)
- **Output Filename**: `hud_hp.png`
- **MCP Capture Recipe**:
  ```json
  project_run(mode="main")
  // Wait 1-2 seconds for scene ready
  editor_screenshot(source="game", max_resolution=0, user_prompt="Combat HUD and Player Cell")
  project_manage(op="stop")
  ```
- **Harness Equivalent**: `TestAchievementPreview.cs` (Stage 7)
- **Visual Targets**: HP bar gradient & damage flash, active skill cooldown dials, mini-map / radar (if active), cytoplasm gel shader rim on Macrophage, background RBC parallax DoF.

---

### 6. Upgrade / Level-Up Selection Modal
- **Scene**: `res://scenes/ui/upgrade_modal.tscn`
- **Output Filename**: `upgrade_modal.png`
- **MCP Capture Recipe**:
  ```json
  // Open the modal directly in the 2D editor or custom run
  scene_open(path="res://scenes/ui/upgrade_modal.tscn")
  editor_screenshot(source="viewport_2d", max_resolution=0, user_prompt="Upgrade Selection Modal Cards")
  ```
- **Visual Targets**: 3 upgrade option cards, strict uniform card width and height, organelle/skill icon placement, rarity/tier color badges, button hover states.

---

### 7. Class Selection View
- **Scene**: `res://scenes/ui/class_view.tscn`
- **Output Filename**: `class_view.png`
- **MCP Capture Recipe**:
  ```json
  project_run(mode="custom", scene="res://scenes/ui/main_menu.tscn")
  // Navigate to Class Selection
  game_manage(op="input_mouse", params={"event": "button", "position": {"x": 640, "y": 360}})
  editor_screenshot(source="game", max_resolution=0, user_prompt="Class Selection View")
  project_manage(op="stop")
  ```
- **Visual Targets**: Macrophage, Neutrophil, NK Cell cards, stat radar/bars, passive ability description box, start run button.

---

### 8. Passive Talent Tree View
- **Scene**: `res://scenes/ui/passive_view.tscn`
- **Output Filename**: `passive_view.png`
- **MCP Capture Recipe**:
  ```json
  project_run(mode="custom", scene="res://scenes/ui/main_menu.tscn")
  // Click Epigenetic Talent Tree button
  editor_screenshot(source="game", max_resolution=0, user_prompt="Passive Talent Tree Graph")
  project_manage(op="stop")
  ```
- **Visual Targets**: Node connection branches/filaments, allocated vs unallocated fluorophore node states, ATP cost display, node tooltip callout.

---

### 9. Organelle Chamber / Loadout View
- **Scene**: `res://scenes/ui/loadout_view.tscn`
- **Output Filename**: `loadout_view.png`
- **MCP Capture Recipe**:
  ```json
  scene_open(path="res://scenes/ui/loadout_view.tscn")
  editor_screenshot(source="viewport_2d", max_resolution=0, user_prompt="Organelle Loadout Slots")
  ```
- **Visual Targets**: Organelle socket slots, equipped passive organelle badges, inventory grid, stats delta panel.

---

### 10. Map Selection & Codex Modals
- **Scene**: `res://scenes/ui/map_view.tscn`, `res://scenes/ui/codex_modal.tscn`, `res://scenes/ui/settings_modal.tscn`
- **Output Filenames**: `map_view.png`, `codex_modal.png`, `settings_modal.png`
- **Visual Targets**: Map thumbnail art (Acute Wound, Sepsis Stream, Bone Marrow), environmental hazard chips, volume/resolution slider controls in settings.
