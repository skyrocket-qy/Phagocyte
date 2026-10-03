/**
 * Cross-dataset referential integrity and game balance validation.
 * Run during `pnpm check` and `pnpm build`.
 */
import {
  toList,
  enumValues,
  checkDupes,
  splitResult,
  type ValidationIssue,
  type ValidationResult,
} from "@games/config-framework";
import { SfxId } from "../ids/audio";
import { StatId, ModType, PassiveModMode } from "../ids/stat";
import { GearCategory } from "../ids/gear";
import { TraitRarity, TraitBranch } from "../ids/trait";
import { ThreatMode } from "../ids/enemy";
import { SkillArchetype, SkillType } from "../ids/skill";
import { StageEffectKind, StageProp } from "../ids/stage";

export function runCrossValidation(data: {
  ailments?: any;
  activeSkills?: any;
  passiveSkills?: any;
  classes?: any;
  enemies?: any;
  enemyCodex?: any;
  bossCodex?: any;
  equipment?: any;
  traits?: any;
  tree?: any;
  stages?: any;
  achievements?: any;
  statLabels?: any;
  uiElements?: any;
}): ValidationResult {
  const issues: ValidationIssue[] = [];

  const addError = (domain: string, message: string) =>
    issues.push({ severity: "error", domain, message });
  const addWarning = (domain: string, message: string) =>
    issues.push({ severity: "warning", domain, message });

  const statIds = enumValues(StatId as unknown as Record<string, string>);
  const sfxIds = enumValues(SfxId as unknown as Record<string, string>);
  const unitIds = enumValues(ModType as unknown as Record<string, string>);
  const modModeIds = enumValues(PassiveModMode as unknown as Record<string, string>);
  const gearCategories = enumValues(GearCategory as unknown as Record<string, string>);
  const traitRarities = enumValues(TraitRarity as unknown as Record<string, string>);
  const traitBranches = enumValues(TraitBranch as unknown as Record<string, string>);
  const threatModes = enumValues(ThreatMode as unknown as Record<string, string>);
  const archetypes = enumValues(SkillArchetype as unknown as Record<string, string>);
  const skillTypes = enumValues(SkillType as unknown as Record<string, string>);
  const effectKinds = enumValues(StageEffectKind as unknown as Record<string, string>);
  const stageProps = enumValues(StageProp as unknown as Record<string, string>);

  const ailmentList = toList((data.ailments as any)?.ailments ?? data.ailments);
  const activeList = toList(data.activeSkills);
  const passiveList = toList(data.passiveSkills);
  const classList = toList(data.classes);
  const enemyList = toList(data.enemies);
  const enemyCodexList = toList(data.enemyCodex);
  const bossCodexList = toList(data.bossCodex);
  const equipmentList = toList(data.equipment);
  const traitList = toList((data.traits as any)?.traits ?? data.traits);
  const stageList = toList(data.stages);
  const achievementList = toList(data.achievements);
  const uiList = toList(data.uiElements);
  const tree = (data.tree as any) ?? {};
  const nodeList: any[] = Array.isArray(tree.nodes) ? tree.nodes : [];
  const edgeList: any[] = Array.isArray(tree.edges) ? tree.edges : [];
  const startNodes: Record<string, string> =
    tree.start_nodes && typeof tree.start_nodes === "object" ? tree.start_nodes : {};

  const ailmentIds = checkDupes(issues, "Ailments", "ailment", ailmentList);
  const activeIds = checkDupes(issues, "Skills", "active skill", activeList);
  const passiveIds = checkDupes(issues, "Skills", "passive skill", passiveList);
  const classIds = checkDupes(issues, "Class", "player class", classList);
  const enemyIds = checkDupes(issues, "Enemy", "enemy", enemyList);
  const enemyCodexIds = checkDupes(issues, "Codex", "enemy codex row", enemyCodexList);
  const bossCodexIds = checkDupes(issues, "Codex", "boss codex row", bossCodexList);
  const equipmentIds = checkDupes(issues, "Gear", "equipment", equipmentList);
  const traitIds = checkDupes(issues, "Passive", "trait", traitList);
  const nodeIds = checkDupes(issues, "Passive", "tree node", nodeList);
  const stageIds = checkDupes(issues, "Stage", "stage", stageList);
  const achievementIds = checkDupes(issues, "Achievement", "achievement", achievementList);
  const uiIds = checkDupes(issues, "Ui", "ui element", uiList);
  void enemyCodexIds;
  void bossCodexIds;
  void uiIds;

  const skillIds = new Set([...activeIds, ...passiveIds]);

  // 1. Ailments: kind + stack vocab
  for (const a of ailmentList) {
    for (const c of a.kinds ?? []) {
      if (c !== "dot" && c !== "slow" && c !== "amp" && c !== "stun") {
        addError("Ailments", `Ailment '${a.id}' has unknown kind '${c}'.`);
      }
    }
    if (!["refresh_max", "strongest_wins", "independent"].includes(a.stack)) {
      addError("Ailments", `Ailment '${a.id}' has unknown stack rule '${a.stack}'.`);
    }
  }

  // 2. Skills -> classes / ailments / sfx / vocab
  for (const s of [...activeList, ...passiveList]) {
    if (s.class_id && !classIds.has(s.class_id)) {
      addError("Skills", `Skill '${s.id}' references unknown class '${s.class_id}'.`);
    }
    if (s.archetype && !archetypes.has(s.archetype)) {
      addError("Skills", `Skill '${s.id}' has unknown archetype '${s.archetype}'.`);
    }
    if (s.type && !skillTypes.has(s.type)) {
      addError("Skills", `Skill '${s.id}' has unknown type '${s.type}'.`);
    }
    const onHit = (s.params as { on_hit?: Array<{ ailment?: unknown }> } | undefined)?.on_hit;
    if (onHit !== undefined) {
      for (const entry of onHit) {
        const ref = typeof entry.ailment === "string" ? entry.ailment : "";
        if (!ailmentIds.has(ref)) {
          addError("Skills", `Skill '${s.id}' on_hit references unknown ailment '${ref}'.`);
        }
      }
    }
    const params = (s.params ?? {}) as Record<string, unknown>;
    const dmgType = params.damage_type;
    if (dmgType !== undefined && (typeof dmgType !== "string" || !["physical", "fire", "cold", "lightning", "chaos"].includes(dmgType))) {
      addError("Skills", `Skill '${s.id}' param 'damage_type' is '${dmgType}' (expected physical/fire/cold/lightning/chaos).`);
    }
    for (const key of ["sfx", "nova_sfx"]) {
      const v = params[key];
      if (typeof v === "string" && !sfxIds.has(v)) {
        addError("Skills", `Skill '${s.id}' param '${key}' references unknown sfx '${v}'.`);
      }
    }
    for (const m of (s.mods ?? []) as Array<{ stat?: unknown; mode?: unknown }>) {
      if (typeof m.stat !== "string" || !statIds.has(m.stat)) {
        addError("Skills", `Passive skill '${s.id}' mod references unknown stat '${m.stat}'.`);
      }
      if (typeof m.mode !== "string" || !modModeIds.has(m.mode)) {
        addError("Skills", `Passive skill '${s.id}' mod has unknown mode '${m.mode}'.`);
      }
    }
  }

  // 3. Classes -> skills / achievements / stats
  for (const c of classList) {
    if (c.innate_skill && !skillIds.has(c.innate_skill)) {
      addError("Class", `Class '${c.id}' innate_skill references unknown skill '${c.innate_skill}'.`);
    }
    if (c.unlock_achievement && !achievementIds.has(c.unlock_achievement)) {
      addError("Class", `Class '${c.id}' references unknown achievement '${c.unlock_achievement}'.`);
    }
    if (c.trait_stat && !statIds.has(c.trait_stat)) {
      addError("Class", `Class '${c.id}' trait_stat references unknown stat '${c.trait_stat}'.`);
    }
    for (const k of Object.keys(c.extra_stats ?? {})) {
      if (!statIds.has(k)) {
        addError("Class", `Class '${c.id}' extra_stats references unknown stat '${k}'.`);
      }
    }
  }

  // 4. Achievements -> classes / stages
  for (const a of achievementList) {
    if (a.reward_cell && !classIds.has(a.reward_cell)) {
      addError("Achievement", `Achievement '${a.id}' rewards unknown class '${a.reward_cell}'.`);
    }
    for (const key of ["stage_id", "unlock_stage", "unlock_hard_stage"]) {
      if (a[key] && !stageIds.has(a[key])) {
        addError("Achievement", `Achievement '${a.id}' references unknown stage '${a[key]}' (${key}).`);
      }
    }
  }

  // 5. Enemies: threat mode, codex coverage, spawn refs
  for (const e of enemyList) {
    if (!threatModes.has(e.threat_mode)) {
      addError("Enemy", `Enemy '${e.id}' has unknown threat_mode '${e.threat_mode}'.`);
    }
    const traits = (e.traits ?? {}) as Record<string, any>;
    for (const [tname, tdef] of Object.entries(traits)) {
      if (tdef && typeof tdef === "object") {
        const spawn = (tdef as Record<string, unknown>).spawn;
        if (typeof spawn === "string" && !enemyIds.has(spawn)) {
          addError("Enemy", `Enemy '${e.id}' trait '${tname}' spawns unknown enemy '${spawn}'.`);
        }
      }
    }
  }
  for (const row of enemyCodexList) {
    if (!enemyIds.has(row.id)) {
      addError("Codex", `Enemy codex row '${row.id}' has no matching enemy in enemies.json.`);
    }
  }

  // 6. Equipment: category / energy / stats / generator drawback
  for (const g of equipmentList) {
    if (!gearCategories.has(g.category)) {
      addError("Gear", `Equipment '${g.id}' has unknown category '${g.category}'.`);
    }
    if (!Number.isInteger(g.energy_cost) || g.energy_cost < -1 || g.energy_cost > 4) {
      addError("Gear", `Equipment '${g.id}' energy_cost ${g.energy_cost} outside [-1, 4].`);
    }
    if (g.energy_cost < 0 && ((g.drawback ?? []) as unknown[]).length === 0) {
      addError("Gear", `Generator gear '${g.id}' must carry a drawback.`);
    }
    for (const key of ["modifiers", "drawback"]) {
      for (const m of ((g[key] ?? []) as Array<Record<string, unknown>>)) {
        if (typeof m.stat !== "string" || !statIds.has(m.stat)) {
          addError("Gear", `Equipment '${g.id}' ${key} entry references unknown stat '${m.stat}'.`);
        }
        if (typeof m.unit !== "string" || !unitIds.has(m.unit)) {
          addError("Gear", `Equipment '${g.id}' ${key} entry has unknown unit '${m.unit}'.`);
        }
        if (typeof m.scaling_stat === "string" && m.scaling_stat.length > 0) {
          if (!statIds.has(m.scaling_stat)) {
            addError("Gear", `Equipment '${g.id}' ${key} entry references unknown scaling_stat '${m.scaling_stat}'.`);
          }
          if (typeof m.scale_per !== "number" || m.scale_per <= 0) {
            addError("Gear", `Equipment '${g.id}' ${key} entry has non-positive scale_per.`);
          }
        }
      }
    }
  }

  // 7. Passive traits + tree
  for (const t of traitList) {
    if (t.rarity && !traitRarities.has(t.rarity)) {
      addError("Passive", `Trait '${t.id}' has unknown rarity '${t.rarity}'.`);
    }
    for (const m of ((t.modifiers ?? []) as Array<Record<string, unknown>>)) {
      if (typeof m.stat !== "string" || !statIds.has(m.stat)) {
        addError("Passive", `Trait '${t.id}' modifies unknown stat '${m.stat}'.`);
      }
      if (typeof m.unit !== "string" || !unitIds.has(m.unit)) {
        addError("Passive", `Trait '${t.id}' modifier has unknown unit '${m.unit}'.`);
      }
    }
  }
  for (const n of nodeList) {
    if (!traitIds.has(n.trait)) {
      addError("Passive", `Tree node '${n.id}' references unknown trait '${n.trait}'.`);
    }
    if (n.branch && !traitBranches.has(n.branch)) {
      addError("Passive", `Tree node '${n.id}' has unknown branch '${n.branch}'.`);
    }
  }
  for (const e of edgeList) {
    const [from, to] = Array.isArray(e) ? e : [undefined, undefined];
    if (typeof from !== "string" || !nodeIds.has(from)) {
      addError("Passive", `Tree edge references unknown node '${from}'.`);
    }
    if (typeof to !== "string" || !nodeIds.has(to)) {
      addError("Passive", `Tree edge references unknown node '${to}'.`);
    }
  }
  for (const [cls, start] of Object.entries(startNodes)) {
    if (!classIds.has(cls)) {
      addError("Passive", `Start node key '${cls}' is not a known class.`);
    }
    if (!nodeIds.has(start)) {
      addError("Passive", `Start node for '${cls}' references unknown node '${start}'.`);
    }
  }

  // 8. Stages: effect stat refs + kind/prop vocab
  for (const s of stageList) {
    for (const fx of ((s.effects ?? []) as Array<Record<string, any>>)) {
      if (typeof fx.kind === "string" && !effectKinds.has(fx.kind)) {
        addError("Stage", `Stage '${s.id}' effect has unknown kind '${fx.kind}'.`);
      }
      if (typeof fx.prop === "string" && !stageProps.has(fx.prop)) {
        addError("Stage", `Stage '${s.id}' effect has unknown prop '${fx.prop}'.`);
      }
      if (typeof fx.stat === "string" && !statIds.has(fx.stat)) {
        addError("Stage", `Stage '${s.id}' effect references unknown stat '${fx.stat}'.`);
      }
      const statId = (fx.overrides as Record<string, unknown> | undefined)?.stat_id;
      if (typeof statId === "string" && !statIds.has(statId)) {
        addError("Stage", `Stage '${s.id}' effect overrides unknown stat_id '${statId}'.`);
      }
    }
  }

  // 9. Unreferenced master rows (warnings, vistrace dead-asset pattern)
  const placedTraits = new Set(nodeList.map((n) => n.trait));
  for (const t of traitIds) {
    if (!placedTraits.has(t)) {
      addWarning("Passive", `Trait '${t}' is never placed on the passive tree.`);
    }
  }

  return splitResult(issues);
}
