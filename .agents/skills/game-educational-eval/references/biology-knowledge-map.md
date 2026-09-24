# Phagocyte Biological Knowledge Map (SSOT)

This document is the Single Source of Truth (SSOT) mapping real-world immunology, cytology, and clinical microbiology concepts to the actual data structures, scenes, and mechanics of Project: Phagocyte.

---

## 1. Immune Defense Cells (Classes)

| Real Biological Prototype | Anatomical & Physiological Function | In-Game Class ID | In-Game Mechanics & Biological Metaphor |
|:---|:---|:---|:---|
| **Macrophage (巨噬细胞)** | Long-lived, tissue-resident phagocyte. Engulfs cellular debris and microbes via wide pseudopods; initiates inflammation. | `macrophage` | High base HP (140), heavy armor (10), base block (8%). Large dynamic pseudopod vertex radius; engulfs and grinds down pathogens in close quarters. |
| **Cytotoxic T Lymphocyte / CD8+ CTL (杀手 T 细胞)** | Adaptive immune assassin. Recognizes foreign peptide-MHC-I complexes and induces apoptosis via perforin/granzymes. | `ctl` | High base speed (260), high crit chance (+15%), low armor (0). Rapid linear strikes and precision perforation; vulnerable to sustained attrition. |
| **Neutrophil (中性粒细胞)** | Short-lived frontline suicide responder. Rapidly extravasates, deploys azurophilic granules, and commits NETosis. | `neutrophil` | High damage might (+20%), moderate speed (230), explosive death/granule sacrifice mechanics. Kamikaze demolition unit designed for sacrificial wipes. |
| **B Lymphocyte / Plasma Cell (B 淋巴细胞/浆细胞)** | Humoral immunity producer. Secretes antigen-specific immunoglobulins (antibodies) to neutralize pathogens from distance. | `b_cell` | High projectile speed (+30%), ranged homing antibody volleys, low contact resilience (0 armor, 95 HP). Pure artillery archetype. |
| **Dendritic Cell (树突状细胞)** | Professional antigen-presenting cell (APC). Samples peripheral tissues with extensive arborized dendrites; migrates to lymph nodes. | `dendritic` | Vast magnet pickup radius (260), high agility, tracer targeting. Gathers antigen resources and directs defensive volleys without direct melee engagement. |

---

## 2. Cellular Organelles & Subcellular Machinery

| Organelle / Specimen | Real Biological Function | Category | In-Game ID | In-Game Game Mechanics |
|:---|:---|:---|:---|:---|
| **Mitochondria Mk II** | Oxidative phosphorylation powerhouse; generates ATP across inner cristae folds. | Metabolism | `mitochondria_mkii` | Cooldown Reduction +16%, Skill Duration +10% (Energy cost: 4). |
| **Glycolytic Bypass** | Anaerobic glucose metabolism; rapid cytoplasmic ATP generation without oxygen. | Metabolism | `glycolytic_bypass` | Cooldown Reduction +5%, Move Speed +3% (Energy cost: 1). |
| **ATP Shuttle** | Adenine nucleotide translocase (ANT); exports mitochondrial ATP into cytosol. | Metabolism | `atp_shuttle` | Move Speed +6%, Skill Duration +6% (Energy cost: 2). |
| **Krebs Cycle** | Tricarboxylic acid cycle; central metabolic hub extracting NADH from acetyl-CoA. | Metabolism | `krebs_cycle` | Cooldown Reduction +10%, Might +10% (Energy cost: 4). |
| **Acidified Lysosome** | V-ATPase proton pump drops pH to 4.5, activating hydrolytic cathepsins/lipases. | Digestion | `acidic_lysosome` | Might +12%, Ailment Damage +15% (Energy cost: 3). |
| **Proteasome Sieve** | Multicatalytic protease complex recycling ubiquitin-tagged damaged proteins. | Digestion | `proteasome_sieve` | Ailment Damage +8%, Health Regen +0.3 (Energy cost: 1). |
| **Bile Salt Pool** | Amphipathic sterol detergents emulsifying lipid bilayers into micelles. | Digestion | `bile_salt_pool` | Ailment Damage +10%, Health Regen +0.4 (Energy cost: 2). |
| **Phagolysosome Core** | Final digestive reactor fusing phagosome and lysosome to destroy engulfed prey. | Digestion | `phagolysosome_core` | Might +15%, Life Steal +5% (Energy cost: 4). |
| **Flagellar Base** | Bacterial-derived basal body rotary motor converting proton motive force to torque. | Cytoskeleton | `flagellar_base` | Move Speed +12%, Knockback +20% (Energy cost: 3). |
| **Microtubule Anchor** | Tubulin polymer tracks serving as railways for kinesin/dynein cargo motors. | Cytoskeleton | `microtubule_anchor` | Move Speed +4%, Area +4% (Energy cost: 1). |
| **Actin Mesh** | Branched Arp2/3 filamentous cortex providing membrane tension and protrusion. | Cytoskeleton | `actin_mesh` | Skill Area +8%, Evasion +2% (Energy cost: 2). |
| **Centrosome Array** | Primary microtubule organizing center (MTOC) orienting spindle asters. | Cytoskeleton | `centrosome_array` | Knockback +30%, Pierce +1 (Energy cost: 4). |
| **Rough ER** | Ribosome-studded membranes synthesizing secretory and transmembrane proteins. | Synthesis | `rough_er` | Projectile Amount +1, Projectile Speed +8% (Energy cost: 4). |
| **Ribosome Cluster** | Polysome complexes translating mRNA transcripts into functional polypeptide chains. | Synthesis | `ribosome_cluster` | Projectile Speed +8%, Skill Duration +8% (Energy cost: 2). |
| **Golgi Stack** | Polarized cisternae sorting, glycosylating, and shipping vesicular cargo. | Synthesis | `golgi_stack` | Projectile Speed +12%, Skill Area +5% (Energy cost: 2). |
| **Nucleolus Prime** | Subnuclear compartment assembling ribosomal RNA subunits around the clock. | Synthesis | `nucleolus_prime` | Projectile Amount +1, Crit Damage +15% (Energy cost: 4). |
| **Ion Channel Array** | Gated transmembrane pore lattice regulating membrane potential ($\text{Na}^+/\text{K}^+/\text{Ca}^{2+}$). | Sensing | `ion_channel_array` | Armor +3, Block +4%, Magnet +15% (Energy cost: 3). |
| **Chemokine Patch** | G-protein coupled receptor clusters detecting chemokines along chemotactic gradients. | Sensing | `chemokine_patch` | Magnet +20%, Evasion +2% (Energy cost: 1). |
| **Toll Receptor** | Leucine-rich repeat (LRR) PRR detecting pathogen-associated molecular patterns (PAMPs). | Sensing | `toll_receptor` | Block +3%, Evasion +2% (Energy cost: 2). |
| **Membrane Raft** | Cholesterol and sphingolipid-rich microdomains clustering signaling receptors. | Sensing | `membrane_raft` | Armor +5, Block +3% (Energy cost: 4). |
| **Symbiotic Flora** | Mutualistic gut/skin microbiota providing colonization resistance at metabolic cost. | Symbiosis | `symbiotic_flora` | Energy +1. Drawback: Move Speed -30%, Might -15%. |
| **Phage Fragment** | Bacteriophage capsid fragments hijacking bacterial machinery for energy. | Symbiosis | `phage_fragment` | Energy +1, CDR +5%. Drawback: Max Health -20%. |
| **Redox Symbiont** | Proto-mitochondrial endosymbiont donating high-energy electrons with oxidative stress. | Symbiosis | `redox_symbiont` | Energy +1, Might +10%. Drawback: Armor -3, Evasion -3%. |
| **Quorum Colony** | Autoinducer-linked bacterial biofilm community sharing nutrients. | Symbiosis | `quorum_colony` | Energy +1, Magnet +25%, Regen +0.5. Drawback: Speed -15%, HP -10%. |

---

## 3. Immunological Weapons & Active Skills

| Active Weapon / Skill | Biochemical Mechanism | In-Game Realization | Pedagogical Learning Takeaway |
|:---|:---|:---|:---|
| **Respiratory Burst (活性氧射流 / ROS)** | NADPH oxidase generates superoxide ($\text{O}_2^{\bullet-}$) and $\text{H}_2\text{O}_2$ causing oxidative lipid peroxidation. | Linear directional oxidative jet piercing enemy ranks, inflicting continuous chemical corrosion DoT. | Explains how phagocytes use reactive oxygen radicals to chemically sterilize engulfed bacteria. |
| **Perforin Lance (穿孔素长矛)** | Monomers polymerize into cylindrical transmembrane pores (13–20 nm) in target lipid bilayers. | High-velocity helical beam piercing straight through priority targets with membrane breach debuff. | Teaches that killer cells physically punch open infected host cells to trigger apoptosis. |
| **Complement MAC (补体复合物 C5b-9)** | Cascade activation forms membrane attack complex (MAC), allowing water influx and osmotic lysis. | Target-locking orbital pore drill inducing target swelling, osmotic destabilization, and pop rupture. | Shows the classical/alternative complement cascade resulting in osmotic bacterial destruction. |
| **NETosis Trap (中性粒细胞诱捕网)** | Neutrophil extrudes decondensed chromatin decorated with histones, elastase, and myeloperoxidase. | Expanding sticky web anchoring enemies in place, applying persistent damage and slow. | Illustrates the remarkable suicide trap strategy of dying neutrophils in severe infections. |
| **Lysozyme Shell (溶酶体裂解环)** | Peptidoglycan $N$-acetylmuramic acid-$\beta(1,4)$-$N$-acetylglucosamine glycoside hydrolase. | Radial burst around cell destroying close-range bacterial walls and clearing space. | Demonstrates enzymatic digestion of Gram-positive bacterial cell walls. |
| **Opsonin Tagging (抗原調理素)** | IgG and C3b coat pathogen surfaces to serve as recognizable flags for phagocytic receptors. | High-affinity glowing marker applied to enemies, granting 100% critical hit and bonus EXP. | Explains how the immune system "seasons" pathogens to make them tasty and visible to phagocytes. |

---

## 4. Pathogen Taxonomy & Virulence Ecology

| Pathogen Name | Taxonomic Classification | Virulence Factor / Clinical Trait | In-Game Behavior & AI Implementation |
|:---|:---|:---|:---|
| **Staphylococcus aureus** | Gram-positive coccus | Coagulase converts fibrinogen to fibrin, forming protective physical clumping shield. | Flocking swarm behavior (`staph`); clustered aggregates acting as shared damage sponges. |
| **Pseudomonas aeruginosa** | Gram-negative bacillus | Alginate extracellular polysaccharide biofilm conferring multi-drug resistance. | Persistent sticky biofilm trail (`pseudomonas`), slowing player movement by 25–50% and shielding bacteria. |
| **Escherichia coli** | Gram-negative flagellated bacillus | Peritrichous flagellar rotation producing alternating "run and tumble" motility. | Chemotactic straight-line charge (`e_coli`); telegraphs brief windup before high-speed ramming. |
| **Mycobacterium tuberculosis** | Acid-fast bacillus | Thick mycolic acid waxy cell wall preventing lysosomal acidification and macrophage destruction. | Heavy slow tank (`tb`) with extreme knockback resistance (90%) and massive HP pool (120). |
| **Influenza A Virus** | Enveloped segmented (-)ssRNA | Antigenic drift via frequent point mutations in Hemagglutinin (HA) and Neuraminidase (NA). | Antigen mutation shift (`flu_drift`); periodically flashes to reset player critical lock-on bonus. |
| **Coronaviral Spike Virion** | Enveloped (+)ssRNA | Trimeric S-glycoproteins bind ACE2 surface receptors with high affinity. | Spike adhesion (`s_virus`); collision inflicts a 35% movement slow, mimicking receptor tethering. |
| **Malignant Mutated Cell** | Eukaryotic neoplastic self-cell | Down-regulation of surface MHC-I expression to evade cytotoxic CD8+ T-cell detection. | Immune escape (`malignant_cell`); immune to ranged automated targeting; requires direct phagocytosis. |
| **Candida albicans** | Dimorphic fungal yeast | Yeast-to-hyphae morphological switching penetrating endothelial tissue layers. | Spore budding into invasive filaments with high knockback resistance. |
