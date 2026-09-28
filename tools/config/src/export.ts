import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { runCrossValidation } from "./lib/cross-validator";
import { configManifest, Ailments, ActiveSkills } from "./index";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Target export directory: assets/data/ in repo root
const ASSETS_DATA_DIR = path.resolve(__dirname, "../../../assets/data");

const isCheckOnly = process.argv.includes("--check");

async function runExporter() {
  console.log(`Validating${isCheckOnly ? "" : " & exporting"} configuration data...`);
  console.log(`Target directory: ${ASSETS_DATA_DIR}`);

  let errors = 0;
  let successCount = 0;

  // 1. Cross-dataset referential integrity (on_hit ailment FKs, dup ids)
  const crossResult = runCrossValidation({
    ailments: Ailments,
    activeSkills: ActiveSkills,
  });

  for (const w of crossResult.warnings) {
    console.log(`  warning [${w.domain}] ${w.message}`);
  }
  for (const e of crossResult.errors) {
    console.error(`  error [${e.domain}] ${e.message}`);
  }
  errors += crossResult.errors.length;

  // 2. Per-dataset schema validation + export
  for (const entry of configManifest) {
    const targetFile = path.join(ASSETS_DATA_DIR, entry.file);
    const result = entry.schema.safeParse(entry.data);
    if (!result.success) {
      console.error(`Validation failed for [${entry.file}]:`);
      const issues = result.error?.issues ?? [];
      for (const issue of issues) {
        const at = Array.isArray(issue.path) ? issue.path.join(".") : "root";
        console.error(`  - ${at || "root"}: ${issue.message}`);
      }
      errors++;
      continue;
    }
    if (!isCheckOnly) {
      try {
        await fs.mkdir(path.dirname(targetFile), { recursive: true });
        await fs.writeFile(targetFile, JSON.stringify(entry.data, null, 2) + "\n", "utf-8");
        successCount++;
      } catch (err) {
        console.error(`Failed to write [${entry.file}]:`, err);
        errors++;
      }
    } else {
      successCount++;
    }
  }

  if (errors > 0) {
    console.error(`Finished with ${errors} error(s).`);
    process.exit(1);
  }
  console.log(
    isCheckOnly
      ? `Check passed: all ${successCount} datasets and referential constraints valid.`
      : `Export complete: ${successCount} JSON files written to assets/data/.`,
  );
}

runExporter().catch((err) => {
  console.error("Fatal export failure:", err);
  process.exit(1);
});
