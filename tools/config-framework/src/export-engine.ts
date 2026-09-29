import fs from "node:fs/promises";
import path from "node:path";
import type { ZodTypeAny } from "zod";
import type { ValidationResult } from "./validate";

export interface ManifestEntry {
  file: string;
  data: unknown;
  schema?: ZodTypeAny;
}

export interface ExporterOptions {
  manifest: ManifestEntry[];
  /** Game's cross-dataset check. Receives no engine state — close over datasets. */
  validate: () => ValidationResult;
  outDir: string;
}

/** Validate every manifest entry (+ cross-checks) and write JSON, or `--check`. */
export async function runExporter({ manifest, validate, outDir }: ExporterOptions): Promise<void> {
  const isCheckOnly = process.argv.includes("--check");

  console.log(`⏳ ${isCheckOnly ? "Validating" : "Validating & Exporting"} configuration data...`);
  console.log(`📂 Target directory: ${outDir}`);

  let errors = 0;
  let successCount = 0;

  // 1. Cross-Dataset Referential Integrity & Balance Validation
  const crossResult = validate();

  if (crossResult.warnings.length > 0) {
    console.log(`\n⚠️ Referential Warnings (${crossResult.warnings.length}):`);
    for (const w of crossResult.warnings) {
      console.log(`   • [${w.domain}] ${w.message}`);
    }
  }

  if (crossResult.errors.length > 0) {
    console.error(`\n❌ Referential Errors (${crossResult.errors.length}):`);
    for (const e of crossResult.errors) {
      console.error(`   • [${e.domain}] ${e.message}`);
    }
    errors += crossResult.errors.length;
  }

  // 2. Individual Dataset Schema Validation & Export
  for (const entry of manifest) {
    const targetFile = path.join(outDir, entry.file);

    // Resolve data (extract array from DataTable if defined via defineTable)
    const exportData =
      entry.data &&
      typeof entry.data === "object" &&
      "asArray" in entry.data &&
      typeof (entry.data as { asArray?: unknown }).asArray === "function"
        ? ((entry.data as { asArray: () => unknown }).asArray)()
        : entry.data;

    // Schema Validation
    if (entry.schema) {
      const result = entry.schema.safeParse(exportData);
      if (!result.success) {
        console.error(`❌ Validation failed for [${entry.file}]:`);
        for (const issue of result.error.issues) {
          console.error(`   • ${issue.path.join(".") || "root"}: ${issue.message}`);
        }
        errors++;
        continue;
      }
    }

    // Export file if not check-only
    if (!isCheckOnly) {
      try {
        await fs.mkdir(path.dirname(targetFile), { recursive: true });
        const jsonContent = JSON.stringify(exportData, null, 2) + "\n";
        await fs.writeFile(targetFile, jsonContent, "utf-8");
        successCount++;
      } catch (err) {
        console.error(`❌ Failed to write [${entry.file}]:`, err);
        errors++;
      }
    } else {
      successCount++;
    }
  }

  if (errors > 0) {
    console.error(`\n💥 Finished with ${errors} error(s).`);
    process.exit(1);
  }

  if (isCheckOnly) {
    console.log(`\n✅ Check passed: All ${successCount} configuration datasets and referential constraints are valid.`);
  } else {
    console.log(`\n🎉 Export complete: Successfully generated ${successCount} JSON files.`);
  }
}
