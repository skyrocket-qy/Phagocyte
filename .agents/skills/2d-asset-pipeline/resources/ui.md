# UI & Microscopic HUD Prompt Library (Phagocyte)

Master prompts for confocal microscope HUD elements, reticles, holographic scanner cards, and clinical status badges.
Runtime output: `assets/gen/ui/`.

---

## Batch 1: Microscope Reticles, Scanner Frames & HUD Elements (16 Items)

### Item Mapping (Row by Row)
- **Row 1**:
  - `reticle_target`: Circular confocal laser focusing reticle with micrometer tick marks
  - `reticle_danger`: Triangular crimson danger warning frame pulsing with exclamation glyph
  - `reticle_scan`: Concentric holographic sonar scanning rings with radial degree gradients
  - `frame_organelle`: Octagonal bio-luminescent border frame for slotting cell organelles
- **Row 2**:
  - `card_mutation`: Holographic medical slide card with glowing DAPI DNA specimen preview
  - `card_superweapon`: High-tech cryogenic slide card radiating amber energy borders
  - `badge_atp`: Glowing golden adenosine triphosphate molecule token (energy currency)
  - `badge_antigen`: Iridescent triangular antigen fragment token with receptor notches
- **Row 3**:
  - `meter_ph`: Narrow vertical glass capillary pH gauge filled with color-shifting acidic fluid
  - `meter_temp`: Microscopic thermal sensor displaying glowing red-to-blue gradient
  - `button_pause`: Circular glass lens bezel button with fluorescent cyan pause bars
  - `button_settings`: Precision microscope objective turret gear icon with glowing teeth
- **Row 4**:
  - `status_inflamed`: Inflamed tissue status badge with red fever sparks and cytokine heat
  - `status_buffered`: Alkaline buffer status badge with tranquil blue ionic crystal shield
  - `status_overclock`: High-energy metabolic status badge with surging green electron currents
  - `status_exhausted`: Depleted ATP status badge with dim flickering gray mitochondrial husk

### Generation Prompt
```text
Confocal laser scanning microscope holographic UI HUD icon set sprite sheet of 16 bio-technological interface elements including micrometer focus reticle, danger alert frame, sonar scan ring, octagonal organelle slot, holographic glass slide card, superweapon cryogenic card, golden ATP molecule token, antigen fragment badge, capillary pH meter, thermal sensor, glass lens button, microscope turret gear, inflamed tissue badge, alkaline buffer shield, metabolic surge badge, exhausted mitochondrial badge. Glowing fluorescent cyan, amber gold, and emerald green vector-like luminous lines on dark-field microscope view. Arranged in a perfectly aligned 4x4 uniform grid. Isolated on solid pure black background #000000, luminous edge glow, flat lighting, no cast shadows, NO text, NO words, NO letters, NO labels, NO captions, NO writing, NO watermark, clean isolated game sprites only, 1024x1024 pixel resolution.
```

### Slicing Command
```bash
python3 tools/slice/main.py grid \
  --input gen/ui/ui_sheet_01.png \
  --rows 4 --cols 4 \
  --names "reticle_target,reticle_danger,reticle_scan,frame_organelle,card_mutation,card_superweapon,badge_atp,badge_antigen,meter_ph,meter_temp,button_pause,button_settings,status_inflamed,status_buffered,status_overclock,status_exhausted" \
  --output gen/ui
```
