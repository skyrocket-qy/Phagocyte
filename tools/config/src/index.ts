import type { z } from "zod";
import { AilmentsFileSchema } from "./schemas/ailment";
import { ActiveSkillsFileSchema } from "./schemas/skill";
import { Ailments } from "./data/ailments";
import { ActiveSkills } from "./data/skills/active";

export interface ManifestEntry {
  file: string;
  data: unknown;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  schema: { safeParse: (data: unknown) => { success: boolean; error?: any } };
}

export const configManifest: ManifestEntry[] = [
  { file: "ailments.json", data: Ailments, schema: AilmentsFileSchema },
  { file: "skill/active.json", data: ActiveSkills, schema: ActiveSkillsFileSchema },
];

export { Ailments, ActiveSkills, AilmentsFileSchema, ActiveSkillsFileSchema };
export type { z };
