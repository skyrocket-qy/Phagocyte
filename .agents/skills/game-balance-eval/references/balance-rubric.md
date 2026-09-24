# Dynamic Balance & Scalability Rubric (Phagocyte)

## Overview

This rubric defines the quantitative criteria and mathematical constraints for Project: Phagocyte's class win-rates, build variety, organelle power curves, and horde performance scalability.

---

## 1. Class Win-Rate Spread & Viability

| Metric | Target Standard | Failure Condition |
|:---|:---:|:---|
| **Class Win-Rate Delta (Hard Mode)** | $\le 15\%$ spread between highest and lowest class | One class $>30\%$ higher or lower win rate |
| **Endless Overdrive Survival Ceiling** | All 5 classes able to reach Minute 18:00+ under skilled play | Any class consistently hard-stuck before Minute 15:00 |
| **Innate Trait Impact** | Trait alters optimal build decisions by $\ge 30\%$ | Trait is purely cosmetic or ignored in meta builds |

### 5-Class Baseline Matrix:
- **Macrophage**: $140\text{ HP}$, $210\text{ Speed}$, $10\text{ Armor}$, Trait: $+8\%\text{ Block}$.
- **CTL**: $90\text{ HP}$, $260\text{ Speed}$, $0\text{ Armor}$, Trait: $+15\%\text{ Crit Chance}$.
- **Neutrophil**: $100\text{ HP}$, $230\text{ Speed}$, $5\text{ Armor}$, Trait: $+20\%\text{ Might}$.
- **B-Cell**: $95\text{ HP}$, $220\text{ Speed}$, $0\text{ Armor}$, Trait: $+30\%\text{ Projectile Speed}$.
- **Dendritic Cell**: $110\text{ HP}$, $225\text{ Speed}$, $2\text{ Armor}$, Trait: $+260\text{px Magnet Range}$.

---

## 2. Organelle Energy Efficiency & Anti-Monopoly

* **Energy Budgeting**:
  - Loadout sockets provide 6 to 12 ATP capacity across progression.
  - Organelle costs range from 1 to 5 ATP, with Symbiosis items providing negative costs (energy batteries, e.g. -1 ATP) in exchange for physiological drawbacks.
* **Potency Scaling**:
  - $1\text{ ATP Item} \approx 5\text{--}8\%$ stat benefit.
  - $3\text{ ATP Item} \approx 15\text{--}22\%$ stat benefit (or powerful dual stats).
  - $5\text{ ATP Item} \approx 30\text{--}40\%$ game-altering stat benefit.
* **Monopoly Prevention**:
  - Pick-rate distribution target: No single organelle exceeds $40\%$ overall pick rate across runs.
  - Zero dead items: Every organelle must possess positive synergy with at least 2 active skills.

---

## 3. Diminishing Returns & Hard Caps

To prevent runaway exponential math that crashes or trivializes the late game:

| Stat Parameter | Mathematical Cap | Diminishing Return Model |
|:---|:---:|:---|
| **Cooldown Reduction** | Hard cap at $75\%$ | Asymptotic curve: $\text{EffectiveCDR} = 1 - \frac{1}{1 + \text{CDR}}$ |
| **Evasion Chance** | Hard cap at $60\%$ | Linear to $40\%$, logarithmic thereafter |
| **Block Chance** | Hard cap at $50\%$ | Linear to $30\%$, diminishing thereafter |
| **Area of Effect** | Visual/physics clamp at $+150\%$ | Prevents full-screen unbounded hitboxes |
| **Life Steal** | Soft cap at $15\%$ proc chance | Maximum 1 heal trigger per 0.2s |

---

## 4. Horde Scalability & Performance Benchmarks

All balance changes, new pathogens, and weapon VFX must satisfy the following performance constraints verified via `tests/TestHordeBenchmark.cs`:

| Stress Tier | Target Average FPS | Target 1% Low FPS | Max Frame Spike |
|:---|:---:|:---:|:---:|
| **Tier 1 (100 Pathogens)** | $\ge 60\text{ FPS}$ | $\ge 50\text{ FPS}$ | $\le 16.6\text{ms}$ |
| **Tier 2 (300 Pathogens)** | $\ge 60\text{ FPS}$ | $\ge 45\text{ FPS}$ | $\le 20.0\text{ms}$ |
| **Tier 3 (500 Pathogens)** | $\ge 55\text{ FPS}$ | $\ge 40\text{ FPS}$ | $\le 25.0\text{ms}$ |

* **Zero GC Hot-Path Allocations**: Verified $0\text{ KB/frame}$ in `_Process` / `_PhysicsProcess`.
* **MultiMesh Batching**: High-density micro-viruses (norovirus, influenza) must automatically batch into `MultiMeshInstance2D` when entity count exceeds 50.
