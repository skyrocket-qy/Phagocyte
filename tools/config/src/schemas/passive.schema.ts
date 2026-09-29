import { TraitId, TraitRarity, TraitBranch } from "../ids/trait";
import { ClassId } from "../ids/class";
export * from "../ids/trait";
export * from "../ids/class";
export * from "../ids/stat";
import { z } from "zod";

export interface TraitModifierDef {
  stat: string;
  unit: string;
  value: number;
}

export const TraitModifierZodSchema = z.object({
  stat: z.string().min(1),
  unit: z.string().min(1),
  value: z.number(),
});

export interface TraitDef {
  id: TraitId;
  bio_key?: string;
  desc_key: string;
  icon: string;
  modifiers: readonly TraitModifierDef[];
  name_key: string;
  rarity: TraitRarity;
}

export const TraitZodSchema = z.object({
  id: z.string().min(1),
  bio_key: z.string().optional(),
  desc_key: z.string(),
  icon: z.string().min(1),
  modifiers: z.array(TraitModifierZodSchema),
  name_key: z.string().min(1),
  rarity: z.string().min(1),
});

export interface TraitsFile {
  traits: readonly TraitDef[];
}

export const TraitsFileZodSchema = z.object({
  traits: z.array(TraitZodSchema).min(1),
});

export interface TreeNodeDef {
  id: string;
  trait: string;
  branch: string;
  row: number;
  col: number;
}

export const TreeNodeZodSchema = z.object({
  id: z.string().min(1),
  trait: z.string().min(1),
  branch: z.string().min(1),
  row: z.number().int(),
  col: z.number().int(),
});

export interface TreeRegionDef {
  branch: string;
  row: number;
  col: number;
}

export const TreeRegionZodSchema = z.object({
  branch: z.string().min(1),
  row: z.number().int(),
  col: z.number().int(),
});

export interface TreeFile {
  edges: readonly (readonly [string, string])[];
  nodes: readonly TreeNodeDef[];
  regions: readonly TreeRegionDef[];
  start_nodes: Record<string, string>;
}

export const TreeFileZodSchema = z.object({
  edges: z.array(z.tuple([z.string().min(1), z.string().min(1)])).min(1),
  nodes: z.array(TreeNodeZodSchema).min(1),
  regions: z.array(TreeRegionZodSchema),
  start_nodes: z.record(z.string(), z.string().min(1)),
});
