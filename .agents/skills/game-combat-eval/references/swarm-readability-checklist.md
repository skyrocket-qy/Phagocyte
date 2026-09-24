# Swarm Visual Readability & Cognitive Ergonomics Checklist (Phagocyte)

## Overview

When 300 to 500 active pathogen entities, dozens of projectiles, and floating damage numbers occupy the screen simultaneously, visual chaos can easily overwhelm the player.

This checklist provides concrete inspection criteria to ensure high visual readability, clear threat prioritization, and photosensitivity safety during intense combat encounters.

---

## 1. Player Cell Silhouette & Core Contrast

- [ ] **Nucleus Visibility**: The cell nucleus (DAPI blue or vibrant core) must maintain high luminance contrast against the darkest and brightest pathogen clusters.
- [ ] **Plasma Membrane Outlining**: An emissive membrane rim shader or procedural outline prevents the player from blending into dense clusters of surrounding bacteria.
- [ ] **Invulnerability / Dodge Roll Indication**:
  - Clear visual feedback during dash/dodge roll (e.g. transient chromatic dispersion, after-image trail, or alpha pulse).
  - Clear recovery state transition indicating when vulnerability resumes.
- [ ] **Hitbox Clarity**: The cell's vulnerable core must match its collision shape, ensuring near-misses feel fair and deliberate.

---

## 2. Threat Hierarchy & Fluorophore Color Language

| Threat Tier | Visual Language | Color Standard | Audio Cue |
|:---|:---|:---|:---|
| **Tier 0: Debris / Neutral** | Translucent, low opacity, slow Brownian drift | Dim Gray / Neutral Brown | Subtle squish |
| **Tier 1: Swarm / Drifter** | Small micro-entities, semi-translucent bodies | Low-intensity Green / Ochre | Soft chitter |
| **Tier 2: Fast Invader / Charger** | High motility, elongated flagella, directional stretch | Amber / Electric Cyan | High-pitch whoosh |
| **Tier 3: Elite Pathogen** | 1.5x–2.0x scale, pulsing fluorescent aura, high opacity | Vibrant Magenta / Neon Purple (`#FF007F`) | Deep resonant rumble |
| **Tier 4: Sub-Boss / Boss** | Full custom procedural shader, screen-space health bar | Multi-spectral iridescent glow | Unique BGM + Boss roar |

- [ ] **Elite Identification**: Players must be able to spot an Elite pathogen within 200ms of entering the screen.
- [ ] **Telegraphed Attack Cones**: Lethal pathogen attacks (e.g. anthrax spore burst, tetanus toxin projectile, bacillus charge) MUST display clear ground telegraph decals prior to release.

---

## 3. Cognitive Load & Particle Density Bounds

- [ ] **Particle Lifetime & Clamping**: Weapon particle emitters must have strict lifetimes ($\le 0.4\text{s}$) and maximum particle counts to avoid filling more than 25% of the screen area with white noise.
- [ ] **Damage Number Stacking**: When multiple damage ticks occur on the same entity within 100ms, damage values should either consolidate or jitter along a defined upward trajectory to prevent opaque numerical walls.
- [ ] **Screen Shake Thresholds**: Camera trauma decay must drop to zero within 0.3s of impact cessation, preventing continuous disorientation.

---

## 4. Photosensitivity & Accessibility Standards

- [ ] **No Full-Screen White Flashes**: Critical hits, bomb detonations, and level-ups must use localized radial bursts rather than 100% white full-screen flashes.
- [ ] **Screen Shake Toggle**: User must be able to completely disable screen shake in Settings (`SettingsManager.ScreenShake`).
- [ ] **Performance & Visual Reduction Mode**: Low-spec and high-fatigue players can enable Performance Mode to switch off non-essential background fluid distortion and particle blooms.
- [ ] **High-Contrast Text**: All HUD numbers, health percentages, and skill cooldown indicators must meet WCAG AA contrast ratio ($\ge 4.5:1$) against combat backdrops.
