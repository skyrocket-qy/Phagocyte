import { ClassId } from "../ids/class";
export * from "../ids/class";
export * from "../ids/skill";
export * from "../ids/stat";
export * from "../ids/achievement";
import { z } from "zod";

const HexColorZodSchema = z.string().regex(/^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$/, {
  message: "Must be a valid hex color (#rrggbb or #rrggbbaa)",
});

export interface ClassDef {
  id: ClassId;
  name_key: string;
  role_key: string;
  scene_path: string;
  trait_key: string;
  bio_key: string;
  unlock_achievement: string;
  unlocked: boolean;
  base_hp: number;
  base_speed: number;
  base_armor: number;
  trait_stat: string;
  trait_stat_value: number;
  extra_stats: Record<string, number>;
  body_microns: number;
  deform_mag: number;
  deform_speed: number;
  deform_kind: string;
  deform_arms: number;
  noise_freq: number;
  noise_octaves: number;
  cyto_color: string;
  membrane_color: string;
  nucleus_color: string;
  nucleus_points: number;
  nucleus_radius: number;
  nucleus_kind: string;
  nucleus_amp: number;
  nucleus_freq: number;
  innate_skill: string;
  innate_slot: number;
}

export const ClassZodSchema = z.object({
  id: z.string().min(1),
  name_key: z.string().min(1),
  role_key: z.string().min(1),
  scene_path: z.string().min(1),
  trait_key: z.string().min(1),
  bio_key: z.string().min(1),
  unlock_achievement: z.string(),
  unlocked: z.boolean(),
  base_hp: z.number().positive(),
  base_speed: z.number().positive(),
  base_armor: z.number(),
  trait_stat: z.string().min(1),
  trait_stat_value: z.number(),
  extra_stats: z.record(z.string(), z.number()),
  body_microns: z.number().positive(),
  deform_mag: z.number(),
  deform_speed: z.number(),
  deform_kind: z.string().min(1),
  deform_arms: z.number().int().nonnegative(),
  noise_freq: z.number(),
  noise_octaves: z.number().int().nonnegative(),
  cyto_color: HexColorZodSchema,
  membrane_color: HexColorZodSchema,
  nucleus_color: HexColorZodSchema,
  nucleus_points: z.number().int().positive(),
  nucleus_radius: z.number().positive(),
  nucleus_kind: z.string().min(1),
  nucleus_amp: z.number(),
  nucleus_freq: z.number(),
  innate_skill: z.string().min(1),
  innate_slot: z.number().int().nonnegative(),
});

export const ClassArrayZodSchema = z.array(ClassZodSchema).min(1);
