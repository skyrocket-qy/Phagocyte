# Project Phagocyte — Visual Overhaul Roadmap

This roadmap tracks the end-to-end transformation of Phagocyte's visual fidelity from prototype UI to commercial-grade confocal laser microscopy and bio-fluorescence art direction.

---

## [COMPLETED] Phase 1: Modal Scrims, Centering & Layout Defect Fixes

Emergency fixes for visual bugs, text collisions, and broken layout containers across modals:

- [x] **Full-Screen Dark Backdrop Scrims for All Modals**:
  - Add or configure full-viewport backdrop dimming shield (`Color(0.01, 0.02, 0.03, 0.98)`) across `EndgameSetupModal`, `SettingsModal`, and `CodexModal`.
  - Prevent background bleed-through: Eliminated main menu title (`噬血者`) and navigation buttons (`开始免疫行动`, `退出游戏`) from glowing through semi-transparent modal headers.
- [x] **Center `EndgameSetupModal`**:
  - Fixed anchor presets (`layout_mode = 1`, `anchors_preset = 15`) so the affliction selection window is centered in the 1280x720 viewport instead of pinned against the left edge.
- [x] **Fix `CodexModal` Scroll List Clipping**:
  - Fixed left item scroll list layout, button height (36px), and margins so the bottom entries are clean and scrollable without horizontal truncation.
- [x] **Visual Verification**:
  - Re-ran `TestFullVisualPreview.cs` to capture `tmp/visual_after/`.
  - Executed `capture_diff.py` to generate side-by-side before/after comparison composites for all touched screens.

---

## [TODO] Phase 2: Emoji Purge & Contextual Sprite Wiring

Eliminating all programmer-art placeholders and restoring confocal microscopy sprite fidelity:

- [ ] **Purge OS Emojis from `UpgradeModal`**:
  - Replace unicode emoji icons (`💨`, `🧬`, `🧪`) in Level-Up Mutation choice cards with actual confocal bio-fluorescent sprites from `assets/gen/skill/` and `assets/gen/organelle/`.
- [ ] **Fix Card Title Truncation in `UpgradeModal`**:
  - Eliminate ugly `...` ellipsis truncation on mutation card headers (`Actin Pseudopod...`, `Mitochondrial...`) by refining font size scaling or two-line subtitle wrapping.
- [ ] **Add Specimen Artwork to `CodexModal`**:
  - Add illustrated fluorophore specimen portraits in the right-hand dossier panel (skill icon, pathogen microscopy render, cell ultrastructure) instead of dry walls of plain text.

---

## [TODO] Phase 3: High-Fidelity UI Reskin & Cyber-Microscopy Styling

Elevating screens from flat wireframes to high-tech immunobiology interfaces:

- [ ] **Class Selection (`ClassView`) Overhaul**:
  - Replace raw text stat lists (`最大生命 140`, `护甲 10`) with animated polygonal bio-radar charts (Vitals, Motility, Armor, Phagocytic Reach).
  - Add illuminated specimen containment brackets and cellular classification holograms.
- [ ] **Passive Talent Tree (`PassiveView`) Organic Rework**:
  - Replace the 6 rigid rectangular coordinate boxes with organic epigenetic chromatin fiber networks, DNA methylation hubs, and pulsing fluorophore node connectors.
- [ ] **Organelle Chamber (`LoadoutView`) Polish**:
  - Upgrade the 4 static socket frames with glowing bio-energy rings and animated ATP power pips.
  - Redesign bottom action buttons with `menu_buttons.tres` cyber-fluorescence styling.

---

## [TODO] Phase 4: Combat Atmosphere, Laser Glow & Shader Polish

Transforming the in-game action into a visceral microscopic bio-horror battlefield:

- [ ] **Visceral Capillary & Tissue Arena Floor**:
  - Enhance arena background with layered microvascular tissue depth, dynamic fluid currents, and drifting out-of-focus RBCs with realistic lens dispersion.
- [ ] **Confocal Laser Illumination & HDR Bloom**:
  - Boost emission intensity on weapon skills (Perforin Lance, ROS Torrent, Grasp chains, MAC Detonations) with piercing laser fluorophore halos.
- [ ] **Organic HUD Health & Vitals Overhaul**:
  - Replace flat green health bar with a pulsating cellular membrane vital sign gauge that reacts to damage and adrenaline surges.
