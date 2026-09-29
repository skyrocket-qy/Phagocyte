import { GearId, GearCategory } from "../ids/gear";
export * from "../ids/gear";
export * from "../ids/stat";
import { z } from "zod";

export interface GearModifierDef {
  stat: string;
  unit: string;
  value: number;
  scaling_stat?: string;
  scale_per?: number;
}

export const GearModifierZodSchema = z.object({
  stat: z.string().min(1),
  unit: z.string().min(1),
  value: z.number(),
  scaling_stat: z.string().min(1).optional(),
  scale_per: z.number().optional(),
});

export interface EquipmentDef {
  id: GearId;
  bio_key: string;
  category: GearCategory;
  desc_key: string;
  drawback: readonly GearModifierDef[];
  energy_cost: number;
  icon: string;
  max_copies: number;
  modifiers: readonly GearModifierDef[];
  name_key: string;
}

export const EquipmentZodSchema = z.object({
  id: z.string().min(1),
  bio_key: z.string().min(1),
  category: z.string().min(1),
  desc_key: z.string().min(1),
  drawback: z.array(GearModifierZodSchema),
  energy_cost: z.number().int(),
  icon: z.string().min(1),
  max_copies: z.number().int().positive(),
  modifiers: z.array(GearModifierZodSchema),
  name_key: z.string().min(1),
});

export const EquipmentArrayZodSchema = z.array(EquipmentZodSchema).min(1);
