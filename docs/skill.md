# Project: Phagocyte Skill System, Super-Weapon Arsenal & Micro-Control Spec (Skill System, Evolutions & Dodge Roll)

---

## 1. Skill Architecture and Stat Integration Principles

In Project: Phagocyte, the skill system is the core vehicle for player combat builds. All skills strictly follow the modular principle of "high cohesion, low coupling":

- **Universal Stat Injection**: cooldown (`cooldown_reduction`), damage (`damage`), area (`area`), projectile speed (`projectile_speed`), projectile count (`amount`), and pierce (`pierce`) for every active skill are **read 100% dynamically from the universal stat pool** and never defined as private variables.
- **Universal Numerical Model Reference**: for the 19 universal stat definitions, standard formulas, and boundary constraints, see the dedicated spec 👉 **[`docs/stat.md`](file:///Users/zelin/project/Phagocyte/docs/stat.md)**.
- **Fully Automatic Independent Fire Loops**: each active skill is mounted under the cell entity with its own cooldown timer and targeting logic, firing automatically when ready so players can focus on positioning, kiting, and micro-physics maneuvering.

---

## 2. Slot Architecture: The "5 Actives + 5 Passives" Classic Loop

In a single run, the player can equip up to **5 active biochemical skills** and **5 passive metabolic traits**:

```
┌────────────────────────────────────────────────────────────────────────┐
│ Active Skill Slots (Active Cytokines x5) - Fully Automatic Cyclic Fire  │
│ [1: Perforin Lance] [2: Complement Falls] [3: Antibody Salvo] [4: ROS Torrent] [5: Pseudopod Lunge] │
├────────────────────────────────────────────────────────────────────────┤
│ Passive Trait Slots (Passive Organelles x5) - Pure Universal Stat Bonus │
│ [1: Lysosome] [2: Actin] [3: Opsonin] [4: Mitochondria] [5: Chemokine Receptor] │
│   (Damage+Regen) (Area+Speed)  (Crit+Dmg)     (CDR+Dur)     (Magnet+Speed)  │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 3. The Five Active Biochemical Skills (Active Cytokines)

Each active skill runs on its own cooldown cycle timer, with output computed live from universal stats:

| Active Skill | Medical Prototype | Universal Stats Consumed | Combat Behavior & Mechanics |
| :--- | :--- | :--- | :--- |
| **0. Phagocytic Grasp<br>(Phagocytic Grasp, Macrophage Innate)** | Rapid pseudopod strike of the Macrophage | `damage`, `area`, `amount`, `cooldown_reduction` | Extends 2 pseudopods from near to far to grab; `amount` adds grabs; chained hits deal contact damage. Amoeboid deformation itself is the shared chassis visual for all cells. |
| **1. Perforin Lance<br>(Perforin Lance)** | Killer T cell pore formation on target membranes | `damage`, `projectile_speed`, `amount`, `pierce`, `crit_chance` | Fires a high-velocity spiral beam at the nearest high-threat pathogen. `amount` adds beams per volley, `pierce` adds targets penetrated. |
| **2. Complement Cascade<br>(Complement Cascade)** | Detonation triggered by chained complement cleavage and deposition | `damage`, `area`, `cooldown_reduction`, `duration` | Spawns biochemical halo mines at random positions around the player; `area` expands mine radius, detonating after a delay with a powerful area shockwave. |
| **3. Y-Type Antibody Salvo<br>(Antibody Salvo)** | B cell secretion of free antibodies neutralizing pathogens | `damage`, `amount`, `cooldown_reduction`, `projectile_speed`, `duration` | Periodically erupts homing Y-shaped antibody missiles in 360 degrees; `amount` directly adds projectiles per volley. |
| **4. ROS Torrent<br>(ROS Torrent / Spray)** | Phagocyte respiratory burst releasing $\text{H}_2\text{O}_2$ | `damage`, `area`, `duration`, `cooldown_reduction` | Sprays a high-pressure cone of acid mist ahead of movement; `area` widens the cone angle and range, melting armor with a sustained-dissolve DoT. |
| **5. Pseudopod Lunge<br>(Pseudopod Lunge)** | Instant directional burst ejection of actin filaments | `damage`, `area`, `amount`, `cooldown_reduction` | Violently ejects amoeboid fleshy grabs outward; `area` extends lunge distance, stunning enemies on hit and forcibly dragging them inward. |

---

## 4. The Five Passive Metabolic Traits (Passive Organelles)

Passive traits contain no weapon-specific logic; they purely grant the host universal Stat bonuses (scaling per level):

| Passive Trait | Biological Flavor | Pure Universal Stat Bonus (per level) | Tactical Role |
| :--- | :--- | :--- | :--- |
| **1. Lysosome Priming<br>(Lysosome Priming)** | Intracellular hydrolase reserves and activation | `damage +10%` / `health_regen +0.6 HP/s` | All-damage scaling plus sustained autophagic repair |
| **2. Actin Polymerization<br>(Actin Polymerization)** | Directed polymerization of cytoskeletal filaments | `area +12%` / `move_speed +6%` | All-skill area scaling plus mobile positioning |
| **3. Opsonin Affinity<br>(Opsonin Affinity)** | Surface-specific receptor proliferation | `crit_chance +5%` / `crit_damage +25%` | Lethal weak-point crits and overkill execution |
| **4. Mitochondrial Overclock<br>(Mitochondrial Overclock)**| Multiplied ATP output from the tricarboxylic acid cycle | `cooldown_reduction +8%` / `duration +10%` | Faster fire cycles for all skills plus longer effect persistence |
| **5. Chemokine Receptors<br>(Chemokine Receptors)** | Highly sensitive chemotactic antennae on the surface | `magnet +25%` / `move_speed +6%` | Wide-area auto-collection plus directed chemotactic swim speed |

### 4.1 Six Organelle Chamber Categories (Organelle Chamber 2x2, Third System)

An independent equipment system coexisting with the 5 actives + 5 passives: passives = universal chassis bonuses, chambers = high-cost extreme pieces + power-generation puzzle. 2x2 = up to 4 pieces (all 1x1, no merging), base energy 6, `energy_cost ∈ [-1, 4]` (`-1` = generates `+1` power but always carries a heavy drawback). Used = sum of positive costs, cap = 6 + total generation; legal iff used <= cap and piece count <= 4. Acquisition flows from enemy drops (base 2% chance) -> upgrade drafts of three choices (at most 1 organelle card per round; swap loadout when slots are full) -> pre-run loadout page (runs start unequipped with 4 empty slots by default).

| Category | High-Cost Core | Budget / Generator Counterpart | Energy |
| :--- | :--- | :--- | :--- |
| **Metabolism (metabolism)** | Mitochondria MkII: `CDR +0.16`/`duration +10%` | Glycolytic Bypass: `CDR +0.05`/`move_speed +3%` | 4/1 |
| **Digestion (digestion)** | Strong-Acid Lysosome: `damage +12%`/`dot_damage +15%` | Proteasome Sieve: `dot_damage +8%`/`health_regen +0.3` | 3/1 |
| **Cytoskeleton (cytoskeleton)** | Flagellar Base: `move_speed +12%` | Microtubule Anchor: `move_speed +4%`/`area +4%` | 3/1 |
| **Synthesis (synthesis)** | Rough Endoplasmic Reticulum: `amount +1`/`projectile_speed +8%` (only `amount+1`, locked at 4 cost) | Ribosome Cluster: `projectile_speed +8%`/`duration +8%` | 4/2 |
| **Sensing (sensing)** | Ion Channel Array: `armor +3`/`block +0.04`/`magnet +15%` | Chemotaxis Patch: `magnet +20%`/`evasion +0.02` | 3/1 |
| **Symbiosis (symbiosis, generator)** | Symbiotic Flora: `+1` generation at the cost of `move_speed -30%`/`damage -15%` (cripple build) | Phage Fragment: `+1` generation/`CDR +0.05` at the cost of `max_health -20%` (health-for-power) | -1/-1 |

---

## 5. The Five Ultimate Epigenetic Super-Weapons (Epigenetic Evolutions)

When an active skill reaches **Lv.5 (Max)** and its matching **passive trait (any level)** is held, killing an elite opens a chest that triggers a 1:1 two-in-one transformative evolution:

```mermaid
graph LR
    subgraph 5-Unit Super-Weapon Two-in-One Fusion Matrix
        A1["Perforin Lance (Lv.5)"] + B1["Lysosome Priming"] --> EVO1["Granzyme Execution<br>(Granzyme Apoptosis)"]
        A2["Complement Cascade (Lv.5)"] + B2["Actin Polymerization"] --> EVO2["Membrane Attack Final Array<br>(MAC Hyper-Array)"]
        A3["Y-Type Antibody Salvo (Lv.5)"] + B3["Opsonin Affinity"] --> EVO3["Neutralizing High-Pressure Storm<br>(Neutralizing Tempest)"]
        A4["ROS Torrent (Lv.5)"] + B4["Mitochondrial Overclock"] --> EVO4["Peroxide Leviathan<br>(Superoxide Leviathan)"]
        A5["Pseudopod Lunge (Lv.5)"] + B5["Chemokine Receptors"] --> EVO5["Amoebic Primal Maw<br>(Amoebic Maelstrom)"]
    end
```

### 5.1 Super-Weapon Mechanics in Detail
1. **Granzyme Execution (Granzyme Apoptosis)**:
   - *Mechanism prototype*: after perforin punches pores in the membrane, granzyme is rapidly injected to trigger programmed apoptosis in the target cell.
   - *Gameplay effect*: a piercing beam implants an apoptosis mark on hit. The target detonates after 1 second, bursting into high-speed chaining pierce rays splashing along 6 orthogonal directions for domino-style screen clears.
2. **Membrane Attack Final Array (MAC Hyper-Array)**:
   - *Mechanism prototype*: the complement C5b-9 complex assembles directly on the cell membrane into irreversible membrane attack pores.
   - *Gameplay effect*: complement mines no longer scatter randomly but attach directly to the tips of the player's dynamic pseudopods. While repositioning, they trail a flowing chemical vortex wake behind the player, instantly lysing every pathogen touched into ATP experience drops.
3. **Neutralizing High-Pressure Storm (Neutralizing Tempest)**:
   - *Mechanism prototype*: mass cross-linking and aggregation of pathogens by high-affinity antibodies, forming insoluble immune complexes.
   - *Gameplay effect*: fires 32 high-frequency cruising antibodies across the screen. When antibodies hit different targets, they stretch "high-voltage biochemical web strands" between them, dealing max-HP-percentage true damage to all lesser enemies crossing the strands.
4. **Peroxide Leviathan (Superoxide Leviathan)**:
   - *Mechanism prototype*: peroxide super-enrichment across the whole cell membrane surface, transforming the cell into a strongly oxidizing ion turbine.
   - *Gameplay effect*: removes the forward-spray restriction. The whole cell is wrapped in a cyan-blue plasma superoxide film, turning the cell itself into a spinning shredder that melts every non-Boss pathogen on contact.
5. **Amoebic Primal Maw (Amoebic Maelstrom)**:
   - *Mechanism prototype*: limit-break pseudopod eruption with microtubule-towed global strangulation.
   - *Gameplay effect*: pseudopod lunges split into 4 omnidirectional giant amoeboid tentacles, opening a massive gravity vortex at the arena center that drags all on-screen pathogen minions and ATP experience motes into a strangling force field and shreds them in one gulp!

---

## 6. Micro-Control: Dodge Roll (Dodge Roll)

To add hardcore micro-control depth to the Survivor-like auto-fire formula, this project includes a dodge-roll mechanic:

- **Input**: tap `Space` (spacebar) or gamepad `L2` to burst-displace along the current movement direction.
- **Physiological counterpart**: a short, rapid chemotactic leap driven by explosive actin polymerization in the white blood cell.
- **Tactical numbers**:
  - 1 dodge charge, replenished every 2.5 seconds.
  - 0.18-second dash at 3.2x speed with 0.22 seconds of invulnerability (dash + afterglow), during which all damage (including environmental) counts as dodged.
  - When surrounded by pathogen hordes or facing dense bullet patterns, one roll carries you through the pack or lets you eat a Boss ultimate unscathed.
