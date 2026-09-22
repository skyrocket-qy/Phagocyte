#!/usr/bin/env python3
"""
Tool for copying and processing generated assets into target game asset directories.
Source folder: gen/
Target folder: assets/gen/ (distinguishing generated AI assets from non-gen)
"""

import argparse
import os
import shutil
import sys
from pathlib import Path
from typing import Tuple, List, Optional
from PIL import Image

SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parent.parent
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

try:
    from .pipeline import ImageContext
    from .registry import get_pipeline_for_path
except ImportError:
    from pipeline import ImageContext
    from registry import get_pipeline_for_path


def process_asset_file(src_file: Path, dest_file: Path, rel_path: Path) -> Tuple[int, List[str]]:
    """Process a single image file through its target category pipeline."""
    dest_file.parent.mkdir(parents=True, exist_ok=True)

    try:
        with Image.open(src_file) as pil_img:
            ctx = ImageContext(
                src_path=src_file,
                dest_path=dest_file,
                category=rel_path.parts[0] if rel_path.parts else "default",
                pil_image=pil_img.convert("RGBA")
            )
            ctx.sync_from_pil()

        pipeline = get_pipeline_for_path(rel_path)
        ctx = pipeline.execute(ctx)

        # Save main processed result
        ctx.pil_image.save(dest_file, format="PNG")
        img_count = 1

        # Save extra outputs (e.g. _unachieved.png)
        for extra_path, extra_pil in ctx.extra_outputs:
            extra_path.parent.mkdir(parents=True, exist_ok=True)
            extra_pil.save(extra_path, format="PNG")
            img_count += 1

        return img_count, ctx.warnings
    except Exception as e:
        print(f"Error processing image {src_file}: {e}", file=sys.stderr)
        return 0, [f"[ERROR] Failed to process {src_file}: {e}"]


def process_all_assets(
    gen_dir: Path,
    assets_dir: Path,
    target_category: Optional[str] = None,
    clean: bool = False
) -> Tuple[int, int, List[str]]:
    if not gen_dir.exists():
        print(f"Source directory {gen_dir} does not exist.", file=sys.stderr)
        return 0, 0, []

    if clean:
        clean_target = assets_dir / target_category if target_category else assets_dir
        if clean_target.exists():
            print(f"Cleaning target directory: {clean_target}")
            shutil.rmtree(clean_target)

    total_images = 0
    total_other = 0
    all_warnings = []

    for root, _, files in os.walk(gen_dir):
        rel_root = Path(root).relative_to(gen_dir)

        if target_category:
            if not rel_root.parts or rel_root.parts[0] != target_category:
                continue

        target_root = assets_dir / rel_root
        target_root.mkdir(parents=True, exist_ok=True)

        for file_name in files:
            if file_name.startswith(".") or file_name.lower().endswith(".import"):
                continue

            src_file = Path(root) / file_name
            dest_file = target_root / file_name
            rel_file_path = rel_root / file_name

            if file_name.lower().endswith((".png", ".jpg", ".jpeg", ".webp")):
                # Ensure output is PNG for transparency
                if not dest_file.suffix.lower() == ".png":
                    dest_file = dest_file.with_suffix(".png")

                img_cnt, warnings = process_asset_file(src_file, dest_file, rel_file_path)
                total_images += img_cnt
                if warnings:
                    all_warnings.extend([f"{rel_file_path}: {w}" for w in warnings])
            else:
                shutil.copy2(src_file, dest_file)
                total_other += 1

    return total_images, total_other, all_warnings


def main():
    parser = argparse.ArgumentParser(
        description="Process generated assets (gen/) to runtime game asset directory (assets/gen/)."
    )
    parser.add_argument(
        "--gen-dir",
        type=Path,
        default=PROJECT_ROOT / "gen",
        help="Source directory with generated source assets (default: gen/)"
    )
    parser.add_argument(
        "--assets-dir",
        type=Path,
        default=PROJECT_ROOT / "assets" / "gen",
        help="Target runtime directory (default: assets/gen/)"
    )
    parser.add_argument(
        "--category",
        type=str,
        default=None,
        help="Target only a specific category (e.g. achievement, skill, passive_tree, ui)"
    )
    parser.add_argument(
        "--clean",
        action="store_true",
        help="Wipe destination category directory before copying"
    )
    parser.add_argument(
        "--no-clean",
        dest="clean",
        action="store_false",
        help="Keep existing destination files (default)"
    )
    parser.set_defaults(clean=False)

    args = parser.parse_args()

    print(f"Starting asset pipeline: {args.gen_dir} -> {args.assets_dir}")
    if args.category:
        print(f"Target category filter: {args.category}")

    img_count, other_count, warnings = process_all_assets(
        gen_dir=args.gen_dir,
        assets_dir=args.assets_dir,
        target_category=args.category,
        clean=args.clean
    )

    print("\n--- Pipeline Summary ---")
    print(f"Total processed images: {img_count}")
    print(f"Other copied assets:    {other_count}")

    if warnings:
        print(f"\n[!] Warnings ({len(warnings)}):")
        for w in warnings:
            print(f"  - {w}")
    else:
        print("\nAll assets processed cleanly with 0 warnings.")


if __name__ == "__main__":
    main()
