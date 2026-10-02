# Project: Phagocyte

> **2D Top-Down Microscopic Action Roguelite (Top-Down Microscopic Survivor-like)**
>
> Engine: Godot 4.x (.NET / C#) | Platform: PC (Steam)

---

## 🔬 Overview

*Project: Phagocyte* is a hardcore action roguelite set in the human microscopic immune system. Players pilot white blood cells (Macrophage, killer T cell, Neutrophil, B cell, dendritic cell) through damaged tissue and microvessels, devouring pathogens with dynamic amoeboid pseudopods, activating antigen presentation, and invoking epigenetic super-weapons against the frenzied invasion of bacteria, viruses, and mutant cancer cells.

### 🌟 Core Highlights
- **WYSIWYG organic physical deformation**: white blood cell silhouettes driven by FastNoiseLite vertex displacement, with polygon boundaries and colliders synced in real time.
- **Hardcore physiology gamified**: antigen presentation, opsonization, overload digestion, and NETosis converted into ultimate roguelite payoffs — "playing is learning immunology".
- **Purely generic global stat matrix**: strictly following Survivor-like philosophy, all stats 100% generalized, with zero single-skill bespoke attributes.
- **Classic "5 actives + 5 passives" with 1:1 ultimate super-weapon closure**: maxed actives fuse two-in-one with passives into 5 epigenetic super-weapons.
- **Orthogonal microtubule hematopoietic stem-cell talent tree**: confocal-fluorescence microscopy styling, 90-degree orthogonal non-overlapping microtubules, five differentiation lineages freely crossing lineages.

---

## 📚 Full Project Design Specs & Knowledge Base (Documentation)

All design documents and specs are archived under the [`docs/`](file:///Users/zelin/project/Phagocyte/docs/README.md) directory:

- 📖 **[Documentation Portal](file:///Users/zelin/project/Phagocyte/docs/README.md)**
- 📋 **[Project Master GDD / PRD (spec.md)](file:///Users/zelin/project/Phagocyte/docs/spec.md)**
- 💡 **[Biological Fidelity & Gamification Philosophy (real.md)](file:///Users/zelin/project/Phagocyte/docs/real.md)**
- 🧬 **[Five White Blood Cells: Morphology & Chassis (cell.md)](file:///Users/zelin/project/Phagocyte/docs/cell.md)**
- 📐 **[Global Generic Stat System Spec (stat.md)](file:///Users/zelin/project/Phagocyte/docs/stat.md)**
- ⚔️ **[Skill System, Super-Weapons & Micro-Play (skill.md)](file:///Users/zelin/project/Phagocyte/docs/skill.md)**
- 🩹 **[Stage Pathology Environments, Fluid Mechanics & Wave Direction (stages.md)](file:///Users/zelin/project/Phagocyte/docs/stages.md)**
- 🦠 **[Pathogen Codex & Immune Countermeasures (pathogen.md)](file:///Users/zelin/project/Phagocyte/docs/pathogen.md)**
- 🕸️ **[Hematopoietic Stem-Cell Orthogonal Talent Tree (passivetree.md)](file:///Users/zelin/project/Phagocyte/docs/passivetree.md)**
- 🏆 **[Achievements, Milestones & Meta Unlocks (achievement.md)](file:///Users/zelin/project/Phagocyte/docs/achievement.md)**
- 📊 **[Clinical Case Reports, History & Leaderboards (record.md)](file:///Users/zelin/project/Phagocyte/docs/record.md)**
- 🧭 **[Onboarding & Intuitive UI/UX Spec (tutorial.md)](file:///Users/zelin/project/Phagocyte/docs/tutorial.md)**
- ♾️ **[Endgame: Endless Cytokine Storm Mode (endgame.md)](file:///Users/zelin/project/Phagocyte/docs/endgame.md)**
- 🛠️ **[Clinical Anomaly Reports, Bug Diagnosis & Telemetry (feedback.md)](file:///Users/zelin/project/Phagocyte/docs/feedback.md)**

---

## 🛠️ Development & Build (Development)

This project is developed with **Godot 4.x (.NET edition)**, with core game logic written in **C#**:

- Dev environment: Godot Engine 4.x .NET / C# 10.0+ / .NET 8.0 SDK
- Main scene: `scenes/Main.tscn`
