"""Checks asset naming conventions (snake_case integrity and casing)."""

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
                # Project-root relative path (e.g. gen/ui/xxx.png)
                display = Path("gen") / rel_dir / f

                # 1. Lowercase file extension
                if Path(f).suffix != suffix:
                    issues.append(f"[Extension] Uppercase extension found: {display}")

                # 2. snake_case enforcement
                if not SNAKE_CASE_PATTERN.match(stem):
                    issues.append(f"[Naming] Non-snake_case filename: {display}")

    return issues
