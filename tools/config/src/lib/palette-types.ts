import { z } from "zod";

/**
 * Represents a single color step in a character or UI palette.
 */
export interface PaletteStep {
  r: number;
  g: number;
  b: number;
  a: number;
  hex: string;
}

export const PaletteStepZodSchema = z.object({
  r: z.number().min(0).max(1),
  g: z.number().min(0).max(1),
  b: z.number().min(0).max(1),
  a: z.number().min(0).max(1),
  hex: z.string().regex(/^#[0-9a-fA-F]{6}$/, "Must be a 6-digit hex color code (#rrggbb)"),
});

export type PaletteDef = readonly PaletteStep[];

export const PaletteDefZodSchema = z.array(PaletteStepZodSchema);

export * from "./colors";
