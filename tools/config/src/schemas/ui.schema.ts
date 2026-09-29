import { UiId } from "../ids/ui";
import { StatId } from "../ids/stat";
export * from "../ids/ui";
export * from "../ids/stat";
import { z } from "zod";

export interface UiElementDef {
  id: UiId;
}

export const UiElementZodSchema = z.object({
  id: z.string().min(1),
});

export const UiElementArrayZodSchema = z.array(UiElementZodSchema).min(1);

export const StatLabelsFileZodSchema = z.record(z.string().min(1), z.string().min(1));
export type StatLabelsFile = Record<string, string>;
