# Phagocyte Educational & Scientific Fidelity Rubric

This rubric establishes a standardized, 5-level evaluation framework to assess Project: Phagocyte's educational efficacy and real-world biological fidelity.

The goal is grounded in `docs/real.md`: **"玩遊戲即理解免疫學 (Play the game to understand immunology)"** — ensuring the game teaches authentic human biology through intuitive gameplay mechanics without devolving into dry, didactic lectures.

---

## The 5 Core Evaluation Dimensions

Each dimension is scored on a 1 to 5 scale (converted to a 100-point normalized index):

```text
Level 1 (Poor / 20 pts)       : Fantasy tropes masquerading as biology; confusing or misleading.
Level 2 (Rudimentary / 40 pts): Surface-level buzzwords; mechanics contradict underlying science.
Level 3 (Adequate / 60 pts)   : Plausible biology; good scientific lore but isolated from mechanics.
Level 4 (Strong / 80 pts)     : Mechanics faithfully mirror biological pathways; rich codex.
Level 5 (Exceptional / 100 pts): Textbook-grade interactive simulation disguised as an adrenaline roguelite.
```

---

### Dimension 1: Mechanistic Scientific Fidelity (机制科学保真度)
*Focus: How accurately do in-game skills, organelles, and cell abilities reflect genuine biochemical, biophysical, and immunological mechanisms?*

| Level | Criteria | Example / Benchmark |
|:---:|:---|:---|
| **Level 1** | **Pure Magic Skin**: Abilities use fantasy mechanics (fireballs, magic shields) with thin biological names slapped on top. Physics and energy conservation are ignored. | "Mitochondria blast": Shoots purple laser beams that stun enemies with no relation to cellular respiration or ATP. |
| **Level 2** | **Superficial Association**: Mechanics loosely relate to terms but misrepresent biological scale or mechanism. Organelle stats contradict real physiological function. | Mitochondria increases armor instead of energy/speed; Lysosome provides movement speed instead of hydrolytic digestion. |
| **Level 3** | **Biochemically Plausible**: Mechanics correspond to broad functional roles (e.g., lysosomes digest, mitochondria provide energy, actin moves). Some cellular processes are oversimplified. | NADPH Oxidase shoots reactive oxygen species (ROS) in a line to damage pathogens; simple damage scaling. |
| **Level 4** | **Pathway-Accurate Transduction**: Mechanics translate multi-step biochemical cascades into game systems. Opportunity costs reflect real cellular constraints (ATP limits, membrane tension, pH thresholds). | V-ATPase proton pump maintains acidic pH 4.5 in lysosomes, enabling cathepsins and nucleases to hydrolyze engulfed prey into recycled monomers; NETosis requires cell sacrifice to eject sticky chromatin mesh. |
| **Level 5** | **Biophysical & Enzymatic Unity**: Every parameter (reaction rates, diffusion, steric hindrance, surface-to-volume ratio) has a biological basis. Emergent gameplay matches medical literature. | Dynamic actin-myosin vertex noise deformation accurately models amoeboid pseudopod engulfment; Arp2/3 branching stiffens the cortex; ANT translocase ferries ATP with realistic kinetic saturation. |

---

### Dimension 2: Pathogen-Host Interaction & Virulence Accuracy (病原宿主交互与毒力机制)
*Focus: How accurately do pathogens (bacteria, viruses, fungi, parasites, malignant cells) behave based on their real clinical microbiology, virulence factors, and immune evasion strategies?*

| Level | Criteria | Example / Benchmark |
|:---:|:---|:---|
| **Level 1** | **Generic Zombie Swarms**: All enemies are palette swaps with uniform collision and melee rush behavior. No biological differentiation. | Bacteria and viruses have identical movement, hitboxes, and attack types. |
| **Level 2** | **Superficial Taxonomy**: Basic division between small/fast and big/slow, but virulence traits are arbitrary video game tropes. | *Staphylococcus* breathes fire; *Influenza* shoots ice shards. |
| **Level 3** | **Clinically Recognizable**: Pathogens feature distinct motility and recognizable morphology (flagella, capsids, biofilm). Basic host-pathogen interactions are present. | *E. coli* uses flagellar bursts; *P. aeruginosa* leaves sticky biofilm trails; *S. aureus* forms clusters. |
| **Level 4** | **Virulence Factor Modeling**: Pathogens deploy authentic immune evasion mechanisms documented in microbiology (coagulase fibrin shields, antigenic drift, encapsulation, toxic shock pulses, cord-factor resistance). | *Influenza* periodically triggers antigenic drift to reset targeted critical lock-on; *M. tuberculosis* has thick mycolic acid waxy wall conferring massive damage reduction; Malignant cells down-regulate MHC-I to evade CTL targeting. |
| **Level 5** | **Microbial Ecology & Clinical Realism**: Host-pathogen arms race feels like a living battlefield. Pathogen morphology, envelope type, Gram status, and receptor targets dictate combat dynamics. | Gram-positive thick peptidoglycan resists osmotic shock but falls to lysosomal enzymes; enveloped viruses adhere to specific membrane receptors (ACE2/sialic acid); quorum sensing triggers coordinated virulence expression. |

---

### Dimension 3: Scientific Nomenclature & Rigor (科学术语与多语言规范性)
*Focus: Precision, consistency, and academic validity of scientific naming across all items, descriptions, and language translations.*

| Level | Criteria | Example / Benchmark |
|:---:|:---|:---|
| **Level 1** | **Pseudo-Science & Typo-Ridden**: Misspelled Latin names, invented pseudo-medical jargon, severe translation errors. | "Stafilococus", "ATP-Laser", mistranslating "Phagocytosis" as "Eating". |
| **Level 2** | **Inconsistent Terminology**: Mix of colloquial slang and medical terms. Latin binomials lack proper capitalization or italics. Language translations lose scientific meaning. | "Staphylococcus Aureus" (improper species capitalization), "white cell gun", Chinese translations using machine garble. |
| **Level 3** | **Standard Nomenclature**: Standard medical and biological terms used consistently. Pathogens use standard binomials (*Genus species*). | *Staphylococcus aureus*, *Pseudomonas aeruginosa*, Macrophage, Cytotoxic T Lymphocyte. |
| **Level 4** | **Peer-Reviewed Precision**: Every skill, organelle, and trait features accurate biochemical names (e.g. Adenine Nucleotide Translocase, Arp2/3 complex, C5b-9 MAC, Peptidoglycan, Mycolic acid). Multilingual parity across all 8 supported languages (`en`, `zh_CN`, `zh_TW`, `ja`, `de`, `fr`, `ru`, `es`). | Both casual descriptions (`_DESC`) and deep biochemical mechanisms (`_BIO`) maintain exact international medical taxonomy (IUPAC/IUBMB standards). |
| **Level 5** | **Etymological & Pedagogical Mastery**: Terms are not only accurate but framed to convey Greek/Latin roots (e.g. *Phago-* = eat, *-cyte* = cell; *Macro-* = large; *Lysis* = dissolve), enhancing vocabulary acquisition for learners. | Rich glossaries linking scientific terminology to historical discoveries (Metchnikoff phagocytosis, Ehrlich side-chain theory, Fleming lysozyme). |

---

### Dimension 4: Incidental Learning & Cognitive Ergonomics (隐性学习与认知脚手架)
*Focus: Does the game deliver "stealth learning" where players naturally internalize biology through play, without cognitive overload or text walls?*

| Level | Criteria | Example / Benchmark |
|:---:|:---|:---|
| **Level 1** | **Didactic Text Wall or Zero Learning**: Either forces players to read dry textbook paragraphs before playing, or conveys zero educational value. | Gameplay pauses for mandatory reading quizzes; or player learns nothing beyond pressing buttons. |
| **Level 2** | **Opaque & Frustrating**: High jargon density with zero cognitive scaffolding. Beginners feel alienated; mechanics feel unintuitive. | Players must memorize complex acronyms (TLR4, MyD88, NF-kB) without visual or tactile explanation. |
| **Level 3** | **Accessible Tooltips**: Clear 1-sentence tactical tooltips with separate biological lore tabs for players who want to learn more. | "Rapid energy courier: Move Speed +6%" in main tooltip, with a secondary bio note explaining translocase kinetics. |
| **Level 4** | **Tactile Metaphor & Dual-Audience Scaffolding**: Mechanics act as intuitive physical metaphors. Medical students intuitively optimize builds; non-biologists learn through high-feedback gameplay loops. | Opsonin makes targets glow yellow and triggers 100% crit feedback, teaching that opsonization tags prey for destruction; Codex modal provides interactive, searchable microscopic specimen cards. |
| **Level 5** | **Intrinsic Epistemic Flow**: The gameplay loop *is* the learning loop. Surviving challenging waves requires applying real immunological strategies. Post-run analytics present clinical "discharge summaries" connecting player tactics to medical outcomes. | A player who has never studied biology can pass a university-level introductory immunology exam question on phagocytic digestion purely through memories of their Macrophage vs *S. aureus* runs. |

---

### Dimension 5: Scientific Misconception Resistance (反科学误导与迷思免疫力)
*Focus: Does the game actively prevent, challenge, and dispel dangerous or widespread biological misconceptions?*

| Level | Criteria | Example / Benchmark |
|:---:|:---|:---|
| **Level 1** | **Reinforces Harmful Myths**: Perpetuates dangerous misconceptions. | Antibiotics cure viral infections; "boosting immunity" indefinitely has no negative side effects; all microbes must be eradicated. |
| **Level 2** | **Careless Conflation**: Blurs distinctions between fundamental biological categories (e.g. treating viruses as tiny bacteria, or antibodies as living cells). | Viruses have cell membranes and organelles; bacteria undergo mitosis like eukaryotes. |
| **Level 3** | **Passive Accuracy**: Does not actively teach false biology, but misses opportunities to address common misconceptions. | Viruses and bacteria are separate enemy pools, but both react identically to standard attacks. |
| **Level 4** | **Active Misconception Quarantine**: Explicitly distinguishes viral, bacterial, fungal, and neoplastic pathogens. Accurately portrays immune overactivation as dangerous (cytokine storms, sepsis, SIRS). Acknowledges beneficial microbiomes. | Symbiotic flora and redox symbionts give benefits alongside real biological taxes; hyper-inflammatory states trigger systemic damage; phages specifically target bacteria. |
| **Level 5** | **Transformative Scientific Literacy**: Masterfully exposes non-linear biological truths: homeostasis vs inflammation balance, self-tolerance vs autoimmunity, trade-offs between innate speed and adaptive specificity. | Players experience the danger of autoimmune tissue destruction when over-investing in non-specific ROS; players understand why antibiotics are useless against Coronaviruses through distinct membrane physics. |

---

## Overall Educational Fidelity Index (EFI) Calculation

$$\text{EFI} = \sum_{i=1}^{5} (w_i \times S_i)$$

Where:
* $w_1 = 0.25$ (Mechanistic Scientific Fidelity)
* $w_2 = 0.20$ (Pathogen-Host Interaction Accuracy)
* $w_3 = 0.20$ (Scientific Nomenclature & Rigor)
* $w_4 = 0.20$ (Incidental Learning & Cognitive Ergonomics)
* $w_5 = 0.15$ (Scientific Misconception Resistance)
* $S_i \in [0, 100]$ is the score for dimension $i$.

### Grade Scale:
* **90 – 100 (Grade A+)**: Gold Standard Academic Edutainment
* **80 – 89  (Grade A)**: Exceptional Scientific Fidelity
* **70 – 79  (Grade B)**: Solid Educational Value with minor simplifications
* **60 – 69  (Grade C)**: Passable scientific theme; noticeable gamification gaps
* **< 60    (Grade F)**: Sub-standard scientific fidelity; requires remediation
