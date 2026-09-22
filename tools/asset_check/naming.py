"""Checks asset naming conventions, snake_case integrity, and standard prefixes."""

import os
import re
from pathlib import Path
from typing import List

SNAKE_CASE_PATTERN = re.compile(r"^[a-z0-9_]+$")


def check_naming_conventions(root_dir: Path = Path(".")) -> List[str]:
    issues: List[str] = []
    # Source of truth is gen/ for achievement/skill/passive_tree/ui.
    # assets/gen/ holds pipeline-processed artifacts, so it is skipped.
    scan_dirs = [root_dir / "gen"]

    for base_dir in scan_dirs:
        if not base_dir.exists():
            continue

        for root, _, files in os.walk(base_dir):
            rel_dir = Path(root).relative_to(base_dir)

            for f in files:
                if f.startswith(".") or f.endswith(".import"):
                    continue

                stem = Path(f).stem
                suffix = Path(f).suffix.lower()

                # 1. Lowercase file extension
                if Path(f).suffix != suffix:
                    issues.append(f"[Extension] Uppercase extension found: {rel_dir / f}")

                # 2. snake_case enforcement
                if not SNAKE_CASE_PATTERN.match(stem):
                    issues.append(f"[Naming] Non-snake_case filename: {rel_dir / f}")

                # 3. Category prefix recommendations
                if rel_dir.parts:
                    cat = rel_dir.parts[0]
                    if cat == "achievement" and not stem.startswith("ach_"):
                        issues.append(f"[Prefix] Achievement icon missing 'ach_' prefix: {rel_dir / f}")
                    elif cat == "passive_tree" and not (stem.startswith("trait_") or stem.startswith("tree_")):
                        issues.append(f"[Prefix] Passive tree icon should start with 'trait_' or 'tree_': {rel_dir / f}")

    return issues
