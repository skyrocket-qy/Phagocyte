import type { AilmentsFile } from "../schemas/ailment";
import type { ActiveSkill } from "../schemas/skill";

export interface ValidationIssue {
  domain: string;
  message: string;
}

export interface CrossValidationResult {
  errors: ValidationIssue[];
  warnings: ValidationIssue[];
}

interface CrossValidationInput {
  ailments: AilmentsFile;
  activeSkills: ActiveSkill[];
}

// Pilot rules: id uniqueness within each dataset + every spawner on_hit
// ailment reference must name a real ailment. Referential failures are
// errors (build-blocking); nothing here warns yet.
export function runCrossValidation(input: CrossValidationInput): CrossValidationResult {
  const errors: ValidationIssue[] = [];
  const warnings: ValidationIssue[] = [];
  const addError = (domain: string, message: string) => errors.push({ domain, message });

  const ailmentIds = new Set<string>();
  for (const a of input.ailments.ailments) {
    if (ailmentIds.has(a.id)) addError("Ailments", `Duplicate ailment id '${a.id}'.`);
    ailmentIds.add(a.id);
  }

  const skillIds = new Set<string>();
  for (const s of input.activeSkills) {
    if (skillIds.has(s.id)) addError("Skills", `Duplicate active skill id '${s.id}'.`);
    skillIds.add(s.id);
    const onHit = (s.params as { on_hit?: Array<{ ailment?: unknown }> }).on_hit;
    if (onHit === undefined) continue;
    for (const entry of onHit) {
      const ref = typeof entry.ailment === "string" ? entry.ailment : "";
      if (!ailmentIds.has(ref)) {
        addError("Skills", `Skill '${s.id}' on_hit references unknown ailment '${ref}'.`);
      }
    }
  }

  return { errors, warnings };
}
