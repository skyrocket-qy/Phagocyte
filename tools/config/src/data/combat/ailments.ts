import { AilmentId, AilmentChannel, AilmentStackRule, type AilmentsFile } from "../../schemas/ailment.schema";

export { AilmentId, AilmentChannel, AilmentStackRule };

export const Ailments: AilmentsFile = {
  schema: 1,
  slow_aggregation: "strongest",
  amp_applies_to_own_dot: true,
  ailments: [
    {
      id: AilmentId.Ignite,
      name: "ROS Oxidative Burn",
      duration: 3,
      magnitude: 0,
      stack: AilmentStackRule.RefreshMax,
      channels: [
        AilmentChannel.Dot
      ]
    },
    {
      id: AilmentId.Chill,
      name: "Agglutination",
      duration: 2.5,
      magnitude: 0.4,
      stack: AilmentStackRule.StrongestWins,
      channels: [
        AilmentChannel.Slow
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
      channels: [
        AilmentChannel.Amp
      ],
      min_magnitude: 0.1,
      max_magnitude: 0.6,
      vfx: "MarkBind"
    },
    {
      id: AilmentId.Bleed,
      name: "Membrane Leakage",
      duration: 3,
      magnitude: 0,
      stack: AilmentStackRule.RefreshMax,
      channels: [
        AilmentChannel.Dot
      ],
      move_multiplier: 3
    },
    {
      id: AilmentId.Poison,
      name: "Endotoxin",
      duration: 4,
      magnitude: 0,
      stack: AilmentStackRule.Independent,
      channels: [
        AilmentChannel.Dot
      ],
      max_stacks: 0
    }
  ]
};
