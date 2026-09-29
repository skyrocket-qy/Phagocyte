# Game Configuration Pipeline (`tools/config`)

Type-safe, single-source-of-truth data authoring and validation system for VistRace Godot. All game data is defined in TypeScript with compile-time foreign key checking and exported directly into `assets/data/*.json`.

---

## Directory Structure

```
tools/config/
├── src/
│   ├── lib/                       # Core primitives & shared types
│   │   ├── colors.ts              # Centralized color palette definitions & ColorToken engine
│   │   ├── palette-types.ts       # Character sprite palette definitions & schemas
│   │   ├── registry.ts            # IdOf & array/map helper types
│   │   ├── shared-types.ts        # StatId, ModType, StatModifier, SrgbaColor, Vec2, etc.
│   │   └── cross-validator.ts     # Referential completeness & balance integrity checker
│   ├── schemas/                   # Zod runtime validation schemas, TS interfaces & Enums
│   │   ├── audio.schema.ts        # BgmId, SfxId, Audio catalogs
│   │   ├── class.schema.ts        # HeroClassId, MasteryId, hero classes, masteries, starters
│   │   ├── combat.schema.ts       # Auras, status effects
│   │   ├── enemy.schema.ts        # EnemyId, enemies, rarities, boss encounters, minions, spawner
│   │   ├── gear.schema.ts         # GearClassId, GearBaseId, UniqueItemId, AffixId, affix tiers, affixes
│   │   ├── gems.schema.ts         # ActiveGemId, SupportGemId, TriggerGemId, active/support/trigger gems
│   │   ├── skills.schema.ts       # SkillId, DamageType, TargetMode, standalone skills
│   │   ├── loot.schema.ts         # Crafting, currencies, loot filters
│   │   ├── map.schema.ts          # BiomeId, biomes, survivor maps, defense maps, game modes, affixes
│   │   ├── passive.schema.ts      # Passive traits & trait groups
│   │   ├── progression.schema.ts  # Achievements, progression config, run bonuses, upgrade pool
│   │   └── visual.schema.ts       # Sprite animation config & palettes
│   ├── data/                      # Single Source of Truth (81 authored data modules)
│   │   ├── audio/                 # bgm.ts, sfx.ts
│   │   ├── class/                 # hero_classes.ts, mastery_classes.ts, starters.ts
│   │   ├── combat/                # auras.ts, status_effects.ts
│   │   ├── enemy/                 # enemies.ts, rarities.ts, boss_encounters.ts, minions.ts, spawner_config.ts
│   │   ├── gear/                  # bases.ts, classes.ts, uniques.ts, affix_tiers.ts, affixes/*.ts (13 files)
│   │   ├── gems/                  # active.ts, support.ts, trigger.ts (1:1 maps to skills)
│   │   ├── skills/                # skills.ts (standalone skill abilities)
│   │   ├── loot/                  # crafting.ts, currencies.ts, filters/*.ts (3 files)
│   │   ├── map/                   # biomes.ts, defense_maps.ts, game_modes.ts, map_affixes.ts, survivor_maps.ts
│   │   ├── passive/               # group/*.ts (4 files), traits/*.ts (5 files)
│   │   ├── progression/           # achievements.ts, config.ts, run_bonuses.ts, upgrade_pool.ts
│   │   └── visual/                # animations.ts, palettes/*.ts (24 files)
│   ├── index.ts                   # Manifest registry bundling all 81 data sources
│   └── export.ts                  # Fast validation & mirror export engine
├── package.json
└── tsconfig.json
```

---

## Core Features & Mechanisms

### 1. Enum ID + Shift+F12 Data Navigation
All primary entity IDs are defined as string enums in their respective schema files. In the IDE:
- **F12** on `HeroClassId.Warrior` navigates directly to the enum identifier definition.
- **Shift + F12** (Find All References / Peek References) instantly reveals the exact data block in [`hero_classes.ts`](file:///Users/zelin/vistrace/tools/config/src/data/class/hero_classes.ts) and all places referencing it.

```ts
// src/data/class/hero_classes.ts
import { HeroClassId, MasteryId, type HeroClassDef } from "../../schemas/class.schema";

export const HeroClasses: readonly HeroClassDef[] = [
  {
    id: HeroClassId.Warrior,
    description: "hero_classes-warrior-desc",
    str: 32,
    dex: 14,
    int: 14,
    masteries: [
      MasteryId.Paladin,
      MasteryId.Berserker,
      MasteryId.Gladiator,
    ],
  },
];
```

---

### 2. Inline Typo Red Squiggle Diagnostics (0ms Feedback)
Misspelled property names or invalid references trigger instant inline compiler diagnostics before saving or exporting.

```ts
// Compile-time error caught immediately by TypeScript language server
export const ForestMap: SurvivorMapDef = {
  name: "Deep Forest",
  // ❌ error TS2339: Property 'DireWolff' does not exist on type 'typeof EnemyId'.
  general_enemies: [EnemyId.DireWolff, EnemyId.Imp],
  // ❌ error TS2353: Object literal may only specify known properties ('bgm_idd' does not exist).
  bgm_idd: BgmId.Forest, 
};
```

---

### 3. Zero ESM Circular Import Deadlocks
Because string enums evaluate to primitive string constants at runtime, circular cross-file relationships (e.g. `HeroClasses` referencing `MasteryId` while `MasteryClasses` references `HeroClassId`) never deadlock or crash Node.js module initialization.

```ts
// src/data/class/mastery_classes.ts
import { HeroClassId, MasteryId, type MasteryClassDef } from "../../schemas/class.schema";
import { StatId, ModType } from "../../lib/shared-types";

export const MasteryClasses: readonly MasteryClassDef[] = [
  {
    id: MasteryId.Paladin,
    name: "Paladin",
    base_class: HeroClassId.Warrior, // ✅ Clean primitive enum reference
    description: "Holy defender combining heavy armor, shields, and life regen.",
    innate_modifiers: [
      { stat: StatId.Armor, type: ModType.Percent, value: 25 },
      { stat: StatId.BlockChancePct, type: ModType.Flat, value: 15 },
    ],
  },
];
```

---

### 4. Stats $\rightarrow$ Affix $\rightarrow$ Gear Tier Filtering
Using TypeScript generics and indexed access types (`keyof typeof GearAffixTiers[TId]["tiers"][TModType]`), `affixPool()` strictly restricts `allowed_tiers` on a gear affix to only the tiers that actually exist on that specific affix.

```ts
import { AffixId, type GearAffixDef } from "../../../schemas/gear.schema";
import type { GearAffixTiers } from "../affix_tiers";

type AffixKey = keyof typeof GearAffixTiers;

export const AffixesBow: readonly GearAffixDef<AffixKey>[] = [
  {
    id: "prefix_attack_damage",
    affix_id: AffixId.AttackDamage,
    type: "prefix",
    mod_type: "percent",
    allowed_tiers: ["t1", "t2", "t3"], // ✅ Valid: AttackDamage has t1, t2, t3
    // allowed_tiers: ["t99"],         // ❌ Compile error: '"t99"' is not assignable
  },
];
```

---

### 5. Polymorphic Data & Discriminated Unions (Component-Based Data)
Game mechanics like Skill Behaviors, AI Triggers, or Quest Rewards require completely different fields depending on a component type tag. Discriminated unions enforce variant-specific fields while forbidding unrelated ones.

```ts
export type SkillBehaviorDef =
  | { projectile: Record<string, unknown> }
  | { aoe: Record<string, unknown> }
  | { minion: Record<string, unknown> }
  | { reservation: Record<string, unknown> }
  | { aura: Record<string, unknown> }
  | { area_damage: Record<string, unknown> };
```

---

### 6. Tag-Based Compatibility Filtering
Enforces category compatibility at authoring time (e.g., Bows can only accept affixes containing the `"attack"` or `"ranged"` tag, but forbid `"melee"` or `"shield"`).

```ts
type AffixTag = "attack" | "spell" | "melee" | "ranged" | "defense";

interface AffixWithTags<TTags extends readonly AffixTag[]> {
  id: string;
  tags: TTags;
}

// Restrict Gear Base to only accept affixes containing compatible tags
interface WeaponBaseDef<TAllowedTag extends AffixTag> {
  id: string;
  allowed_affixes: readonly AffixWithTags<readonly TAllowedTag[]>[];
}
```

---

### 7. Unreferenced Master Item & Dead Asset Validation
Catches dangling assets, orphan items, or dead references at validation time (e.g. items defined in `uniques.ts` that never appear in any boss loot table, drop table, or shop).

```ts
// Automated cross-validation in src/lib/cross-validator.ts:
export function runCrossValidation(data: { ... }) {
  // Checks all master unique items are referenced in boss encounter loot tables
  for (const unique of uniqueList) {
    if (!referencedUniqueIds.has(unique.id)) {
      addWarning("Loot", `Master unique item '${unique.id}' is not referenced in any boss encounter loot table.`);
    }
  }
}
```

---

### 8. Loot Weight Sanity Checks
Guarantees drop rates and weight tables are logically sound (non-zero positive weights, valid probability ranges, or normalized totals).

```ts
import { z } from "zod";

export const BossLootTableZodSchema = z
  .array(
    z.object({
      item_id: z.string().min(1),
      weight: z.number().int().positive(), // Must be > 0
    })
  )
  .min(1, "Loot table must contain at least one drop entry");
```

---

## Future Roadmap (TODO)

- [ ] **Automated Multi-Language Runtime Codegen**:
  - Automatically generate C# records/DTOs directly from TypeScript schemas to eliminate manual C# DTO synchronization.
- [ ] **Graph-Based Dead Code / Dead Asset Purge**:
  - Cross-check `assets/vfx/`, `assets/audio/`, and 3D GLBs against TypeScript dataset references to detect unused physical asset files.

---

## Commands

```bash
# Check all types and validate data schemas without writing files (CI mode)
pnpm run check

# Validate and export all JSON files to assets/data/
pnpm run build

# Watch mode for continuous validation and export during authoring
pnpm run watch
```
