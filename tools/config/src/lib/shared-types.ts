import { StatId, ModType } from "../ids/stat";
export * from "../ids/stat";
import { z } from "zod";

/**
 * Known stat identifiers across the game.
 */
export type StatName = StatId | `${StatId}`;
export const Stats = StatId;

export type ModifierType = ModType | `${ModType}`;

export interface StatModifier {
  stat?: StatName ;
  Stat?: StatName ;
  type?: ModifierType;
  Type?: ModifierType;
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
