# AGENTS.md — Phagocyte contributor notes

Read this before touching code, scenes, or assets. Conventions below were
earned the hard way (red suites, broken builds, ghost diffs).

## Engine generalization (Phase 0 rule — domain-decoupled, data-driven)

- Root namespace is `Game.*`. Never `Phagocyte.*` in `scripts/`, never a domain literal in engine code (`macrophage/staph/fibrin/opsonize/acute_wound`-style tokens belong in JSON + locale + art only).
- Engine sees only `id`, `StatBlock`, `tags[]`, `EffectSpec{effect_id, magnitude, duration, radius, tint}`. No `if (id == "<domain>")` branches.
- New hero / enemy / skill / stage / equipment / mutator = new JSON row + composition of generic archetypes, never a new `.cs` class. Heroes: one `PlayerActor`; enemies: one `EnemyActor` + trait components; skills: `ProjectileSalvo/Beam/Aura/NovaBlast/DashStrike/Zone` + `StatPassive/ProcPassive`; stages: one `StageEnvironment` + `BuffZone/BlockerWall/DotZone/SlowZone`.
- A "special" case extends its archetype schema (new field/component, all rows migrated) — no one-off classes, no per-domain subclasses.
- No compat shims for renames: no `[Obsolete]` aliases, no old↔new key maps. Rename directly, fix call sites forward (extends "Code size" rule below).
- Verify with zero-residual grep before declaring done: `BaseCell|BaseEnemy|Pathogen|Fibrin|Opson|Macrophage|Phagocyte\.` must return zero hits in `scripts/` (JSON/locale/art exempt).

### Damage payload vs pipeline rule

- `HitPayload` (`scripts/combat/DamageService.cs`) carries only what is **frozen at cast time** (raw damage, type, force flags, faction, attacker id, knockback impulse, effect specs). `DamagePipeline` (`scripts/combat/DamagePipeline.cs`) reads only what is **live at hit time** (armor/resists/shields, evasion/block, HP thresholds, attacker stats for crit/leech). Test for a new field: *"could this change between fire and impact?"* No → payload; yes → pipeline.
- Flight/render state (`Position`, `ProjectileTypeIndex`, lifetime) lives **beside** the payload in `ProjectileData`, never inside it. Payload is small value-types + shared effect refs — a new field must justify its bytes and migrate all spawner call sites.
- Resolution (`InstanceFromId`, status slots, catalogs) happens **on hit only**, never per-tick. Enforced by `check_arch.py`: no deleted-interface refs (`ILeechable`/`ISlowable`/`IStunnable`), `ResolveHit` callers limited to combat simulation layers, no dictionary reads in `ProjectileManager._PhysicsProcess`.

## Performance budget (1000+ entities — constraint, not aspiration)

- Every design assumes 1000+ live entities. Current caps (300/450/500) are tuning, not architecture limits.
- No per-entity Nodes for pure state: numbers/timers/status live in POCO containers, ticked inline in the owner's existing `_PhysicsProcess`. A `Node` wrapper is justified only by scene-tree membership (drawing, collision, grouping) — never by code organization. Precedent: `StatBlock` (plain) + `ActorStats` (adapter); `StatusController`.
- Hot paths (per-frame, per-projectile, per-query): no linear `ActiveEnemies` scans — QuadTree/spatial hash; no per-frame allocations (structs, pools, buffers); cache per-tick values in locals, never repeat dictionary/signal reads in a loop.
- Scaling is validated through `benchmarks/`, never by assertion. A benchmark regression means the design is wrong, not the benchmark.

## Session start (capability probe — never skip)

Never assert a tool is missing without probing. Before claiming anything
about available tooling, run `execute` → `search({query: "godot"})` and
`session_manage(op="list")`. The `godot-ai` MCP bridge (editor state,
`project_run`, `game_eval`, `editor_screenshot source="game"`) may be live
even when the session prompt doesn't advertise it.

## Focus on current state over history (no git archaeology)

- Always focus on the **current** codebase, active workspace files, and present behavior.
- **No git archaeology:** Never get bogged down running repetitive `git log`, `git show`,
  or diffing against past commits to understand problems unless the user explicitly asks.
- Diagnose and solve problems directly from the current code (`view_file`, ripgrep),
  live runtime logs, test harness output, and current visual captures.
- In visual / VFX validation, establish clean, identical current baselines (e.g. pre-cast
  vs peak-cast in the same scene) rather than digging through broken past revisions.
- **No comment archaeology or roadmap tags:** Never write comments explaining historical
  refactors, migrations, or past engine iterations (`Vistrace`, `TODO Phase X`, "Replaces duck typing",
  "Extracted from X"). Zero roadmap tags in production code.
- **Comments — less but clear:** If the class, method, or property name already clearly
  represents what it does, do NOT add a comment. Eliminate redundant line comments
  (`// Flash modulate`, `// Ensure container`). Doc summaries (`<summary>`) are reserved
  strictly for non-obvious invariants, math formulas, or zero-GC contracts, capped at 1–2
  lines max.

## AI collaboration (grill protocols)

Vibe-coding unfamiliar code is a comprehension trap: fast output, zero
retained mental model. Route by domain familiarity:

- Familiar domain (backend/systems) or ambiguous requirements →
  `grill-me`: AI interrogates the spec **before** building. No code until
  scope, edge cases, and verification are locked.
- Unfamiliar domain (Godot 4 / C# game code) → `reverse-grill-me`: AI may
  generate, but the diff is **uncommittable until you pass an oral defense**
  (3–5 questions, one at a time, graded against
  `.agents/skills/reverse-grill-me/references/godot-defense-rubric.md`).
  Two failed attempts → shrink the diff, rewrite by hand, re-grill.
- Trigger phrases: "grill me" (pre-build spec), "defend this" / "reverse
  grill" (post-generation defense).

## Build

```sh
dotnet build Phagocyte.csproj --warnaserror   # or: make build
python3 tools/check_arch.py                   # or: make check-arch (layer boundaries, docs/architecture/ARCH_RULE.md)
make check-config                             # or: cd tools/config && pnpm run check (data schemas + cross-dataset FKs)
```

Zero warnings tolerated. Godot binary:
`/Applications/Godot_mono.app/Contents/MacOS/Godot`.

## Tests (GdUnit4 C#, headless)

```sh
Godot --headless --path . -s res://tests/<Suite>.cs
```

- PASS = no `[FAIL]` / `TestFailedException` in output + `PASSED SUCCESSFULLY` footer.
- `TestHarness` is abstract scaffolding — never run it directly.
- `TestAchievementPreview` is the visual preview harness: under `--headless`
  it only verifies logic (capture is skipped); real pixels need a headed run
  (below). Exclude it from full-sweep loops.
- `benchmarks/` (HordeBenchmark, PerformancePipeline, TickBudget) are load
  instruments, not checks — never run in sweep loops (54 runnable suites).
- Known-red at HEAD (fix or explicitly re-baseline, never silently delete):
  TestPerformancePipeline species count
  (parked in benchmarks/, do not fix in passing).
  TestNewSystemsTriad Phase 3 (mock was never `IDamageable`, DoT never lands).
  Fixed this pass: TestCheats (helper moved to scripts/testing/CheatTools.cs),
  TestDifficultyTracks (suite now opts into MapEffectsEnabled),
  TestGearBalance opener bound (phagolysosome_core might 0.15 → 0.12).
- Frame-gate async work with `Gate(ref _frame, n)`; saves are auto-isolated
  (`IsolateSaves`, `user://test_*`) so suites never touch real profiles.

### Determinism rules (flakes we already fixed once — don't reintroduce)

- Damage asserts: zero the RNG first —
  `Stats.SetBase("block", 0)`, `Stats.SetBase("evasion", 0)`.
  Zero AFTER `AddChild`: class `trait_stat` (e.g. macrophage `block` 0.08)
  is reseeded in `_Ready` and overwrites earlier values (8% flake).
- HUD / exp / arena suites: freeze spawners + clear the arena + reset player
  baselines on frame 1, and reset HUD snapshots (`LastCurrentExp` etc. —
  setting `CurrentExp` directly does NOT fire `ExpChanged`).
- Dash/charge tests: always add a positive control (prove the action works
  unblocked) before asserting the blocked case.

## Preview / screenshots (headed!)

```sh
PHAGOCYTE_CAPTURE_DIR=/tmp/xxx Godot --path . -s res://tests/TestAchievementPreview.cs
```

- NEVER add `--headless` here — headless viewports render blank.
- `TestHarness.CaptureScreenshot(name)` handles the plumbing; keep PNGs in
  `/tmp` (or overlay dirs like `/tmp/theme_after`), never commit them.
- Visual refactors require before/after PNG comparison (eyeball): capture
  the baseline FIRST, then change code. Current captures: title, gallery/all,
  gallery/locked.

### Mandatory UI screenshot verification (agent does it, never the user)

- ANY change affecting pixels — scenes, theme/styleboxes, UI code, icons,
  translated strings that alter layout — must be verified by the agent with a
  `godot-ai` game screenshot (`editor_screenshot` with `source="game"`)
  before declaring done. NEVER ask the user to eyeball it for you.
- C# does NOT hot-reload: after `dotnet build`, restart the run
  (`project_manage(op="stop")` → `project_run(mode="main")`), drive to the
  affected view with `game_eval` (e.g. call the menu flow methods directly),
  then capture. A stale session will show pre-change pixels and waste a round.
- Invisible-stylebox checklist (learned the hard way): flat `Button`s skip
  the normal stylebox (`flat = true`); check `modulate` alpha, shared vs
  per-instance stylebox overrides, and asymmetric corner radii.

## Assets

- `gen/<achievement|skill|passive_tree|ui>/` is source of truth (prefix-free
  names); `assets/gen/` is pipeline artifact. Check with
  `python3 tools/asset_check/main.py --summary` (`make check-assets`).
- Audio has its own manifest SSOT: `assets/audio/manifest.json` lists every
  BGM/SFX the game may play. New sounds go there FIRST, then the file, then
  the `AudioManager` call. Enforced three ways: `check-assets` step 7 (disk),
  `AudioManager.ValidateAudioManifest` at boot (fail fast), `TestAudioAssets`
  (red suite). `PlaySfx` warns on miss — silence is always a bug, never a default.
- Runtime loads ONLY via `AssetLoader` (`scripts/core/assets/`):
  `Load<T>` throws `AssetLoadException` on miss, `TryLoad<T>` returns null.
  Raw `GD.Load` / `ResourceLoader.*` live solely in `GodotAssetProvider.cs`
  (zero-residual rule — grep before adding any).
- Paths via `AssetPaths.*`; missing art falls back to `PlaceholderIcon`,
  so scenes must render correctly with placeholders.
- `.uid` sidecars are tracked in git — keep them.

## Scenes (Godot idioms enforced here)

### Scene-vs-code rule (don't re-litigate this)

- Fixed node set → `.tscn` (static shell, editor-adjustable).
- Count varies with data → container in `.tscn` + item scene + code loop.
- Geometry computed at runtime (tree layout, data-driven positions) → code,
  tuned via exported constants. Preview with the harness, not the inspector.
- Sole exception: transient effect nodes (spawn → animate → `QueueFree`,
  e.g. damage numbers) stay in code — nothing to adjust in the editor.
- Prototype debt is real: code-built static UI must be converted to scenes
  when the design stabilizes, not "later".

- One reusable UI per `.tscn`; big scenes compose via `instance=ExtResource`
  (`main_menu.tscn` is a pure assembly file — keep it that way).
- NEVER rename nodes or restructure internal paths: `MainMenu.cs` and tests
  resolve `GetNodeOrNull("AchievementView")`-style relative paths. Instance
  roots keep their original names; preserve `visible` flags on extraction.
- Extracted scenes must be self-contained: every `ExtResource`/`SubResource`
  they reference must be declared in the same file (duplicate small styleboxes
  if needed — do NOT leave cross-file references; the parser will fail).
- Shared button look lives in `assets/theme/menu_buttons.tres`. Per-button
  `font_size` overrides stay on the buttons (sizes vary 13–18 by design).
- Keep scene edits pure: move first, clean up (dead nodes, style dedup) in a
  separate change. Verify with the dangling-reference check
  (every used `ExtResource("id")` has a matching `id="..."` decl).

## Code size and file organization (net-negative by default)

- Prefer deleting to adding: remove dead branches, unused overloads/params,
  and one-use abstractions instead of switching them off (`if (false)`,
  unused `skipX` params, unreachable `else` arms, placeholder overrides).
- No compatibility shims for removed mechanics: update the call sites,
  don't keep adapters (`DamageOrEngulf`-style fallbacks).
- No speculative hooks: if nothing calls it, it doesn't ship.
- **Co-locate small POCO types; avoid file fragmentation:** Do not create tiny standalone
  files (< 20 lines) for plain C# interfaces, enums, or POCO structs that belong to a
  single consumer subsystem (e.g. `DamageResult` in `DamageService.cs`, `ISlowable` in
  `CombatInterfaces.cs`, `Team` in `ProjectileData.cs`, `VfxType` in `VfxManager.cs`). Keep Godot
  `Node`/`Resource` classes 1:1 with filenames (required by Godot's C# source generators),
  but consolidate tightly-coupled plain C# types in their primary service or owner file.
- Verify with a zero-residual grep: removed symbol names must return zero
  hits outside history/flavor text before declaring done.

## Commits

- Inspect `git status` when preparing commits; stage only intended files;
  never commit secrets. Do not waste turns on git history loops.
  Suites must be green before pushing.
- Hot files (`Hud.cs`, `MainMenu.cs`, shared scenes): coordinate parallel edits
  — concurrent changes here have broken builds before. When in doubt, run the
  full sweep (all 54 suites, ~7 min) before declaring victory.
