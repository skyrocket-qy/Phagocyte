# Phagocyte Visual Quality Evaluation Checklist & Rubric

This reference provides the comprehensive rubric and evaluation criteria for visual inspections, screenshot audits, and UI/shader quality reviews in Project: Phagocyte.

All visuals in Phagocyte adhere to the **Confocal Laser Scanning Microscopy & Microscopic Bio-Fluorescence** aesthetic (暗視野顯微鏡 / 共軛焦螢光顯微鏡視覺體系).

---

## 1. Aesthetic & Color Language Fidelity

Phagocyte's visuals simulate looking through an advanced fluorescence microscope. Every element must respect the optical and chromatic physics of this setting:

| Fluorophore / Color Channel | Hex Range | Meaning & Usage | Acceptable Appearance | Failure Modes |
|---|---|---|---|---|
| **Deep Void Black** | `#000000` ~ `#080c14` | Specimen fluid / dark-field substrate | Deep inky black, slight subtle lens vignetting at edges | Gray washed background, milkiness, visible bounding box rectangles |
| **GFP Emerald Green** | `#00FF88` / `#2ECC71` | Viability, active enzymes, Macrophage cytoplasm, benign cellular structures | Luminous emerald glow, HDR intensity > 1.0 on outer membrane rim | Dull flat olive, yellowish desaturated murky green |
| **RFP Crimson / Carmine** | `#FF3366` / `#E74C3C` | Pathogens, lytic enzymes, perforin drill beams, danger indicators | Piercing neon crimson with bloom halo | Washed pink, mud-orange, flat opaque red |
| **DAPI / Hoechst Electric Blue** | `#00D2FF` / `#3498DB` | Cell nuclei, DNA/chromatin strands, NETosis traps, UI interactive states | Crisp electric cyan/blue, phosphorescent glow | Muted grayish denim, oversaturated purple |
| **YFP / FITC Amber Gold** | `#FFD700` / `#F39C12` | Opsonin markers, ATP currency, achievements, elite buffs | Warm incandescent gold with bright core | Dirty mustard, brown, flat canary yellow |

### Microscopic Atmosphere Checks
- [ ] **2D WorldEnvironment Glow**: Emissive elements have an HDR bloom fringe (`Environment.GlowEnabled == true`, Canvas mode).
- [ ] **Cytoplasm Gel Shader**: Macrophage body has semi-transparent core (`inner_alpha` ~ 0.55-0.75) with bright Fresnel rim glow (`rim_color` HDR > 1.0).
- [ ] **Parallax Depth-of-Field (DoF)**: Background RBCs and foreground bokeh disks appear naturally out-of-focus (gaussian blurred) and drift according to camera velocity.
- [ ] **Lens Post-Processing**: Outer borders exhibit subtle radial vignette and gentle chromatic aberration (color fringe at extreme edges). No excessive blur that hinders UI legibility.

---

## 2. UI Layout, Spacing & Typography

### Typography & Font Rendering
- [ ] **No Missing Glyphs (Tofu)**: All English and Simplified/Traditional Chinese text (`assets/fonts/NotoSansSC-Body.ttf`, `Orbitron-Display.ttf`, `SmileySans-Title.ttf`) render crisp without empty boxes `□` or unexpected system font fallback.
- [ ] **Text Overflow & Truncation**: Card titles, badge chips, and descriptions fit cleanly within their allotted containers without clipping or spilling over borders.
- [ ] **Font Hierarchy**: Titles use display/title font with distinct sizing (18-24px); body labels use clean readable sans-serif (13-16px); numerical chips use monospaced or tabular numbers.

### Alignment & Spatial Uniformity
- [ ] **Uniform Card Dimensions**: Upgrade cards, achievement cards, and codex entries in horizontal or grid containers must maintain 100% identical minimum bounding boxes regardless of short vs. long text descriptions.
- [ ] **Modal Centering**: Modals (`EndlessSetupModal`, `UpgradeModal`, `CodexModal`, `SettingsModal`) are centered in the viewport with a dark tinted backdrop shield (`Color(0, 0, 0, 0.7)`).
- [ ] **Consistent Touch & Click Padding**: Interactive buttons have adequate internal margins (minimum 8px vertical, 14px horizontal) and at least 8-12px clearance from neighboring controls.
- [ ] **Theme Uniformity**: Buttons follow `assets/theme/menu_buttons.tres` (dark blue-gray normal state, emerald/cyan hover border glow, pressed reaction). Modals maintain matching border styling (`Color(0.25, 0.45, 0.6, 0.7)`).

---

## 3. Asset & Sprite Quality in Context

When inspecting sliced and runtime assets rendered inside UI slots or the game arena:

### Boundary & Keying Quality
- [ ] **No Alpha Edge Halos**: Sprites keyed against black background have Telea inpainting applied on transparent boundary pixels; no white or gray outline fringing.
- [ ] **No Border Clipping**: Biological specimen structures do not touch or clip against the outer bounding box of the texture (minimum 6-10% padding margin).
- [ ] **Clean Transparent Corners**: All 4 corners of any isolated circular or organic sprite are completely transparent (`alpha == 0`).

### Icon & Variant Rendering
- [ ] **No Fallback Placeholders**: No magenta default textures or unassigned blank rectangles appear in active slots.
- [ ] **Achievement States**: Unlocked badges show vivid full-spectrum fluorophores; locked/unachieved badges show the desaturated, darkened `*_unachieved.png` variant.
- [ ] **Texture Filtering & Sharpness**: In-game icons remain crisp at native 1x scale without bilinear smearing or pixelated jagged scaling artifacts.

---

## 4. Animation & Dynamic Visual States

- [ ] **Tween Settling**: Toast banners (`ToastBanner`, `AchievementToast`) slide down smoothly from the top viewport margin and come to a resting position without jitter or clipping.
- [ ] **Spring Dynamics**: The Macrophage nucleus visibly lags behind cell acceleration and rebounds smoothly when changing directions.
- [ ] **Membrane Elasticity**: Cytoplasm polygon maintains smooth organic contours via Catmull-Rom interpolation (128 vertices) without sharp polygonal creases.
- [ ] **Breathing Oscillation**: Pathogen sprites oscillate gently in scale ($0.95 \times \sim 1.05 \times$) to convey biological vitality.

---

## 5. Visual Defect Diagnostic Matrix

| Symptom | Probable Root Cause | Fix Recipe |
|---|---|---|
| **White/gray halo around sprite** | Floodfill keying did not inpaint edge boundary pixels | Re-run `tools/to_target_asset/main.py --no-clean --category <cat>` with edge inpainting enabled |
| **Card sizes differ in upgrade modal** | Dynamic label text causes auto-sizing without fixed `custom_minimum_size` | Enforce container `custom_minimum_size` or verify `TestCardUniformSize.cs` assertions |
| **Chinese characters show as □** | Font resource missing Chinese glyphs or fallback font not chained | Check `Theme` font overrides; use `NotoSansSC-Body.ttf` or `SmileySans-Title.ttf` |
| **Headless capture produces black PNG** | Running `Godot --headless` with viewport capture | Must run headed Godot or use `editor_screenshot(source="game")` via godot-ai MCP |
| **Glow effect missing in viewport** | `WorldEnvironment.Environment.GlowEnabled` false or camera background not Canvas | Verify `WorldEnvironment` in scene; verify HDR colors have component values > 1.0 |
| **Modal controls bleed offscreen** | Anchor presets or container min sizes exceed base resolution ($1280 \times 720$) | Set anchors to center / adjust container max-width constraints |
