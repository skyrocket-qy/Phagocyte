"""Detects orphan Godot .import files and unreferenced assets."""

import os
from pathlib import Path
from typing import List


def check_orphan_imports(root_dir: Path = Path(".")) -> List[str]:
    """Find .import files whose source asset no longer exists."""
    orphans: List[str] = []
    assets_dir = root_dir / "assets"

    if not assets_dir.exists():
        return orphans

    for root, _, files in os.walk(assets_dir):
        for f in files:
            if f.endswith(".import"):
                import_file = Path(root) / f
                # Source file name is the import file name without .import
                source_name = f[:-7]
                source_file = Path(root) / source_name

                if not source_file.exists():
                    orphans.append(f"[Orphan .import] Missing source for: {import_file.relative_to(root_dir)}")

    return orphans


def check_orphan_assets(root_dir: Path = Path(".")) -> List[str]:
    """Detect unreferenced assets or stray files in assets/gen/."""
    # Placeholder for scene/script asset reference scanning if needed in future
    return []
