import { BossCodexId } from "../ids/codex";
import { EnemyId } from "../ids/enemy";
export * from "../ids/codex";
export * from "../ids/enemy";
import { z } from "zod";

export interface CodexRowDef {
  id: string;
  bio_key: string;
  danger_level: string;
  desc_key: string;
  icon: string;
  name_key: string;
  trait_key: string;
}

export const CodexRowZodSchema = z.object({
  id: z.string().min(1),
  bio_key: z.string().min(1),
  danger_level: z.string().min(1),
  desc_key: z.string().min(1),
  icon: z.string().min(1),
  name_key: z.string().min(1),
  trait_key: z.string().min(1),
});

export const EnemyCodexArrayZodSchema = z.array(CodexRowZodSchema).min(1);
export const BossCodexArrayZodSchema = z.array(CodexRowZodSchema).min(1);
