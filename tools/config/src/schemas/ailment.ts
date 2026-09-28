import { z } from "zod";

// Engine channel vocabulary (mechanism, not domain): dot = damage over time,
// slow = strongest-wins speed fraction removed, amp = damage-taken fraction added.
export const AilmentChannelSchema = z.enum(["dot", "slow", "amp"]);

export const AilmentStackRuleSchema = z.enum([
  "refresh_max",
  "strongest_wins",
  "independent",
]);

export const AilmentDefSchema = z.object({
  id: z.string().min(1),
  name: z.string().min(1),
  duration: z.number().positive(),
  magnitude: z.number(),
  stack: AilmentStackRuleSchema,
  channels: z.array(AilmentChannelSchema).min(1),
  move_multiplier: z.number().min(1).optional(),
  max_stacks: z.number().int().optional(),
  min_magnitude: z.number().optional(),
  max_magnitude: z.number().optional(),
  // Optional hit VFX played by the adapter on successful application
  // (must match the runtime VfxType enum; validated at export).
  vfx: z.string().min(1).optional(),
});

export const AilmentsFileSchema = z.object({
  schema: z.literal(1),
  slow_aggregation: z.literal("strongest"),
  amp_applies_to_own_dot: z.boolean(),
  ailments: z.array(AilmentDefSchema).min(1),
});

export type AilmentDef = z.infer<typeof AilmentDefSchema>;
export type AilmentsFile = z.infer<typeof AilmentsFileSchema>;
