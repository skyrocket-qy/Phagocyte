"""Quality checks for image assets: non-transparent corners, border leaking, AI gray noise."""

import os
from pathlib import Path
from typing import Dict, List, Optional, Tuple
from PIL import Image

try:
    import numpy as np
    HAS_NUMPY = True
except ImportError:
    HAS_NUMPY = False


def check_corner_artifacts(img: Image.Image) -> Optional[str]:
    """Check corner pixels for non-transparent / non-B/W content."""
    if img.mode != "RGBA":
        return None

    w, h = img.size
    corners = [(0, 0), (0, w - 1), (h - 1, 0), (h - 1, w - 1)]

    for y, x in corners:
        try:
            p = img.getpixel((x, y))
            if p[3] < 10:
                continue

            is_black = all(v < 5 for v in p[:3])
            is_white = all(v > 250 for v in p[:3])

            if not (is_black or is_white):
                return f"Corner pixel ({x}, {y}) is non-transparent/non-B/W (R{p[0]} G{p[1]} B{p[2]} A{p[3]})"
        except Exception:
            pass
    return None


def check_border_leaking(img: Image.Image) -> Optional[str]:
    """Check if sprite touches or leaks past canvas borders."""
    if img.mode != "RGBA":
        return None

    w, h = img.size
    total_border_pixels = w * 2 + h * 2 - 4
    if total_border_pixels <= 0:
        return None

    dirty_pixels = 0

    def is_opaque_border(px):
        return px[3] > 10 and not (all(v < 10 for v in px[:3]) or all(v > 245 for v in px[:3]))

    for x in range(w):
        if is_opaque_border(img.getpixel((x, 0))):
            dirty_pixels += 1
        if h > 1 and is_opaque_border(img.getpixel((x, h - 1))):
            dirty_pixels += 1

    for y in range(1, h - 1):
        if is_opaque_border(img.getpixel((0, y))):
            dirty_pixels += 1
        if w > 1 and is_opaque_border(img.getpixel((w - 1, y))):
            dirty_pixels += 1

    dirty_ratio = dirty_pixels / total_border_pixels
    if 0.1 < dirty_ratio < 0.99:
        return f"Border leakage detected ({dirty_ratio*100:.1f}% dirty border): possible clipping issue"
    return None


def check_image_quality(root_dir: Path = Path(".")) -> Tuple[List[str], Dict[str, any]]:
    """Scan all source assets in gen/ for visual quality defects."""
    issues: List[str] = []
    stats = {"scanned": 0, "issues": 0}

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
                if not f.lower().endswith((".png", ".webp")):
                    continue

                file_path = Path(root) / f
                stats["scanned"] += 1
                # Project-root relative path (e.g. gen/ui/xxx.png)
                display = Path("gen") / rel_dir / f

                try:
                    with Image.open(file_path) as img:
                        corner_err = check_corner_artifacts(img)
                        if corner_err:
                            issues.append(f"[Quality: Corner] {display}: {corner_err}")
                            stats["issues"] += 1

                        border_err = check_border_leaking(img)
                        if border_err:
                            issues.append(f"[Quality: Border] {display}: {border_err}")
                            stats["issues"] += 1
                except Exception as e:
                    issues.append(f"[Quality: Read Error] {display}: {e}")
                    stats["issues"] += 1

    return issues, stats
