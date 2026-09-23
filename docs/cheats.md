# Test Cheats — full-unlock / full-build testing guide (測試作弊指南)

One-call setup for testing with everything unlocked and maxed out.
Core: `tests/TestCheats.cs`. Self-check: `tests/TestCheatUnlocks.cs`.

## What "full" means

`TestCheats.UnlockAllMeta()` unlocks:

- All 19 achievements → all 5 cells, all 5 organ maps + Hard modes, Endless mode, talent points
- All 12 organelles (`OrganelleUnlockManager.UnlockAll()`)
- Tree level 15 (meta cap) for every cell + 50 spendable bonus points

`TestCheats.MaxOutPlayer(main)` builds the run: level 30, innate kept + 4 actives / 5 passives
filled and maxed, full vault + best-effort chamber, block/evasion RNG zeroed.

By design the passive tree is **never pre-allocated** — suites spend the granted points explicitly.
Godmode defaults to **off** so damage-sensitive tests keep their signal; opt in per call.

## Manual testing (headed run)

All headed entries are debug-only: no-ops in release builds.

| Method | Menu | Run |
|---|---|---|
| Editor Run button (F5) + hotkey | **F9** unlock all, **F10** reset to fresh profile | **F9** max player, **Shift+F9** + godmode |
| Terminal flags | `Godot --path . -- --cheats=all` | add `--godmode` for invulnerable |
| Env var | `PHAGOCYTE_CHEATS=all godot --path .` (`reset`, `godmode`, comma-separated) | same |

Why F5 alone does nothing: the editor spawns the game with zero CLI args, so flag-based
cheats see nothing — the F9/F10 hotkeys exist precisely for click-to-run testing.

⚠️ Headed cheats write your **real** `user://*.json` profile. Restore with F10 in the menu
or `Godot --path . -- --cheats-reset`. Headless suites are unaffected (isolated `user://test_*`).

## Writing a headless suite with cheats

```csharp
public override bool _Process(double delta)
{
    // ... frame gate ...
    TestCheats.LockToBaseline();   // exact state first (UnlockAllMeta is additive)
    TestCheats.UnlockAllMeta();    // everything unlocked
    var main = InstantiateMain();  // TestHarness: isolated saves, physics off
    // ... settle frames ...
    TestCheats.MaxOutPlayer(main);              // full build, mortal
    TestCheats.MaxOutPlayer(main, godmode: true); // only when the suite must survive
    // ... asserts ...
    FreeMain(main);
    ResetRunGlobals();
    RestoreSaves();
}
```

Rules (per `AGENTS.md`):

- `TestHarness` already calls `IsolateSaves(tag)` — never touch real profiles from a suite.
- `MaxOutPlayer` sets `CurrentLevel` directly and fires **no** signals: reset HUD snapshots
  (`LastCurrentExp`, `LastLevel`, …) manually, same as setting `CurrentExp` by hand.
- Prove the unblocked case before the blocked one; don't hide the system under test behind godmode.

## Verification

```sh
dotnet build Phagocyte.csproj --warnaserror
Godot --headless --path . -s res://tests/TestCheatUnlocks.cs   # self-check (baseline → full → baseline)
```

Green = no `[FAIL]` / `TestFailedException` + `PASSED SUCCESSFULLY` footer.
