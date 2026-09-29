import { z } from "zod";

/**
 * Zod validation schema for Hex color strings (#RRGGBB or #RRGGBBAA).
 */
export const HexColorZodSchema = z
  .string()
  .regex(/^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$/, "Must be a valid hex color code (#RRGGBB or #RRGGBBAA)");

/**
 * Returns a hex color with adjusted alpha opacity.
 *
 * @param hexStr Base hex string (#RRGGBB or #RRGGBBAA).
 * @param alpha Opacity from 0.0 to 1.0.
 * @returns 8-character hex string with alpha.
 */
export function withAlpha(hexStr: string, alpha: number): string {
  const cleaned = hexStr.trim().replace(/^#/, "");
  const baseHex = cleaned.length >= 6 ? cleaned.slice(0, 6) : cleaned.padEnd(6, "0");
  const clampedAlpha = Math.max(0, Math.min(1, alpha));
  const alphaHex = Math.round(clampedAlpha * 255)
    .toString(16)
    .padStart(2, "0")
    .toUpperCase();
  return `#${baseHex.toUpperCase()}${alphaHex}`;
}

/**
 * Pure flat color palette mapping color names directly to Hex string literals.
 */
export const Palette = {
  // ── Golds & Yellows ───────────────────────────────────────────────────
  Gold: "#FFD700",
  BrightYellow: "#F4D03F",
  ElectricGold: "#FFD933",
  PaleGold: "#E0C285",
  WarmGold: "#FFE699",
  SunGold: "#F2CC4D",
  LightningYellow: "#FFEA0040",
  Amber: "#FFB30040",
  WarmAmber: "#AF601A",
  SandGold: "#CC9933",
  DuneGold: "#E6B34D",

  // ── Reds, Oranges & Magentas ──────────────────────────────────────────
  Crimson: "#D32F2F40",
  Scarlet: "#E5393540",
  DarkRed: "#B71C1C40",
  CoralRed: "#E74C3C",
  WarriorRed: "#E6383840",
  RustRed: "#99331A",
  DarkRust: "#260500",
  Orange: "#E67E2240",
  DeepOrange: "#FF572240",
  FireOrange: "#FF3D0040",
  LavaOrange: "#CC4D00",
  Magenta: "#FF80FF",

  // ── Greens, Teals & Cyans ─────────────────────────────────────────────
  ForestGreen: "#388E3C40",
  Emerald: "#388F3D40",
  LimeGreen: "#76FF0340",
  MutedGreen: "#338033",
  MossGreen: "#1A661A",
  FernGreen: "#4D9933",
  DarkMoss: "#051405",
  Teal: "#00968840",
  DeepTeal: "#00968740",
  Cyan: "#00BCD440",
  IceCyan: "#00E5FF40",
  BrightCyan: "#4DD9FF",

  // ── Blues & Purples ───────────────────────────────────────────────────
  RoyalBlue: "#1976D240",
  CobaltBlue: "#1466BF40",
  OceanBlue: "#0288D140",
  SkyBlue: "#3498DB",
  ElectricBlue: "#4D80FF",
  GlacierBlue: "#66CCFF",
  FrostBlue: "#3366CC",
  SteelBlue: "#738CBF",
  DeepFrost: "#1A2633",
  DeepPurple: "#7A1FA340",
  Amethyst: "#9B59B6",
  Purple: "#9C27B040",
  Lavender: "#AB47BC40",
  VoidPurple: "#331A4D",
  AstralPurple: "#663399",

  // ── Grays, Slates & Neutrals ──────────────────────────────────────────
  Gray: "#808080",
  LightGray: "#B3B3B3",
  CoolGray: "#A0A0B0",
  SlateGray: "#607D8B40",
  DarkSlate: "#455A6440",
  MutedSlate: "#6B6A78",
  Charcoal: "#4D4D4D",
  NightBlack: "#0D0D14",
  DarkSand: "#33260D",
  Obsidian: "#030305",
  BastionSlate: "#262E40",
  BastionBlue: "#738CBF",
  BastionDark: "#0D0F1A",
  WarmTaupe: "#A1887F40",
  DarkBronze: "#6B593B",
  Sandstone: "#D4C4A8",
} as const;

export type PaletteKey = keyof typeof Palette;
export type PaletteColor = (typeof Palette)[PaletteKey];
