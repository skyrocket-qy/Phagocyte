#!/usr/bin/env python3
"""
Phagocyte Game Visual Evaluation - Image Diff & Regression Comparator.
Pure-Pillow image comparison tool for before/after visual evaluation captures.
Generates side-by-side composites, amplified pixel delta maps, and summary metrics.
"""

import argparse
import os
import sys
from pathlib import Path
from typing import Dict, List, Optional, Tuple

try:
    from PIL import Image, ImageChops, ImageDraw, ImageFont
except ImportError:
    print("Error: Pillow (PIL) is required. Run 'pip install Pillow' or 'make py-env'.", file=sys.stderr)
    sys.exit(1)


# ANSI Terminal Colors
RESET = "\033[0m"
BOLD = "\033[1m"
RED = "\033[91m"
GREEN = "\033[92m"
YELLOW = "\033[93m"
BLUE = "\033[94m"
CYAN = "\033[96m"
WHITE = "\033[97m"


def get_default_font(size: int = 16):
    """Attempt to load a standard truetype font or fallback to default bitmap."""
    candidates = [
        "/System/Library/Fonts/SFNSMono.ttf",
        "/System/Library/Fonts/Menlo.ttc",
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf",
        "/usr/share/fonts/TTF/DejaVuSans.ttf",
    ]
    for c in candidates:
        if os.path.exists(c):
            try:
                return ImageFont.truetype(c, size)
            except Exception:
                pass
    return ImageFont.load_default()


def compute_image_diff(
    img_before: Image.Image,
    img_after: Image.Image,
    pixel_tolerance: int = 5,
) -> Tuple[float, float, Image.Image]:
    """
    Compare two images and return:
    - changed_ratio: Fraction of pixels differing by more than pixel_tolerance (0.0 to 1.0)
    - max_delta: Maximum absolute channel difference observed (0 to 255)
    - diff_map: RGBA image highlighting differences in electric red/magenta over darkened background
    """
    # Normalize sizes if mismatched
    w = max(img_before.width, img_after.width)
    h = max(img_before.height, img_after.height)

    b_canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    a_canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    b_canvas.paste(img_before.convert("RGBA"), (0, 0))
    a_canvas.paste(img_after.convert("RGBA"), (0, 0))

    b_pix = b_canvas.load()
    a_pix = a_canvas.load()

    diff_map = Image.new("RGBA", (w, h))
    diff_pix = diff_map.load()

    total_pixels = w * h
    changed_count = 0
    max_delta = 0

    for y in range(h):
        for x in range(w):
            rb, gb, bb, ab = b_pix[x, y]
            ra, ga, ba, aa = a_pix[x, y]

            dr = abs(rb - ra)
            dg = abs(gb - ga)
            db = abs(bb - ba)
            da = abs(ab - aa)
            delta = max(dr, dg, db, da)

            if delta > max_delta:
                max_delta = delta

            if delta > pixel_tolerance:
                changed_count += 1
                intensity = min(255, 60 + int(delta * 2.5))
                diff_pix[x, y] = (intensity, 20, 90, 255)
            else:
                avg_lum = (ra + ga + ba) // 9
                diff_pix[x, y] = (avg_lum, avg_lum + 5, avg_lum + 12, 180)

    changed_ratio = changed_count / max(1, total_pixels)
    return changed_ratio, float(max_delta), diff_map


def render_composite(
    img_before: Image.Image,
    img_after: Image.Image,
    diff_map: Image.Image,
    name: str,
    changed_ratio: float,
    max_delta: float,
) -> Image.Image:
    """Create a 3-column side-by-side composite: [ BEFORE | AFTER | DIFF MAP ]."""
    w = max(img_before.width, img_after.width, diff_map.width)
    h = max(img_before.height, img_after.height, diff_map.height)

    header_h = 42
    footer_h = 32
    col_gap = 12
    margin = 16

    total_w = margin * 2 + w * 3 + col_gap * 2
    total_h = margin * 2 + header_h + h + footer_h

    composite = Image.new("RGB", (total_w, total_h), (16, 20, 28))
    draw = ImageDraw.Draw(composite)
    font = get_default_font(14)
    title_font = get_default_font(16)

    # Title & Stats header
    pct_text = f"{changed_ratio * 100:.2f}%"
    status_text = "IDENTICAL" if changed_ratio == 0 else f"DIFF: {pct_text} (Max Δ: {int(max_delta)})"
    draw.text(
        (margin, margin),
        f"Surface: {name}  |  {status_text}",
        fill=(0, 220, 255),
        font=title_font,
    )

    # Subheaders for each column
    col_labels = ["1. BEFORE (Baseline)", "2. AFTER (Current)", f"3. DIFF MAP ({pct_text} changed)"]
    col_x = [
        margin,
        margin + w + col_gap,
        margin + (w + col_gap) * 2,
    ]

    base_y = margin + header_h

    for x_pos, label in zip(col_x, col_labels):
        draw.text((x_pos, base_y - 20), label, fill=(180, 195, 210), font=font)
        # Background box for viewport
        draw.rectangle(
            [x_pos - 1, base_y - 1, x_pos + w, base_y + h],
            outline=(45, 60, 80),
            width=1,
        )

    # Paste panels
    composite.paste(img_before.convert("RGB"), (col_x[0], base_y))
    composite.paste(img_after.convert("RGB"), (col_x[1], base_y))
    composite.paste(diff_map.convert("RGB"), (col_x[2], base_y))

    # Footer note
    draw.text(
        (margin, base_y + h + 8),
        "Phagocyte Visual Eval — Unchanged context dimmed; differences highlighted in fluorescent magenta/red.",
        fill=(110, 130, 150),
        font=font,
    )

    return composite


def run_diff(
    before_dir: Path,
    after_dir: Path,
    output_dir: Path,
    threshold: float = 0.02,
    pixel_tolerance: int = 5,
) -> bool:
    """Run visual comparison across matching PNG captures in before/after directories."""
    print(f"\n{CYAN}{BOLD}╔══════════════════════════════════════════════════════════════════════════╗{RESET}")
    print(f"{CYAN}{BOLD}║{WHITE}{BOLD}             🔬  PHAGOCYTE VISUAL REGRESSION COMPARATOR                   {CYAN}{BOLD}║{RESET}")
    print(f"{CYAN}{BOLD}╚══════════════════════════════════════════════════════════════════════════╝{RESET}\n")

    if not before_dir.exists():
        print(f"{RED}Error: Before directory does not exist: {before_dir}{RESET}", file=sys.stderr)
        return False
    if not after_dir.exists():
        print(f"{RED}Error: After directory does not exist: {after_dir}{RESET}", file=sys.stderr)
        return False

    output_dir.mkdir(parents=True, exist_ok=True)

    # Gather images
    valid_exts = {".png", ".webp"}
    before_files = {p.name: p for p in before_dir.iterdir() if p.suffix.lower() in valid_exts}
    after_files = {p.name: p for p in after_dir.iterdir() if p.suffix.lower() in valid_exts}

    all_names = sorted(set(before_files.keys()) | set(after_files.keys()))
    if not all_names:
        print(f"{YELLOW}No image files found in either directory.{RESET}")
        return True

    print(f"{BLUE}Comparing captures:{RESET}")
    print(f"  • Before : {before_dir}")
    print(f"  • After  : {after_dir}")
    print(f"  • Output : {output_dir}")
    print(f"  • Threshold: {threshold * 100:.1f}% max allowed changed pixels\n")

    results: List[Dict] = []
    has_failures = False

    for name in all_names:
        b_path = before_files.get(name)
        a_path = after_files.get(name)

        if not b_path:
            print(f"  {YELLOW}• {name:<26} : [NEW SURFACE IN AFTER DIR]{RESET}")
            results.append({"name": name, "status": "NEW", "ratio": 0.0, "delta": 0.0})
            continue
        if not a_path:
            print(f"  {RED}• {name:<26} : [MISSING IN AFTER DIR]{RESET}")
            results.append({"name": name, "status": "MISSING", "ratio": 1.0, "delta": 255.0})
            has_failures = True
            continue

        try:
            with Image.open(b_path) as img_b, Image.open(a_path) as img_a:
                ratio, max_delta, diff_map = compute_image_diff(img_b, img_a, pixel_tolerance)

                composite = render_composite(img_b, img_a, diff_map, name, ratio, max_delta)
                out_path = output_dir / f"diff_{name}"
                composite.save(out_path, format="PNG")

                is_fail = ratio > threshold
                if is_fail:
                    has_failures = True

                status = "FAIL" if is_fail else ("DIFF" if ratio > 0 else "PASS")
                status_color = RED if is_fail else (YELLOW if ratio > 0 else GREEN)

                pct_str = f"{ratio * 100:>6.2f}%"
                print(f"  • {name:<26} : {status_color}{BOLD}{status:<5}{RESET} ({pct_str} changed, max Δ={int(max_delta):>3}) -> {out_path.name}")
                results.append({"name": name, "status": status, "ratio": ratio, "delta": max_delta, "out": out_path})

        except Exception as e:
            print(f"  {RED}• {name:<26} : ERROR: {e}{RESET}")
            has_failures = True
            results.append({"name": name, "status": "ERROR", "ratio": 1.0, "delta": 0.0})

    # Summary table
    print(f"\n{CYAN}{BOLD}══════════════════════════════════════════════════════════════════════════{RESET}")
    passed_count = sum(1 for r in results if r["status"] in ("PASS", "DIFF"))
    failed_count = sum(1 for r in results if r["status"] in ("FAIL", "MISSING", "ERROR"))

    if has_failures:
        print(f"{RED}{BOLD}❌ VISUAL REGRESSION DETECTED: {failed_count} surface(s) exceeded delta threshold ({threshold * 100:.1f}%).{RESET}")
        print(f"Review composites in: {output_dir}")
        return False
    else:
        print(f"{GREEN}{BOLD}🎉 ALL {passed_count} VISUAL SURFACES MATCH WITHIN TOLERANCE ({threshold * 100:.1f}%).{RESET}")
        return True


def main():
    parser = argparse.ArgumentParser(description="Phagocyte Game Visual Evaluation - Image Comparator")
    parser.add_argument("--before", "-b", type=Path, required=True, help="Directory with baseline PNG captures")
    parser.add_argument("--after", "-a", type=Path, required=True, help="Directory with new PNG captures")
    parser.add_argument("--output", "-o", type=Path, required=True, help="Directory to save diff composite PNGs")
    parser.add_argument("--threshold", "-t", type=float, default=0.02, help="Max fraction of changed pixels before failing (default: 0.02 = 2%%)")
    parser.add_argument("--tolerance", "-p", type=int, default=5, help="Per-channel pixel tolerance (0-255, default: 5)")

    args = parser.parse_args()
    success = run_diff(
        before_dir=args.before,
        after_dir=args.after,
        output_dir=args.output,
        threshold=args.threshold,
        pixel_tolerance=args.tolerance,
    )
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()
