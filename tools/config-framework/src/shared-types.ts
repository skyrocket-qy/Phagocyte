import { z } from "zod";

/**
 * Generic stat modifier row. The stat vocabulary is game-specific and
 * supplied as a type parameter; at runtime any non-empty string is
 * accepted (vocabulary membership is enforced by the game's own
 * cross-validator, never here).
 */
export interface StatModifier<TStat extends string = string> {
  stat?: TStat;
  Stat?: TStat;
  type?: string;
  Type?: string;
  value?: number;
  Value?: number;
}

export const StatModifierZodSchema = z.object({
  stat: z.string().optional(),
  Stat: z.string().optional(),
  type: z.enum(["flat", "percent", "Flat", "Percent"]).optional(),
  Type: z.enum(["flat", "percent", "Flat", "Percent"]).optional(),
  value: z.number().optional(),
  Value: z.number().optional(),
});

export interface SrgbaColor {
  red: number;
  green: number;
  blue: number;
  alpha: number;
}

export const SrgbaColorZodSchema = z.object({
  red: z.number().min(0).max(1),
  green: z.number().min(0).max(1),
  blue: z.number().min(0).max(1),
  alpha: z.number().min(0).max(1),
});

export type RgbTuple = [number, number, number] | readonly [number, number, number];
export type RgbaTuple = [number, number, number, number] | readonly [number, number, number, number];
export type Vec2Tuple = [number, number] | readonly [number, number];

export const RgbTupleZodSchema = z.tuple([z.number(), z.number(), z.number()]);
export const Vec2TupleZodSchema = z.tuple([z.number(), z.number()]);

export const GodotResourcePathZodSchema = z.string().regex(/^(res:\/\/|[a-zA-Z0-9_\-\/]+\.[a-zA-Z0-9]+)/, {
  message: "Must be a valid resource path (res://... or relative asset path)",
});

export * from "./colors";
