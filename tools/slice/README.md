# Phagocyte Sprite Slicing Tool

Slices 1024x1024 Gemini-generated sprite sheets into isolated sprites for the `gen/` directory.

## Requirements
Pure Python + Pillow (`pip install Pillow`).

## Usage

### 1. 4x4 Grid Slicing (Standard)
Slices a 1024x1024 sheet into 16 uniform 256x256 px cells:

```bash
python3 tools/slice/main.py grid \
  --input /path/to/sheet.png \
  --rows 4 \
  --cols 4 \
  --names "achievement/ach_first_digestion, achievement/ach_engulf_20, skill/phagocytic_grasp, skip, passive_tree/trait_actin" \
  --output gen
```

Key options:
- `--keep-bg`: Do not key out black background (`#000000`).
- `--bg-threshold 40`: RGB cutoff for dark background keying.
- `--wipe-labels`: Blank out bottom 40px of each cell row before slicing if AI model rendered unwanted text/captions.

### 2. Flexible Region Slicing
Extracts arbitrary unit-based sub-regions:

```bash
python3 tools/slice/main.py flexible \
  --input /path/to/sheet.png \
  --unit 256 \
  --specs "ui/large_card,0,0,2,2; ui/slot,2,0,1,1" \
  --output gen
```

Format of specs: `relative_path,grid_x,grid_y,width_in_units,height_in_units` (separated by semicolons `;`).
