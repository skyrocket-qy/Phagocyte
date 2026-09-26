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
scenes/characters/*.tscn   # 5 cells
scenes/enemies/*.tscn      # staph + others
scripts/Main.cs            # run orchestrator
scripts/core/              # GameManager, Stats, save, audio, achievements, tree, chamber
scripts/core/assets/       # AssetLoader, AssetPaths, GodotAssetProvider
scripts/core/data/         # CatalogBuilders/Loader, DataPaths, DataValidator
scripts/player/            # BaseCell + 5 cells + CameraFollow
scripts/skills/            # BaseSkill, SkillManager, 17 active + 13 passive + visuals
scripts/combat/            # CombatHelper, IDamageable, ProjectileManager, VfxManager, Targeting
scripts/enemies/           # BaseEnemy, PathogenSpawner, steering, bosses/, hazards/
scripts/directors/         # Wave, Boss, Overdrive, Organ, Settlement, Neutral, IRunContext
scripts/environment/       # 5 organ environments + parallax/tissue layers
scripts/gear/        # Gear, ReceptorSpikes
scripts/ui/ + ui/hud/      # MainMenu, Hud, modals, views
gen/<cat>/                 # source-of-truth PNGs (prefix-free)
assets/gen/                # pipeline artifact (do not edit directly)
assets/audio/manifest.json # BGM/SFX SSOT (11 bgm, 21 sfx)
assets/data/*.json         # classes/maps/skills/gear/pathogens/bosses/achievements
docs/*.md                  # 15 design docs (spec, stat, skill, map, pathogen, etc.)
tests/Test*.cs             # 56 files incl. TestHarness scaffolding
tools/asset_check/         # lint: missing/naming/dedup/orphans/resolution/quality/audio
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
- `scenes/main.tscn:60-68` — `Main` root + 6 director nodes
- `scripts/Main.cs:25`, `:130`, `:269`, `:384`

### What to learn
`Main` is an assembler, not a god-object. Public members are thin facades so tests/HUD keep working:
- `ActiveScreenCap`, `SwarmWindowTimer`, `ActivePathogenCount`, `EliteRaidTriggered`, `FirstSwarmTriggered`, `ExtremeSwarmTriggered` (`Main.cs:47-55`)
- `SubBossTriggered`, `BossLockdownActive`, `SubBoss`, `TerminalBoss`, `TerminalBossNeutralized`, `RaidBosses` (`Main.cs:58-64`)
- `OverdriveCycle`, `OverdriveHealthMultiplier`, `OverdriveSpeedMultiplier`, `AcidSafeRadius` (`Main.cs:67-73`)
- `OrganEnvironment`, `EnvironmentId`, `EnvironmentPlayerDrift` (`Main.cs:87-89`)

`_Ready()` order (`Main.cs:130-246`):
1. `EnemySteering.ConfigureArena`, `HostUlceration.Reset`, `PathogenSpawner.ConfigureRun(HardMode, Overdrive)`
2. `GetNodeOrNull` for `Macrophage`, `HUD`, `EnemyContainer`, `Camera2D`, `Background/ArenaBG`, `Background/ArenaBorders`
3. `ResolveDirectors()` (`Main.cs:269-323`) — get-or-create + inject `Context=this` + `WaveDirector.TerminalPhaseReached += BossManager.EnterBossLockdown` (`Main.cs:322`)
4. Cell swap if `GameManager.SelectedClass != macrophage` (`Main.cs:153-168`)
5. `ApplyTreeLoadout`, `ApplyChamberLoadout`, `Overdrive.ApplyAfflictionLoadout`, `HudNode.ConnectPlayer`
6. Ensure `ProjectileManager`, `RunTelemetryManager`, `VfxManager` exist
7. `MapId=GameManager.SelectedMap`, `OrganSystem.ConfigureArenaVisuals+Initialize`
8. `BackdropQuality.ApplyTo(SettingsManager.PerformanceMode)`, initial `SpawnWave(...,10)`, `PathogenSwarmRenderer`, `SpawnTutorialGuides` (2 dormant staph at +150px), `NeutralMatter.SeedInitialPopulation`, `PlayMapBgm`

`_PhysicsProcess()` order (`Main.cs:384-411`) — memorize this:
1. `EnvironmentTime+=dt; AchievementManager.RecordEvent("survival_time",...)`
2. `if RunEnded return`
3. `WaveDirector.PhysicsTick` → `BossManager.PhysicsTick` → `OrganSystem.PhysicsTick` (first so fluid current is same-frame) → `Overdrive.PhysicsTick` → `Overdrive.ProcessAfflictions` → `NeutralMatter.PhysicsTick`
4. `if BossLockdownActive { CheckTerminalBossState(); return; }` — backfill skipped
5. `WaveDirector.ProcessBackfill()`

`_Process()` (`Main.cs:375-382`) only syncs `SwarmRenderer`.

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
| `AchievementManager` | yes | `scripts/core/AchievementManager.cs:22,55` | definitions/progress/unlocks, `RecordEvent`, `RecordMapClear`, Steam sync, map/class unlock chain |
| `SettingsManager` | yes | `scripts/core/SettingsManager.cs:19,53` | `settings.json`, Master/SFX/BGM dB, fullscreen/VSync/MaxFps, `PerformanceMode`, `MapEffectsEnabled` |
| `RunRecordManager` | yes | `scripts/core/RunRecordManager.cs:132,160` | `run_records.json` cap 50, KPM, rank D-C-B-A-S + SSS/EX, `ComputeScore/Rank`, `RecordRun`, `CanSettleVictory` |
| `AudioManager` | yes | `scripts/core/AudioManager.cs:12,42` | 24-voice SFX pool, fading BGM, `PlayMapBgm`, `WireClicks`, manifest validation |
| `DamageNumberSpawner` | yes `project.godot:24` | `scripts/ui/DamageNumberSpawner.cs:19-23` | pooled 256 floating numbers |

Non-autoload (common mistake):
- `Stat` — plain value object `Final=(Base+Flat)*(1+Pct)` (`scripts/core/Stat.cs:11-22`)
- `CellStats` — `Node,IStatHost` on each cell (`scripts/core/CellStats.cs:11`)
- `UpgradeManager` — static draft engine (`scripts/core/UpgradeManager.cs`)
- `PassiveTreeManager` — static + `JsonStore` (`scripts/core/PassiveTreeManager.cs:340-352`)
- `GearChamber` — `Node2D` child of cell (`scripts/core/GearChamber.cs:19-29`)
- `LoadoutManager` — static + `JsonStore` (`scripts/core/LoadoutManager.cs:18-23`)

Key APIs to read:
- `GameManager`: `GetCellScene` (`:46`), `GetSkillInfo` (`:311`), `GetPathogenInfo` (`:330`), `GetBossInfo` (`:348`), `IsMapUnlocked/IsMapHardUnlocked` (`:283,288`), `StartGame` (`:375`), `StartEndlessGame` (`:391`)
- `AchievementManager`: `Unlock` (`:87`), `RecordEvent` (`:141`), `RecordMapClear` (`:214`), `IsEndlessUnlocked` (`:238` checks `wound_hard_clear`), `SaveToDisk/LoadFromDisk` (`:412,426`)
- `RunRecordManager`: `StandardClearSeconds=900` (`:28`), `ComputeKpm` (`:64`), `ComputeRank` (`:80`), `ComputeScore` (`:122`), `IsVictoryCriteriaMet/CanSettleVictory` (`:140,150`), `RecordRun` (`:174`)
- `SettingsManager`: `ApplySettings` (`:66`), setters (`:96-152`)
- `AudioManager`: `ValidateAudioManifest` (`:118`), `PlayBgm/PlaySfx` (`:165,210`), `PlayMapBgm` (`:349`), `MapBgmTracks` (`:340`)

Connections to `Main`:
- `Main.IsEndlessRun=GameManager.EndlessMode` (`Main.cs:119`), `RunDifficulty=GameManager.SelectedDifficulty` (`Main.cs:109`)
- `Main.CurrentMapUnlocked/HardUnlocked` → `GameManager.IsMapUnlocked/HardUnlocked`
- `Main._PhysicsProcess` → `AchievementManager.RecordEvent("survival_time",...)` every tick
- `Main._Ready` → `AudioManager.PlayMapBgm(SelectedMap)`

### Hands-on
Grep `Instance` in `AudioManager`, `AchievementManager`, `RunRecordManager`, `SettingsManager`. Confirm none of `PassiveTreeManager/LoadoutManager/UpgradeManager` have `Instance` (they are static).

### Done-check
- [ ] Which data survives scene change? (autoloads + static + JsonStore) vs what dies with `Main`? (directors, EnemyContainer, ProjectileManager).

---

## Step 3 — Player cells + universal Stat matrix (60 min)

### Goal
Understand the only numbers that matter: 19 universal stats, no skill-specific stats.

### Files
- `scripts/player/BaseCell.cs:18` — abstract chassis (`CharacterBody2D`)
- `scripts/player/Macrophage.cs`, `NeutrophilCell.cs`, `BCell.cs`, `CtlCell.cs`, `DendriticCell.cs`
- `scripts/core/CellStats.cs:17-39,90-210`
- `scripts/core/IStatHost.cs`
- `scenes/characters/*.tscn` — body `layer1/mask4`, sensor `layer1/mask2`

### Stat matrix (`CellStats.cs:17-39`)
Combat: `might(1.0)`, `area(1.0)`, `cooldown_reduction(0)`, `projectile_speed(1.0)`, `duration(1.0)`, `amount(0)`, `pierce(0)`, `knockback(1.0)`, `crit_chance(.05)`, `crit_damage(2.0)`, `ailment_damage(1.0)`
Defense: `max_health(100)`, `health_regen(0)`, `armor(0)`, `move_speed(230)`, `evasion(0)`, `block(0)`, `life_steal(0)`
Utility: `magnet(150)`

Formula: `GetStat` (`:90-113`) with caps: CDR 75%, crit 100%, evasion 60%, block 75%, lifesteal 20%.
Helpers: `GetDamageReductionRatio=armor/(armor+50)` (`:153`), `RollCritical/Evasion/Block/LifeSteal` (`:164-194`), `CalculateAilmentDamage/Duration` (`:199-210`).
Signal: `StatChanged` (`:13-14`).

`BaseCell._Ready` (`BaseCell.cs:205-269`): `AddToGroup("player")`, cache `Cytoplasm/Membrane/Nucleus/EngulfArea/SkillManager`, create `CellStats`, seed `max_health/move_speed`, call virtual `ApplyClassBaseStats()`, derive `Health/CurrentSpeed/CurrentRadius`, subscribe `StatChanged→OnStatChanged`, `CellSkillManager.Setup`, `SetupInitialSkills`, `GearChamber.Setup`.

Class deltas (examples):
- `Macrophage.cs:57-66` — `armor 10, area 1.25, might 1.0, block .08` + innate `PhagocyticGraspSkill` (`:68-78`)
- `NeutrophilCell.cs:56-65` — `armor 5, might 1.2, knockback 1.4, regen .5` + innate `GranzymeDetonationSkill`
- `BCell.cs:56-65` — `proj_speed 1.3, CDR .10, amount 1.0` + innate `AntibodySalvoSkill`
- `CtlCell.cs:54-63` — `crit .15, evasion .10, pierce 1.0` + innate `PerforinLanceSkill`
- `DendriticCell.cs:54-63` — `armor 2, magnet 260, duration 1.2, CDR .10` + innate `MhcTracerBeamSkill`

Per-tick reads: `move_speed` in `HandleMovement` (`BaseCell.cs:457`), `health_regen` in `HandleRegen` (`:445`), `area` in `UpdatePseudopodDeformation` (`:568`), `armor` in `ApplyDamage/ApplyImpulse` (`:895,939`).
EXP: `ExpChanged(float,float,int)` + `LevelUp(int)` (`BaseCell.cs:27`), `AddExp` (`:825-838`, curve `ExpToNext*1.35+15`), `DrainAtp` (`:969-974`).

### Hands-on
Open `Macrophage.cs:19-34` (`SetupCellIdentity`) and `BaseCell.cs:271-314` (`_PhysicsProcess` → `CellSkillManager.UpdateAllSkills`). Change nothing; just trace one `move_speed` read.

### Done-check
- [ ] Why are passives only allowed to touch these 19 stats? (Survivor-like philosophy, `docs/stat.md`).

---

## Step 4 — Skills, passives, organelles (90 min)

### Goal
Know skill lifecycle, slot rules, and how builds are assembled.

### Files
- `scripts/skills/BaseSkill.cs:13,30-102,114-243`
- `scripts/skills/SkillManager.cs:12,16-20,44-134,154-208`
- `scripts/core/SkillIds.cs:10-42` — 17 active + 13 passive IDs
- `scripts/core/UpgradeManager.cs:17-39,142-412`
- `scripts/gear/Gear.cs:13-53`, `ReceptorSpikes.cs:16-108`
- `scripts/core/GearChamber.cs`, `scripts/core/LoadoutManager.cs`

### Hierarchy
```
CharacterBody2D → BaseCell → Macrophage / Neutrophil / B / CTL / Dendritic
Node2D → BaseSkill → active (Perforin, Antibody, ROS, Defensin, Grasp, ...) / passive (Actin, Bilayer, ...) / TreeStatBundleSkill
Node2D → SkillManager (container, NOT a skill)
Node2D → Gear → ReceptorSpikes
Area2D → AntibodyMissile (exception, not BaseSkill)
Node2D visuals (transient): LanceBeamVisual, PoreDecal, BarbProjectile, PseudopodChainVisual, RosJet
```

### Lifecycle
- `BaseSkill.Setup(host)` (`BaseSkill.cs:30-54`): cache `Host`, resolve `Stats`, if `IsPassive` → `ApplyPassiveModifiers()` immediately.
- `SkillManager.Setup` (`SkillManager.cs:36-39`) only stores host. `EquipActive/EquipPassive` (`:44-106`): 5+5 slots, innate guard `if occupied && IsInnate return false` (`:64,91`) — slot-0 innate weapons cannot be overwritten. `AssignActiveSlot/AssignPassiveSlot` (`:108-134`): free old, `AddChild`, `Setup`, emit `SkillsChanged`.
- Tick: `BaseCell._PhysicsProcess` → `SkillManager.UpdateAllSkills` (actives only, `:154-162`) → `BaseSkill.UpdateSkill` (`:56-73`): passives early-return; actives decrement `CooldownTimer` and `Trigger()` at zero.
- Fire: base `Trigger()` only resets timer (`:75-78`). Overrides do work — e.g. `PerforinLanceSkill.Trigger (:33-58)`, `AntibodySalvoSkill.Trigger (:37-60)`, `RosTorrentSkill.Trigger (:32-62)`, `DefensinBarbsSkill.Trigger (:36-64)`.
- Stat helpers: `GetCalculatedCooldown/Damage/Area/Amount/Pierce/Speed/Duration` (`BaseSkill.cs:150-243`), `GetDamage(base,out dmg,out crit)` (`:114-119`).
- `Upgrade()` (`:80-94`): passives remove→level→re-apply; actives level++. `_ExitTree` (`:96-102`) removes passive modifiers.

### Draft engine (`UpgradeManager`)
- `ActiveCatalog` (`:17`), `PassiveCatalog` (`:39`), `CatalystActiveLevel=5` (`:78`), `CatalystPairs` (`:80`), `IsCatalystReady` (`:93`)
- `GenerateChoices(player,3)` (`:142`), `ApplyChoice` (`:305`): `new_active` (`:322`), `new_passive` (`:337`), `new_gear` (`:352`), `upgrade_*` (`:379`), `heal_fallback` (`:394`)

### Organelles
- `Gear.AttachTo/GetStat` (`:13-53`) reads `Host.Stats.GetStat`.
- `GearChamber`: `MaxSlots=4` (`:19`), `BaseEnergy=6` (`:20`), `BackpackCap=24` (`:21`), `CanEquip/ValidateSlots/Equip/Unequip/Swap/AddToBackpack/Discard` (`:140,204,257,282,325,335,349`)
- `LoadoutManager`: `MaxProfiles=3` (`:18`), `GetActiveSlots` (`:166`), `SetSlots` (`:176`) — `Main.ApplyChamberLoadout (Main.cs:352-369)` deploys it; empty = nothing equipped, stale/locked entries skipped.

### Hands-on
Read `PerforinLanceSkill.cs:33-115` end-to-end: `FindTargetDirection` → `GetCalculatedAmount/Pierce` → `ExecuteLanceStrike` → `DealDamage` → visuals. Then read one passive e.g. `PassiveActinPolymerization.cs:29-43`.

### Done-check
- [ ] Draw skill attach tree: Cell → SkillManager → BaseSkill children with Host/Stats back-pointers.
- [ ] Explain innate guard + catalyst (5 active + 5 passive → superweapon).

---

## Step 5 — Combat damage pipeline (45 min)

### Goal
Trace any damage number from trigger to death.

### Files
- `scripts/combat/CombatHelper.cs:15-44`
- `scripts/combat/IDamageable.cs:11`
- `scripts/combat/TargetingService.cs:38-97`
- `scripts/combat/ProjectileManager.cs:54-56,105-249,262-355`
- `scripts/combat/VfxManager.cs:15-82,192-209`, `VfxType.cs:3-12`
- `scripts/ui/DamageNumberSpawner.cs:19-88`
- `scripts/enemies/BaseEnemy.cs:316-375,415-472,519-535`

### Standard path (Perforin example)
1. `GetDamage(BaseDamage,out dmg,out crit)` — `might` + crit roll (`BaseSkill.cs:168-198`)
2. Target: `TargetingService.FindTargetDirection/CollectInRadius/FindNearest` (iterates `BaseEnemy.ActiveEnemies`) or capsule scan (`PerforinLanceSkill.cs:89-114`, `beamWidth=24*area`)
3. Dispatch: `CombatHelper.DealDamage(target,dmg,Host,isCrit)` → `IDamageable.TakeDamage` else duck-type `take_damage`
4. Intake `BaseEnemy.TakeDamageInternal`: fibrin absorb → `BossPhase.ApplyDamageReduction` → opsonized mult → `max(1,dmg-Armor)` → `NotifyHealthChanged` → numbers/telemetry/lifesteal/audio/VFX/flash → `Die` if ≤0
5. FX: `VfxManager.Play(...)` (pooled 16/type) + code-drawn transients + `LaserGlow`
6. Numbers: `ShowDamage` (yellow/13, gold crit/18), `ShowPlayerDamage` (red), `ShowHeal`, `ShowEvaded/Blocked`

Branch paths:
- Batched: `DefensinBarbsSkill` → `ProjectileManager.Spawn(...)` (4096 circular buffer + `ProjectileData` + QuadTree query + multimesh sync). Fallback when no singleton: per-enemy `BarbProjectile`.
- Homing: `AntibodyMissile:Area2D (mask=2)` → `AreaEntered→DealDamage` + `VfxManager.Play(OpsoninBind)`.
- Scene projectile: `RosTorrentSkill.FireJet` → `ros_jet.tscn` → `RosJet.Setup`.
- Contact/organelle: `BaseCell.ProcessContactDamage (BaseCell.cs:813-823)` → `enemy.TryContactStrike` (1 hit/s); `ReceptorSpikes._PhysicsProcess (:33-79)` → `Intercept (:89-108)`.
- Player intake (reverse): `BaseCell.ApplyDamage (:865-935)`: invuln→evaded→`RollEvasion`→`RollBlock`→armor DR→`ShowPlayerDamage`→death/trauma/flash.

### Hands-on
Zero RNG per `AGENTS.md`: `Stats.SetBase("block",0)`, `Stats.SetBase("evasion",0)` before asserting damage in tests. Find one usage in `tests/TestContactDamage.cs:100`.

### Done-check
- [ ] Trace `Die`: `PlayEnemyDeath` → `player.AddExp(Atp*ExpGainMultiplier)` (sole EXP path) → `TrySpawnDrop` → `RecordEvent("pathogen_killed")` → telemetry → `EnemyDied` → `QueueFree`.

---

## Step 6 — Enemies, steering, spawner (60 min)

### Goal
Know how 500 entities stay alive without melting CPU.

### Files
- `scripts/enemies/BaseEnemy.cs:11,67-98,216-300,316-535`
- `scripts/enemies/PathogenSpawner.cs:21-90,138-580`
- `scripts/enemies/EnemySteering.cs:32-208`, `EnemyThreatMode.cs:8-23`
- `scripts/enemies/bosses/SubBosses.cs`, `TerminalBosses.cs`
- `scripts/enemies/hazards/` — `DormantToxinVesicle.cs`, `BiofilmArea.cs`, etc.
- `scripts/combat/BossPhaseComponent.cs`

### Enemy lifecycle
Registry static HashSet (`BaseEnemy.cs:67-68`), `_EnterTree+_Ready` double-add, `_ExitTree` remove. `_Ready`: `AddToGroup("pathogens")`, `CurrentHealth=MaxHealth`, randomize drift/breathe/wander, ensure `AilmentController`, cache `BossPhaseComponent`, `EnsureCollisionNodes` (passive `Area2D Layer2/Mask0/Monitoring=false` — cuts broadphase), `SetupEnemy()` virtual.
`_PhysicsProcess`: cull (headless skip; non-boss off-screen return), stun/slow, `HandleBrownianDrift + CustomPhysicsProcess`, 30Hz breathe scale.
Movement: `FloatSpeed*SlowFactor` × agglutinate × `BossPhase.CurrentSpeedMult`, steering via `EnemySteering.GetDirection` or wander, `Position+=Velocity*dt` (pure `Node2D`).

Contact: table `ContactDamageByEnemyId` (`:415-440`, 3-12; bosses 16-30), `TryContactStrike` 1 hit/s (`:461-472`).

### Spawner (static library)
- `EscalationInterval=180`, `StandardRunDuration=900`, `PhaseCount=5` (`:21,24,26`)
- Caps: `MaxActiveNormal=300`, `MaxActiveSwarm=450`, `MaxActiveEndless=500`, `SwarmWindowSeconds=30`, `MaxBackfillPerTick=7` (`:30,33,43,166,169`)
- Overdrive ladder: `OverdriveHealthBonus {0.5,1.2,2.2,3.6}`, `OverdriveSpeedBonus {0.15,0.3,0.5,0.7}`; cycle 5+ health `×4.6*2^extra`, speed cap +100% (`:55,56,120-130`)
- `ExpGainMultiplier=0.3` (`:49`), Hard `1.4x HP, 1.2x speed` (`:90,93`)
- Run-scoped `RunConfig{HardMode,Overdrive}` (`:63-66`), `ConfigureRun` (`:71-74`) called from `Main._Ready`
- Factories: `CreatePathogen` (`:211-252`), phase pools (`:177-209`), `GetPhaseIndex/Pool` (`:257-280`), `SpawnWave` (`:285-298`), `SpawnElite` (`:303-323`, boost `1+0.6*tier`), `SpawnSwarm` (`:328-340`), `SpawnSubBoss/TerminalBoss/RaidBoss` (`:345,380,401`), `Backfill` (`:441-465`), `GetOffscreenSpawnPoint` (`:471-490`), `ClampToArena` (`:504-511`)

Special clustering in `SpawnSingle`: `staph`→3 cocci with `FibrinShield=1`, `norovirus`→10 units.

Bosses: `SubBossEnemy (BaseScore=600, Contact=16)`, `TerminalBossEnemy (BaseScore=3000, Contact=26)`, `BossPhaseComponent` (phases, DR, speed mult incl. hard-enrage ×1.4). Terminal map table in `SpawnTerminalBoss/CreateTerminalBoss`: alveolar→SyncytialMegaCapsid, hepatic→PlasmodiumMacroSchizont, gastric→HpyloriBiofilmCore, BBB→PrpscAmyloidAggregate, default wound→MrsASuperColony.

### Steering + layers
Modes: `Drifter/ChemoChaser/Interceptor/Standoff/Invader` (`EnemyThreatMode.cs`).
`ConfigureArena` → 6 anchors on 0.82-ring. `SeekPlayer` (jitter 0.22), `InterceptPlayer` (lead `clamp(vel*0.6,100,200)`), `Standoff` (approach >1.15×, retreat <0.6×, orbit), `InvadeTissue` (latch ≤max(8,40) → Zero for ulceration).
Layers: enemies passive `Layer2/Mask0`; skills `Layer0/Mask2`; enemy shots `Layer0/Mask1`; player body `1|4`, sensor `1|2`; fibrin walls `Layer4/Mask0`. Organ/overdrive drift writes `Position` directly, skipping `is_being_eaten`.

### Hands-on
Read `PathogenSpawner.cs:9-17` (timeline doc) + `SpawnSingle:513-558`. Then `EnemySteering.cs:55-65` dispatch.

### Done-check
- [ ] Why must scaling run pre-`_Ready`? (`_Ready` copies Max→Current).
- [ ] Why are enemies `Node2D` not `CharacterBody2D`?

---

## Step 7 — Directors: wave / boss / overdrive / organ / settlement / neutral (60 min)

### Goal
Understand the run clock: 03:00 → 06:00 → 09:00 → 12:00 → 15:00 → endless.

### Files
- `scripts/directors/WaveDirectorComponent.cs:18,35-123,145-191`
- `scripts/directors/BossEncounterManager.cs:40-237`
- `scripts/directors/OverdriveDirector.cs:11-329`
- `scripts/directors/OrganEnvironmentSystem.cs:37-109`
- `scripts/environment/*.cs` (5 organs)
- `scripts/directors/RunSettlementService.cs:32-181`
- `scripts/directors/NeutralMatterManager.cs:25-132`
- `scripts/directors/IRunContext.cs`

Wave (`WaveDirectorComponent`):
- `TerminalPhaseReached` event (`:18`), `PhysicsTick` (`:70-102`)
- `≥180` elite raid (tier-1 + sfx), `≥360` first swarm (2 elites + `SwarmWindowTimer=30`), `≥720` extreme swarm (30s + tier-2), `≥RunGoalSeconds(900)` → `TerminalPhaseReached` → `BossManager.EnterBossLockdown`
- `ProcessBackfill`: `deficit=ActiveScreenCap-ActivePathogenCount`, `batch=min(deficit,7)`. `ActiveScreenCap`: endless+≥900→500 else swarm?swarm:normal. Count only `BaseEnemy` children.

Boss (`BossEncounterManager`):
- `PhysicsTick`: `≥540` once → `TriggerSubBossEncounter` (`SpawnSubBoss`, `EnemyDied→OnSubBossDefeated`, BGM `boss`)
- `OnSubBossDefeated`: restore map BGM, reward `AddExp(ExpToNext-CurrentExp)`
- `EnterBossLockdown` (15:00 event): `BossLockdownActive=true`, `SpawnTerminalBoss`, BGM `boss` (or `boss_final` endless). Null container/player → `EndRun(false,system_failure)`.
- `OnTerminalBossDefeated`: `TerminalBossNeutralized=true`; endless → clear lockdown, continue; standard → `EndRun(true,specific_neutralization)`
- `CheckTerminalBossState` (only under lockdown): vanished → endless release + warning; standard `EndRun(false,system_failure)`
- `ProcessBossRaids(cycle)`: `cycle≥2` (18:00 first), `≥1800s` triple else twin, other-organ maps, HUD alert

Overdrive (endless only, `OverdriveDirector`):
- Cycle announce, `cycle≥2` shear storm (`34+18*min(cycle,5)`, sinusoidal, `CurrentFluidVector*0.7`), fibrin nets every 4.5s (9s/90px/slow 0.45), `cycle≥3` bile surge (strip armor 3s every 15s), `cycle≥4` acid tide (safe radius 2300→850, 6dps + slow 0.6 outside), `cycle≥5` composite + exponential HP
- `ProcessAfflictions`: viscosity move penalty, febrile burn (%maxHP/5s), antigenic drift clears opsonize/20s

Organ (`OrganEnvironmentSystem` + `scripts/environment/`):
- `Initialize(hard)`, `ConfigureArenaVisuals`, `PhysicsTick`: `Current.Tick→PlayerDrift`, `CurrentFluidVector=FluidVector`. Gated by `SettingsManager.MapEffectsEnabled` (off → tints only).
- Wound: suction (16,10), fibrin clots/7s max 6; Alveolar: 12s breath + CDR pockets/6s max 3; Hepatic: flow drag + armor-strip/18s + fenestra walls/24s max 3; Gastric: churn + safe zones/9s max 3 + acid surges/14s; BBB: shear (±26,±12) + 5 astrocyte pillars + invert pulse/11s×1.6s

Settlement (`RunSettlementService.TryEndRun :32-140`):
- Guard `RunEnded`; victory validated by `CanSettleVictory(time,bossNeutralized,endless)` = `!endless && bossNeutralized && time≥900-0.01`. Endless victory always rejected. Shortfall warns, returns false (Main does NOT raise `RunEnded`).
- Victory: BGM victory + `RecordMapClear`; defeat: BGM defeat. Causes: `specific_neutralization` / `membrane_rupture` / `system_failure`.
- `RecordRun` (downgrades unearned victory), endless → `SteamBridge.SubmitEndlessLeaderboard`, open `RunRecordsModal.OpenSettlement` + `PauseManager.PushHold(Settlement)`
- Rule: **standard victory = 15:00 + terminal-boss kill** (`docs/record.md §3.1`); **defeat = membrane zero**; **endless = defeat-only**; boss-vanish = `system_failure`.

Neutral (`NeutralMatterManager`): seed 3 vesicles, vesicle/6s cap 10, ulcer mist gated `HostUlceration.Pulses≥4` (severe ≥8). Neutrals never consume screen-cap (not `BaseEnemy`).

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

Per-view: Class (`SetupClassButtons:577-606`, `SelectClass:608-720`, radar `BioRadarChart.SetStats`, lock via `AchievementManager`), Loadout (`RefreshLoadoutView:746-748 → LoadoutView.Open`), Passive (`SelectPassiveBuild:758-766`, `Purchase/Refund/Reset :902-931`, profile tabs code-built `:812-900`), Map (`SetupMapButtons:941-972`, `SelectMap:974-1061`, `HoloScanner.SelectOrgan`, difficulty/endless gates), Title auxiliaries (Codex/Records/Achievements/Settings/Quit).

Run root (`scenes/main.tscn:60-183`): `Main` + `Background/{ArenaBG,MicroscopeParallax,CapillaryTissue,FluidParticles,ArenaBorders}` + `ArenaBoundaries` + `EnemyContainer` + directors + instanced `Macrophage` + `Camera2D(CameraFollow)` + `MicroscopePostProcess` + instanced `HUD` + `UIOverlay/RunRecordsModal`.

### HUD (`scripts/ui/Hud.cs:17-348`, `scripts/ui/hud/`)
`Hud` is mediator, no readout logic. `_Ready:118-198` constructs 6 sub-views (`Vitals/SkillBar/WaveTimer/PauseMenu/TutorialCue/Toast`), `Bind`s each, instantiates `UpgradeModal` if missing, wires signals, language listener, FPS label. `_Process:206-219` fans out `Tick`s.
`ConnectPlayer` (`:228-256`, caller `Main.cs:176-182`): fans out to `_vitals/_skills/_pause/_tutorial`, typed `BaseCell` vs fallback GDScript signals.

Vitals (`VitalsView.cs:13-312`): `Bind(:58-73)`, `ConnectPlayer(:76-99)` subscribes `StatsChanged/ExpChanged`, `ConnectFallback(:102-110)`, `UpdateExpDisplay(:148-161)`, throttled text (`:181-201`). Emission: `BaseCell.AddExp(:825-838)` emits single `ExpChanged`; `DrainAtp` also emits.

### Upgrade chain
`AddExp → LevelUp+ExpChanged → TutorialCueView.OnPlayerLevelUp (:152-173) → bullet-time (0.5s, TimeScale→0.05) → UpgradeModal.OpenUpgradeModal (:95-125, queues _pendingLevels, PauseManager.PushHold(UpgradeDraft)) → GenerateChoices → PopulateCards (:127-246, icons via AssetPaths, catalyst aura) → OnCardClicked (:306-323) → ApplyChoice → ResolveChoice (:326-343, PopHold, deferred next)`. Organelle swap (`:349-549`): auto-equip or `EnterSwapMode` with `SwapSlot0-3`, Store/Discard/Cancel.

Scene: `upgrade_modal.tscn:49-390` fixed shell (3 fixed `Card0-2`, each `SelectButton+Vbox/{IconTexture,Title,Badge,Desc}`).

### Hands-on
Open `hud.tscn:170-728`. Find `HPContainer`, `EXPContainer`, `BottomExpBar`, `MembraneGauge`. Then find their `Bind` in `VitalsView.cs:58-73`. Rename nothing.

### Done-check
- [ ] Explain container+loop vs fixed-shell with one example each (`CodexModal.RenderCurrentTab` vs `upgrade_modal Card0-2`).
- [ ] Why must screenshots re-run after any pixel change? (C# no hot-reload; stale `game` session shows old pixels).

---

## Step 9 — Meta: tree, achievements, records, i18n (45 min)

### Goal
Understand out-of-run progression.

- Passive tree (`PassiveTreeManager.cs`): topology `passive_tree.json`, per-cell levels/3 profiles, connectivity/purchase/refund, `CreateSkill` factory, live bonus from achievements. `Main.ApplyTreeLoadout` attaches `TreeStatBundleSkill`s. View: `PassiveTreeView.cs` (shell in `passive_view.tscn`, graph code-drawn, nodes `TreeNode_{id}`).
- Achievements (`AchievementManager.cs:28-507`): `achievements.json` + Steam, `RecordEvent/EvaluateThreshold/RecordMapClear`, `IsEndlessUnlocked` (needs `wound_hard_clear`), `ApplyMapRewards/SyncMapUnlocks`, gallery `AchievementGalleryView/Card/Toast`.
- Records (`RunRecordManager.cs` + `RunRecordsModal.cs`): `OpenHistory` vs `OpenSettlement`, grades, KPM/score.
- Loadouts (`LoadoutManager.cs` + `LoadoutView.cs:131-577`): `gear_loadouts.json`, scratch chamber, `BuildBackpackCards:255-286` instantiates `gear_slot.tscn`, profile tabs code-built.
- Unlocks (`GearUnlockManager.cs`): `IsUnlocked`, `TrySpawnDrop`.
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
- FibrinShield — hit-absorbing shield (staph clusters start with 1).
- Opsonize — damage-taken multiplier (antigenic drift clears it).
- Backfill — kill-driven refill up to `ActiveScreenCap`, max 7/tick.
- Lockdown — 15:00 terminal-boss phase, backfill paused.
- Overdrive — endless ≥15:00 escalation, cycles/180s.
- Ulceration — H.pylori invader meter (≥4 mist, ≥8 severe).
- Chamber/Backpack/Energy — 2×2 equip, 24-cap storage, 6+generator budget.
- Settlement — victory validation + record persist + modal.

## Appendix B — Key docs map

`docs/spec.md` (master GDD), `stat.md` (19 stats), `skill.md` (17+13+catalyst), `cell.md` (5 cells), `pathogen.md` (enemies), `map.md` (5 organs), `passivetree.md`, `achievement.md`, `record.md` (victory rule §3.1), `tutorial.md` (§2 cues), `endgame.md` (§3.1 endless), `real.md` (bio fidelity), `feedback.md`, `cheats.md`, `README.md`.

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

1. `partial class` everywhere: `scripts/Main.cs:25`, `scripts/player/BaseCell.cs:18`, `scripts/player/Macrophage.cs:13`, `scripts/ui/MainMenu.cs:9`. Reason: Godot generator adds hidden glue. Review rule: never remove `partial`; missing `partial` = broken scene binding.
2. `[Export]` = editor-wired dependency: `scripts/Main.cs:29-41` (`StaphScene`, `ArenaSize`, `WaveDirector`, `BossManager`, ...). Review rule: if you rename an `[Export]` prop, check `scenes/main.tscn:60-68` `WaveDirector = NodePath(...)` wiring still resolves; `ResolveDirectors()` (`Main.cs:269-323`) creates code fallback via `GetNodeOrNull` + `new`, so missing scene wiring silently changes behavior.
3. Nullable + `GetNodeOrNull<T>`: `Main.cs:142-148` (`Player`, `HudNode`, `EnemyContainer`, `ArenaBg`). Review rule: `GetNode` throws on miss, `GetNodeOrNull` returns null — this repo prefers null + fallback. Any new `GetNode` is a smell; any unchecked deref of `GetNodeOrNull` result is a defect under `Nullable enable`.
4. C# events for in-run signals: `CellStats.StatChanged` (`CellStats.cs:13`), `BaseCell.ExpChanged/LevelUp` (`BaseCell.cs:27`), `WaveDirector.TerminalPhaseReached` (`WaveDirectorComponent.cs:18`). Subscribed in `VitalsView.ConnectPlayer (:79-80)`, `Main.ResolveDirectors (:322)`. GDScript fallback only in `VitalsView.ConnectFallback (:102-110)` via `Callable`. Review rule: setting `CurrentExp` directly does NOT fire `ExpChanged` — must reset HUD snapshots (`LastCurrentExp`) in tests.
5. `Callable.From + TweenMethod` for time effects: `TutorialCueView.cs:181,200` (`Engine.TimeScale 1.0 -> 0.05 -> 1.0`). Paired with `GameManager.StartGame (:379,404)` resetting `Engine.TimeScale=1.0` before `ChangeSceneToFile`. Review rule: any early-return that skips timescale restore = permanent slow-mo.
6. `QueueFree` discipline: `Main.cs:160` (old cell on class swap), `SkillManager.AssignActiveSlot` (old skill). Skills/passives clean up in `_ExitTree` (`BaseSkill.cs:96-102` removes modifiers). Review rule: leaked passive = permanent stat buff.
7. `static` vs autoload `Instance`: autoloads expose `Instance` (`AudioManager.cs:12`, `AchievementManager.cs:22`); `PassiveTreeManager/LoadoutManager/UpgradeManager/PathogenSpawner` are `static` + `JsonStore`. Review rule: static state leaks across runs unless `Reset`/`ReloadFromDisk` is called (`Main._Ready` calls `HostUlceration.Reset`, `PathogenSpawner.ConfigureRun`).
8. `ChangeSceneToFile`: `GameManager.cs:382,407,415,420` (`main.tscn` vs `main_menu.tscn`). Review rule: must reset `Engine.TimeScale` + validate unlocks before switch.

Hands-on: open `BaseSkill.cs:30-102` and classify each member as lifecycle (`Setup/Update/Trigger/Upgrade/_ExitTree`) vs stat helper (`GetCalculated*`).

## Appendix E — Godot concepts used here (review-minimum)

1. Scenes are composition: `scenes/main.tscn:1-18` `ExtResource` decls + `60-183` nodes; `main_menu.tscn:133-143` composes 6 views via `instance=ExtResource`. Each `.tscn` must be self-contained (all `ExtResource/SubResource` declared in-file). Review rule: scene diff must show matching `id="..."` decl for every `ExtResource("...")` use.
2. Autoloads = always-alive singletons: `project.godot:18-26`. Review rule: new global state belongs in an existing autoload/static, not a new autoload.
3. Groups = runtime registry: `player` (`BaseCell.cs:207`, `Main.cs:172`), `pathogens` (`BaseEnemy.cs:79`), `hazards` (`BioHazardArea.cs:32`), `neutral_matter` (`EnvironmentProps.cs:36`), `telegraphed_attacks` (`TelegraphedAttack.cs:51`), `enemy_shots` (`EnemyPellet.cs:47`). `TargetingService` iterates `BaseEnemy.ActiveEnemies`, not groups. Review rule: missing `AddToGroup` = invisible to scans/HUD.
4. Physics layers (`project.godot:96-98`): 1=Player, 2=Pathogens, 4=Environment. Enemies are passive `Area2D Layer2/Mask0/Monitoring=false` (`BaseEnemy.cs:185-214`) — detected BY player sensors/skills, cutting broadphase at 300-500 bodies. Skills `Layer0/Mask2`, enemy shots `Layer0/Mask1`, player body `1|4` sensor `1|2`. Review rule: any new `CollisionShape` must state layer/mask or it defaults wrong.
5. Node lifecycle: `_Ready` (wire once) vs `_PhysicsProcess` (deterministic tick) vs `_Process` (render/sync). `Main._PhysicsProcess` order is contract (wave→boss→organ→overdrive→neutral→backfill). `Main._Process` only syncs `SwarmRenderer`. Review rule: gameplay logic in `_Process` = frame-rate dependent bug.
6. No C# hot-reload: after `dotnet build`, restart run (`stop` → `project_run(main)`), drive with `game_eval`, then `editor_screenshot source="game"`. Review rule: screenshot from stale session = false evidence.
7. Persistence: `JsonStore` + `user://*.json`; tests isolate to `user://test_*` with `DropsEnabled=false`. Headed cheats touch real profiles. Review rule: test that writes non-`test_*` path is a defect.

Hands-on: open `scenes/main.tscn:104-148` and map each node to its `GetNodeOrNull` in `Main.cs:142-148,271-311`.

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
