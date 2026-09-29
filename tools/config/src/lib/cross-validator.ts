/**
 * Cross-dataset referential integrity and game balance validation.
 * Run during `pnpm check` and `pnpm build`.
 */

export interface ValidationIssue {
  severity: "error" | "warning";
  domain: string;
  message: string;
}

export function runCrossValidation(data: {
  heroClasses?: any;
  masteryClasses?: any;
  starters?: any;
  enemies?: any;
  bossEncounters?: any;
  gearBases?: any;
  gearUniques?: any;
  gearAffixTiers?: any;
  affixes?: Record<string, any>;
  skills?: any;
  activeGems?: any;
  survivorMaps?: any;
  defenseMaps?: any;
  bgm?: any;
  biomes?: any;
}): { errors: ValidationIssue[]; warnings: ValidationIssue[] } {
  const issues: ValidationIssue[] = [];

  const addError = (domain: string, message: string) =>
    issues.push({ severity: "error", domain, message });
  const addWarning = (domain: string, message: string) =>
    issues.push({ severity: "warning", domain, message });

  const toList = (src: any): any[] => {
    if (!src) return [];
    if (Array.isArray(src)) return src;
    if (typeof src.asArray === "function") return src.asArray();
    if (typeof src === "object") return Object.values(src);
    return [];
  };

  const heroList = toList(data.heroClasses);
  const masteryList = toList(data.masteryClasses);
  const enemyList = toList(data.enemies);
  const bossList = toList(data.bossEncounters);
  const uniqueList = toList(data.gearUniques);
  const baseList = toList(data.gearBases);
  const skillList = toList(data.skills);
  const survivorMapList = toList(data.survivorMaps);
  const defenseMapList = toList(data.defenseMaps);
  const bgmList = toList(data.bgm);
  const starterList = toList(data.starters);
  const activeGemList = toList(data.activeGems);

  const heroIds = new Set(heroList.map((h) => h.id));
  const masteryIds = new Set(masteryList.map((m) => m.id));
  const enemyIds = new Set(enemyList.map((e) => e.id));
  const uniqueIds = new Set(uniqueList.map((u) => u.id));
  const baseIds = new Set(baseList.map((b) => b.id));
  const skillIds = new Set(skillList.map((s) => s.id));
  const activeGemIds = new Set(activeGemList.map((g) => g.id));
  const bgmIds = new Set(bgmList.map((b) => b.id));

  // 1. Hero Classes -> Masteries Integrity
  for (const hero of heroList) {
    if (Array.isArray(hero.masteries)) {
      for (const m of hero.masteries) {
        if (!masteryIds.has(m)) {
          addError("Class", `Hero class '${hero.id}' references unknown mastery '${m}'.`);
        }
      }
    }
  }

  // 2. Class Starters Integrity
  for (const starter of starterList) {
    if (starter.base_class && !heroIds.has(starter.base_class)) {
      addError("Starters", `Starter config references unknown base_class '${starter.base_class}'.`);
    }
    if (starter.starter_weapon && !baseIds.has(starter.starter_weapon)) {
      addError("Starters", `Starter config for '${starter.base_class}' references unknown weapon base '${starter.starter_weapon}'.`);
    }
    if (starter.starter_gem_id && !activeGemIds.has(starter.starter_gem_id)) {
      addError("Starters", `Starter config for '${starter.base_class}' references unknown active gem '${starter.starter_gem_id}'.`);
    }
  }

  // 3. Active Gems -> Skills 1:1 Integrity
  for (const gem of activeGemList) {
    if (!gem.skill_id) {
      addError("Gems", `Active gem '${gem.id}' has no skill_id configured (must map 1:1 to a skill).`);
    } else if (!skillIds.has(gem.skill_id)) {
      addError("Gems", `Active gem '${gem.id}' references unknown skill '${gem.skill_id}'.`);
    }
  }

  // 4. Gear Uniques -> Bases Integrity
  for (const unique of uniqueList) {
    if (unique.base_type && !baseIds.has(unique.base_type)) {
      addError("Gear", `Unique item '${unique.id}' references unknown base_type '${unique.base_type}'.`);
    }
  }

  // 4. Survivor Maps Integrity
  for (const map of survivorMapList) {
    if (map.bgm_id && !bgmIds.has(map.bgm_id)) {
      addError("Map", `Survivor map '${map.name || map.id}' references unknown BGM '${map.bgm_id}'.`);
    }
    if (Array.isArray(map.general_enemies)) {
      for (const enemyId of map.general_enemies) {
        if (!enemyIds.has(enemyId)) {
          addError("Map", `Survivor map '${map.name || map.id}' references unknown enemy '${enemyId}'.`);
        }
      }
    }
    if (Array.isArray(map.waves)) {
      for (let i = 0; i < map.waves.length; i++) {
        const wave = map.waves[i];
        if (Array.isArray(wave?.enemy_ids)) {
          for (const enemyId of wave.enemy_ids) {
            if (!enemyIds.has(enemyId)) {
              addError("Map", `Survivor map '${map.name || map.id}' wave ${i + 1} references unknown enemy '${enemyId}'.`);
            }
          }
        }
        if (wave?.boss?.enemy_id && !enemyIds.has(wave.boss.enemy_id)) {
          addError("Map", `Survivor map '${map.name || map.id}' wave ${i + 1} boss references unknown enemy '${wave.boss.enemy_id}'.`);
        }
      }
    }
  }

  // 5. Defense Maps Integrity
  for (const map of defenseMapList) {
    if (map.bgm_id && !bgmIds.has(map.bgm_id)) {
      addError("Map", `Defense map '${map.name || map.id}' references unknown BGM '${map.bgm_id}'.`);
    }
    if (Array.isArray(map.stages)) {
      for (const stage of map.stages) {
        if (Array.isArray(stage?.enemy_types)) {
          for (const enemyId of stage.enemy_types) {
            if (!enemyIds.has(enemyId)) {
              addError("Map", `Defense map '${map.name || map.id}' stage '${stage.name || stage.stage_index}' references unknown enemy '${enemyId}'.`);
            }
          }
        }
      }
    }
  }

  // 6. Enemy Active Gems Integrity
  for (const enemy of enemyList) {
    if (!Array.isArray(enemy.gems) || enemy.gems.length === 0) {
      addError("Enemy", `Enemy '${enemy.id}' must have at least one active gem configured.`);
    } else {
      for (const g of enemy.gems) {
        if (!g.gem_id || typeof g.gem_id !== "string") {
          addError("Enemy", `Enemy '${enemy.id}' contains invalid gem configuration.`);
        }
      }
    }
  }

  // 7. Boss Encounters & Loot Weight Sanity
  const referencedUniqueIds = new Set<string>();
  for (const boss of bossList) {
    if (boss.base_enemy_type && !enemyIds.has(boss.base_enemy_type)) {
      addError("Enemy", `Boss encounter '${boss.id}' references unknown base_enemy_type '${boss.base_enemy_type}'.`);
    }
    if (Array.isArray(boss.loot_table)) {
      let totalWeight = 0;
      for (const item of boss.loot_table) {
        if (item.weight !== undefined) {
          if (item.weight <= 0) {
            addError("Loot", `Boss encounter '${boss.id}' has non-positive loot weight: ${item.weight} for item '${item.item_id}'.`);
          }
          totalWeight += item.weight;
        }
        if (item.type === "unique" && item.item_id) {
          referencedUniqueIds.add(item.item_id);
          if (!uniqueIds.has(item.item_id)) {
            addError("Loot", `Boss encounter '${boss.id}' drops unknown unique item '${item.item_id}'.`);
          }
        }
      }
      if (boss.loot_table.length > 0 && totalWeight <= 0) {
        addError("Loot", `Boss encounter '${boss.id}' loot table total weight is not positive.`);
      }
    }
  }

  // 7. Unreferenced Master Items Check
  for (const unique of uniqueList) {
    if (!referencedUniqueIds.has(unique.id)) {
      addWarning("Loot", `Master unique item '${unique.id}' is not referenced in any boss encounter loot table.`);
    }
  }

  const errors = issues.filter((i) => i.severity === "error");
  const warnings = issues.filter((i) => i.severity === "warning");

  return { errors, warnings };
}
