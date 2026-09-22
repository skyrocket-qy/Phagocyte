"""Validates original asset resolutions and aspect ratios in gen/ against Phagocyte standards."""

import os
from pathlib import Path
from typing import Dict, List, Tuple
from PIL import Image

# Minimum acceptable dimensions per asset category in gen/ (>= 128x128 for icons/sprites, standard 256x256)
MINIMUM_DIMENSIONS: Dict[str, Tuple[int, int]] = {
    "achievement": (128, 128),
    "skill": (128, 128),
    "passive_tree": (128, 128),
    "ui": (64, 64),
}

# Categories where 1:1 square aspect ratio is strictly required
ASPECT_RATIO_1_1_CATEGORIES = {
    "achievement",
    "skill",
    "passive_tree",
}


def _get_category_key(rel_str: str) -> str:
    parts = rel_str.split("/")
    if parts:
        return parts[0]
    return "default"


def check_asset_resolutions(root_dir: Path = Path(".")) -> Tuple[List[str], Dict[str, any]]:
    """Scan all original generated images in gen/ and report non-1:1 aspect ratios and sub-minimum sizes."""
    violations: List[str] = []
    stats = {
        "total_scanned": 0,
        "valid": 0,
        "sub_minimum": 0,
        "non_square": 0,
    }

    gen_dir = root_dir / "gen"
    if not gen_dir.exists():
        return violations, stats

    for root, _, files in os.walk(gen_dir):
        rel_dir = Path(root).relative_to(gen_dir)
        cat_key = _get_category_key(str(rel_dir).replace("\\", "/"))

        min_w, min_h = MINIMUM_DIMENSIONS.get(cat_key, (64, 64))
        require_square = cat_key in ASPECT_RATIO_1_1_CATEGORIES

        for f in files:
            if f.startswith(".") or f.endswith(".import"):
                continue
            if not f.lower().endswith((".png", ".jpg", ".jpeg", ".webp")):
                continue

            file_path = Path(root) / f
            stats["total_scanned"] += 1
            # Project-root relative path (e.g. gen/ui/xxx.png)
            display = Path("gen") / rel_dir / f

            try:
                with Image.open(file_path) as img:
                    w, h = img.size

                    # Check 1: Minimum dimensions
                    if w < min_w or h < min_h:
                        violations.append(
                            f"[Low Resolution] {display}: {w}x{h} px (Minimum required for '{cat_key}' is {min_w}x{min_h} px)"
                        )
                        stats["sub_minimum"] += 1
                        continue

                    # Check 2: 1:1 Aspect Ratio (Square)
                    if require_square and w != h:
                        violations.append(
                            f"[Non-Square Aspect Ratio] {display}: {w}x{h} px (1:1 square strictly required for '{cat_key}')"
                        )
                        stats["non_square"] += 1
                        continue

                    stats["valid"] += 1
            except Exception as e:
                violations.append(f"[Corrupt Image] {display}: Failed to open ({e})")

    return violations, stats
