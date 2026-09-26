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


def _extract_ids(data: Any) -> List[str]:
    """Accept [{id}], {traits: [{id}]}, or plain [id-strings]."""
    if isinstance(data, list):
        ids = []
        for item in data:
            if isinstance(item, str) and item:
                ids.append(item)
            elif isinstance(item, dict) and item.get("id"):
                ids.append(item["id"])
        return ids
    if isinstance(data, dict):
        traits = data.get("traits", [])
        if isinstance(traits, list):
            return [t.get("id") for t in traits if isinstance(t, dict) and t.get("id")]
    return []


def check_missing_assets(
    root_dir: Path = Path("."),
    target_category: Optional[str] = None
) -> Tuple[Dict[str, List[str]], List[str], Dict[str, int]]:
    """
    Bidirectional Asset Integrity Verification across active categories.
    Ids equal gen/ file stems (no prefixes, Vistrace-style):
    - Achievement (from achievements.json -> gen/achievement/{id}.png)
    - Skill (from skill/active.json + passive.json -> gen/skill/{id}.png)
    - PassiveTree (from passive_traits.json -> gen/passive_tree/{id}.png)
    - Gear (from gear.json -> gen/gear/{id}.png)
    - UI (from ui.json -> gen/ui/{id}.png)

    Returns:
        missing_by_cat: Dict mapping category name to list of missing asset paths/names
        unmapped: List of stray files on disk not registered in game data
        category_counts: Dict mapping category name to total registered item count
    """
    missing_by_cat: Dict[str, List[str]] = {
        "Achievement": [],
        "Skill": [],
        "PassiveTree": [],
        "Gear": [],
        "UI": [],
    }
    unmapped: List[str] = []
    category_counts: Dict[str, int] = {}

    assets_dir = root_dir / "assets"
    gen_dir = root_dir / "gen"

    # Per-category expected stems (id == stem) for reverse checks.
    expected_stems: Dict[str, Set[str]] = {}

    # ── 1. Achievements ───────────────────────────────────────────────────
    # The *_unachieved.png locked variants are derived artifacts produced by
    # the to_target_asset pipeline into assets/gen/achievement/, so only the
    # base source is required here.
    achievements_file = assets_dir / "data" / "achievements.json"
    for ach_id in _extract_ids(_load_json(achievements_file) or []):
        expected_stems.setdefault("achievement", set()).update([ach_id, f"{ach_id}_unachieved"])
        category_counts["Achievement"] = category_counts.get("Achievement", 0) + 1

        gen_path = gen_dir / "achievement" / f"{ach_id}.png"
        if not gen_path.exists():
            missing_by_cat["Achievement"].append(f"gen/achievement/{ach_id}.png")

    # ── 2. Skills ─────────────────────────────────────────────────────────
    skill_ids: list[str] = []
    for skill_file in ("active.json", "passive.json"):
        skill_ids.extend(_extract_ids(_load_json(assets_dir / "data" / "skill" / skill_file) or []))
    for skill_id in skill_ids:
        expected_stems.setdefault("skill", set()).add(skill_id)
        category_counts["Skill"] = category_counts.get("Skill", 0) + 1

        gen_path = gen_dir / "skill" / f"{skill_id}.png"
        if not gen_path.exists():
            missing_by_cat["Skill"].append(f"gen/skill/{skill_id}.png")

    # ── 3. Passive Tree Traits ────────────────────────────────────────────
    traits_file = assets_dir / "data" / "passive_traits.json"
    for trait_id in _extract_ids(_load_json(traits_file) or {}):
        expected_stems.setdefault("passive_tree", set()).add(trait_id)
        category_counts["PassiveTree"] = category_counts.get("PassiveTree", 0) + 1

        gen_path = gen_dir / "passive_tree" / f"{trait_id}.png"
        if not gen_path.exists():
            missing_by_cat["PassiveTree"].append(f"gen/passive_tree/{trait_id}.png")

    # ── 4. Gear Chamber Equipment ────────────────────────────────────
    gear_file = assets_dir / "data" / "gear.json"
    for gear_id in _extract_ids(_load_json(gear_file) or []):
        expected_stems.setdefault("gear", set()).add(gear_id)
        category_counts["Gear"] = category_counts.get("Gear", 0) + 1

        gen_path = gen_dir / "gear" / f"{gear_id}.png"
        if not gen_path.exists():
            missing_by_cat["Gear"].append(f"gen/gear/{gear_id}.png")

    # ── 5. UI (registry: assets/data/ui.json) ────────────────────────────
    ui_file = assets_dir / "data" / "ui.json"
    for ui_id in _extract_ids(_load_json(ui_file) or []):
        expected_stems.setdefault("ui", set()).add(ui_id)
        category_counts["UI"] = category_counts.get("UI", 0) + 1

        gen_path = gen_dir / "ui" / f"{ui_id}.png"
        if not gen_path.exists():
            missing_by_cat["UI"].append(f"gen/ui/{ui_id}.png")

    # ── 6. Reverse Reference Checks (Disk -> Data, gen/ only) ───────────────
    # Only gen/ is scanned: it is the source of truth for
    # achievement/skill/passive_tree/gear/ui. Processed outputs under
    # assets/gen/ are pipeline artifacts, not registered sources.
    if gen_dir.exists():
        for root, _, files in os.walk(gen_dir):
            rel_root = Path(root).relative_to(gen_dir)
            if not rel_root.parts:
                continue
            cat = rel_root.parts[0]
            if cat not in ["achievement", "skill", "passive_tree", "gear", "ui"]:
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
            "gear": "Gear",
            "ui": "UI",
        }.get(target_category.lower(), target_category)

        missing_by_cat = {k: v for k, v in missing_by_cat.items() if k.lower() == cat_key.lower()}

    return missing_by_cat, unmapped, category_counts
