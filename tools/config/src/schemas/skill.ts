import { z } from "zod";

// One on-hit effect entry inside a skill row's params. The ailment id is data:
// the archetype executor passes it straight into EffectSpec/Ailments.Apply
// without ever naming an ailment in code. magnitude = mult != null ? dmg * mult : flat
// (both absent = def defaults, e.g. pure mark). duration absent = def default.
export const OnHitEntrySchema = z.object({
  ailment: z.string().min(1),
  mult: z.number().positive().optional(),
  flat: z.number().optional(),
  duration: z.number().positive().optional(),
});

export const ActiveSkillParamsSchema = z
  .object({
    on_hit: z.array(OnHitEntrySchema).max(3).optional(),
  })
  .catchall(z.unknown());

export const ActiveSkillSchema = z.object({
  bio_key: z.string().min(1),
  class_id: z.string(),
  cooldown: z.number().positive(),
  cooldown_per_level: z.array(z.number().positive()).min(1),
  damage_per_level: z.array(z.number().positive()).min(1),
  desc_key: z.string().min(1),
  icon: z.string().min(1),
  id: z.string().min(1),
  max_level: z.number().int().positive(),
  name_key: z.string().min(1),
  tags: z.array(z.string().min(1)),
  type: z.enum(["innate", "active"]),
  archetype: z.string().min(1),
  params: ActiveSkillParamsSchema,
});

export const ActiveSkillsFileSchema = z.array(ActiveSkillSchema).min(1);

export type OnHitEntry = z.infer<typeof OnHitEntrySchema>;
export type ActiveSkill = z.infer<typeof ActiveSkillSchema>;
