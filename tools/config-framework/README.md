# `@games/config-framework` — pure data-authoring framework

Game-agnostic engine for TypeScript-as-source-of-truth game data: registries, shared types, validation harness, JSON export. **Zero game content** — this directory copies to a new game with no edits.

## Contents

```
src/
  registry.ts       # defineTable / DataTable / IdOf / array+map registries
  shared-types.ts   # StatModifier<T>, SrgbaColor, tuples, resource paths
  colors.ts         # hex helpers + named palette
  palette-types.ts  # PaletteStep / PaletteDef
  validate.ts       # ValidationIssue/Result, toList, enumValues, checkDupes, splitResult
  export-engine.ts  # ManifestEntry, runExporter({ manifest, validate, outDir })
  index.ts          # barrel
check-purity.sh     # guard: fails on any game reference (run in CI)
```

## Copy recipe (new game)

1. Copy this directory next to the game's config package.
2. In the game package: `"@games/config-framework": "file:../config-framework"`, `pnpm install`.
3. Write game `ids/`, `schemas/`, `data/`, a manifest, game rules on `validate.ts` helpers, and a thin `export.ts` calling `runExporter`.
4. Done. Never edit framework files per-game; bump `version` here when the engine changes and let games pin.

## Rules

- Framework code must never name a game concept (`id` vocabularies, dataset shapes, rule sections all live game-side).
- `StatModifier` stays generic over the stat vocabulary (`TStat extends string`); runtime accepts any string, membership is the game's cross-validator job.
- `check-purity.sh` enforces this — keep it green.
