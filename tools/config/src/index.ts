export * from "./ids";
import type { ZodTypeAny } from "zod";
import { AudioArrayZodSchema } from "./schemas/audio.schema";
import { ClassStarterArrayZodSchema, HeroClassArrayZodSchema, MasteryClassArrayZodSchema } from "./schemas/class.schema";
import { AuraArrayZodSchema, StatusEffectArrayZodSchema, CombatConfigZodSchema } from "./schemas/combat.schema";
import { BossEncounterArrayZodSchema, EnemyArrayZodSchema, EnemyRaritiesFileZodSchema, MinionArrayZodSchema, SpawnerConfigZodSchema } from "./schemas/enemy.schema";
import { AffixTiersFileZodSchema, GearAffixArrayZodSchema, GearBaseArrayZodSchema, GearClassArrayZodSchema, GearUniqueArrayZodSchema } from "./schemas/gear.schema";
import { ActiveGemArrayZodSchema, SupportGemArrayZodSchema, TriggerGemArrayZodSchema } from "./schemas/gems.schema";
import { ProjectileArrayZodSchema } from "./schemas/projectiles.schema";
import { SkillArrayZodSchema } from "./schemas/skills.schema";
import { CraftArrayZodSchema, CurrencyArrayZodSchema, LootFilterZodSchema } from "./schemas/loot.schema";
import { BiomeArrayZodSchema, DefenseMapArrayZodSchema, GameModeConfigZodSchema, MapAffixArrayZodSchema, SurvivorMapArrayZodSchema } from "./schemas/map.schema";
import { PassiveGroupZodSchema, PassiveTraitArrayZodSchema } from "./schemas/passive.schema";
import { AchievementArrayZodSchema, ProgressionConfigZodSchema, RunBonusesFileZodSchema, UpgradePoolZodSchema } from "./schemas/progression.schema";
import { AnimationsConfigZodSchema, PaletteDefZodSchema } from "./schemas/visual.schema";
import { DecorationArrayZodSchema, TerrainArrayZodSchema, TileArrayZodSchema } from "./schemas/environment.schema";
import { UiElementArrayZodSchema } from "./schemas/ui.schema";

import { Bgm } from "./data/audio/bgm";
import { Sfx } from "./data/audio/sfx";
import { HeroClasses } from "./data/class/hero_classes";
import { MasteryClasses } from "./data/class/mastery_classes";
import { ClassStarters } from "./data/class/starters";
import { Auras } from "./data/combat/auras";
import { StatusEffects } from "./data/combat/status_effects";
import { CombatConfig } from "./data/combat/combat_config";
import { Enemies } from "./data/enemy/enemies";
import { Minions } from "./data/enemy/minions";
import { EnemyRarities } from "./data/enemy/rarities";
import { SpawnerConfig } from "./data/enemy/spawner_config";
import { BossEncounters } from "./data/enemy/boss_encounters";
import { GearClasses } from "./data/gear/classes";
import { GearBases } from "./data/gear/bases";
import { GearUniques } from "./data/gear/uniques";
import { GearAffixTiers } from "./data/gear/affix_tiers";
import { AffixesAmulet } from "./data/gear/affixes/amulet";
import { AffixesBelt } from "./data/gear/affixes/belt";
import { AffixesBodyArmour } from "./data/gear/affixes/body_armour";
import { AffixesBoots } from "./data/gear/affixes/boots";
import { AffixesBow } from "./data/gear/affixes/bow";
import { AffixesDagger } from "./data/gear/affixes/dagger";
import { AffixesGloves } from "./data/gear/affixes/gloves";
import { AffixesHelmet } from "./data/gear/affixes/helmet";
import { AffixesOneHandSword } from "./data/gear/affixes/one_hand_sword";
import { AffixesRing } from "./data/gear/affixes/ring";
import { AffixesShield } from "./data/gear/affixes/shield";
import { AffixesStaff } from "./data/gear/affixes/staff";
import { AffixesWand } from "./data/gear/affixes/wand";
import { ActiveGems } from "./data/gems/active";
import { Skills } from "./data/skills/skills";
import { Projectiles } from "./data/skills/projectiles";
import { SupportGems } from "./data/gems/support";
import { TriggerGems } from "./data/gems/trigger";
import { Crafting } from "./data/loot/crafting";
import { Currencies } from "./data/loot/currencies";
import { LootFilterDefault } from "./data/loot/filters/default";
import { LootFilterEndgame } from "./data/loot/filters/endgame";
import { LootFilterStrict } from "./data/loot/filters/strict";
import { Biomes } from "./data/map/biomes";
import { SurvivorMaps } from "./data/map/survivor_maps";
import { DefenseMaps } from "./data/map/defense_maps";
import { GameModes } from "./data/map/game_modes";
import { MapAffixes } from "./data/map/map_affixes";
import { PassiveGroupAttributes } from "./data/passive/group/attributes";
import { PassiveGroupClass } from "./data/passive/group/class";
import { PassiveGroupDefensive } from "./data/passive/group/defensive";
import { PassiveGroupOffensive } from "./data/passive/group/offensive";
import { PassiveTraitsClassStart } from "./data/passive/traits/class_start";
import { PassiveTraitsMagic } from "./data/passive/traits/magic";
import { PassiveTraitsNormal } from "./data/passive/traits/normal";
import { PassiveTraitsRare } from "./data/passive/traits/rare";
import { PassiveTraitsUnique } from "./data/passive/traits/unique";
import { Achievements } from "./data/progression/achievements";
import { ProgressionConfig } from "./data/progression/config";
import { RunBonuses } from "./data/progression/run_bonuses";
import { UpgradePool } from "./data/progression/upgrade_pool";
import { Animations } from "./data/visual/animations";
import { AgonizerPalette } from "./data/visual/palettes/agonizer_palette";
import { AlchemistPalette } from "./data/visual/palettes/alchemist_palette";
import { AssassinPalette } from "./data/visual/palettes/assassin_palette";
import { BeastmasterPalette } from "./data/visual/palettes/beastmaster_palette";
import { BelieverPalette } from "./data/visual/palettes/believer_palette";
import { DancerPalette } from "./data/visual/palettes/dancer_palette";
import { DruidPalette } from "./data/visual/palettes/druid_palette";
import { InquisitorPalette } from "./data/visual/palettes/inquisitor_palette";
import { MimicPalette } from "./data/visual/palettes/mimic_palette";
import { MonkPalette } from "./data/visual/palettes/monk_palette";
import { NoblePalette } from "./data/visual/palettes/noble_palette";
import { PaladinPalette } from "./data/visual/palettes/paladin_palette";
import { RoguePalette } from "./data/visual/palettes/rogue_palette";
import { SaboteurPalette } from "./data/visual/palettes/saboteur_palette";
import { SamuraiPalette } from "./data/visual/palettes/samurai_palette";
import { ScholarPalette } from "./data/visual/palettes/scholar_palette";
import { ShamanPalette } from "./data/visual/palettes/shaman_palette";
import { ShaperPalette } from "./data/visual/palettes/shaper_palette";
import { SniperPalette } from "./data/visual/palettes/sniper_palette";
import { StalkerPalette } from "./data/visual/palettes/stalker_palette";
import { ThiefPalette } from "./data/visual/palettes/thief_palette";
import { VagabondPalette } from "./data/visual/palettes/vagabond_palette";
import { WarriorPalette } from "./data/visual/palettes/warrior_palette";
import { WitchPalette } from "./data/visual/palettes/witch_palette";

import { Decorations } from "./data/environment/decorations";
import { Terrains } from "./data/environment/terrains";
import { Tiles } from "./data/environment/tiles";
import { UiElements } from "./data/ui/ui_elements";

export interface ManifestEntry {
  file: string;
  data: unknown;
  schema?: ZodTypeAny;
}

export const configManifest: ManifestEntry[] = [
  { file: "audio/bgm.json", data: Bgm, schema: AudioArrayZodSchema },
  { file: "audio/sfx.json", data: Sfx, schema: AudioArrayZodSchema },
  { file: "class/hero_classes.json", data: HeroClasses, schema: HeroClassArrayZodSchema },
  { file: "class/mastery_classes.json", data: MasteryClasses, schema: MasteryClassArrayZodSchema },
  { file: "class/starters.json", data: ClassStarters, schema: ClassStarterArrayZodSchema },
  { file: "combat/auras.json", data: Auras, schema: AuraArrayZodSchema },
  { file: "combat/status_effects.json", data: StatusEffects, schema: StatusEffectArrayZodSchema },
  { file: "combat/combat_config.json", data: CombatConfig, schema: CombatConfigZodSchema },
  { file: "enemy/enemies.json", data: Enemies, schema: EnemyArrayZodSchema },
  { file: "enemy/minions.json", data: Minions, schema: MinionArrayZodSchema },
  { file: "enemy/rarities.json", data: EnemyRarities, schema: EnemyRaritiesFileZodSchema },
  { file: "enemy/spawner_config.json", data: SpawnerConfig, schema: SpawnerConfigZodSchema },
  { file: "enemy/boss_encounters.json", data: BossEncounters, schema: BossEncounterArrayZodSchema },
  { file: "gear/classes.json", data: GearClasses, schema: GearClassArrayZodSchema },
  { file: "gear/bases.json", data: GearBases, schema: GearBaseArrayZodSchema },
  { file: "gear/uniques.json", data: GearUniques, schema: GearUniqueArrayZodSchema },
  { file: "gear/affix_tiers.json", data: GearAffixTiers, schema: AffixTiersFileZodSchema },
  { file: "gear/affixes/amulet.json", data: AffixesAmulet, schema: GearAffixArrayZodSchema },
  { file: "gear/affixes/belt.json", data: AffixesBelt, schema: GearAffixArrayZodSchema },
  { file: "gear/affixes/body_armour.json", data: AffixesBodyArmour, schema: GearAffixArrayZodSchema },
  { file: "gear/affixes/boots.json", data: AffixesBoots, schema: GearAffixArrayZodSchema },
  { file: "gear/affixes/bow.json", data: AffixesBow, schema: GearAffixArrayZodSchema },
  { file: "gear/affixes/dagger.json", data: AffixesDagger, schema: GearAffixArrayZodSchema },
  { file: "gear/affixes/gloves.json", data: AffixesGloves, schema: GearAffixArrayZodSchema },
  { file: "gear/affixes/helmet.json", data: AffixesHelmet, schema: GearAffixArrayZodSchema },
  { file: "gear/affixes/one_hand_sword.json", data: AffixesOneHandSword, schema: GearAffixArrayZodSchema },
  { file: "gear/affixes/ring.json", data: AffixesRing, schema: GearAffixArrayZodSchema },
  { file: "gear/affixes/shield.json", data: AffixesShield, schema: GearAffixArrayZodSchema },
  { file: "gear/affixes/staff.json", data: AffixesStaff, schema: GearAffixArrayZodSchema },
  { file: "gear/affixes/wand.json", data: AffixesWand, schema: GearAffixArrayZodSchema },
  { file: "gems/active.json", data: ActiveGems, schema: ActiveGemArrayZodSchema },
  { file: "skills/skills.json", data: Skills, schema: SkillArrayZodSchema },
  { file: "skills/projectiles.json", data: Projectiles, schema: ProjectileArrayZodSchema },
  { file: "gems/support.json", data: SupportGems, schema: SupportGemArrayZodSchema },
  { file: "gems/trigger.json", data: TriggerGems, schema: TriggerGemArrayZodSchema },
  { file: "loot/crafting.json", data: Crafting, schema: CraftArrayZodSchema },
  { file: "loot/currencies.json", data: Currencies, schema: CurrencyArrayZodSchema },
  { file: "loot/filters/default.json", data: LootFilterDefault, schema: LootFilterZodSchema },
  { file: "loot/filters/endgame.json", data: LootFilterEndgame, schema: LootFilterZodSchema },
  { file: "loot/filters/strict.json", data: LootFilterStrict, schema: LootFilterZodSchema },
  { file: "map/biomes.json", data: Biomes, schema: BiomeArrayZodSchema },
  { file: "map/survivor_maps.json", data: SurvivorMaps, schema: SurvivorMapArrayZodSchema },
  { file: "map/defense_maps.json", data: DefenseMaps, schema: DefenseMapArrayZodSchema },
  { file: "map/game_modes.json", data: GameModes, schema: GameModeConfigZodSchema },
  { file: "map/map_affixes.json", data: MapAffixes, schema: MapAffixArrayZodSchema },
  { file: "environment/decorations.json", data: Decorations, schema: DecorationArrayZodSchema },
  { file: "environment/terrains.json", data: Terrains, schema: TerrainArrayZodSchema },
  { file: "environment/tiles.json", data: Tiles, schema: TileArrayZodSchema },
  { file: "ui/ui_elements.json", data: UiElements, schema: UiElementArrayZodSchema },
  { file: "passive/group/attributes.json", data: PassiveGroupAttributes, schema: PassiveGroupZodSchema },
  { file: "passive/group/class.json", data: PassiveGroupClass, schema: PassiveGroupZodSchema },
  { file: "passive/group/defensive.json", data: PassiveGroupDefensive, schema: PassiveGroupZodSchema },
  { file: "passive/group/offensive.json", data: PassiveGroupOffensive, schema: PassiveGroupZodSchema },
  { file: "passive/traits/class_start.json", data: PassiveTraitsClassStart, schema: PassiveTraitArrayZodSchema },
  { file: "passive/traits/magic.json", data: PassiveTraitsMagic, schema: PassiveTraitArrayZodSchema },
  { file: "passive/traits/normal.json", data: PassiveTraitsNormal, schema: PassiveTraitArrayZodSchema },
  { file: "passive/traits/rare.json", data: PassiveTraitsRare, schema: PassiveTraitArrayZodSchema },
  { file: "passive/traits/unique.json", data: PassiveTraitsUnique, schema: PassiveTraitArrayZodSchema },
  { file: "progression/achievements.json", data: Achievements, schema: AchievementArrayZodSchema },
  { file: "progression/config.json", data: ProgressionConfig, schema: ProgressionConfigZodSchema },
  { file: "progression/run_bonuses.json", data: RunBonuses, schema: RunBonusesFileZodSchema },
  { file: "progression/upgrade_pool.json", data: UpgradePool, schema: UpgradePoolZodSchema },
  { file: "visual/animations.json", data: Animations, schema: AnimationsConfigZodSchema },
  { file: "visual/palettes/agonizer_palette.json", data: AgonizerPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/alchemist_palette.json", data: AlchemistPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/assassin_palette.json", data: AssassinPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/beastmaster_palette.json", data: BeastmasterPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/believer_palette.json", data: BelieverPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/dancer_palette.json", data: DancerPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/druid_palette.json", data: DruidPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/inquisitor_palette.json", data: InquisitorPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/mimic_palette.json", data: MimicPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/monk_palette.json", data: MonkPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/noble_palette.json", data: NoblePalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/paladin_palette.json", data: PaladinPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/rogue_palette.json", data: RoguePalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/saboteur_palette.json", data: SaboteurPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/samurai_palette.json", data: SamuraiPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/scholar_palette.json", data: ScholarPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/shaman_palette.json", data: ShamanPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/shaper_palette.json", data: ShaperPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/sniper_palette.json", data: SniperPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/stalker_palette.json", data: StalkerPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/thief_palette.json", data: ThiefPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/vagabond_palette.json", data: VagabondPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/warrior_palette.json", data: WarriorPalette, schema: PaletteDefZodSchema },
  { file: "visual/palettes/witch_palette.json", data: WitchPalette, schema: PaletteDefZodSchema },
];

// Re-export all data modules for easy import
export * from "./data/audio/bgm";
export * from "./data/audio/sfx";
export * from "./data/class/hero_classes";
export * from "./data/class/mastery_classes";
export * from "./data/class/starters";
export * from "./data/combat/auras";
export * from "./data/combat/status_effects";
export * from "./data/enemy/boss_encounters";
export * from "./data/enemy/enemies";
export * from "./data/enemy/minions";
export * from "./data/enemy/rarities";
export * from "./data/enemy/spawner_config";
export * from "./data/gear/affix_tiers";
export * from "./data/gear/affixes/amulet";
export * from "./data/gear/affixes/belt";
export * from "./data/gear/affixes/body_armour";
export * from "./data/gear/affixes/boots";
export * from "./data/gear/affixes/bow";
export * from "./data/gear/affixes/dagger";
export * from "./data/gear/affixes/gloves";
export * from "./data/gear/affixes/helmet";
export * from "./data/gear/affixes/one_hand_sword";
export * from "./data/gear/affixes/ring";
export * from "./data/gear/affixes/shield";
export * from "./data/gear/affixes/staff";
export * from "./data/gear/affixes/wand";
export * from "./data/gear/bases";
export * from "./data/gear/classes";
export * from "./data/gear/uniques";
export * from "./data/gems/active";
export * from "./data/skills/skills";
export * from "./data/gems/support";
export * from "./data/gems/trigger";
export * from "./data/loot/crafting";
export * from "./data/loot/currencies";
export * from "./data/loot/filters/default";
export * from "./data/loot/filters/endgame";
export * from "./data/loot/filters/strict";
export * from "./data/map/biomes";
export * from "./data/map/defense_maps";
export * from "./data/map/game_modes";
export * from "./data/map/map_affixes";
export * from "./data/map/survivor_maps";
export * from "./data/passive/group/attributes";
export * from "./data/passive/group/class";
export * from "./data/passive/group/defensive";
export * from "./data/passive/group/offensive";
export * from "./data/passive/traits/class_start";
export * from "./data/passive/traits/magic";
export * from "./data/passive/traits/normal";
export * from "./data/passive/traits/rare";
export * from "./data/passive/traits/unique";
export * from "./data/progression/achievements";
export * from "./data/progression/config";
export * from "./data/progression/run_bonuses";
export * from "./data/progression/upgrade_pool";
export * from "./data/visual/animations";
export * from "./data/visual/palettes/agonizer_palette";
export * from "./data/visual/palettes/alchemist_palette";
export * from "./data/visual/palettes/assassin_palette";
export * from "./data/visual/palettes/beastmaster_palette";
export * from "./data/visual/palettes/believer_palette";
export * from "./data/visual/palettes/dancer_palette";
export * from "./data/visual/palettes/druid_palette";
export * from "./data/visual/palettes/inquisitor_palette";
export * from "./data/visual/palettes/mimic_palette";
export * from "./data/visual/palettes/monk_palette";
export * from "./data/visual/palettes/noble_palette";
export * from "./data/visual/palettes/paladin_palette";
export * from "./data/visual/palettes/rogue_palette";
export * from "./data/visual/palettes/saboteur_palette";
export * from "./data/visual/palettes/samurai_palette";
export * from "./data/visual/palettes/scholar_palette";
export * from "./data/visual/palettes/shaman_palette";
export * from "./data/visual/palettes/shaper_palette";
export * from "./data/visual/palettes/sniper_palette";
export * from "./data/visual/palettes/stalker_palette";
export * from "./data/visual/palettes/thief_palette";
export * from "./data/visual/palettes/vagabond_palette";
export * from "./data/visual/palettes/warrior_palette";
export * from "./data/visual/palettes/witch_palette";
export * from "./data/environment/decorations";
export * from "./data/environment/terrains";
export * from "./data/environment/tiles";
export * from "./data/ui/ui_elements";
export * from "./schemas/environment.schema";
export * from "./schemas/ui.schema";
