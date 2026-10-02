# Project: Phagocyte Hematopoietic Stem Cell Orthogonal Talent Tree Spec (Hematopoiesis Talent Matrix)

---

## 1. Core Design Concept and Five-Starting-Hub Architecture

The talent system combines the Path of Exile single-giant-tree architecture with microscopic biology to build a top-tier **confocal fluorescence microscopic biochemical tree (Confocal Fluorescence Microtubule Board)**:

- **Five White Blood Cells, Independent Starting Hubs (5 Distinct Cell Starting Hubs)**:
  Unlike traditional designs radiating from a single center, **each of the five immune cells has its own dedicated starting hub**.
  When the player deploys a Macrophage, they light up directly from the "Macrophage starting hub"; when deploying a CTL, they start from the "CTL starting hub". Players can deepen their own lineage or extend along the microtubule network to the central interconnect trunk and cross into other cells' starting hubs and specialization zones (cross-lineage chimeric differentiation).

```mermaid
flowchart TD
    subgraph StartingHubs [Five Independent Cell Starting Hubs (5 Distinct Starting Hubs)]
        HUB_MAC["[Macrophage Starting Hub]<br>(Macrophage Hub - top-left)<br>Bulk Area / Health HP / Armor / Block"]
        HUB_CTL["[Killer T Starting Hub]<br>(CTL Hub - top-right)<br>Move Speed / Crit / Pierce / Evasion"]
        HUB_NEU["[Neutrophil Starting Hub]<br>(Neutrophil Hub - bottom-left)<br>Damage / Knockback / Regen"]
        HUB_B["[B Cell Starting Hub]<br>(B-Cell Hub - bottom-right)<br>Projectile Count Amount / Projectile Speed ProjSpd / CDR / Drain LifeSteal"]
        HUB_DC["[Dendritic Starting Hub]<br>(Dendritic Hub - mid-left)<br>Pickup Magnet / Duration / Cooldown CDR"]
    end

    subgraph CentralHighway [Central Microtubule Interconnect Trunk Network (Inter-Hub Microtubules)]
        CORE["[Microtubule Interconnect Nexus (Nexus)]<br>Cross-lineage chimera differentiation channel"]
    end

    HUB_MAC <--> CORE
    HUB_CTL <--> CORE
    HUB_NEU <--> CORE
    HUB_B <--> CORE
    HUB_DC <--> CORE
```

### 1.1 Six-Region Grid Layout (2x3 Region Matrix)

Each region is a complete **3x3 = 9-node square (Region Square)** with the cell start at the exact center of the square;
the five cell squares plus the "Hematopoietic Core" form six squares joined edge-to-edge with no gaps in a tight **2-column x 3-row** arrangement,
assembling into one **6x9 node board (54 nodes total)**:

```
[macrophage vitality ][CTL precision  ]
[dendritic senses   ][core core      ]
[neutrophil motility ][B cell ballistics]
```

- Every grid node connects to its orthogonally adjacent nodes above, below, left, and right with unit orthogonal microtubules (including cross-region borders), 93 edges total.
- The **Hematopoietic Core** sits in the middle-right column of the board, adjacent to the dendritic, CTL, and B cell regions.
- Each region square has a side length of 450px (3 cells); starts are lit innately, and players light outward in all four directions from their start, freely crossing region borders.

### 1.2 Node-Trait-Stat Three-Layer Architecture (Node -> Trait -> Stat)

Data is strictly split into three layers, matching the microtubule tree's "talent points / traits / attributes":

- **Node**: owns only position and region `{id, branch, col, row}`, plus one `trait` reference.
- **Trait**: a reusable talent definition `{id, name_key, desc_key, icon, rarity, modifiers[]}`.
  Name, icon, rarity, and attributes are **all owned by the trait** - so **the same trait always has the same icon**, and same-name-different-icon cases are impossible.
- **Stat**: `modifiers` inside a trait map directly to the generic attributes in `stat_labels.json`.

Currently the 54 nodes map to 54 traits one-to-one (one node, one trait); when small repeat nodes are added later, sharing an existing trait id automatically inherits the same name, icon, and values.
The exception is the five cell starts: their traits carry **no attributes** (`modifiers` empty) and serve only as departure anchors, so all five starts look exactly identical (cyan square, new "Start" rarity).

---

## 2. Orthogonal Microtubule Grid Board Topology and Ring-Layer Definitions (Orthogonal Grid & Ring Layers)

To eliminate spider-web crossings and diagonal-overlap visual chaos, the whole tree strictly uses a **90-degree orthogonal grid (Orthogonal Grid Board)**:

- **One node per cell principle**: each node lands precisely on a single integer grid point $(col, row)$.
- **World-coordinate mapping formula**:
  $$\text{Position} = \text{WorldCenter} + \begin{pmatrix} col \times \text{GridStep} \\ -row \times \text{GridStep} \end{pmatrix} \quad (\text{GridStep} = 150.0\,\text{px})$$
- **Orthogonal-adjacency edge principle**: microtubule fibers may only run between orthogonally adjacent grid points above, below, left, and right; **diagonal edges are never allowed, and crossings/overlaps never occur**.
- **3x3 Region Squares (Region Squares)**: the five cell regions and the hematopoietic core each occupy one complete 3x3 node square (9 nodes), tiled 2 columns x 3 rows into one 6x9 board; each cell start is fixed at the exact center of its square. All orthogonally adjacent nodes (including cross-region borders) connect with unit orthogonal microtubules, so regions can be freely lit across borders.
- **Cell-Specific Manhattan Ring Layer ($L$)**:
  Each node's depth relative to each cell-specific start $(col_{\text{start}}, row_{\text{start}})$ is determined by Manhattan steps:
  $$\text{Ring Layer } L_{\text{cell}} = |col - col_{\text{start}}| + |row - row_{\text{start}}|$$
  - **$L = 0$**: that cell's dedicated starting hub (innately and permanently lit, costs 0 points).
  - **$L = 1 \sim 2$**: that cell's proximal core metabolism ring (provides core survival and specialization base attributes).
  - **$L = 3 \sim 4$**: mid-range specialization microtubules and liaison trunks to the central interconnect network.
  - **$L \ge 5$**: entry into the central microtubule nexus, or cross-lineage infiltration into other cells' specialization domains.

---

## 3. Node Rarity and "Zero-Complexity Scaling" Numeric Design Principles

> [!IMPORTANT]
> **Simple numeric principles (Simple & No Scaling Philosophy)**:
> At the current version stage, talent attribute design strictly follows the **"pure, intuitive, no compound derived scaling (No Scaling / No Cross-Attribute Conversion)"** principle.
> - 🚫 **Attribute-linkage conversions are forbidden** (e.g. "gain 5% damage per 100 health", "convert armor into crit rate" are all rejected).
> - ✅ **Strictly use the standard dual-track base bonuses**:
>   - **Pure flat (Flat)**: e.g. `max_health +15`, `armor +2`, `amount +1`, `pierce +1`.
>   - **Pure percent (Simple Percent)**: e.g. `damage +5%`, `move_speed +5%`, `area +6%`, `cooldown_reduction +4%`, `evasion +3%`, `block +4%`, `life_steal +1%`.
> - The final generic formula stays pure and transparent: $\text{FinalStat} = (\text{Base} + \text{FlatBonus}) \times (1.0 + \text{PercentBonus})$.

### Node Rarity Tier Table

Fixed quota of 54 nodes: **2 legendary / 4 rare / 10 magic / 33 normal / 5 start**;
rarity uses **scattered placement (scatter)**: every region holds at least one non-normal node and at most 3 special nodes,
the 2 legendaries sit in different regions, rares span 3 regions;
the five cell starts share the new "Start" rarity - cyan squares, innately lit, carrying no attributes.

| Rarity Tier | Appearance Primitive | Numeric Bonus Structure (Pure Intuitive Bonuses) | Biological Metabolic Cost (Trade-off) | Current Status |
| :--- | :--- | :--- | :--- | :--- |
| **Normal (Normal)** | Small white round vesicle | Grants one basic generic attribute (e.g. `damage +4%` or `health_regen +0.3`). | No cost. | **Shipped (33)** |
| **Magic (Magic)** | Larger blue round vesicle | Grants one stronger or two complementary generic attributes (e.g. `duration +14%`, `magnet +40%`). | No cost. | **Shipped (10)** |
| **Rare (Rare)** | Gold diamond biochemical complex totem | Grants two large pure-attribute bonuses (e.g. `armor +3` + `block +4%`, `crit_damage +15%` + `crit_chance +3%`). | No cost. | **Shipped (4)** |
| **Unique/Legendary (Unique)** | Orange hexagon totem | Top-tier multi-attribute totem (e.g. `max_health +15%` + `armor +2`, `area +30%`, up to five bonuses). | Significant flat cost (e.g. `max_health -25%`). | **Shipped (2)** |
| **Start (Start)** | Cyan square hub | **Carries no attributes** - serves only as each cell's departure point; all five starts look exactly identical. | Innately lit (0 points). | **Shipped (5)** |

---

## 4. Five Cell-Specific Starts and Region Specializations

Each cell start and its surrounding microtubule network carry a dedicated microscopic fluorescence staining atmosphere:

### 4.1 Macrophage Starting Hub (Macrophage Starting Hub)
- **Palette**: dark reddish-brown with warm amber fluorescence.
- **Position**: top-left of the tree.
- **Specialized pure attributes**: `area` (bulk/area), `max_health` (max health), `armor` (membrane rigidity mitigation), `block` (glycocalyx block rate).

### 4.2 Killer T Cell Starting Hub (CTL Starting Hub)
- **Palette**: ice blue with sharp cyan-green fluorescence.
- **Position**: top-right of the tree.
- **Specialized pure attributes**: `move_speed` (move speed), `crit_chance` (crit rate), `crit_damage` (crit damage), `pierce` (projectile pierce count), `evasion` (fluid evasion rate).

### 4.3 Neutrophil Starting Hub (Neutrophil Starting Hub)
- **Palette**: blaze orange with acidic bright-yellow fluorescence.
- **Position**: bottom-left of the tree.
- **Specialized pure attributes**: `damage` (damage strength), `health_regen` (health regen), `armor` (membrane rigidity armor).

### 4.4 B Cell Starting Hub (B-Cell Starting Hub)
- **Palette**: deep indigo with ionized violet fluorescence.
- **Position**: bottom-right of the tree.
- **Specialized pure attributes**: `amount` (projectile spawn count), `projectile_speed` (projectile velocity), `cooldown_reduction` (skill cooldown reduction), `life_steal` (receptor drain/life on hit).

### 4.5 Dendritic Cell Starting Hub (Dendritic Starting Hub)
- **Palette**: ionized violet with faint gold-green fluorescence.
- **Position**: mid-left of the tree.
- **Specialized pure attributes**: `magnet` (ATP pickup radius), `duration` (status and aura duration), `cooldown_reduction` (skill cooldown reduction), `area` (sensing and effect area).

---

## 5. Lighting Mechanics and Cross-Lineage Builds (Progression & Cross-Lineage Builds)

1. **Point-spend principles**:
   - Each normal / magic / rare / legendary node lights once, costing a flat **1 microtubule talent point** per purchase; nodes have only placed / unplaced states, with no rank concept.
   - The currently selected cell's starting-hub node is innately active and permanently lit, costing 0 points; the five starts carry no attributes and uniformly appear as cyan squares ("Start" rarity).
2. **Talent point sources**:
   - **Map clearance rewards**: clearing the 5 organ maps on Normal and Hard grants 1 point each (10 points steadily obtainable in total).
   - **Achievement milestones**: hitting specific single-condition research achievements grants bonus talent points.
3. **Cross-Lineage Differentiation (Cross-Lineage Differentiation)**:
   - Players start from their own cell start, light toward the central microtubule interconnect nexus, then freely extend into other cells' specialization domains:
     - *Example*: a Neutrophil starts on the left, crosses the central microtubule network into the bottom-right B cell region, building a "huge projectile count + furious high damage" artillery build;
     - *Example*: a Macrophage starts top-left, crosses the center into the right-side CTL region, building a "high move speed + giant-body crush" speedy-macrophage build.
4. **Painless respec experience**:
   - Tree data is saved locally to `user://passive_tree.json`, with **one-click free reset (Reset All)** at any time, encouraging players to try different pure-attribute archetypes freely.
5. **Multiple build profiles (Build Profiles, up to 3 slots)**:
   - Each cell owns 1-3 talent profiles (dynamically added/removed, `+` / red `Delete`); a new profile starts empty and automatically becomes current, deleting the current profile falls back to the previous one automatically, and the last profile cannot be deleted.
   - Cell levels and achievement talent points are shared across the cell's three profiles; switching profiles only changes spent points, with no respec needed.
   - Deployment uses the profile selected when "Next" was pressed; legacy single-profile saves auto-migrate into profile one.
   - The pause-menu talent overview labels the current profile name (read-only).
