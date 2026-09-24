# Scientific Misconception Audit Checklist (Phagocyte)

This document catalogs common biological, immunological, and microbiological fallacies found in pop-culture and video games. Project: Phagocyte actively audits against this checklist to ensure that game mechanics do not reinforce false science and, wherever possible, use game systems to intuitively dispel them.

---

## The 15 Biological Misconceptions

### 1. "Antibiotics Cure Viral Infections" (抗生素能杀灭病毒)
* **The Fallacy**: Players believe any antimicrobial weapon or "antibiotic" works against colds, flu, and coronaviruses.
* **The Reality**: Antibiotics specifically target bacterial cell walls (peptidoglycan), bacterial ribosomes (70S), or bacterial DNA gyrase. Viruses lack cell walls, ribosomes, and independent metabolism, rendering antibiotics 100% ineffective.
* **In-Game Audit Requirement**:
  - Antibiotic or enzymatic lytic skills (like Lysozyme) must strictly target bacterial cell wall structures.
  - Viral enemies (e.g. `flu_drift`, `s_virus`, `norovirus`) must be neutralized via envelope rupture, interferon-induced apoptosis, or direct macrophage/CTL phagocytosis, never via cell-wall inhibitors.

---

### 2. "All Bacteria and Microbes are Harmful Germs" (所有微生物都是有害病菌)
* **The Fallacy**: The human body is a sterile fortress and every microscopic organism is an enemy that must be eradicated.
* **The Reality**: The human microbiota comprises tens of trillions of beneficial and commensal microbes essential for digestion, vitamin synthesis, and colonization resistance against pathogens.
* **In-Game Audit Requirement**:
  - The game must include beneficial microbial mechanics.
  - In Phagocyte: `symbiotic_flora` and `quorum_colony` are equippable organelle chamber specimens granting energy and bonuses; `phage_fragment` models bacteriophages that hunt bacteria.

---

### 3. "More Immunity is Always Better / No Downside to Hyperactivation" (免疫力越强越好)
* **The Fallacy**: "Boosting" the immune system indefinitely makes you invincible with zero collateral damage.
* **The Reality**: Hyperactive immunity causes acute respiratory distress, autoimmune destruction, anaphylaxis, cytokine storms, and lethal sepsis / Systemic Inflammatory Response Syndrome (SIRS).
* **In-Game Audit Requirement**:
  - High-intensity weapons and traits must carry genuine biological trade-offs.
  - In Phagocyte: The game features the **SIRS (Systemic Inflammatory Response Syndrome)** end condition; self-sacrificial mechanics (`neutrophil` kamikaze, `redox_symbiont` membrane depolarization) mirror real immunopathology.

---

### 4. "Phagocytosis is Instant Annihilation" (吞噬等于瞬间消灭)
* **The Fallacy**: As soon as a white blood cell touches a microbe, the microbe simply vanishes into thin air.
* **The Reality**: Phagocytosis is a multi-step sequence: chemotaxis $\rightarrow$ pseudopod extension $\rightarrow$ phagosome formation $\rightarrow$ lysosomal fusion (phagolysosome) $\rightarrow$ enzymatic digestion at pH 4.5 $\rightarrow$ antigen presentation. Many virulent bacteria (like *M. tuberculosis*) survive inside phagosomes if acidification fails.
* **In-Game Audit Requirement**:
  - Engulfment must involve sustained digestion or grinding mechanics (`phagolysosome_core`, `acidic_lysosome`), not instantaneous deletion.
  - *M. tuberculosis* (`tb`) has massive health and waxy resistance, demonstrating that engulfing alone is insufficient without high digestive power.

---

### 5. "Antibodies are Standalone Death Lasers" (抗体是独立激光炮)
* **The Fallacy**: Antibodies fire like energy bullets and disintegrate pathogens on contact.
* **The Reality**: Antibodies (immunoglobulins) do not possess intrinsic destructive power. They function by binding specific epitopes to: (1) neutralize toxins/receptors, (2) aggregate pathogens (agglutination), (3) activate complement MAC, or (4) opsonize targets for macrophage recognition.
* **In-Game Audit Requirement**:
  - B-cell projectiles and antibody skills should emphasize tagging, opsonization, aggregation, or guiding secondary effector cascades rather than purely acting like generic kinetic artillery.

---

### 6. "Cells Have Infinite Energy Without Metabolism" (细胞拥有无限能量/无需代谢)
* **The Fallacy**: Cells can fire superweapons and dash continuously without ATP consumption, nutrient recycling, or respiratory limits.
* **The Reality**: Every mechanical action (actin polymerization, kinesin walking, ion pumping) requires ATP hydrolysis ($\text{ATP} \rightarrow \text{ADP} + \text{P}_i$). Energy budget is tightly regulated by oxidative phosphorylation and glycolysis.
* **In-Game Audit Requirement**:
  - Organelle chamber enforces a strict **Base Energy budget (6 Energy)**. Equipping high-impact synthetic or metabolic machinery requires paying energy costs, while generator specimens (`symbiotic_flora`, `phage_fragment`) impose physiological drawbacks.

---

### 7. "Bacteria and Viruses Have the Same Internal Structure" (细菌和病毒结构相同)
* **The Fallacy**: Treating viruses as just "microscopic bacteria" with their own organelles and metabolism.
* **The Reality**: Bacteria are autonomous prokaryotic cells with peptidoglycan walls, plasma membranes, circular DNA, and 70S ribosomes. Viruses are obligate intracellular parasite particles composed solely of a nucleic acid genome (RNA/DNA) inside a protein capsid, sometimes wrapped in a lipid envelope stolen from a host cell.
* **In-Game Audit Requirement**:
  - In `pathogens.json` and `translations.csv`, descriptions must strictly distinguish prokaryotic morphology (flagella, bacilli, cocci) from viral architecture (spike proteins, icosahedral capsids, RNA segments).

---

### 8. "Inflammation is Pure Disease That Must Be Stopped" (发炎是纯粹有害的疾病)
* **The Fallacy**: Any inflammation (redness, heat, swelling, pain) is a medical malfunction with no defensive value.
* **The Reality**: Acute inflammation is an essential, highly coordinated physiological defense that increases vascular permeability, recruits neutrophils, and restricts pathogen dissemination.
* **In-Game Audit Requirement**:
  - Battlefield environments (e.g. `inflamed_tissue`) provide tactical benefits (chemokine currents, dilated vessels) alongside environmental hazards.

---

### 9. "All Microbes Die at the Same Temperature / Acidity" (所有微生物耐受力相同)
* **The Fallacy**: A single environmental hazard (acid, heat) destroys all pathogens equally.
* **The Reality**: *Helicobacter pylori* thrives in stomach acid (pH 1.5–2.0) by secreting urease; thermophilic and spore-forming bacteria survive extreme conditions; enveloped viruses are vulnerable to detergents while non-enveloped viruses endure harsh environments.
* **In-Game Audit Requirement**:
  - Pathogens possess differential environmental and ailment resistances based on their biology (e.g. *H. pylori* acid resistance, spore dormancy in *Anthrax*).

---

### 10. "Cancer is an External Invader Like a Parasite" (肿瘤细胞是外来寄生虫)
* **The Fallacy**: Cancer cells are foreign microbes that entered the body from outside.
* **The Reality**: Cancer cells are mutated host cells (self-cells) that suffered oncogenic DNA mutations, evaded cell cycle checkpoints, and down-regulated MHC-I surface markers to escape natural killer (NK) and cytotoxic T-cell detection.
* **In-Game Audit Requirement**:
  - `malignant_cell` in `pathogens.json` is explicitly defined as a mutated host cell; its in-game trait `Immune Escape` reflects real down-regulation of MHC-I presentation, forcing close-range macrophage breakdown rather than automated receptor targeting.

---

### 11. "Cells are Empty Water Balloons with Floating Dots" (细胞是空心水球)
* **The Fallacy**: The cell cytoplasm is an empty watery fluid where organelles float around freely.
* **The Reality**: The cytoplasm is a dense, gel-like colloidal lattice packed with actin microfilaments, intermediate filaments, and microtubules, experiencing extreme macromolecular crowding.
* **In-Game Audit Requirement**:
  - Visuals feature dynamic colloidal particles, Brownian motion granules, and tensegrity cytoskeleton lines; `actin_mesh` and `microtubule_anchor` provide tangible mechanical elasticity.

---

### 12. "Mutations are Always Deleterious and Fatal" (基因突变全是有害的)
* **The Fallacy**: Any mutation results in immediate deformities or degeneration.
* **The Reality**: Mutations are the raw engine of evolutionary adaptation. In the immune system, **somatic hypermutation (SHM)** in germinal center B-cells intentionally mutates immunoglobulin genes to drastically increase antibody affinity (affinity maturation).
* **In-Game Audit Requirement**:
  - The in-game talent tree explicitly frames genetic mutations and epigenetic modifications as adaptive optimization pathways.

---

### 13. "Host Fever is an Enemy Attack" (发热是病原体发动的攻击)
* **The Fallacy**: Fever is caused directly by pathogens to harm the patient.
* **The Reality**: Endogenous pyrogens (IL-1, IL-6, TNF-$\alpha$) released by macrophages act on the hypothalamus to raise body temperature, inhibiting bacterial replication rates and accelerating immune cell motility and enzymatic kinetics.
* **In-Game Audit Requirement**:
  - In-game pyrogenic or heat mechanics should empower or accelerate cellular action kinetics rather than act solely as damage debuffs.

---

### 14. "White Blood Cells Patrol in Straight Open Highways" (微血管是畅通无阻的空旷公路)
* **The Fallacy**: Immune cells swim freely through blood vessels like fish in open water.
* **The Reality**: In capillaries, diameter is often smaller than the white blood cell itself (7–15 $\mu$m). Cells must deform heavily, experience immense fluid shear stress, and roll along selectins/integrins on the endothelial wall to extravasate.
* **In-Game Audit Requirement**:
  - Capillary and sinusoid battlefields feature fluid drag, hemodynamic shear, and wall collisions; cell deformation mechanics simulate amoeboid squeezing.

---

### 15. "T-Cells and B-Cells Work Completely Independently" (T细胞和B细胞完全互不相干)
* **The Fallacy**: Humoral immunity (antibodies) and cellular immunity (killer cells) operate in separate silos with zero crosstalk.
* **The Reality**: B-cell antibody class switching and affinity maturation require T-follicular helper ($T_{FH}$) CD40L-CD40 costimulation and cytokine release (IL-4, IL-21).
* **In-Game Audit Requirement**:
  - Shared epigenetic passive talent tree allows hybrid builds, encouraging players to combine lymphocyte receptor cascades.
