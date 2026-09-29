export * from "./ids";
import type { ZodTypeAny } from "zod";
import { AilmentsFileZodSchema } from "./schemas/ailment.schema";
import { ActiveSkillArrayZodSchema, PassiveSkillArrayZodSchema } from "./schemas/skill.schema";
import { ClassArrayZodSchema } from "./schemas/class.schema";
import { EnemyArrayZodSchema } from "./schemas/enemy.schema";
import { EnemyCodexArrayZodSchema, BossCodexArrayZodSchema } from "./schemas/codex.schema";
import { EquipmentArrayZodSchema } from "./schemas/gear.schema";
import { TraitsFileZodSchema, TreeFileZodSchema } from "./schemas/passive.schema";
import { StageArrayZodSchema } from "./schemas/stage.schema";
import { AchievementArrayZodSchema } from "./schemas/progression.schema";
import { UiElementArrayZodSchema, StatLabelsFileZodSchema } from "./schemas/ui.schema";

import { Ailments } from "./data/combat/ailments";
import { StatLabels } from "./data/combat/stats";
import { ActiveSkills } from "./data/skills/active";
import { PassiveSkills } from "./data/skills/passive";
import { Classes } from "./data/class/classes";
import { Enemies } from "./data/enemy/enemies";
import { EnemyCodex } from "./data/codex/enemy_codex";
import { BossCodex } from "./data/codex/boss_codex";
import { Equipment } from "./data/gear/equipment";
import { Traits } from "./data/passive/traits";
import { Tree } from "./data/passive/tree";
import { Stages } from "./data/map/stages";
import { Achievements } from "./data/progression/achievements";
import { UiElements } from "./data/ui/elements";

export interface ManifestEntry {
  file: string;
  data: unknown;
  schema?: ZodTypeAny;
}

export const configManifest: ManifestEntry[] = [
  { file: "ailments.json", data: Ailments, schema: AilmentsFileZodSchema },
  { file: "skill/active.json", data: ActiveSkills, schema: ActiveSkillArrayZodSchema },
  { file: "skill/passive.json", data: PassiveSkills, schema: PassiveSkillArrayZodSchema },
  { file: "player_classes.json", data: Classes, schema: ClassArrayZodSchema },
  { file: "enemies.json", data: Enemies, schema: EnemyArrayZodSchema },
  { file: "enemy_codex.json", data: EnemyCodex, schema: EnemyCodexArrayZodSchema },
  { file: "boss_codex.json", data: BossCodex, schema: BossCodexArrayZodSchema },
  { file: "equipment.json", data: Equipment, schema: EquipmentArrayZodSchema },
  { file: "passive_traits.json", data: Traits, schema: TraitsFileZodSchema },
  { file: "passive_tree.json", data: Tree, schema: TreeFileZodSchema },
  { file: "stages.json", data: Stages, schema: StageArrayZodSchema },
  { file: "achievements.json", data: Achievements, schema: AchievementArrayZodSchema },
  { file: "stat_labels.json", data: StatLabels, schema: StatLabelsFileZodSchema },
  { file: "ui.json", data: UiElements, schema: UiElementArrayZodSchema },
];

// Re-export all data modules for easy import
export * from "./data/combat/ailments";
export * from "./data/combat/stats";
export * from "./data/skills/active";
export * from "./data/skills/passive";
export * from "./data/class/classes";
export * from "./data/enemy/enemies";
export * from "./data/codex/enemy_codex";
export * from "./data/codex/boss_codex";
export * from "./data/gear/equipment";
export * from "./data/passive/traits";
export * from "./data/passive/tree";
export * from "./data/map/stages";
export * from "./data/progression/achievements";
export * from "./data/ui/elements";
export * from "./schemas/ailment.schema";
export * from "./schemas/skill.schema";
export * from "./schemas/class.schema";
export * from "./schemas/enemy.schema";
export * from "./schemas/codex.schema";
export * from "./schemas/gear.schema";
export * from "./schemas/passive.schema";
export * from "./schemas/stage.schema";
export * from "./schemas/progression.schema";
export * from "./schemas/ui.schema";
