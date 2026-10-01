import { EnemyId, ThreatMode } from "../ids/enemy";
export * from "../ids/enemy";
import { z } from "zod";

export interface SpawnClusterDef {
  count: number;
  spread: number;
}

export const SpawnClusterZodSchema = z.object({
  count: z.number().int().positive(),
  spread: z.number(),
});

export interface EnemyDef {
  id: EnemyId;
  max_health: number;
  armor: number;
  xp: number;
  score: number;
  float_speed: number;
  contact_damage: number;
  threat_mode: ThreatMode;
  elite: boolean;
  boss: boolean;
  body_microns: number;
  tint: string;
  spawn_cluster?: SpawnClusterDef;
  steering?: Record<string, unknown>;
  damage_mult?: number;
  batch_variant?: unknown;
  telegraph_scale?: number;
  telegraph_damage?: number;
  traits: Record<string, unknown>;
  [key: string]: unknown;
}

export const EnemyZodSchema = z.object({
  id: z.string().min(1),
  max_health: z.number().positive(),
  armor: z.number(),
  xp: z.number(),
  score: z.number().int().nonnegative(),
  float_speed: z.number(),
  contact_damage: z.number(),
  threat_mode: z.string().min(1),
  elite: z.boolean(),
  boss: z.boolean(),
  body_microns: z.number().positive(),
  tint: z.string(),
  spawn_cluster: SpawnClusterZodSchema.optional(),
  steering: z.record(z.string(), z.unknown()).optional(),
  damage_mult: z.number().optional(),
  batch_variant: z.unknown().optional(),
  telegraph_scale: z.number().optional(),
  telegraph_damage: z.number().optional(),
  traits: z.record(z.string(), z.unknown()),
}).passthrough();

export const EnemyArrayZodSchema = z.array(EnemyZodSchema).min(1);
