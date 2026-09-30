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
  - Replaced unicode emoji icons (`💨`, `🧬`, `🧪`) in Level-Up Mutation choice cards with actual confocal bio-fluorescent sprites from `assets/gen/skill/` and `assets/gen/gear/`.
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
  - New `CapillaryTissueLayer` (`scripts/map/vfx/CapillaryTissueLayer.cs`,
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

---

## [TODO] Phase 7: Engine generalization (domain-decoupled renames + data-driven)

Goal: `Game.*` engine knows only ids/stats/tags/timings; all phagocyte names live in data + locale + art. New hero/enemy/skill/stage = new JSON row, never a new `.cs`.

- [x] **7.0 Baseline + decisions (done 2026-09-28)**: root `Game.*`, full rewrite with no backward compat (JSON ids may change, saves may break), all domains archetype + data-driven (specials extend archetypes). Baseline `scripts/*.cs` banned-token hits = 878 total (`Phagocyte\.`: 449, `BaseCell`: 68, `BaseEnemy`: 100, `Pathogen`: 194, `Fibrin`: 11, `Opson`: 38, `Macrophage`: 23); build 0 warnings, arch 0 violations.
- [x] **7.1 Mechanical namespace + folder rename (done)**: `Game.*` root; `player/`, `stages/`, `equipment/`, `endgame/` folded into `directors/`; `.tscn` paths + `check_arch.py` prefixes updated; build 0 warnings, arch clean, `TestMapEnvironments` green.
- [x] **7.2 Core type renames (done)**: `PlayerActor/EnemyActor/ActorStats/BodyDeformation/HazardZone/DamageService/EnemySpawner/SwarmRenderer/StageSystem/NeutralPropManager/EndlessDirector/RunMutatorService/EnemyProjectile/BeamGlow/Equipment*/RadarChart/StageSelectMap/HealthBar/GameRoot` + `enemies`/`neutral_props` groups; file + test-class renames match; build 0 warnings, arch clean, `TestStageEnvironments` + `TestTelegraphAndProjectiles` green.
- [x] **7.3 Collapse subclasses into data (done)**: heroes → `classes.json` + `PlayerActor` (5 files/scenes deleted); stages → `maps.json` effects + `StageEnvironment` (5 files deleted); enemies → `enemies.json` + `EnemyActor` traits (30 files deleted); skills → `active.json`/`passive.json` + 7 archetype executors (30 files + `ros_jet.tscn` deleted). Build 0 warnings, arch clean, all domain suites green.
- [x] **7.4 Strings/assets decoupling (done)**: `die_hero` → `die_player` (file+manifest+code, reimported); `characters/` → `actors/player_base.tscn`; `classes/maps/gear/pathogens/bosses.json` → `player_classes/stages/equipment/enemy_codex/boss_codex.json`; `GetCellScene/GetClassInfo/GetMapInfo/MapData/ClassData/MapId` → `GetPlayerScene/GetPlayerClass/GetStageInfo/StageData/PlayerClassData/StageId`; `PATHOGEN_BASE` → `ENEMY_BASE`; audio + asset checks green.
- [x] **7.5 Gates + docs (done)**: `AGENTS.md` Phase-0 rule, `ARCH_RULE.md` layers, `GUIDE.md` hero/skills/combat/enemy/director/appendix sections updated; build 0 warnings, arch clean, banned-token grep zero in `scripts/`, asset/audio checks green.
- [x] **7.6 Test-fix pass (done)**: former known-reds fixed — `TestCheats` helper moved to `scripts/testing/CheatTools.cs` (`Game.Testing`, sweep no longer probes it), `TestDifficultyTracks` opts into `MapEffectsEnabled`, `TestGearBalance` opener re-pinned (`phagolysosome_core` might 0.15 → 0.12); flakes hardened — `TestSurvivorHudUx` resets telemetry in isolation, `TestCardUniformSize` gained its `SUCCESSFULLY` footer; orphan `organelle_slot.tscn` deleted; stale `check_arch.py` map fixed. Full sweep **52/52 green, 0 FAIL** (3 suites use non-standard footers: Expanded/Multilingual/RunRecords — all verified passing).
- [x] **7.7 map→stage vocabulary (done)**: `StageSelectMap.cs`→`StageSelectView.cs`, `map_view.tscn`→`stage_view.tscn`, `TestMapLockUi.cs`→`TestStageLockUi.cs`, `docs/map.md`→`docs/stages.md`; engine API unified (`SelectedStage`, `IsStageUnlocked`, `SelectStage`, `StageEffectsEnabled`, `PlayStageBgm`, `RecordStageClear`, `BuildStages`, data keys `stage_id`/`unlock_stage`, 19 `achievements.json` keys). Scene node names, `map_locked` meta, locale keys and history notes intentionally preserved. Full sweep **52/52 green**.
- [x] **7.8 Unified projectile (done)**: `EnemyProjectile.cs` + `SalvoSkill.SalvoProjectile` deleted; all shots are batched `ProjectileManager` structs keyed by `Team{Player,Enemy}` with inline `EffectSpec` hits (stun/mark/agglutination/burn via `IStunnable`/`IAilmentHost`, zero heap alloc) and data steering (homing lock/nearest, chain reacquire, wobble). `PlayerActor` implements `IDamageable` (+`TakeDoTDamage`); both actors gain `IStunnable`/`IAilmentHost`. Data keys normalized (`bounces`→`pierce`, `via_manager`/`stick_time`/`stick_dist` dropped). Full sweep **52/52 green**.
- [x] **7.9 Hazard concept dissolved (done)**: `hazards/` deleted — `ProximityMine.cs`→`scripts/directors/` (`Game.Directors`, dead `enemy_shots` scan removed), `BlockerObstacle.cs`→`scripts/stages/DebrisWall.cs` (`BlockerWall` was taken by fenestra terrain); `HazardZone` × `ZoneSkill.ZoneNode` unified into per-node `Zone.cs` (`Team` target set, `DamageService` + inline `EffectSpec` ticks, grow/pull/mine-nova/tint as data, `SkillRef` back-pointer gone). `hazards` group dropped (no consumers). Full sweep **52/52 green**.
- [x] **7.10 General ailment controller + config pipeline (done)**: `AilmentController` knows no ailment by name — generic `Apply/IsActive/GetTimer/GetStackCount/Clear` + channel conveniences (`ApplySlow`, `ClearChannel`, `SlowTimer`, `HasSlow`); `EffectSpec` switch deleted (stun→`IStunnable`, rest→`Apply`); hit VFX rides the def (`vfx` field, fail-fast at load). Spawner ids moved into data via `tools/config/` (TS SSOT + zod + FK check, vistrace method; pilot: `ailments.json`, `skill/active.json` with uniform `on_hit` rows across salvo/zone/beam/aura). `make check-config` gate added. Full sweep **52/52 green**.

## [TODO] Phase 8: Stat decoupling (host-agnostic profiles + POCO)

Goal: stats work on any host (hero full set, enemy/minion subsets), 1000+ entity budget (AGENTS.md performance rule).

- [x] **8.1 Schema-ized container (done)**: `StatBlock` POCO (L1, no SceneTree) + `StatProfiles.{Full,Enemy,Minion}` + `HasStat`; `ActorStats` thin Node adapter (signals) delegating 1:1, zero behavior change. `IStatHost.RemoveScaledModifier` returns bool (silent-absent preserved).
- [ ] **8.2 Enemy adoption**: `EnemyActor` holds enemy-profile `StatBlock`; delete `MaxHealth/FloatSpeed/Armor` fields, fix spawner/traits/tests forward (`TestDifficultyTracks`, `TestEndlessMode`, `TestTerminalBosses` et al.).
- [ ] **8.3 Ailment tick inline**: fold `AilmentController._PhysicsProcess` into `EnemyActor._PhysicsProcess` (kill the per-entity node callback).

## [TODO] Phase 9: PoE-style damage pipeline (PLAN.md migration)

Goal: snapshot at cast (`HitPayload`), dumb flight (`ProjectileManager`), smart hit (`DamagePipeline`). Source: `PLAN.md` (5 phases).

- [ ] **9.0 Baselines (decided)**: capture `benchmarks/` numbers first. Locked: 3-slot `EffectSpec` stays (amend PLAN.md); `Team` gains `Neutral = 2`; `EmitterId` → `AttackerId` rename-direct during migration; zero-alloc via benchmark observation, not asserts; confirm `check-config` already FK-checks skill→ailment refs before adding boot validation.
- [ ] **9.1 Contracts**: `HitPayload` (~40B: raw damage, damage type, hit flags incl. cast-rolled `IsCrit`, faction, armor pen, ailment mult, knockback impulse, effect specs, attacker id) + `HitResult` + `DamageType`/`HitFlags`, co-located in `DamageService.cs`. Crit moves from `DealDamage` RNG roll to cast-time snapshot.
- [ ] **9.2 Pipeline**: new `DamagePipeline.ResolveHit(payload, target)` (mitigation → ailment derivation vs max-HP threshold → status dispatch → leech via attacker id). Migrate the 16 `DealDamage` call sites one archetype at a time (projectiles → beam/aura/nova/zone/strike → DoT/traits), deleting old paths per archetype. Actors keep HP/death/EXP/VFX. Deletes `ILeechable` (bridge from emitter work — zero-residual grep) and `ISlowable` (route via status dispatch).
- [ ] **9.3 Dead-owner pruning**: flat registry fed by `Die()`; prune adapted to `activeSlots` indirection (PLAN.md snippet assumes direct swap-remove); fizzle visual = new `VfxType` row, no new code.
- [ ] **9.4 Player grace**: `Downed` state + 1.0s timer on `PlayerActor`; victory intercept vs `BossEncounterManager`/`RunSettlementService` before `RunRecordsModal`. (Player bullets already survive death via emitter ids.)
- [ ] **9.5 Lockdown + AGENTS.md**: `check_arch.py` asserts no `ILeechable`/`ISlowable` refs, `ResolveHit` callers limited to combat layers, no per-tick dict lookups in `ProjectileManager`. Apply the payload-vs-pipeline rule to `AGENTS.md`:
  > **Damage payload vs pipeline rule:** `HitPayload` carries only what is **frozen at cast time** (raw damage, type, pen, crit flag, faction, attacker id, knockback impulse, effect specs). `DamagePipeline` reads only what is **live at hit time** (armor/resists/shields, evasion/block, HP thresholds, attacker stats for leech). Test for a new field: *"could this change between fire and impact?"* No → payload; yes → pipeline. Flight/render state (`Position`, `ProjectileTypeIndex`, lifetime) lives **beside** the payload in `ProjectileData`, never inside it. Payload is value-types only, ~40B budget — a new field must justify its bytes and migrate all spawner call sites. Resolution (`InstanceFromId`, status slots, catalogs) happens **on hit only**, never per-tick.
- Each sub-phase: build 0 warnings, arch clean, affected suites + full sweep green before next.