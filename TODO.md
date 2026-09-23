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

## [COMPLETED] Phase 2: Emoji Purge & Contextual Sprite Wiring

Eliminating all programmer-art placeholders and restoring confocal microscopy sprite fidelity:

- [x] **Purge OS Emojis from `UpgradeModal`**:
  - Replaced unicode emoji icons (`💨`, `🧬`, `🧪`) in Level-Up Mutation choice cards with actual confocal bio-fluorescent sprites from `assets/gen/skill/` and `assets/gen/organelle/`.
- [x] **Fix Card Title Truncation in `UpgradeModal`**:
  - Eliminated ugly `...` ellipsis truncation on mutation card headers (`Actin Pseudopod...`, `Mitochondrial...`) with two-line word-smart wrapping (`autowrap_mode = 3`, `text_overrun_behavior = 0`, font size 14px).
- [x] **Add Specimen Artwork to `CodexModal`**:
  - Added illustrated fluorophore specimen portraits in the right-hand dossier panel (skill icons, cell evolution portraits, pathogen threat icons, and tissue clear badges) with dedicated microscope specimen frame styling.
- [x] **Visual Verification**:
  - Re-ran `TestFullVisualPreview.cs` and generated before/after diffs (`diff_upgrade_modal.png`, `diff_codex_modal.png`). All 3 cards retain 100% identical dimensions in `TestCardUniformSize`.

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


- 如果我先裝-能量再裝能量到超多6，這時我拿掉-能量的裝備會有問題