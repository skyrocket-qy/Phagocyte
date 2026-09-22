#!/usr/bin/env python3
"""
Phagocyte Sprite Slicing Tool for Gemini 1024x1024 Assets.
Pure Python + Pillow implementation (no OpenCV/NumPy required).
"""

import argparse
import sys
from pathlib import Path
from PIL import Image, ImageDraw


def remove_dark_background(img: Image.Image, threshold: int = 40) -> Image.Image:
    """Replaces dark background pixels (R < threshold and G < threshold and B < threshold) with transparent alpha."""
    rgba = img.convert("RGBA")
    pix = rgba.load()
    w, h = rgba.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = pix[x, y]
            if r < threshold and g < threshold and b < threshold:
                pix[x, y] = (r, g, b, 0)
    return rgba


def wipe_text_labels(img: Image.Image, rows: int, strip_height: int = 40) -> Image.Image:
    """Blanks out bottom strip of each row to eliminate unwanted AI text labels/watermarks before slicing."""
    cleaned = img.copy()
    draw = ImageDraw.Draw(cleaned)
    w, h = cleaned.size
    cell_h = h // rows
    for r in range(rows):
        y1 = (r + 1) * cell_h - strip_height
        y2 = (r + 1) * cell_h
        draw.rectangle([0, y1, w, y2], fill=(0, 0, 0, 255 if cleaned.mode == "RGBA" else 0))
    return cleaned


def slice_grid(
    input_path: Path,
    rows: int,
    cols: int,
    names: str,
    output_dir: Path,
    keep_bg: bool = False,
    bg_threshold: int = 40,
    wipe_labels: bool = False,
    wipe_height: int = 40,
):
    if not input_path.exists():
        print(f"Error: Input file does not exist: {input_path}", file=sys.stderr)
        sys.exit(1)

    output_dir.mkdir(parents=True, exist_ok=True)

    with Image.open(input_path) as img:
        img = img.convert("RGBA")
        if wipe_labels:
            img = wipe_text_labels(img, rows=rows, strip_height=wipe_height)

        w, h = img.size
        cell_w = w // cols
        cell_h = h // rows

        if cell_w == 0 or cell_h == 0:
            print(f"Error: Grid dimensions result in 0px cell size ({w}x{h} / {cols}x{rows})", file=sys.stderr)
            sys.exit(1)

        print(f"Processing image {w}x{h} -> Grid {cols}x{rows} (Cell size: {cell_w}x{cell_h} px)")

        name_list = [s.strip() for s in names.split(",") if s.strip()]
        max_cells = rows * cols

        for i, name in enumerate(name_list):
            if i >= max_cells:
                print(f"[WARN] Provided more names than grid cells ({len(name_list)} > {max_cells}). Stopping.")
                break

            if not name or name.lower() == "skip":
                continue

            col = i % cols
            row = i // cols

            x = col * cell_w
            y = row * cell_h
            box = (x, y, x + cell_w, y + cell_h)
            cell = img.crop(box)

            if not keep_bg:
                cell = remove_dark_background(cell, threshold=bg_threshold)

            # Handle nested paths in names (e.g. achievement/ach_first_digestion)
            if "/" in name or "\\" in name:
                save_path = output_dir / Path(name)
                if not save_path.suffix:
                    save_path = save_path.with_suffix(".png")
                save_path.parent.mkdir(parents=True, exist_ok=True)
            else:
                save_path = output_dir / f"{name}.png"

            cell.save(save_path, format="PNG")
            print(f"Saved {save_path}")


def slice_flexible(
    input_path: Path,
    unit: int,
    specs: str,
    output_dir: Path,
):
    if not input_path.exists():
        print(f"Error: Input file does not exist: {input_path}", file=sys.stderr)
        sys.exit(1)

    with Image.open(input_path) as img:
        img = img.convert("RGBA")
        w, h = img.size

        for spec in specs.split(";"):
            if not spec.strip():
                continue

            parts = [p.strip() for p in spec.split(",")]
            if len(parts) != 5:
                print(f"Invalid spec format: {spec}. Expected path,x,y,w,h", file=sys.stderr)
                continue

            path_str, x_unit, y_unit, w_unit, h_unit = parts
            try:
                x, y, sw, sh = int(x_unit), int(y_unit), int(w_unit), int(h_unit)
            except ValueError:
                print(f"Invalid numeric values in spec: {spec}", file=sys.stderr)
                continue

            rx, ry = x * unit, y * unit
            rw, rh = sw * unit, sh * unit

            if rx + rw > w or ry + rh > h:
                print(f"Warning: Region {path_str} ({rw}x{rh} at {rx},{ry}) is out of bounds. Skipping.")
                continue

            box = (rx, ry, rx + rw, ry + rh)
            sub_img = img.crop(box)

            save_path = output_dir / Path(path_str)
            if not save_path.suffix:
                save_path = save_path.with_suffix(".png")

            save_path.parent.mkdir(parents=True, exist_ok=True)
            sub_img.save(save_path, format="PNG")
            print(f"Saved {save_path} ({sw}x{sh} units -> {rw}x{rh} pixels)")


def main():
    parser = argparse.ArgumentParser(description="Phagocyte Sprite Slicing Tool for Gemini 1024x1024 Assets")
    subparsers = parser.add_subparsers(dest="command", required=True)

    # Grid command
    grid_parser = subparsers.add_parser("grid", help="Slice regular grid")
    grid_parser.add_argument("--input", "-i", required=True, type=Path, help="Input image path")
    grid_parser.add_argument("--rows", "-r", type=int, default=4, help="Number of rows (default 4)")
    grid_parser.add_argument("--cols", "-c", type=int, default=4, help="Number of columns (default 4)")
    grid_parser.add_argument("--names", "-n", required=True, type=str, help="Comma-separated list of filenames/paths")
    grid_parser.add_argument("--output", "-o", type=Path, default=Path("gen"), help="Output directory (default gen/)")
    grid_parser.add_argument("--keep-bg", action="store_true", help="Keep dark background (R,G,B < 40)")
    grid_parser.add_argument("--bg-threshold", type=int, default=40, help="Background keying threshold")
    grid_parser.add_argument("--wipe-labels", action="store_true", help="Wipe bottom row strip of labels before slicing")
    grid_parser.add_argument("--wipe-height", type=int, default=40, help="Height of label strip in pixels (default 40)")

    # Flexible command
    flex_parser = subparsers.add_parser("flexible", help="Slice variable unit regions")
    flex_parser.add_argument("--input", "-i", required=True, type=Path, help="Input image path")
    flex_parser.add_argument("--unit", "-u", type=int, default=128, help="Unit size in pixels (default 128)")
    flex_parser.add_argument("--specs", "-s", required=True, type=str, help="Semi-colon separated list of path,x,y,w,h")
    flex_parser.add_argument("--output", "-o", type=Path, default=Path("gen"), help="Output root directory")

    args = parser.parse_args()

    if args.command == "grid":
        slice_grid(
            input_path=args.input,
            rows=args.rows,
            cols=args.cols,
            names=args.names,
            output_dir=args.output,
            keep_bg=args.keep_bg,
            bg_threshold=args.bg_threshold,
            wipe_labels=args.wipe_labels,
            wipe_height=args.wipe_height,
        )
    elif args.command == "flexible":
        slice_flexible(
            input_path=args.input,
            unit=args.unit,
            specs=args.specs,
            output_dir=args.output,
        )


if __name__ == "__main__":
    main()
