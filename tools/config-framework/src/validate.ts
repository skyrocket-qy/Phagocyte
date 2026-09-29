/**
 * Game-agnostic validation harness. Games import these helpers and write
 * only their own rule sections — no rule logic lives here.
 */

export interface ValidationIssue {
  severity: "error" | "warning";
  domain: string;
  message: string;
}

export interface ValidationResult {
  errors: ValidationIssue[];
  warnings: ValidationIssue[];
}

/** Normalize a dataset to a row array (plain array, DataTable, or map). */
export function toList(src: any): any[] {
  if (!src) return [];
  if (Array.isArray(src)) return src;
  if (typeof src.asArray === "function") return src.asArray();
  if (typeof src === "object") return Object.values(src);
  return [];
}

/** Value set of a string enum (for vocabulary-membership checks). */
export function enumValues(e: Record<string, string>): Set<string> {
  return new Set(Object.values(e));
}

export interface IdRow {
  id?: unknown;
}

/**
 * Assert per-row id presence + uniqueness. Appends to `issues` and
 * returns the seen id set for FK checks.
 */
export function checkDupes(
  issues: ValidationIssue[],
  domain: string,
  kind: string,
  list: readonly IdRow[],
): Set<string> {
  const seen = new Set<string>();
  for (const row of list) {
    if (typeof row?.id !== "string" || row.id.length === 0) {
      issues.push({ severity: "error", domain, message: `A ${kind} row is missing its id.` });
    } else if (seen.has(row.id)) {
      issues.push({ severity: "error", domain, message: `Duplicate ${kind} id '${row.id}'.` });
    } else {
      seen.add(row.id);
    }
  }
  return seen;
}

/** Split mixed issues into a result (errors block, warnings don't). */
export function splitResult(issues: ValidationIssue[]): ValidationResult {
  return {
    errors: issues.filter((i) => i.severity === "error"),
    warnings: issues.filter((i) => i.severity === "warning"),
  };
}
