# Passive Tree & Organelle Prompt Library (Phagocyte)

Master prompts for passive traits, organelles, and epigenetic talent nodes (`assets/data/passive_traits.json`).
Runtime resolution: $128 \times 128$ px circular icon (with circular mask step applied).

---

## Batch 1: Primary Organelles & Cytoskeletal Pathways (16 Items)

### Item Mapping (Row by Row)
- **Row 1**:
  - `passive_actin`: Polymerizing actin microfilaments forming dense meshwork lattice
  - `passive_autophagy`: Double-membrane autophagosome engulfing damaged organelle
  - `passive_bilayer`: Fluid mosaic lipid bilayer with transmembrane protein channels
  - `passive_chemokine`: Chemokine receptor cluster bound to glowing ligand molecules
- **Row 2**:
  - `passive_mitochondria`: Mitochondrion with intricate folded cristae radiating golden ATP energy
  - `passive_ribosome`: Ribosome complex translating mRNA strand into polypeptide chain
  - `passive_endoplasmic`: Rough endoplasmic reticulum sheets studded with glowing ribosomal dots
  - `passive_centrosome`: Pair of perpendicular centrioles projecting dynamic microtubule asters
- **Row 3**:
  - `passive_nucleus`: Dense eukaryotic cell nucleus with coiled chromatin and prominent nucleolus
  - `passive_opsonin`: Complement receptor binding opsonized antigen with high affinity
  - `passive_tcr`: T-cell receptor (TCR) engaging MHC peptide complex with golden spark
  - `passive_nadph`: Membrane-bound NADPH oxidase complex transferring electron cascade
- **Row 4**:
  - `passive_toll`: Toll-like receptor (TLR) horseshoe leucine-rich repeat domain
  - `passive_integrin`: High-affinity integrin anchor clasping endothelial extracellular matrix
  - `passive_selectin`: Selectin carbohydrate-binding domain mediating cellular rolling
  - `passive_calcium`: Endoplasmic reticulum releasing intense wave of green calcium ions

### Generation Prompt
```text
Fluorescence microscopy style sprite sheet of 16 cellular organelles and molecular passive traits including actin filament meshwork, autophagosome vesicle, lipid bilayer mosaic, chemokine receptor cluster, mitochondrion cristae with ATP glow, ribosome translation complex, rough endoplasmic reticulum sheets, centrosome microtubule aster, cell nucleus chromatin, opsonin receptor binding, T-cell receptor MHC engagement, NADPH oxidase electron flow, Toll-like receptor horseshoe, integrin matrix anchor, selectin rolling hook, calcium ion signaling flash. Glowing fluorophores in GFP emerald green, Hoechst blue, and rhodamine red with cryo-EM ultrastructure textures, circular biological medallion motif. Arranged in a perfectly aligned 4x4 uniform grid. Isolated on solid pure black background #000000, dark-field microscope view, luminous edge borders, no cast shadows, NO text, NO words, NO letters, NO labels, NO captions, NO writing, NO watermark, clean isolated game sprites only, 1024x1024 pixel resolution.
```

### Slicing Command
```bash
python3 tools/slice/main.py grid \
  --input gen/passive_tree/passive_tree_sheet_01.png \
  --rows 4 --cols 4 \
  --names "passive_actin,passive_autophagy,passive_bilayer,passive_chemokine,passive_mitochondria,passive_ribosome,passive_endoplasmic,passive_centrosome,passive_nucleus,passive_opsonin,passive_tcr,passive_nadph,passive_toll,passive_integrin,passive_selectin,passive_calcium" \
  --output gen/passive_tree
```
