# Canonical Roguelite Build Archetypes (Phagocyte)

## Overview

Project: Phagocyte rejects linear progression where players simply stack raw damage. Instead, player builds emerge from synergies between **Cell Class Traits**, **Active Weapons / Skills**, **Organelles (Slot Items)**, and the **Epigenetic Passive Tree**.

This document defines the **5 Canonical Immunological Archetypes**, detailing their core win conditions, required items, and scaling metrics.

---

## Archetype 1: Respiratory Burst (ROS) Oxidation Melt

> **Concept**: *Release reactive oxygen species ($\text{H}_2\text{O}_2$, $\text{O}_2^{\bullet-}$) to induce lipid peroxidation and melt swarms over a broad area.*

* **Primary Classes**: Macrophage, Neutrophil
* **Core Skills**:
  - `ros_torrent` (Continuous chemical flame jet)
  - `phagolysosome_vent` (Acidic mist venting)
  - `nitric_oxide_halo` (Peroxynitrite zone)
* **Key Supporting Organelles**:
  - `acidic_lysosome` (+Might, +Digestion)
  - `v_atpase_pump` (+Ailment tick rate)
  - `catalase_quenched` (+Duration, -Drawback self-damage)
* **Primary Scaling Stats**: `might`, `duration`, `area`, `ailment_damage`
* **Target Combat Feel**: Incinerating wave clear; satisfying chemical sizzle audio and rapid cyan/lime tick numbers.
* **Benchmark DPS**: High AoE sustained DPS ($120\text{--}240\text{ DPS}$ across 30+ enemies), moderate single-target damage.

---

## Archetype 2: Cytotoxic Piercing Sniper

> **Concept**: *Polymerize perforin cylinders into target cell membranes, delivering lethal granzyme B proteases for targeted executions.*

* **Primary Classes**: Cytotoxic T-Lymphocyte (CTL)
* **Core Skills**:
  - `perforin_lance` (High-pierce directional projectile)
  - `granzyme_detonation` (Targeted proteolytic explosion on low-health targets)
  - `nuclease_blades` (High-velocity slicing orbitals)
* **Key Supporting Organelles**:
  - `ribosome_hypertrophy` (+Crit chance, +Crit damage)
  - `centrosome_spindle` (+Pierce count, +Projectile speed)
  - `secretory_vesicle_mkii` (+Crit multiplier)
* **Primary Scaling Stats**: `crit_chance`, `crit_damage`, `projectile_speed`, `pierce`
* **Target Combat Feel**: Sniping high-threat elites and bosses from safe distance; explosive golden crit pops and glass shatter SFX.
* **Benchmark DPS**: Massive single-target burst ($350\text{--}600\text{ DPS}$ against bosses/elites), narrow projectile trajectory requiring directional aim.

---

## Archetype 3: Engulf-and-Digest Heavy Tank

> **Concept**: *Massive membrane extensions trap pathogens into phagosomes, rapidly digesting them to replenish ATP and restore vital cellular integrity.*

* **Primary Classes**: Macrophage, Dendritic Cell
* **Core Skills**:
  - `phagocytic_grasp` (Pseudopod chain hook & engulfment)
  - `lysosomal_overload` (Immediate enzymatic digestion burst)
  - `pseudopod_lunge` (High-mass kinetic slam)
* **Key Supporting Organelles**:
  - `cortical_actin_mesh` (+Max health, +Block chance)
  - `digestive_vacuole_prime` (+Life steal, +Digestion speed)
  - `integrin_anchor` (+Armor, +Knockback resistance)
* **Primary Scaling Stats**: `max_health`, `armor`, `life_steal`, `block`
* **Target Combat Feel**: Immovable heavyweight absorbing enemy charges; thick organic squish sound and green heal numbers floating continuously.
* **Benchmark DPS**: Low projectile DPS, but high execution kill rate and pseudo-infinite sustain in 400-enemy crowds.

---

## Archetype 4: Complement Cascade Trap Network

> **Concept**: *Deposit C3b/C5b convertase units across the battlefield, triggering automated Membrane Attack Complex (MAC) pore formation when enemies cross the threshold.*

* **Primary Classes**: Neutrophil, B-Cell
* **Core Skills**:
  - `complement_cascade` (Delayed high-yield MAC ring minefields)
  - `exosome_singularity` (Micro-gravitational suction pulling pathogens into killzones)
* **Key Supporting Organelles**:
  - `mitochondria_mkii` (+Cooldown reduction, +Duration)
  - `glycolytic_bypass` (+Move speed, +Cooldown reduction)
  - `endoplasmic_reticulum_matrix` (+Area of effect)
* **Primary Scaling Stats**: `cooldown_reduction`, `area`, `duration`
* **Target Combat Feel**: Tactical battlefield zoning; baiting swarms into synchronized MAC ring detonations with cascading explosions.
* **Benchmark DPS**: Burst cycles ($400\text{--}800\text{ Damage}$ per detonation wave every 2.5s).

---

## Archetype 5: Antibody Opsonization Swarm

> **Concept**: *Unleash high-frequency bivalent immunoglobulins that tag pathogens with opsonins, triggering cascading chain reactions and homing projectile swarms.*

* **Primary Classes**: B-Lymphocyte, Dendritic Cell
* **Core Skills**:
  - `antibody_salvo` (High-cadence multi-target homing immunoglobulins)
  - `defensin_barbs` (Reactive thorn discharge on contact)
  - `mhc_tracer_beam` (Opsonin target painter)
* **Key Supporting Organelles**:
  - `golgi_distributor` (+Projectile amount, +Spread)
  - `polyribosome_cluster` (+Fire cadence, -Cooldown)
  - `chemokine_patch` (+Magnet range, +Drop affinity)
* **Primary Scaling Stats**: `projectile_amount`, `cooldown_reduction`, `move_speed`, `magnet`
* **Target Combat Feel**: Screen-filling fluorescent micro-missile swarms; rapid machine-gun chitter SFX and cascading neon blue tracer arcs.
* **Benchmark DPS**: High distributed multi-target DPS ($180\text{--}300\text{ DPS}$ spreading evenly across the screen).
