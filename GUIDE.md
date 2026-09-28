# Phagocyte — Full Understanding Guide

> Goal: take you from "vibe-coding, can't understand anything" to "can trace any run, any damage number, any UI screen to code + scene + data".
> Language: English. Method: read code in order, answer check-questions, run 1 command per step.
> Repo root: `C:\Users\skyro\project\phagocyte`

How to use this guide:
1. Do steps 0-11 in order. Each step lists **Goal / Files / What to learn / Hands-on task / Done-check**.
2. Never rename nodes/paths in Step 8 without updating code — they are load-bearing.
3. Build baseline: `dotnet build Phagocyte.csproj --warnaserror` (or `make build`). Zero warnings tolerated.
4. Godot binary (per `AGENTS.md`): `/Applications/Godot_mono.app/Contents/MacOS/Godot`.

---

## Step 0 — Repo map + commands (15 min)

### Goal
Know where everything lives and which commands are source-of-truth.

### Layout
```
project.godot              # boot scene + autoloads + input + layers
scenes/main.tscn           # run assembler (game world)
scenes/ui/main_menu.tscn   # menu assembler (pure assembly)
scenes/ui/*.tscn           # title/class/map/loadout/passive/hud/modals
scenes/actors/*.tscn       # player_base (single data-driven actor scene)
scripts/GameRoot.cs        # run orchestrator
scripts/core/              # GameManager, Stats, save, audio, achievements, tree, chamber
scripts/core/assets/       # AssetLoader, AssetPaths, GodotAssetProvider
scripts/core/data/         # CatalogBuilders/Loader, DataPaths, DataValidator
scripts/player/            # PlayerActor (ClassId + classes.json def)
scripts/camera/         # CameraFollow (hero-agnostic follow + trauma shake)
scripts/skills/            # BaseSkill, SkillManager, 7 archetypes + generic visuals
scripts/combat/            # Damage/SlowService, StatusCore/Ailments, Targeting, Projectiles, Vfx, hazards/
scripts/enemies/           # EnemyActor + traits, EnemySpawner, steering, BossPhaseComponent
scripts/directors/         # Wave, Boss, Endless, Stage, Settlement, Neutral, IRunContext
scripts/stages/            # data-driven StageEnvironment + generic props + vfx/
scripts/equipment/         # EquipmentPiece, ContactSpikes
scripts/ui/ + ui/hud/      # MainMenu, Hud, modals, views
gen/<cat>/                 # source-of-truth PNGs (prefix-free)
assets/gen/                # pipeline artifact (do not edit directly)
assets/audio/manifest.json # BGM/SFX SSOT (11 bgm, 37 sfx)
assets/data/*.json         # player_classes/stages/skills/equipment/enemies/bosses/achievements/ailments
docs/*.md                  # 15 design docs (spec, stat, skill, map, pathogen, etc.)
tests/Test*.cs             # 54 suites incl. TestHarness scaffolding
tools/asset_check/         # lint: missing/naming/dedup/orphans/resolution/quality/audio
tools/check_arch.py        # layer-boundary enforcement (make check-arch)
```

### Commands (SSOT: `AGENTS.md`, `Makefile`)
```sh
dotnet build Phagocyte.csproj --warnaserror   # or: make build
Godot --headless --path . -s res://tests/<Suite>.cs   # one suite
python3 tools/asset_check/main.py --summary   # or: make check-assets
PHAGOCYTE_CAPTURE_DIR=/tmp/xxx Godot --path . -s res://tests/TestAchievementPreview.cs  # headed only, never --headless
```

PASS = no `[FAIL]` / `TestFailedException` + `PASSED SUCCESSFULLY` footer.

### Done-check
- [ ] You can list the 6 autoloads from `project.godot:18-26` from memory.
- [ ] `make build` is green with zero warnings.

---

## Step 1 — Boot + run entry (30 min)

### Goal
Understand how the game starts and who ticks every frame.

### Files
- `project.godot:14` — `run/main_scene="res://scenes/ui/main_menu.tscn"` (NOT `main.tscn`)
- `project.godot:18-26` — autoloads: `GameManager`, `AchievementManager`, `SettingsManager`, `RunRecordManager`, `DamageNumberSpawner`, `AudioManager`
- `scenes/main.tscn` - `GameRoot` root + 6 director nodes
- `scripts/GameRoot.cs`

### What to learn
`GameRoot` is an assembler, not a god-object. Public members are thin facades so tests/HUD keep working:
- `ActiveScreenCap`, `SwarmWindowTimer`, `ActiveEnemyCount`, `EliteRaidTriggered`, `FirstSwarmTriggered`, `ExtremeSwarmTriggered`
- `SubBossTriggered`, `BossLockdownActive`, `SubBoss`, `TerminalBoss`, `TerminalBossNeutralized`, `RaidBosses`
- `OverdriveCycle`, `OverdriveHealthMultiplier`, `OverdriveSpeedMultiplier`, `AcidSafeRadius`
- `Stage`, `EnvironmentId`

`_Ready()` order:
1. `EnemySteering.ConfigureArena`, `HostUlceration.Reset`, `EnemySpawner.ConfigureRun(HardMode, Overdrive)`
2. `GetNodeOrNull` for `Player`, `HUD`, `EnemyContainer`, `Camera2D`, `Background/ArenaBG`, `Background/ArenaBorders`
3. `ResolveDirectors()` - get-or-create + inject `Context=this` + `WaveDirector.TerminalPhaseReached += BossManager.EnterBossLockdown`
4. Class swap if `GameManager.SelectedClass` isn't the scene default: instantiate `actors/player_base.tscn`, set `ClassId` pre-`AddChild`
5. `ApplyTreeLoadout`, `ApplyChamberLoadout`, `Overdrive.ApplyMutatorLoadout`, `HudNode.ConnectPlayer`
6. Ensure `ProjectileManager`, `RunTelemetryManager`, `VfxManager` exist
7. `StageId=GameManager.SelectedMap`, `StageSystem.ConfigureArenaVisuals+Initialize`
8. Backdrop quality, initial `SpawnWave`, `SwarmRenderer`, `SpawnTutorialGuides` (2 dormant guides at +150px), `NeutralPropManager.SeedInitialPopulation`, `PlayStageBgm`

`_PhysicsProcess()` order - memorize this:
1. `EnvironmentTime+=dt; AchievementManager.RecordEvent("survival_time",...)`
2. `if RunEnded return`
3. `WaveDirector.PhysicsTick` → `BossManager.PhysicsTick` → `StageSystem.PhysicsTick` (first so stage effects are same-frame) → `Overdrive.PhysicsTick` → `Overdrive.ProcessMutators` → `NeutralPropManager.PhysicsTick`
4. `if BossLockdownActive { CheckTerminalBossState(); return; }` - backfill skipped
5. `WaveDirector.ProcessBackfill()`

`_Process()` only syncs `SwarmRenderer`.

### Hands-on
Open `scenes/main.tscn:132-148` — see 6 director nodes. Set a breakpoint or `GD.Print` in `_PhysicsProcess` and confirm tick order for 5s.

### Done-check
- [ ] Explain why 15:00 lockdown arrives as an event, not a wave-director boss spawn.
- [ ] Explain why `Main` keeps facades instead of exposing directors directly.

---

## Step 2 — Autoload singletons (45 min)

### Goal
Know global state vs run-scoped state. This fixes 50% of "where is this stored?" confusion.

| Singleton | Autoload? | File | Responsibility |
|---|---|---|---|
| `GameManager` | yes `project.godot:20` | `scripts/core/GameManager.cs:1-424` | `SelectedClass/Map/Difficulty/Language`, `EndlessMode`, catalogs, `StartGame/StartEndlessGame/GoToMenu/RestartGame` |
| `AchievementManager` | yes | `scripts/core/AchievementManager.cs:22,55` | definitions/progress/unlocks, `RecordEvent`, `RecordStageClear`, Steam sync, map/class unlock chain |
| `SettingsManager` | yes | `scripts/core/SettingsManager.cs:19,53` | `settings.json`, Master/SFX/BGM dB, fullscreen/VSync/MaxFps, `PerformanceMode`, `StageEffectsEnabled` |
| `RunRecordManager` | yes | `scripts/core/RunRecordManager.cs:132,160` | `run_records.json` cap 50, KPM, rank D-C-B-A-S + SSS/EX, `ComputeScore/Rank`, `RecordRun`, `CanSettleVictory` |
| `AudioManager` | yes | `scripts/core/AudioManager.cs:12,42` | 24-voice SFX pool, fading BGM, `PlayStageBgm`, `WireClicks`, manifest validation |
| `DamageNumberSpawner` | yes `project.godot:24` | `scripts/ui/DamageNumberSpawner.cs:19-23` | pooled 256 floating numbers |

Non-autoload (common mistake):
- `Stat` — plain value object `Final=(Base+Flat)*(1+Pct)`
- `ActorStats` — `Node,IStatHost` on each actor
- `UpgradeManager` — static draft engine
- `PassiveTreeManager` — static + `JsonStore`
- `EquipmentChamber` — `Node2D` child of player
- `LoadoutManager` — static + `JsonStore`

Key APIs to read:
- `GameManager`: `GetPlayerScene`, `GetSkillInfo`, `GetEnemyInfo`, `GetBossInfo`, `IsMapUnlocked/IsMapHardUnlocked`, `StartGame`, `StartEndlessGame`
- `AchievementManager`: `Unlock`, `RecordEvent`, `RecordStageClear`, `IsEndlessUnlocked` (checks `wound_hard_clear`), `SaveToDisk/LoadFromDisk`
- `RunRecordManager`: `StandardClearSeconds=900`, `ComputeKpm`, `ComputeRank`, `ComputeScore`, `IsVictoryCriteriaMet/CanSettleVictory`, `RecordRun`
- `SettingsManager`: `ApplySettings`, setters
- `AudioManager`: `ValidateAudioManifest`, `PlayBgm/PlaySfx`, `PlayStageBgm`, `StageBgmTracks`

Connections to `GameRoot`:
- `GameRoot.IsEndlessRun=GameManager.EndlessMode`, `RunDifficulty=GameManager.SelectedDifficulty`
- `GameRoot.CurrentMapUnlocked/HardUnlocked` → `GameManager.IsMapUnlocked/HardUnlocked`
- `GameRoot._PhysicsProcess` → `AchievementManager.RecordEvent("survival_time",...)` every tick
- `GameRoot._Ready` → `AudioManager.PlayStageBgm(SelectedStage)`

### Hands-on
Grep `Instance` in `AudioManager`, `AchievementManager`, `RunRecordManager`, `SettingsManager`. Confirm none of `PassiveTreeManager/LoadoutManager/UpgradeManager` have `Instance` (they are static).

### Done-check
- [ ] Which data survives scene change? (autoloads + static + JsonStore) vs what dies with `Main`? (directors, EnemyContainer, ProjectileManager).

---

## Step 3 — Player cells + universal Stat matrix (60 min)

### Goal
Understand the only numbers that matter: 19 universal stats, no skill-specific stats.

### Files
- `scripts/player/PlayerActor.cs` — data-driven chassis (`CharacterBody2D`, `ClassId` + `classes.json` def)
- `scripts/core/ActorStats.cs:17-39,90-210`
- `scripts/core/IStatHost.cs`
- `scenes/actors/player_base.tscn` — body `layer1/mask4`, sensor `layer1/mask2`

### Stat matrix (`ActorStats.cs:17-39`)
Combat: `might(1.0)`, `area(1.0)`, `cooldown_reduction(0)`, `projectile_speed(1.0)`, `duration(1.0)`, `amount(0)`, `pierce(0)`, `knockback(1.0)`, `crit_chance(.05)`, `crit_damage(2.0)`, `ailment_damage(1.0)`
Defense: `max_health(100)`, `health_regen(0)`, `armor(0)`, `move_speed(230)`, `evasion(0)`, `block(0)`, `life_steal(0)`
Utility: `magnet(150)`

Formula: `GetStat` (`:90-113`) with caps: CDR 75%, crit 100%, evasion 60%, block 75%, lifesteal 20%.
Helpers: `GetDamageReductionRatio=armor/(armor+50)` (`:153`), `RollCritical/Evasion/Block/LifeSteal` (`:164-194`), `CalculateAilmentDamage/Duration` (`:199-210`).
Signal: `StatChanged` (`:13-14`).

`PlayerActor._Ready`: `AddToGroup("player")`, cache `Cytoplasm/Membrane/Nucleus/EngulfArea/SkillManager`, create `ActorStats`, load class def (`ClassId` → `GetPlayerClass`), seed `max_health/move_speed`, `ApplyClassBaseStats()` (armor + trait + extras), derive `Health/CurrentSpeed/CurrentRadius`, subscribe `StatChanged→OnStatChanged`, `CellSkillManager.Setup`, `SetupInitialSkills` (innate via `SkillFactory` id), `Equipment.Setup`.

Class deltas live in `assets/data/player_classes.json`, never subclasses:
- `macrophage` — `armor 10, area 1.25, might 1.0, block .08` + innate `phagocytic_grasp`
- `neutrophil` — `armor 5, might 1.2, knockback 1.4, regen .5` + innate `granzyme_detonation`
- `b_cell` — `proj_speed 1.3, CDR .10, amount 1.0` + innate `antibody_salvo`
- `ctl` — `crit .15, evasion .10, pierce 1.0` + innate `perforin_lance`
- `dendritic` — `armor 2, magnet 260, duration 1.2, CDR .10` + innate `mhc_tracer_beam`
Each row also carries body/visual/nucleus/deform params; `PlayerActor` builds tints, nucleus shape and deform rig parametrically.

Per-tick reads: `move_speed` in `HandleMovement`, `health_regen` in `HandleRegen`, `area` in `UpdateBodyDeformation`, `armor` in `ApplyDamage/ApplyImpulse`.
EXP: `ExpChanged(float,float,int)` + `LevelUp(int)`, `AddExp` (curve `ExpToNext*1.35+15`), `DrainAtp`.

### Hands-on
Open `assets/data/player_classes.json` (macrophage row) and `PlayerActor.SetupCellIdentity/ApplyClassBaseStats`. Change nothing; just trace one `move_speed` read.

### Done-check
- [ ] Why are passives only allowed to touch these 19 stats? (Survivor-like philosophy, `docs/stat.md`).

---

## Step 4 — Skills, passives, organelles (90 min)

### Goal
Know skill lifecycle, slot rules, and how builds are assembled.

### Files
- `scripts/skills/BaseSkill.cs`
- `scripts/skills/SkillManager.cs`
- `scripts/core/SkillIds.cs` — 18 active + 13 passive IDs
- `scripts/core/UpgradeManager.cs`
- `scripts/equipment/EquipmentPiece.cs`, `ContactSpikes.cs`
- `scripts/core/EquipmentChamber.cs`, `scripts/core/LoadoutManager.cs`

### Hierarchy
```
CharacterBody2D → PlayerActor (ClassId-driven, one class for all heroes)
Node2D → BaseSkill → salvo/beam/nova/zone/strike/aura archetypes + StatPassive / TreeStatBundleSkill
Node2D → SkillManager (container, NOT a skill)
Node2D → EquipmentPiece → ContactSpikes
Area2D → batched structs (`ProjectileData` in `ProjectileManager`: faction + steering + `EffectSpec`)
Node2D visuals (transient, palette-flavored): BeamVisual, NovaVisual/Marker, ZoneNode, ChainVisual, AuraVisual
```
New skills are JSON rows (`assets/data/skill/active.json`: `archetype` + `params`); new behavior extends an archetype — never a new skill class. Instantiation is id-routed via `SkillFactory` (no reflection).

### Lifecycle
- `BaseSkill.Setup(host)` (`BaseSkill.cs:30-54`): cache `Host`, resolve `Stats`, if `IsPassive` → `ApplyPassiveModifiers()` immediately.
- `SkillManager.Setup` (`SkillManager.cs:36-39`) only stores host. `EquipActive/EquipPassive` (`:44-106`): 5+5 slots, innate guard `if occupied && IsInnate return false` (`:64,91`) — slot-0 innate weapons cannot be overwritten. `AssignActiveSlot/AssignPassiveSlot` (`:108-134`): free old, `AddChild`, `Setup`, emit `SkillsChanged`.
- Tick: `PlayerActor._PhysicsProcess` → `SkillManager.UpdateAllSkills` (actives only) → `BaseSkill.UpdateSkill`: passives early-return; actives decrement `CooldownTimer` and `Trigger()` at zero.
- Fire: base `Trigger()` only resets timer. Archetype overrides do work — e.g. `SalvoSkill.Trigger` (volley patterns), `BeamSkill.Trigger` (pierce/channel/chain), `NovaSkill.Trigger` (sphere/cone/delayed), `ZoneSkill.Trigger` (deploy + ticks), `StrikeSkill.Trigger` (chain grabs), `AuraSkill` (radial ticks / orbital blades).
- Stat helpers: `GetCalculatedCooldown/Damage/Area/Amount/Pierce/Speed/Duration` (`BaseSkill.cs:150-243`), `GetDamage(base,out dmg,out crit)` (`:114-119`).
- `Upgrade()` (`:80-94`): passives remove→level→re-apply; actives level++. `_ExitTree` (`:96-102`) removes passive modifiers.

### Draft engine (`UpgradeManager`)
- `ActiveCatalog` (`:17`), `PassiveCatalog` (`:39`), `CatalystActiveLevel=5` (`:78`), `CatalystPairs` (`:80`), `IsCatalystReady` (`:93`)
- `GenerateChoices(player,3)` (`:142`), `ApplyChoice` (`:305`): `new_active` (`:322`), `new_passive` (`:337`), `new_gear` (`:352`), `upgrade_*` (`:379`), `heal_fallback` (`:394`)

### Organelles
- `EquipmentPiece.AttachTo/GetStat` reads `Host.Stats.GetStat`.
- `EquipmentChamber`: `MaxSlots=4`, backpack + energy rules via `Equip` itself.
- `LoadoutManager`: `MaxProfiles=3`, `GetActiveSlots`, `SetSlots` — `GameRoot.ApplyChamberLoadout` deploys it; empty = nothing equipped, stale/locked entries skipped.

### Hands-on
Read `BeamSkill.FirePierce` end-to-end: `FindTargetDirection` → `GetCalculatedAmount/Pierce` → segment test → `DealDamage` → visuals. Then read `StatPassive.ApplyPassiveModifiers` + one `passive.json` mods row.

### Done-check
- [ ] Draw skill attach tree: Cell → SkillManager → BaseSkill children with Host/Stats back-pointers.
- [ ] Explain innate guard + catalyst (5 active + 5 passive → superweapon).

---

## Step 5 — Combat damage pipeline (45 min)

### Goal
Trace any damage number from trigger to death.

### Files
- `scripts/combat/DamageService.cs`
- `scripts/combat/SlowService.cs`
- `scripts/combat/IDamageable.cs`, `ISlowable.cs`, `IStunnable.cs`, `IAilmentHost.cs`
- `scripts/combat/Team.cs`, `EffectSpec.cs`, `ProjectileData.cs`
- `scripts/combat/StatusCore.cs`, `AilmentController.cs` (+ `assets/data/ailments.json`)
- `scripts/combat/TargetingService.cs`
- `scripts/combat/ProjectileManager.cs`
- `scripts/combat/VfxManager.cs`, `VfxType.cs`
- `scripts/ui/DamageNumberSpawner.cs`
- `scripts/enemies/EnemyActor.cs` + `EnemyTraits.cs`

### Standard path (perforin example)
1. `GetDamage(BaseDamage,out dmg,out crit)` — `might` + crit roll
2. Target: `TargetingService.FindTargetDirection/CollectInRadius/FindNearest` (iterates `EnemyActor.ActiveEnemies`) or segment test (`beamWidth=24*area`)
3. Dispatch: `DamageService.DealDamage(target,dmg,Host,isCrit)` → `IDamageable.TakeDamage` else duck-type `take_damage`
4. Intake `EnemyActor.TakeDamageInternal`: shell absorb → `BossPhase.ApplyDamageReduction` → marked mult → `max(1,dmg-Armor)` → `NotifyHealthChanged` → numbers/telemetry/lifesteal/audio/VFX/flash → `Die` if ≤0 (death traits: splits, drops, obstacles)
5. FX: `VfxManager.Play(...)` (pooled 16/type) + code-drawn transients + `BeamGlow`
6. Numbers: `ShowDamage` (yellow/13, gold crit/18), `ShowPlayerDamage` (red), `ShowHeal`, `ShowEvaded/Blocked`

Branch paths:
- Projectiles (all batched): `SalvoSkill` / enemy `ranged` trait → `ProjectileManager.Spawn(...)` with `Team` + `EffectSpec` + steering data (circular buffer + `ProjectileData` + QuadTree query + multimesh sync). No per-shot nodes exist.
- Homing: `ProjectileData.SteeringHoming` (locked-target id or nearest + mark) / chain: `SteeringChain` (reacquire + pierce redirect) / `BeamSkill` chain mode.
- Contact/equipment: `PlayerActor.ProcessContactDamage` → `enemy.TryContactStrike` (1 hit/s); `ContactSpikes._PhysicsProcess` → `Intercept`.
- Player intake (reverse): `PlayerActor.ApplyDamage`: invuln→evaded→`RollEvasion`→`RollBlock`→armor DR→`ShowPlayerDamage`→death/trauma/flash.

### Status path (slow example)
1. Sources (HazardZone, NovaSkill, contact/slow-aura traits, stage dot-scan, acid tide) call `SlowService.ApplySlow(node,dur,factor)` — never concrete types
2. Dispatch: `ISlowable.ApplySlow` → `Ailments.ApplyAgglutination(dur, 1−factor)` (agglutination channel, `assets/data/ailments.json`)
3. State: `StatusController` (strongest-wins, `SpeedMultiplier = 1−strongest`, `IsActive(id)` for synergies); movement reads `Ailments.SpeedMultiplier` every frame
4. No `SlowTimer/SlowFactor` fields exist — timed slow via `ActorStats` is unsupported (modifiers are permanent); HUD reads `AgglutinationTimer`

### Hands-on
Zero RNG per `AGENTS.md`: `Stats.SetBase("block",0)`, `Stats.SetBase("evasion",0)` before asserting damage in tests. Find one usage in `tests/TestContactDamage.cs:100`.

### Done-check
- [ ] Trace `Die`: `PlayEnemyDeath` → `player.AddExp(Xp*ExpGainMultiplier)` (sole EXP path) → `TrySpawnDrop` → `RecordEvent("enemy_killed")` → telemetry → `EnemyDied` → `QueueFree`.

---

## Step 6 — Enemies, steering, spawner (60 min)

### Goal
Know how 500 entities stay alive without melting CPU.

### Files
- `scripts/enemies/EnemyActor.cs` + `EnemyTraits.cs` (def + trait engine)
- `scripts/enemies/EnemySpawner.cs` (id pools, scaling, boss tables)
- `scripts/enemies/EnemySteering.cs`, `EnemyThreatMode.cs`
- `scripts/combat/hazards/` — `ProximityMine.cs`, `BlockerObstacle.cs` (enemy pellets are `ProjectileManager` shots with `Team.Enemy`)
- `scripts/enemies/BossPhaseComponent.cs`
- `assets/data/enemies.json` — 35 defs (stats + steering + traits)

### Enemy lifecycle
One concrete `EnemyActor` for all 35 ids. `CreateEnemy(id)` → `ApplyDef` (stats, threat, tint, steering, traits) → `AddChild` → `_Ready`: `AddToGroup("enemies")`, `CurrentHealth=MaxHealth`, randomize drift/breathe/wander, ensure `AilmentController`, cache `BossPhaseComponent`, `EnsureCollisionNodes` (passive `Area2D Layer2/Mask0/Monitoring=false` — cuts broadphase).
`_PhysicsProcess`: cull (headless skip; non-boss off-screen return), stun tick (slow/DoT/amp tick inside child `AilmentController`), pre-drift lock → `HandleBrownianDrift` → `TickTraits`, 30Hz breathe scale.
Movement: `FloatSpeed` × `Ailments.SpeedMultiplier` × `BossPhase.CurrentSpeedMult`, steering via `EnemySteering.GetDirection` or wander, `Position+=Velocity*dt` (pure `Node2D`).

Contact: per-def `contact_damage`, `TryContactStrike` 1 hit/s. Contact debuffs (slow/invert/drain) are traits.

### Spawner (static library)
- `EscalationInterval=180`, `StandardRunDuration=900`, `PhaseCount=5`
- Caps: `MaxActiveNormal=300`, `MaxActiveSwarm=450`, `MaxActiveEndless=500`, `SwarmWindowSeconds=30`, `MaxBackfillPerTick=7`
- Overdrive ladder: `OverdriveHealthBonus {0.5,1.2,2.2,3.6}`, `OverdriveSpeedBonus {0.15,0.3,0.5,0.7}`; cycle 5+ health `×4.6*2^extra`, speed cap +100%
- `ExpGainMultiplier=0.3`, Hard `1.4x HP, 1.2x speed`
- Run-scoped `RunConfig{HardMode,Overdrive}`, `ConfigureRun` called from `GameRoot._Ready`
- Factories: `CreateEnemy` (def-based), phase pools (id lists), `GetPhaseIndex/Pool`, `SpawnWave`, `SpawnElite` (boost `1+0.6*tier`), `SpawnSwarm`, `SpawnSubBoss/TerminalBoss/RaidBoss`, `Backfill`, `GetOffscreenSpawnPoint`, `ClampToArena`

Special clustering in `SpawnSingle` is data (`spawn_cluster`: staph→3 with shield 1, norovirus→10).

Bosses are def rows (`IsBoss`, score/contact, `telegraph_scale`): sub-bosses (score 600) + terminal (score 3000), `BossPhaseComponent` (phases, DR, speed mult incl. hard-enrage ×1.4). Terminal map table in `CreateTerminalBoss`: alveolar→syncytial, hepatic→macroschizont, gastric→biofilm_core, BBB→amyloid, default wound→mrsa. Boss-only mechanics (splits, shells, auras, chains) are traits in the same engine.

### Steering + layers
Modes: `Drifter/ChemoChaser/Interceptor/Standoff/Invader` (`EnemyThreatMode.cs`).
`ConfigureArena` → 6 anchors on 0.82-ring. `SeekPlayer` (jitter 0.22), `InterceptPlayer` (lead `clamp(vel*0.6,100,200)`), `Standoff` (approach >1.15×, retreat <0.6×, orbit), `InvadeTissue` (latch ≤max(8,40) → Zero for ulceration).
Layers: enemies passive `Layer2/Mask0`; skills `Layer0/Mask2`; enemy shots `Layer0/Mask1`; player body `1|4`, sensor `1|2`.

### Hands-on
Read `EnemySpawner` (timeline doc + `SpawnSingle`). Then `EnemySteering` dispatch.

### Done-check
- [ ] Why must scaling run pre-`_Ready`? (`_Ready` copies Max→Current).
- [ ] Why are enemies `Node2D` not `CharacterBody2D`?

---

## Step 7 — Directors: wave / boss / overdrive / organ / settlement / neutral (60 min)

### Goal
Understand the run clock: 03:00 → 06:00 → 09:00 → 12:00 → 15:00 → endless.

### Files
- `scripts/directors/WaveDirectorComponent.cs`
- `scripts/directors/BossEncounterManager.cs`
- `scripts/directors/EndlessDirector.cs`
- `scripts/directors/StageSystem.cs`
- `scripts/stages/StageEnvironment.cs` + `EnvironmentProps.cs` (generic props)
- `scripts/directors/RunSettlementService.cs`
- `scripts/directors/NeutralPropManager.cs`
- `scripts/directors/IRunContext.cs`

Wave (`WaveDirectorComponent`):
- `TerminalPhaseReached` event (`:18`), `PhysicsTick` (`:70-102`)
- `≥180` elite raid (tier-1 + sfx), `≥360` first swarm (2 elites + `SwarmWindowTimer=30`), `≥720` extreme swarm (30s + tier-2), `≥RunGoalSeconds(900)` → `TerminalPhaseReached` → `BossManager.EnterBossLockdown`
- `ProcessBackfill`: `deficit=ActiveScreenCap-ActiveEnemyCount`, `batch=min(deficit,7)`. `ActiveScreenCap`: endless+≥900→500 else swarm?swarm:normal. Count only `EnemyActor` children.

Boss (`BossEncounterManager`):
- `PhysicsTick`: `≥540` once → `TriggerSubBossEncounter` (`SpawnSubBoss`, `EnemyDied→OnSubBossDefeated`, BGM `boss`)
- `OnSubBossDefeated`: restore map BGM, reward `AddExp(ExpToNext-CurrentExp)`
- `EnterBossLockdown` (15:00 event): `BossLockdownActive=true`, `SpawnTerminalBoss`, BGM `boss` (or `boss_final` endless). Null container/player → `EndRun(false,system_failure)`.
- `OnTerminalBossDefeated`: `TerminalBossNeutralized=true`; endless → clear lockdown, continue; standard → `EndRun(true,specific_neutralization)`
- `CheckTerminalBossState` (only under lockdown): vanished → endless release + warning; standard `EndRun(false,system_failure)`
- `ProcessBossRaids(cycle)`: `cycle≥2` (18:00 first), `≥1800s` triple else twin, other-organ maps, HUD alert

Overdrive (endless only, `EndlessDirector`):
- Cycle announce, `cycle≥3` bile surge (strip armor 3s every 15s), `cycle≥4` acid tide (safe radius 2300→850, 6dps + slow 0.6 outside), `cycle≥5` composite + exponential HP
- Run mutators: viscosity move penalty, burn (%maxHP/5s), drift clears marks/20s

Stage (`StageSystem` + `scripts/stages/`):
- `Initialize(hard)`, `ConfigureArenaVisuals`, `PhysicsTick`: `Current.Tick` drives data-driven effects. Gated by `SettingsManager.StageEffectsEnabled` (off → tints only).
- Wound: no env hazards; Alveolar: buff-zone spawner/6s max 3; Hepatic: stat-strip/18s + blocker walls/24s max 3; Gastric: safe zones/9s max 3 + dot volleys/14s + dot scan; BBB: blocker scatter ×5 + scramble pulse/11s×1.6s. All params in `stages.json` effects; props are generic (`BuffZone/BlockerWall/DotZone/SafeZone/BlockerPillar`).

Settlement (`RunSettlementService.TryEndRun :32-140`):
- Guard `RunEnded`; victory validated by `CanSettleVictory(time,bossNeutralized,endless)` = `!endless && bossNeutralized && time≥900-0.01`. Endless victory always rejected. Shortfall warns, returns false (Main does NOT raise `RunEnded`).
- Victory: BGM victory + `RecordStageClear`; defeat: BGM defeat. Causes: `specific_neutralization` / `membrane_rupture` / `system_failure`.
- `RecordRun` (downgrades unearned victory), endless → `SteamBridge.SubmitEndlessLeaderboard`, open `RunRecordsModal.OpenSettlement` + `PauseManager.PushHold(Settlement)`
- Rule: **standard victory = 15:00 + terminal-boss kill** (`docs/record.md §3.1`); **defeat = membrane zero**; **endless = defeat-only**; boss-vanish = `system_failure`.

Neutral (`NeutralPropManager`): seed 3 mines, mine/6s cap 10, ulcer mist gated `HostUlceration.Pulses≥4` (severe ≥8). Neutrals never consume screen-cap (not `EnemyActor`).

### Hands-on
Trace one full run in code: `WaveDirector.PhysicsTick` → 15:00 event → `EnterBossLockdown` → `OnTerminalBossDefeated` → `TryEndRun(true)` → `RecordRun`.

### Done-check
- [ ] Why is 09:00 sub-boss owned by BossManager, not WaveDirector? (wave stays boss-free).

---

## Step 8 — UI: menu → HUD → upgrade (60 min)

### Goal
Navigate any pixel to its scene + code. This is where vibe-coding hurts most.

Rules (from `AGENTS.md`, do not re-litigate):
- Fixed node set → `.tscn`. Varying count → container + item scene + code loop. Runtime geometry → code + exported constants. Transient effects → code only.
- One reusable UI per `.tscn`; `main_menu.tscn` is pure assembly.
- NEVER rename nodes/paths: `MainMenu.cs` + tests use `GetNodeOrNull("AchievementView")`-style paths. Keep instance root names + `visible` flags. Each extracted scene must be self-contained (no cross-file ExtResource).

### Menu assembly (`scenes/ui/main_menu.tscn:1-164`)
Root `MainMenu:Control` + `scripts/ui/MainMenu.cs:9`. Static ambience in-file: `Background`, `BgImage`, `Particles`, `Watermark`. 6 composed views (`instance=ExtResource`): `TitleView`, `ClassView`, `LoadoutView (visible=false)`, `PassiveView (visible=false)`, `MapView (visible=false)`, `AchievementView (visible=false)`. Shared `GlobalBackButton (visible=false)`. 3 modals in-tscn: `CodexModal`, `SettingsModal`, `RunRecordsModal (visible=false)`. 7th modal code-instantiated: `EndgameSetupModal` (`MainMenu.cs:229-231`). Code-injected: `EndlessButton`, `DifficultyToggle`, `MapLockStatusLabel`, `ProfileHBox` (`MainMenu.cs:192-246,309-341`).

Flow (`MainMenu.cs:515-526,722-1078`):
`Title.Start → ClassView → (Confirm) LoadoutView → (Confirmed signal) PassiveView → (Confirm) MapView → (Deploy) GameManager.StartGame → main.tscn`
Back: `Map→Passive→Loadout→Class→Title` (`OnGlobalBackPressed:534-554`, ESC `TryHandleEscapeAsBack:429-458`). Settlement `RecordsModal` consumes ESC.

Per-view: Class (`SetupClassButtons`, `SelectClass`, radar `RadarChart.SetStats`, lock via `AchievementManager`), Loadout (`RefreshLoadoutView → LoadoutView.Open`), Passive (`SelectPassiveBuild`, `Purchase/Refund/Reset`, profile tabs code-built), Stage (`SetupStageButtons`, `SelectStage`, `StageSelectView.SelectStage`, difficulty/endless gates), Title auxiliaries (Codex/Records/Achievements/Settings/Quit).

Run root (`scenes/main.tscn`): `GameRoot` + `Background/{ArenaBG,MicroscopeParallax,CapillaryTissue,FluidParticles,ArenaBorders}` + `ArenaBoundaries` + `EnemyContainer` + directors + instanced `Player` + `Camera2D(CameraFollow)` + `MicroscopePostProcess` + instanced `HUD` + `UIOverlay/RunRecordsModal`.

### HUD (`scripts/ui/Hud.cs:17-348`, `scripts/ui/hud/`)
`Hud` is mediator, no readout logic. `_Ready:118-198` constructs 6 sub-views (`Vitals/SkillBar/WaveTimer/PauseMenu/TutorialCue/Toast`), `Bind`s each, instantiates `UpgradeModal` if missing, wires signals, language listener, FPS label. `_Process:206-219` fans out `Tick`s.
`ConnectPlayer` (caller `GameRoot` run setup): fans out to `_vitals/_skills/_pause/_tutorial`, typed `PlayerActor` vs fallback GDScript signals.

Vitals (`VitalsView.cs`): `Bind`, `ConnectPlayer` subscribes `StatsChanged/ExpChanged`, `ConnectFallback`, throttled text. Emission: `PlayerActor.AddExp` emits single `ExpChanged`; `DrainAtp` also emits.

### Upgrade chain
`AddExp → LevelUp+ExpChanged → TutorialCueView.OnPlayerLevelUp (:152-173) → bullet-time (0.5s, TimeScale→0.05) → UpgradeModal.OpenUpgradeModal (:95-125, queues _pendingLevels, PauseManager.PushHold(UpgradeDraft)) → GenerateChoices → PopulateCards (:127-246, icons via AssetPaths, catalyst aura) → OnCardClicked (:306-323) → ApplyChoice → ResolveChoice (:326-343, PopHold, deferred next)`. Organelle swap (`:349-549`): auto-equip or `EnterSwapMode` with `SwapSlot0-3`, Store/Discard/Cancel.

Scene: `upgrade_modal.tscn:49-390` fixed shell (3 fixed `Card0-2`, each `SelectButton+Vbox/{IconTexture,Title,Badge,Desc}`).

### Hands-on
Open `hud.tscn`. Find `HPContainer`, `EXPContainer`, `BottomExpBar`, `HealthBar`. Then find their `Bind` in `VitalsView.cs`. Rename nothing.

### Done-check
- [ ] Explain container+loop vs fixed-shell with one example each (`CodexModal.RenderCurrentTab` vs `upgrade_modal Card0-2`).
- [ ] Why must screenshots re-run after any pixel change? (C# no hot-reload; stale `game` session shows old pixels).

---

## Step 9 — Meta: tree, achievements, records, i18n (45 min)

### Goal
Understand out-of-run progression.

- Passive tree (`PassiveTreeManager.cs`): topology `passive_tree.json`, per-class levels/3 profiles, connectivity/purchase/refund, `CreateSkill` factory, live bonus from achievements. `GameRoot.ApplyTreeLoadout` attaches `TreeStatBundleSkill`s. View: `PassiveTreeView.cs` (shell in `passive_view.tscn`, graph code-drawn, nodes `TreeNode_{id}`).
- Achievements (`AchievementManager.cs`): `achievements.json` + Steam, `RecordEvent/EvaluateThreshold/RecordStageClear`, `IsEndlessUnlocked` (needs `wound_hard_clear`), `ApplyStageRewards/SyncStageUnlocks`, gallery `AchievementGalleryView/Card/Toast`.
- Records (`RunRecordManager.cs` + `RunRecordsModal.cs`): `OpenHistory` vs `OpenSettlement`, grades, KPM/score.
- Loadouts (`LoadoutManager.cs` + `LoadoutView.cs`): loadout JSON, scratch chamber, `BuildBackpackCards` instantiates `equipment_slot.tscn`, profile tabs code-built.
- Unlocks (`EquipmentUnlockManager.cs`): `IsUnlocked`, `TrySpawnDrop`.
- i18n: 8 locales in `project.godot:92`, `GameManager.SetLanguage/ToggleLanguage (:152,167)`, `UpdateAllTexts` in menu/HUD, `TestI18n/TestMultilingualLayout`.

Docs: `docs/passivetree.md`, `achievement.md`, `record.md`, `tutorial.md`, `endgame.md`.

### Done-check
- [ ] Empty loadout deploys what? (nothing — fresh cell has no equipment).

---

## Step 10 — Assets, audio, saves (30 min)

### Goal
Stop breaking builds with missing art/sound.

- Source-of-truth: `gen/<achievement|skill|passive_tree|ui>/` (prefix-free). Artifact: `assets/gen/`. Check: `python3 tools/asset_check/main.py --summary`.
- Runtime: ONLY via `AssetLoader` (`scripts/core/assets/`): `Load<T>` throws `AssetLoadException` on miss, `TryLoad<T>` returns null. Raw `GD.Load/ResourceLoader.*` only in `GodotAssetProvider.cs` (zero-residual rule — grep before adding).
- Paths via `AssetPaths.*`; missing art → `PlaceholderIcon (assets/gen/ui/frame_organelle.png)`, so scenes must render with placeholders. `.uid` sidecars tracked in git.
- Audio SSOT: `assets/audio/manifest.json` first, then file, then `AudioManager` call. Enforced 3 ways: `check-assets` step 7 (disk), `ValidateAudioManifest` at boot (fail fast, `AudioManager.cs:118-144`), `TestAudioAssets` (red suite). `PlaySfx` warns on miss — silence is always a bug.
- Saves: `JsonStore.cs`, `user://*.json`. Tests auto-isolate (`TestHarness` ctor `IsolateSaves`, `user://test_*`, `DropsEnabled=false`).

### Done-check
- [ ] Add a fake manifest entry and see which of the 3 enforcement layers fires first.

---

## Step 11 — Tests, determinism, visual verification (30 min)

### Goal
Run suites like CI and verify pixels correctly.

- Suites: `Godot --headless --path . -s res://tests/<Suite>.cs`. `TestHarness` is abstract scaffolding — never run directly. `TestAchievementPreview` under `--headless` verifies logic only (capture skipped); real pixels need headed run. Exclude both from full-sweep loops (43 runnable suites, ~7min).
- Frame-gate async with `Gate(ref _frame,n)` (`TestHarness.cs:34-42`); saves auto-isolated (`IsolateSaves`, `user://test_*`).
- Determinism (do not reintroduce flakes): damage asserts zero RNG (`SetBase("block",0)`, `SetBase("evasion",0)`); HUD/exp/arena freeze spawners + clear arena + reset baselines frame 1 + reset HUD snapshots (`LastCurrentExp` — setting `CurrentExp` does NOT fire `ExpChanged`); dash/charge add positive control first.
- Screenshots (headed!): `PHAGOCYTE_CAPTURE_DIR=/tmp/xxx Godot --path . -s res://tests/TestAchievementPreview.cs` — NEVER `--headless` (blank viewports). `TestHarness.CaptureScreenshot(name)` handles plumbing; keep PNGs in `/tmp`, never commit. Visual refactors need before/after comparison (baseline FIRST).
- Mandatory UI screenshot verification: ANY pixel change must be verified by agent with `godot-ai` game screenshot (`editor_screenshot source="game"`) before done. C# does NOT hot-reload: after `dotnet build`, restart run (`stop→project_run(main)`), drive to view with `game_eval`, then capture.
- Hot files (`Hud.cs`, `MainMenu.cs`, shared scenes): coordinate parallel edits. When in doubt, full sweep.

### Hands-on
Run: `TestAssetLoader`, `TestAudioAssets`, `TestStatAndSkills`, `TestMenuFlow`, `TestWaveTimeline`. All green before touching code.

### Done-check
- [ ] Explain why `TestHarness.CaptureScreenshot` honors `PHAGOCYTE_CAPTURE_DIR` else `user://captures`.

---

## Appendix A — Glossary (memorize)

- Might/Area/CDR/Amount/Pierce/Duration — universal damage scalers, never per-skill.
- Innate — slot-0 un-overwritable starter weapon per cell.
- Catalyst — L5 active + paired passive → epigenetic superweapon.
- ShieldCharges — hit-absorbing shield (cluster spawns start with 1).
- Marked — damage-taken multiplier (drift clears it).
- Backfill — kill-driven refill up to `ActiveScreenCap`, max 7/tick.
- Lockdown — 15:00 terminal-boss phase, backfill paused.
- Overdrive — endless ≥15:00 escalation, cycles/180s.
- Ulceration — H.pylori invader meter (≥4 mist, ≥8 severe).
- Chamber/Backpack/Energy — 2×2 equip, 24-cap storage, 6+generator budget.
- Settlement — victory validation + record persist + modal.

## Appendix B — Key docs map

`docs/spec.md` (master GDD), `stat.md` (19 stats), `skill.md` (18+13+catalyst), `cell.md` (5 classes), `pathogen.md` (enemies), `stages.md` (5 stages), `passivetree.md`, `achievement.md`, `record.md` (victory rule §3.1), `tutorial.md` (§2 cues), `endgame.md` (§3.1 endless), `real.md` (bio fidelity), `feedback.md`, `cheats.md`, `README.md`.

## Appendix C — Anti-vibe-coding workflow (going forward)

1. Baseline PNG first (if pixels), then code.
2. One reusable UI per `.tscn`; keep scene edits pure (move first, cleanup separate).
3. No dead branches, unused overloads, one-use abstractions, `if(false)`, `skipX` params, or shims — delete, update call sites, zero-residual grep.
4. No speculative hooks: if nothing calls it, it doesn't ship.
5. `git status/diff/log --oneline -5` first; stage only intended files; suites green before push.
6. After each step in this guide, write 3 bullet notes in your own words — that is how you reclaim the codebase.

---

## Appendix D — C# patterns in this repo (review-minimum)

Stack: `Phagocyte.csproj:1-6` — `Godot.NET.Sdk/4.7.1`, `net8.0`, C# 12, `Nullable enable`, `TreatWarningsAsErrors true`. Zero warnings tolerated.

1. `partial class` everywhere: `scripts/GameRoot.cs`, `scripts/player/PlayerActor.cs`, `scripts/ui/MainMenu.cs`. Reason: Godot generator adds hidden glue. Review rule: never remove `partial`; missing `partial` = broken scene binding.
2. `[Export]` = editor-wired dependency. Review rule: if you rename an `[Export]` prop, check `scenes/main.tscn` director wiring still resolves; `ResolveDirectors()` creates code fallback via `GetNodeOrNull` + `new`, so missing scene wiring silently changes behavior.
3. Nullable + `GetNodeOrNull<T>`: `GameRoot` player/HUD/container lookups. Review rule: `GetNode` throws on miss, `GetNodeOrNull` returns null - this repo prefers null + fallback. Any new `GetNode` is a smell; any unchecked deref of `GetNodeOrNull` result is a defect under `Nullable enable`.
4. C# events for in-run signals: `ActorStats.StatChanged`, `PlayerActor.ExpChanged/LevelUp`, `WaveDirector.TerminalPhaseReached`. Subscribed in `VitalsView.ConnectPlayer`, `GameRoot.ResolveDirectors`. GDScript fallback only in `VitalsView.ConnectFallback` via `Callable`. Review rule: setting `CurrentExp` directly does NOT fire `ExpChanged` - must reset HUD snapshots (`LastCurrentExp`) in tests.
5. `Callable.From + TweenMethod` for time effects: `TutorialCueView.cs:181,200` (`Engine.TimeScale 1.0 -> 0.05 -> 1.0`). Paired with `GameManager.StartGame (:379,404)` resetting `Engine.TimeScale=1.0` before `ChangeSceneToFile`. Review rule: any early-return that skips timescale restore = permanent slow-mo.
6. `QueueFree` discipline: `GameRoot` (old player on class swap), `SkillManager.AssignActiveSlot` (old skill). Skills/passives clean up in `_ExitTree` (removes modifiers). Review rule: leaked passive = permanent stat buff.
7. `static` vs autoload `Instance`: autoloads expose `Instance`; `PassiveTreeManager/LoadoutManager/UpgradeManager/EnemySpawner` are `static` + `JsonStore`. Review rule: static state leaks across runs unless `Reset`/`ReloadFromDisk` is called (`GameRoot._Ready` calls `HostUlceration.Reset`, `EnemySpawner.ConfigureRun`).
8. `ChangeSceneToFile`: `GameManager.cs:382,407,415,420` (`main.tscn` vs `main_menu.tscn`). Review rule: must reset `Engine.TimeScale` + validate unlocks before switch.

Hands-on: open `BaseSkill.cs:30-102` and classify each member as lifecycle (`Setup/Update/Trigger/Upgrade/_ExitTree`) vs stat helper (`GetCalculated*`).

## Appendix E — Godot concepts used here (review-minimum)

1. Scenes are composition: `scenes/main.tscn:1-18` `ExtResource` decls + `60-183` nodes; `main_menu.tscn:133-143` composes 6 views via `instance=ExtResource`. Each `.tscn` must be self-contained (all `ExtResource/SubResource` declared in-file). Review rule: scene diff must show matching `id="..."` decl for every `ExtResource("...")` use.
2. Autoloads = always-alive singletons: `project.godot:18-26`. Review rule: new global state belongs in an existing autoload/static, not a new autoload.
3. Groups = runtime registry: `player`, `enemies`, `hazards`, `neutral_props`, `telegraphed_attacks`, `enemy_shots`. `TargetingService` iterates `EnemyActor.ActiveEnemies`, not groups. Review rule: missing `AddToGroup` = invisible to scans/HUD.
4. Physics layers (`project.godot`): 1=Player, 2=Enemies, 4=Environment. Enemies are passive `Area2D Layer2/Mask0/Monitoring=false` — detected BY player sensors/skills, cutting broadphase at 300-500 bodies. Skills `Layer0/Mask2`, enemy shots `Layer0/Mask1`, player body `1|4` sensor `1|2`. Review rule: any new `CollisionShape` must state layer/mask or it defaults wrong.
5. Node lifecycle: `_Ready` (wire once) vs `_PhysicsProcess` (deterministic tick) vs `_Process` (render/sync). `GameRoot._PhysicsProcess` order is contract (wave→boss→stage→endless→neutral→backfill). `GameRoot._Process` only syncs `SwarmRenderer`. Review rule: gameplay logic in `_Process` = frame-rate dependent bug.
6. No C# hot-reload: after `dotnet build`, restart run (`stop` → `project_run(main)`), drive with `game_eval`, then `editor_screenshot source="game"`. Review rule: screenshot from stale session = false evidence.
7. Persistence: `JsonStore` + `user://*.json`; tests isolate to `user://test_*` with `DropsEnabled=false`. Headed cheats touch real profiles. Review rule: test that writes non-`test_*` path is a defect.

Hands-on: open `scenes/main.tscn` and map each node to its `GetNodeOrNull` in `GameRoot`.

## Appendix F — Change review checklist (use on every PR)

Copy-paste into PR description and check off:

- [ ] Scope: `git status/diff` shows only intended files; secrets excluded.
- [ ] Build: `dotnet build Phagocyte.csproj --warnaserror` zero warnings.
- [ ] Scenes: every `ExtResource("id")` has `id="..."` decl in same file; instance root names + `visible` flags preserved; no renamed `GetNodeOrNull` paths.
- [ ] Code size: no dead branch, unused overload/param, `if(false)`, `skipX`, shim, or speculative hook; zero-residual grep for removed symbols = 0 hits.
- [ ] Stats: only 19 universal stats touched; no skill-specific stat added.
- [ ] Assets: `gen/` source updated if art changed; `AssetPaths` used; `PlaceholderIcon` fallback renders; `make check-assets` green.
- [ ] Audio: `manifest.json` first, then file, then `AudioManager` call; `TestAudioAssets` green; no silent miss (`PlaySfx` warns).
- [ ] Saves: no real-profile writes from tests (`user://test_*` only).
- [ ] Determinism: damage test zeroes block/evasion; HUD test resets `LastCurrentExp`; dash/charge has positive control; `Gate(ref _frame,n)` used.
- [ ] Tests: relevant suite(s) green + full 43-suite sweep if `Hud.cs`/`MainMenu.cs`/shared scene touched.
- [ ] Pixels (if any): baseline PNG captured FIRST, after-change PNG captured from fresh restarted run via `game` screenshot, eyeballed before/after.

---

*Generated from live repo survey 2026-09-26. If a line number drifted, grep the symbol name — zero-residual grep is source-of-truth.*

