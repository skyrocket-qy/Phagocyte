"""Checks for missing assets referenced by data configurations and unmapped assets on disk (Bidirectional Integrity)."""

import json
import os
from pathlib import Path
from typing import Dict, List, Set, Tuple, Any, Optional


def _load_json(file_path: Path) -> Any:
    if not file_path.exists():
        return None
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        print(f"Error loading JSON {file_path}: {e}")
        return None


# Source filenames under gen/ carry no category prefix; registered data IDs do.
# Maps gen/ subdirectory -> ID prefixes stripped (in order, repeatedly) when
# resolving source filenames. E.g. trait_passive_actin -> actin.
CATEGORY_PREFIXES: Dict[str, List[str]] = {
    "achievement": ["ach_"],
    "skill": ["passive_"],
    "passive_tree": ["trait_", "passive_", "tree_"],
    "ui": ["ui_"],
}


def _source_stem(category: str, name: str) -> str:
    """Map a registered data ID to its prefix-free source file stem in gen/."""
    prefixes = CATEGORY_PREFIXES.get(category, [])
    stripped = name
    changed = True
    while changed:
        changed = False
        for prefix in prefixes:
            if prefix and stripped.startswith(prefix):
                stripped = stripped[len(prefix):]
                changed = True
                break
    return stripped


def check_missing_assets(
    root_dir: Path = Path("."),
    target_category: Optional[str] = None
) -> Tuple[Dict[str, List[str]], List[str], Dict[str, int]]:
    """
    Bidirectional Asset Integrity Verification across active categories.
    Source of truth is gen/ (raw generated assets before pipeline processing).
    Source filenames carry no category prefix (e.g. gen/achievement/<id without ach_>.png):
    - Achievement (19 items from achievements.json -> gen/achievement/)
    - Skill (31 items from skills.json -> gen/skill/)
    - PassiveTree (54 items from passive_traits.json -> gen/passive_tree/)
    - UI (16 items expected in gen/ui/)

    Returns:
        missing_by_cat: Dict mapping category name to list of missing asset paths/names
        unmapped: List of stray files on disk not registered in game data
        category_counts: Dict mapping category name to total registered item count
    """
    missing_by_cat: Dict[str, List[str]] = {
        "Achievement": [],
        "Skill": [],
        "PassiveTree": [],
        "UI": [],
    }
    unmapped: List[str] = []
    category_counts: Dict[str, int] = {}

    assets_dir = root_dir / "assets"
    gen_dir = root_dir / "gen"

    # Per-category expected source stems (prefix-free) for reverse checks.
    expected_stems: Dict[str, Set[str]] = {}

    # ── 1. Achievements (19 items) ──────────────────────────────────────────
    # Source files live prefix-free in gen/achievement/ (e.g. ach_first_digestion
    # -> first_digestion.png). The *_unachieved.png Steam locked variants are
    # derived artifacts produced by the to_target_asset pipeline into
    # assets/gen/achievement/, so only the base source is required here.
    achievements_file = assets_dir / "data" / "achievements.json"
    ach_data = _load_json(achievements_file) or []
    for item in ach_data:
        ach_id = item.get("id")
        if not ach_id:
            continue

        stem = _source_stem("achievement", ach_id)
        expected_stems.setdefault("achievement", set()).update([stem, f"{stem}_unachieved"])
        category_counts["Achievement"] = category_counts.get("Achievement", 0) + 1

        # Check prefix-free source in gen/achievement/
        gen_path = gen_dir / "achievement" / f"{stem}.png"
        if not gen_path.exists():
            missing_by_cat["Achievement"].append(f"gen/achievement/{stem}.png")

    # ── 2. Skills (31 items) ────────────────────────────────────────────────
    skills_file = assets_dir / "data" / "skills.json"
    skill_data = _load_json(skills_file) or []
    for item in skill_data:
        skill_id = item.get("id")
        if not skill_id:
            continue

        stem = _source_stem("skill", skill_id)
        expected_stems.setdefault("skill", set()).add(stem)
        category_counts["Skill"] = category_counts.get("Skill", 0) + 1

        # Check source in gen/skill/
        gen_path = gen_dir / "skill" / f"{stem}.png"
        if not gen_path.exists():
            missing_by_cat["Skill"].append(f"gen/skill/{stem}.png")

    # ── 3. Passive Tree Traits (54 items) ───────────────────────────────────
    traits_file = assets_dir / "data" / "passive_traits.json"
    traits_json = _load_json(traits_file) or {}
    traits_list = traits_json.get("traits", []) if isinstance(traits_json, dict) else traits_json
    for item in traits_list:
        trait_id = item.get("id")
        if not trait_id:
            continue

        stem = _source_stem("passive_tree", trait_id)
        expected_stems.setdefault("passive_tree", set()).add(stem)
        category_counts["PassiveTree"] = category_counts.get("PassiveTree", 0) + 1

        # Check prefix-free source in gen/passive_tree/
        gen_path = gen_dir / "passive_tree" / f"{stem}.png"
        if not gen_path.exists():
            missing_by_cat["PassiveTree"].append(f"gen/passive_tree/{stem}.png")

    # ── 4. UI: check expected prefix-free icons exist in gen/ui/ ───────────
    expected_ui_assets = [
        "reticle_target.png",
        "reticle_danger.png",
        "reticle_scan.png",
        "frame_organelle.png",
        "card_mutation.png",
        "card_superweapon.png",
        "badge_atp.png",
        "badge_antigen.png",
        "meter_ph.png",
        "meter_temp.png",
        "button_pause.png",
        "button_settings.png",
        "status_inflamed.png",
        "status_buffered.png",
        "status_overclock.png",
        "status_exhausted.png",
    ]
    category_counts["UI"] = len(expected_ui_assets)
    for ui_name in expected_ui_assets:
        expected_stems.setdefault("ui", set()).add(Path(ui_name).stem)
        gen_path = gen_dir / "ui" / ui_name
        if not gen_path.exists():
            missing_by_cat["UI"].append(f"gen/ui/{ui_name}")

    # ── 5. Reverse Reference Checks (Disk -> Data, gen/ only) ───────────────
    # Only gen/ is scanned: it is the source of truth for
    # achievement/skill/passive_tree/ui. Processed outputs under
    # assets/gen/ are pipeline artifacts, not registered sources.
    if gen_dir.exists():
        for root, _, files in os.walk(gen_dir):
            rel_root = Path(root).relative_to(gen_dir)
            if not rel_root.parts:
                continue
            cat = rel_root.parts[0]
            if cat not in ["achievement", "skill", "passive_tree", "ui"]:
                continue

            for f in files:
                if f.startswith(".") or f.endswith(".import"):
                    continue
                # Skip sheet sources / intermediate files, only check sliced outputs
                if "sheet" in f.lower():
                    continue
                stem = Path(f).stem
                if stem not in expected_stems.get(cat, set()):
                    unmapped.append(f"[{cat}] Unmapped file on disk not registered in data: gen/{rel_root / f}")

    # Filter by target_category if specified
    if target_category:
        cat_key = {
            "achievement": "Achievement",
            "skill": "Skill",
            "passive_tree": "PassiveTree",
            "ui": "UI",
        }.get(target_category.lower(), target_category)

        missing_by_cat = {k: v for k, v in missing_by_cat.items() if k.lower() == cat_key.lower()}

    return missing_by_cat, unmapped, category_counts
