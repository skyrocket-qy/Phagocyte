import type { AilmentsFile } from "../schemas/ailment";

// Single source of truth for assets/data/ailmentson. Values mirror the
// legacy hand-authored rows; the only addition is the hit VFX on opsonization
// (previously hardwired in SalvoSkill.HitTarget, now data).
export const Ailments: AilmentsFile = {
  schema: 1,
  slow_aggregation: "strongest",
  amp_applies_to_own_dot: true,
  ailments: [
    {
      id: "oxidative_burn",
      name: "ROS Oxidative Burn",
      duration: 3.0,
      magnitude: 0.0,
      stack: "refresh_max",
      channels: ["dot"],
    },
    {
      id: "agglutination",
      name: "Agglutination",
      duration: 2.5,
      magnitude: 0.4,
      stack: "strongest_wins",
      channels: ["slow"],
      min_magnitude: 0.05,
      max_magnitude: 0.75,
    },
    {
      id: "opsonization",
      name: "Opsonization",
      duration: 4.0,
      magnitude: 0.3,
      stack: "refresh_max",
      channels: ["amp"],
      min_magnitude: 0.1,
      max_magnitude: 0.6,
      vfx: "MarkBind",
    },
    {
      id: "membrane_leak",
      name: "Membrane Leakage",
      duration: 3.0,
      magnitude: 0.0,
      stack: "refresh_max",
      channels: ["dot"],
      move_multiplier: 3.0,
    },
    {
      id: "endotoxin",
      name: "Endotoxin",
      duration: 4.0,
      magnitude: 0.0,
      stack: "independent",
      channels: ["dot"],
      max_stacks: 0,
    },
  ],
};
