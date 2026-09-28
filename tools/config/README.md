# Game Configuration Pipeline (`tools/config`)

TypeScript source of truth for game data (Vistrace `tools/config` method):
Zod schemas + typed data modules + cross-dataset FK validation, exported to
`assets/data/*.json`. The JSON files are build artifacts — edit the `.ts`
sources, never the JSON by hand.

## Layout

```
tools/config/
├── src/
│   ├── schemas/      # Zod schemas + TS types (ailment, skill)
│   ├── data/         # Authored rows (ailments.ts, skills/active.ts)
│   ├── lib/          # cross-validator.ts (dup ids, on_hit ailment FKs)
│   ├── index.ts      # Manifest: file/data/schema triples
│   └── export.ts     # Validate (+ cross-check), write JSON or --check
├── package.json / tsconfig.json
```

## Commands (run inside `tools/config/`)

```sh
npm run check   # tsc + zod + cross-validation, writes nothing (CI mode)
npm run build   # validate + export JSON to assets/data/
npm run watch   # continuous validate + export while authoring
```

Repo-level: `make check-config` (check), `make config-export` (build).

## Rules

- New ailment / skill / spawner effect = new TS row. Never hand-edit `assets/data/*.json` for covered datasets.
- Spawner on_hit entries must name a real ailment id — the exporter fails the build otherwise. C# never names an ailment.
- Export output is canonical `JSON.stringify` (whole-number floats print as `3`, not `3.0`); runtime-safe (`CatalogLoader` accepts int/float variants). Re-exports are byte-stable.
- Pilot scope: `ailments.json`, `skill/active.json`. More datasets migrate here over time.
