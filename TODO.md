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

## [DONE] Phase 3: High-Fidelity UI Reskin & Cyber-Microscopy Styling

Elevating screens from flat wireframes to high-tech immunobiology interfaces:

- [x] **Class Selection (`ClassView`) Overhaul**:
  - Replaced raw text stat lists with animated polygonal `BioRadarChart` (Vitals, Motility, Armor, Special Trait), featuring concentric cyber-fluorescent grid webs, glowing polygons, and vertex pips.
  - Added illuminated microscopy specimen containment brackets, 方案 B asymmetric chamfer (`12, 3, 12, 3`), and glowing active button highlights in `ClassList`.
- [x] **Passive Talent Tree (`PassiveView`) Organic Rework**:
  - Replaced 6 rigid rectangular coordinate boxes with organic Epigenetic Chromatin Networks (breathing territorial wash + sinusoidal chromatin micro-filaments).
  - Added DNA Methylation / Histone Octamer Hubs with multi-ring bio-respiration pulsing halos.
  - Enhanced active edge lines (`DrawEdges`) with multi-stage fluorophore excitation trails and harmonic electron pulses.
- [x] **Organelle Chamber (`LoadoutView`) Polish**:
  - Upgraded the 4 static socket frames into double-ring bio-energy rails with 方案 B asymmetric chamfers (`14, 4, 14, 4`) and cyan outer glow.
  - Upgraded `EnergyPips` with dynamic ATP glowing halos, concentric rims, and specular photon excitation cores.
  - Added flowing ATP energy pulses along microtubule channels in `ChamberLinks`.
  - Unified category tabs and profile buttons with 方案 B cyber-fluorescence styling.

---

## [COMPLETED] Phase 4: Combat Atmosphere, Laser Glow & Shader Polish

Combat visuals verified live (godot-ai headed screenshots) plus headless suites
(`TestVisualOverhaul`, `TestSkillVisuals`, `TestSurvivorHudUx`,
`TestMapEnvironments`, `TestPhagocyticGrasp` green; build zero warnings):

- [x] **Visceral Capillary & Tissue Arena Floor**:
  - New `CapillaryTissueLayer` (`scripts/environment/CapillaryTissueLayer.cs`,
    seeded 9-vessel branching network with lumen/wall/sheen + drifting
    fluid-current dashes, 0.55x camera parallax) wired into `main.tscn` as
    `Background/CapillaryTissue` between the tissue shader and the RBC plane.
  - `MicroscopeParallax` gained a sinusoidal fluid-current field
    (`CurrentStrength`) and red/cyan lens-dispersion fringe rims on RBCs.
- [x] **Confocal Laser Illumination & HDR Bloom**:
  - `Environment_main` glow boosted (`glow_intensity` 0.85, `glow_bloom` 0.35,
    HDR threshold 0.85); new shared `LaserGlow` helper
    (`scripts/combat/LaserGlow.cs`: halo + mid + white-hot core beam passes,
    impact halos) used by lance, ROS jet, MAC blast and granzyme burst.
- [x] **Organic HUD Health & Vitals Overhaul**:
  - New `MembraneGauge : ProgressBar` (`scripts/ui/hud/MembraneGauge.cs`)
    on both `hud.tscn` HP bars (paths unchanged): breathing pulse, white
    damage flash on HP drops, red adrenaline surge rim below 30% HP,
    per-instance fill stylebox; `VitalsView` untouched.


- [x] **每個skill視覺設計要跟他的asset圖一樣 (All 17 Active & Innate Skills Visual Alignment)**:
  - Extended `SkillAssetPalette` (`scripts/skills/SkillAssetPalette.cs`) with saturated accent & white-excitation core colors for all 17 skills.
  - Aligned shape languages, kinetic behaviors, and shader/glow rendering to canonical 256x256 icon artwork:
    - `perforin_lance`: Confocal laser lance (`#39c06a`) with pore puncture decals.
    - `ros_torrent`: Hydrodynamic jet spray (`#2bd2b9`) with bubbling oxidative stress particles.
    - `complement_cascade`: Electric sky-blue (`#25a4e2`) MAC pore assembly and 6-petal lysis blast.
    - `granzyme_detonation`: Bioluminescent orange (`#e78c41`) caspase shockwave detonation.
    - `antibody_salvo`: Dual-pronged bio-cyan (`#26afc0`) Y-shaped immunoglobulin missiles.
    - `nuclease_blades`: Triple rotating emerald (`#41c471`) crescent scythe blades with nucleotide shards.
    - `defensin_barbs`: Sharp crystalline peptide needles (`#4de892`) with cationic barb thorns.
    - `pro_inflammatory_arc`: Amber-gold (`#e3a638`) branching cytokine electric discharge arcs.
    - `exosome_singularity`: Bio-cyan (`#22add4`) vesicular vortex with central void and lipid arms.
    - `phagolysosome_vent`: Corrosive crimson (`#ed4543`) 16-lobed organic puddle with enzymatic fizzing.
    - `mhc_tracer_beam`: Emerald (`#1dc457`) dual laser scanner with molecular targeting reticle.
    - `histamine_surge`: Golden-amber (`#e3a638`) degranulation wave with expelled mast granules.
    - `nitric_oxide_halo`: Cyan (`#4ccae3`) fluctuating multi-ring gas aura with Brownian fringe.
    - `interferon_wave`: Deep cyan (`#1aabc6`) multi-harmonic acoustic pressure ripple.
    - `lysozyme_ricochet`: Cobalt-blue (`#1c6fdc`) globular catalytic protein capsule with velocity streaks.
    - `phagocytic_grasp`: Amoeboid pseudopod with terminal phagosomal cup clamp jaws.
    - `pseudopod_lunge`: Dense amoebic punch fist with triple knuckle lobes and kinetic impact wave.
  - Test suites updated: `TestSkillVisuals.cs` verifies all 17 skills headless, and `TestFullVisualPreview.cs` captures headed pixel-perfect viewports into `tmp/visual_after/`.

---

## [TODO] Phase 5: Audio Coverage & Per-Map BGM

Finish the audio pass (P0 wired: boss BGM switch, wave/game-start stingers, dodge/equip/error/achievement sounds; manifest + boot check + `TestAudioAssets` + check-assets step 7 all live):

- [x] **Dedicated player_hit SFX**: `PlayPlayerHit` now plays `player_hit.wav` (synthesized hurt thump, `assets/audio/sfx/`), manifest enforces existence.
- [x] **Wire button click sounds**: restored `PlayClick` (`ui_click`) + idempotent `AudioManager.WireClicks(root)` hooked to MainMenu (incl. class/map lists, settings, loadout), Hud (pause/upgrade), and UpgradeModal draft/swap cards.
- [x] **Per-map battle BGM**: `AudioManager.PlayMapBgm` table (wound→battle_bgm, alveolar→echoes_of_the_aether, hepatic→swamp, gastric→crypt, bbb→dimension); run start + post-sub-boss use it, endless terminal lockdown plays `boss_final`, endless overdrive plays `battle_bgm_2`. All six tracks added to the manifest.

---

## [COMPLETED] Phase 6: 360° Game Quality Evaluation Framework

Holistic quality assurance system unifying tactile combat feel, build synergies, biological fidelity, confocal microscopy aesthetics, and 500-entity horde performance:

- [x] **`game-combat-eval` Skill** (`.agents/skills/game-combat-eval/`):
  - Full workflow and scoring manual (`SKILL.md`).
  - Tactile kinesthetics & juice rubric (`references/combat-juice-rubric.md`): Hit-stop micro-pauses (30-60ms), quadratic camera trauma ($\text{Shake} = \text{Trauma}^2$), zero-allocation struct-pooled floating combat text (`DamageNumberSpawner`), and 3-band audio frequency staging.
  - Swarm visual readability & cognitive ergonomics checklist (`references/swarm-readability-checklist.md`).
  - Automated Python combat audit CLI (`scripts/audit_combat_feel.py`): Achieved CQS 100.0/100.0 (Grade A+).
- [x] **`game-balance-eval` Skill** (`.agents/skills/game-balance-eval/`):
  - Full workflow and balancing manual (`SKILL.md`).
  - Canonical 5-Archetype build matrix (`references/build-archetypes.md`): ROS Melt, Cytotoxic Sniper, Engulf Tank, Complement Network, Antibody Swarm.
  - Balance & scalability rubric (`references/balance-rubric.md`): 5-class win-rate parity (<15% spread), anti-monopoly/zero dead items, asymptotic diminishing returns caps.
  - Automated Python build balance audit CLI (`scripts/audit_build_balance.py`): Achieved BBI 100.0/100.0 (Grade A+).
- [x] **Automated 500-Horde Performance Benchmark (`TestHordeBenchmark.cs`)**:
  - Stress tests active horde tiers (100, 300, 500 entities) with weapon discharge and MultiMesh GPU batching.
  - Measures average FPS, 1% Low FPS, and maximum frame-time spikes across 180+ frames.
  - Headless execution verified: Tier 1 (147.6 FPS), Tier 2 (141.6 FPS), Tier 3 500-Pathogen Swarm (140.9 FPS, 1% Low 92.2 FPS, 203 batched entities).