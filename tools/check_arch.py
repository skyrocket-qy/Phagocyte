#!/usr/bin/env python3
"""
Architecture Layer Boundary Validator for Game (Godot C#).
Enforces the layer rules in docs/architecture/ARCH_RULE.md against the
current scripts/ layout (no file moves required):

  Layer 1: Domain   - pure data/logic (explicit file list under scripts/core/)
  Layer 2: Autoload - global singletons + static services (scripts/core/ managers)
  Layer 3: Gameplay - entities/simulation (combat, player, enemies, skills,
                       directors, stages, equipment, debug + core
                       gameplay helpers like ActorStats/EquipmentChamber/UpgradeManager)
  Layer 4: UI       - presentation (scripts/ui/, except autoload DamageNumberSpawner)
  Layer T: Tests    - verification (tests/, can import all — never checked)

Ported from Vistrace-godot scripts/check_arch.py (same strip + dynamic-UI-types
approach), adapted to Game's feature-folder layout.
Run: python3 tools/check_arch.py  (or: make check-arch)
"""

import re
import sys
from pathlib import Path

ROOT_DIR = Path(__file__).resolve().parent.parent
SCRIPTS_DIR = ROOT_DIR / "scripts"

# --- Layer file classification (relative to repo root, forward slashes) ---

# L1: pure domain. No Node/SceneTree/autoload/gameplay/UI deps.
# data/CatalogBuilders + data/DataValidator live in data/ but are classified
# L2 (validation services with query-only autoload reads) — see ARCH_RULE.md.
# core/GameEvents.cs is L2 for the same reason: its subscriptions wire two
# autoloads together (AchievementManager -> GameManager via events).
DOMAIN_FILES = {
    "scripts/core/IStatHost.cs",
    "scripts/core/StatBlock.cs",
    "scripts/core/BodyDeformation.cs",
    "scripts/core/QuadTree.cs",
    "scripts/core/SkillIds.cs",
    "scripts/core/Stat.cs",
    "scripts/core/TextFormatter.cs",
    "scripts/core/assets/AssetLoader.cs",
    "scripts/core/assets/AssetLoadException.cs",
    "scripts/core/assets/AssetPaths.cs",
    "scripts/core/assets/GodotAssetProvider.cs",
    "scripts/core/assets/IAssetProvider.cs",
    "scripts/core/data/CatalogLoader.cs",
    "scripts/core/data/DataLoadException.cs",
    "scripts/core/data/DataPaths.cs",
}

# L2: autoload singletons + static global services + validation services.
AUTOLOAD_FILES = {
    "scripts/core/GameEvents.cs",
    "scripts/core/AchievementManager.cs",
    "scripts/core/AudioManager.cs",
    "scripts/core/GameManager.cs",
    "scripts/core/RunRecordManager.cs",
    "scripts/core/SettingsManager.cs",
    "scripts/core/EquipmentUnlockManager.cs",
    "scripts/core/LoadoutManager.cs",
    "scripts/core/PassiveTreeManager.cs",
    "scripts/core/PauseManager.cs",
    "scripts/core/JsonStore.cs",
    "scripts/core/KeyBindings.cs",
    "scripts/core/SteamBridge.cs",
    "scripts/core/data/CatalogBuilders.cs",
    "scripts/core/data/DataValidator.cs",
    "scripts/ui/DamageNumberSpawner.cs",  # registered autoload; file move TBD
}

# L3: everything gameplay (directory prefixes + gameplay-tagged core files).
GAMEPLAY_PREFIXES = (
    "scripts/camera/",
    "scripts/combat/",
    "scripts/player/",
    "scripts/enemies/",
    "scripts/skills/",
    "scripts/directors/",
    "scripts/stages/",
    "scripts/equipment/",
    "scripts/debug/",
)
GAMEPLAY_FILES = {
    "scripts/core/ActorStats.cs",
    "scripts/core/EquipmentChamber.cs",
    "scripts/core/EquipmentDrop.cs",
    "scripts/core/RunTelemetryManager.cs",
    "scripts/core/UpgradeManager.cs",
}

# Composition root: owns scene wiring across all layers (assembler).
# Allowed to reference concrete UI types to connect player/HUD/views.
COMPOSITION_ROOT_FILES = {
    "scripts/GameRoot.cs",
}

# L4: UI (whole scripts/ui/ except the autoload above). Checked lightly:
# UI may import anything, but sibling-modal direct manipulation is flagged.
UI_PREFIX = "scripts/ui/"

# Per-line allowlist: (rel_path, line_pattern) pairs that are known,
# documented exceptions. Keep this list at zero growth.
ALLOWLIST = [
    # QuadTree Node2D fallback: convenience when callers omit positionOf.
    # Pure path (explicit positionOf) is preferred; removal is a behavior
    # change, so the single fallback line is grandfathered (ARCH_RULE.pm).
    ("scripts/core/QuadTree.cs", r"candidate\s+is\s+Node2D"),
]

# Gameplay may use this ONE UI-namespace symbol: the DamageNumberSpawner
# autoload (gameplay -> autoload is allowed; the file lives in scripts/ui/
# for now). Every other Game.UI reference in gameplay is a violation.
GAMEPLAY_UI_ALLOW = re.compile(r"\bDamageNumberSpawner\b|\bDamageNumberType\b")

# Forbidden patterns per layer: list of (regex, description).
STATIC_DOMAIN_FORBIDDEN = [
    (r"\bCharacterBody2D\b", "Godot physics node 'CharacterBody2D' in domain"),
    (r"\bCharacterBody3D\b", "Godot physics node 'CharacterBody3D' in domain"),
    (r"\bArea2D\b", "Godot node 'Area2D' in domain"),
    (r"\bCanvasLayer\b", "Godot node 'CanvasLayer' in domain"),
    (r"\bControl\b", "Godot UI node 'Control' in domain"),
    (r"\bButton\b", "Godot UI node 'Button' in domain"),
    (r"\bTextureRect\b", "Godot UI node 'TextureRect' in domain"),
    (r":\s*Node\b", "Godot 'Node' base class in domain"),
    (r"\.GetTree\s*\(", "SceneTree API '.GetTree()' in domain"),
    (r"\.GetNodesInGroup\s*\(", "SceneTree API '.GetNodesInGroup()' in domain"),
    (r"\bGetNode(OrNull)?\s*[<(]", "SceneTree lookup 'GetNode' in domain"),
    (r"\bGetFirstNodeInGroup\s*\(", "SceneTree lookup in domain"),
    (r"\bCurrentScene\b", "SceneTree 'CurrentScene' in domain"),
    # Concrete gameplay actors / components.
    (r"(?<!\.)\bPlayerActor\b", "Gameplay actor 'PlayerActor' in domain"),
    (r"(?<!\.)\bEnemyActor\b", "Gameplay actor 'EnemyActor' in domain"),
    (r"(?<!\.)\bBaseSkill\b", "Gameplay type 'BaseSkill' in domain"),
    (r"(?<!\.)\bSkillManager\b", "Gameplay type 'SkillManager' in domain"),
    (r"\bCombatComponent\b", "Gameplay component in domain"),
    (r"\bStatusController\b", "Gameplay component in domain"),
    (r"\bEquipmentChamber\b", "Gameplay component 'EquipmentChamber' in domain"),
    (r"\bActorStats\b", "Gameplay component 'ActorStats' in domain"),
    # Autoload singletons (domain must not couple to globals).
    (r"\bGameManager\b", "Autoload 'GameManager' in domain"),
    (r"\bAudioManager\b", "Autoload 'AudioManager' in domain"),
    (r"\bSettingsManager\b", "Autoload 'SettingsManager' in domain"),
    (r"\bAchievementManager\b", "Autoload 'AchievementManager' in domain"),
    (r"\bRunRecordManager\b", "Autoload 'RunRecordManager' in domain"),
    (r"\bPassiveTreeManager\b", "Global 'PassiveTreeManager' in domain"),
]

STATIC_AUTOLOAD_FORBIDDEN = [
    (r"\bCharacterBody2D\b", "Physics actor 'CharacterBody2D' in autoload"),
    (r"\bPlayerActor\b", "Concrete actor 'PlayerActor' in autoload"),
    (r"\bEnemyActor\b", "Concrete actor 'EnemyActor' in autoload"),
    (r"\bSkillManager\b", "Gameplay 'SkillManager' in autoload"),
]

# Phase 9 lockdown: deleted hit-dispatch interfaces. Any reference anywhere
# in scripts/ (all layers) is a leftover from the DealDamage era.
STATIC_PIPELINE_FORBIDDEN = [
    (r"\bILeechable\b", "Deleted interface 'ILeechable' referenced (route leech via HitPipeline attacker id)"),
    (r"\bISlowable\b", "Deleted interface 'ISlowable' referenced (route slow via IStatusHost.Status)"),
    (r"\bIStunnable\b", "Deleted interface 'IStunnable' referenced (route stun via IStatusHost.Status)"),
]

# ResolveHit may only be invoked from gameplay simulation code. Domain must
# stay SceneTree-free, autoloads must not deal damage, UI must not resolve hits.
RESOLVE_HIT_NON_GAMEPLAY_LAYERS = ("domain", "autoload", "ui")

STATIC_GAMEPLAY_FORBIDDEN = [
    (r"use\s+Game\.UI\s*;", "Namespace 'Game.UI' imported in gameplay"),
    (r"\bRunRecordsModal\b", "Concrete UI 'RunRecordsModal' in gameplay"),
    (r"\bUpgradeModal\b", "Concrete UI 'UpgradeModal' in gameplay"),
    (r"\bPassiveTreeView\b", "Concrete UI 'PassiveTreeView' in gameplay"),
    (r"\bSkillBarView\b", "Concrete UI 'SkillBarView' in gameplay"),
    (r"\bVitalsView\b", "Concrete UI 'VitalsView' in gameplay"),
    (r"\bToastView\b", "Concrete UI 'ToastView' in gameplay"),
    (r"\bSettingsModal\b", "Concrete UI 'SettingsModal' in gameplay"),
    (r"\bLoadoutView\b", "Concrete UI 'LoadoutView' in gameplay"),
    (r"\bCodexModal\b", "Concrete UI 'CodexModal' in gameplay"),
    (r"\bEndgameSetupModal\b", "Concrete UI 'EndgameSetupModal' in gameplay"),
    (r"UIOverlay/", "UI scene-path lookup 'UIOverlay/' in gameplay"),
]


def strip_code_decorations(text: str) -> list[tuple[int, str]]:
    """Return lines with comments and string literals stripped (1-indexed)."""
    lines = text.split("\n")
    processed = []
    in_block_comment = False
    for idx, line in enumerate(lines, start=1):
        clean = ""
        i = 0
        in_string = False
        str_char = ""
        verbatim = False
        while i < len(line):
            if in_block_comment:
                if line[i : i + 2] == "*/":
                    in_block_comment = False
                    i += 2
                else:
                    i += 1
            elif in_string:
                if line[i] == "\\" and not verbatim:
                    i += 2
                elif line[i] == str_char:
                    in_string = False
                    i += 1
                else:
                    i += 1
            else:
                if line[i : i + 2] == "/*":
                    in_block_comment = True
                    i += 2
                elif line[i : i + 2] == "//":
                    break
                elif line[i] in ('"', "'"):
                    in_string = True
                    str_char = line[i]
                    verbatim = i > 0 and line[i - 1] in ("@", "$")
                    i += 1
                else:
                    clean += line[i]
                    i += 1
        processed.append((idx, clean))
    return processed


def collect_declared_types(directory: Path) -> set[str]:
    """Scan C# files and return declared type names (for dynamic UI rules)."""
    names: set[str] = set()
    decl = re.compile(r"\b(?:class|struct|enum|interface|record)\s+([A-Za-z0-9_]+)")
    for fp in directory.glob("**/*.cs"):
        if fp.name == "DamageNumberSpawner.cs":
            continue  # autoload exception, not a UI-layer type
        try:
            for _, line in strip_code_decorations(fp.read_text(encoding="utf-8")):
                for m in decl.finditer(line):
                    names.add(m.group(1))
        except Exception:
            continue
    return names


def layer_of(rel: str) -> str | None:
    """Return 'domain' | 'autoload' | 'gameplay' | 'ui' | None (unchecked)."""
    if rel in DOMAIN_FILES:
        return "domain"
    if rel in AUTOLOAD_FILES:
        return "autoload"
    if rel in GAMEPLAY_FILES:
        return "gameplay"
    if rel in COMPOSITION_ROOT_FILES:
        return None  # assembler: unchecked by design (see ARCH_RULE.md)
    if rel.startswith(GAMEPLAY_PREFIXES):
        return "gameplay"
    if rel.startswith(UI_PREFIX):
        return "ui"
    return None  # tests/, Main-adjacent misc: unchecked


def is_allowlisted(rel: str, line: str) -> bool:
    for path, pat in ALLOWLIST:
        if rel == path and re.search(pat, line):
            return True
    return False


def check_file(
    path: Path,
    ui_types: set[str],
) -> list[str]:
    rel = path.relative_to(ROOT_DIR).as_posix()
    layer = layer_of(rel)
    if layer is None:
        return []
    try:
        content = path.read_text(encoding="utf-8")
    except Exception as e:
        return [f"{rel}: failed to read: {e}"]
    violations = []
    for num, line in strip_code_decorations(content):
        if not line.strip():
            continue
        if is_allowlisted(rel, line):
            continue
        for pat, desc in STATIC_PIPELINE_FORBIDDEN:
            if re.search(pat, line):
                violations.append(
                    f"  {rel}:{num} -> [VIOLATION] {desc}\n    Line: {line.strip()}"
                )
        if layer == "domain":
            rules = list(STATIC_DOMAIN_FORBIDDEN)
            for t in sorted(ui_types):
                rules.append(
                    (rf"\b{re.escape(t)}\b", f"UI type '{t}' in domain")
                )
            for pat, desc in rules:
                if re.search(pat, line):
                    violations.append(
                        f"  {rel}:{num} -> [VIOLATION] {desc}\n    Line: {line.strip()}"
                    )
        elif layer == "autoload":
            for pat, desc in STATIC_AUTOLOAD_FORBIDDEN:
                if re.search(pat, line):
                    violations.append(
                        f"  {rel}:{num} -> [VIOLATION] {desc}\n    Line: {line.strip()}"
                    )
            for t in sorted(ui_types):
                if re.search(rf"\b{re.escape(t)}\b", line):
                    violations.append(
                        f"  {rel}:{num} -> [VIOLATION] UI type '{t}' in autoload\n"
                        f"    Line: {line.strip()}"
                    )
        elif layer == "gameplay":
            for pat, desc in STATIC_GAMEPLAY_FORBIDDEN:
                if not re.search(pat, line):
                    continue
                # 'using Game.UI;' is allowed ONLY when the file's sole
                # UI-namespace use is the DamageNumberSpawner autoload. We
                # check the whole file for other UI symbols at the end; here
                # record the import and filter below.
                violations.append(
                    f"  {rel}:{num} -> [VIOLATION] {desc}\n    Line: {line.strip()}"
                )
            for t in sorted(ui_types):
                if t in ("DamageNumberSpawner", "DamageNumberType"):
                    continue
                if re.search(rf"\b{re.escape(t)}\b", line):
                    violations.append(
                        f"  {rel}:{num} -> [VIOLATION] UI type '{t}' in gameplay\n"
                        f"    Line: {line.strip()}"
                    )
        # 'ui' layer: imports allowed by design (UI may read gameplay intent
        # state). No static rules.
        if layer in RESOLVE_HIT_NON_GAMEPLAY_LAYERS and re.search(r"\bResolveHit\s*\(", line):
            violations.append(
                f"  {rel}:{num} -> [VIOLATION] 'ResolveHit' called from {layer} layer (combat simulation only)\n"
                f"    Line: {line.strip()}"
            )
    if layer == "gameplay":
        # Post-filter: drop the 'using Game.UI' hit when the file only
        # touches the DamageNumberSpawner autoload (allowed exception).
        uses_ui_ns = any(
            "Game.UI" in v for v in violations
        )
        if uses_ui_ns:
            other_ui = [v for v in violations if "Game.UI" not in v]
            non_spawner_ui = [
                v
                for v in other_ui
                if "UI type" in v or "Concrete UI" in v or "UIOverlay" in v
            ]
            if not non_spawner_ui:
                violations = [v for v in violations if "Game.UI" not in v]
    return violations


def check_projectile_tick() -> list[str]:
    """Phase 9 lockdown: no dictionary lookups inside the projectile hot loop."""
    rel = "scripts/combat/ProjectileManager.cs"
    path = ROOT_DIR / rel
    try:
        content = path.read_text(encoding="utf-8")
    except Exception as e:
        return [f"{rel}: failed to read: {e}"]
    cleaned = "\n".join(line for _, line in strip_code_decorations(content))
    dict_fields = set(re.findall(r"Dictionary<[^;{}]*?>\s+(_\w+)", cleaned))
    start = cleaned.find("void _PhysicsProcess")
    if start == -1:
        return [f"{rel}: _PhysicsProcess not found"]
    depth = 0
    body_start = -1
    body_end = -1
    for i in range(cleaned.find("{", start), len(cleaned)):
        if cleaned[i] == "{":
            if body_start == -1:
                body_start = i
            depth += 1
        elif cleaned[i] == "}":
            depth -= 1
            if depth == 0:
                body_end = i
                break
    if body_start == -1 or body_end == -1:
        return [f"{rel}: _PhysicsProcess body not found"]
    body = cleaned[body_start:body_end]
    violations = []
    for field in sorted(dict_fields):
        for m in re.finditer(rf"{re.escape(field)}\b", body):
            ln = body.count("\n", 0, m.start()) + 1
            violations.append(
                f"  {rel}:_PhysicsProcess~{ln} -> [VIOLATION] dictionary '{field}' read on the per-tick hot path"
            )
    for pat in (r"\.TryGetValue\s*\(", r"\.ContainsKey\s*\("):
        for m in re.finditer(pat, body):
            ln = body.count("\n", 0, m.start()) + 1
            violations.append(
                f"  {rel}:_PhysicsProcess~{ln} -> [VIOLATION] dictionary lookup '{m.group(0)}' on the per-tick hot path"
            )
    return violations


def main() -> int:
    if not SCRIPTS_DIR.exists():
        print(f"Error: {SCRIPTS_DIR} does not exist.")
        return 1
    ui_types = collect_declared_types(ROOT_DIR / "scripts" / "ui")
    # Director-facing UI interfaces live in gameplay but are implemented by UI.
    ui_types -= {"IDirectorHud", "IRunSettlementModal"}
    # DamageNumberSpawner is an autoload, not a UI-layer type.
    ui_types -= {"DamageNumberSpawner", "DamageNumberType", "DamageNumberEntry"}
    cs_files = sorted(SCRIPTS_DIR.rglob("*.cs"))
    print(
        f"Checking architecture boundaries across {len(cs_files)} C# files "
        f"({len(ui_types)} dynamic UI types tracked)..."
    )
    all_violations: list[str] = []
    for fp in cs_files:
        all_violations.extend(check_file(fp, ui_types))
    all_violations.extend(check_projectile_tick())
    if all_violations:
        print("\n" + "=" * 70)
        print(f"ARCHITECTURE VIOLATIONS DETECTED ({len(all_violations)} total):")
        print("=" * 70)
        for v in all_violations:
            print(v)
        print("=" * 70)
        print("Fix per docs/architecture/ARCH_RULE.md")
        return 1
    print("\n" + "=" * 70)
    print(
        f"ARCHITECTURE CHECK PASSED: 0 violations "
        f"({len(ui_types)} UI types decoupled)."
    )
    print("=" * 70)
    return 0


if __name__ == "__main__":
    sys.exit(main())


