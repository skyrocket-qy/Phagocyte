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


def check_missing_assets(
    root_dir: Path = Path("."),
    target_category: Optional[str] = None
) -> Tuple[Dict[str, List[str]], List[str], Dict[str, int]]:
    """
    Bidirectional Asset Integrity Verification across active categories:
    - Achievement (19 items from achievements.json)
    - Skill (31 items from skills.json)
    - PassiveTree (54 items from passive_traits.json)
    - UI (uncompiled items in gen/ui/)

    Returns:
        missing_by_cat: Dict mapping category name to list of missing asset paths/names
        unmapped: List of stray files on disk not registered in game data
        category_counts: Dict mapping category name to total registered item count
    """
    missing_by_cat: Dict[str, List[str]] = {
        "Achievement": [],
        "Skill": [],
        "PassiveTree": [],
    }
    unmapped: List[str] = []
    category_counts: Dict[str, int] = {}

    assets_dir = root_dir / "assets"
    gen_dir = root_dir / "gen"
    assets_gen_dir = assets_dir / "gen"

    registered_ids: Set[str] = set()

    # ── 1. Achievements (19 items) ──────────────────────────────────────────
    achievements_file = assets_dir / "data" / "achievements.json"
    ach_data = _load_json(achievements_file) or []
    for item in ach_data:
        ach_id = item.get("id")
        if not ach_id:
            continue

        registered_ids.add(ach_id)
        registered_ids.add(f"{ach_id}_unachieved")
        category_counts["Achievement"] = category_counts.get("Achievement", 0) + 1

        # Strictly check assets/gen/achievement/
        gen_path = assets_gen_dir / "achievement" / f"{ach_id}.png"
        if not gen_path.exists():
            missing_by_cat["Achievement"].append(f"{ach_id}.png")
        else:
            # Check for Steam unachieved variant
            gen_unach = assets_gen_dir / "achievement" / f"{ach_id}_unachieved.png"
            if not gen_unach.exists():
                missing_by_cat["Achievement"].append(f"{ach_id}_unachieved.png (Steam locked variant)")

    # ── 2. Skills (31 items) ────────────────────────────────────────────────
    skills_file = assets_dir / "data" / "skills.json"
    skill_data = _load_json(skills_file) or []
    for item in skill_data:
        skill_id = item.get("id")
        if not skill_id:
            continue

        registered_ids.add(skill_id)
        category_counts["Skill"] = category_counts.get("Skill", 0) + 1

        # Strictly check assets/gen/skill/
        gen_path = assets_gen_dir / "skill" / f"{skill_id}.png"
        if not gen_path.exists():
            missing_by_cat["Skill"].append(f"{skill_id}.png")

    # ── 3. Passive Tree Traits (54 items) ───────────────────────────────────
    traits_file = assets_dir / "data" / "passive_traits.json"
    traits_json = _load_json(traits_file) or {}
    traits_list = traits_json.get("traits", []) if isinstance(traits_json, dict) else traits_json
    for item in traits_list:
        trait_id = item.get("id")
        if not trait_id:
            continue

        registered_ids.add(trait_id)
        category_counts["PassiveTree"] = category_counts.get("PassiveTree", 0) + 1

        # Strictly check assets/gen/passive_tree/
        gen_path = assets_gen_dir / "passive_tree" / f"{trait_id}.png"
        if not gen_path.exists():
            missing_by_cat["PassiveTree"].append(f"{trait_id}.png")

    # ── 4. UI: check if source in gen/ui not compiled to assets/gen/ui ──────
    gen_ui_dir = gen_dir / "ui"
    if gen_ui_dir.exists():
        for f in gen_ui_dir.glob("*.png"):
            ui_dest = assets_gen_dir / "ui" / f.name
            if not ui_dest.exists():
                missing_by_cat.setdefault("UI", []).append(f"{f.name} (not compiled from gen/ui/)")

    # ── 5. Reverse Reference Checks (Disk -> Data) ──────────────────────────
    for base_dir in [gen_dir, assets_gen_dir]:
        if not base_dir.exists():
            continue
        for root, _, files in os.walk(base_dir):
            rel_root = Path(root).relative_to(base_dir)
            if not rel_root.parts:
                continue
            cat = rel_root.parts[0]
            if cat not in ["achievement", "skill", "passive_tree"]:
                continue

            for f in files:
                if f.startswith(".") or f.endswith(".import"):
                    continue
                stem = Path(f).stem
                if stem not in registered_ids:
                    unmapped.append(f"[{cat}] Unmapped file on disk not registered in data: {rel_root / f}")

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
