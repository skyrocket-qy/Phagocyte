# Achievement Prompt Library (Phagocyte)

All 19 achievements defined in `assets/data/achievements.json`.
Master resolution: $256 \times 256$ px (derived from $1024 \times 1024$ 4×4 sheets).
Output: Produces `<id>.png` and `<id>_unachieved.png` in `assets/gen/achievement/`.

---

## Batch 1: Core Milestones & Normal Map Clears (16 Items)

### Item Mapping (Row by Row)
- **Row 1**:
  - `first_digestion`: Green glowing phagosome enveloping a small bacterium
  - `engulf_20`: Mass phagocytosis vortex pulling multiple glowing bacterial rods
  - `devour_50`: Overloaded macrophage bursting with fluorescent cytoplasmic enzymes
  - `reach_level_5`: DNA double helix splitting and radiating bioluminescent mutation energy
- **Row 2**:
  - `survive_180s`: White blood cell anchored firmly inside microcapillary flow
  - `giant_volume`: Massive hypertrophic phagocyte with thick pseudopods dominating the frame
  - `full_arsenal`: 5 distinct molecular weapons orbiting a glowing cell nucleus
  - `first_evolution`: Two glowing biomolecules fusing into a blazing epigenetic superweapon
- **Row 3**:
  - `prion_cleared`: Misfolded beta-sheet prion protein being fractured and dissolved by enzymes
  - `wound_clear`: Skin epidermal layer regenerating with fibrin mesh settling into peace
  - `alveolar_clear`: Deep cyan pulmonary alveolus open and clean with pristine gas exchange
  - `hepatic_clear`: Hepatic sinusoid blood vessel with Kupffer cell clearing toxins
- **Row 4**:
  - `gastric_clear`: Gastric mucosa layer protected by thick glowing mucus shield from gastric acid
  - `bbb_clear`: Astrocyte end-feet tightly sealing blood-brain barrier capillaries
  - `skip`: Skip cell
  - `skip`: Skip cell

### Generation Prompt
```text
Confocal laser scanning microscopy style sprite sheet of 16 biological immunology achievement badges including phagosome vesicle digestion, mass engulfment vortex, overloaded macrophage burst, DNA mutation ascension, microcapillary anchor, hypertrophic giant phagocyte, five molecular weapon orbit, superweapon epigenetic fusion, fractured prion dissolution, skin wound fibrin resolution, pristine lung alveolus, liver sinusoid filtration, gastric mucus shield, blood-brain barrier tight junction seal. Glowing fluorophores in vibrant GFP emerald, RFP crimson, DAPI electric blue, and Hoechst amber with cryo-EM ultrastructure textures, circular biological medal motif. Arranged in a perfectly aligned 4x4 uniform grid. Isolated on solid pure black background #000000, dark-field microscope view, luminous edge borders, no cast shadows, NO text, NO words, NO letters, NO labels, NO captions, NO writing, NO watermark, clean isolated game sprites only, 1024x1024 pixel resolution.
```

### Slicing Command
```bash
python3 tools/slice/main.py grid \
  --input gen/achievement/achievement_sheet_01.png \
  --rows 4 --cols 4 \
  --names "first_digestion,engulf_20,devour_50,reach_level_5,survive_180s,giant_volume,full_arsenal,first_evolution,prion_cleared,wound_clear,alveolar_clear,hepatic_clear,gastric_clear,bbb_clear,skip,skip" \
  --output gen/achievement
```

---

## Batch 2: Hard Difficulty Clears (5 Items)

### Item Mapping
- `wound_hard_clear`: Crimson inflamed wound erupting with purulent exudate pacified
- `alveolar_hard_clear`: Choking toxic alveolar fog dispersed by surfactant waves
- `hepatic_hard_clear`: Fibrotic liver cirrhosis tissue restored to healthy cellular matrix
- `gastric_hard_clear`: Caustic gastric acid abyss neutralized by heavy bicarbonate buffer
- `bbb_hard_clear`: Severe encephalitis storm in brain tissue subdued by T-cell surveillance
