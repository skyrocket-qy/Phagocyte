# Passive Tree & Organelle Prompt Library (Phagocyte)

Master prompts for passive traits, organelles, and epigenetic talent nodes (`assets/data/passive_traits.json`).
Runtime resolution: $128 \times 128$ px circular icon (with circular mask step applied).

---

## Batch 1: Primary Organelles & Cytoskeletal Pathways (16 Items)

### Item Mapping (Row by Row)
- **Row 1**:
  - `trait_passive_actin`: Polymerizing actin microfilaments forming dense meshwork lattice
  - `trait_passive_autophagy`: Double-membrane autophagosome engulfing damaged organelle
  - `trait_passive_bilayer`: Fluid mosaic lipid bilayer with transmembrane protein channels
  - `trait_passive_chemokine`: Chemokine receptor cluster bound to glowing ligand molecules
- **Row 2**:
  - `trait_passive_mitochondria`: Mitochondrion with intricate folded cristae radiating golden ATP energy
  - `trait_passive_ribosome`: Ribosome complex translating mRNA strand into polypeptide chain
  - `trait_passive_endoplasmic`: Rough endoplasmic reticulum sheets studded with glowing ribosomal dots
  - `trait_passive_centrosome`: Pair of perpendicular centrioles projecting dynamic microtubule asters
- **Row 3**:
  - `trait_passive_nucleus`: Dense eukaryotic cell nucleus with coiled chromatin and prominent nucleolus
  - `trait_passive_opsonin`: Complement receptor binding opsonized antigen with high affinity
  - `trait_passive_tcr`: T-cell receptor (TCR) engaging MHC peptide complex with golden spark
  - `trait_passive_nadph`: Membrane-bound NADPH oxidase complex transferring electron cascade
- **Row 4**:
  - `trait_passive_toll`: Toll-like receptor (TLR) horseshoe leucine-rich repeat domain
  - `trait_passive_integrin`: High-affinity integrin anchor clasping endothelial extracellular matrix
  - `trait_passive_selectin`: Selectin carbohydrate-binding domain mediating cellular rolling
  - `trait_passive_calcium`: Endoplasmic reticulum releasing intense wave of green calcium ions

### Generation Prompt
```text
Fluorescence microscopy style sprite sheet of 16 cellular organelles and molecular passive traits including actin filament meshwork, autophagosome vesicle, lipid bilayer mosaic, chemokine receptor cluster, mitochondrion cristae with ATP glow, ribosome translation complex, rough endoplasmic reticulum sheets, centrosome microtubule aster, cell nucleus chromatin, opsonin receptor binding, T-cell receptor MHC engagement, NADPH oxidase electron flow, Toll-like receptor horseshoe, integrin matrix anchor, selectin rolling hook, calcium ion signaling flash. Glowing fluorophores in GFP emerald green, Hoechst blue, and rhodamine red with cryo-EM ultrastructure textures, circular biological medallion motif. Arranged in a perfectly aligned 4x4 uniform grid. Isolated on solid pure black background #000000, dark-field microscope view, luminous edge borders, no cast shadows, NO text, NO words, NO letters, NO labels, NO captions, NO writing, NO watermark, clean isolated game sprites only, 1024x1024 pixel resolution.
```

### Slicing Command
```bash
python3 tools/slice/main.py grid \
  --input gen/passive_tree/passive_tree_sheet_01.png \
  --rows 4 --cols 4 \
  --names "trait_passive_actin,trait_passive_autophagy,trait_passive_bilayer,trait_passive_chemokine,trait_passive_mitochondria,trait_passive_ribosome,trait_passive_endoplasmic,trait_passive_centrosome,trait_passive_nucleus,trait_passive_opsonin,trait_passive_tcr,trait_passive_nadph,trait_passive_toll,trait_passive_integrin,trait_passive_selectin,trait_passive_calcium" \
  --output gen/passive_tree
```
