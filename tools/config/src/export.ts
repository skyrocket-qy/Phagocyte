import path from "node:path";
import { fileURLToPath } from "node:url";
import { runExporter } from "@games/config-framework";
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

runExporter({
  manifest: configManifest,
  validate: () =>
    runCrossValidation({
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
    }),
  outDir: ASSETS_DATA_DIR,
}).catch((err) => {
  console.error("❌ Fatal export failure:", err);
  process.exit(1);
});
