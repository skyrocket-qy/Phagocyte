# Game Configuration Pipeline (`tools/config`)

Type-safe, single-source-of-truth data authoring and validation system for Phagocyte. All game data is defined in TypeScript with compile-time foreign key checking and exported directly into `assets/data/*.json`.

---

## Directory Structure

```
tools/config/
├── src/
│   ├── ids/                         # Per-domain string enums (the FK vocabulary)
│   │   ├── ailment.ts               # AilmentId, AilmentChannel, AilmentStackRule
│   │   ├── skill.ts                 # SkillId (31), SkillArchetype, SkillType
│   │   ├── class.ts                 # ClassId (5 player classes)
│   │   ├── enemy.ts                 # EnemyId (35), ThreatMode
│   │   ├── codex.ts                 # BossCodexId (11 display rows)
│   │   ├── stage.ts                 # StageId, StageEffectKind, StageProp
│   │   ├── gear.ts                  # GearId (24), GearCategory
│   │   ├── trait.ts                 # TraitId (54), TraitRarity, TraitBranch
│   │   ├── achievement.ts           # AchievementId (19), AchievementStatKey
│   │   ├── stat.ts                  # StatId (19), ModType, PassiveModMode
│   │   ├── audio.ts                 # SfxId (37, manifest mirror), BgmId
│   │   ├── ui.ts                    # UiId (16)
│   │   └── index.ts                 # Barrel re-export
│   ├── lib/                         # Framework primitives (mirrored, game-agnostic)
│   │   ├── colors.ts                # Hex color helpers & named palette
│   │   ├── palette-types.ts         # Palette step definitions & schemas
│   │   ├── registry.ts              # IdOf & array/map helper types + defineTable
│   │   ├── shared-types.ts          # StatModifier, SrgbaColor, Vec2, resource paths
│   │   └── cross-validator.ts       # Referential completeness checker (Phagocyte rules)
│   ├── schemas/                     # Zod runtime schemas + TS interfaces per domain
│   │   ├── ailment.schema.ts        # AilmentsFile (schema 1 wrapper)
│   │   ├── skill.schema.ts          # Active skills (params passthrough) + passive skills
│   │   ├── class.schema.ts          # Player classes (visuals + innate skill)
│   │   ├── enemy.schema.ts          # Enemies (traits passthrough)
│   │   ├── codex.schema.ts          # Shared codex row (enemy + boss codex)
│   │   ├── gear.schema.ts           # Equipment (modifiers/drawback/scaling)
│   │   ├── passive.schema.ts        # Traits file + tree file (nodes/edges/starts)
│   │   ├── stage.schema.ts          # Stages (effects passthrough)
│   │   ├── progression.schema.ts    # Achievements
│   │   └── ui.schema.ts             # UI elements + stat-labels record
│   ├── data/                        # Single Source of Truth (14 authored modules)
│   │   ├── combat/                  # ailments.ts, stats.ts
│   │   ├── skills/                  # active.ts (18), passive.ts (13)
│   │   ├── class/                   # classes.ts (5)
│   │   ├── enemy/                   # enemies.ts (35)
│   │   ├── codex/                   # enemy_codex.ts (20), boss_codex.ts (11)
│   │   ├── gear/                    # equipment.ts (24)
│   │   ├── passive/                 # traits.ts (54), tree.ts (54 nodes / 93 edges)
│   │   ├── map/                     # stages.ts (5)
│   │   ├── progression/             # achievements.ts (19)
│   │   └── ui/                      # elements.ts (16)
│   ├── index.ts                     # Manifest registry bundling all 14 data sources
│   └── export.ts                    # Fast validation & mirror export engine
├── package.json
└── tsconfig.json
```

---

## Core Features & Mechanisms

### 1. Enum ID + Shift+F12 Data Navigation
All primary entity IDs are defined as string enums in `src/ids/`. In the IDE:
- **F12** on `ClassId.Macrophage` navigates directly to the enum definition.
- **Shift + F12** (Find All References) instantly reveals the class row in [`classes.ts`](src/data/class/classes.ts) and every skill/achievement referencing it.

```ts
// src/data/class/classes.ts
import { ClassId, SkillId, type ClassDef } from "../../schemas/class.schema";

export const Classes: readonly ClassDef[] = [
  {
    id: ClassId.Macrophage,
    innate_skill: SkillId.PhagocyticGrasp,
    // ...
  },
];
```

---

### 2. Inline Typo Red Squiggle Diagnostics (0ms Feedback)
Misspelled property names or invalid references trigger instant inline compiler diagnostics before saving or exporting.

```ts
// Compile-time error caught immediately by TypeScript language server
export const Stages: readonly StageDef[] = [
  {
    id: StageId.AcuteWound,
    // ❌ error TS2339: Property 'AcuteWoudn' does not exist on type 'typeof StageId'.
    // id: StageId.AcuteWoudn,
  },
];
```

---

### 3. Zero ESM Circular Import Deadlocks
Because string enums evaluate to primitive string constants at runtime, circular cross-file relationships (e.g. skills referencing `ClassId` while classes reference `SkillId`) never deadlock Node.js module initialization.

---

### 4. Stat → Modifier Unit Filtering
`ModType` (`flat`/`percent`/`percentagepoints`) and `PassiveModMode` (`flat`/`flat_once`/`mult`) are enums, so equipment/trait modifier rows can only use known units — unknown units fail `pnpm run check` instead of surfacing as runtime `DataLoadException`s.

---

### 5. Polymorphic Data via Passthrough (Tighten Later)
Game mechanics with variant-specific fields — skill `params` per archetype (strike/salvo/beam/aura/zone/nova), enemy `traits` (30 shapes), stage `effects` (6 kinds) — are `z.record().passthrough()` / `.catchall()` in pass 1. Cross-dataset FKs inside them (`on_hit.ailment`, `spawn`, `stat_id`, `sfx`) are still validated by the cross-validator. Promote hotspots to discriminated unions when a second bug class appears, not before.

---

### 6. Energy-Cost Balance Rules in Code
`CatalogBuilders` balance invariants are mirrored at authoring time: `energy_cost` must stay in `[-1, 4]`, negative-cost (generator) gear must carry a `drawback`, and every `stat`/`scaling_stat` must name a real `StatId`.

---

### 7. Unplaced-Trait & Dead-Reference Warnings
Automated checks catch dead data at validation time: traits never placed on the tree warn, and every FK miss (skill→class, class→skill/achievement, achievement→class/stage, node→trait, edge→node, effect→stat) is a build-blocking error.

---

### 8. Canonical Export Formatting
Export output is canonical `JSON.stringify(obj, null, 2)` (whole-number floats print as `3`, not `3.0`); the runtime is int/float-tolerant (`CatalogLoader` accepts both). Re-exports of unchanged data are byte-stable.

---

## Commands

```bash
# Check all types and validate data schemas without writing files (CI mode)
pnpm run check

# Validate and export all JSON files to assets/data/
pnpm run build

# Watch mode for continuous validation and export during authoring
pnpm run watch
```

Repo-level: `make check-config` (check), `make config-export` (build).

## Rules

- New ailment / skill / enemy / gear / trait / stage / achievement = new TS row. Never hand-edit `assets/data/*.json` for covered datasets — JSON files are build artifacts.
- Every FK must resolve: skills→classes/ailments/sfx, classes→skills/achievements/stats, achievements→classes/stages, tree→traits/nodes/stats, equipment→stats, stages→stats, enemies→enemies (spawn refs). The exporter fails the build otherwise. C# never names an id.
- `SfxId` mirrors `assets/audio/manifest.json` (read-only mirror + `sfx` FK check). Audio and `translations.csv` bodies stay hand-authored; `tools/config` only checks references into them.
