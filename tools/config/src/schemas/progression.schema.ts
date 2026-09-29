import { AchievementId, AchievementStatKey } from "../ids/achievement";
export * from "../ids/achievement";
export * from "../ids/class";
export * from "../ids/stage";
import { z } from "zod";

export interface AchievementDef {
  id: AchievementId;
  desc_key: string;
  icon: string;
  reward_cell: string;
  reward_key: string;
  stat_key: string;
  target_value: number;
  title_key: string;
  difficulty?: string;
  stage_id?: string;
  talent_points?: number;
  unlock_hard_stage?: string;
  unlock_stage?: string;
  unlock_endless?: boolean;
  [key: string]: unknown;
}

export const AchievementZodSchema = z.object({
  id: z.string().min(1),
  desc_key: z.string().min(1),
  icon: z.string().min(1),
  reward_cell: z.string(),
  reward_key: z.string(),
  stat_key: z.string().min(1),
  target_value: z.number(),
  title_key: z.string().min(1),
  difficulty: z.string().min(1).optional(),
  stage_id: z.string().min(1).optional(),
  talent_points: z.number().int().nonnegative().optional(),
  unlock_hard_stage: z.string().min(1).optional(),
  unlock_stage: z.string().min(1).optional(),
  unlock_endless: z.boolean().optional(),
}).passthrough();

export const AchievementArrayZodSchema = z.array(AchievementZodSchema).min(1);
