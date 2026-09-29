import { StageId, StageEffectKind, StageProp } from "../ids/stage";
export * from "../ids/stage";
export * from "../ids/stat";
import { z } from "zod";

const HexColorZodSchema = z.string().regex(/^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$/, {
  message: "Must be a valid hex color (#rrggbb or #rrggbbaa)",
});

export interface StageDef {
  id: StageId;
  bg_color: string;
  bg_color_accent: string;
  bg_color_deep: string;
  bio_key: string;
  color_code: string;
  difficulty: number;
  env_key: string;
  fiber_color: string;
  hard_unlocked: boolean;
  effects: readonly Record<string, unknown>[];
  mech_key: string;
  name_key: string;
  organ_icon: string;
  organ_key: string;
  scanner_pos: readonly [number, number];
  subtitle_key: string;
  threat_key: string;
  unlocked: boolean;
}

export const StageZodSchema = z.object({
  id: z.string().min(1),
  bg_color: HexColorZodSchema,
  bg_color_accent: HexColorZodSchema,
  bg_color_deep: HexColorZodSchema,
  bio_key: z.string().min(1),
  color_code: HexColorZodSchema,
  difficulty: z.number().int().positive(),
  env_key: z.string().min(1),
  fiber_color: HexColorZodSchema,
  hard_unlocked: z.boolean(),
  effects: z.array(z.record(z.string(), z.unknown())),
  mech_key: z.string().min(1),
  name_key: z.string().min(1),
  organ_icon: z.string().min(1),
  organ_key: z.string().min(1),
  scanner_pos: z.tuple([z.number(), z.number()]),
  subtitle_key: z.string().min(1),
  threat_key: z.string().min(1),
  unlocked: z.boolean(),
});

export const StageArrayZodSchema = z.array(StageZodSchema).min(1);
