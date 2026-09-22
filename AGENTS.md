# AGENTS.md — Phagocyte contributor notes

Read this before touching code, scenes, or assets. Conventions below were
earned the hard way (red suites, broken builds, ghost diffs).

## Build

```sh
dotnet build Phagocyte.csproj --warnaserror   # or: make build
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
  (below). Exclude both from full-sweep loops (42 runnable suites).
- Frame-gate async work with `Gate(ref _frame, n)`; saves are auto-isolated
  (`IsolateSaves`, `user://test_*`) so suites never touch real profiles.

### Determinism rules (flakes we already fixed once — don't reintroduce)

- Damage asserts: zero the RNG first —
  `Stats.SetBase("block", 0)`, `Stats.SetBase("evasion", 0)`.
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

## Assets

- `gen/<achievement|skill|passive_tree|ui>/` is source of truth (prefix-free
  names); `assets/gen/` is pipeline artifact. Check with
  `python3 tools/asset_check/main.py --summary` (`make check-assets`).
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

## Commits

- Inspect `git status`, `git diff`, `git log --oneline -5` first; stage only
  intended files; never commit secrets. Suites green before pushing.
- Hot files (`Hud.cs`, `MainMenu.cs`, shared scenes): coordinate parallel edits
  — concurrent changes here have broken builds before. When in doubt, run the
  full sweep (all 42 suites, ~7 min) before declaring victory.
