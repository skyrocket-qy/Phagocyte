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
    Bidirectional Asset Integrity Verification across active categories.
    Source of truth is gen/ (raw generated assets before pipeline processing):
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

    registered_ids: Set[str] = set()

    # ── 1. Achievements (19 items) ──────────────────────────────────────────
    # Source files live in gen/achievement/. The *_unachieved.png Steam locked
    # variants are derived artifacts produced by the to_target_asset pipeline
    # into assets/gen/achievement/, so only the base <id>.png is required here.
    achievements_file = assets_dir / "data" / "achievements.json"
    ach_data = _load_json(achievements_file) or []
    for item in ach_data:
        ach_id = item.get("id")
        if not ach_id:
            continue

        registered_ids.add(ach_id)
        registered_ids.add(f"{ach_id}_unachieved")
        category_counts["Achievement"] = category_counts.get("Achievement", 0) + 1

        # Check source in gen/achievement/
        gen_path = gen_dir / "achievement" / f"{ach_id}.png"
        if not gen_path.exists():
            missing_by_cat["Achievement"].append(f"{ach_id}.png")

    # ── 2. Skills (31 items) ────────────────────────────────────────────────
    skills_file = assets_dir / "data" / "skills.json"
    skill_data = _load_json(skills_file) or []
    for item in skill_data:
        skill_id = item.get("id")
        if not skill_id:
            continue

        registered_ids.add(skill_id)
        category_counts["Skill"] = category_counts.get("Skill", 0) + 1

        # Check source in gen/skill/
        gen_path = gen_dir / "skill" / f"{skill_id}.png"
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

        # Check source in gen/passive_tree/
        gen_path = gen_dir / "passive_tree" / f"{trait_id}.png"
        if not gen_path.exists():
            missing_by_cat["PassiveTree"].append(f"{trait_id}.png")

    # ── 4. UI: check expected icons exist as source in gen/ui/ ──────────────
    expected_ui_assets = [
        "ui_reticle_target.png",
        "ui_reticle_danger.png",
        "ui_reticle_scan.png",
        "ui_frame_organelle.png",
        "ui_card_mutation.png",
        "ui_card_superweapon.png",
        "ui_badge_atp.png",
        "ui_badge_antigen.png",
        "ui_meter_ph.png",
        "ui_meter_temp.png",
        "ui_button_pause.png",
        "ui_button_settings.png",
        "ui_status_inflamed.png",
        "ui_status_buffered.png",
        "ui_status_overclock.png",
        "ui_status_exhausted.png",
    ]
    category_counts["UI"] = len(expected_ui_assets)
    for ui_name in expected_ui_assets:
        registered_ids.add(Path(ui_name).stem)
        gen_path = gen_dir / "ui" / ui_name
        if not gen_path.exists():
            missing_by_cat["UI"].append(f"{ui_name} (missing source in gen/ui/)")

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
