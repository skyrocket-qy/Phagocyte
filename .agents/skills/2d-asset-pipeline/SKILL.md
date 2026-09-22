---
name: 2d-asset-pipeline
description: Complete guide and workflow for generating, prompt-engineering, slicing, and processing 2D Microscopic Bio-Fluorescence game assets and uniform sprite sheets for Phagocyte.
---

# 2D Asset Pipeline Workflow (Phagocyte)

## Overview
This skill defines the canonical, end-to-end workflow for generating, prompt-crafting, slicing, and processing 2D game assets in Project: Phagocyte.
All visual assets adhere to the **Confocal Laser Scanning Microscopy & Microscopic Bio-Fluorescence** aesthetic (暗視野顯微鏡 / 共軛焦螢光顯微鏡語言).

---

## ⚠️ The Iron Generation Rules

### 1. Mandatory Gemini Image Model Generation (NO Procedural/Script Art)
- **Rule**: All visual game assets MUST be generated using the Gemini image model via the `generate_image` tool.
- **Strictly Forbidden**: **NEVER** generate, draw, synthesize, or fake game sprites using procedural Python scripts, Pillow/OpenCV drawing primitives, Matplotlib, SVG generators, or algorithmic canvas hacks.
- **Role of Scripts**: Python scripts in `tools/slice/` and `tools/to_target_asset/` are strictly for **slicing** and **post-processing** (background removal, edge inpainting, filtering, downscaling, format conversion), **NEVER** for generating original visual art.

### 2. The Zero-Text Rule
AI image models frequently attempt to draw text labels, names, or captions under sprites. You MUST prevent and sanitize this:
- **Prompt Mandate**: Always append:
  ```text
  NO text, NO words, NO letters, NO labels, NO captions, NO writing, NO watermark, clean isolated game sprites only.
  ```
- **Pre-Slice Sanitization**: If any text labels are inadvertently rendered at the bottom of cell rows, use `--wipe-labels` with `tools/slice/main.py` or wipe the bottom 40px of each row before slicing:
  ```bash
  python3 tools/slice/main.py grid --input /path/to/sheet.png --wipe-labels --names "..."
  ```

### 3. Solid Pure Black Background (`#000000`)
- Always specify:
  ```text
  Isolated on solid pure black background #000000, dark-field confocal microscopy, glowing bio-luminescent edges, flat lighting, no cast shadows.
  ```
- Avoid white or checkered backgrounds which cause edge haloing during keying.

### 4. $4 \times 4$ Uniform Grid Standard
- Generate in **$1024 \times 1024$** square resolution with **$4 \times 4$** uniform cells (16 sprites per sheet).
- Slicing $1024 \div 4 = \mathbf{256 \times 256\text{ px}}$ per sprite. This exceeds the minimum $128 \times 128$ requirement in `gen/` and preserves crisp cryo-EM and confocal detail.

### 5. Microscopic Confocal Fluorescent Color Language
- **GFP Emerald Green (`#00FF88` / `#2ECC71`)**: Active organelles, enzymes, viability, phagocytic pseudopods.
- **RFP Crimson / Carmine Red (`#FF3366` / `#E74C3C`)**: Perforin drills, lytic enzymes, danger signals, terminal superweapons.
- **DAPI / Hoechst Electric Blue (`#00D2FF` / `#3498DB`)**: Nuclei, chromatin fibers, nucleic acids, NETosis traps.
- **YFP / FITC Amber Gold (`#FFD700`)**: Opsonin markers, ATP, metabolic overclocks, achievements.
- **Deep Void Black (`#000000`)**: Dark-field microscopic background.

---

## Directory Architecture

- **Source Art Tier (`gen/`)**: Stores source assets (either raw 1024x1024 sheets or $256 \times 256$ sliced sprites). Contains `gen/.gdignore` to prevent Godot from importing unoptimized source art.
  - Active categories: `achievement/`, `skill/`, `passive_tree/`, `ui/`.
- **Runtime Game Tier (`assets/gen/`)**: Stores runtime-optimized, transparent, edge-inpainted, Guided/Kuwahara-filtered game textures referenced by Godot scenes and JSON configurations.
  - Active categories: `achievement/` (with `*_unachieved.png` Steam variants), `skill/`, `passive_tree/`, `ui/`.

---

## Standard Master Prompt Templates

### 1. Master Achievement / Badge Prompt
```text
Confocal laser scanning microscopy style sprite sheet of 16 biological immunology achievement badges including <LIST_OF_16_ACHIEVEMENTS>. Glowing bioluminescent fluorophores in GFP emerald, RFP crimson, and DAPI electric blue with cryo-EM ultrastructure textures, circular biological medal motif. Arranged in a perfectly aligned 4x4 uniform grid. Isolated on solid pure black background #000000, dark-field microscope view, luminous edge borders, no cast shadows, NO text, NO words, NO letters, NO labels, NO captions, NO writing, NO watermark, clean isolated game sprites only, 1024x1024 pixel resolution.
```

### 2. Master Skill & Epigenetic Superweapon Prompt
```text
Confocal fluorescent microscopy style sprite sheet of 16 white blood cell biochemical skills and superweapons including <LIST_OF_16_SKILLS>. Energetic biocatalytic effects, lytic enzymes, reactive oxygen species clouds, perforin spiral beams, chromatin net traps. Vibrant GFP green and RFP red bioluminescence. Arranged in a perfectly aligned 4x4 uniform grid. Isolated on solid pure black background #000000, flat microscope lighting, sharp membrane edges, no cast shadows, NO text, NO words, NO letters, NO labels, NO captions, NO writing, NO watermark, clean isolated game sprites only, 1024x1024 pixel resolution.
```

### 3. Master Passive Tree & Organelle Prompt
```text
Fluorescence microscopy style sprite sheet of 16 cellular organelles and molecular passive traits including <LIST_OF_16_ORGANELLES>. Microtubule filaments, actin meshwork, double-membrane vesicles, mitochondria cristae, ribosomes, centrosomes. Hoechst blue and GFP fluorophore staining. Arranged in a perfectly aligned 4x4 uniform grid. Isolated on solid pure black background #000000, sharp edges, no shadows, NO text, NO words, NO letters, NO labels, NO captions, NO writing, NO watermark, clean isolated game sprites only, 1024x1024 pixel resolution.
```

---

## Step-by-Step Generation & Processing Workflow

### Step 1: Identify Missing Assets
Check missing assets using the integrity linter:
```bash
python3 tools/asset_check/main.py   # or: make check-assets
```
Pick 16-item batches from the [Subject Prompt Libraries](resources/README.md).

### Step 2: Generate Sprite Sheet via Gemini
Invoke `generate_image` tool:
```json
{
  "AspectRatio": "1:1",
  "ImageName": "<category>_4x4_01",
  "Prompt": "<MASTER_PROMPT_WITH_ZERO_TEXT_RULE>"
}
```

### Step 3: Slice into Sprites (`tools/slice/main.py`)
Run the slicing tool:
```bash
python3 tools/slice/main.py grid \
  --input /path/to/generated_sheet.png \
  --rows 4 \
  --cols 4 \
  --names "<item_01>,<item_02>,...,<item_16>" \
  --output gen/<category>
```

### Step 4: Run Target Pipeline (`tools/to_target_asset/main.py`)
Process source sprites into runtime game assets:
```bash
python3 tools/to_target_asset/main.py --no-clean --category <category>
```
This automatically applies:
- BFS outer floodfill background keying (`tolerance=28`).
- Telea RGB edge inpainting into transparent boundary pixels.
- Guided / Kuwahara edge-preserving filter.
- Downscaling to target runtime dimensions ($256 \times 256$ for achievements, $128 \times 128$ for skills/traits).
- Grayscale + dimmed unachieved variant generation (`*_unachieved.png` for achievements).

### Step 5: Reimport in Godot
Run Godot in headless editor mode to generate `.import` files for all new textures:
```bash
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --editor --quit
```

### Step 6: Verify Quality & Integrity
```bash
make check-assets     # Runs all 6 asset checks
make build            # Verifies C# build clean (0 errors, 0 warnings)
```
