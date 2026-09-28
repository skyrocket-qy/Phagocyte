# Architecture Rules (Phagocyte C#)

Enforced by `python3 scripts/check_arch.py` (`make check-arch`).
Ported from Vistrace-godot's 5-layer model, adapted to Phagocyte's
`scripts/` feature-folder layout — no file moves required.

## 1. Layers

```text
scripts/
├── core/GameEvents.cs, IStatHost.cs, BodyDeformation.cs, QuadTree.cs,   # [L1] DOMAIN
│   SkillIds.cs, Stat.cs, TextFormatter.cs
├── core/assets/* (except none), core/data/CatalogLoader.cs,       # [L1] DOMAIN-infra
│   DataPaths.cs, DataLoadException.cs
├── core/AchievementManager.cs, AudioManager.cs, GameManager.cs,    # [L2] AUTOLOAD
│   RunRecordManager.cs, SettingsManager.cs, EquipmentUnlockManager.cs,
│   LoadoutManager.cs, PassiveTreeManager.cs, PauseManager.cs,
│   JsonStore.cs, KeyBindings.cs, SteamBridge.cs, GameEvents.cs,
│   data/CatalogBuilders.cs, data/DataValidator.cs
├── combat/, player/, camera/, enemies/, skills/, directors/, stages/,  # [L3] GAMEPLAY
│   equipment/, testing/
│   + core/ActorStats.cs, EquipmentChamber.cs, EquipmentDrop.cs,
│     RunTelemetryManager.cs, UpgradeManager.cs
├── ui/ (except DamageNumberSpawner.cs)                             # [L4] UI
├── GameRoot.cs                                                     # [ROOT] composition root (unchecked)
└── check_arch.py                                                   # enforcement
tests/                                                              # [T] can import all (never checked)
```

Engine rule (Phase 0): `Game.*` namespaces only. Heroes, enemies,
stages, skills, equipment and mutators are data rows (JSON) driven by
generic archetype executors (`PlayerActor`, `EnemyActor`, stage effects,
salvo/beam/nova/zone/strike/aura, `StatPassive`) — never per-domain
subclasses. A new behavior extends its archetype's schema.

The full file→layer map lives in `scripts/check_arch.py`
(`DOMAIN_FILES`, `AUTOLOAD_FILES`, `GAMEPLAY_PREFIXES`, `GAMEPLAY_FILES`,
`COMPOSITION_ROOT_FILES`). Update the map — not the prose — when files move.

## 2. Dependency rules

| Layer | May import | Must NOT import |
|---|---|---|
| **L1 Domain** | C# stdlib, `System.Text.Json`, Godot math (`Vector2`, `Rect2`, `Color`, `Mathf`), `Resource`/`FileAccess` IO | `Node`/`Control`/`CharacterBody2D`, SceneTree (`GetTree`, `GetNode`, `CurrentScene`), gameplay actors (`PlayerActor`, `EnemyActor`, `SkillManager`, `EquipmentChamber`, `ActorStats`), autoloads (`GameManager`, `AudioManager`, `SettingsManager`, `AchievementManager`, `PassiveTreeManager`), any `scripts/ui/` type |
| **L2 Autoload** | L1, Godot `Node` lifecycle | Concrete gameplay actors, any `scripts/ui/` type |
| **L3 Gameplay** | L1, L2, Godot 2D physics | Concrete UI types (`Hud`, `*Modal`, `*View`, `UIOverlay/` lookups). SOLE EXCEPTION: the `DamageNumberSpawner` autoload (lives in `scripts/ui/` pending a move; gameplay→autoload is legal) |
| **L4 UI** | L1, L2, L3 (read/intent only) | Sibling-modal manipulation stays signal-based |
| **ROOT GameRoot.cs** | All (assembler wiring) | — |

## 3. Inversion points (how gameplay talks to UI without depending on it)

- `IDirectorHud` (`scripts/directors/IRunContext.cs`): `ShowOverdriveAlert`,
  `PauseInputSuppressed`, `ResumeGame`. Implemented by `Game.UI.Hud`.
  Directors touch `IRunContext.HudNode` only.
- `IRunSettlementModal`: `OpenSettlement(Dictionary)`. Implemented by
  `Game.UI.RunRecordsModal`. Resolved via
  `GetNodeOrNull<IRunSettlementModal>`, never the concrete type.
- `GameRoot` keeps the concrete `Hud? HudNode` property for wiring/tests and
  satisfies the interface explicitly
  (`IDirectorHud? IRunContext.HudNode => HudNode;`).

## 4. Grandfathered exceptions (zero-growth allowlist in `check_arch.py`)

1. `QuadTree.cs` `candidate is Node2D` fallback — removal changes lookup
   behavior for callers omitting `positionOf`; prefer the explicit callback.
2. `DamageNumberSpawner.cs` living in `scripts/ui/` while registered as an
   autoload — target: move to `scripts/core/` (namespace stays resolvable,
   no `.tscn` churn since autoload name is unchanged).
3. `data/CatalogBuilders.cs` + `data/DataValidator.cs` classified L2, not L1:
   query-only reads of autoload catalogs for build/validation.
4. `core/GameEvents.cs` classified L2: event-bus wiring between two autoloads
   (`AchievementManager` → `GameManager`); `Raise*` callers stay decoupled.

Do NOT extend this list without updating the checker and this doc together.
