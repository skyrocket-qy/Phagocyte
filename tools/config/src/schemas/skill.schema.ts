import { SkillId, SkillArchetype, SkillType } from "../ids/skill";
export * from "../ids/skill";
export * from "../ids/ailment";
export * from "../ids/audio";
export * from "../ids/stat";
import { z } from "zod";

export interface OnHitEntry {
  ailment: string;
  mult?: number;
  flat?: number;
  duration?: number;
}

export const OnHitEntryZodSchema = z.object({
  ailment: z.string().min(1),
  mult: z.number().positive().optional(),
  flat: z.number().optional(),
  duration: z.number().positive().optional(),
});

export interface ActiveSkillDef {
  id: SkillId;
  bio_key: string;
  class_id: string;
  cooldown: number;
  cooldown_per_level: readonly number[];
  damage_per_level: readonly number[];
  desc_key: string;
  icon: string;
  max_level: number;
  name_key: string;
  tags: readonly string[];
  type: SkillType;
  archetype: SkillArchetype;
  params: Record<string, unknown>;
}

export const ActiveSkillZodSchema = z.object({
  id: z.string().min(1),
  bio_key: z.string().min(1),
  class_id: z.string(),
  cooldown: z.number().positive(),
  cooldown_per_level: z.array(z.number().positive()).min(1),
  damage_per_level: z.array(z.number().positive()).min(1),
  desc_key: z.string().min(1),
  icon: z.string().min(1),
  max_level: z.number().int().positive(),
  name_key: z.string().min(1),
  tags: z.array(z.string().min(1)),
  type: z.string().min(1),
  archetype: z.string().min(1),
  params: z.object({ on_hit: z.array(OnHitEntryZodSchema).max(3).optional() }).catchall(z.unknown()),
});

export const ActiveSkillArrayZodSchema = z.array(ActiveSkillZodSchema).min(1);

export interface PassiveModDef {
  stat: string;
  per_level: number;
  mode: string;
}

export const PassiveModZodSchema = z.object({
  stat: z.string().min(1),
  per_level: z.number(),
  mode: z.string().min(1),
});

export interface PassiveSkillDef {
  id: SkillId;
  bio_key: string;
  class_id: string;
  cooldown: number;
  desc_key: string;
  icon: string;
  max_level: number;
  name_key: string;
  type: string;
  archetype: string;
  mods: readonly PassiveModDef[];
}

export const PassiveSkillZodSchema = z.object({
  id: z.string().min(1),
  bio_key: z.string().min(1),
  class_id: z.string(),
  cooldown: z.number(),
  desc_key: z.string().min(1),
  icon: z.string().min(1),
  max_level: z.number().int().positive(),
  name_key: z.string().min(1),
  type: z.string().min(1),
  archetype: z.string().min(1),
  mods: z.array(PassiveModZodSchema),
});

export const PassiveSkillArrayZodSchema = z.array(PassiveSkillZodSchema).min(1);
