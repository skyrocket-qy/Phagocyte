import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { runCrossValidation } from "./lib/cross-validator";
import {
  configManifest,
  Ailments,
  ActiveSkills,
  PassiveSkills,
  Classes,
  Enemies,
  EnemyCodex,
  BossCodex,
  Equipment,
  Traits,
  Tree,
  Stages,
  Achievements,
  StatLabels,
  UiElements,
} from "./index";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Target export directory: assets/data/ in repo root
const ASSETS_DATA_DIR = path.resolve(__dirname, "../../../assets/data");

const isCheckOnly = process.argv.includes("--check");

async function runExporter() {
  console.log(`⏳ ${isCheckOnly ? "Validating" : "Validating & Exporting"} configuration data...`);
  console.log(`📂 Target directory: ${ASSETS_DATA_DIR}`);

  let errors = 0;
  let successCount = 0;

  // 1. Cross-Dataset Referential Integrity & Balance Validation
  const crossResult = runCrossValidation({
    ailments: Ailments,
    activeSkills: ActiveSkills,
    passiveSkills: PassiveSkills,
    classes: Classes,
    enemies: Enemies,
    enemyCodex: EnemyCodex,
    bossCodex: BossCodex,
    equipment: Equipment,
    traits: Traits,
    tree: Tree,
    stages: Stages,
    achievements: Achievements,
    statLabels: StatLabels,
    uiElements: UiElements,
  });

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
  for (const entry of configManifest) {
    const targetFile = path.join(ASSETS_DATA_DIR, entry.file);

    // Resolve data (extract array from DataTable if defined via defineTable)
    const exportData =
      entry.data &&
      typeof entry.data === "object" &&
      "asArray" in entry.data &&
      typeof (entry.data as any).asArray === "function"
        ? (entry.data as any).asArray()
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
    console.log(`\n🎉 Export complete: Successfully generated ${successCount} JSON files in assets/data/.`);
  }
}

runExporter().catch((err) => {
  console.error("❌ Fatal export failure:", err);
  process.exit(1);
});
