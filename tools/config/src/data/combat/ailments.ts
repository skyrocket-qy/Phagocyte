import { AilmentId, AilmentKind, AilmentStackRule, type AilmentsFile } from "../../schemas/ailment.schema";

export { AilmentId, AilmentKind, AilmentStackRule };

export const Ailments: AilmentsFile = {
  schema: 1,
  slow_aggregation: "strongest",
  amp_applies_to_own_dot: true,
  ailments: [
    {
      id: AilmentId.Ignite,
      name: "ROS Oxidative Burn",
      duration: 4,
      magnitude: 0,
      stack: AilmentStackRule.RefreshMax,
      kinds: [
        AilmentKind.Dot
      ]
    },
    {
      id: AilmentId.Chill,
      name: "Agglutination",
      duration: 2.5,
      magnitude: 0.4,
      stack: AilmentStackRule.StrongestWins,
      kinds: [
        AilmentKind.Slow
      ],
      min_magnitude: 0.05,
      max_magnitude: 0.75
    },
    {
      id: AilmentId.Shock,
      name: "Opsonization",
      duration: 4,
      magnitude: 0.3,
      stack: AilmentStackRule.RefreshMax,
      kinds: [
        AilmentKind.Amp
      ],
      min_magnitude: 0.1,
      max_magnitude: 0.6,
      vfx: "MarkBind"
    },
    {
      id: AilmentId.Bleed,
      name: "Membrane Leakage",
      duration: 5,
      magnitude: 0,
      stack: AilmentStackRule.RefreshMax,
      kinds: [
        AilmentKind.Dot
      ],
      move_multiplier: 2
    },
    {
      id: AilmentId.Poison,
      name: "Endotoxin",
      duration: 6,
      magnitude: 0,
      stack: AilmentStackRule.Independent,
      kinds: [
        AilmentKind.Dot
      ],
      max_stacks: 5
    },
    {
      id: AilmentId.Stun,
      name: "Neural Arrest",
      duration: 1,
      magnitude: 0,
      stack: AilmentStackRule.RefreshMax,
      kinds: [
        AilmentKind.Stun
      ]
    }
  ]
};
