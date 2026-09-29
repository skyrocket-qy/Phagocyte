import { AilmentId, AilmentChannel, AilmentStackRule } from "../ids/ailment";
export * from "../ids/ailment";
import { z } from "zod";

export interface AilmentDef {
  id: AilmentId;
  name: string;
  duration: number;
  magnitude: number;
  stack: AilmentStackRule;
  channels: readonly AilmentChannel[];
  move_multiplier?: number;
  max_stacks?: number;
  min_magnitude?: number;
  max_magnitude?: number;
  vfx?: string;
}

export const AilmentDefZodSchema = z.object({
  id: z.string().min(1),
  name: z.string().min(1),
  duration: z.number().positive(),
  magnitude: z.number(),
  stack: z.string().min(1),
  channels: z.array(z.string().min(1)).min(1),
  move_multiplier: z.number().min(1).optional(),
  max_stacks: z.number().int().optional(),
  min_magnitude: z.number().optional(),
  max_magnitude: z.number().optional(),
  vfx: z.string().min(1).optional(),
});

export interface AilmentsFile {
  schema: 1;
  slow_aggregation: "strongest";
  amp_applies_to_own_dot: boolean;
  ailments: readonly AilmentDef[];
}

export const AilmentsFileZodSchema = z.object({
  schema: z.literal(1),
  slow_aggregation: z.literal("strongest"),
  amp_applies_to_own_dot: z.boolean(),
  ailments: z.array(AilmentDefZodSchema).min(1),
});
