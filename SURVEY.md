# FPS Survey — Phagocyte

Date: 2026-09-24
Scope: read-only survey of likely low-FPS causes (GPU fill-rate + per-frame CPU churn).
No code changed.

## Summary

Low FPS is not one hotspot. It is a stack:

1. Full-screen shader + glow/postprocess overdraw every frame.
2. Background layers (`MicroscopeParallax`, `CapillaryTissueLayer`, `CPUParticles2D`) redrawn fully every frame with no culling.
3. Player membrane rebuilt every physics tick, including collision-shape rebake + per-tick allocations.
4. 300–500 concurrent enemies, mostly unbatched, each with physics + steering + tween/flash on hit.
5. 4096-slot projectile loop + QuadTree rebuild + per-bullet queries.
6. Skill/hazard VFX with unconditional `QueueRedraw` + 3-pass laser overdraw.
7. HUD skill bar rebuilt every frame (dictionaries + translations + texture loads).

Counts observed: 69 `_Process`/`_PhysicsProcess` sites, ~120 `QueueRedraw`/`_Draw` sites.

## 1. Full-screen GPU fill-rate stack

- `scenes/main.tscn:19,52,69`:
  - `Background/ArenaBG` is a 4800x4800 `ColorRect` with `microscope_tissue_bg.gdshader`.
  - `WorldEnvironment` glow enabled with 4 levels, `glow_intensity = 0.85`, `glow_bloom = 0.35`, `glow_hdr_threshold = 0.85`.
  - `MicroscopePostProcess/LensOverlay` is a full-viewport `ColorRect` with `microscope_postprocess.gdshader`.
- `shaders/microscope_tissue_bg.gdshader:24-42`:
  - 5x `fbm()` calls per pixel, each 3 octaves x 4 hashes (~60 hash/sin per pixel).
  - `TIME`-animated (`flow_speed`), so it cannot cache; runs fullscreen every frame.
- `shaders/microscope_postprocess.gdshader:14-34`:
  - 3x `screen_texture` taps for chromatic aberration + vignette + per-pixel hash grain.
  - Fullscreen, `TIME`-animated.
- `shaders/cytoplasm_gel.gdshader:59-118`:
  - `screen_texture` refraction sample + 3x3 `vacuoles()` neighbor loop + 2x `noise()` + specular/Fresnel.
  - Forces an extra framebuffer copy for the player cell.
- `shaders/nucleus_sphere.gdshader:29-76`: per-pixel noise + chromatin + nucleolus.
- `project.godot:107`: `renderer/rendering_method="mobile"` combined with glow/bloom + screen-read shaders.
- `scripts/core/SettingsManager.cs:77`: vsync defaults on; missing 60 FPS drops to 30.
- `scenes/ui/hud.tscn:6`: additional fullscreen critical-HP vignette shader.

Effect: even idle, every frame pays tissue FBM + glow/bloom + postprocess + vignette + cytoplasm refraction.

## 2. Background CPU redraw, no culling

- `scripts/environment/MicroscopeParallax.cs:105,148,179`:
  - `_Process` always `QueueRedraw()`.
  - `_Draw` draws 14 RBCs x (5 blur layers + inner circle + 2x 40-segment arcs) + 8 bokeh x 4 layers.
  - No viewport / distance culling; `CurrentField()` does sin/cos per RBC per frame.
- `scripts/environment/CapillaryTissueLayer.cs:76,86,89,103-113`:
  - `_Process` always `QueueRedraw()`.
  - 9 vessels x 3x 22-point wide polylines (width 26–70) + 4 dash circles each.
  - Allocates `new Vector2[] shifted` + `sheenPts` per vessel per frame.
- `scenes/main.tscn:88`:
  - `CPUParticles2D FluidParticles`, `amount = 128`, 4800x4800 emission rect, scale 8–24, CPU-simulated.

Effect: 5x backdrop overdraw (tissue shader + capillary + RBC/bokeh + CPU particles + postprocess).

## 3. Player rebuilt every physics tick

- `scripts/player/BaseCell.cs:556,567,627-656`:
  - `UpdatePseudopodDeformation()` runs every `_PhysicsProcess`.
  - 64x `FastNoiseLite.GetNoise3D` per tick, 2-pass Laplacian, Catmull-Rom 32 -> 128 points.
  - Fresh `new Vector2[32/128/129]`, `new float[32]` x2, `new UV[128]` at 60 Hz.
  - `EngulfCollider.Polygon = points` rebakes `CollisionPolygon2D` every physics frame.
  - `Cytoplasm.Polygon`, `UV`, `Membrane.Points` reassigned every tick.
  - `cell_radius` shader param uploaded every tick (`:641`).
- `scripts/player/BaseCell.cs:662,697`:
  - `UpdateGranules()` per physics tick + `GranuleCanvas.QueueRedraw()`; 24 granules x up to 2 circles.
- `scripts/player/BaseCell.cs:766`:
  - `EngulfArea.GetOverlappingAreas()` array alloc + iterate every physics tick.

## 4. Enemy concurrency 300–500, mostly unbatched

- `scripts/enemies/PathogenSpawner.cs:29,32,42,174`:
  - Caps: normal 300, swarm 450, endless 500; backfill up to 7 per tick.
- `scripts/enemies/PathogenSwarmRenderer.cs:44`:
  - Only `norovirus` and `flu_drift` are GPU-batched; ~20 other species stay per-node.
- `scripts/enemies/BaseEnemy.cs:176,205,225,310,397`:
  - Every enemy runs `_PhysicsProcess`: drift + `EnemySteering.GetDirection()` + `Scale=` update + `Position +=`.
  - `TakeDamage`: `FlashModulate()` -> `CreateTween()`, `QueueRedraw()`, `DamageNumberSpawner.ShowDamage()`, `AudioManager.PlayHit()`, optional VFX.
  - `BeEngulfed`: parallel 3-track `CreateTween` per engulf.
- `scripts/enemies/EnemySteering.cs:167,173`:
  - `FindNearestNeutral()` loops `GetNodesInGroup("senescent_rbc")` per invader per tick after ulceration threshold.
- Subclass `_Draw` cost (arcs 40–64 segments, flagella, hyphae, spikes) x hundreds of visible nodes.

## 5. Projectiles + targeting

- `scripts/combat/ProjectileManager.cs:18,260,265,290,329`:
  - Fixed 4096 slots, full active loop every `_PhysicsProcess`.
  - `RebuildEnemyIndex()` reinserts up to 500 enemies into QuadTree per physics frame.
  - Per bullet: `QueryCircle` + `DistanceSquaredTo` + `TakeDamage` + `SetInstanceTransform2D` upload.
- `scripts/combat/TargetingService.cs:55,95,123`:
  - Linear scans over `ActiveEnemies`; used by singularity and other skills.

## 6. Skill / hazard VFX always redrawing

- Unconditional `_Process` -> `QueueRedraw` in, e.g.:
  - `scripts/skills/ExosomeSingularitySkill.cs:101`
  - `scripts/skills/ComplementCascadeSkill.cs:112,203`
  - `scripts/skills/GranzymeDetonationSkill.cs:104,128`
  - `scripts/skills/DefensinBarbsSkill.cs:110`
  - `scripts/skills/InterferonWaveSkill.cs:92`
  - `scripts/skills/LysozymeRicochetSkill.cs:88`
  - `scripts/skills/HistamineSurgeSkill.cs:103`
  - `scripts/skills/PerforinLanceSkill.cs:134,181`
  - `scripts/skills/NucleaseBladesSkill.cs:103`
  - `scripts/skills/NitricOxideHaloSkill.cs:88`
  - `scripts/skills/MhcTracerBeamSkill.cs:88,102`
  - `scripts/combat/TelegraphedAttack.cs:73`
  - `scripts/combat/BioHazardArea.cs:55`
- `scripts/combat/LaserGlow.cs:23-25,33-35`:
  - Every beam = 3x `DrawLine` (halo 2.2x width); every impact = fill + 40-seg ring + spark, under bloom.
- `scripts/skills/ExosomeSingularitySkill.cs:132-154`: 36 circles + arcs per vortex per frame.
- `scripts/combat/VfxManager.cs:56,60,203`:
  - 8 types x 16 pooled `GpuParticles2D` = 128 emitters resident; `Restart()` per kill/hit.
- `scripts/environment/EnvironmentProps.cs:80,185,248,311,363`:
  - `HyperoxicPocket`, `FenestraWall`, `NeutralizationZone`, `AcidSurge`, `AstrocytePillar` each `QueueRedraw` every physics frame.
  - Each does `GetTree().GetFirstNodeInGroup("player")` per tick.
  - `FenestraWall:197` calls `SetDeferred(disabled)` per frame; `AstrocytePillar` redraws static pillar for a sine pulse.

## 7. HUD / floating text rebuilt every frame

- `scripts/ui/Hud.cs:182-191`:
  - Every `_Process`: `Tick`, `TickVignette` (`SetShaderParameter`), `TickAlpha`, `UpdateBuffStatus`, `UpdateSkillSlots`.
- `scripts/ui/hud/SkillBarView.cs:145,164,202,251,389`:
  - `UpdateSkillSlots()` -> `GetAllUiData()` -> 10x `GetUiData()`; each allocates Godot `Dictionary`/`Array`.
  - `BaseSkill.cs:269-293`: 3x `Tr()` + path + dictionary per skill per frame.
  - Per slot per frame: 3x `GetNodeOrNull`, 1–2x `AssetLoader.TryLoad`, `Tr("SKILL_LV")` + format, `ProgressBar.Value` set.
- `scripts/ui/hud/VitalsView.cs:125,191`:
  - Vignette param upload; buff tag does `Tr()` + string format per frame.
  - `OnPathogenDigested:258`: group lookup + `Get("digested_count")` per kill.
- `scripts/ui/DamageNumberSpawner.cs:129,185,212,224`:
  - `_Process` loops 256 pool entries; `QueueRedraw` when any active.
  - `_Draw` does outline + text draw per number.
  - Spawn path: O(256) scan + `TranslationServer.Translate` + `Random.Shared`.
- `scripts/ui/hud/MembraneGauge.cs:55`, `scripts/ui/EnergyPips.cs:47`, `scripts/ui/ChamberLinks.cs:34`, `scripts/ui/HoloBodyScanner.cs:214`, `scripts/ui/PassiveTreeView.cs:190`: unconditional redraws, some multiple per frame.

## Suggested verification order

1. Toggle postprocess `LensOverlay`, tissue shader, and glow off; compare FPS to isolate fill-rate.
2. Cap enemies to ~50 and disable backfill; compare to isolate entity/physics cost.
3. Freeze player deformation / collision rebake rate; profile physics tick allocations.
4. Change `UpdateSkillSlots` to dirty-only (cooldown threshold / level / equip change); profile main-thread string/texture work.
5. Add viewport culling + halved redraw rate for parallax/capillary/hazards; measure `_Draw` primitive reduction.
6. Batch more enemy species and reduce CPU particle / VFX pool counts if still bound.
