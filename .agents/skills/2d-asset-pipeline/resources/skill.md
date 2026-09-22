# Skill & Epigenetic Superweapon Prompt Library (Phagocyte)

Master prompts for innate skills, active weapons, and epigenetic superweapons (`assets/data/skills.json`).
Runtime resolution: $128 \times 128$ px (sliced from 256x256 in `gen/skill/` to `assets/gen/skill/`).

---

## Batch 1: Primary Weapons & Active Biochemicals (16 Items)

### Item Mapping (Row by Row)
- **Row 1**:
  - `phagocytic_grasp`: Thick glowing pseudopod reaching forward like an amoebic claw
  - `lysosomal_overload`: Acidic lysosome vesicle exploding with toxic hydrolytic enzymes
  - `ros_torrent`: High-pressure conical burst of reactive oxygen species and peroxide bubbles
  - `perforin_spike`: High-velocity helical protein projectile piercing membrane barriers
- **Row 2**:
  - `netosis_trap`: Fluorescent web of decondensed chromatin and antimicrobial peptides
  - `membrane_shear`: High-frequency undulating lipid bilayer wave slicing fluid particles
  - `chemokine_flare`: Radiant cytokine signal beacon emitting concentric attraction waves
  - `antibody_salvo`: Swarm of Y-shaped immunoglobulin G proteins launching in fan formation
- **Row 3**:
  - `complement_cascade`: Ring of complement proteins assembling into a membrane attack complex (MAC)
  - `granule_burst`: Degranulation shockwave releasing histamine and antimicrobial enzymes
  - `superoxide_cloud`: Dense violet cloud of superoxide free radicals dissolving pathogen cell walls
  - `interferon_pulse`: Concentric golden shield pulse repelling viral capsid penetration
- **Row 4**:
  - `nitric_oxide_blast`: Blue fluorescent gas expansion of toxic reactive nitrogen species
  - `catalase_surge`: Catalase enzyme cluster neutralizing toxins into bubbling water and oxygen
  - `defensin_dart`: Needle-like antimicrobial defensin peptides flying forward
  - `endosome_fuse`: Early endosome merging with lysosome in bright catalytic flash

### Generation Prompt
```text
Confocal fluorescent microscopy style sprite sheet of 16 white blood cell biochemical weapons and skills including amoebic pseudopod claw, lysosome acidic enzyme burst, reactive oxygen peroxide spray, helical perforin spear, chromatin net trap, membrane shear wave, cytokine signal pulse, Y-shaped antibody swarm, membrane attack complex ring, degranulation shockwave, superoxide radical cloud, interferon shield pulse, nitric oxide blast, catalase enzyme surge, defensin peptide needles, endosome fusion flash. Glowing fluorophores in vibrant GFP emerald green, RFP crimson red, and DAPI electric cyan blue on cryo-EM ultrastructure textures. Arranged in a perfectly aligned 4x4 uniform grid. Isolated on solid pure black background #000000, dark-field microscope view, luminous edge borders, no cast shadows, NO text, NO words, NO letters, NO labels, NO captions, NO writing, NO watermark, clean isolated game sprites only, 1024x1024 pixel resolution.
```

### Slicing Command
```bash
python3 tools/slice/main.py grid \
  --input gen/skill/skill_sheet_01.png \
  --rows 4 --cols 4 \
  --names "phagocytic_grasp,lysosomal_overload,ros_torrent,perforin_spike,netosis_trap,membrane_shear,chemokine_flare,antibody_salvo,complement_cascade,granule_burst,superoxide_cloud,interferon_pulse,nitric_oxide_blast,catalase_surge,defensin_dart,endosome_fuse" \
  --output gen/skill
```

---

## Batch 2: The 5 Epigenetic Superweapons (二合一終極超武)

### Item Mapping
- `superweapon_amoeba_maw`: 【阿米巴原生巨口】Colossal living pseudopod gaping maw devouring elite pathogens whole
- `superweapon_perforin_drill`: 【質膜裂解螺旋鑽】Massive rotating multi-helical perforin drill puncturing through heavy armor
- `superweapon_peroxide_leviathan`: 【過氧化利維坦】Continuous flamethrower-like stream of boiling hydrogen peroxide and acidic mist
- `superweapon_chromatin_web`: 【染色質天羅地網】Screen-wide crystalline DNA/histone meshwork anchoring and detonating pathogens
- `superweapon_catalytic_nova`: 【催化溶解核爆】Miniature cellular supernova of enzymes dissolving everything in immediate radius
